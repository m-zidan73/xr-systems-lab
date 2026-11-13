using System.Globalization;

namespace Lab.W01.HoleTools
{
    /// <summary>Every sentence the hole tools can say, in one place.</summary>
    public static class ToolMessages
    {
        public const string MeasureFirst = "Drill this hole before measuring it.";
        public const string NoBone = "No bone under this hole. Move the plate so the hole sits over the bone.";
        public const string ReadingLost = "The plate moved, so the depth no longer holds. Measure the hole again.";
        public const string Cleared = "Every hole is untouched again.";

        static string Mm(float value) => value.ToString("0.#", CultureInfo.InvariantCulture);
        static string Depth(float value) => value.ToString("0.0", CultureInfo.InvariantCulture);

        public static string HoleLabel(float diameterMm, bool measured, float depthMm) =>
            measured ? $"Ø {Mm(diameterMm)} · {Depth(depthMm)} mm" : $"Ø {Mm(diameterMm)}";

        public static string Drilled(float diameterMm, float bitMm, string maker) =>
            $"Drilled the Ø {Mm(diameterMm)} hole with the {Mm(bitMm)} mm bit, the size {maker} gives for that screw.";

        public static string AlreadyDrilled(float diameterMm) => $"The Ø {Mm(diameterMm)} hole is already drilled.";

        public static string NotInCatalog(float diameterMm, string maker) =>
            $"{maker} has no {Mm(diameterMm)} mm screw, so there is no bit for this hole.";

        public static string Measured(float diameterMm, float depthMm) =>
            $"Depth {Depth(depthMm)} mm at the Ø {Mm(diameterMm)} hole, from the plate to the far side of the bone.";
    }
}
