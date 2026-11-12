using UnityEditor;

namespace Lab.W01.AdjustablePlate.EditorTools
{
    /// <summary>Imports this system's models as plain meshes: no materials, cameras, lights or animation.</summary>
    public class PlateModelImport : AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(PlatePaths.ModelsFolder + "/")) return;
            var importer = (ModelImporter)assetImporter;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.animationType = ModelImporterAnimationType.None;
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            // The demo builder reads the vertices to fit the plate against the bone.
            importer.isReadable = true;
        }
    }
}
