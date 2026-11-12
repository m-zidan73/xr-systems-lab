using NUnit.Framework;
using static Lab.W01.StepGuidance.Tests.StepTestScene;

namespace Lab.W01.StepGuidance.Tests
{
    public class StepMessagesTests
    {
        StepTestScene _s;

        [SetUp]
        public void Build() => _s = new StepTestScene();

        [TearDown]
        public void Destroy() => _s.Dispose();

        [Test]
        public void Instruction_UsesTheWrittenText()
        {
            Assert.AreEqual("Fix the end first.", StepMessages.Instruction(Seat(_s.Hole35, _s.Screw35, "  Fix the end first. ")));
        }

        [Test]
        public void Instruction_WithoutText_SaysWhichScrewGoesInWhichHole()
        {
            Assert.AreEqual("Put the Ø 3.5 mm screw in the 3.5 mm hole.", StepMessages.Instruction(Seat(_s.Hole35, _s.Screw35)));
            Assert.AreEqual("Put a screw in the 2.7 mm hole.", StepMessages.Instruction(Seat(_s.Hole27, null)));
            Assert.AreEqual("Confirm to continue.", StepMessages.Instruction(Check("")));
        }

        [Test]
        public void Header_CountsSteps_ThenSaysDone()
        {
            Assert.AreEqual("Step 2 of 5", StepMessages.Header(1, 5));
            Assert.AreEqual("All steps done", StepMessages.Header(5, 5));
            Assert.AreEqual(StepMessages.DoneTitle, StepMessages.Header(5, 5));
        }

        [Test]
        public void MistakeCount_ReadsNaturally()
        {
            Assert.AreEqual("Mistakes: 0", StepMessages.MistakeCount(0));
            Assert.AreEqual("Mistakes: 3", StepMessages.MistakeCount(3));
        }

        [Test]
        public void Describe_NamesEachKindOfMistake()
        {
            var step = Seat(_s.Hole35, _s.Screw35);
            Assert.AreEqual("Wrong size: the Ø 4 mm × 20 mm screw doesn't fit the 3.5 mm hole.",
                StepMessages.Describe(MistakeKind.WrongSize, 0, step, _s.Hole35, _s.Screw40));
            Assert.AreEqual("Wrong hole: step 1 uses the 3.5 mm hole, so the Ø 2.7 mm × 20 mm screw goes back to the tray.",
                StepMessages.Describe(MistakeKind.WrongHole, 0, step, _s.Hole27, _s.Screw27));
            Assert.AreEqual("Too early: finish step 2 first, so the Ø 3.5 mm × 20 mm screw goes back to the tray.",
                StepMessages.Describe(MistakeKind.TooEarly, 1, Check("Look"), _s.Hole35, _s.Screw35));
            Assert.AreEqual("Hole already used: the 3.5 mm hole already holds a screw.",
                StepMessages.Describe(MistakeKind.HoleTaken, 0, step, _s.Hole35, _s.Screw35));
            Assert.AreEqual("Size not in the catalog: Test Ortho has no 2.7 mm screw.",
                StepMessages.Describe(MistakeKind.NotInCatalog, 0, step, _s.Hole27, _s.Screw27));
        }

        [Test]
        public void Summary_ListsTheMistakes_WithTheirSteps()
        {
            Assert.AreEqual("No mistakes.", StepMessages.Summary(new Mistake[0]));
            Assert.AreEqual("1 mistake: wrong size in step 2.",
                StepMessages.Summary(new[] { new Mistake(1, MistakeKind.WrongSize, "") }));
            Assert.AreEqual("5 mistakes: wrong size in step 1, wrong hole in step 2, too early in step 1 and 2 more.",
                StepMessages.Summary(new[]
                {
                    new Mistake(0, MistakeKind.WrongSize, ""),
                    new Mistake(1, MistakeKind.WrongHole, ""),
                    new Mistake(0, MistakeKind.TooEarly, ""),
                    new Mistake(2, MistakeKind.HoleTaken, ""),
                    new Mistake(3, MistakeKind.NotInCatalog, ""),
                }));
        }

        [Test]
        public void Reopened_NamesTheEmptyHole()
        {
            Assert.AreEqual("Step 1 is open again: the 3.5 mm hole is empty.",
                StepMessages.Reopened(0, Seat(_s.Hole35, _s.Screw35)));
        }
    }
}
