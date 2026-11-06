using NUnit.Framework;
using UnityEngine;

namespace Lab.Core.Tests
{
    public class DropTargetPickerTests
    {
        const float MaxPixels = 48f;

        [Test]
        public void Closest_PicksTheNearestTargetInRange()
        {
            var points = new[] { new Vector3(100f, 100f, 1f), new Vector3(130f, 100f, 1f) };
            Assert.AreEqual(1, DropTargetPicker.Closest(points, new Vector2(125f, 100f), MaxPixels));
        }

        [Test]
        public void Closest_NothingInRange_ReturnsMinusOne()
        {
            var points = new[] { new Vector3(100f, 100f, 1f) };
            Assert.AreEqual(-1, DropTargetPicker.Closest(points, new Vector2(500f, 500f), MaxPixels));
        }

        [Test]
        public void Closest_IgnoresTargetsBehindTheCamera()
        {
            var points = new[] { new Vector3(100f, 100f, -1f) };
            Assert.AreEqual(-1, DropTargetPicker.Closest(points, new Vector2(100f, 100f), MaxPixels));
        }

        [Test]
        public void Closest_WithNoTargets_ReturnsMinusOne()
        {
            Assert.AreEqual(-1, DropTargetPicker.Closest(new Vector3[0], Vector2.zero, MaxPixels));
        }
    }
}
