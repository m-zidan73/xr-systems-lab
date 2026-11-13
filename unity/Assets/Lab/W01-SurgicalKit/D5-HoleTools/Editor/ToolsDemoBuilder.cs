using System.Linq;
using Lab.Core;
using Lab.W01.AdjustablePlate;
using Lab.W01.AdjustablePlate.EditorTools;
using Lab.W01.ColourCodedHoles;
using Lab.W01.StepGuidance.EditorTools;
using Lab.W01.VariableScrew;
using Lab.W01.VariableScrew.EditorTools;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace Lab.W01.HoleTools.EditorTools
{
    /// <summary>
    /// Builds the hole tools demo on a copy of the adjustable plate scene: the plate starts in place on
    /// the fibula, four of its openings become labelled holes, and a drill and a depth gauge hang on
    /// a board beside it. The plate's own scene is left as it is.
    /// </summary>
    public static class ToolsDemoBuilder
    {
        // Metres.
        public const float Reach = 0.08f;
        const float RingMargin = 0.0009f;
        const float RingLift = 0.0003f;
        const float BoreLength = 0.012f;
        const int LabelFontSize = 64;
        const float BoardDistance = 0.19f;
        const float BoardRight = 0.062f;
        const float BoardDown = 0.012f;
        static readonly Vector3 BoardSize = new Vector3(0.04f, 0.066f, 0.003f);
        const float ToolSpacing = 0.018f;
        const float NameHeight = 0.003f;
        const float WorkSeconds = 0.7f;

        // The plate starts in place here, so each range is centred on where it belongs.
        static readonly (string label, float min, float max)[] Ranges =
        {
            ("Across (X)", -5f, 5f),
            ("Along (Y)", -8f, 8f),
            ("Lift (Z)", 0f, 5f),
            ("Tilt (X)", -10f, 10f),
            ("Roll (Y)", -10f, 10f),
            ("Swivel (Z)", -10f, 10f),
        };

        const string Hint =
            "Drag the drill onto a hole, then the depth gauge. Each hole reads its own depth.\n" +
            "Move the plate with the sliders and measure again. R starts again. Right-drag orbits, scroll zooms.";

        [MenuItem("Lab/W01/Build Hole Tools Demo")]
        public static void Build()
        {
            if (OverwriteGuard.Allow(ToolPaths.ScenePath)) Rebuild();
        }

        /// <summary>Builds without asking, for scripted runs where no one can answer a dialog.</summary>
        public static void Rebuild()
        {
            var scene = EditorSceneManager.OpenScene(PlatePaths.ScenePath, OpenSceneMode.Single);
            // Saved under its new name at once, so nothing below can touch the plate's own scene.
            EditorSceneManager.SaveScene(scene, ToolPaths.ScenePath);

            var catalog = AssetDatabase.LoadAssetAtPath<ImplantCatalog>(ToolPaths.CatalogPath);
            var ringMesh = AssetDatabase.LoadAssetAtPath<Mesh>(StepPaths.RingMeshPath);
            var adjuster = Object.FindAnyObjectByType<PlateAdjuster>();
            var fibula = Object.FindObjectsByType<MeshFilter>().FirstOrDefault(m => m.name == PlatePaths.FibulaName);
            var controls = Object.FindAnyObjectByType<StatusLine>();
            if (catalog == null || ringMesh == null || adjuster == null || fibula == null || controls == null)
            {
                Debug.LogError("The catalog, the ring mesh, or the plate scene's plate, fibula or controls are missing, so the demo can't be built.");
                return;
            }

            EnsureFolder(ToolPaths.MaterialsFolder);
            var ringMaterial = MakeMaterial("HoleRing", "Universal Render Pipeline/Unlit", Color.white, 0f, 0f);
            var boreMaterial = MakeMaterial("Bore", "Universal Render Pipeline/Unlit", new Color(0.04f, 0.04f, 0.05f), 0f, 0f);
            var steel = MakeMaterial("ToolSteel", "Universal Render Pipeline/Lit", new Color(0.78f, 0.8f, 0.83f), 0.85f, 0.6f);
            var drillGrip = MakeMaterial("DrillGrip", "Universal Render Pipeline/Lit", new Color(0.9f, 0.47f, 0.16f), 0.1f, 0.45f);
            var gaugeGrip = MakeMaterial("GaugeGrip", "Universal Render Pipeline/Lit", new Color(0.2f, 0.52f, 0.78f), 0.1f, 0.45f);
            var boardMaterial = MakeMaterial("ToolBoard", "Universal Render Pipeline/Lit", new Color(0.13f, 0.15f, 0.18f), 0.2f, 0.3f);

            // The plate's own scene is about placing it. Here it starts in place, so its outline, banner and
            // placement messages go, and the sliders stay for moving it under the gauge.
            var plate = adjuster.gameObject;
            foreach (var ghost in Object.FindObjectsByType<PlateTargetGhost>()) Object.DestroyImmediate(ghost.gameObject);
            foreach (var feedback in Object.FindObjectsByType<PlateFeedback>()) Object.DestroyImmediate(feedback);
            foreach (var banner in Object.FindObjectsByType<Banner>()) Object.DestroyImmediate(banner);
            plate.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            var axes = new SerializedObject(adjuster).FindProperty("axes");
            var adjusterWiring = axes.serializedObject;
            for (var i = 0; i < Ranges.Length; i++)
            {
                var axis = axes.GetArrayElementAtIndex(i);
                axis.FindPropertyRelative("label").stringValue = Ranges[i].label;
                axis.FindPropertyRelative("min").floatValue = Ranges[i].min;
                axis.FindPropertyRelative("max").floatValue = Ranges[i].max;
                axis.FindPropertyRelative("target").floatValue = 0f;
            }
            adjusterWiring.ApplyModifiedPropertiesWithoutUndo();

            var bone = fibula.gameObject.GetComponent<MeshCollider>();
            if (bone == null) bone = fibula.gameObject.AddComponent<MeshCollider>();
            bone.sharedMesh = fibula.sharedMesh;

            var plateVertices = plate.GetComponentInChildren<MeshFilter>().sharedMesh.vertices;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var targets = new HoleSiteTarget[PlateHoles.Used.Length];
            for (var i = 0; i < targets.Length; i++)
                targets[i] = MakeHole(PlateHoles.Used[i], i + 1, plate.transform, plateVertices, catalog, bone, ringMesh, ringMaterial, boreMaterial);

            var camera = Object.FindAnyObjectByType<Camera>();
            if (camera.GetComponent<MouseInteractor>() == null) camera.gameObject.AddComponent<MouseInteractor>();
            MakeBoard(camera.transform, boardMaterial, steel, drillGrip, gaugeGrip, font);

            var statusWiring = new SerializedObject(controls);
            statusWiring.FindProperty("hint").stringValue = Hint;
            statusWiring.ApplyModifiedPropertiesWithoutUndo();

            var labels = controls.gameObject.AddComponent<HoleLabels>();
            var labelsWiring = new SerializedObject(labels);
            var labelled = labelsWiring.FindProperty("sites");
            labelled.arraySize = targets.Length;
            for (var i = 0; i < targets.Length; i++) labelled.GetArrayElementAtIndex(i).objectReferenceValue = targets[i].Site;
            labelsWiring.ApplyModifiedPropertiesWithoutUndo();

            var sites = controls.gameObject.AddComponent<HoleSites>();
            var sitesWiring = new SerializedObject(sites);
            var list = sitesWiring.FindProperty("targets");
            list.arraySize = targets.Length;
            for (var i = 0; i < targets.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = targets[i];
            sitesWiring.FindProperty("status").objectReferenceValue = controls;
            sitesWiring.ApplyModifiedPropertiesWithoutUndo();

            // R already puts the plate back. It now clears the holes as well.
            var restart = Object.FindObjectsByType<KeyAction>().First(k => k.Key == Key.R);
            UnityEventTools.AddPersistentListener(restart.OnPressed, sites.ClearAll);
            EditorUtility.SetDirty(restart);

            EditorSceneManager.SaveScene(scene, ToolPaths.ScenePath);
            AssetDatabase.SaveAssets();
        }

        static HoleSiteTarget MakeHole(PlateHoles.Opening opening, int number, Transform plate, Vector3[] plateVertices,
            ImplantCatalog catalog, Collider bone, Mesh ringMesh, Material ringMaterial, Material boreMaterial)
        {
            var x = opening.XMm * 0.001f;
            var y = opening.YMm * 0.001f;
            var radius = opening.DiameterMm * 0.0005f;

            // The hole sits on the plate's outer face and looks into the bone, which is the plate's -Z.
            var go = new GameObject($"Hole {number} ({opening.Name})");
            go.transform.SetParent(plate, false);
            go.transform.localPosition = new Vector3(x, y, PlateHoles.RimHeight(plateVertices, x, y));
            go.transform.localRotation = Quaternion.LookRotation(Vector3.back, Vector3.up);

            var ring = new GameObject("Ring", typeof(MeshFilter), typeof(MeshRenderer));
            ring.transform.SetParent(go.transform, false);
            // The ring mesh faces its own +Y. Turn that to face out of the plate, away from the bone.
            ring.transform.localRotation = Quaternion.FromToRotation(Vector3.up, Vector3.back);
            ring.transform.localPosition = Vector3.back * RingLift;
            ring.transform.localScale = new Vector3(radius + RingMargin, 1f, radius + RingMargin);
            ring.GetComponent<MeshFilter>().sharedMesh = ringMesh;
            var ringRenderer = ring.GetComponent<MeshRenderer>();
            ringRenderer.sharedMaterial = ringMaterial;
            ringRenderer.shadowCastingMode = ShadowCastingMode.Off;

            var bore = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bore.name = "Bore";
            Object.DestroyImmediate(bore.GetComponent<Collider>());
            bore.transform.SetParent(go.transform, false);
            // A cylinder is 2 units tall along its Y. Lay it along the hole, starting at the plate's face.
            bore.transform.localRotation = Quaternion.FromToRotation(Vector3.up, Vector3.forward);
            bore.transform.localPosition = Vector3.forward * (BoreLength * 0.5f);
            bore.transform.localScale = new Vector3(radius * 1.4f, BoreLength * 0.5f, radius * 1.4f);
            var boreRenderer = bore.GetComponent<MeshRenderer>();
            boreRenderer.sharedMaterial = boreMaterial;
            boreRenderer.shadowCastingMode = ShadowCastingMode.Off;

            var hole = go.AddComponent<Hole>();
            var holeWiring = new SerializedObject(hole);
            holeWiring.FindProperty("diameterMm").floatValue = opening.DiameterMm;
            holeWiring.FindProperty("ring").objectReferenceValue = ringRenderer;
            holeWiring.ApplyModifiedPropertiesWithoutUndo();
            hole.SetCatalog(catalog);

            var site = go.AddComponent<HoleSite>();
            var siteWiring = new SerializedObject(site);
            siteWiring.FindProperty("bone").objectReferenceValue = bone;
            siteWiring.FindProperty("reachMetres").floatValue = Reach;
            siteWiring.FindProperty("bore").objectReferenceValue = bore;
            siteWiring.ApplyModifiedPropertiesWithoutUndo();
            bore.SetActive(false);

            return go.AddComponent<HoleSiteTarget>();
        }

        // A board that faces the camera's starting view, with the tools hanging on it tip down.
        static void MakeBoard(Transform camera, Material boardMaterial, Material steel, Material drillGrip, Material gaugeGrip, Font font)
        {
            var board = new GameObject("Tool Board").transform;
            board.SetPositionAndRotation(
                camera.position + camera.forward * BoardDistance + camera.right * BoardRight - camera.up * BoardDown,
                camera.rotation);
            Part("Board", PrimitiveType.Cube, board, Vector3.forward * (BoardSize.z * 0.5f + 0.008f), BoardSize, Quaternion.identity, boardMaterial);

            var hang = Quaternion.LookRotation(Vector3.down, Vector3.back);
            MakeDrill(board, new Vector3(-ToolSpacing * 0.5f, -0.004f, 0f), hang, steel, drillGrip);
            MakeGauge(board, new Vector3(ToolSpacing * 0.5f, -0.004f, 0f), hang, steel, gaugeGrip);

            var drillName = MakeText("Drill Name", board, font, NameHeight, "Drill");
            drillName.transform.localPosition = new Vector3(-ToolSpacing * 0.5f, BoardSize.y * 0.5f - 0.005f, 0.006f);
            var gaugeName = MakeText("Gauge Name", board, font, NameHeight, "Gauge");
            gaugeName.transform.localPosition = new Vector3(ToolSpacing * 0.5f, BoardSize.y * 0.5f - 0.005f, 0.006f);
        }

        // Every tool has its origin where it meets the plate: the working end runs along +Z into the hole
        // and the handle runs back along -Z.
        static void MakeDrill(Transform board, Vector3 localPosition, Quaternion localRotation, Material steel, Material grip)
        {
            var tool = MakeTool("Drill", ToolKind.Drill, board, localPosition, localRotation, 0.005f, 0.036f);
            Rod("Bit", tool, 0.0012f, -0.004f, 0.016f, steel);
            Rod("Chuck", tool, 0.0032f, -0.012f, -0.004f, steel);
            Rod("Body", tool, 0.0048f, -0.04f, -0.012f, grip);
        }

        static void MakeGauge(Transform board, Vector3 localPosition, Quaternion localRotation, Material steel, Material grip)
        {
            var tool = MakeTool("Depth Gauge", ToolKind.Gauge, board, localPosition, localRotation, 0.0045f, 0.036f);
            Rod("Probe", tool, 0.0006f, -0.004f, 0.022f, steel);
            Rod("Sleeve", tool, 0.0022f, -0.02f, 0f, steel);
            Rod("Handle", tool, 0.0042f, -0.04f, -0.02f, grip);
        }

        static Transform MakeTool(string name, ToolKind kind, Transform board, Vector3 localPosition, Quaternion localRotation, float gripRadius, float gripLength)
        {
            var go = new GameObject(name);
            go.transform.SetParent(board, false);
            go.transform.SetLocalPositionAndRotation(localPosition, localRotation);
            var grip = go.AddComponent<CapsuleCollider>();
            grip.direction = 2;
            grip.radius = gripRadius;
            grip.height = gripLength + gripRadius * 2f;
            grip.center = new Vector3(0f, 0f, -0.004f - gripLength * 0.5f);
            go.AddComponent<Grabbable>();
            go.AddComponent<HoleTool>().Load(kind, WorkSeconds);
            return go.transform;
        }

        // A cylinder along the tool's Z, from one distance along it to another.
        static void Rod(string name, Transform tool, float radius, float fromZ, float toZ, Material material)
        {
            Part(name, PrimitiveType.Cylinder, tool, Vector3.forward * ((fromZ + toZ) * 0.5f),
                new Vector3(radius * 2f, (toZ - fromZ) * 0.5f, radius * 2f), Quaternion.FromToRotation(Vector3.up, Vector3.forward), material);
        }

        // Shapes carry no collider of their own: a tool is picked up by its one grip collider.
        static void Part(string name, PrimitiveType shape, Transform parent, Vector3 localPosition, Vector3 localScale, Quaternion localRotation, Material material)
        {
            var go = GameObject.CreatePrimitive(shape);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.SetLocalPositionAndRotation(localPosition, localRotation);
            go.transform.localScale = localScale;
            go.GetComponent<Renderer>().sharedMaterial = material;
        }

        static TextMesh MakeText(string name, Transform parent, Font font, float heightMetres, string content)
        {
            var go = new GameObject(name, typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMesh>();
            text.font = font;
            text.fontSize = LabelFontSize;
            // Unity draws a character about a tenth of its font size tall per unit of character size.
            text.characterSize = heightMetres / (LabelFontSize * 0.1f);
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.text = content;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = font.material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return text;
        }

        static Material MakeMaterial(string name, string shader, Color colour, float metallic, float smoothness)
        {
            var path = $"{ToolPaths.MaterialsFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find(shader));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", colour);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
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
