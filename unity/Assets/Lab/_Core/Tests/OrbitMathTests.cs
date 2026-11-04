using NUnit.Framework;
using UnityEngine;

namespace Lab.Core.Tests
{
    public class OrbitMathTests
    {
        const float Tolerance = 1e-5f;

        [Test]
        public void Position_AtZeroAngles_IsBehindTarget()
        {
            var position = OrbitMath.Position(Vector3.zero, 0f, 0f, 0.12f);
            Assert.AreEqual(0f, position.x, Tolerance);
            Assert.AreEqual(0f, position.y, Tolerance);
            Assert.AreEqual(-0.12f, position.z, Tolerance);
        }

        [Test]
        public void Position_KeepsTheDistanceFromTarget()
        {
            var target = new Vector3(1f, 2f, 3f);
            var position = OrbitMath.Position(target, 37f, -25f, 0.5f);
            Assert.AreEqual(0.5f, Vector3.Distance(target, position), Tolerance);
        }

        [Test]
        public void Rotate_MovesYawAndPitchWithTheMouse()
        {
            var (yaw, pitch) = OrbitMath.Rotate(10f, 5f, new Vector2(10f, 10f), 0.2f);
            Assert.AreEqual(12f, yaw, Tolerance);
            Assert.AreEqual(3f, pitch, Tolerance);
        }

        [Test]
        public void Rotate_ClampsPitchToLimit()
        {
            var (_, up) = OrbitMath.Rotate(0f, 0f, new Vector2(0f, 100000f), 0.2f);
            var (_, down) = OrbitMath.Rotate(0f, 0f, new Vector2(0f, -100000f), 0.2f);
            Assert.AreEqual(-OrbitMath.PitchLimitDeg, up, Tolerance);
            Assert.AreEqual(OrbitMath.PitchLimitDeg, down, Tolerance);
            Assert.AreEqual(85f, OrbitMath.PitchLimitDeg, Tolerance);
        }

        [Test]
        public void Zoom_ScrollUpMovesCloser_ByOneStep()
        {
            Assert.AreEqual(0.9f, OrbitMath.Zoom(1f, 1f, 0.02f, 3f), Tolerance);
            Assert.AreEqual(0.9f, OrbitMath.Zoom(1f, 120f, 0.02f, 3f), Tolerance);
            Assert.AreEqual(1.1f, OrbitMath.Zoom(1f, -1f, 0.02f, 3f), Tolerance);
            Assert.AreEqual(1f, OrbitMath.Zoom(1f, 0f, 0.02f, 3f), Tolerance);
        }

        [Test]
        public void Zoom_StaysInRange()
        {
            Assert.AreEqual(0.02f, OrbitMath.Zoom(0.02f, 1f, 0.02f, 3f), Tolerance);
            Assert.AreEqual(3f, OrbitMath.Zoom(3f, -1f, 0.02f, 3f), Tolerance);
        }
    }
}
