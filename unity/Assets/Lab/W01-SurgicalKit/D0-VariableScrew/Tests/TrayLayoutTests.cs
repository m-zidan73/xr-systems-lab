using Lab.W01.VariableScrew.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lab.W01.VariableScrew.Tests
{
    public class TrayLayoutTests
    {
        const float Tolerance = 1e-5f;

        [Test]
        public void HeadDiameter_MatchesTheAuthoredMesh()
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(ScrewPaths.MeshPath);
            Assert.AreEqual(mesh.bounds.size.x * 1000f, ScrewUnits.HeadDiameterMm(ScrewUnits.MeshNominalDiameterMm), 0.02f);
        }

        [Test]
        public void HeadDiameter_ScalesWithTheScrew_AndZeroUsesTheMeshWidth()
        {
            var nominal = ScrewUnits.HeadDiameterMm(ScrewUnits.MeshNominalDiameterMm);
            Assert.AreEqual(nominal * 2f, ScrewUnits.HeadDiameterMm(7f), Tolerance);
            Assert.AreEqual(nominal, ScrewUnits.HeadDiameterMm(0f), Tolerance);
        }

        [Test]
        public void Centres_LeaveTheSameGapBetweenHeadsOfAnyWidth()
        {
            var diameters = new[] { 2.2f, 7f, 3.5f };
            var centres = TrayLayout.Centres(diameters);
            for (var i = 1; i < centres.Length; i++)
            {
                var halfWidths = ScrewUnits.MmWorld(ScrewUnits.HeadDiameterMm(diameters[i - 1]) + ScrewUnits.HeadDiameterMm(diameters[i])) * 0.5f;
                Assert.AreEqual(ScrewUnits.MmWorld(2f), centres[i] - centres[i - 1] - halfWidths, Tolerance);
            }
        }

        [Test]
        public void Centres_CentreTheRowOnZero()
        {
            var diameters = new[] { 2.2f, 7f, 3.5f };
            var centres = TrayLayout.Centres(diameters);
            var first = centres[0] - ScrewUnits.MmWorld(ScrewUnits.HeadDiameterMm(diameters[0])) * 0.5f;
            var last = centres[2] + ScrewUnits.MmWorld(ScrewUnits.HeadDiameterMm(diameters[2])) * 0.5f;
            Assert.AreEqual(0f, first + last, Tolerance);
        }

        [Test]
        public void Centres_WithNoScrews_IsEmpty()
        {
            Assert.IsEmpty(TrayLayout.Centres(new float[0]));
        }
    }
}
