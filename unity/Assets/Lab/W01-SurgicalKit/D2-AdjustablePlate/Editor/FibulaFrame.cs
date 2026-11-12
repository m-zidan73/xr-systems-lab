using System.Collections.Generic;
using UnityEngine;

namespace Lab.W01.AdjustablePlate.EditorTools
{
    /// <summary>
    /// Works out where the plate belongs on the bone model: along the fibula, on its outer (lateral) side,
    /// with its distal end just above the tip of the fibula, resting on the surface without sinking in.
    /// The anatomy is a downloaded scan with no landmarks, so this reads the shape of the vertices.
    /// </summary>
    public static class FibulaFrame
    {
        const float BinSize = 0.002f;
        const float UpperShare = 0.4f;      // the top part of the fibula, where the tibia is the only other bone
        const float AcrossSlab = 0.005f;

        public struct Fit
        {
            /// <summary>Where the plate's origin goes, in world space.</summary>
            public Vector3 position;
            /// <summary>The plate's rotation: its Y up the bone, its Z out of the bone's side or into it when flipped.</summary>
            public Quaternion rotation;
            public Vector3 axis;
            public Vector3 lateral;
            /// <summary>True when the plate lies face-down, which fits this bone's shape better.</summary>
            public bool flipped;
            /// <summary>The average space between the plate's underside and the bone, in metres.</summary>
            public float meanGap;
        }

        /// <param name="fibula">The fibula's vertices in world space.</param>
        /// <param name="others">The other bones' vertices in world space.</param>
        /// <param name="plate">The plate's vertices in its own space: Y along its length with the distal end at -Y, Z through its face.</param>
        /// <param name="distalMargin">Metres between the tip of the fibula and the plate's distal end.</param>
        /// <param name="gap">Metres left between the plate and the bone at the closest point.</param>
        public static Fit Place(IReadOnlyList<Vector3> fibula, IReadOnlyList<Vector3> others, IReadOnlyList<Vector3> plate, float distalMargin, float gap)
        {
            var centre = Mean(fibula);
            var axis = PrincipalAxis(fibula, centre);
            if (axis.y < 0f) axis = -axis;

            float tMin = float.MaxValue, tMax = float.MinValue;
            foreach (var v in fibula)
            {
                var t = Vector3.Dot(v - centre, axis);
                tMin = Mathf.Min(tMin, t);
                tMax = Mathf.Max(tMax, t);
            }

            var lateral = Lateral(fibula, others, centre, axis, tMax - UpperShare * (tMax - tMin), tMax);
            var across = Vector3.Cross(axis, lateral);

            float plateMinY = float.MaxValue;
            foreach (var p in plate) plateMinY = Mathf.Min(plateMinY, p.y);
            var tCentre = tMin + distalMargin - plateMinY;

            // The highest point of the bone's outer side, every couple of millimetres along it.
            var binCount = Mathf.CeilToInt((tMax - tMin) / BinSize) + 1;
            var surface = new float[binCount];
            for (var i = 0; i < binCount; i++) surface[i] = float.MinValue;
            float acrossSum = 0f;
            var acrossCount = 0;
            foreach (var v in fibula)
            {
                var offset = v - centre;
                var t = Vector3.Dot(offset, axis);
                var bin = Mathf.Clamp(Mathf.FloorToInt((t - tMin) / BinSize), 0, binCount - 1);
                surface[bin] = Mathf.Max(surface[bin], Vector3.Dot(offset, lateral));
                if (Mathf.Abs(t - tCentre) < AcrossSlab)
                {
                    acrossSum += Vector3.Dot(offset, across);
                    acrossCount++;
                }
            }
            var acrossOffset = acrossCount > 0 ? acrossSum / acrossCount : 0f;

            var faceUp = Rest(plate, surface, tMin, tCentre, +1f, gap);
            var faceDown = Rest(plate, surface, tMin, tCentre, -1f, gap);
            var flipped = faceDown.meanGap < faceUp.meanGap;
            var chosen = flipped ? faceDown : faceUp;
            var sign = flipped ? -1f : 1f;

            return new Fit
            {
                position = centre + axis * tCentre + across * acrossOffset + lateral * chosen.height,
                rotation = Quaternion.LookRotation(lateral * sign, axis),
                axis = axis,
                lateral = lateral,
                flipped = flipped,
                meanGap = chosen.meanGap,
            };
        }

