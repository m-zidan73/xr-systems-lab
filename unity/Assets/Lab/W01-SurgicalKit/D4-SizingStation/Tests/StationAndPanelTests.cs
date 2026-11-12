using Lab.W01.VariableScrew;
using NUnit.Framework;
using UnityEngine;

namespace Lab.W01.SizingStation.Tests
{
    public class ScrewStationTests
    {
        const float Tolerance = 1e-5f;

        SizingTestKit _kit;
        Screw _first;
        ScrewSizer _sizer;
        ScrewStation _station;
        Transform _cradle;
        Transform _rack;

        [SetUp]
        public void Build()
        {
            _kit = new SizingTestKit();
            _first = _kit.Screw("First");
            _sizer = _kit.Sizer(_first);
            var along = Quaternion.LookRotation(Vector3.right, Vector3.up);
            _cradle = _kit.Track(new GameObject("Cradle")).transform;
            _cradle.SetPositionAndRotation(new Vector3(-0.03f, 0.006f, 0f), along);
            _rack = _kit.Track(new GameObject("Rack")).transform;
            _rack.SetPositionAndRotation(new Vector3(-0.03f, 0f, 0.1f), along);
            _station = _kit.Track(new GameObject("Station")).AddComponent<ScrewStation>();
            // The first screw doubles as the prefab: a copy of it shares its mesh and material.
            _station.Load(_sizer, _first, _first, _cradle, _rack);
        }

        [TearDown]
        public void Destroy() => _kit.Dispose();

        static float HeadRadius(float diameterMm) => ScrewUnits.MmWorld(ScrewUnits.HeadDiameterMm(diameterMm)) * 0.5f;

        [Test]
        public void TheScrew_LiesInTheHolder_RestingOnItsHead()
        {
            var expected = _cradle.position + Vector3.up * HeadRadius(3.5f);
            Assert.That(Vector3.Distance(expected, _first.transform.position), Is.LessThan(Tolerance));
            Assert.That(Vector3.Angle(Vector3.right, _first.transform.forward), Is.LessThan(0.01f), "It points along the scale.");
        }

        [Test]
        public void AWiderScrew_IsLifted_SoItStillRestsOnTheBase()
        {
            _sizer.SelectDiameter(2);
            Assert.AreEqual(_cradle.position.y + HeadRadius(5f), _first.transform.position.y, Tolerance);
        }

        [Test]
        public void Keep_PutsTheScrewOnTheRack_AndStartsANewOneAtTheSameSize()
        {
            _sizer.SelectLength(2);
            Screw spawned = null;
            _station.Spawned += screw => spawned = screw;

            _station.Keep();

            Assert.AreEqual(1, _station.Kept.Count);
            Assert.AreSame(_first, _station.Kept[0]);
            Assert.AreEqual(_rack.position.z, _first.transform.position.z, Tolerance);
            Assert.AreNotSame(_first, _station.Current);
            Assert.AreSame(_station.Current, spawned);
            Assert.AreSame(_station.Current, _sizer.Screw, "The panel now sizes the new screw.");
            Assert.AreEqual(14f, _station.Current.LengthMm, 1e-4f);
            Assert.AreSame(_kit.SharedMesh, _station.Current.GetComponent<MeshFilter>().sharedMesh, "The new screw is the same mesh again.");

            _sizer.SelectLength(0);
            Assert.AreEqual(14f, _first.LengthMm, 1e-4f, "A kept screw keeps its size.");
        }

        [Test]
        public void KeptScrews_FillTheRackPlaceByPlace_ThenStartANewRow()
        {
            for (var i = 0; i < 7; i++) _station.Keep();

            var first = _station.Kept[0].transform.position;
            var second = _station.Kept[1].transform.position;
            var seventh = _station.Kept[6].transform.position;
            Assert.AreEqual(0.015f, Vector3.Distance(first, second), Tolerance);
            Assert.AreEqual(first.z, seventh.z, Tolerance, "The seventh starts the second row, beside the first.");
            Assert.AreEqual(first.x + 0.09f, seventh.x, Tolerance);
        }

