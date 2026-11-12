using System.Collections.Generic;
using Lab.W01.ColourCodedHoles;
using Lab.W01.ColourCodedHoles.Tests;
using Lab.W01.VariableScrew;
using UnityEngine;

namespace Lab.W01.StepGuidance.Tests
{
    /// <summary>
    /// Three holes (2.7, 3.5 and 4 mm) and a grabbable screw for each, from one catalog, plus a guide to
    /// drive. Built on day 1's test kit; everything it makes is destroyed by Dispose.
    /// </summary>
    public sealed class StepTestScene
    {
        readonly HoleTestKit _kit = new HoleTestKit();
        readonly List<Object> _made = new List<Object>();

        public readonly ScrewSpec Spec35;
        public readonly ImplantCatalog Catalog;
        public readonly Hole Hole27, Hole35, Hole40;
        public readonly Screw Screw27, Screw35, Screw40;

        public StepTestScene()
        {
            var spec27 = _kit.Spec(2.7f, Color.yellow);
            Spec35 = _kit.Spec(3.5f, Color.blue);
            var spec40 = _kit.Spec(4f, Color.green);
            Catalog = _kit.Catalog("Test Ortho", spec27, Spec35, spec40);

            Hole27 = _kit.Hole(2.7f, Catalog, new Vector3(-0.015f, 0f, 0f));
            Hole35 = _kit.Hole(3.5f, Catalog, Vector3.zero);
            Hole40 = _kit.Hole(4f, Catalog, new Vector3(0.015f, 0f, 0f));

            Screw27 = Grabbable(spec27, new Vector3(-0.02f, 0f, -0.08f));
            Screw35 = Grabbable(Spec35, new Vector3(0f, 0f, -0.08f));
            Screw40 = Grabbable(spec40, new Vector3(0.02f, 0f, -0.08f));
        }

        public IReadOnlyList<Hole> AllHoles => new[] { Hole27, Hole35, Hole40 };

        public Screw Grabbable(ScrewSpec spec, Vector3 position)
        {
            var screw = _kit.Screw(spec, position);
            _kit.Grabbable(screw);
            return screw;
        }

        public StepGuide Guide(params TrainingStep[] steps)
        {
            var guide = Track(new GameObject("Step Guide")).AddComponent<StepGuide>();
            guide.Load(steps, AllHoles);
            return guide;
        }

        public T Track<T>(T thing) where T : Object
        {
            _made.Add(thing);
            return thing;
        }

        public static TrainingStep Seat(Hole hole, Screw screw, string instruction = "") =>
            new TrainingStep { instruction = instruction, finishesOn = StepFinish.ScrewSeated, hole = hole, screw = screw };

        public static TrainingStep Check(string instruction) =>
            new TrainingStep { instruction = instruction, finishesOn = StepFinish.Confirmed };

        public void Dispose()
        {
            foreach (var thing in _made)
                if (thing != null) Object.DestroyImmediate(thing);
            _made.Clear();
            _kit.Dispose();
        }
    }
}
