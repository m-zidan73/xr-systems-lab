using System.Linq;
using System.Reflection;
using Lab.Core;
using Lab.W01.VariableScrew.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lab.W01.VariableScrew.Tests
{
    /// <summary>Checks the assets arrived intact: paths, prefabs, scenes, materials and data.</summary>
    public class PortTests
    {
        const int ScrewsInEachScene = 10;
        const float DemoStartDistance = 0.145f;

        static T[] InScene<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

        static void AssertOrbitCamera(Scene scene)
        {
            var camera = InScene<Camera>(scene).Single(c => c.name == "Main Camera");
            var orbit = camera.GetComponent<OrbitCamera>();
            Assert.IsNotNull(orbit, "Main Camera needs an OrbitCamera");
            Assert.LessOrEqual(orbit.NearClip, 0.001f);
            Assert.AreEqual(DemoStartDistance, orbit.StartDistance, 1e-5f);
        }

        [Test]
        public void EveryScrewPathResolves()
        {
            var paths = typeof(ScrewPaths)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.IsLiteral && field.FieldType == typeof(string))
                .Select(field => (string)field.GetRawConstantValue())
                .ToArray();
            Assert.Greater(paths.Length, 10);
            foreach (var path in paths)
                Assert.IsNotEmpty(AssetDatabase.AssetPathToGUID(path, AssetPathToGUIDOptions.OnlyExistingAssets), path);
        }

        [Test]
        public void Prefabs_KeepComponentsAndSpecs()
        {
            var shader = AssetDatabase.LoadAssetAtPath<GameObject>(ScrewPaths.PrefabPath);
            Assert.IsNotNull(shader.GetComponent<Screw>().Spec);
            var helix = AssetDatabase.LoadAssetAtPath<GameObject>(ScrewPaths.HelixPrefabPath);
            Assert.IsNotNull(helix.GetComponent<HelixScrew>().Spec);
        }

        [Test]
        public void ShaderScene_HasTenScrewsHeroAndOrbitCamera()
        {
            var scene = EditorSceneManager.OpenScene(ScrewPaths.ScenePath, OpenSceneMode.Additive);
            try
            {
                var screws = InScene<Screw>(scene);
                Assert.AreEqual(ScrewsInEachScene, screws.Length);
                Assert.IsTrue(screws.Any(screw => screw.name == "Hero Screw"));
                foreach (var screw in screws) Assert.IsNotNull(screw.Spec, screw.name);
                AssertOrbitCamera(scene);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void HelixScene_HasTenScrewsAndOrbitCamera()
        {
            var scene = EditorSceneManager.OpenScene(ScrewPaths.HelixScenePath, OpenSceneMode.Additive);
            try
            {
                var screws = InScene<HelixScrew>(scene);
                Assert.AreEqual(ScrewsInEachScene, screws.Length);
                foreach (var screw in screws) Assert.IsNotNull(screw.Spec, screw.name);
                AssertOrbitCamera(scene);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        /// <summary>Average brightness of everything the camera sees except its background.</summary>
        static float MeanSurfaceBrightness(Camera camera)
        {
            const int Width = 320;
            const int Height = 200;
            const float BackgroundTolerance = 0.04f;

            var target = RenderTexture.GetTemporary(Width, Height, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                image.Apply();

                var background = camera.backgroundColor;
                var surface = image.GetPixels().Where(pixel =>
                    Mathf.Abs(pixel.r - background.r) + Mathf.Abs(pixel.g - background.g) + Mathf.Abs(pixel.b - background.b)
                    > BackgroundTolerance).ToArray();
                Assert.IsNotEmpty(surface, "Nothing but background in view");
                return surface.Average(pixel => pixel.grayscale);
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(target);
                Object.DestroyImmediate(image);
            }
        }

        // Custom URP shaders need the cluster keyword under Forward+, or direct light silently
        // drops to zero and only the ambient remains.
        [TestCase(ScrewPaths.ScenePath)]
        [TestCase(ScrewPaths.HelixScenePath)]
        public void Screws_AreLitByTheSceneLights(string scenePath)
        {
            // In a freshly imported project a Unity package logs an error on the first render.
            // It isn't about these shaders, and Materials_HaveWorkingShaders covers those.
            LogAssert.ignoreFailingMessages = true;
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            try
            {
                var camera = InScene<Camera>(scene).Single(c => c.name == "Main Camera");
                var lit = MeanSurfaceBrightness(camera);
                foreach (var light in InScene<Light>(scene)) light.enabled = false;
                var unlit = MeanSurfaceBrightness(camera);
                Assert.Greater(lit, unlit * 1.2f, $"lit {lit}, unlit {unlit}");
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void Materials_HaveWorkingShaders()
        {
            foreach (var path in new[] { ScrewPaths.MaterialPath, ScrewPaths.ShaftMaterialPath, ScrewPaths.ThreadMaterialPath })
            {
                var shader = AssetDatabase.LoadAssetAtPath<Material>(path).shader;
                Assert.AreNotEqual("Hidden/InternalErrorShader", shader.name, path);
                Assert.IsFalse(ShaderUtil.ShaderHasError(shader), path);
            }
        }

        [Test]
        public void BothCatalogs_SayIllustrative_AndLengthsAscend()
        {
            var catalogs = AssetDatabase.FindAssets("t:ImplantCatalog", new[] { ScrewPaths.DataFolder })
                .Select(guid => AssetDatabase.LoadAssetAtPath<ImplantCatalog>(AssetDatabase.GUIDToAssetPath(guid)))
                .ToArray();
            Assert.AreEqual(2, catalogs.Length);
            foreach (var catalog in catalogs)
            {
                StringAssert.Contains("Illustrative", catalog.notes, catalog.name);
                Assert.IsNotEmpty(catalog.screws, catalog.name);
                foreach (var spec in catalog.screws)
                {
                    Assert.IsNotEmpty(spec.availableLengthsMm, spec.name);
                    CollectionAssert.IsOrdered(spec.availableLengthsMm, spec.name);
                }
            }
        }
    }
}
