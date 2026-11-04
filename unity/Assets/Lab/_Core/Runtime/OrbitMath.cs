using UnityEngine;

namespace Lab.Core
{
    /// <summary>The camera orbit's sums, kept apart from input so they can be tested.</summary>
    public static class OrbitMath
    {
        public const float PitchLimitDeg = 85f;

        const float ZoomInFactor = 0.9f;
        const float ZoomOutFactor = 1.1f;

        public static Vector3 Position(Vector3 target, float yawDeg, float pitchDeg, float distance)
        {
            return target + Quaternion.Euler(pitchDeg, yawDeg, 0f) * (Vector3.back * distance);
        }

        public static (float yawDeg, float pitchDeg) Rotate(float yawDeg, float pitchDeg, Vector2 mouseDelta, float sensitivity)
        {
            return (
                yawDeg + mouseDelta.x * sensitivity,
                Mathf.Clamp(pitchDeg - mouseDelta.y * sensitivity, -PitchLimitDeg, PitchLimitDeg));
        }

        /// <summary>Uses only the sign of the scroll, because scroll units differ between setups.</summary>
        public static float Zoom(float distance, float scrollY, float minDistance, float maxDistance)
        {
            if (scrollY == 0f) return distance;
            var factor = scrollY > 0f ? ZoomInFactor : ZoomOutFactor;
            return Mathf.Clamp(distance * factor, minDistance, maxDistance);
        }
    }
}
