using UnityEngine;

namespace Lab.W01.HoleTools
{
    /// <summary>
    /// Measures how far it is from a hole to the far side of the bone under it, the way a depth gauge
    /// hooks the far side and reads back to the plate. The reading starts at the hole, so it includes any
    /// space between the plate and the bone: that is the length a screw has to span.
    /// </summary>
    public static class BoneDepth
    {
        public const float MetresToMillimetres = 1000f;

        /// <param name="bone">The bone's collider. Only this collider is measured, whatever else is in the way.</param>
        /// <param name="origin">Where the screw's head will sit, in world space.</param>
        /// <param name="direction">The way the screw points, into the bone.</param>
        /// <param name="reachMetres">How far past the hole to look. Longer than the bone is thick.</param>
        /// <param name="depthMm">Millimetres from the origin to the far side of the bone.</param>
        /// <param name="farPoint">Where the hole's axis leaves the bone.</param>
        /// <returns>False when there is no bone on the hole's axis.</returns>
        public static bool TryMeasure(Collider bone, Vector3 origin, Vector3 direction, float reachMetres, out float depthMm, out Vector3 farPoint)
        {
            depthMm = 0f;
            farPoint = origin;
            if (bone == null || direction.sqrMagnitude < 1e-12f || reachMetres <= 0f) return false;
            direction.Normalize();

            // Looking back from beyond the bone, the first surface met is the far side. Looking forward
            // from the hole would stop at the near side instead.
            var back = new Ray(origin + direction * reachMetres, -direction);
            if (!bone.Raycast(back, out var hit, reachMetres)) return false;

            farPoint = hit.point;
            depthMm = (reachMetres - hit.distance) * MetresToMillimetres;
            return depthMm > 0f;
        }
    }
}
