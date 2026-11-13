using System.Collections.Generic;
using System.Linq;
using Lab.Core;
using Lab.Core.EditorTools;
using Lab.W01.VariableScrew.EditorTools;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Lab.W01.AdjustablePlate.EditorTools
{
    /// <summary>
    /// Builds the adjustable plate demo from the lab's demo template: the stand-in foot, the plate fitted to
    /// the fibula and then moved out of place, a see-through copy where it belongs, the slider panel and the keys.
    /// </summary>
    public static class PlateDemoBuilder
    {
        const float DistalMargin = 0.006f;
        const float RestGap = 0.0004f;
        const float CameraDistance = 0.2f;
        const float BannerCentre = 0.62f;
        const float BannerTop = 0.76f;

        const string Hint =
            "Line the plate up with the blue outline on the bone, using the sliders or the arrow keys. R resets.\n" +
            "Right-drag orbits, middle-drag or Shift + right-drag pans, scroll zooms.";
        const string PanelTitle = "Plate placement";
        const string PanelHelp = "Up and Down pick a row. Left and Right nudge it by 1 mm or 1°, with Shift by 0.1.";

        [MenuItem("Lab/W01/Build Adjustable Plate Demo")]
        public static void Build()
        {
            if (OverwriteGuard.Allow(PlatePaths.ScenePath)) Rebuild();
        }

        /// <summary>Builds without asking, for scripted runs where no one can answer a dialog.</summary>
        public static void Rebuild()
        {
            // Open the template first: opening a scene unloads assets loaded before it.
            var scene = EditorSceneManager.OpenScene(DemoTemplateBuilder.ScenePath, OpenSceneMode.Single);

            var footAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PlatePaths.FootModelPath);
            var plateAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PlatePaths.PlateModelPath);
            var ghostShader = Shader.Find(PlatePaths.GhostShaderName);
            if (footAsset == null || plateAsset == null || ghostShader == null)
            {
                Debug.LogError("The foot or plate model, or the Lab/Ghost shader, is missing, so the demo can't be built.");
                return;
            }

            EnsureFolder(PlatePaths.MaterialsFolder);
            var boneMaterial = LitMaterial("Bone", new Color(0.86f, 0.82f, 0.72f), 0f, 0.25f);
            var plateMaterial = LitMaterial("Plate", new Color(0.5f, 0.56f, 0.66f), 0.6f, 0.5f);
            var ghostMaterial = Material("Ghost", ghostShader);

            var foot = (GameObject)PrefabUtility.InstantiatePrefab(footAsset, scene);
            foot.name = "Stand-in Foot";
            Assign(foot, boneMaterial);
            var meshes = foot.GetComponentsInChildren<MeshFilter>();
            var fibula = meshes.Single(m => m.name == PlatePaths.FibulaName);
            var otherBones = meshes.Where(m => m != fibula).SelectMany(WorldVertices).ToList();

            var fit = FibulaFrame.Place(WorldVertices(fibula).ToList(), otherBones, PlateVertices(plateAsset), DistalMargin, RestGap);
            var frame = new GameObject("Plate Frame").transform;
            var (seatPosition, seatRotation) = PlateSeat.From(fit);
            frame.SetPositionAndRotation(seatPosition, seatRotation);

            var plate = (GameObject)PrefabUtility.InstantiatePrefab(plateAsset, scene);
            plate.name = "Distal Fibula Plate";
            plate.transform.SetParent(frame, false);
            Assign(plate, plateMaterial);
            var adjuster = plate.AddComponent<PlateAdjuster>();
            var targets = PlateAdjuster.DefaultAxes().Select(a => a.target).ToArray();
            var (startPosition, startRotation) = PlatePose.StartFor(Vector3.zero, Quaternion.identity, targets);
            plate.transform.SetLocalPositionAndRotation(startPosition, startRotation);

            var ghost = (GameObject)PrefabUtility.InstantiatePrefab(plateAsset, scene);
            ghost.name = "Target Ghost";
            ghost.transform.SetParent(frame, false);
            ghost.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            Assign(ghost, ghostMaterial);
            foreach (var r in ghost.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var ghostLook = ghost.AddComponent<PlateTargetGhost>();
            Wire(ghostLook, "adjuster", adjuster);

            var controls = new GameObject("Demo Controls");
            var status = controls.AddComponent<StatusLine>();
            WireString(status, "hint", Hint);
            var feedback = controls.AddComponent<PlateFeedback>();
            Wire(feedback, "adjuster", adjuster);
            Wire(feedback, "status", status);
            var banner = controls.AddComponent<Banner>();
            var bannerWiring = new SerializedObject(banner);
            // Low and right of centre: clear of the plate, the status box and the slider panel.
            bannerWiring.FindProperty("horizontalCentre").floatValue = BannerCentre;
            bannerWiring.FindProperty("topEdge").floatValue = BannerTop;
            bannerWiring.ApplyModifiedPropertiesWithoutUndo();
            Wire(feedback, "banner", banner);
            var resetKey = controls.AddComponent<KeyAction>();
            var keyWiring = new SerializedObject(resetKey);
            keyWiring.FindProperty("key").intValue = (int)Key.R;
            keyWiring.ApplyModifiedPropertiesWithoutUndo();
            UnityEventTools.AddPersistentListener(resetKey.OnPressed, adjuster.ResetAll);
            EditorUtility.SetDirty(resetKey);

            AxisPanelBuilder.Build(adjuster, PanelTitle, PanelHelp);

            PlaceCamera(fit, seatPosition, Front(meshes, fit.axis));

            EditorSceneManager.SaveScene(scene, PlatePaths.ScenePath);
            AssetDatabase.SaveAssets();
        }

        // From the lateral side, a little in front and above, so the plate and the outline both show.
        static void PlaceCamera(FibulaFrame.Fit fit, Vector3 seat, Vector3 front)
        {
            var camera = Object.FindAnyObjectByType<Camera>();
            var direction = (fit.lateral * 0.85f + front * 0.35f + Vector3.up * 0.3f).normalized;
            camera.transform.position = seat + direction * CameraDistance;
            camera.transform.LookAt(seat, Vector3.up);
            camera.GetComponent<OrbitCamera>().StartDistance = CameraDistance;
        }

        // Towards the toes: the foot bones near the floor sit in front of the leg.
        static Vector3 Front(IEnumerable<MeshFilter> meshes, Vector3 axis)
        {
            var points = meshes.SelectMany(WorldVertices).ToList();
            var low = points.Where(p => p.y < 0.03f).ToList();
            if (low.Count == 0) return Vector3.forward;
            var direction = Vector3.ProjectOnPlane(FibulaFrame.Mean(low) - FibulaFrame.Mean(points), axis);
            return direction.sqrMagnitude > 1e-10f ? direction.normalized : Vector3.forward;
        }

        static IEnumerable<Vector3> WorldVertices(MeshFilter filter)
        {
            var matrix = filter.transform.localToWorldMatrix;
            return filter.sharedMesh.vertices.Select(v => matrix.MultiplyPoint3x4(v));
        }

        // The plate model's vertices in the space of its root, the transform the adjuster moves.
        static List<Vector3> PlateVertices(GameObject plateAsset)
        {
            var root = plateAsset.transform.worldToLocalMatrix;
            var points = new List<Vector3>();
            foreach (var filter in plateAsset.GetComponentsInChildren<MeshFilter>())
            {
                var matrix = root * filter.transform.localToWorldMatrix;
                points.AddRange(filter.sharedMesh.vertices.Select(v => matrix.MultiplyPoint3x4(v)));
            }
            return points;
        }

        static void Assign(GameObject go, Material material)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>()) r.sharedMaterial = material;
        }

        static void Wire(Object component, string field, Object value)
        {
            var wiring = new SerializedObject(component);
            wiring.FindProperty(field).objectReferenceValue = value;
            wiring.ApplyModifiedPropertiesWithoutUndo();
        }

        static void WireString(Object component, string field, string value)
        {
            var wiring = new SerializedObject(component);
            wiring.FindProperty(field).stringValue = value;
            wiring.ApplyModifiedPropertiesWithoutUndo();
        }

        static Material LitMaterial(string name, Color colour, float metallic, float smoothness)
        {
            var material = Material(name, Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", colour);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material Material(string name, Shader shader)
        {
            var path = $"{PlatePaths.MaterialsFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            return material;
        }

        static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
