using System.Linq;
using Lab.W01.VariableScrew.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lab.W01.VariableScrew.Tests
{
    /// <summary>Screws now differ in width, so the demo stacks have to leave room for the widest heads.</summary>
    public class DemoTrayFitTests
    {
        Scene _scene;

        void Open(string path) => _scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

        [TearDown]
        public void CloseScene() => EditorSceneManager.CloseScene(_scene, true);

        // Only the shaft mesh: the helix ribbon's bounds describe its unwrapped strip, not the screw.
        Bounds[] ScrewBounds()
        {
            var shaftMesh = AssetDatabase.LoadAssetAtPath<Mesh>(ScrewPaths.MeshPath);
            return _scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<MeshFilter>(true))
                .Where(filter => filter.sharedMesh == shaftMesh)
                .Select(filter => filter.GetComponent<Renderer>().bounds)
                .ToArray();
        }

        [TestCase(ScrewPaths.ScenePath)]
        [TestCase(ScrewPaths.HelixScenePath)]
        public void NoTwoScrewsOverlap(string scenePath)
        {
            Open(scenePath);
            var bounds = ScrewBounds();
            Assert.AreEqual(10, bounds.Length);
            for (var a = 0; a < bounds.Length; a++)
                for (var b = a + 1; b < bounds.Length; b++)
                    Assert.IsFalse(bounds[a].Intersects(bounds[b]), $"screws {a} and {b} overlap");
        }

        [TestCase(ScrewPaths.ScenePath)]
        [TestCase(ScrewPaths.HelixScenePath)]
        public void EveryScrewStartsInView(string scenePath)
        {
            Open(scenePath);
            var camera = _scene.GetRootGameObjects().Select(root => root.GetComponent<Camera>()).First(found => found != null);
            foreach (var bounds in ScrewBounds())
            {
                var top = camera.WorldToViewportPoint(new Vector3(bounds.center.x, bounds.max.y, bounds.center.z)).y;
                var bottom = camera.WorldToViewportPoint(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z)).y;
                Assert.LessOrEqual(top, 1f);
                Assert.GreaterOrEqual(bottom, 0f);
            }
        }
    }
}
