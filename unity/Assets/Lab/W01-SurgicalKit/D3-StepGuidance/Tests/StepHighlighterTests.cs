using Lab.Core;
using Lab.W01.VariableScrew;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using static Lab.W01.StepGuidance.Tests.StepTestScene;

namespace Lab.W01.StepGuidance.Tests
{
    public class StepHighlighterTests
    {
        StepTestScene _s;
        StepGuide _guide;
        StepHighlighter _highlighter;
        Renderer _holeMarker;
        Renderer _screwMarker;

        [SetUp]
        public void Build()
        {
            _s = new StepTestScene();
            _guide = _s.Guide(Check("Look"), Seat(_s.Hole35, _s.Screw35), Seat(_s.Hole27, null));
            _holeMarker = Marker("Hole Marker");
            _screwMarker = Marker("Screw Marker");
            _highlighter = _s.Track(new GameObject("Highlighter")).AddComponent<StepHighlighter>();
            var wiring = new SerializedObject(_highlighter);
            wiring.FindProperty("holeMarker").objectReferenceValue = _holeMarker;
            wiring.FindProperty("screwMarker").objectReferenceValue = _screwMarker;
            wiring.ApplyModifiedPropertiesWithoutUndo();
            _highlighter.Connect(_guide);
        }

        [TearDown]
        public void Destroy() => _s.Dispose();

        Renderer Marker(string name)
        {
            var go = _s.Track(new GameObject(name));
            go.AddComponent<MeshFilter>();
            return go.AddComponent<MeshRenderer>();
        }

        [Test]
        public void ACheckStep_MarksNothing()
        {
            Assert.IsFalse(_holeMarker.enabled);
            Assert.IsFalse(_screwMarker.enabled);
            Assert.IsNull(_highlighter.TargetHole);
        }

        [Test]
        public void AScrewStep_RingsItsHoleAndItsScrew()
        {
            _guide.Confirm();

            Assert.AreSame(_s.Hole35, _highlighter.TargetHole);
            Assert.AreSame(_s.Screw35, _highlighter.TargetScrew);
            Assert.IsTrue(_holeMarker.enabled);
            Assert.IsTrue(_screwMarker.enabled);

            var hole = _s.Hole35.transform.position;
            var marker = _holeMarker.transform.position;
            Assert.AreEqual(hole.x, marker.x, 1e-6f);
            Assert.AreEqual(hole.z, marker.z, 1e-6f);
            Assert.That(marker.y, Is.GreaterThan(hole.y), "It sits just above the hole's colour ring.");
            Assert.That(_holeMarker.transform.localScale.x, Is.GreaterThan(_s.Hole35.DiameterMm * 0.0005f + 0.0015f),
                "It rings the hole outside its colour ring.");

            var middle = (_s.Screw35.transform.position + _s.Screw35.TipPosition) * 0.5f;
            Assert.That(Vector3.Distance(middle, _screwMarker.transform.position), Is.LessThan(1e-6f));
            Assert.That(_screwMarker.transform.localScale.x, Is.GreaterThan(HeadRadius(_s.Screw35)), "It clears the screw's head.");
        }

        static float HeadRadius(Screw screw) => ScrewUnits.MmWorld(ScrewUnits.HeadDiameterMm(screw.Spec.diameterMm)) * 0.5f;

        [Test]
        public void TheScrewRing_IsAnOvalAlongALyingScrew_AndACircleRoundAnUprightOne()
        {
            var tip = new GameObject("Tip").transform;
            tip.SetParent(_s.Screw35.transform, false);
            var wiring = new SerializedObject(_s.Screw35);
            wiring.FindProperty("tipPoint").objectReferenceValue = tip;
            wiring.ApplyModifiedPropertiesWithoutUndo();
            _s.Screw35.Set(_s.Spec35, 20f);
            _guide.Confirm();

            var scale = _screwMarker.transform.localScale;
            Assert.That(scale.z, Is.GreaterThan(0.01f), "Longer than half the 20 mm screw.");
            Assert.That(scale.x, Is.GreaterThan(HeadRadius(_s.Screw35)));
            Assert.That(scale.x, Is.LessThan(scale.z * 0.6f), "Narrow across, so it stays off the neighbours.");
            Assert.That(Vector3.Angle(_s.Screw35.transform.forward, _screwMarker.transform.forward), Is.LessThan(0.01f));

            _s.Screw35.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
            _highlighter.Follow();
            scale = _screwMarker.transform.localScale;
            Assert.AreEqual(scale.x, scale.z, 1e-6f);
        }

