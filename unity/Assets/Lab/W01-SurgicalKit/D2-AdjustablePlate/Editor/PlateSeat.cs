using UnityEngine;

namespace Lab.W01.AdjustablePlate.EditorTools
{
    /// <summary>
    /// Where the plate belongs on the bone. <see cref="FibulaFrame"/> works out a first fit from the shape
    /// of the scan, which leaves the plate standing off the bone because the stand-in anatomy doesn't
    /// match the plate's curve. The owner then seated the plate by hand, and that correction is kept here,
    /// so every scene built from the fit (placement, drilling, measuring) agrees on the same spot.
    /// </summary>
    public static class PlateSeat
    {
        /// <summary>The hand correction from the first fit, in the fit's own axes: metres up the bone (Y) and into it (-Z).</summary>
        public static readonly Vector3 OwnerMove = new Vector3(0f, 0.0144f, -0.0049f);
        /// <summary>The hand correction's turn, in degrees around the fit's X axis.</summary>
        public const float OwnerTiltDeg = -1.621f;

        public static (Vector3 position, Quaternion rotation) From(FibulaFrame.Fit fit) =>
            (fit.position + fit.rotation * OwnerMove, fit.rotation * Quaternion.Euler(OwnerTiltDeg, 0f, 0f));
    }
}
