using System;
using System.Collections.Generic;
using System.Linq;
using Lab.Core;
using Lab.W01.ColourCodedHoles;
using Lab.W01.VariableScrew;
using UnityEngine;

namespace Lab.W01.StepGuidance
{
    /// <summary>
    /// Walks a trainee through a procedure written as data. The current step is the first one not yet
    /// finished, so emptying a finished step's hole opens that step again. The guide listens to the holes,
    /// so any input that seats screws (the mouse today, a VR hand later) drives it the same way.
    /// A refused screw counts as a mistake. So does a screw seated in a hole the current step doesn't ask
    /// for, and that screw is taken back out. Once every step is done the result stands until Restart.
    /// </summary>
    public class StepGuide : MonoBehaviour
    {
        [SerializeField] TrainingStep[] steps = new TrainingStep[0];
        [Tooltip("Every hole on the plate, including holes no step uses, so a screw put in one counts as a mistake.")]
        [SerializeField] Hole[] holes = new Hole[0];

        readonly List<Hole> _watched = new List<Hole>();
        readonly List<Mistake> _mistakes = new List<Mistake>();
        bool[] _confirmed = new bool[0];
        bool _undoing;

        /// <summary>The step index before and after. After equals StepCount once every step is done.</summary>
        public event Action<int, int> StepChanged;
        public event Action<Mistake> MistakeMade;
        public event Action Finished;
        public event Action Restarted;

        public IReadOnlyList<TrainingStep> Steps => steps;
        public IReadOnlyList<Mistake> Mistakes => _mistakes;
        public int StepCount => steps.Length;
        public int CurrentIndex { get; private set; }
        public TrainingStep Current => CurrentIndex < steps.Length ? steps[CurrentIndex] : null;
        public bool IsDone => CurrentIndex >= steps.Length;

        /// <summary>The latest mistake made on the step that is open now, for showing beside it.</summary>
        public Mistake? CurrentStepMistake =>
            !IsDone && _mistakes.Count > 0 && _mistakes[_mistakes.Count - 1].Step == CurrentIndex
                ? _mistakes[_mistakes.Count - 1]
                : (Mistake?)null;

        void OnEnable() => Begin();
        void OnDisable() => Disconnect();

        /// <summary>Replaces the procedure and the plate's holes, and starts from the first step.</summary>
        public void Load(IEnumerable<TrainingStep> newSteps, IEnumerable<Hole> plateHoles)
        {
            steps = newSteps?.ToArray() ?? new TrainingStep[0];
            holes = plateHoles?.ToArray() ?? new Hole[0];
            Begin();
        }

        public bool IsFinished(int index) => index >= 0 && index < steps.Length && IsSatisfied(index);

        /// <summary>Finishes the current step if it waits for a confirmation. Does nothing otherwise.</summary>
        public void Confirm()
        {
            if (IsDone || Current == null || Current.finishesOn != StepFinish.Confirmed) return;
            _confirmed[CurrentIndex] = true;
            Refresh();
        }

        /// <summary>Takes every screw back out of the plate, clears the mistakes and starts again.</summary>
        public void Restart()
        {
            _undoing = true;
            foreach (var hole in _watched)
            {
                var occupant = hole.Occupant;
                if (occupant == null) continue;
                hole.Vacate();
                SendHome(occupant);
            }
            _undoing = false;
            Array.Clear(_confirmed, 0, _confirmed.Length);
            _mistakes.Clear();
            CurrentIndex = FirstOpen();
            Restarted?.Invoke();
        }

        void Begin()
        {
            Disconnect();
            foreach (var hole in holes.Concat(steps.Where(s => s != null).Select(s => s.hole)))
            {
                if (hole == null || _watched.Contains(hole)) continue;
                hole.Accepted += OnAccepted;
                hole.Refused += OnRefused;
                hole.Vacated += OnVacated;
                _watched.Add(hole);
            }
            for (var i = 0; i < steps.Length; i++)
            {
                if (steps[i] != null && steps[i].finishesOn == StepFinish.ScrewSeated && steps[i].hole == null)
                    Debug.LogWarning($"Step {i + 1} waits for a screw but has no hole, so it is skipped.", this);
            }
            _confirmed = new bool[steps.Length];
            _mistakes.Clear();
            CurrentIndex = FirstOpen();
        }

        void Disconnect()
        {
            foreach (var hole in _watched)
            {
                if (hole == null) continue;
                hole.Accepted -= OnAccepted;
                hole.Refused -= OnRefused;
                hole.Vacated -= OnVacated;
            }
            _watched.Clear();
        }

        void OnAccepted(Hole hole, Screw screw)
        {
            if (_undoing || IsDone) return;
            var step = Current;
            if (step.finishesOn == StepFinish.ScrewSeated && step.hole == hole)
            {
                Refresh();
                return;
            }
            Record(step.finishesOn == StepFinish.ScrewSeated ? MistakeKind.WrongHole : MistakeKind.TooEarly, hole, screw);
            _undoing = true;
            hole.Vacate();
            _undoing = false;
            SendHome(screw);
        }

        void OnRefused(Hole hole, Screw screw, AcceptResult result)
        {
            if (_undoing || IsDone || screw == null) return;
            Record(KindOf(result), hole, screw);
        }

        void OnVacated(Hole hole, Screw screw)
        {
            if (_undoing || IsDone) return;
            Refresh();
        }

        static MistakeKind KindOf(AcceptResult result)
        {
            switch (result)
            {
                case AcceptResult.Occupied: return MistakeKind.HoleTaken;
                case AcceptResult.NotInCatalog: return MistakeKind.NotInCatalog;
                default: return MistakeKind.WrongSize;
            }
        }

        void Record(MistakeKind kind, Hole hole, Screw screw)
        {
            var mistake = new Mistake(CurrentIndex, kind, StepMessages.Describe(kind, CurrentIndex, Current, hole, screw));
            _mistakes.Add(mistake);
            MistakeMade?.Invoke(mistake);
        }

        void Refresh()
        {
            var from = CurrentIndex;
            CurrentIndex = FirstOpen();
            if (CurrentIndex == from) return;
            StepChanged?.Invoke(from, CurrentIndex);
            if (IsDone) Finished?.Invoke();
        }

        int FirstOpen()
        {
            for (var i = 0; i < steps.Length; i++)
                if (!IsSatisfied(i)) return i;
            return steps.Length;
        }

        bool IsSatisfied(int index)
        {
            var step = steps[index];
            if (step == null) return true;
            if (step.finishesOn == StepFinish.Confirmed) return _confirmed[index];
            // A screw step with no hole could never finish, so it is skipped rather than trapping the trainee.
            return step.hole == null || step.hole.Occupant != null;
        }

        static void SendHome(Screw screw)
        {
            var item = screw != null ? screw.GetComponent<Grabbable>() : null;
            if (item != null) item.ReturnHome();
        }
    }
}
