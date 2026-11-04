using System.Globalization;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lab.W01.VariableScrew.Tests
{
    public class ScrewSystemTests
    {
        const float Tolerance = 1e-5f;

        CultureInfo _originalCulture;
        GameObject _screwObject;
        ScrewSpec _spec;
        ImplantCatalog _catalog;

        [SetUp]
        public void SetUp()
        {
            _originalCulture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        }

        [TearDown]
        public void TearDown()
        {
            CultureInfo.CurrentCulture = _originalCulture;
            if (_screwObject != null) Object.DestroyImmediate(_screwObject);
            if (_spec != null) Object.DestroyImmediate(_spec);
            if (_catalog != null) Object.DestroyImmediate(_catalog);
        }

        ScrewSpec MakeSpec(float diameterMm, params float[] lengthsMm)
        {
            _spec = ScriptableObject.CreateInstance<ScrewSpec>();
            _spec.diameterMm = diameterMm;
            _spec.availableLengthsMm = lengthsMm;
            return _spec;
        }

        Screw MakeScrew(out CapsuleCollider collider, out Transform tip)
        {
            _screwObject = new GameObject("Test Screw");
            var screw = _screwObject.AddComponent<Screw>();
            collider = _screwObject.AddComponent<CapsuleCollider>();
            tip = new GameObject("Tip").transform;
            tip.SetParent(_screwObject.transform);

            var wiring = new SerializedObject(screw);
            wiring.FindProperty("shaftCollider").objectReferenceValue = collider;
            wiring.FindProperty("tipPoint").objectReferenceValue = tip;
            wiring.ApplyModifiedPropertiesWithoutUndo();
            return screw;
        }

        [Test]
        public void NearestAvailable_OutOfRange_SnapsToEnds()
        {
            var spec = MakeSpec(3.5f, 10f, 12f, 14f);
            Assert.AreEqual(10f, spec.NearestAvailable(-5f));
            Assert.AreEqual(14f, spec.NearestAvailable(1000f));
        }

        [Test]
        public void NearestAvailable_Tie_PicksShorter()
        {
            Assert.AreEqual(10f, MakeSpec(3.5f, 10f, 12f).NearestAvailable(11f));
        }

        [Test]
        public void NearestAvailable_EmptyList_ReturnsRequest()
        {
            Assert.AreEqual(7f, MakeSpec(3.5f).NearestAvailable(7f));
        }

        [Test]
        public void Set_SnapsAndMovesColliderAndTip()
        {
            var screw = MakeScrew(out var collider, out var tip);
            screw.Set(MakeSpec(3.5f, 10f, 22f, 24f), 23f);
            Assert.AreEqual(22f, screw.LengthMm);
            Assert.AreEqual(0.022f, collider.height, Tolerance);
            Assert.AreEqual(0.022f, tip.localPosition.z, Tolerance);
        }

        [Test]
        public void Set_NullSpec_DoesNotThrow()
        {
            var screw = MakeScrew(out _, out _);
            screw.Set(null, 23f);
            Assert.AreEqual(23f, screw.LengthMm);
        }

        [Test]
        public void Label_KeepsSymbols()
        {
            var screw = MakeScrew(out _, out _);
            screw.Set(MakeSpec(3.5f, 10f, 26f), 26f);
            Assert.AreEqual("Ø 3.5 mm × 26 mm", screw.Label);
        }

        [Test]
        public void ForHole_MatchesOrReturnsNull()
        {
            var spec = MakeSpec(3.5f, 10f);
            _catalog = ScriptableObject.CreateInstance<ImplantCatalog>();
            Assert.IsNull(_catalog.ForHole(3.5f));
            _catalog.screws = new[] { spec };
            Assert.AreSame(spec, _catalog.ForHole(3.5f));
            Assert.IsNull(_catalog.ForHole(3.3f));
        }
    }
}
