using NUnit.Framework;
using UnityEngine;

namespace Lab.Core.Tests
{
    public class OrbitPanTests
    {
        const float Tolerance = 1e-5f;
        const float FieldOfView = 60f;
        const float ScreenHeight = 1000f;

        static float MetresPerPixel(float distance) =>
            2f * distance * Mathf.Tan(FieldOfView * 0.5f * Mathf.Deg2Rad) / ScreenHeight;

        static Vector3 Pan(Vector3 target, Quaternion rotation, Vector2 delta, float distance) =>
            OrbitMath.Pan(target, rotation, delta, distance, FieldOfView, ScreenHeight);

        static void AssertClose(Vector3 expected, Vector3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, Tolerance);
            Assert.AreEqual(expected.y, actual.y, Tolerance);
            Assert.AreEqual(expected.z, actual.z, Tolerance);
        }

        [Test]
        public void Pan_WithNoMovement_KeepsThePoint()
        {
            var target = new Vector3(1f, 2f, 3f);
            AssertClose(target, Pan(target, Quaternion.identity, Vector2.zero, 0.5f));
        }

        [Test]
        public void Pan_DraggingRight_MovesThePointLeft_SoTheSceneFollowsTheMouse()
        {
            var moved = Pan(Vector3.zero, Quaternion.identity, new Vector2(100f, 0f), 1f);
            AssertClose(new Vector3(-100f * MetresPerPixel(1f), 0f, 0f), moved);
        }

        [Test]
        public void Pan_DraggingUp_MovesThePointDown()
        {
            var moved = Pan(Vector3.zero, Quaternion.identity, new Vector2(0f, 100f), 1f);
            AssertClose(new Vector3(0f, -100f * MetresPerPixel(1f), 0f), moved);
        }

        [Test]
        public void Pan_MovesLessWhenZoomedIn()
        {
            var far = Pan(Vector3.zero, Quaternion.identity, new Vector2(100f, 0f), 1f);
            var near = Pan(Vector3.zero, Quaternion.identity, new Vector2(100f, 0f), 0.1f);
            Assert.AreEqual(far.x * 0.1f, near.x, Tolerance);
        }

        [Test]
        public void Pan_FollowsTheCameraRotation()
        {
            var moved = Pan(Vector3.zero, Quaternion.Euler(0f, 90f, 0f), new Vector2(100f, 0f), 1f);
            AssertClose(new Vector3(0f, 0f, 100f * MetresPerPixel(1f)), moved);
        }

        [Test]
        public void Pan_WithNoScreenHeight_StaysFinite()
        {
            var moved = OrbitMath.Pan(Vector3.zero, Quaternion.identity, new Vector2(100f, 100f), 1f, FieldOfView, 0f);
            Assert.IsFalse(float.IsInfinity(moved.x) || float.IsNaN(moved.x));
        }
    }
}
