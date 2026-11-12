using System.Collections.Generic;
using UnityEngine;

namespace Lab.W01.AdjustablePlate
{
    /// <summary>
    /// Turns six offsets from the start pose into a pose, in the frame the plate sits in. Moves go along the
    /// frame's axes; turns go around the frame's axes through the plate's own origin, so each slider means
    /// the same thing however the plate was placed, and turning never drags the plate sideways.
    /// </summary>
    public static class PlatePose
    {
        public const float MillimetresToMetres = 0.001f;

        public static Vector3 Translation(IReadOnlyList<float> offsets) =>
            new Vector3(offsets[0], offsets[1], offsets[2]) * MillimetresToMetres;

        public static Quaternion Rotation(IReadOnlyList<float> offsets) =>
            Quaternion.Euler(offsets[3], offsets[4], offsets[5]);

        public static (Vector3 position, Quaternion rotation) Apply(Vector3 startPosition, Quaternion startRotation, IReadOnlyList<float> offsets) =>
            (startPosition + Translation(offsets), Rotation(offsets) * startRotation);

        /// <summary>The start pose from which these offsets land the plate exactly on the target pose.</summary>
        public static (Vector3 position, Quaternion rotation) StartFor(Vector3 targetPosition, Quaternion targetRotation, IReadOnlyList<float> targetOffsets) =>
            (targetPosition - Translation(targetOffsets), Quaternion.Inverse(Rotation(targetOffsets)) * targetRotation);
    }
}
