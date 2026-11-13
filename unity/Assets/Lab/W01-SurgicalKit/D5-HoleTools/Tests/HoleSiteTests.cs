using System.Collections.Generic;
using Lab.Core;
using Lab.W01.ColourCodedHoles;
using NUnit.Framework;
using UnityEngine;

namespace Lab.W01.HoleTools.Tests
{
    public class BoneDepthTests
    {
        ToolTestScene _s;

        [SetUp] public void Build() => _s = new ToolTestScene();
        [TearDown] public void Destroy() => _s.Dispose();

        [Test]
        public void Depth_RunsFromTheHoleToTheFarSide_IncludingTheGap()
        {
            Assert.IsTrue(BoneDepth.TryMeasure(_s.Bone, Vector3.zero, Vector3.down, 0.08f, out var depth, out var far));
            Assert.AreEqual(ToolTestScene.DepthMm, depth, 0.01f);
            Assert.AreEqual(-ToolTestScene.DepthMm * 0.001f, far.y, 1e-5f);
        }

        [Test]
        public void Depth_IsMeasuredAlongTheHole_SoATiltedHoleReadsDeeper()
        {
            var tilted = Quaternion.Euler(0f, 0f, 30f) * Vector3.down;
            Assert.IsTrue(BoneDepth.TryMeasure(_s.Bone, Vector3.zero, tilted, 0.08f, out var depth, out _));
            Assert.AreEqual(ToolTestScene.DepthMm / Mathf.Cos(30f * Mathf.Deg2Rad), depth, 0.01f);
        }

        [Test]
        public void NoBoneOnTheAxis_IsNotADepth()
        {
            Assert.IsFalse(BoneDepth.TryMeasure(_s.Bone, new Vector3(0.2f, 0f, 0f), Vector3.down, 0.08f, out _, out _));
            Assert.IsFalse(BoneDepth.TryMeasure(_s.Bone, Vector3.zero, Vector3.up, 0.08f, out _, out _), "Pointing away from the bone.");
            Assert.IsFalse(BoneDepth.TryMeasure(null, Vector3.zero, Vector3.down, 0.08f, out _, out _));
        }

        [Test]
        public void OnlyTheBoneIsMeasured_WhateverElseIsInTheWay()
        {
            var other = _s.Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            other.transform.position = new Vector3(0f, -0.05f, 0f);
            other.transform.localScale = Vector3.one * 0.02f;
            Physics.SyncTransforms();
            Assert.IsTrue(BoneDepth.TryMeasure(_s.Bone, Vector3.zero, Vector3.down, 0.08f, out var depth, out _));
            Assert.AreEqual(ToolTestScene.DepthMm, depth, 0.01f);
        }
    }

    public class HoleSiteTests
    {
        ToolTestScene _s;

        [SetUp] public void Build() => _s = new ToolTestScene();
        [TearDown] public void Destroy() => _s.Dispose();

        [Test]
        public void AnUntouchedHole_ShowsOnlyItsDiameter()
        {
            Assert.IsFalse(_s.Site.IsDrilled);
            Assert.IsFalse(_s.Site.IsMeasured);
            Assert.AreEqual("Ø 3.5", _s.Site.Label);
            Assert.IsFalse(_s.Bore.activeSelf);
        }

        [Test]
        public void Drill_UsesTheBitTheCatalogGives()
        {
            HoleSite heard = null;
            _s.Site.Drilled += site => heard = site;

            Assert.AreEqual(DrillResult.Drilled, _s.Site.Drill());

            Assert.IsTrue(_s.Site.IsDrilled);
            Assert.AreEqual(2.5f, _s.Site.DrillMm, 1e-4f);
            Assert.AreSame(_s.Site, heard);
            Assert.IsTrue(_s.Bore.activeSelf);
            Assert.AreEqual(DrillResult.AlreadyDrilled, _s.Site.Drill());
        }

        [Test]
        public void Drill_HasNoBit_WhenTheCatalogHasNoScrewForTheHole()
        {
            _s.Hole.SetCatalog(null);
            Assert.AreEqual(DrillResult.NotInCatalog, _s.Site.Drill());
            Assert.IsFalse(_s.Site.IsDrilled);
        }

        [Test]
        public void Measure_ComesAfterDrilling()
        {
            Assert.AreEqual(GaugeResult.NotDrilled, _s.Site.Measure());
            Assert.IsFalse(_s.Site.IsMeasured);
        }

        [Test]
        public void Measure_ReadsTheDepth_AndTheLabelShowsIt()
        {
            HoleSite heard = null;
            _s.Site.Measured += site => heard = site;
            _s.Site.Drill();

            Assert.AreEqual(GaugeResult.Measured, _s.Site.Measure());

            Assert.AreEqual(ToolTestScene.DepthMm, _s.Site.DepthMm, 0.01f);
            Assert.AreEqual("Ø 3.5 · 15.0 mm", _s.Site.Label);
            Assert.AreSame(_s.Site, heard);
            Assert.AreEqual(-ToolTestScene.DepthMm * 0.001f, _s.Site.FarPoint.y, 1e-5f);
        }

