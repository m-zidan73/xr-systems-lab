using System.Linq;
using Lab.Core;
using Lab.W01.AdjustablePlate.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace Lab.W01.AdjustablePlate.Tests
{
    public class DemoSceneTests
    {
        Scene _scene;

        [SetUp]
        public void OpenScene() => _scene = EditorSceneManager.OpenScene(PlatePaths.ScenePath, OpenSceneMode.Additive);

        [TearDown]
        public void CloseScene() => EditorSceneManager.CloseScene(_scene, true);

        T[] InScene<T>() where T : Component =>
            _scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

        PlateAdjuster Adjuster => InScene<PlateAdjuster>().Single();

        static Bounds RenderBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            return bounds;
        }

        [Test]
        public void Plate_IsRealSize_AndStartsOutOfPlace()
        {
            var adjuster = Adjuster;
            var filter = adjuster.GetComponentInChildren<MeshFilter>();
            var size = filter.sharedMesh.bounds.size;
            Assert.AreEqual(0.1f, Mathf.Max(size.x, size.y, size.z), 0.002f, "The plate is 100 mm long.");
            Assert.AreEqual(1f, filter.transform.lossyScale.magnitude / Vector3.one.magnitude, 0.001f, "Nothing rescales it in the scene.");
            Assert.IsFalse(adjuster.IsPlaced);
        }

        [Test]
        public void Ghost_SitsWhereTheTargetOffsetsPutThePlate()
        {
            var adjuster = Adjuster;
            var ghost = InScene<PlateTargetGhost>().Single();
            Assert.AreSame(adjuster.transform.parent, ghost.transform.parent);
            var (position, rotation) = adjuster.TargetPose;
            Assert.That(Vector3.Distance(position, ghost.transform.localPosition), Is.LessThan(1e-5f));
            Assert.That(Quaternion.Angle(rotation, ghost.transform.localRotation), Is.LessThan(0.01f));

            var wiring = new SerializedObject(ghost);
            Assert.AreSame(adjuster, wiring.FindProperty("adjuster").objectReferenceValue);
        }

        [Test]
        public void Target_IsOnTheFibula()
        {
            var fibula = InScene<MeshFilter>().Single(m => m.name == PlatePaths.FibulaName);
            var ghost = InScene<PlateTargetGhost>().Single();
            var bounds = fibula.GetComponent<Renderer>().bounds;
            bounds.Expand(0.02f);
            Assert.IsTrue(bounds.Contains(ghost.transform.position), "The plate's target is right beside the fibula.");
        }

        [Test]
        public void Panel_HasARowPerAxis_DrivingTheAdjuster()
        {
            var panel = InScene<AxisPanel>().Single();
            var wiring = new SerializedObject(panel);
            Assert.AreSame(Adjuster, wiring.FindProperty("target").objectReferenceValue);
            Assert.AreEqual(PlateAxes.Count, panel.RowCount);
            for (var i = 0; i < panel.RowCount; i++)
            {
                var row = panel.RowAt(i);
                Assert.IsNotNull(row.slider, $"Row {i}");
                Assert.AreEqual(Adjuster.Settings((PlateAxis)i).label, row.label.text);
                Assert.AreEqual(Adjuster.Settings((PlateAxis)i).min, row.slider.minValue);
                Assert.AreEqual(Adjuster.Settings((PlateAxis)i).max, row.slider.maxValue);
            }
            Assert.IsNotNull(panel.ResetButton);
        }

        [Test]
        public void Ui_UsesTheNewInputSystemModule()
        {
            var eventSystem = InScene<EventSystem>().Single();
            Assert.IsNotNull(eventSystem.GetComponent<InputSystemUIInputModule>());
            Assert.IsNull(eventSystem.GetComponent<StandaloneInputModule>());
        }

        [Test]
        public void RKey_ResetsThePlate()
        {
            var key = InScene<KeyAction>().Single();
            Assert.AreEqual(Key.R, key.Key);
            Assert.AreEqual(1, key.OnPressed.GetPersistentEventCount());
            Assert.AreSame(Adjuster, key.OnPressed.GetPersistentTarget(0));
            Assert.AreEqual(nameof(PlateAdjuster.ResetAll), key.OnPressed.GetPersistentMethodName(0));
        }

        [Test]
        public void Feedback_IsWiredToTheStatusLine()
        {
            var feedback = new SerializedObject(InScene<PlateFeedback>().Single());
            Assert.AreSame(Adjuster, feedback.FindProperty("adjuster").objectReferenceValue);
            Assert.AreSame(InScene<StatusLine>().Single(), feedback.FindProperty("status").objectReferenceValue);
            Assert.AreSame(InScene<Banner>().Single(), feedback.FindProperty("banner").objectReferenceValue);
            StringAssert.Contains("arrow keys", InScene<StatusLine>().Single().Hint);
        }

        [Test]
        public void Camera_StartsLookingAtThePlate()
        {
            var camera = InScene<Camera>().Single();
            var viewport = camera.WorldToViewportPoint(RenderBounds(Adjuster.gameObject).center);
            Assert.That(viewport.x, Is.InRange(0.25f, 0.75f));
            Assert.That(viewport.y, Is.InRange(0.25f, 0.75f));
            Assert.Greater(viewport.z, 0f);
        }

        [Test]
        public void Foot_IsReducedToAReasonableBudget()
        {
            var triangles = InScene<MeshFilter>()
                .Where(m => m.GetComponentInParent<PlateAdjuster>() == null && m.GetComponentInParent<PlateTargetGhost>() == null)
                .Sum(m => m.sharedMesh.triangles.Length / 3);
            Assert.Less(triangles, 60000);
        }
    }
}
