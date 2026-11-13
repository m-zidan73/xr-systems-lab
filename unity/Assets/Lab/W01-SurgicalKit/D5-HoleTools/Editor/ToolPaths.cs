using Lab.W01.VariableScrew.EditorTools;

namespace Lab.W01.HoleTools.EditorTools
{
    /// <summary>Where this system's assets live, and the catalog it borrows from the variable screw.</summary>
    public static class ToolPaths
    {
        public const string Root = "Assets/Lab/W01-SurgicalKit/D5-HoleTools";
        public const string ScenePath = Root + "/Demo_HoleTools.unity";
        public const string MaterialsFolder = Root + "/Art/Materials";

        public const string CatalogPath = ScrewPaths.DataFolder + "/CatalogA_ChromaticOrtho/CatalogA_ChromaticOrtho.asset";
    }
}
