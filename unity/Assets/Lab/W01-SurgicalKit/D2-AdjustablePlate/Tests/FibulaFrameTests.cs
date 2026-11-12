using System.Collections.Generic;
using System.Linq;
using Lab.W01.AdjustablePlate.EditorTools;
using NUnit.Framework;
using UnityEngine;

namespace Lab.W01.AdjustablePlate.Tests
{
    public class FibulaFrameTests
    {
        // A thin bone 30 mm to the side of a thick one, both upright, like a fibula beside a tibia.
        static readonly Vector3 FibulaCentre = new Vector3(0.03f, 0f, 0f);
        const float FibulaRadius = 0.006f;
        const float FibulaBottom = 0.1f;
        const float Margin = 0.006f;
        const float Gap = 0.0004f;

        static List<Vector3> Cylinder(Vector3 centre, float radius, float bottom, float top)
        {
            var points = new List<Vector3>();
            for (var y = bottom; y <= top + 1e-6f; y += 0.002f)
            for (var i = 0; i < 24; i++)
            {
                var angle = i * Mathf.PI * 2f / 24f;
                points.Add(centre + new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius));
            }
            return points;
        }

        // A plate 100 mm long and 2 mm thick; bend > 0 tips its distal 20 mm towards -Z.
        static List<Vector3> Plate(float bend)
        {
            var points = new List<Vector3>();
            for (var y = -0.05f; y <= 0.05f + 1e-6f; y += 0.001f)
            {
                var drop = y < -0.03f ? bend * (-0.03f - y) / 0.02f : 0f;
                foreach (var x in new[] { -0.005f, 0.005f })
                foreach (var z in new[] { -0.001f, 0.001f })
                    points.Add(new Vector3(x, y, z - drop));
            }
            return points;
        }

        static FibulaFrame.Fit Fit(List<Vector3> plate) => FibulaFrame.Place(
            Cylinder(FibulaCentre, FibulaRadius, FibulaBottom, 0.25f),
            Cylinder(Vector3.zero, 0.015f, 0f, 0.25f),
            plate, Margin, Gap);

        [Test]
        public void Axis_RunsUpTheBone_AndLateralPointsAwayFromTheOtherBone()
        {
            var fit = Fit(Plate(0f));
            Assert.Greater(Vector3.Dot(fit.axis, Vector3.up), 0.999f);
            Assert.Greater(Vector3.Dot(fit.lateral, Vector3.right), 0.99f);
        }

        [Test]
        public void FlatPlate_RestsOnTheOuterSide_JustAboveTheTip()
        {
            var fit = Fit(Plate(0f));
            var expectedX = FibulaCentre.x + FibulaRadius + 0.001f + Gap;
            Assert.AreEqual(expectedX, fit.position.x, 0.0005f, "Touching the outer surface, plus the gap.");
            Assert.AreEqual(0f, fit.position.z, 0.0005f, "Centred across the bone.");
            Assert.AreEqual(FibulaBottom + Margin + 0.05f, fit.position.y, 0.001f, "The distal end sits the margin above the tip.");
            Assert.Greater(Vector3.Dot(fit.rotation * Vector3.up, Vector3.up), 0.999f, "The plate's length runs up the bone.");
        }

        [Test]
        public void BentPlate_LiesSoTheBendGoesAwayFromTheBone()
        {
            var plate = Plate(0.004f);
            var fit = Fit(plate);
            var world = plate.Select(p => fit.position + fit.rotation * p).ToList();
            float SpaceToBone(Vector3 p) => p.x - (FibulaCentre.x + FibulaRadius);
            var shaftGap = world.Where(p => p.y > FibulaBottom + Margin + 0.04f).Min(SpaceToBone);
            var headGap = world.Where(p => p.y < FibulaBottom + Margin + 0.01f).Min(SpaceToBone);
            Assert.Less(shaftGap, 0.002f, "The long shaft rests on the bone.");
            Assert.Greater(headGap, shaftGap, "The bent end lifts away instead of pushing the shaft off the bone.");
            Assert.GreaterOrEqual(world.Min(SpaceToBone), 0f, "No part of the plate is inside the bone.");
        }
    }
}
