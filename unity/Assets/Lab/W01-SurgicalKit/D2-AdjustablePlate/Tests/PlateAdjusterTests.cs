using System.Collections.Generic;
using Lab.Core;
using NUnit.Framework;
using UnityEngine;

namespace Lab.W01.AdjustablePlate.Tests
{
    public class PlatePoseTests
    {
        static readonly Vector3 StartPosition = new Vector3(0.01f, 0.02f, 0.03f);
        static readonly Quaternion StartRotation = Quaternion.Euler(10f, 20f, 30f);

        [Test]
        public void ZeroOffsets_LeaveTheStartPose()
        {
            var (position, rotation) = PlatePose.Apply(StartPosition, StartRotation, new float[6]);
            Assert.That(Vector3.Distance(StartPosition, position), Is.LessThan(1e-6f));
            Assert.That(Quaternion.Angle(StartRotation, rotation), Is.LessThan(0.01f));
        }

        [Test]
        public void Moves_AreMillimetresAlongTheFrameAxes()
        {
            var (position, _) = PlatePose.Apply(StartPosition, StartRotation, new[] { 3f, -12f, 5f, 0f, 0f, 0f });
            var expected = StartPosition + new Vector3(0.003f, -0.012f, 0.005f);
            Assert.That(Vector3.Distance(expected, position), Is.LessThan(1e-6f));
        }

        [Test]
        public void Turns_GoAroundTheFrameAxes_AndNeverMoveTheOrigin()
        {
            var (position, rotation) = PlatePose.Apply(StartPosition, StartRotation, new[] { 0f, 0f, 0f, 0f, 90f, 0f });
            Assert.That(Vector3.Distance(StartPosition, position), Is.LessThan(1e-6f));
            Assert.That(Quaternion.Angle(Quaternion.AngleAxis(90f, Vector3.up) * StartRotation, rotation), Is.LessThan(0.01f));
        }

        [Test]
        public void StartFor_LandsOnTheTargetAtTheTargetOffsets()
        {
            var offsets = new[] { 3f, -12f, -4f, 6f, -8f, 10f };
            var targetPosition = new Vector3(0.1f, 0.05f, -0.02f);
            var targetRotation = Quaternion.Euler(-30f, 75f, 5f);
            var (startPosition, startRotation) = PlatePose.StartFor(targetPosition, targetRotation, offsets);
            var (position, rotation) = PlatePose.Apply(startPosition, startRotation, offsets);
            Assert.That(Vector3.Distance(targetPosition, position), Is.LessThan(1e-6f));
            Assert.That(Quaternion.Angle(targetRotation, rotation), Is.LessThan(0.01f));
        }
    }

    public class PlateAdjusterTests
    {
        GameObject _frame;
        PlateAdjuster _adjuster;

        [SetUp]
        public void Build()
        {
            _frame = new GameObject("Frame");
            _frame.transform.SetPositionAndRotation(new Vector3(1f, 2f, 3f), Quaternion.Euler(0f, 40f, 0f));
            var plate = new GameObject("Plate");
            plate.transform.SetParent(_frame.transform, false);
            plate.transform.SetLocalPositionAndRotation(new Vector3(0.01f, 0f, 0f), Quaternion.Euler(0f, 0f, 15f));
            _adjuster = plate.AddComponent<PlateAdjuster>();
        }

        [TearDown]
        public void Destroy() => Object.DestroyImmediate(_frame);

        static IEnumerable<PlateAxis> AllAxes()
        {
            for (var i = 0; i < PlateAxes.Count; i++) yield return (PlateAxis)i;
        }

        void MoveToTarget()
        {
            foreach (var axis in AllAxes()) _adjuster.Set(axis, _adjuster.Settings(axis).target);
        }

        [Test]
        public void Defaults_HaveSixAxesWithTargetsInsideTheirLimits()
        {
            foreach (var axis in AllAxes())
            {
                var settings = _adjuster.Settings(axis);
                Assert.IsFalse(string.IsNullOrEmpty(settings.label), axis.ToString());
                Assert.Less(settings.min, settings.max, axis.ToString());
                Assert.That(settings.target, Is.InRange(settings.min, settings.max), axis.ToString());
                Assert.AreNotEqual(0f, settings.target, $"{axis}: the plate starts out of place on every axis.");
            }
        }

