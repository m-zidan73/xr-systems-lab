using System.Linq;
using Lab.Core;
using Lab.W01.SizingStation.EditorTools;
using Lab.W01.VariableScrew;
using Lab.W01.VariableScrew.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace Lab.W01.SizingStation.Tests
{
    public class DemoSceneTests
    {
        Scene _scene;

        [SetUp]
        public void OpenScene() => _scene = EditorSceneManager.OpenScene(SizingPaths.ScenePath, OpenSceneMode.Additive);

        [TearDown]
        public void CloseScene() => EditorSceneManager.CloseScene(_scene, true);

        T[] InScene<T>() where T : Component =>
            _scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

        ScrewSizer Sizer => InScene<ScrewSizer>().Single();
        ScrewStation Station => InScene<ScrewStation>().Single();

        void AssertKey(Key key, Object target, string method)
        {
            var action = InScene<KeyAction>().Single(k => k.Key == key);
            Assert.AreEqual(1, action.OnPressed.GetPersistentEventCount(), $"{key}");
            Assert.AreSame(target, action.OnPressed.GetPersistentTarget(0), $"{key}");
            Assert.AreEqual(method, action.OnPressed.GetPersistentMethodName(0), $"{key}");
        }

        [Test]
        public void TheSizer_HasBothCatalogs_AndSizesTheScrewInTheHolder()
        {
            var wiring = new SerializedObject(Sizer);
            var catalogs = wiring.FindProperty("catalogs");
            Assert.AreEqual(2, catalogs.arraySize);
            Assert.AreEqual(SizingPaths.CatalogAPath, AssetDatabase.GetAssetPath(catalogs.GetArrayElementAtIndex(0).objectReferenceValue));
            Assert.AreEqual(SizingPaths.CatalogBPath, AssetDatabase.GetAssetPath(catalogs.GetArrayElementAtIndex(1).objectReferenceValue));

            var screw = InScene<Screw>().Single();
            Assert.AreSame(screw, wiring.FindProperty("screw").objectReferenceValue);
            Assert.AreSame(screw, Station.Current);
            Assert.AreEqual(3.5f, screw.Spec.diameterMm, 1e-4f);
            Assert.AreEqual(20f, screw.LengthMm, 1e-4f);
        }

        [Test]
        public void TheStation_StartsNewScrewsFromTheSharedPrefab()
        {
            var wiring = new SerializedObject(Station);
            Assert.AreSame(Sizer, wiring.FindProperty("sizer").objectReferenceValue);
            Assert.AreEqual(ScrewPaths.PrefabPath, AssetDatabase.GetAssetPath(wiring.FindProperty("screwPrefab").objectReferenceValue));
            Assert.IsNotNull(wiring.FindProperty("cradle").objectReferenceValue);
            Assert.IsNotNull(wiring.FindProperty("rack").objectReferenceValue);
        }

        [Test]
        public void TheScrew_LiesAlongTheScale_FromItsZeroMark()
        {
            var screw = InScene<Screw>().Single();
            var zero = _scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>()).Single(t => t.name == "Mark 0 mm");
            var twenty = _scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>()).Single(t => t.name == "Mark 20 mm");

            Assert.AreEqual(zero.position.x, screw.transform.position.x, 1e-5f);
            Assert.AreEqual(twenty.position.x, screw.TipPosition.x, 1e-5f, "A 20 mm screw's tip reads 20 on the scale.");
            Assert.AreEqual(0.02f, twenty.position.x - zero.position.x, 1e-5f);
        }

        [Test]
        public void TheScale_ReachesTheLongestScrewEitherCatalogShips()
        {
            var longest = new[] { SizingPaths.CatalogAPath, SizingPaths.CatalogBPath }
                .Select(AssetDatabase.LoadAssetAtPath<ImplantCatalog>)
                .SelectMany(catalog => catalog.screws)
                .SelectMany(spec => spec.availableLengthsMm)
                .Max();
            var marks = _scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>())
                .Where(t => t.name.StartsWith("Mark ")).ToArray();
            var zero = marks.Single(t => t.name == "Mark 0 mm").position.x;
            var last = marks.Max(t => t.position.x);
            Assert.That((last - zero) * 1000f, Is.GreaterThanOrEqualTo(longest - 1e-3f), "The scale has a mark at or past the longest screw.");
            Assert.AreEqual(SizingDemoBuilder.ScaleLengthMm(
                AssetDatabase.LoadAssetAtPath<ImplantCatalog>(SizingPaths.CatalogAPath),
                AssetDatabase.LoadAssetAtPath<ImplantCatalog>(SizingPaths.CatalogBPath)), (last - zero) * 1000f, 1e-2f);
        }

        [Test]
        public void EveryTenOnTheScale_HasItsNumberUnderIt()
        {
            var all = _scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>()).ToArray();
            var numbers = InScene<UnityEngine.UI.Text>().Where(text => text.name.StartsWith("Number ")).ToArray();
            Assert.That(numbers.Length, Is.GreaterThanOrEqualTo(7));
            foreach (var number in numbers)
            {
                var mark = all.Single(t => t.name == $"Mark {number.text} mm");
                Assert.AreEqual(mark.position.x, number.transform.position.x, 1e-5f, number.name);
                Assert.That(number.transform.position.z, Is.LessThan(mark.position.z), $"{number.name} sits in front of its mark.");
            }
        }

        [Test]
        public void ThePanel_SizesAgainstTheDemoDepth_AndTheReadoutUsesTheSameSizer()
        {
            var panel = InScene<SizePanel>().Single();
            Assert.AreSame(Sizer, panel.Sizer);
            Assert.AreEqual(SizingDemoBuilder.DemoDepthMm, panel.DepthMm, 1e-4f);
            Assert.AreSame(Sizer, new SerializedObject(InScene<MemoryReadout>().Single()).FindProperty("sizer").objectReferenceValue);
        }

        [Test]
        public void TheKeys_AreWired()
        {
            AssertKey(Key.Enter, Station, nameof(ScrewStation.Keep));
            AssertKey(Key.R, Station, nameof(ScrewStation.Restart));
            AssertKey(Key.C, Sizer, nameof(ScrewSizer.NextCatalog));
            AssertKey(Key.M, InScene<MemoryReadout>().Single(), nameof(MemoryReadout.ToggleComparison));
            AssertKey(Key.RightArrow, Sizer, nameof(ScrewSizer.StepLength));
            AssertKey(Key.LeftArrow, Sizer, nameof(ScrewSizer.StepLength));
            AssertKey(Key.UpArrow, Sizer, nameof(ScrewSizer.StepDiameter));
            AssertKey(Key.DownArrow, Sizer, nameof(ScrewSizer.StepDiameter));
        }

        [Test]
        public void TheUi_UsesTheNewInputSystem_AndTheHintNamesTheKeys()
        {
            Assert.AreEqual(1, InScene<EventSystem>().Length);
            Assert.AreEqual(1, InScene<InputSystemUIInputModule>().Length);
            var hint = InScene<StatusLine>().Single().Hint;
            foreach (var word in new[] { "Enter", "M compares", "C switches", "R starts again" })
                StringAssert.Contains(word, hint);
        }
    }
}
