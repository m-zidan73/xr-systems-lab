namespace Lab.W01.VariableScrew.EditorTools
{
    /// <summary>Where this system's assets live, so moving a folder means changing one file.</summary>
    public static class ScrewPaths
    {
        public const string Root = "Assets/Lab/W01-SurgicalKit/D0-VariableScrew";
        public const string DataFolder = Root + "/Data";
        public const string TexturesFolder = Root + "/Art/Textures";

        public const string FbxPath = Root + "/Art/Meshes/Screw_Base.fbx";
        public const string MeshPath = Root + "/Art/Meshes/Screw_Base_Normalized.asset";
        public const string RibbonFbxPath = Root + "/Art/Meshes/Thread_Ribbon.fbx";
        public const string RibbonMeshPath = Root + "/Art/Meshes/Thread_Ribbon_Encoded.asset";
        public const string ThreadNormalPath = TexturesFolder + "/Thread_Normal.png";

        public const string MaterialPath = Root + "/Art/Materials/Screw.mat";
        public const string ShaftMaterialPath = Root + "/Art/Materials/ScrewShaft_Plain.mat";
        public const string ThreadMaterialPath = Root + "/Art/Materials/ScrewThread.mat";

        public const string PrefabPath = Root + "/Prefabs/Screw.prefab";
        public const string HelixPrefabPath = Root + "/Prefabs/ScrewHelix.prefab";

        public const string ScenePath = Root + "/Demo_ShaderThread.unity";
        public const string HelixScenePath = Root + "/Demo_HelixThread.unity";
    }
}
