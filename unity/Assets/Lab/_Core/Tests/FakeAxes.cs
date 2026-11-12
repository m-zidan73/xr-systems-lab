using System;
using UnityEngine;

namespace Lab.Core.Tests
{
    /// <summary>Two adjustable axes with limits, for testing panels without a real system.</summary>
    public class FakeAxes : IAxisAdjustable
    {
        public static readonly string[] Labels = { "Lift", "Roll" };
        public static readonly string[] Units = { "mm", AxisFormat.Degrees };
        public static readonly float[] Mins = { -4f, -20f };
        public static readonly float[] Maxes = { 8f, 20f };

        readonly float[] _values = new float[2];
        public int ResetCount { get; private set; }

        public int AxisCount => 2;
        public string AxisLabel(int axis) => Labels[axis];
        public string AxisUnit(int axis) => Units[axis];
        public float AxisMin(int axis) => Mins[axis];
        public float AxisMax(int axis) => Maxes[axis];
        public float GetAxis(int axis) => _values[axis];

        public float SetAxis(int axis, float value)
        {
            _values[axis] = Mathf.Clamp(value, Mins[axis], Maxes[axis]);
            Changed?.Invoke();
            return _values[axis];
        }

        public void ResetAxes()
        {
            ResetCount++;
            Array.Clear(_values, 0, _values.Length);
            Changed?.Invoke();
        }

        public event Action Changed;
    }
}
