using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lab.Core.Tests
{
    public class DemoTemplateTests
    {
        const string ScenePath = "Assets/Lab/_Core/DemoTemplate.unity";

        Scene _scene;

        [SetUp]
        public void OpenScene()
        {
            _scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        }

        [TearDown]
        public void CloseScene()
        {
            EditorSceneManager.CloseScene(_scene, true);
        }

        T[] FindInScene<T>() where T : Component =>
            _scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

        [Test]
        public void Template_HasOrbitCameraWithSmallNearClip()
        {
            var cameras = FindInScene<Camera>();
            Assert.AreEqual(1, cameras.Length);
            var orbit = cameras[0].GetComponent<OrbitCamera>();
            Assert.IsNotNull(orbit);
            Assert.LessOrEqual(orbit.NearClip, 0.001f);
            Assert.LessOrEqual(cameras[0].nearClipPlane, 0.0101f, "Small parts must show before Play too");
            Assert.AreEqual(0.65f, orbit.StartDistance, 1e-5f);
            Assert.AreEqual("MainCamera", cameras[0].tag);
        }

        [Test]
        public void OrbitCamera_AppliesItsNearClipToTheCamera()
        {
            var go = new GameObject("Near clip test");
            try
            {
                var camera = go.AddComponent<Camera>();
                var orbit = go.AddComponent<OrbitCamera>();
                orbit.ApplyNearClip();
                Assert.AreEqual(orbit.NearClip, camera.nearClipPlane, 1e-6f);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Template_HasThreeUnshadowedDirectionalLightsAndFloor()
        {
            var lights = FindInScene<Light>();
            CollectionAssert.AreEquivalent(new[] { "Key Light", "Fill Light", "Rim Light" }, lights.Select(l => l.name));
            foreach (var light in lights)
            {
                Assert.AreEqual(LightType.Directional, light.type, light.name);
                Assert.AreEqual(LightShadows.None, light.shadows, light.name);
            }
            Assert.IsTrue(_scene.GetRootGameObjects().Any(root => root.name == "Floor"));
        }

        [Test]
        public void Template_FloorIsDarkAndMatte_SoPartsStandOut()
        {
            var floor = _scene.GetRootGameObjects().Single(root => root.name == "Floor").GetComponent<Renderer>().sharedMaterial;
            Assert.LessOrEqual(floor.GetFloat("_Smoothness"), 0.2f);
            Assert.LessOrEqual(floor.GetColor("_BaseColor").grayscale, 0.3f);
        }
    }
}
