using System.Collections.Generic;
using System.Linq;
using Lab.W01.VariableScrew.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lab.W01.VariableScrew.Tests
{
    /// <summary>The shader-thread screw has to show its diameter, the way the helix screw does.</summary>
    public class ScrewDiameterTests
    {
        const float Tolerance = 1e-5f;

        readonly List<Object> _made = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var thing in _made)
                if (thing != null) Object.DestroyImmediate(thing);
            _made.Clear();
        }

        ScrewSpec MakeSpec(float diameterMm)
        {
            var spec = ScriptableObject.CreateInstance<ScrewSpec>();
            spec.diameterMm = diameterMm;
            spec.availableLengthsMm = new[] { 10f, 20f, 30f };
            spec.threadPitchMm = 1.25f;
            _made.Add(spec);
            return spec;
        }

        Screw MakeScrew(ScrewSpec spec, float lengthMm = 30f)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScrewPaths.PrefabPath);
            var go = Object.Instantiate(prefab);
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            _made.Add(go);
            var screw = go.GetComponent<Screw>();
            screw.Set(spec, lengthMm);
            return screw;
        }

        static float RenderedWidth(Screw screw) => screw.GetComponent<Renderer>().bounds.size.x;

        static float ColliderWorldDiameter(Screw screw)
        {
            Physics.SyncTransforms();
            return screw.GetComponent<CapsuleCollider>().bounds.size.x;
        }

        static float ShaderValue(Screw screw, string property)
        {
            var block = new MaterialPropertyBlock();
            screw.GetComponent<Renderer>().GetPropertyBlock(block);
            return block.GetFloat(property);
        }

        [Test]
        public void RenderedWidth_FollowsTheSpecDiameter()
        {
            var nominal = RenderedWidth(MakeScrew(MakeSpec(3.5f)));
            Assert.AreEqual(nominal * 2f, RenderedWidth(MakeScrew(MakeSpec(7f))), Tolerance);
            Assert.AreEqual(nominal * (2.2f / 3.5f), RenderedWidth(MakeScrew(MakeSpec(2.2f))), Tolerance);
        }

        [TestCase(2.2f)]
        [TestCase(3.5f)]
        [TestCase(7f)]
        public void Collider_WorldDiameterEqualsTheSpecDiameter(float diameterMm)
        {
            Assert.AreEqual(diameterMm * 0.001f, ColliderWorldDiameter(MakeScrew(MakeSpec(diameterMm))), Tolerance);
        }

        [Test]
        public void LengthTipAndThreadSpacing_DoNotChangeWithDiameter()
        {
            var thin = MakeScrew(MakeSpec(2.2f));
            var wide = MakeScrew(MakeSpec(7f));

            Assert.AreEqual(ScrewUnits.WorldScale, thin.transform.localScale.z, Tolerance);
            Assert.AreEqual(ScrewUnits.WorldScale, wide.transform.localScale.z, Tolerance);
            Assert.AreEqual(0f, Vector3.Distance(thin.TipPosition, wide.TipPosition), Tolerance);
            Assert.AreEqual(0.03f, wide.TipPosition.z, Tolerance);
            foreach (var property in new[] { "_ThreadTiling", "_ThreadStarts", "_Stretch" })
                Assert.AreEqual(ShaderValue(thin, property), ShaderValue(wide, property), Tolerance, property);
        }

        [Test]
        public void ZeroDiameter_DrawsAtTheMeshsOwnWidth()
        {
            var screw = MakeScrew(MakeSpec(0f));
            Assert.AreEqual(ScrewUnits.WorldScale, screw.transform.localScale.x, Tolerance);
            Assert.AreEqual(ScrewUnits.WorldScale, screw.transform.localScale.y, Tolerance);
            Assert.Greater(ColliderWorldDiameter(screw), 0f);
        }

        [Test]
        public void MissingSpec_LeavesAUsableWidth()
        {
            var screw = MakeScrew(MakeSpec(7f));
            var before = screw.transform.localScale;
            Assert.DoesNotThrow(() => screw.Set(null, 20f));
            Assert.AreEqual(before, screw.transform.localScale);
        }

        [Test]
        public void NominalDiameter_MatchesTheAuthoredShaft()
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(ScrewPaths.MeshPath);
            var colours = mesh.colors;
            var shaft = mesh.vertices.Where((vertex, index) => colours[index].r > 0.5f).ToArray();
            Assert.IsNotEmpty(shaft);
            var diameterMm = shaft.Max(vertex => new Vector2(vertex.x, vertex.y).magnitude) * 2f * 1000f;
            Assert.AreEqual(ScrewUnits.MeshNominalDiameterMm, diameterMm, 0.02f, $"measured {diameterMm} mm");
        }
    }
}
