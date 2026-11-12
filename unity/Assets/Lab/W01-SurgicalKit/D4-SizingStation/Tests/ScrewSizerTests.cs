using Lab.Core;
using Lab.W01.VariableScrew;
using NUnit.Framework;
using UnityEngine;

namespace Lab.W01.SizingStation.Tests
{
    public class ScrewSizerTests
    {
        const float Tolerance = 1e-4f;

        SizingTestKit _kit;
        Screw _screw;
        ScrewSizer _sizer;

        [SetUp]
        public void Build()
        {
            _kit = new SizingTestKit();
            _screw = _kit.Screw();
            _sizer = _kit.Sizer(_screw);
        }

        [TearDown]
        public void Destroy() => _kit.Dispose();

        [Test]
        public void ItStartsOnTheStartSize_AndSizesTheScrew()
        {
            Assert.AreEqual(3.5f, _sizer.Spec.diameterMm, Tolerance);
            Assert.AreEqual(20f, _sizer.LengthMm, Tolerance);
            Assert.AreSame(_sizer.Spec, _screw.Spec);
            Assert.AreEqual(20f, _screw.LengthMm, Tolerance);
            Assert.AreEqual("Ø 3.5 mm × 20 mm", _sizer.Label);
        }

        [Test]
        public void ANewDiameter_MovesTheLengthToTheNearestOneItShips()
        {
            _sizer.SelectDiameter(0);

            Assert.AreEqual(2.7f, _sizer.Spec.diameterMm, Tolerance);
            Assert.AreEqual(12f, _sizer.LengthMm, Tolerance, "2.7 mm stops at 12, so 20 comes down to it.");
            Assert.AreSame(_sizer.Spec, _screw.Spec);
            Assert.AreEqual(12f, _screw.LengthMm, Tolerance);
        }

        [Test]
        public void ADiameterOutsideTheCatalog_StopsAtTheEnds()
        {
            _sizer.SelectDiameter(99);
            Assert.AreEqual(2, _sizer.DiameterIndex);
            _sizer.SelectDiameter(-5);
            Assert.AreEqual(0, _sizer.DiameterIndex);
        }

        [Test]
        public void ALength_OnlyLandsOnOnesThatShip()
        {
            _sizer.SetLengthMm(16.9f);
            Assert.AreEqual(16f, _sizer.LengthMm, Tolerance);
            Assert.AreEqual(16f, _screw.LengthMm, Tolerance);

            _sizer.SelectLength(0);
            Assert.AreEqual(10f, _sizer.LengthMm, Tolerance);
            Assert.AreEqual(0, _sizer.LengthIndex);
        }

        [Test]
        public void SteppingTheLength_StopsAtTheShortestAndTheLongest()
        {
            _sizer.SelectLength(0);
            _sizer.StepLength(-1);
            Assert.AreEqual(10f, _sizer.LengthMm, Tolerance);

            for (var i = 0; i < 20; i++) _sizer.StepLength(1);
            Assert.AreEqual(24f, _sizer.LengthMm, Tolerance);
        }

        [Test]
        public void SteppingTheDiameter_MovesOneAtATime()
        {
            _sizer.StepDiameter(1);
            Assert.AreEqual(5f, _sizer.Spec.diameterMm, Tolerance);
            _sizer.StepDiameter(1);
            Assert.AreEqual(5f, _sizer.Spec.diameterMm, Tolerance);
        }

        [Test]
        public void TheNextCatalog_KeepsTheNearestDiameterAndLength_AndSaysSo()
        {
            ImplantCatalog told = null;
            var changes = 0;
            _sizer.CatalogChanged += catalog => told = catalog;
            _sizer.Changed += () => changes++;

            _sizer.NextCatalog();

            Assert.AreSame(_kit.CatalogB, _sizer.Catalog);
            Assert.AreSame(_kit.CatalogB, told);
            Assert.AreEqual(1, changes);
            Assert.AreEqual(4f, _sizer.Spec.diameterMm, Tolerance, "Maker B has no 3.5, and 4 is nearer than 2.4.");
            Assert.AreEqual(20f, _sizer.LengthMm, Tolerance);
            Assert.AreSame(_sizer.Spec, _screw.Spec);

            _sizer.NextCatalog();
            Assert.AreSame(_kit.CatalogA, _sizer.Catalog, "After the last catalog comes the first.");
        }

        [Test]
        public void EveryChange_IsAnnouncedOnce()
        {
            var changes = 0;
            _sizer.Changed += () => changes++;
            _sizer.SelectDiameter(2);
            _sizer.SelectLength(1);
            _sizer.ResetAxes();
            Assert.AreEqual(3, changes);
        }

        [Test]
        public void AsAxes_ItReportsTheCatalogsRange_AndSnaps()
        {
            IAxisAdjustable axes = _sizer;
            Assert.AreEqual(2, axes.AxisCount);
            Assert.AreEqual("mm", axes.AxisUnit(ScrewSizer.LengthAxis));
            Assert.AreEqual(2.7f, axes.AxisMin(ScrewSizer.DiameterAxis), Tolerance);
            Assert.AreEqual(5f, axes.AxisMax(ScrewSizer.DiameterAxis), Tolerance);
            Assert.AreEqual(10f, axes.AxisMin(ScrewSizer.LengthAxis), Tolerance);
            Assert.AreEqual(24f, axes.AxisMax(ScrewSizer.LengthAxis), Tolerance);

            Assert.AreEqual(5f, axes.SetAxis(ScrewSizer.DiameterAxis, 4.6f), Tolerance);
            Assert.AreEqual(25f, axes.SetAxis(ScrewSizer.LengthAxis, 26f), Tolerance);
            Assert.AreEqual(25f, _screw.LengthMm, Tolerance);
        }

        [Test]
        public void Reset_GoesBackToTheStartSize()
        {
            _sizer.SelectDiameter(2);
            _sizer.ResetAxes();
            Assert.AreEqual(3.5f, _sizer.Spec.diameterMm, Tolerance);
            Assert.AreEqual(20f, _sizer.LengthMm, Tolerance);
        }

        [Test]
        public void AttachingAnotherScrew_GivesItTheCurrentSize_AndLeavesTheFirstAlone()
        {
            _sizer.SelectDiameter(2);
            _sizer.SelectLength(1);
            var second = _kit.Screw("Second");

            _sizer.Attach(second);
            _sizer.SelectLength(2);

            Assert.AreEqual(30f, second.LengthMm, Tolerance);
            Assert.AreEqual(25f, _screw.LengthMm, Tolerance, "The first screw keeps the size it had.");
            Assert.IsTrue(second.IsGliding, "Only the picture slides. The size is already right.");
        }

        [Test]
        public void WithNoCatalog_ItDoesNothing_AndSaysNoSize()
        {
            var empty = _kit.Track(new GameObject("Empty")).AddComponent<ScrewSizer>();
            empty.Load(new ImplantCatalog[0], null);
            empty.SelectDiameter(1);
            empty.StepLength(1);
            empty.NextCatalog();
            Assert.IsNull(empty.Spec);
            Assert.AreEqual("no size", empty.Label);
        }
    }
}
