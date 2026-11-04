using UnityEngine;

namespace Lab.W01.VariableScrew
{
    /// <summary>
    /// One screw type from one manufacturer's system. Everything that varies between
    /// implant systems lives here, so swapping the catalog swaps the whole exercise.
    /// Dimensions are millimetres and are converted where they are used.
    /// </summary>
    [CreateAssetMenu(menuName = "Implants/Screw Spec", fileName = "ScrewSpec")]
    public class ScrewSpec : ScriptableObject
    {
        [Header("Identity")]
        public string displayName = "3.5 mm Locking Cortical";
        [Tooltip("Nominal outer thread diameter.")]
        public float diameterMm = 3.5f;
        public bool locking = true;

        [Header("Tray")]
        [Tooltip("Anodised colour this manufacturer uses for this diameter. There is no " +
                 "industry standard mapping, which is exactly why it belongs in data.")]
        public Color trayColor = new Color(0.16f, 0.42f, 0.85f);

        [Header("Lengths this system actually ships, ascending")]
        public float[] availableLengthsMm = { 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30 };

        [Header("Drilling")]
        public float threadHoleDrillMm = 2.5f;
        public float glideHoleDrillMm = 3.5f;

        [Header("Thread")]
        [Tooltip("Millimetres of advance per turn. Varies with diameter and with screw type, " +
                 "so it belongs in the catalog rather than on the material.")]
        public float threadPitchMm = 1.25f;
        [Tooltip("Single start on every screw here. Exposed because the shader assumes it.")]
        public int threadStarts = 1;

        [Header("Source mesh")]
        [Tooltip("Length of the authored mesh, under the head to the tip.")]
        public float baseMeshLengthMm = 20f;
        [Tooltip("Where the head stops and the stretchable shaft begins, from z = 0.")]
        public float headEndMm = 2.5f;
        [Tooltip("Length of the rigid tip that is translated rather than stretched.")]
        public float tipLengthMm = 2f;

        [Header("Provenance")]
        [TextArea(2, 4)]
        [Tooltip("Where these numbers came from. Keep this honest: invented values must " +
                 "say so, because someone will eventually assume they are clinical fact.")]
        public string notes = "Illustrative example data, not a real manufacturer's specification.";

        public float ShaftLengthMm => baseMeshLengthMm - headEndMm - tipLengthMm;

        /// <summary>
        /// How many thread repeats the authored shaft holds. The shader scales this as the
        /// shaft stretches, so the pitch on screen stays at threadPitchMm at every length.
        /// </summary>
        public float ThreadRepeatsAtBase => ShaftLengthMm / Mathf.Max(threadPitchMm, 0.01f);

        /// <summary>Snap a requested length to one that actually exists in the tray.</summary>
        public float NearestAvailable(float requestedMm)
        {
            if (availableLengthsMm == null || availableLengthsMm.Length == 0) return requestedMm;

            float best = availableLengthsMm[0];
            float bestDelta = Mathf.Abs(requestedMm - best);
            for (int i = 1; i < availableLengthsMm.Length; i++)
            {
                float delta = Mathf.Abs(requestedMm - availableLengthsMm[i]);
                if (delta < bestDelta) { best = availableLengthsMm[i]; bestDelta = delta; }
            }
            return best;
        }

        /// <summary>Index of the nearest available length, for an index driven slider.</summary>
        public int NearestIndex(float requestedMm)
        {
            if (availableLengthsMm == null || availableLengthsMm.Length == 0) return 0;

            int best = 0;
            float bestDelta = Mathf.Abs(requestedMm - availableLengthsMm[0]);
            for (int i = 1; i < availableLengthsMm.Length; i++)
            {
                float delta = Mathf.Abs(requestedMm - availableLengthsMm[i]);
                if (delta < bestDelta) { best = i; bestDelta = delta; }
            }
            return best;
        }

        /// <summary>
        /// How far the screw sits proud of the far cortex. Positive protrudes, which is
        /// the error that irritates soft tissue. Negative fails to get purchase.
        /// </summary>
        public float ProtrusionMm(float chosenLengthMm, float measuredDepthMm)
            => chosenLengthMm - measuredDepthMm;
    }
}