        [Test]
        public void Measure_SaysSo_WhenThereIsNoBoneUnderTheHole()
        {
            _s.Site.Drill();
            _s.MoveHole(new Vector3(0.2f, 0f, 0f));
            Assert.AreEqual(GaugeResult.NoBone, _s.Site.Measure());
            Assert.IsFalse(_s.Site.IsMeasured);
        }

        [Test]
        public void MovingThePlate_DropsTheReading_AndMeasuringAgainGivesTheNewDepth()
        {
            var lost = 0;
            _s.Site.ReadingLost += _ => lost++;
            _s.Site.Drill();
            _s.Site.Measure();
            Assert.IsFalse(_s.Site.CheckReading(), "Nothing moved yet.");

            _s.MoveHole(new Vector3(0f, 0.002f, 0f));

            Assert.IsTrue(_s.Site.CheckReading());
            Assert.AreEqual(1, lost);
            Assert.IsFalse(_s.Site.IsMeasured);
            Assert.IsTrue(_s.Site.IsDrilled, "The hole stays drilled.");
            Assert.AreEqual("Ø 3.5", _s.Site.Label);

            Assert.AreEqual(GaugeResult.Measured, _s.Site.Measure());
            Assert.AreEqual(ToolTestScene.DepthMm + 2f, _s.Site.DepthMm, 0.01f);
        }

        [Test]
        public void Clear_MakesTheHoleUntouchedAgain()
        {
            var cleared = 0;
            _s.Site.Cleared += _ => cleared++;
            _s.Site.Drill();
            _s.Site.Measure();

            _s.Site.Clear();

            Assert.IsFalse(_s.Site.IsDrilled);
            Assert.IsFalse(_s.Site.IsMeasured);
            Assert.IsFalse(_s.Bore.activeSelf);
            Assert.AreEqual(1, cleared);
            Assert.AreEqual(GaugeResult.NotDrilled, _s.Site.Measure());
        }
    }

    public class HoleSiteTargetTests
    {
        ToolTestScene _s;

        [SetUp] public void Build() => _s = new ToolTestScene();
        [TearDown] public void Destroy() => _s.Dispose();

        [Test]
        public void TheHoleOffersOnePlaceToDrop_ForToolsAndScrews()
        {
            Assert.AreEqual(1, _s.Hole.GetComponents<IDropTarget>().Length);
            Assert.AreEqual(_s.Hole.Seat.position, _s.Target.DropPoint);
        }

        [Test]
        public void DroppingTheDrill_DrillsTheHole_AndTheDrillStaysForAMoment()
        {
            DrillResult? heard = null;
            _s.Target.DrillUsed += (_, result) => heard = result;

            Assert.IsTrue(_s.Target.TryDrop(_s.Drill.GetComponent<Grabbable>()));

            Assert.IsTrue(_s.Site.IsDrilled);
            Assert.AreEqual(DrillResult.Drilled, heard);
            Assert.IsTrue(_s.Drill.IsWorking);
            Assert.That(Vector3.Distance(_s.Hole.Seat.position, _s.Drill.transform.position), Is.LessThan(1e-6f));
            Assert.That(Vector3.Angle(_s.Hole.Seat.forward, _s.Drill.transform.forward), Is.LessThan(0.01f), "It points along the hole.");

            _s.Drill.Finish();
            Assert.IsFalse(_s.Drill.IsWorking);
            Assert.That(Vector3.Distance(new Vector3(0.1f, 0f, 0f), _s.Drill.transform.position), Is.LessThan(1e-6f), "Back where it was picked up.");
        }

        [Test]
        public void DroppingTheGaugeFirst_IsRefused_AndTheGaugeIsLeftAlone()
        {
            GaugeResult? heard = null;
            _s.Target.GaugeUsed += (_, result) => heard = result;
            var before = _s.Gauge.transform.position;

            Assert.IsFalse(_s.Target.TryDrop(_s.Gauge.GetComponent<Grabbable>()));

            Assert.AreEqual(GaugeResult.NotDrilled, heard);
            Assert.IsFalse(_s.Gauge.IsWorking);
            Assert.AreEqual(before, _s.Gauge.transform.position);
        }

        [Test]
        public void DrillThenGauge_MeasuresTheHole()
        {
            _s.Target.TryDrop(_s.Drill.GetComponent<Grabbable>());
            Assert.IsTrue(_s.Target.TryDrop(_s.Gauge.GetComponent<Grabbable>()));
            Assert.IsTrue(_s.Site.IsMeasured);
            Assert.AreEqual(ToolTestScene.DepthMm, _s.Site.DepthMm, 0.01f);
        }