        [Test]
        public void TheScrewRing_FollowsTheScrew()
        {
            _guide.Confirm();
            _s.Screw35.GetComponent<Grabbable>().MoveTo(new Vector3(0.05f, 0.03f, 0f));
            _highlighter.Follow();
            var middle = (_s.Screw35.transform.position + _s.Screw35.TipPosition) * 0.5f;
            Assert.That(Vector3.Distance(middle, _screwMarker.transform.position), Is.LessThan(1e-6f));
        }

        [Test]
        public void AStepWithoutAScrew_RingsOnlyTheHole()
        {
            _guide.Confirm();
            _s.Hole35.TryAccept(_s.Screw35);
            Assert.AreSame(_s.Hole27, _highlighter.TargetHole);
            Assert.IsTrue(_holeMarker.enabled);
            Assert.IsFalse(_screwMarker.enabled);
        }

        [Test]
        public void WhenEveryStepIsDone_NothingIsMarked_UntilARestart()
        {
            _guide.Confirm();
            _s.Hole35.TryAccept(_s.Screw35);
            _s.Hole27.TryAccept(_s.Screw27);
            Assert.IsTrue(_guide.IsDone);
            Assert.IsFalse(_holeMarker.enabled);
            Assert.IsFalse(_screwMarker.enabled);

            _guide.Restart();
            _guide.Confirm();
            Assert.AreSame(_s.Hole35, _highlighter.TargetHole);
            Assert.IsTrue(_holeMarker.enabled);
        }
    }

    public class StepFeedbackTests
    {
        StepTestScene _s;
        StepGuide _guide;
        StatusLine _status;
        Banner _banner;

        [SetUp]
        public void Build()
        {
            _s = new StepTestScene();
            _guide = _s.Guide(Seat(_s.Hole35, _s.Screw35), Seat(_s.Hole27, _s.Screw27));
            var go = _s.Track(new GameObject("Feedback"));
            _status = go.AddComponent<StatusLine>();
            _banner = go.AddComponent<Banner>();
            var feedback = go.AddComponent<StepFeedback>();
            var wiring = new SerializedObject(feedback);
            wiring.FindProperty("afterDone").stringValue = "Press R to start again.";
            wiring.ApplyModifiedPropertiesWithoutUndo();
            feedback.Connect(_guide, _status, _banner);
        }

        [TearDown]
        public void Destroy() => _s.Dispose();

        [Test]
        public void FinishingTheLastStep_ShowsAllStepsDone_WithTheMistakeCount()
        {
            _s.Hole35.TryAccept(_s.Screw40);
            _s.Hole35.TryAccept(_s.Screw35);
            Assert.IsFalse(_banner.IsShowing);
            StringAssert.Contains("Step 1 done", _status.Message);

            _s.Hole27.TryAccept(_s.Screw27);

            Assert.IsTrue(_banner.IsShowing);
            Assert.AreEqual("All steps done", _banner.Title);
            StringAssert.StartsWith("1 mistake: wrong size in step 1.", _banner.Subtitle);
            StringAssert.EndsWith("Press R to start again.", _banner.Subtitle);
        }

        [Test]
        public void Restart_HidesTheBanner()
        {
            _s.Hole35.TryAccept(_s.Screw35);
            _s.Hole27.TryAccept(_s.Screw27);
            _guide.Restart();
            Assert.IsFalse(_banner.IsShowing);
            StringAssert.Contains("step 1", _status.Message);
        }

        [Test]
        public void EmptyingAFinishedHole_SaysWhichStepIsOpenAgain()
        {
            _s.Hole35.TryAccept(_s.Screw35);
            _s.Hole35.Vacate();
            Assert.AreEqual("Step 1 is open again: the 3.5 mm hole is empty.", _status.Message);
        }
    }
}
