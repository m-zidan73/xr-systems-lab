using System.Linq;
using Lab.Core;
using Lab.W01.ColourCodedHoles.EditorTools;
using Lab.W01.VariableScrew;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lab.W01.ColourCodedHoles.Tests
{
    /// <summary>Screws differ in width now: the tray has to fit them, and the mouse and the holes must still work with them.</summary>
    public class TrayFitTests
    {
        const float Tolerance = 1e-4f;

        Scene _scene;
        CatalogSwitcher _switcher;
        Camera _camera;

        [SetUp]
        public void OpenScene()
        {
            _scene = EditorSceneManager.OpenScene(HolePaths.ScenePath, OpenSceneMode.Additive);
            var roots = _scene.GetRootGameObjects();
            _switcher = roots.SelectMany(root => root.GetComponentsInChildren<CatalogSwitcher>(true)).Single();
            _camera = roots.SelectMany(root => root.GetComponentsInChildren<Camera>(true)).Single();
        }

        [TearDown]
        public void CloseScene() => EditorSceneManager.CloseScene(_scene, true);

        Screw[] ActiveTray() => _switcher.TrayScrews.Where(screw => screw.gameObject.activeSelf).ToArray();

        static Bounds BoundsOf(Screw screw) => screw.GetComponent<Renderer>().bounds;

        [TestCase(0)]
        [TestCase(1)]
        public void TrayScrews_DoNotOverlap(int catalog)
        {
            _switcher.Show(catalog);
            var tray = ActiveTray();
            for (var a = 0; a < tray.Length; a++)
                for (var b = a + 1; b < tray.Length; b++)
                    Assert.IsFalse(BoundsOf(tray[a]).Intersects(BoundsOf(tray[b])), $"{tray[a].name} and {tray[b].name}");
        }

        [TestCase(0)]
        [TestCase(1)]
        public void TrayScrews_StayAboveTheFloor(int catalog)
        {
            _switcher.Show(catalog);
            foreach (var screw in ActiveTray())
                Assert.GreaterOrEqual(BoundsOf(screw).min.y, -Tolerance, screw.name);
        }

        [TestCase(0)]
        [TestCase(1)]
        public void TrayScrews_StayInView(int catalog)
        {
            _switcher.Show(catalog);
            foreach (var screw in ActiveTray())
            {
                var bounds = BoundsOf(screw);
                foreach (var corner in new[] { bounds.min, bounds.max })
                {
                    var view = _camera.WorldToViewportPoint(corner);
                    Assert.IsTrue(view.x >= 0f && view.x <= 1f && view.y >= 0f && view.y <= 1f, $"{screw.name}: {view}");
                }
            }
        }

        // The same ray the mouse uses to find a part under the cursor.
        [TestCase(0)]
        [TestCase(1)]
        public void EveryTrayScrew_CanBeHovered(int catalog)
        {
            _switcher.Show(catalog);
            Physics.SyncTransforms();
            foreach (var screw in ActiveTray())
            {
                var shaftMiddle = screw.transform.TransformPoint(new Vector3(0f, 0f, screw.LengthMm * 0.001f * 0.5f));
                var ray = new Ray(_camera.transform.position, (shaftMiddle - _camera.transform.position).normalized);
                Assert.IsTrue(Physics.Raycast(ray, out var hit, 5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore), screw.name);
                Assert.AreSame(screw.GetComponent<Grabbable>(), hit.collider.GetComponentInParent<Grabbable>(), screw.name);
            }
        }

        [Test]
        public void EveryHole_SeatsItsScrew_KeepingTheScrewsWidth()
        {
            _switcher.Show(0);
            var seated = new System.Collections.Generic.List<Bounds>();
            foreach (var hole in _switcher.Holes)
            {
                var screw = _switcher.TrayScrews.Single(candidate => Mathf.Approximately(candidate.Spec.diameterMm, hole.DiameterMm));
                var widthBefore = screw.transform.localScale;
                Assert.AreEqual(AcceptResult.Accepted, hole.TryAccept(screw), hole.name);
                Assert.AreEqual(widthBefore, screw.transform.localScale, hole.name);
                Assert.AreEqual(0f, Vector3.Distance(hole.Seat.position, screw.transform.position), Tolerance, hole.name);
                var bounds = BoundsOf(screw);
                foreach (var other in seated) Assert.IsFalse(bounds.Intersects(other), $"{hole.name} touches a neighbour");
                seated.Add(bounds);
            }
        }

        [Test]
        public void ASeatedScrew_CanBePickedUpAgainAndGoHome()
        {
            _switcher.Show(0);
            var hole = _switcher.Holes[3];
            var screw = _switcher.TrayScrews.Single(candidate => Mathf.Approximately(candidate.Spec.diameterMm, hole.DiameterMm));
            var item = screw.GetComponent<Grabbable>();
            item.SetHome();
            var home = screw.transform.position;

            Assert.IsTrue(hole.GetComponent<HoleDropTarget>().TryDrop(item));
            item.PickUp();
            Assert.IsNull(hole.Occupant);
            item.Drop(false);
            Assert.AreEqual(0f, Vector3.Distance(home, screw.transform.position), Tolerance);
        }
    }
}
