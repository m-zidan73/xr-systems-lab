using UnityEditor;
using UnityEngine;

namespace Lab.W01.VariableScrew.EditorTools
{
    /// <summary>
    /// Import settings for anything dropped into Art/Textures, so a generated or a
    /// freshly baked normal map is correct without anyone remembering to tick the boxes.
    /// A thread map imported as a colour texture looks almost right and is lit completely
    /// wrong, which is a miserable thing to debug at midnight.
    /// </summary>
    public class ScrewTextureSettings : AssetPostprocessor
    {
        const string Folder = ScrewPaths.TexturesFolder + "/";

        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').StartsWith(Folder)) return;

            var importer = (TextureImporter)assetImporter;
            importer.wrapMode   = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.anisoLevel = 4;

            if (assetPath.EndsWith("_Normal.png"))
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.sRGBTexture = false;
            }
        }
    }
}
