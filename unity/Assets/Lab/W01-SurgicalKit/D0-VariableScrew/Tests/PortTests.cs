using System.Linq;
using System.Reflection;
using Lab.Core;
using Lab.W01.VariableScrew.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lab.W01.VariableScrew.Tests
{
    /// <summary>Checks the assets arrived intact: paths, prefabs, scenes, materials and data.</summary>
    public class PortTests
    {
        const int ScrewsInEachScene = 10;
        const float DemoStartDistance = 0.12f;

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
