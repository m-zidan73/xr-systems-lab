using System.Linq;
using Lab.Core;
using Lab.W01.AdjustablePlate;
using Lab.W01.ColourCodedHoles;
using Lab.W01.HoleTools.EditorTools;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Lab.W01.HoleTools.Tests
{
    public class DemoSceneTests
    {
        Scene _scene;

        [SetUp]
        public void OpenScene()
        {
            _scene = EditorSceneManager.OpenScene(ToolPaths.ScenePath, OpenSceneMode.Additive);
            Physics.SyncTransforms();
        }

        [TearDown]
        public void CloseScene() => EditorSceneManager.CloseScene(_scene, true);

        T[] InScene<T>() where T : Component =>
            _scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

        [Test]
        public void FourHoles_RideOnThePlate_EachWithOnePlaceToDrop()
        {
            var plate = InScene<PlateAdjuster>().Single();
            var sites = InScene<HoleSite>();
            Assert.AreEqual(4, sites.Length);
            foreach (var site in sites)
            {
                Assert.IsTrue(site.transform.IsChildOf(plate.transform), site.name);
                Assert.AreEqual(1, site.GetComponents<IDropTarget>().Length, site.name);
                Assert.IsNotNull(site.GetComponent<HoleSiteTarget>(), site.name);
                Assert.IsNotNull(site.Hole.ExpectedSpec, $"{site.name}: the catalog ships its diameter.");
                Assert.That(site.BitMm, Is.GreaterThan(0f).And.LessThan(site.Hole.DiameterMm), site.name);
                Assert.IsFalse(site.IsDrilled);
            }
        }

        [Test]
        public void ThePlate_StartsInPlace()
        {
            Assert.IsTrue(InScene<PlateAdjuster>().Single().IsOnTarget(PlateAxis.MoveX));
            Assert.IsEmpty(InScene<PlateTargetGhost>());
            Assert.IsEmpty(InScene<PlateFeedback>());
        }

        [Test]
        public void EveryHole_HasBoneUnderIt_AndTheDepthsDiffer()
        {
            var depths = InScene<HoleSite>().Select(site =>
            {
                Assert.IsTrue(site.TryProbe(out var depth, out _), $"{site.name}: no bone under it.");
                return depth;
            }).ToArray();

            foreach (var depth in depths) Assert.That(depth, Is.InRange(10f, 30f), "A depth the catalogs ship screws for.");
            for (var i = 0; i < depths.Length; i++)
                for (var j = i + 1; j < depths.Length; j++)
                    Assert.That(Mathf.Abs(depths[i] - depths[j]), Is.GreaterThan(0.3f), "Two holes read the same depth.");
        }

        [Test]
        public void MovingThePlate_ChangesADepth()
        {
            var plate = InScene<PlateAdjuster>().Single();
            var site = InScene<HoleSite>().First();
            site.TryProbe(out var before, out _);

            plate.transform.localPosition += new Vector3(0f, 0f, 0.003f);
            Physics.SyncTransforms();
            site.TryProbe(out var lifted, out _);

            Assert.AreEqual(before + 3f, lifted, 0.2f, "Lifting the plate 3 mm adds 3 mm to the span.");
        }

        [Test]
        public void EveryHole_HasALabel_ShowingOnlyItsDiameter()
        {
            var labels = InScene<HoleLabels>().Single();
            CollectionAssert.AreEquivalent(InScene<HoleSite>(), labels.Sites);
            foreach (var site in labels.Sites)
            {
                StringAssert.StartsWith("Ø ", HoleLabels.Text(site));
                StringAssert.DoesNotContain("mm", HoleLabels.Text(site));
            }
        }

        [Test]
        public void ADrillAndAGauge_CanBePickedUp()
        {
            var tools = InScene<HoleTool>();
            CollectionAssert.AreEquivalent(new[] { ToolKind.Drill, ToolKind.Gauge }, tools.Select(t => t.Kind));
            foreach (var tool in tools)
            {
                Assert.IsNotNull(tool.GetComponent<Grabbable>(), tool.name);
                Assert.AreEqual(1, tool.GetComponentsInChildren<Collider>().Length, $"{tool.name} has one collider to grab it by.");
            }
            Assert.AreEqual(1, InScene<MouseInteractor>().Length);
        }

        [Test]
        public void TheControls_AreWired()
        {
            var sites = InScene<HoleSites>().Single();
            CollectionAssert.AreEquivalent(InScene<HoleSiteTarget>(), sites.Targets);

            var restart = InScene<KeyAction>().Single(k => k.Key == Key.R);
            var methods = Enumerable.Range(0, restart.OnPressed.GetPersistentEventCount()).Select(restart.OnPressed.GetPersistentMethodName).ToArray();
            CollectionAssert.Contains(methods, nameof(HoleSites.ClearAll));
            CollectionAssert.Contains(methods, nameof(PlateAdjuster.ResetAll));

            StringAssert.Contains("drill", InScene<StatusLine>().Single().Hint.ToLowerInvariant());
        }
    }
}
