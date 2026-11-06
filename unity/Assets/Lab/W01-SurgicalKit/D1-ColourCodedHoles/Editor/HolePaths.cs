using Lab.W01.VariableScrew.EditorTools;

namespace Lab.W01.ColourCodedHoles.EditorTools
{
    /// <summary>Where this system's assets live, and the catalogs it borrows from the variable screw.</summary>
    public static class HolePaths
    {
        public const string Root = "Assets/Lab/W01-SurgicalKit/D1-ColourCodedHoles";
        public const string ScenePath = Root + "/Demo_ColourCodedHoles.unity";
        public const string MaterialsFolder = Root + "/Art/Materials";

        public const string CatalogAPath = ScrewPaths.DataFolder + "/CatalogA_ChromaticOrtho/CatalogA_ChromaticOrtho.asset";
        public const string CatalogBPath = ScrewPaths.DataFolder + "/CatalogB_NorthlineFixation/CatalogB_NorthlineFixation.asset";
    }
}
