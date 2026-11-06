using System.Linq;
using Lab.Core;
using Lab.W01.ColourCodedHoles.EditorTools;
using Lab.W01.VariableScrew;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Lab.W01.ColourCodedHoles.Tests
{
    public class DemoSceneTests
    {
        Scene _scene;

        [SetUp]
        public void OpenScene() => _scene = EditorSceneManager.OpenScene(HolePaths.ScenePath, OpenSceneMode.Additive);

        [TearDown]
        public void CloseScene() => EditorSceneManager.CloseScene(_scene, true);

        T[] InScene<T>() where T : Component =>
            _scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

        static ImplantCatalog CatalogA => AssetDatabase.LoadAssetAtPath<ImplantCatalog>(HolePaths.CatalogAPath);
        static ImplantCatalog CatalogB => AssetDatabase.LoadAssetAtPath<ImplantCatalog>(HolePaths.CatalogBPath);

        [Test]
        public void Scene_HasFourHolesThatTakeDrops()
        {
            var holes = InScene<Hole>();
            CollectionAssert.AreEquivalent(new[] { 2.7f, 3.5f, 4.0f, 5.0f }, holes.Select(h => h.DiameterMm));
            foreach (var hole in holes)
            {
                Assert.IsNotNull(hole.GetComponent<HoleDropTarget>(), hole.name);
                Assert.AreSame(CatalogA, hole.Catalog, hole.name);
                Assert.IsNotNull(hole.Ring, hole.name);
            }
        }

        [Test]
        public void Scene_HasATrayOfGrabbableScrewsFromTheFirstCatalog()
        {
            var switcher = InScene<CatalogSwitcher>().Single();
            Assert.AreEqual(CatalogA.screws.Length, switcher.TrayScrews.Count);
            for (var i = 0; i < switcher.TrayScrews.Count; i++)
            {
                var screw = switcher.TrayScrews[i];
                Assert.AreSame(CatalogA.screws[i], screw.Spec, screw.name);
                Assert.IsNotNull(screw.GetComponent<Grabbable>(), screw.name);
                Assert.IsNotNull(screw.GetComponent<Collider>(), screw.name);
            }
        }

        [Test]
        public void Scene_SwitcherKnowsBothCatalogsAndAllHoles()
        {
            var switcher = InScene<CatalogSwitcher>().Single();
            CollectionAssert.AreEqual(new[] { CatalogA, CatalogB }, switcher.Catalogs);
            Assert.AreEqual(4, switcher.Holes.Count);
        }

        [Test]
        public void Scene_EveryRendererHasAMaterial()
        {
            foreach (var renderer in InScene<Renderer>())
                Assert.IsTrue(renderer.sharedMaterials.Length > 0 && renderer.sharedMaterials.All(m => m != null), renderer.name);
        }

        [Test]
        public void Scene_CameraOrbitsAndInteracts()
        {
            var camera = InScene<Camera>().Single(c => c.name == "Main Camera");
            Assert.IsNotNull(camera.GetComponent<OrbitCamera>());
            Assert.IsNotNull(camera.GetComponent<MouseInteractor>());
        }

        [Test]
        public void Scene_CKeySwitchesTheCatalog()
        {
            var key = InScene<KeyAction>().Single();
            Assert.AreEqual(Key.C, key.Key);
            Assert.AreEqual(1, key.OnPressed.GetPersistentEventCount());
            Assert.IsInstanceOf<CatalogSwitcher>(key.OnPressed.GetPersistentTarget(0));
            Assert.AreEqual(nameof(CatalogSwitcher.Next), key.OnPressed.GetPersistentMethodName(0));
        }

        [Test]
        public void Scene_StatusLineExplainsTheControls()
        {
            var status = InScene<StatusLine>().Single();
            StringAssert.Contains("Drag", status.Hint);
            StringAssert.Contains("C ", status.Hint);
            Assert.IsNotNull(InScene<HoleFeedback>().SingleOrDefault());
        }
    }

    public class ScrewHighlightTests
    {
        [Test]
        public void ScrewShaders_SupportTheHoverHighlight()
        {
            foreach (var name in new[] { "ScrewSystem/Screw", "ScrewSystem/ScrewHelix" })
                Assert.GreaterOrEqual(Shader.Find(name).FindPropertyIndex("_Highlight"), 0, name);
        }
    }
}
