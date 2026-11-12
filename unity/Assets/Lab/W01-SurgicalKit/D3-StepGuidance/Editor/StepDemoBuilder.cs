using System.Collections.Generic;
using System.Linq;
using Lab.Core;
using Lab.W01.ColourCodedHoles;
using Lab.W01.ColourCodedHoles.EditorTools;
using Lab.W01.VariableScrew.EditorTools;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace Lab.W01.StepGuidance.EditorTools
{
    /// <summary>
    /// Builds the step guidance demo on a copy of day 1's colour-coded holes scene: the same plate, holes and
    /// tray, plus a five-step procedure, rings on the hole and screw to use, the step panel and the keys.
    /// Day 1's catalog key and hole messages come out, because the guide does that job now.
    /// </summary>
    public static class StepDemoBuilder
    {
        const float RingInnerFraction = 0.7f;
        const int RingSegments = 64;
        static readonly Color RingColour = new Color(1f, 0.86f, 0.3f, 0.95f);
        const float BannerCentre = 0.5f;
        const float BannerTop = 0.4f;

        const string Hint =
            "Follow the steps on the right. Drag a screw into a hole, and scroll while holding to turn it.\n" +
            "Space confirms a check step. R starts again. Right-drag orbits, middle-drag pans, scroll zooms.";
        const string AfterDone = "Press R to start again.";
        const string CheckColours =
            "Check the colour code: each hole's ring matches one screw colour in the tray. Press Space when you've matched all four.";

        // Not left to right, so the trainee follows the guide rather than the plate.
        static readonly float[] ScrewOrderMm = { 3.5f, 2.7f, 5f, 4f };

        [MenuItem("Lab/W01/Build Step Guidance Demo")]
        public static void Build()
        {
            if (OverwriteGuard.Allow(StepPaths.ScenePath)) Rebuild();
        }

        /// <summary>Builds without asking, for scripted runs where no one can answer a dialog.</summary>
        public static void Rebuild()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(HolePaths.ScenePath) == null)
            {
                Debug.LogError("Day 1's colour-coded holes scene is missing, so the step guidance demo can't be built.");
                return;
            }
            var scene = EditorSceneManager.OpenScene(HolePaths.ScenePath, OpenSceneMode.Single);
            // Save under this system's name straight away, so nothing below can change day 1's scene.
            if (!EditorSceneManager.SaveScene(scene, StepPaths.ScenePath))
            {
                Debug.LogError($"Couldn't save {StepPaths.ScenePath}.");
                return;
            }

            var ghostShader = Shader.Find(StepPaths.GhostShaderName);
            var controls = GameObject.Find("Demo Controls");
            var switcher = controls != null ? controls.GetComponent<CatalogSwitcher>() : null;
            if (ghostShader == null || switcher == null)
            {
                Debug.LogError("The Lab/Ghost shader or day 1's catalog switcher is missing, so the demo can't be built.");
                return;
            }

            var status = controls.GetComponent<StatusLine>();
            Object.DestroyImmediate(controls.GetComponent<HoleFeedback>());
            foreach (var key in controls.GetComponents<KeyAction>()) Object.DestroyImmediate(key);
            WireString(status, "hint", Hint);

            var holes = switcher.Holes.ToArray();
            var guideObject = new GameObject("Step Guide");
            var guide = guideObject.AddComponent<StepGuide>();
            guide.Load(Procedure(holes, switcher), holes);
            EditorUtility.SetDirty(guide);

            EnsureFolder(StepPaths.MeshesFolder);
            EnsureFolder(StepPaths.MaterialsFolder);
            var ring = RingAsset();
            var material = MarkerMaterial(ghostShader);
            var highlighter = guideObject.AddComponent<StepHighlighter>();
            Wire(highlighter, "guide", guide);
            Wire(highlighter, "holeMarker", Marker("Hole Marker", guideObject.transform, ring, material));
            Wire(highlighter, "screwMarker", Marker("Screw Marker", guideObject.transform, ring, material));
            highlighter.Connect(guide);

            var panel = guideObject.AddComponent<StepPanel>();
            Wire(panel, "guide", guide);

            var banner = controls.AddComponent<Banner>();
            var bannerWiring = new SerializedObject(banner);
            // Between the plate and the tray, clear of the status box and the step panel.
            bannerWiring.FindProperty("horizontalCentre").floatValue = BannerCentre;
            bannerWiring.FindProperty("topEdge").floatValue = BannerTop;
            bannerWiring.ApplyModifiedPropertiesWithoutUndo();

            var feedback = controls.AddComponent<StepFeedback>();
            Wire(feedback, "guide", guide);
            Wire(feedback, "status", status);
            Wire(feedback, "banner", banner);
            WireString(feedback, "afterDone", AfterDone);

            AddKey(controls, Key.Space, guide.Confirm);
            AddKey(controls, Key.R, guide.Restart);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        static List<TrainingStep> Procedure(Hole[] holes, CatalogSwitcher switcher)
        {
            var steps = new List<TrainingStep> { new TrainingStep { instruction = CheckColours, finishesOn = StepFinish.Confirmed } };
            foreach (var mm in ScrewOrderMm)
            {
                var step = new TrainingStep
                {
                    finishesOn = StepFinish.ScrewSeated,
                    hole = holes.Single(h => Mathf.Approximately(h.DiameterMm, mm)),
                    screw = switcher.TrayScrews.Single(s => s != null && s.Spec != null && Mathf.Approximately(s.Spec.diameterMm, mm)),
                };
                // Written into the step, so the Inspector shows the text a trainee reads.
                step.instruction = StepMessages.Instruction(step);
                steps.Add(step);
            }
            return steps;
        }

        static Renderer Marker(string name, Transform parent, Mesh mesh, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        static Mesh RingAsset()
        {
            var built = RingMesh.Build(RingInnerFraction, RingSegments);
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(StepPaths.RingMeshPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(built, StepPaths.RingMeshPath);
                return built;
            }
            EditorUtility.CopySerialized(built, existing);
            Object.DestroyImmediate(built);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        static Material MarkerMaterial(Shader shader)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(StepPaths.MarkerMaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, StepPaths.MarkerMaterialPath);
            }
            material.shader = shader;
            material.SetColor("_Color", RingColour);
            EditorUtility.SetDirty(material);
            return material;
        }

        static void AddKey(GameObject host, Key key, UnityAction action)
        {
            var keyAction = host.AddComponent<KeyAction>();
            var wiring = new SerializedObject(keyAction);
            wiring.FindProperty("key").intValue = (int)key;
            wiring.ApplyModifiedPropertiesWithoutUndo();
            UnityEventTools.AddPersistentListener(keyAction.OnPressed, action);
            EditorUtility.SetDirty(keyAction);
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
