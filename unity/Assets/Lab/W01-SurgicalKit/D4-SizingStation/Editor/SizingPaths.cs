using Lab.W01.VariableScrew.EditorTools;

namespace Lab.W01.SizingStation.EditorTools
{
    /// <summary>Where this system's assets live, and the catalogs it borrows from the variable screw.</summary>
    public static class SizingPaths
    {
        public const string Root = "Assets/Lab/W01-SurgicalKit/D4-SizingStation";
        public const string ScenePath = Root + "/Demo_SizingStation.unity";
        public const string MaterialsFolder = Root + "/Art/Materials";

        public const string CatalogAPath = ScrewPaths.DataFolder + "/CatalogA_ChromaticOrtho/CatalogA_ChromaticOrtho.asset";
        public const string CatalogBPath = ScrewPaths.DataFolder + "/CatalogB_NorthlineFixation/CatalogB_NorthlineFixation.asset";
    }
}
