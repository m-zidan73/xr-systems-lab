using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static Lab.W01.StepGuidance.Tests.StepTestScene;

namespace Lab.W01.StepGuidance.Tests
{
    public class StepGuideTests
    {
        StepTestScene _s;

        [SetUp]
        public void Build() => _s = new StepTestScene();

        [TearDown]
        public void Destroy() => _s.Dispose();

        static float Moved(Vector3 from, Component to) => Vector3.Distance(from, to.transform.position);

        [Test]
        public void StartsAtTheFirstStep_WithNoMistakes()
        {
            var guide = _s.Guide(Seat(_s.Hole35, _s.Screw35), Seat(_s.Hole27, _s.Screw27));
            Assert.AreEqual(0, guide.CurrentIndex);
            Assert.AreEqual(2, guide.StepCount);
            Assert.IsFalse(guide.IsDone);
            Assert.AreSame(_s.Hole35, guide.Current.hole);
            Assert.AreEqual(0, guide.Mistakes.Count);
        }

        [Test]
        public void SeatingAScrewInTheStepsHole_FinishesTheStep()
        {
            var guide = _s.Guide(Seat(_s.Hole35, _s.Screw35), Seat(_s.Hole27, _s.Screw27));
            var changes = new List<(int, int)>();
            guide.StepChanged += (from, to) => changes.Add((from, to));

            _s.Hole35.TryAccept(_s.Screw35);

            Assert.AreEqual(1, guide.CurrentIndex);
            CollectionAssert.AreEqual(new[] { (0, 1) }, changes);
            Assert.AreSame(_s.Screw35, _s.Hole35.Occupant, "The right screw stays in.");
            Assert.AreEqual(0, guide.Mistakes.Count);
        }

        [Test]
        public void Confirm_OnlyFinishesAStepThatWaitsForIt()
        {
            var guide = _s.Guide(Seat(_s.Hole35, _s.Screw35), Check("Look"), Seat(_s.Hole27, _s.Screw27));
            guide.Confirm();
            Assert.AreEqual(0, guide.CurrentIndex, "A step that waits for a screw ignores Confirm.");

            _s.Hole35.TryAccept(_s.Screw35);
            guide.Confirm();
            Assert.AreEqual(2, guide.CurrentIndex);
        }

        [Test]
        public void AWrongSizeScrew_IsAMistake_NamingTheScrewAndTheHole()
        {
            var guide = _s.Guide(Seat(_s.Hole35, _s.Screw35));
            var raised = new List<Mistake>();
            guide.MistakeMade += raised.Add;

            _s.Hole35.TryAccept(_s.Screw40);

            var mistake = guide.Mistakes.Single();
            Assert.AreEqual(MistakeKind.WrongSize, mistake.Kind);
            Assert.AreEqual(0, mistake.Step);
            StringAssert.Contains("Ø 4 mm", mistake.Detail);
            StringAssert.Contains("3.5 mm hole", mistake.Detail);
            CollectionAssert.AreEqual(guide.Mistakes, raised);
            Assert.AreEqual(0, guide.CurrentIndex);
        }

        [Test]
        public void TheRightSizeInTheWrongHole_IsAMistake_AndTheScrewGoesBack()
        {
            var guide = _s.Guide(Seat(_s.Hole35, _s.Screw35), Seat(_s.Hole27, _s.Screw27));
            var home = _s.Screw27.transform.position;

            _s.Hole27.TryAccept(_s.Screw27);

            var mistake = guide.Mistakes.Single();
            Assert.AreEqual(MistakeKind.WrongHole, mistake.Kind);
            StringAssert.Contains("3.5 mm hole", mistake.Detail, "It names the hole the step uses.");
            Assert.IsNull(_s.Hole27.Occupant);
            Assert.That(Moved(home, _s.Screw27), Is.LessThan(1e-6f));
            Assert.AreEqual(0, guide.CurrentIndex);
        }

        [Test]
        public void AScrewInAHoleNoStepUses_IsAWrongHoleMistake()
        {
            var guide = _s.Guide(Seat(_s.Hole35, _s.Screw35));
            _s.Hole40.TryAccept(_s.Screw40);
            Assert.AreEqual(MistakeKind.WrongHole, guide.Mistakes.Single().Kind);
            Assert.IsNull(_s.Hole40.Occupant);
        }

        [Test]
        public void SeatingAScrewDuringACheckStep_IsTooEarly()
        {
            var guide = _s.Guide(Check("Look"), Seat(_s.Hole35, _s.Screw35));
            _s.Hole35.TryAccept(_s.Screw35);
            Assert.AreEqual(MistakeKind.TooEarly, guide.Mistakes.Single().Kind);
            Assert.IsNull(_s.Hole35.Occupant);
            Assert.AreEqual(0, guide.CurrentIndex);
        }

        [Test]
        public void DroppingOnAFilledHole_IsAMistake()
        {
            var guide = _s.Guide(Seat(_s.Hole35, _s.Screw35), Seat(_s.Hole27, _s.Screw27));
            _s.Hole35.TryAccept(_s.Screw35);
            var spare = _s.Grabbable(_s.Spec35, new Vector3(0.04f, 0f, -0.08f));

            _s.Hole35.TryAccept(spare);

            var mistake = guide.Mistakes.Single();
            Assert.AreEqual(MistakeKind.HoleTaken, mistake.Kind);
            Assert.AreEqual(1, mistake.Step);
        }

