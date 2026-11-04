using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Lab.W01.VariableScrew.EditorTools
{
    /// <summary>
    /// Builds two example implant catalogs in one click, so the retargeting demo can be
    /// set up without hand authoring twenty ScriptableObjects.
    ///
    /// Two catalogs exist on purpose. Swapping one for the other recolours the tray and
    /// changes the slider's steps with no code edit, which is the whole argument: colour
    /// coding and length increments are per manufacturer, so they are data, not code.
    ///
    /// Menu: Implants > Create Example Catalogs
    /// </summary>
    public static class CatalogBuilder
    {
        const string RootFolder = ScrewPaths.Root;
        const string DataFolder = ScrewPaths.DataFolder;

        const string Disclaimer =
            "Illustrative example data assembled for a prototype. Colours follow a " +
            "reference product photo; lengths and drill sizes are plausible but invented. " +
            "Not a real manufacturer's specification and not clinical guidance.";

        struct Entry
        {
            public float Diameter;
            public string Hex;
            public float From, To, Step;
            public float ThreadDrill, GlideDrill;
            public float Pitch;
            public string Name;
        }

        [MenuItem("Implants/Create Example Catalogs")]
        public static void CreateExampleCatalogs()
        {
            EnsureFolder(RootFolder);
            EnsureFolder(DataFolder);

            // Catalog A: colours taken from the instrument tray reference photo.
            // Trailing float before the name is thread pitch in millimetres per turn.
            var systemA = new[]
            {
                New(2.2f, "#FF7ACB",  6f, 20f, 2f, 1.8f, 2.2f, 0.60f, "2.2 mm Locking Cortical"),
                New(2.7f, "#B9C0C7",  8f, 26f, 2f, 2.0f, 2.7f, 1.00f, "2.7 mm Locking Cortical"),
                New(3.0f, "#2FDDE4", 10f, 30f, 2f, 2.4f, 3.0f, 1.00f, "3.0 mm Locking Cortical"),
                New(3.5f, "#FF7A33", 10f, 40f, 2f, 2.5f, 3.5f, 1.25f, "3.5 mm Locking Cortical"),
                New(4.0f, "#E23F9B", 10f, 40f, 2f, 2.9f, 4.0f, 1.25f, "4.0 mm Locking Cortical"),
                New(4.5f, "#4FE07E", 14f, 50f, 2f, 3.2f, 4.5f, 1.75f, "4.5 mm Locking Cortical"),
                New(5.0f, "#4C8CFF", 20f, 60f, 5f, 3.6f, 5.0f, 1.75f, "5.0 mm Locking Cortical"),
                New(6.0f, "#EFE64A", 25f, 70f, 5f, 4.5f, 6.0f, 2.00f, "6.0 mm Locking Cortical"),
                New(7.0f, "#FF9BD8", 30f, 80f, 5f, 5.0f, 7.0f, 2.75f, "7.0 mm Cancellous"),
            };

            // Catalog B: a different manufacturer. Different diameters, different colours
            // for the diameters that overlap, finer length steps at the small end, and a
            // different pitch on the 3.5, so swapping catalogs visibly changes the thread.
            var systemB = new[]
            {
                New(2.0f, "#A87CFF",  6f, 18f, 1f, 1.5f, 2.0f, 0.60f, "2.0 mm Locking Cortical"),
                New(2.4f, "#2FD3F5",  8f, 22f, 1f, 1.8f, 2.4f, 0.75f, "2.4 mm Locking Cortical"),
                New(2.7f, "#6BDC7C",  8f, 26f, 2f, 2.0f, 2.7f, 1.00f, "2.7 mm Locking Cortical"),
                New(3.5f, "#5AA0F0", 12f, 38f, 2f, 2.5f, 3.5f, 1.50f, "3.5 mm Locking Cortical"),
                New(5.0f, "#FFCE3D", 20f, 60f, 5f, 3.6f, 5.0f, 1.75f, "5.0 mm Locking Cortical"),
            };

            var a = BuildCatalog("Chromatic Ortho", "CatalogA_ChromaticOrtho", systemA);
            var b = BuildCatalog("Northline Fixation", "CatalogB_NorthlineFixation", systemB);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = a;
            EditorGUIUtility.PingObject(a);

            Debug.Log($"Created {a.screws.Length} + {b.screws.Length} screw specs and two " +
                      $"catalogs in {DataFolder}. Swap one catalog for the other to demonstrate " +
                      $"retargeting.");
        }

        static Entry New(float diameter, string hex, float from, float to, float step,
                         float threadDrill, float glideDrill, float pitch, string name)
            => new Entry
            {
                Diameter = diameter, Hex = hex,
                From = from, To = to, Step = step,
                ThreadDrill = threadDrill, GlideDrill = glideDrill,
                Pitch = pitch,
                Name = name
            };

        static ImplantCatalog BuildCatalog(string manufacturer, string assetName, Entry[] entries)
        {
            string folder = $"{DataFolder}/{assetName}";
            EnsureFolder(folder);

            var specs = new List<ScrewSpec>(entries.Length);

            foreach (var entry in entries)
            {
                var spec = ScriptableObject.CreateInstance<ScrewSpec>();
                spec.displayName          = entry.Name;
                spec.diameterMm           = entry.Diameter;
                spec.locking              = true;
                spec.trayColor            = ParseHex(entry.Hex);
                spec.availableLengthsMm   = Steps(entry.From, entry.To, entry.Step);
                spec.threadHoleDrillMm    = entry.ThreadDrill;
                spec.glideHoleDrillMm     = entry.GlideDrill;
                spec.threadPitchMm        = entry.Pitch;
                spec.threadStarts         = 1;
                spec.baseMeshLengthMm     = 20f;
                spec.headEndMm            = 2.5f;
                spec.tipLengthMm          = 2f;
                spec.notes                = Disclaimer;

                // Update in place rather than creating a duplicate, so re-running this after a
                // data change keeps the GUIDs and does not break the prefab and scene.
                string file = $"{folder}/Screw_{entry.Diameter.ToString("0.0").Replace('.', '_')}mm.asset";
                var existing = AssetDatabase.LoadAssetAtPath<ScrewSpec>(file);
                if (existing != null)
                {
                    EditorUtility.CopySerialized(spec, existing);
                    EditorUtility.SetDirty(existing);
                    spec = existing;
                }
                else
                {
                    AssetDatabase.CreateAsset(spec, file);
                }

                specs.Add(spec);
            }

            var catalog = ScriptableObject.CreateInstance<ImplantCatalog>();
            catalog.manufacturer = manufacturer;
            catalog.notes        = Disclaimer;
            catalog.screws       = specs.ToArray();

            string catalogPath = $"{folder}/{assetName}.asset";
            var existingCatalog = AssetDatabase.LoadAssetAtPath<ImplantCatalog>(catalogPath);
            if (existingCatalog != null)
            {
                EditorUtility.CopySerialized(catalog, existingCatalog);
                EditorUtility.SetDirty(existingCatalog);
                catalog = existingCatalog;
            }
            else
            {
                AssetDatabase.CreateAsset(catalog, catalogPath);
            }

            return catalog;
        }

        static float[] Steps(float from, float to, float step)
        {
            var values = new List<float>();
            for (float v = from; v <= to + 0.001f; v += step)
                values.Add(Mathf.Round(v * 10f) / 10f);
            return values.ToArray();
        }

        static Color ParseHex(string hex)
            => ColorUtility.TryParseHtmlString(hex, out var color) ? color : Color.magenta;

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            int split = path.LastIndexOf('/');
            string parent = path.Substring(0, split);
            string leaf   = path.Substring(split + 1);

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