        [Test]
        public void Set_KeepsEveryAxisWithinItsLimits()
        {
            foreach (var axis in AllAxes())
            {
                var settings = _adjuster.Settings(axis);
                Assert.AreEqual(settings.max, _adjuster.Set(axis, settings.max + 100f), axis.ToString());
                Assert.IsTrue(_adjuster.AtLimit(axis));
                Assert.AreEqual(settings.min, _adjuster.Set(axis, settings.min - 100f), axis.ToString());
                Assert.IsTrue(_adjuster.AtLimit(axis));
            }
        }

        [Test]
        public void Lift_CannotGoPastTheBone()
        {
            var lift = _adjuster.Settings(PlateAxis.MoveZ);
            Assert.AreEqual(lift.target, lift.min, "On target the plate rests on the bone, and Lift's limit stops it there.");
        }

        [Test]
        public void Set_MovesThePlateInItsParentFrame()
        {
            var start = _adjuster.transform.localPosition;
            _adjuster.Set(PlateAxis.MoveY, 10f);
            Assert.That(Vector3.Distance(start + new Vector3(0f, 0.01f, 0f), _adjuster.transform.localPosition), Is.LessThan(1e-6f));
            _adjuster.Set(PlateAxis.TurnZ, 20f);
            var expected = Quaternion.AngleAxis(20f, Vector3.forward) * Quaternion.Euler(0f, 0f, 15f);
            Assert.That(Quaternion.Angle(expected, _adjuster.transform.localRotation), Is.LessThan(0.01f));
        }

        [Test]
        public void Set_ReportsChangesAndLimits()
        {
            var changes = 0;
            var axisChanges = new List<PlateAxis>();
            var limits = new List<PlateAxis>();
            _adjuster.Changed += () => changes++;
            _adjuster.AxisChanged += axisChanges.Add;
            _adjuster.LimitReached += limits.Add;

            _adjuster.Set(PlateAxis.TurnX, 5f);
            _adjuster.Set(PlateAxis.TurnX, 5f);
            _adjuster.Set(PlateAxis.MoveX, 99f);
            _adjuster.Set(PlateAxis.MoveX, 99f);

            Assert.AreEqual(2, changes, "Setting the same value again changes nothing.");
            CollectionAssert.AreEqual(new[] { PlateAxis.TurnX, PlateAxis.MoveX }, axisChanges);
            CollectionAssert.AreEqual(new[] { PlateAxis.MoveX, PlateAxis.MoveX }, limits, "Pushing against a limit reports it every time.");
        }

        [Test]
        public void IsPlaced_OnlyWhenEveryAxisIsWithinTolerance()
        {
            Assert.IsFalse(_adjuster.IsPlaced);
            MoveToTarget();
            Assert.IsTrue(_adjuster.IsPlaced);

            var along = _adjuster.Settings(PlateAxis.MoveY).target;
            _adjuster.Set(PlateAxis.MoveY, along + _adjuster.PositionToleranceMm * 0.9f);
            Assert.IsTrue(_adjuster.IsPlaced, "Just inside the tolerance still counts.");
            _adjuster.Set(PlateAxis.MoveY, along + _adjuster.PositionToleranceMm * 1.5f);
            Assert.IsFalse(_adjuster.IsPlaced);
        }

        [Test]
        public void PlacedChanged_FiresOnceOnArrivingAndOnceOnLeaving()
        {
            var events = new List<bool>();
            _adjuster.PlacedChanged += events.Add;
            MoveToTarget();
            _adjuster.Nudge(PlateAxis.TurnY, 0.5f);
            _adjuster.Nudge(PlateAxis.TurnY, 5f);
            CollectionAssert.AreEqual(new[] { true, false }, events);
        }

        [Test]
        public void AtTheTargetOffsets_ThePlateSitsOnTheTargetPose()
        {
            MoveToTarget();
            var (position, rotation) = _adjuster.TargetPose;
            Assert.That(Vector3.Distance(position, _adjuster.transform.localPosition), Is.LessThan(1e-6f));
            Assert.That(Quaternion.Angle(rotation, _adjuster.transform.localRotation), Is.LessThan(0.01f));
        }

