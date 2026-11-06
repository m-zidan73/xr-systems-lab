using Lab.Core;
using Lab.Core.EditorTools;
using Lab.W01.VariableScrew;
using Lab.W01.VariableScrew.EditorTools;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Lab.W01.ColourCodedHoles.EditorTools
{
    /// <summary>
    /// Builds the colour-coded holes demo from the lab's demo template: a bone block with a plate
    /// and four holes, a tray of screws from the first catalog, and the mouse and keyboard controls.
    /// </summary>
    public static class HolesDemoBuilder
    {
        static readonly float[] HoleDiametersMm = { 2.7f, 3.5f, 4.0f, 5.0f };

        // Metres. The bone block is 70 × 50 × 30 mm, deep enough for the longest screw.
        static readonly Vector3 BlockSize = new Vector3(0.07f, 0.05f, 0.03f);
        static readonly Vector3 PlateSize = new Vector3(0.064f, 0.002f, 0.014f);
        const float HoleSpacing = 0.015f;
        const float RingWidth = 0.0015f;
        // ScrewSpec.headEndMm: the head rests on the plate and the shaft goes down into the bone.
        const float HeadHeight = 0.0025f;
        const float TraySpacing = 0.007f;
        const float TrayZ = -0.075f;
        const float TrayLengthMm = 20f;
        static readonly Vector3 CameraTarget = new Vector3(0f, 0.03f, -0.035f);
        const float CameraDistance = 0.2f;
        const float CameraPitchDeg = 32f;

        const string Hint =
            "Drag a screw into a hole. Scroll while holding to turn it. Press C to switch the catalog.\n" +
            "Right-drag orbits, middle-drag or Shift + right-drag pans, scroll zooms.";

        [MenuItem("Lab/W01/Build Colour-Coded Holes Demo")]
        public static void Build()
        {
            if (!OverwriteGuard.Allow(HolePaths.ScenePath)) return;

            // Open the template first: opening a scene unloads assets loaded before it, and a
            // SerializedProperty then stores those dead references as empty.
            var scene = EditorSceneManager.OpenScene(DemoTemplateBuilder.ScenePath, OpenSceneMode.Single);

            var catalogA = AssetDatabase.LoadAssetAtPath<ImplantCatalog>(HolePaths.CatalogAPath);
            var catalogB = AssetDatabase.LoadAssetAtPath<ImplantCatalog>(HolePaths.CatalogBPath);
            var screwPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScrewPaths.PrefabPath);
            if (catalogA == null || catalogB == null || screwPrefab == null)
            {
                Debug.LogError("The variable screw's catalogs or screw prefab are missing, so the demo can't be built.");
                return;
            }

            EnsureFolder(HolePaths.MaterialsFolder);
            var bone = MakeMaterial("Bone", new Color(0.86f, 0.82f, 0.72f), 0f, 0.25f);
            var plate = MakeMaterial("Plate", new Color(0.62f, 0.64f, 0.68f), 0.6f, 0.55f);
            var ring = MakeMaterial("HoleRing", Color.white, 0.3f, 0.5f);
            var opening = MakeMaterial("HoleOpening", new Color(0.05f, 0.05f, 0.06f), 0f, 0.2f);

            Box("Bone Block", new Vector3(0f, BlockSize.y * 0.5f, 0f), BlockSize, bone);
            Box("Plate", new Vector3(0f, BlockSize.y + PlateSize.y * 0.5f, 0f), PlateSize, plate);
            var plateTop = BlockSize.y + PlateSize.y;

            var holes = new Hole[HoleDiametersMm.Length];
            for (var i = 0; i < holes.Length; i++)
            {
                var x = (i - (holes.Length - 1) * 0.5f) * HoleSpacing;
                holes[i] = MakeHole(HoleDiametersMm[i], new Vector3(x, plateTop, 0f), ring, opening, catalogA);
            }

            var tray = new GameObject("Tray").transform;
            var trayScrews = new Screw[catalogA.screws.Length];
            for (var i = 0; i < trayScrews.Length; i++)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(screwPrefab, scene);
                instance.name = $"Tray Screw {i + 1}";
                instance.transform.SetParent(tray, true);
                var x = (i - (trayScrews.Length - 1) * 0.5f) * TraySpacing;
                instance.transform.SetPositionAndRotation(new Vector3(x, 0.004f, TrayZ), Quaternion.identity);
                trayScrews[i] = instance.GetComponent<Screw>();
                trayScrews[i].Set(catalogA.screws[i], TrayLengthMm);
                instance.AddComponent<Grabbable>();
            }

            var camera = Object.FindFirstObjectByType<Camera>();
            var direction = Quaternion.Euler(CameraPitchDeg, 0f, 0f) * Vector3.back;
            camera.transform.position = CameraTarget + direction * CameraDistance;
            camera.transform.LookAt(CameraTarget);
            camera.GetComponent<OrbitCamera>().StartDistance = CameraDistance;
            camera.gameObject.AddComponent<MouseInteractor>();

            var controls = new GameObject("Demo Controls");
            var switcher = controls.AddComponent<CatalogSwitcher>();
            var switcherWiring = new SerializedObject(switcher);
            SetArray(switcherWiring.FindProperty("catalogs"), catalogA, catalogB);
            SetArray(switcherWiring.FindProperty("holes"), holes);
            SetArray(switcherWiring.FindProperty("trayScrews"), trayScrews);
            switcherWiring.FindProperty("trayLengthMm").floatValue = TrayLengthMm;
            switcherWiring.ApplyModifiedPropertiesWithoutUndo();

            var status = controls.AddComponent<StatusLine>();
            var statusWiring = new SerializedObject(status);
            statusWiring.FindProperty("hint").stringValue = Hint;
            statusWiring.ApplyModifiedPropertiesWithoutUndo();

            var feedback = controls.AddComponent<HoleFeedback>();
            var feedbackWiring = new SerializedObject(feedback);
            SetArray(feedbackWiring.FindProperty("holes"), holes);
            feedbackWiring.FindProperty("switcher").objectReferenceValue = switcher;
            feedbackWiring.FindProperty("status").objectReferenceValue = status;
            feedbackWiring.ApplyModifiedPropertiesWithoutUndo();

            var keyAction = controls.AddComponent<KeyAction>();
            var keyWiring = new SerializedObject(keyAction);
            keyWiring.FindProperty("key").intValue = (int)Key.C;
            keyWiring.ApplyModifiedPropertiesWithoutUndo();
            UnityEventTools.AddPersistentListener(keyAction.OnPressed, switcher.Next);
            EditorUtility.SetDirty(keyAction);

            EditorSceneManager.SaveScene(scene, HolePaths.ScenePath);
            AssetDatabase.SaveAssets();
        }

        static Hole MakeHole(float diameterMm, Vector3 position, Material ringMaterial, Material openingMaterial, ImplantCatalog catalog)
        {
            var root = new GameObject($"Hole {diameterMm:0.0} mm");
            root.transform.position = position;
            var diameter = diameterMm * 0.001f;
            var ring = Cylinder("Ring", root.transform, diameter + RingWidth * 2f, 0.0004f, ringMaterial);
            Cylinder("Opening", root.transform, diameter, 0.0006f, openingMaterial);

            var seat = new GameObject("Seat").transform;
            seat.SetParent(root.transform, false);
            seat.localPosition = new Vector3(0f, HeadHeight, 0f);
            seat.localRotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);

            var hole = root.AddComponent<Hole>();
            var wiring = new SerializedObject(hole);
            wiring.FindProperty("diameterMm").floatValue = diameterMm;
            wiring.FindProperty("catalog").objectReferenceValue = catalog;
            wiring.FindProperty("seat").objectReferenceValue = seat;
            wiring.FindProperty("ring").objectReferenceValue = ring.GetComponent<Renderer>();
            wiring.ApplyModifiedPropertiesWithoutUndo();
            hole.SetCatalog(catalog);
            root.AddComponent<HoleDropTarget>();
            return hole;
        }

        // Unity's cylinder is 1 m across and 2 m tall. Decor has no collider, so only parts take clicks.
        static GameObject Cylinder(string name, Transform parent, float diameter, float height, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
            go.transform.localScale = new Vector3(diameter, height * 0.5f, diameter);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        static void Box(string name, Vector3 centre, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = centre;
            go.transform.localScale = size;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = material;
        }

        static Material MakeMaterial(string name, Color colour, float metallic, float smoothness)
        {
            var path = $"{HolePaths.MaterialsFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", colour);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        static void SetArray(SerializedProperty property, params Object[] values)
        {
            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
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
