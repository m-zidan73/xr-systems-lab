using System.Globalization;
using System.Linq;
using Lab.Core;
using Lab.W01.ColourCodedHoles;
using Lab.W01.StepGuidance.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Lab.W01.StepGuidance.Tests
{
    public class DemoSceneTests
    {
        Scene _scene;

        [SetUp]
        public void OpenScene() => _scene = EditorSceneManager.OpenScene(StepPaths.ScenePath, OpenSceneMode.Additive);

        [TearDown]
        public void CloseScene() => EditorSceneManager.CloseScene(_scene, true);

        T[] InScene<T>() where T : Component =>
            _scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

        StepGuide Guide => InScene<StepGuide>().Single();

        void AssertKey(Key key, string method)
        {
            var action = InScene<KeyAction>().Single(k => k.Key == key);
            Assert.AreEqual(1, action.OnPressed.GetPersistentEventCount(), $"{key}");
            Assert.AreSame(Guide, action.OnPressed.GetPersistentTarget(0), $"{key}");
            Assert.AreEqual(method, action.OnPressed.GetPersistentMethodName(0), $"{key}");
        }

        [Test]
        public void Procedure_StartsWithACheck_ThenOneScrewPerHole()
        {
            var steps = Guide.Steps;
            Assert.AreEqual(5, steps.Count);
            Assert.AreEqual(StepFinish.Confirmed, steps[0].finishesOn);
            StringAssert.Contains("colour", steps[0].instruction);

            var screwSteps = steps.Skip(1).ToArray();
            CollectionAssert.AreEquivalent(InScene<Hole>(), screwSteps.Select(s => s.hole), "Every hole on the plate is used once.");
            foreach (var step in screwSteps)
            {
                Assert.AreEqual(StepFinish.ScrewSeated, step.finishesOn);
                Assert.IsNotNull(step.screw, step.instruction);
                Assert.AreEqual(step.hole.DiameterMm, step.screw.Spec.diameterMm, 1e-4f, "The ringed screw fits the ringed hole.");
                StringAssert.Contains(step.hole.DiameterMm.ToString("0.#", CultureInfo.InvariantCulture) + " mm hole", step.instruction);
            }
            var order = screwSteps.Select(s => s.hole.DiameterMm).ToArray();
            CollectionAssert.AreNotEqual(order.OrderBy(d => d).ToArray(), order, "The order isn't simply left to right.");
        }

        [Test]
        public void Guide_WatchesEveryHoleOnThePlate()
        {
            var holes = new SerializedObject(Guide).FindProperty("holes");
            var watched = Enumerable.Range(0, holes.arraySize).Select(i => holes.GetArrayElementAtIndex(i).objectReferenceValue);
            CollectionAssert.AreEquivalent(InScene<Hole>(), watched);
        }

        [Test]
        public void DayOnesCatalogKeyAndHoleMessages_AreGone()
        {
            Assert.IsEmpty(InScene<HoleFeedback>());
            Assert.IsFalse(InScene<KeyAction>().Any(k => k.Key == Key.C));
        }

        [Test]
        public void Space_Confirms_AndR_StartsAgain()
        {
            AssertKey(Key.Space, nameof(StepGuide.Confirm));
            AssertKey(Key.R, nameof(StepGuide.Restart));
        }

        [Test]
        public void Rings_AreWired_AndCatchNoClicks()
        {
            var highlighter = new SerializedObject(InScene<StepHighlighter>().Single());
            Assert.AreSame(Guide, highlighter.FindProperty("guide").objectReferenceValue);
            foreach (var field in new[] { "holeMarker", "screwMarker" })
            {
                var marker = (Renderer)highlighter.FindProperty(field).objectReferenceValue;
                Assert.IsNotNull(marker, field);
                Assert.IsNull(marker.GetComponent<Collider>(), "A ring must not get in the way of the mouse.");
                Assert.AreEqual(ShadowCastingMode.Off, marker.shadowCastingMode);
                Assert.AreEqual(StepPaths.GhostShaderName, marker.sharedMaterial.shader.name);
                Assert.IsNotNull(marker.GetComponent<MeshFilter>().sharedMesh);
            }
        }

        [Test]
        public void PanelFeedbackAndHint_AreWired()
        {
            Assert.AreSame(Guide, InScene<StepPanel>().Single().Guide);
            var feedback = new SerializedObject(InScene<StepFeedback>().Single());
            Assert.AreSame(Guide, feedback.FindProperty("guide").objectReferenceValue);
            Assert.AreSame(InScene<StatusLine>().Single(), feedback.FindProperty("status").objectReferenceValue);
            Assert.AreSame(InScene<Banner>().Single(), feedback.FindProperty("banner").objectReferenceValue);
            StringAssert.Contains("R to start again", feedback.FindProperty("afterDone").stringValue);

            var hint = InScene<StatusLine>().Single().Hint;
            StringAssert.Contains("Space", hint);
            StringAssert.Contains("R starts again", hint);
        }

        [Test]
        public void TheMouseStillCarriesScrewsToTheHoles()
        {
            Assert.AreEqual(1, InScene<MouseInteractor>().Length);
            Assert.AreEqual(4, InScene<HoleDropTarget>().Length);
            Assert.Greater(InScene<Grabbable>().Length, 4, "The tray holds more screws than there are holes.");
        }
    }
}