        // How far out the plate's origin has to sit so no part of it is inside the bone, and how much space
        // is then left under it on average. sign says whether the plate's +Z points out of the bone or in.
        static (float height, float meanGap) Rest(IReadOnlyList<Vector3> plate, float[] surface, float tMin, float tCentre, float sign, float gap)
        {
            var height = float.MinValue;
            var underside = new float[surface.Length];
            for (var i = 0; i < underside.Length; i++) underside[i] = float.MaxValue;
            foreach (var p in plate)
            {
                var bin = Mathf.FloorToInt((tCentre + p.y - tMin) / BinSize);
                if (bin < 0 || bin >= surface.Length || surface[bin] == float.MinValue) continue;
                var face = sign * p.z;
                height = Mathf.Max(height, surface[bin] - face);
                underside[bin] = Mathf.Min(underside[bin], face);
            }
            if (height == float.MinValue) return (0f, float.MaxValue);
            height += gap;

            float total = 0f;
            var count = 0;
            for (var i = 0; i < surface.Length; i++)
            {
                if (underside[i] == float.MaxValue || surface[i] == float.MinValue) continue;
                total += height + underside[i] - surface[i];
                count++;
            }
            return (height, count > 0 ? total / count : float.MaxValue);
        }

        // From the tibia towards the fibula, at a height where the tibia is the only other bone.
        static Vector3 Lateral(IReadOnlyList<Vector3> fibula, IReadOnlyList<Vector3> others, Vector3 centre, Vector3 axis, float tFrom, float tTo)
        {
            var fibulaMid = MeanInSlab(fibula, centre, axis, tFrom, tTo, out var fibulaCount);
            var otherMid = MeanInSlab(others, centre, axis, tFrom, tTo, out var otherCount);
            var direction = fibulaCount > 0 && otherCount > 0 ? fibulaMid - otherMid : Vector3.right;
            direction = Vector3.ProjectOnPlane(direction, axis);
            return direction.sqrMagnitude > 1e-12f ? direction.normalized : Vector3.ProjectOnPlane(Vector3.right, axis).normalized;
        }

        static Vector3 MeanInSlab(IReadOnlyList<Vector3> points, Vector3 centre, Vector3 axis, float tFrom, float tTo, out int count)
        {
            var sum = Vector3.zero;
            count = 0;
            foreach (var p in points)
            {
                var t = Vector3.Dot(p - centre, axis);
                if (t < tFrom || t > tTo) continue;
                sum += p;
                count++;
            }
            return count > 0 ? sum / count : Vector3.zero;
        }

        public static Vector3 Mean(IReadOnlyList<Vector3> points)
        {
            var sum = Vector3.zero;
            foreach (var p in points) sum += p;
            return points.Count > 0 ? sum / points.Count : Vector3.zero;
        }

        /// <summary>The direction the points spread along most, by power iteration on their covariance.</summary>
        public static Vector3 PrincipalAxis(IReadOnlyList<Vector3> points, Vector3 centre)
        {
            float xx = 0, xy = 0, xz = 0, yy = 0, yz = 0, zz = 0;
            foreach (var p in points)
            {
                var d = p - centre;
                xx += d.x * d.x; xy += d.x * d.y; xz += d.x * d.z;
                yy += d.y * d.y; yz += d.y * d.z; zz += d.z * d.z;
            }
            var v = new Vector3(0.1f, 1f, 0.1f).normalized;
            for (var i = 0; i < 64; i++)
            {
                var next = new Vector3(
                    xx * v.x + xy * v.y + xz * v.z,
                    xy * v.x + yy * v.y + yz * v.z,
                    xz * v.x + yz * v.y + zz * v.z);
                if (next.sqrMagnitude < 1e-20f) break;
                v = next.normalized;
            }
            return v;
        }
    }
}
