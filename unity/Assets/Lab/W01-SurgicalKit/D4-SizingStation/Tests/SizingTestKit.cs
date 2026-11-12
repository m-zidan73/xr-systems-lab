using System.Collections.Generic;
using Lab.W01.VariableScrew;
using UnityEngine;

namespace Lab.W01.SizingStation.Tests
{
    /// <summary>Builds two throwaway catalogs, screws that share one mesh and one material, and a sizer, and cleans them up.</summary>
    public sealed class SizingTestKit
    {
        readonly List<Object> _made = new List<Object>();

        public readonly ImplantCatalog CatalogA;
        public readonly ImplantCatalog CatalogB;
        public readonly Mesh SharedMesh;
        public readonly Material SharedMaterial;

        public SizingTestKit()
        {
            CatalogA = Catalog("Maker A",
                Spec(2.7f, Color.red, 8f, 10f, 12f),
                Spec(3.5f, Color.blue, 10f, 12f, 14f, 16f, 18f, 20f, 22f, 24f),
                Spec(5f, Color.green, 20f, 25f, 30f));
            CatalogB = Catalog("Maker B",
                Spec(2.4f, Color.yellow, 6f, 8f),
                Spec(4f, Color.cyan, 14f, 16f, 18f, 20f, 22f, 24f, 26f, 28f, 30f, 32f, 34f, 36f, 38f, 40f));

            SharedMesh = Track(new Mesh { name = "Shared Screw Mesh" });
            SharedMesh.vertices = new[] { Vector3.zero, Vector3.right, Vector3.up };
            SharedMesh.triangles = new[] { 0, 1, 2 };
            SharedMaterial = Track(new Material(Shader.Find("Universal Render Pipeline/Unlit")));
        }

        public T Track<T>(T thing) where T : Object
        {
            _made.Add(thing);
            return thing;
        }

        public void Dispose()
        {
            foreach (var thing in _made)
                if (thing != null) Object.DestroyImmediate(thing);
            _made.Clear();
        }

        public ScrewSpec Spec(float diameterMm, Color colour, params float[] lengthsMm)
        {
            var spec = Track(ScriptableObject.CreateInstance<ScrewSpec>());
            spec.diameterMm = diameterMm;
            spec.trayColor = colour;
            spec.availableLengthsMm = lengthsMm;
            return spec;
        }

        public ImplantCatalog Catalog(string manufacturer, params ScrewSpec[] specs)
        {
            var catalog = Track(ScriptableObject.CreateInstance<ImplantCatalog>());
            catalog.manufacturer = manufacturer;
            catalog.screws = specs;
            return catalog;
        }

        public Screw Screw(string name = "Screw")
        {
            var go = Track(new GameObject(name));
            go.AddComponent<MeshFilter>().sharedMesh = SharedMesh;
            var screw = go.AddComponent<Screw>();
            go.GetComponent<MeshRenderer>().sharedMaterial = SharedMaterial;
            return screw;
        }

        public ScrewSizer Sizer(Screw screw)
        {
            var sizer = Track(new GameObject("Sizer")).AddComponent<ScrewSizer>();
            sizer.Load(new[] { CatalogA, CatalogB }, screw);
            return sizer;
        }
    }
}