        [Test]
        public void DrillingTwice_DoesNothingMore()
        {
            _s.Target.TryDrop(_s.Drill.GetComponent<Grabbable>());
            _s.Drill.Finish();
            Assert.IsFalse(_s.Target.TryDrop(_s.Drill.GetComponent<Grabbable>()));
        }

        [Test]
        public void AScrew_StillGoesThroughDayOnesRule()
        {
            var fits = _s.Screw(_s.Spec35);
            var wrong = _s.Screw(_s.OtherSpec(5f));

            Assert.IsFalse(_s.Target.TryDrop(_s.Grabbable(wrong)));
            Assert.IsNull(_s.Hole.Occupant);

            var item = _s.Grabbable(fits);
            Assert.IsTrue(_s.Target.TryDrop(item));
            Assert.AreSame(fits, _s.Hole.Occupant);

            item.PickUp();
            Assert.IsNull(_s.Hole.Occupant, "Lifting the screw out empties the hole, as on day 1.");
        }
    }

    public class HoleSitesTests
    {
        ToolTestScene _s;
        StatusLine _status;
        HoleSites _sites;

        [SetUp]
        public void Build()
        {
            _s = new ToolTestScene();
            var go = _s.Track(new GameObject("Controls"));
            _status = go.AddComponent<StatusLine>();
            _sites = go.AddComponent<HoleSites>();
            _sites.Connect(new[] { _s.Target }, _status);
        }

        [TearDown] public void Destroy() => _s.Dispose();

        [Test]
        public void TheStatusLine_SaysWhatEachToolDid()
        {
            _s.Target.Use(_s.Gauge);
            Assert.AreEqual(ToolMessages.MeasureFirst, _status.Message);

            _s.Target.Use(_s.Drill);
            Assert.AreEqual("Drilled the Ø 3.5 hole with the 2.5 mm bit, the size Test Maker gives for that screw.", _status.Message);

            _s.Target.Use(_s.Drill);
            Assert.AreEqual("The Ø 3.5 hole is already drilled.", _status.Message);

            _s.Target.Use(_s.Gauge);
            Assert.AreEqual("Depth 15.0 mm at the Ø 3.5 hole, from the plate to the far side of the bone.", _status.Message);
        }

        [Test]
        public void TheStatusLine_SaysWhenAReadingNoLongerHolds_OrThereIsNoBone()
        {
            _s.Target.Use(_s.Drill);
            _s.Target.Use(_s.Gauge);
            _s.MoveHole(new Vector3(0.2f, 0f, 0f));
            _s.Site.CheckReading();
            Assert.AreEqual(ToolMessages.ReadingLost, _status.Message);

            _s.Target.Use(_s.Gauge);
            Assert.AreEqual(ToolMessages.NoBone, _status.Message);
        }

        [Test]
        public void ClearAll_PutsEveryHoleBack()
        {
            _s.Target.Use(_s.Drill);
            _s.Target.Use(_s.Gauge);
            _sites.ClearAll();
            Assert.IsFalse(_s.Site.IsDrilled);
            Assert.IsFalse(_s.Site.IsMeasured);
            Assert.AreEqual(ToolMessages.Cleared, _status.Message);
        }
    }

    public class HoleLabelsTests
    {
        ToolTestScene _s;

        [SetUp] public void Build() => _s = new ToolTestScene();
        [TearDown] public void Destroy() => _s.Dispose();

        [Test]
        public void ALabel_ShowsTheDiameter_ThenTheDepth_AndNeverALength()
        {
            Assert.AreEqual("Ø 3.5", HoleLabels.Text(_s.Site));
            _s.Site.Drill();
            _s.Site.Measure();
            Assert.AreEqual("Ø 3.5 · 15.0 mm", HoleLabels.Text(_s.Site));
        }

        [Test]
        public void Spread_LeavesLabelsAlone_WhenTheyAlreadyHaveRoom()
        {
            var rows = new List<float> { 100f, 140f, 200f };
            HoleLabels.Spread(rows, 30f);
            CollectionAssert.AreEqual(new[] { 100f, 140f, 200f }, rows);
        }

        [Test]
        public void Spread_PushesCrowdedLabelsApart_AroundWhereTheyWere()
        {
            var rows = new List<float> { 100f, 104f, 110f };
            HoleLabels.Spread(rows, 30f);
            Assert.AreEqual(30f, rows[1] - rows[0], 1e-3f);
            Assert.AreEqual(30f, rows[2] - rows[1], 1e-3f);
            Assert.AreEqual((100f + 104f + 110f) / 3f, (rows[0] + rows[1] + rows[2]) / 3f, 1e-3f, "The group stays centred.");
        }
    }
}
