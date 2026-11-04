using UnityEngine;

namespace Lab.W01.VariableScrew
{
    /// <summary>
    /// A manufacturer's screw system. This is the asset a customer replaces to retarget
    /// the training to their own implants, without anyone touching code. Colour coding
    /// and length increments differ between manufacturers, so they cannot be hardcoded.
    /// </summary>
    [CreateAssetMenu(menuName = "Implants/Catalog", fileName = "ImplantCatalog")]
    public class ImplantCatalog : ScriptableObject
    {
        public string manufacturer = "Example Surgical";

        [TextArea(2, 4)]
        public string notes = "Illustrative example data, not a real manufacturer's specification.";

        public ScrewSpec[] screws;

        public ScrewSpec ByDiameter(float diameterMm)
        {
            if (screws == null) return null;
            foreach (var spec in screws)
                if (spec != null && Mathf.Approximately(spec.diameterMm, diameterMm))
                    return spec;
            return null;
        }

        /// <summary>
        /// The screw that belongs in a given plate hole. Returns null when the trainee
        /// reached for the wrong diameter, which is a teachable moment rather than a bug.
        /// </summary>
        public ScrewSpec ForHole(float holeDiameterMm) => ByDiameter(holeDiameterMm);
    }
}