        [Test]
        public void AHoleTheCatalogHasNoScrewFor_IsAMistake()
        {
            var guide = _s.Guide(Seat(_s.Hole35, _s.Screw35));
            _s.Catalog.screws = new[] { _s.Spec35 };
            _s.Hole27.SetCatalog(_s.Catalog);

            _s.Hole27.TryAccept(_s.Screw27);

            var mistake = guide.Mistakes.Single();
            Assert.AreEqual(MistakeKind.NotInCatalog, mistake.Kind);
            StringAssert.Contains("Test Ortho", mistake.Detail);
        }

        [Test]
        public void FinishingEveryStep_RaisesFinishedOnce()
        {
            var guide = _s.Guide(Seat(_s.Hole35, _s.Screw35), Check("Look"));
            var finished = 0;
            guide.Finished += () => finished++;

            _s.Hole35.TryAccept(_s.Screw35);
            guide.Confirm();
            guide.Confirm();

            Assert.IsTrue(guide.IsDone);
            Assert.AreEqual(2, guide.CurrentIndex);
            Assert.IsNull(guide.Current);
            Assert.AreEqual(1, finished);
        }

        [Test]
        public void EmptyingAFinishedStepsHole_OpensThatStepAgain_WithoutAMistake()
        {
            var guide = _s.Guide(Seat(_s.Hole35, _s.Screw35), Seat(_s.Hole27, _s.Screw27), Seat(_s.Hole40, _s.Screw40));
            _s.Hole35.TryAccept(_s.Screw35);
            _s.Hole27.TryAccept(_s.Screw27);
            var changes = new List<(int, int)>();
            guide.StepChanged += (from, to) => changes.Add((from, to));

            _s.Hole35.Vacate();
            Assert.AreEqual(0, guide.CurrentIndex);
            Assert.AreEqual(0, guide.Mistakes.Count);

            _s.Hole35.TryAccept(_s.Screw35);
            Assert.AreEqual(2, guide.CurrentIndex, "Step 2's screw is still in, so the guide moves straight past it.");
            CollectionAssert.AreEqual(new[] { (2, 0), (0, 2) }, changes);
        }

        [Test]
        public void Restart_EmptiesTheHoles_SendsTheScrewsHome_AndClearsMistakes()
        {
            var guide = _s.Guide(Check("Look"), Seat(_s.Hole35, _s.Screw35));
            var home = _s.Screw35.transform.position;
            guide.Confirm();
            _s.Hole35.TryAccept(_s.Screw40);
            _s.Hole35.TryAccept(_s.Screw35);
            Assert.IsTrue(guide.IsDone);
            var restarted = 0;
            guide.Restarted += () => restarted++;

            guide.Restart();

            Assert.AreEqual(0, guide.CurrentIndex);
            Assert.AreEqual(0, guide.Mistakes.Count);
            Assert.IsNull(_s.Hole35.Occupant);
            Assert.That(Moved(home, _s.Screw35), Is.LessThan(1e-6f));
            Assert.AreEqual(1, restarted);
            guide.Confirm();
            Assert.AreEqual(1, guide.CurrentIndex, "Check steps have to be confirmed again.");
        }

        [Test]
        public void OnceFinished_TheResultStands()
        {
            var guide = _s.Guide(Seat(_s.Hole35, _s.Screw35));
            _s.Hole35.TryAccept(_s.Screw35);

            _s.Hole27.TryAccept(_s.Screw40);
            _s.Hole40.TryAccept(_s.Screw40);
            _s.Hole35.Vacate();

            Assert.IsTrue(guide.IsDone);
            Assert.AreEqual(0, guide.Mistakes.Count);
            Assert.AreSame(_s.Screw40, _s.Hole40.Occupant, "Nothing is taken back out once the procedure is over.");
        }

        [Test]
        public void CurrentStepMistake_IsTheLatestOnThisStep_AndClearsWhenTheStepMoves()
        {
            var guide = _s.Guide(Seat(_s.Hole35, _s.Screw35), Seat(_s.Hole27, _s.Screw27));
            Assert.IsNull(guide.CurrentStepMistake);

            _s.Hole35.TryAccept(_s.Screw40);
            _s.Hole35.TryAccept(_s.Screw27);
            Assert.AreEqual(MistakeKind.WrongSize, guide.CurrentStepMistake?.Kind);
            StringAssert.Contains("Ø 2.7 mm", guide.CurrentStepMistake?.Detail);

            _s.Hole35.TryAccept(_s.Screw35);
            Assert.IsNull(guide.CurrentStepMistake);
            Assert.AreEqual(2, guide.Mistakes.Count, "The count keeps every mistake.");
        }

        [Test]
        public void IsFinished_ReportsEachStep()
        {
            var guide = _s.Guide(Seat(_s.Hole35, _s.Screw35), Check("Look"));
            _s.Hole35.TryAccept(_s.Screw35);
            Assert.IsTrue(guide.IsFinished(0));
            Assert.IsFalse(guide.IsFinished(1));
            Assert.IsFalse(guide.IsFinished(-1));
            Assert.IsFalse(guide.IsFinished(2));
        }

        [Test]
        public void AScrewStepWithNoHole_IsSkippedWithAWarning()
        {
            LogAssert.Expect(LogType.Warning, new Regex("Step 1 waits for a screw but has no hole"));
            var guide = _s.Guide(Seat(null, _s.Screw35), Seat(_s.Hole27, _s.Screw27));
            Assert.AreEqual(1, guide.CurrentIndex);
        }

        [Test]
        public void NoSteps_IsDoneStraightAway()
        {
            var guide = _s.Guide();
            Assert.IsTrue(guide.IsDone);
            Assert.AreEqual(0, guide.StepCount);
        }
    }
}
