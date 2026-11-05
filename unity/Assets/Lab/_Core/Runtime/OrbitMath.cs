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

        /// <summary>
        /// Moves the orbit point across the view. One pixel of mouse movement covers the same
        /// on-screen distance at any zoom, so the part being dragged stays under the cursor.
        /// </summary>
        public static Vector3 Pan(Vector3 target, Quaternion rotation, Vector2 mouseDelta, float distance, float fieldOfViewDeg, float screenHeightPixels)
        {
            var metresPerPixel = 2f * distance * Mathf.Tan(fieldOfViewDeg * 0.5f * Mathf.Deg2Rad) / Mathf.Max(screenHeightPixels, 1f);
            return target - rotation * new Vector3(mouseDelta.x, mouseDelta.y, 0f) * metresPerPixel;
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
