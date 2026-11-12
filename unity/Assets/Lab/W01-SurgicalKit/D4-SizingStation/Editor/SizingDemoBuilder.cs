using Lab.Core;
using Lab.Core.EditorTools;
using Lab.W01.VariableScrew;
using Lab.W01.VariableScrew.EditorTools;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Lab.W01.SizingStation.EditorTools
{
    /// <summary>
    /// Builds the sizing station demo from the lab's demo template: a holder with a millimetre
    /// scale, one screw to size, a rack for kept screws, the size panel and the memory readout.
    /// </summary>
    public static class SizingDemoBuilder
    {
        public const float DemoDepthMm = 19.6f;

        // Metres.
        const float BaseHeight = 0.006f;
        const float BaseMargin = 0.012f;
        const float BaseDepth = 0.04f;
        const float CradleX = -0.03f;
        const float ScaleZ = -0.0105f;
        const float NumberZ = -0.0166f;
        // One canvas unit is a tenth of a millimetre, so the numbers are sized like print on the base.
        const float NumberCanvasScale = 0.0001f;
        const int NumberFontSize = 30;
        const float RackGap = 0.006f;
        const float RackHeight = 0.002f;
        const float RackClearance = 0.0002f;
        const int RackPlaces = 8;
        const float RackPlaceMetres = 0.0125f;
        static readonly Color TenColour = new Color(1f, 0.78f, 0.4f);
        static readonly Vector3 CameraTarget = new Vector3(0.006f, 0f, 0.012f);
        const float CameraDistance = 0.25f;
        const float CameraPitchDeg = 46f;

        const string Hint =
            "Pick a diameter and drag the length. Enter keeps the screw and starts another. M compares the memory.\n" +
            "Arrow keys step the size. C switches the catalog. R starts again. Right-drag orbits, scroll zooms.";

        [MenuItem("Lab/W01/Build Sizing Station Demo")]
        public static void Build()
        {
            if (OverwriteGuard.Allow(SizingPaths.ScenePath)) Rebuild();
        }

        /// <summary>Builds without asking, for scripted runs where no one can answer a dialog.</summary>
        public static void Rebuild()
        {
            // Open the template first: opening a scene unloads assets loaded before it.
            var scene = EditorSceneManager.OpenScene(DemoTemplateBuilder.ScenePath, OpenSceneMode.Single);

            var catalogA = AssetDatabase.LoadAssetAtPath<ImplantCatalog>(SizingPaths.CatalogAPath);
            var catalogB = AssetDatabase.LoadAssetAtPath<ImplantCatalog>(SizingPaths.CatalogBPath);
            var screwPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScrewPaths.PrefabPath);
            if (catalogA == null || catalogB == null || screwPrefab == null)
            {
                Debug.LogError("The variable screw's catalogs or screw prefab are missing, so the demo can't be built.");
                return;
            }

            EnsureFolder(SizingPaths.MaterialsFolder);
            var baseMaterial = MakeMaterial("StationBase", "Universal Render Pipeline/Lit", new Color(0.13f, 0.15f, 0.18f), 0.55f, 0.45f);
            var markMaterial = MakeMaterial("ScaleMark", "Universal Render Pipeline/Unlit", new Color(0.86f, 0.9f, 0.93f), 0f, 0f);
            var tenMaterial = MakeMaterial("ScaleMarkTen", "Universal Render Pipeline/Unlit", TenColour, 0f, 0f);
            var rackMaterial = MakeMaterial("Rack", "Universal Render Pipeline/Lit", new Color(0.3f, 0.33f, 0.37f), 0.2f, 0.3f);

            var station = new GameObject("Sizing Station");
            var scaleLengthMm = ScaleLengthMm(catalogA, catalogB);
            var scaleLength = scaleLengthMm * 0.001f;
            var baseWidth = scaleLength + BaseMargin * 2f;
            Box("Base", station.transform, new Vector3(CradleX + scaleLength * 0.5f, BaseHeight * 0.5f, 0f),
                new Vector3(baseWidth, BaseHeight, BaseDepth), baseMaterial);

            // The screw lies along +X with the top of its head at the cradle, so its tip reads its length on the scale.
            var along = Quaternion.LookRotation(Vector3.right, Vector3.up);
            var cradle = new GameObject("Cradle").transform;
            cradle.SetParent(station.transform, false);
            cradle.SetPositionAndRotation(new Vector3(CradleX, BaseHeight, 0f), along);

            var scale = new GameObject("Scale").transform;
            scale.SetParent(station.transform, false);
            for (var mm = 0; mm <= Mathf.RoundToInt(scaleLengthMm); mm += 2)
            {
                var ten = mm % 10 == 0;
                var length = ten ? 0.006f : 0.003f;
                // Every mark starts at the same edge, nearest the screw, and the tens reach further out.
                var centre = new Vector3(CradleX + mm * 0.001f, BaseHeight + 0.00015f, ScaleZ + 0.003f - length * 0.5f);
                Box($"Mark {mm} mm", scale, centre, new Vector3(ten ? 0.0005f : 0.0003f, 0.0003f, length), ten ? tenMaterial : markMaterial);
            }

            MakeNumbers(station.transform, scaleLengthMm);

            // Kept screws lie on a tray behind the holder, first place nearest it, so the row grows away from the camera.
            var rackDepth = RackPlaces * RackPlaceMetres;
            var rackFront = BaseDepth * 0.5f + RackGap;
            Box("Rack Tray", station.transform, new Vector3(CradleX + scaleLength * 0.5f, RackHeight * 0.5f, rackFront + rackDepth * 0.5f),
                new Vector3(baseWidth, RackHeight, rackDepth), rackMaterial);
            var rack = new GameObject("Rack").transform;
            rack.SetParent(station.transform, false);
            rack.SetPositionAndRotation(new Vector3(CradleX, RackHeight + RackClearance, rackFront + RackPlaceMetres * 0.5f), along);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(screwPrefab, scene);
            instance.name = "Station Screw 1";
            instance.transform.SetParent(station.transform, true);
            var screw = instance.GetComponent<Screw>();

            var controls = new GameObject("Demo Controls");
            var sizer = controls.AddComponent<ScrewSizer>();
            var sizerWiring = new SerializedObject(sizer);
            SetArray(sizerWiring.FindProperty("catalogs"), catalogA, catalogB);
            sizerWiring.FindProperty("screw").objectReferenceValue = screw;
            sizerWiring.ApplyModifiedPropertiesWithoutUndo();

            var stationLogic = station.AddComponent<ScrewStation>();
            var stationWiring = new SerializedObject(stationLogic);
            stationWiring.FindProperty("sizer").objectReferenceValue = sizer;
            stationWiring.FindProperty("screwPrefab").objectReferenceValue = screwPrefab.GetComponent<Screw>();
            stationWiring.FindProperty("current").objectReferenceValue = screw;
            stationWiring.FindProperty("cradle").objectReferenceValue = cradle;
            stationWiring.FindProperty("rack").objectReferenceValue = rack;
            stationWiring.FindProperty("rackColumns").intValue = RackPlaces;
            // The rack's right points at the camera, so a minus sign runs the places away from it.
            stationWiring.FindProperty("rackColumnMetres").floatValue = -RackPlaceMetres;
            stationWiring.FindProperty("rackRowMetres").floatValue = baseWidth;
            stationWiring.ApplyModifiedPropertiesWithoutUndo();
            // Size and place the screw now, so the saved scene already shows it in the holder.
            sizer.Load(new[] { catalogA, catalogB }, screw);
            stationLogic.Load(sizer, screwPrefab.GetComponent<Screw>(), screw, cradle, rack);
            EditorUtility.SetDirty(screw);

            var panel = controls.AddComponent<SizePanel>();
            var panelWiring = new SerializedObject(panel);
            panelWiring.FindProperty("sizer").objectReferenceValue = sizer;
            panelWiring.FindProperty("depthMm").floatValue = DemoDepthMm;
            panelWiring.ApplyModifiedPropertiesWithoutUndo();

            var readout = controls.AddComponent<MemoryReadout>();
            var readoutWiring = new SerializedObject(readout);
            readoutWiring.FindProperty("sizer").objectReferenceValue = sizer;
            readoutWiring.ApplyModifiedPropertiesWithoutUndo();

            var status = controls.AddComponent<StatusLine>();
            var statusWiring = new SerializedObject(status);
            statusWiring.FindProperty("hint").stringValue = Hint;
            statusWiring.ApplyModifiedPropertiesWithoutUndo();

            AddKey(controls, Key.Enter, stationLogic.Keep);
            AddKey(controls, Key.R, stationLogic.Restart);
            AddKey(controls, Key.C, sizer.NextCatalog);
            AddKey(controls, Key.M, readout.ToggleComparison);
            AddKey(controls, Key.RightArrow, sizer.StepLength, 1);
            AddKey(controls, Key.LeftArrow, sizer.StepLength, -1);
            AddKey(controls, Key.UpArrow, sizer.StepDiameter, 1);
            AddKey(controls, Key.DownArrow, sizer.StepDiameter, -1);

            // The project uses the new Input System only, so the UI needs its input module.
            if (Object.FindAnyObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var camera = Object.FindAnyObjectByType<Camera>();
            var direction = Quaternion.Euler(CameraPitchDeg, 0f, 0f) * Vector3.back;
            camera.transform.position = CameraTarget + direction * CameraDistance;
            camera.transform.LookAt(CameraTarget);
            camera.GetComponent<OrbitCamera>().StartDistance = CameraDistance;

            EditorSceneManager.SaveScene(scene, SizingPaths.ScenePath);
            AssetDatabase.SaveAssets();
        }

        /// <summary>The scale runs to the longest screw any of the catalogs ships, rounded up to a ten.</summary>
        public static float ScaleLengthMm(params ImplantCatalog[] catalogs)
        {
            var longest = 0f;
            foreach (var catalog in catalogs)
            {
                if (catalog == null || catalog.screws == null) continue;
                foreach (var spec in catalog.screws)
                {
                    if (spec == null || spec.availableLengthsMm == null) continue;
                    foreach (var length in spec.availableLengthsMm) longest = Mathf.Max(longest, length);
                }
            }
            return Mathf.Ceil(longest / 10f) * 10f;
        }

        // The numbers lie flat on the base like print. A world-space canvas is depth-tested, so a screw
        // in front of a number covers it, which 3D text would not allow.
        static void MakeNumbers(Transform station, float scaleLengthMm)
        {
            var canvasObject = new GameObject("Scale Numbers", typeof(Canvas));
            canvasObject.transform.SetParent(station, false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var canvasRect = (RectTransform)canvasObject.transform;
            canvasRect.sizeDelta = Vector2.zero;
            canvasRect.SetPositionAndRotation(new Vector3(0f, BaseHeight + 0.0003f, 0f), Quaternion.Euler(90f, 0f, 0f));
            canvasRect.localScale = Vector3.one * NumberCanvasScale;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            for (var mm = 0; mm <= Mathf.RoundToInt(scaleLengthMm); mm += 10)
            {
                var text = new GameObject($"Number {mm}", typeof(RectTransform)).AddComponent<Text>();
                text.transform.SetParent(canvasRect, false);
                text.rectTransform.sizeDelta = new Vector2(100f, 40f);
                // The canvas lies flat at the origin, so its x is the world's x and its y is the world's z.
                text.rectTransform.anchoredPosition3D = new Vector3((CradleX + mm * 0.001f) / NumberCanvasScale, NumberZ / NumberCanvasScale, 0f);
                text.font = font;
                text.fontSize = NumberFontSize;
                text.fontStyle = FontStyle.Bold;
                text.alignment = TextAnchor.MiddleCenter;
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                text.verticalOverflow = VerticalWrapMode.Overflow;
                text.color = TenColour;
                text.raycastTarget = false;
                text.text = mm.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        static KeyAction NewKey(GameObject on, Key key)
        {
            var action = on.AddComponent<KeyAction>();
            var wiring = new SerializedObject(action);
            wiring.FindProperty("key").intValue = (int)key;
            wiring.ApplyModifiedPropertiesWithoutUndo();
            return action;
        }

        static void AddKey(GameObject on, Key key, UnityAction call)
        {
            var action = NewKey(on, key);
            UnityEventTools.AddPersistentListener(action.OnPressed, call);
            EditorUtility.SetDirty(action);
        }

        static void AddKey(GameObject on, Key key, UnityAction<int> call, int argument)
        {
            var action = NewKey(on, key);
            UnityEventTools.AddIntPersistentListener(action.OnPressed, call, argument);
            EditorUtility.SetDirty(action);
        }

        // Decor has no collider, so nothing here ever takes a click.
        static void Box(string name, Transform parent, Vector3 centre, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = centre;
            go.transform.localScale = size;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            // The scale marks are paint, not parts, so they cast no shadow.
            renderer.shadowCastingMode = size.y < 0.001f ? ShadowCastingMode.Off : ShadowCastingMode.On;
        }

        static Material MakeMaterial(string name, string shader, Color colour, float metallic, float smoothness)
        {
            var path = $"{SizingPaths.MaterialsFolder}/{name}.mat";
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
