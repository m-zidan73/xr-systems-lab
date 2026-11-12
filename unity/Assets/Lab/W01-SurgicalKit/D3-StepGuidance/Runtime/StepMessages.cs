using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Lab.W01.ColourCodedHoles;
using Lab.W01.VariableScrew;

namespace Lab.W01.StepGuidance
{
    /// <summary>Plain-language text for the step panel, the mistakes and the end of the procedure.</summary>
    public static class StepMessages
    {
        public const string DoneTitle = "All steps done";
        const int MistakesListed = 3;

        static string Mm(float value) => value.ToString("0.#", CultureInfo.InvariantCulture);

        public static string Instruction(TrainingStep step)
        {
            if (step == null) return "";
            if (!string.IsNullOrWhiteSpace(step.instruction)) return step.instruction.Trim();
            if (step.finishesOn == StepFinish.Confirmed) return "Confirm to continue.";
            if (step.hole == null) return "This step has no hole set.";
            var hole = Mm(step.hole.DiameterMm);
            return step.screw != null && step.screw.Spec != null
                ? $"Put the Ø {Mm(step.screw.Spec.diameterMm)} mm screw in the {hole} mm hole."
                : $"Put a screw in the {hole} mm hole.";
        }

        public static string Header(int currentIndex, int stepCount) =>
            currentIndex >= stepCount ? DoneTitle : $"Step {currentIndex + 1} of {stepCount}";

        public static string MistakeCount(int count) => $"Mistakes: {count}";

        public static string Name(MistakeKind kind)
        {
            switch (kind)
            {
                case MistakeKind.WrongSize: return "wrong size";
                case MistakeKind.WrongHole: return "wrong hole";
                case MistakeKind.TooEarly: return "too early";
                case MistakeKind.HoleTaken: return "hole already used";
                default: return "size not in the catalog";
            }
        }

        /// <summary>The sentence that names a mistake, written when it happens so later catalog changes can't alter it.</summary>
        public static string Describe(MistakeKind kind, int stepIndex, TrainingStep step, Hole hole, Screw screw)
        {
            var screwName = screw != null && screw.Spec != null ? $"the {screw.Label} screw" : "that screw";
            var holeMm = hole != null ? Mm(hole.DiameterMm) : "?";
            switch (kind)
            {
                case MistakeKind.WrongSize:
                    return $"Wrong size: {screwName} doesn't fit the {holeMm} mm hole.";
                case MistakeKind.WrongHole:
                    var stepHole = step != null && step.hole != null ? $"the {Mm(step.hole.DiameterMm)} mm hole" : "another hole";
                    return $"Wrong hole: step {stepIndex + 1} uses {stepHole}, so {screwName} goes back to the tray.";
                case MistakeKind.TooEarly:
                    return $"Too early: finish step {stepIndex + 1} first, so {screwName} goes back to the tray.";
                case MistakeKind.HoleTaken:
                    return $"Hole already used: the {holeMm} mm hole already holds a screw.";
                default:
                    var maker = hole != null && hole.Catalog != null ? hole.Catalog.manufacturer : "This catalog";
                    return $"Size not in the catalog: {maker} has no {holeMm} mm screw.";
            }
        }

        public static string Summary(IReadOnlyList<Mistake> mistakes)
        {
            if (mistakes == null || mistakes.Count == 0) return "No mistakes.";
            var list = string.Join(", ", mistakes.Take(MistakesListed).Select(m => $"{Name(m.Kind)} in step {m.Step + 1}"));
            if (mistakes.Count > MistakesListed) list += $" and {mistakes.Count - MistakesListed} more";
            return $"{mistakes.Count} mistake{(mistakes.Count == 1 ? "" : "s")}: {list}.";
        }

        public static string StepDone(int stepIndex) => $"Step {stepIndex + 1} done.";

        public static string Reopened(int stepIndex, TrainingStep step) =>
            step != null && step.hole != null
                ? $"Step {stepIndex + 1} is open again: the {Mm(step.hole.DiameterMm)} mm hole is empty."
                : $"Step {stepIndex + 1} is open again.";

        public const string StartedAgain = "Started again from step 1.";
    }
}
