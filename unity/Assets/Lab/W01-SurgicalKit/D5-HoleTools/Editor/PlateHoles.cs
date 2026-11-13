using UnityEngine;

namespace Lab.W01.HoleTools.EditorTools
{
    /// <summary>
    /// The plate openings the exercise uses, measured from the plate model: where each one is on the
    /// plate's face, and the screw diameter it takes. The plate has more openings than this; four keep
    /// the exercise short. Diameters are ones both example catalogs ship, picked by the opening's width,
    /// and each opening is one where the catalogs ship a screw long enough to reach the far side.
    /// </summary>
    public static class PlateHoles
    {
        public readonly struct Opening
        {
            public readonly string Name;
            /// <summary>Millimetres across the plate, from its centre line.</summary>
            public readonly float XMm;
            /// <summary>Millimetres along the plate, from its middle. The distal end is negative.</summary>
            public readonly float YMm;
            public readonly float DiameterMm;

            public Opening(string name, float xMm, float yMm, float diameterMm)
            {
                Name = name;
                XMm = xMm;
                YMm = yMm;
                DiameterMm = diameterMm;
            }
        }

        public static readonly Opening[] Used =
        {
            new Opening("Shaft slot, upper", -0.04f, 15.53f, 3.5f),
            new Opening("Shaft slot, lower", 0.04f, -16.26f, 5.0f),
            new Opening("Distal hole, centre", 0.01f, -34.27f, 3.5f),
            new Opening("Distal hole, edge", -4.82f, -24.13f, 2.7f),
        };

        const float RimSearchMetres = 0.0004f;

        /// <summary>
        /// The height of the plate's outer face at an opening, in the plate's own space, read from the
        /// vertices on the opening's rim. The plate is curved, so this differs from hole to hole.
        /// </summary>
        public static float RimHeight(Vector3[] plateVertices, float x, float y)
        {
            var nearest = float.MaxValue;
            foreach (var v in plateVertices)
                nearest = Mathf.Min(nearest, new Vector2(v.x - x, v.y - y).magnitude);
            var top = float.MinValue;
            foreach (var v in plateVertices)
                if (new Vector2(v.x - x, v.y - y).magnitude <= nearest + RimSearchMetres) top = Mathf.Max(top, v.z);
            return top;
        }
    }
}
