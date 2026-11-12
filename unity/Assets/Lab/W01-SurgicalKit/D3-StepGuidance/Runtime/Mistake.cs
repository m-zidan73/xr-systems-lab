namespace Lab.W01.StepGuidance
{
    public enum MistakeKind
    {
        /// <summary>The hole refused the screw because the diameters differ.</summary>
        WrongSize,
        /// <summary>The screw fitted, but in a hole the current step doesn't use.</summary>
        WrongHole,
        /// <summary>A screw went in while the current step is a check.</summary>
        TooEarly,
        /// <summary>The hole already holds a screw.</summary>
        HoleTaken,
        /// <summary>The catalog has no screw for that hole.</summary>
        NotInCatalog,
    }

    /// <summary>One mistake: the step it happened on (counted from 0), its kind, and a sentence that names it.</summary>
    public readonly struct Mistake
    {
        public Mistake(int step, MistakeKind kind, string detail)
        {
            Step = step;
            Kind = kind;
            Detail = detail;
        }

        public int Step { get; }
        public MistakeKind Kind { get; }
        public string Detail { get; }
    }
}
