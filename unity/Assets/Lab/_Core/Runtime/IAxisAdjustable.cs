using System;

namespace Lab.Core
{
    /// <summary>
    /// Anything with a few numeric axes a person can adjust within limits, such as a plate's position
    /// and angle. Panels, keys and later VR controls drive it through this, so a system never reads input.
    /// </summary>
    public interface IAxisAdjustable
    {
        int AxisCount { get; }
        string AxisLabel(int axis);
        /// <summary>"mm" or "°", shown after the value.</summary>
        string AxisUnit(int axis);
        float AxisMin(int axis);
        float AxisMax(int axis);
        float GetAxis(int axis);
        /// <summary>Sets the axis, kept within its limits, and returns the value it ended up at.</summary>
        float SetAxis(int axis, float value);
        void ResetAxes();
        /// <summary>Raised after any axis changes, including a reset.</summary>
        event Action Changed;
    }
}