        [Test]
        public void ResetAll_PutsThePlateBackWhereItStarted()
        {
            var (startPosition, startRotation) = _adjuster.StartPose;
            MoveToTarget();
            var events = new List<bool>();
            _adjuster.PlacedChanged += events.Add;
            _adjuster.ResetAll();
            foreach (var axis in AllAxes()) Assert.AreEqual(0f, _adjuster.Get(axis), axis.ToString());
            Assert.That(Vector3.Distance(startPosition, _adjuster.transform.localPosition), Is.LessThan(1e-6f));
            Assert.That(Quaternion.Angle(startRotation, _adjuster.transform.localRotation), Is.LessThan(0.01f));
            CollectionAssert.AreEqual(new[] { false }, events);
        }

        [Test]
        public void AsAnAdjustable_ItShowsMillimetresAndDegrees()
        {
            IAxisAdjustable adjustable = _adjuster;
            Assert.AreEqual(6, adjustable.AxisCount);
            Assert.AreEqual("mm", adjustable.AxisUnit((int)PlateAxis.MoveZ));
            Assert.AreEqual(AxisFormat.Degrees, adjustable.AxisUnit((int)PlateAxis.TurnX));
            Assert.AreEqual(_adjuster.Settings(PlateAxis.MoveX).max, adjustable.SetAxis((int)PlateAxis.MoveX, 1000f));
        }
    }

    public class PlateFeedbackTests
    {
        GameObject _root;
        PlateAdjuster _adjuster;
        StatusLine _status;
        Banner _banner;
        PlateTargetGhost _ghost;

        [SetUp]
        public void Build()
        {
            _root = new GameObject("Frame");
            var plate = new GameObject("Plate");
            plate.transform.SetParent(_root.transform, false);
            _adjuster = plate.AddComponent<PlateAdjuster>();
            _status = _root.AddComponent<StatusLine>();
            _banner = _root.AddComponent<Banner>();
            _root.AddComponent<PlateFeedback>().Connect(_adjuster, _status, _banner);
            var ghost = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ghost.transform.SetParent(_root.transform, false);
            _ghost = ghost.AddComponent<PlateTargetGhost>();
            _ghost.Connect(_adjuster);
        }

        [TearDown]
        public void Destroy() => Object.DestroyImmediate(_root);

        void MoveToTarget()
        {
            for (var i = 0; i < PlateAxes.Count; i++) _adjuster.Set((PlateAxis)i, _adjuster.Settings((PlateAxis)i).target);
        }

        [Test]
        public void Ghost_SitsWhereThePlateBelongs()
        {
            var (position, rotation) = _adjuster.TargetPose;
            Assert.That(Vector3.Distance(position, _ghost.transform.localPosition), Is.LessThan(1e-6f));
            Assert.That(Quaternion.Angle(rotation, _ghost.transform.localRotation), Is.LessThan(0.01f));
        }

        [Test]
        public void Ghost_TurnsGreenWhenThePlateArrives()
        {
            Assert.AreEqual(_ghost.WaitingColour, _ghost.CurrentColour);
            MoveToTarget();
            Assert.AreEqual(_ghost.PlacedColour, _ghost.CurrentColour);
            _adjuster.ResetAll();
            Assert.AreEqual(_ghost.WaitingColour, _ghost.CurrentColour);
        }

        [Test]
        public void Status_SaysInPlace_ThenOutOfPlace()
        {
            MoveToTarget();
            StringAssert.StartsWith("In place", _status.Message);
            _adjuster.ResetAll();
            Assert.AreEqual(PlateMessages.LeftPlace, _status.Message);
        }

        [Test]
        public void Banner_AnnouncesTheFinishedStep_AndGoesWhenThePlateLeaves()
        {
            Assert.IsFalse(_banner.IsShowing);
            MoveToTarget();
            Assert.IsTrue(_banner.IsShowing);
            Assert.AreEqual(PlateMessages.PlacedTitle, _banner.Title);
            StringAssert.Contains("screws", _banner.Subtitle, "It says what comes next.");
            _adjuster.Nudge(PlateAxis.MoveY, 5f);
            Assert.IsFalse(_banner.IsShowing);
        }

        [Test]
        public void Status_NamesTheAxisAtItsLimit()
        {
            _adjuster.Set(PlateAxis.TurnZ, 1000f);
            Assert.AreEqual("Swivel (Z) stops at its limit, +30.0°.", _status.Message);
        }
    }
}
