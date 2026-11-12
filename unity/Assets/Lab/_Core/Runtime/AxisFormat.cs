using UnityEngine;

namespace Lab.Core
{
    /// <summary>How an axis value reads on screen: always signed, one decimal, so the direction shows at a glance on video.</summary>
    public static class AxisFormat
    {
        const char Minus = '−';
        public const string Degrees = "°";
        const float LimitTolerance = 0.0001f;

        public static string Readout(float value, string unit)
        {
            // Halves round away from zero, as people read them, not to the nearest even digit.
            var tenths = (int)System.Math.Round(value * 10.0, System.MidpointRounding.AwayFromZero);
            var sign = tenths > 0 ? "+" : tenths < 0 ? Minus.ToString() : "";
            var number = (Mathf.Abs(tenths) / 10f).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
            var gap = unit == Degrees ? "" : " ";
            return $"{sign}{number}{gap}{unit}";
        }

        public static bool AtLimit(float value, float min, float max) =>
            value <= min + LimitTolerance || value >= max - LimitTolerance;
    }
}
