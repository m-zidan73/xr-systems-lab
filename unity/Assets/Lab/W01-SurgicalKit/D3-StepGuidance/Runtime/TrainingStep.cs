using System;
using Lab.W01.ColourCodedHoles;
using Lab.W01.VariableScrew;
using UnityEngine;

namespace Lab.W01.StepGuidance
{
    /// <summary>The event that finishes a step.</summary>
    public enum StepFinish
    {
        /// <summary>The step's hole takes a screw. The hole's own size rule decides which screws fit.</summary>
        ScrewSeated,
        /// <summary>The trainee confirms, for steps that are a check rather than an action.</summary>
        Confirmed,
    }

    /// <summary>
    /// One entry in a procedure: what to do, what to highlight, and which event finishes it. A new procedure
    /// is a new list of these in the Inspector, not new code.
    /// </summary>
    [Serializable]
    public class TrainingStep
    {
        [Tooltip("What the trainee reads. Left empty, a screw step says which screw goes in which hole.")]
        [TextArea(1, 3)] public string instruction = "";
        public StepFinish finishesOn = StepFinish.ScrewSeated;
        [Tooltip("The hole to use, ringed while the step is open. A screw step finishes when this hole takes a screw.")]
        public Hole hole;
        [Tooltip("The tray screw to use, ringed while the step is open. Only a pointer: the hole decides what fits.")]
        public Screw screw;
    }
}
