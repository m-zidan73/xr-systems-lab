using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lab.W01.VariableScrew.Tests
{
    /// <summary>The drawn length may slide between sizes. The chosen length, the collider and the tip never do.</summary>
    public class ScrewGlideTests
    {
        const float Tolerance = 1e-5f;

        GameObject _go;
        ScrewSpec _spec;
        Screw _screw;
        CapsuleCollider _collider;
        Transform _tip;

        [SetUp]
        public void Build()
        {
            _spec = ScriptableObject.CreateInstance<ScrewSpec>();
            _spec.diameterMm = 3.5f;
            _spec.baseMeshLengthMm = 20f;
            _spec.availableLengthsMm = new[] { 10f, 20f, 30f };

            _go = new GameObject("Screw");
            _screw = _go.AddComponent<Screw>();
            _collider = _go.AddComponent<CapsuleCollider>();
            _tip = new GameObject("Tip").transform;
            _tip.SetParent(_go.transform, false);
            var wiring = new SerializedObject(_screw);
            wiring.FindProperty("shaftCollider").objectReferenceValue = _collider;
            wiring.FindProperty("tipPoint").objectReferenceValue = _tip;
            wiring.ApplyModifiedPropertiesWithoutUndo();
            _screw.Set(_spec, 10f);
        }

        [TearDown]
        public void Destroy()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_spec);
        }

        float DrawnStretch()
        {
            var block = new MaterialPropertyBlock();
            _go.GetComponent<MeshRenderer>().GetPropertyBlock(block);
            return block.GetFloat("_Stretch");
        }

        void Settle()
        {
            for (var i = 0; i < 200 && _screw.IsGliding; i++) _screw.Tick(0.02f);
        }

        [Test]
        public void SetSmooth_ChoosesTheSizeAtOnce_AndStillDrawsTheOldLength()
        {
            _screw.SetSmooth(_spec, 30f);

            Assert.AreEqual(30f, _screw.LengthMm, Tolerance);
            Assert.AreEqual(0.03f, _tip.localPosition.z, Tolerance, "The tip is already where the chosen screw ends.");
            Assert.AreEqual(0.03f, _collider.height, Tolerance);
            Assert.IsTrue(_screw.IsGliding);
            Assert.AreEqual(10f, _screw.ShownLengthMm, Tolerance);
            Assert.AreEqual((10f - 20f) * 0.001f, DrawnStretch(), Tolerance);
        }

        [Test]
        public void AGlide_EndsExactlyOnTheChosenLength()
        {
            _screw.SetSmooth(_spec, 30f);
            _screw.Tick(0.02f);
            Assert.That(_screw.ShownLengthMm, Is.GreaterThan(10f).And.LessThan(30f));

            Settle();

            Assert.IsFalse(_screw.IsGliding);
            Assert.AreEqual(30f, _screw.ShownLengthMm, Tolerance);
            Assert.AreEqual((30f - 20f) * 0.001f, DrawnStretch(), Tolerance);
        }

        [Test]
        public void ASizeAskedForMidGlide_StartsFromWhatIsDrawn_AndLandsOnAShippedLength()
        {
            _screw.SetSmooth(_spec, 30f);
            _screw.Tick(0.02f);
            var drawn = _screw.ShownLengthMm;

            _screw.SetSmooth(_spec, 19f);

            Assert.AreEqual(20f, _screw.LengthMm, Tolerance, "19 mm doesn't ship, so it is 20.");
            Assert.AreEqual(drawn, _screw.ShownLengthMm, Tolerance, "No jump: it carries on from where it was.");
            Settle();
            Assert.AreEqual(20f, _screw.ShownLengthMm, Tolerance);
            Assert.AreEqual(0.02f, _tip.localPosition.z, Tolerance);
        }

        [Test]
        public void Set_StopsAGlide()
        {
            _screw.SetSmooth(_spec, 30f);
            _screw.Set(_spec, 20f);

            Assert.IsFalse(_screw.IsGliding);
            Assert.AreEqual(20f, _screw.ShownLengthMm, Tolerance);
            Assert.AreEqual(0f, DrawnStretch(), Tolerance);
        }

        [Test]
        public void SetSmooth_ToTheSameLength_DoesNotGlide()
        {
            _screw.SetSmooth(_spec, 10f);
            Assert.IsFalse(_screw.IsGliding);
        }

        [Test]
        public void Tick_WithNoGlide_ChangesNothing()
        {
            _screw.Tick(1f);
            Assert.AreEqual(10f, _screw.ShownLengthMm, Tolerance);
            Assert.AreEqual(10f, _screw.LengthMm, Tolerance);
        }
    }
}
