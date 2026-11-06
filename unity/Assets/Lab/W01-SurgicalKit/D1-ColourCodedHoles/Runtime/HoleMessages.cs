using System.Globalization;
using Lab.W01.VariableScrew;

namespace Lab.W01.ColourCodedHoles
{
    /// <summary>Plain-language lines for what a hole or the catalog switcher just did.</summary>
    public static class HoleMessages
    {
        static string Mm(float value) => value.ToString("0.#", CultureInfo.InvariantCulture);

        public static string Describe(Hole hole, Screw screw, AcceptResult result)
        {
            var size = Mm(hole.DiameterMm);
            switch (result)
            {
                case AcceptResult.Accepted:
                    return $"Seated: {screw.Label} in the {size} mm hole.";
                case AcceptResult.WrongSize:
                    return screw != null && screw.Spec != null
                        ? $"Refused: a {Mm(screw.Spec.diameterMm)} mm screw doesn't fit the {size} mm hole."
                        : $"Refused: that screw has no size set, so it doesn't fit the {size} mm hole.";
                case AcceptResult.NotInCatalog:
                    var maker = hole.Catalog != null ? hole.Catalog.manufacturer : "This catalog";
                    return $"Refused: {maker} has no {size} mm screw, so this hole stays empty.";
                case AcceptResult.Occupied:
                    return $"Refused: the {size} mm hole already holds a screw.";
                default:
                    return "Only screws go in the holes.";
            }
        }

        public static string Switched(ImplantCatalog catalog)
        {
            var maker = catalog != null ? catalog.manufacturer : "none";
            return $"Catalog: {maker}. The hole colours and the tray now follow it.";
        }
    }
}
