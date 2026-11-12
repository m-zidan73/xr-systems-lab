using System;
using Lab.Core;
using UnityEngine;

namespace Lab.W01.AdjustablePlate
{
    /// <summary>
    /// Moves and turns the plate it sits on, relative to where the plate was at the start, and never past
    /// an axis's limits. It reports every change and says when the plate comes into or leaves the right
    /// spot. It reads no input: the slider panel, keys and later a VR hand all call <see cref="Set"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlateAdjuster : MonoBehaviour, IAxisAdjustable
    {
        const float Epsilon = 0.0001f;

        [Tooltip("One entry per axis, in this order: Move X, Move Y, Move Z, Turn X, Turn Y, Turn Z.")]
        [SerializeField] AxisSettings[] axes = DefaultAxes();
        [Tooltip("How close each move has to be to its target, in millimetres, to count as in place.")]
        [SerializeField, Min(0f)] float positionToleranceMm = 1f;
        [Tooltip("How close each turn has to be to its target, in degrees, to count as in place.")]
        [SerializeField, Min(0f)] float rotationToleranceDeg = 2f;

        readonly float[] _offsets = new float[PlateAxes.Count];
        Vector3 _startPosition;
        Quaternion _startRotation = Quaternion.identity;
        bool _hasStart;
        bool _placed;

        /// <summary>Raised after any axis changes, including a reset.</summary>
        public event Action Changed;
        /// <summary>Raised after one axis changes.</summary>
        public event Action<PlateAxis> AxisChanged;
        /// <summary>Raised when a value had to be held back at an axis limit.</summary>
        public event Action<PlateAxis> LimitReached;
        /// <summary>Raised with true when the plate comes into the right spot, and false when it leaves it.</summary>
        public event Action<bool> PlacedChanged;

        public float PositionToleranceMm => positionToleranceMm;
        public float RotationToleranceDeg => rotationToleranceDeg;
        public bool IsPlaced => _placed;

        public static AxisSettings[] DefaultAxes() => new[]
        {
            new AxisSettings("Across (X)", -10f, 10f, 6f),
            new AxisSettings("Along (Y)", -30f, 30f, -20f),
            new AxisSettings("Lift (Z)", -5f, 10f, -5f),
            new AxisSettings("Tilt (X)", -20f, 20f, 8f),
            new AxisSettings("Roll (Y)", -25f, 25f, -12f),
            new AxisSettings("Swivel (Z)", -30f, 30f, 15f),
        };

        void Awake()
        {
            CaptureStart();
            _placed = ComputePlaced();
        }

        void OnValidate()
        {
            if (axes != null && axes.Length == PlateAxes.Count) return;
            var defaults = DefaultAxes();
            var fixedAxes = new AxisSettings[PlateAxes.Count];
            for (var i = 0; i < fixedAxes.Length; i++)
                fixedAxes[i] = axes != null && i < axes.Length ? axes[i] : defaults[i];
            axes = fixedAxes;
        }

        /// <summary>Takes the plate's current local pose as the start, where every offset is zero.</summary>
        public void CaptureStart()
        {
            _startPosition = transform.localPosition;
            _startRotation = transform.localRotation;
            _hasStart = true;
        }

        public AxisSettings Settings(PlateAxis axis) => axes[(int)axis];

        public float Get(PlateAxis axis) => _offsets[(int)axis];

        /// <summary>Sets one axis, held within its limits, and returns the value it ended up at.</summary>
        public float Set(PlateAxis axis, float value)
        {
            EnsureStart();
            var index = (int)axis;
            var kept = axes[index].Clamp(value);
            var changed = Mathf.Abs(kept - _offsets[index]) > Epsilon;
            if (changed)
            {
                _offsets[index] = kept;
                ApplyPose();
                AxisChanged?.Invoke(axis);
                Changed?.Invoke();
            }
            if (Mathf.Abs(kept - value) > Epsilon) LimitReached?.Invoke(axis);
            if (changed) UpdatePlaced();
            return _offsets[index];
        }

        public float Nudge(PlateAxis axis, float delta) => Set(axis, Get(axis) + delta);

        /// <summary>Puts the plate back where it started.</summary>
        public void ResetAll()
        {
            EnsureStart();
            Array.Clear(_offsets, 0, _offsets.Length);
            ApplyPose();
            Changed?.Invoke();
            UpdatePlaced();
        }

        public bool AtLimit(PlateAxis axis) => AxisFormat.AtLimit(Get(axis), axes[(int)axis].min, axes[(int)axis].max);

        public bool IsOnTarget(PlateAxis axis) =>
            Mathf.Abs(Get(axis) - axes[(int)axis].target) <= Tolerance(axis) + Epsilon;

        public float Tolerance(PlateAxis axis) => axis.IsTurn() ? rotationToleranceDeg : positionToleranceMm;

        /// <summary>Where the plate belongs, in the same space as its local pose.</summary>
        public (Vector3 position, Quaternion rotation) TargetPose
        {
            get
            {
                EnsureStart();
                var targets = new float[PlateAxes.Count];
                for (var i = 0; i < targets.Length; i++) targets[i] = axes[i].target;
                return PlatePose.Apply(_startPosition, _startRotation, targets);
            }
        }

        public (Vector3 position, Quaternion rotation) StartPose
        {
            get
            {
                EnsureStart();
                return (_startPosition, _startRotation);
            }
        }

        void EnsureStart()
        {
            if (!_hasStart) CaptureStart();
        }

        void ApplyPose()
        {
            var (position, rotation) = PlatePose.Apply(_startPosition, _startRotation, _offsets);
            transform.SetLocalPositionAndRotation(position, rotation);
        }

        bool ComputePlaced()
        {
            for (var i = 0; i < PlateAxes.Count; i++)
                if (!IsOnTarget((PlateAxis)i)) return false;
            return true;
        }

        void UpdatePlaced()
        {
            var placed = ComputePlaced();
            if (placed == _placed) return;
            _placed = placed;
            PlacedChanged?.Invoke(placed);
        }

        int IAxisAdjustable.AxisCount => PlateAxes.Count;
        string IAxisAdjustable.AxisLabel(int axis) => axes[axis].label;
        string IAxisAdjustable.AxisUnit(int axis) => ((PlateAxis)axis).Unit();
        float IAxisAdjustable.AxisMin(int axis) => axes[axis].min;
        float IAxisAdjustable.AxisMax(int axis) => axes[axis].max;
        float IAxisAdjustable.GetAxis(int axis) => Get((PlateAxis)axis);
        float IAxisAdjustable.SetAxis(int axis, float value) => Set((PlateAxis)axis, value);
        void IAxisAdjustable.ResetAxes() => ResetAll();
    }
}
