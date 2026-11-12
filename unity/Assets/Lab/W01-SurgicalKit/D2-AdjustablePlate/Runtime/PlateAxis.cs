using System;
using Lab.Core;
using UnityEngine;

namespace Lab.W01.AdjustablePlate
{
    /// <summary>
    /// The six ways the plate can be adjusted, along and around the axes of the frame it sits in.
    /// In the demo that frame is lined up with the bone: Y runs along it and Z points out of it.
    /// </summary>
    public enum PlateAxis
    {
        MoveX,
        MoveY,
        MoveZ,
        TurnX,
        TurnY,
        TurnZ,
    }

    public static class PlateAxes
    {
        public const int Count = 6;

        public static bool IsTurn(this PlateAxis axis) => axis >= PlateAxis.TurnX;

        /// <summary>Millimetres for a move, degrees for a turn.</summary>
        public static string Unit(this PlateAxis axis) => axis.IsTurn() ? AxisFormat.Degrees : "mm";
    }

    /// <summary>One axis: its name on screen, how far it may go from the start pose, and where the plate belongs.</summary>
    [Serializable]
    public struct AxisSettings
    {
        public string label;
        [Tooltip("The lowest offset from the start pose: millimetres for a move, degrees for a turn.")]
        public float min;
        [Tooltip("The highest offset from the start pose.")]
        public float max;
        [Tooltip("The offset that puts the plate in the right spot. Keep it between min and max.")]
        public float target;

        public AxisSettings(string label, float min, float max, float target)
        {
            this.label = label;
            this.min = min;
            this.max = max;
            this.target = target;
        }

        public float Clamp(float value) => Mathf.Clamp(value, min, max);
    }
}
