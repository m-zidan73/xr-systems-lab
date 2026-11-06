using System.Collections.Generic;
using Lab.Core;
using Lab.W01.VariableScrew;
using UnityEditor;
using UnityEngine;

namespace Lab.W01.ColourCodedHoles.Tests
{
    /// <summary>Builds throwaway specs, catalogs, holes and screws for the tests, and cleans them up.</summary>
    public sealed class HoleTestKit
    {
        readonly List<Object> _made = new List<Object>();

        T Track<T>(T thing) where T : Object
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

        public ScrewSpec Spec(float diameterMm, Color colour)
        {
            var spec = Track(ScriptableObject.CreateInstance<ScrewSpec>());
            spec.diameterMm = diameterMm;
            spec.trayColor = colour;
            spec.availableLengthsMm = new[] { 10f, 20f, 30f };
            return spec;
        }

        public ImplantCatalog Catalog(string manufacturer, params ScrewSpec[] specs)
        {
            var catalog = Track(ScriptableObject.CreateInstance<ImplantCatalog>());
            catalog.manufacturer = manufacturer;
            catalog.screws = specs;
            return catalog;
        }

        public Hole Hole(float diameterMm, ImplantCatalog catalog, Vector3 position)
        {
            var go = Track(new GameObject($"Hole {diameterMm}"));
            go.transform.position = position;
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.transform.SetParent(go.transform, false);
            var seat = new GameObject("Seat").transform;
            seat.SetParent(go.transform, false);
            seat.localPosition = new Vector3(0f, 0.0025f, 0f);
            seat.localRotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);

            var hole = go.AddComponent<Hole>();
            var wiring = new SerializedObject(hole);
            wiring.FindProperty("diameterMm").floatValue = diameterMm;
            wiring.FindProperty("seat").objectReferenceValue = seat;
            wiring.FindProperty("ring").objectReferenceValue = ring.GetComponent<Renderer>();
            wiring.ApplyModifiedPropertiesWithoutUndo();
            hole.SetCatalog(catalog);
            return hole;
        }

        public Screw Screw(ScrewSpec spec, Vector3 position)
        {
            var go = Track(new GameObject("Screw"));
            go.transform.position = position;
            var screw = go.AddComponent<Screw>();
            go.AddComponent<CapsuleCollider>();
            if (spec != null) screw.Set(spec, 20f);
            return screw;
        }

        public Grabbable Grabbable(Screw screw)
        {
            var item = screw.gameObject.AddComponent<Grabbable>();
            item.SetHome();
            return item;
        }

        public static Color RingColour(Hole hole)
        {
            var block = new MaterialPropertyBlock();
            hole.Ring.GetPropertyBlock(block);
            return block.GetColor("_BaseColor");
        }

        /// <summary>Colours come back from a property block with float rounding, so compare per channel.</summary>
        public static void AssertRingColour(Color expected, Hole hole)
        {
            var actual = RingColour(hole);
            for (var channel = 0; channel < 4; channel++)
                NUnit.Framework.Assert.AreEqual(expected[channel], actual[channel], 1e-4f, $"{hole.name} channel {channel}");
        }
    }
}