        [Test]
        public void Restart_ClearsTheRack_AndGoesBackToTheStartSize()
        {
            _sizer.SelectDiameter(2);
            _station.Keep();
            _station.Keep();

            _station.Restart();

            Assert.AreEqual(0, _station.Kept.Count);
            Assert.AreEqual(3.5f, _station.Current.Spec.diameterMm, 1e-4f);
            Assert.AreEqual(20f, _station.Current.LengthMm, 1e-4f);
        }
    }

    public class SizePanelTests
    {
        SizingTestKit _kit;
        Screw _screw;
        ScrewSizer _sizer;
        SizePanel _panel;

        [SetUp]
        public void Build()
        {
            _kit = new SizingTestKit();
            _screw = _kit.Screw();
            _sizer = _kit.Sizer(_screw);
            _panel = _kit.Track(new GameObject("Panel")).AddComponent<SizePanel>();
            _panel.Connect(_sizer);
            _panel.Build();
        }

        [TearDown]
        public void Destroy() => _kit.Dispose();

        [Test]
        public void ThereIsOneChipPerDiameter_AndOneTickPerShippedLength()
        {
            Assert.AreEqual(3, _panel.ChipCount);
            Assert.AreEqual("3.5", _panel.Chip(1).GetComponentInChildren<UnityEngine.UI.Text>().text);
            Assert.AreEqual(8, _panel.TickCount);
            Assert.AreEqual(7f, _panel.LengthSlider.maxValue);
            Assert.IsTrue(_panel.LengthSlider.wholeNumbers, "The slider can't stop between two lengths.");
            Assert.AreEqual("Maker A", _panel.MakerText);
            Assert.AreEqual("Ø 3.5 mm × 20 mm", _panel.SizeText);
        }

        [Test]
        public void ClickingAChip_PicksThatDiameter_AndRedrawsTheTicks()
        {
            _panel.Chip(2).onClick.Invoke();

            Assert.AreEqual(2, _sizer.DiameterIndex);
            Assert.AreEqual(5f, _screw.Spec.diameterMm, 1e-4f);
            Assert.AreEqual(20f, _sizer.LengthMm, 1e-4f, "The shorter slider must not pick a length of its own.");
            Assert.AreEqual(0f, _panel.LengthSlider.value);
            Assert.AreEqual(3, _panel.TickCount);
            Assert.AreEqual(2f, _panel.LengthSlider.maxValue);
            Assert.IsTrue(_panel.Chip(2).GetComponent<UnityEngine.UI.Outline>().enabled);
            Assert.IsFalse(_panel.Chip(1).GetComponent<UnityEngine.UI.Outline>().enabled);
        }

        [Test]
        public void DraggingTheSlider_PicksTheLengthAtThatTick()
        {
            _panel.LengthSlider.value = 6f;

            Assert.AreEqual(22f, _sizer.LengthMm, 1e-4f);
            Assert.AreEqual(22f, _screw.LengthMm, 1e-4f);
            Assert.AreEqual("Ø 3.5 mm × 22 mm", _panel.SizeText);
        }

        [Test]
        public void TheSlider_FollowsASizeChosenElsewhere()
        {
            _sizer.StepLength(-2);
            Assert.AreEqual(3f, _panel.LengthSlider.value);
        }

        [Test]
        public void AnotherCatalog_GetsItsOwnChips()
        {
            _sizer.NextCatalog();

            Assert.AreEqual(2, _panel.ChipCount);
            Assert.AreEqual("Maker B", _panel.MakerText);
            Assert.AreEqual(14, _panel.TickCount);
            Assert.AreEqual("4", _panel.Chip(1).GetComponentInChildren<UnityEngine.UI.Text>().text);
        }

        [Test]
        public void TheMeasuredDepth_ShowsOnlyWhenThereIsOne()
        {
            Assert.AreEqual("", _panel.DepthText);
            _panel.ShowDepth(19.6f);
            Assert.AreEqual("Measured depth: 19.6 mm", _panel.DepthText);
            _panel.HideDepth();
            Assert.AreEqual("", _panel.DepthText);
        }
    }
}
