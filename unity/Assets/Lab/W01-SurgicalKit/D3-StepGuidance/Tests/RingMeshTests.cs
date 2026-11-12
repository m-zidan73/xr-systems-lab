using Lab.W01.StepGuidance.EditorTools;
using NUnit.Framework;
using UnityEngine;

namespace Lab.W01.StepGuidance.Tests
{
    public class RingMeshTests
    {
        Mesh _mesh;

        [SetUp]
        public void Build() => _mesh = RingMesh.Build(0.7f, 48);

        [TearDown]
        public void Destroy() => Object.DestroyImmediate(_mesh);

        [Test]
        public void IsAFlatRing_WithAnOuterRadiusOfOne()
        {
            Assert.AreEqual(96, _mesh.vertexCount);
            foreach (var v in _mesh.vertices)
            {
                Assert.AreEqual(0f, v.y, 1e-6f);
                var radius = new Vector2(v.x, v.z).magnitude;
                Assert.That(Mathf.Abs(radius - 1f) < 1e-5f || Mathf.Abs(radius - 0.7f) < 1e-5f, $"radius {radius}");
            }
        }

        [Test]
        public void FacesUp()
        {
            var vertices = _mesh.vertices;
            var triangles = _mesh.triangles;
            Assert.AreEqual(48 * 6, triangles.Length);
            for (var i = 0; i < triangles.Length; i += 3)
            {
                var a = vertices[triangles[i]];
                var normal = Vector3.Cross(vertices[triangles[i + 1]] - a, vertices[triangles[i + 2]] - a);
                Assert.Greater(normal.y, 0f, $"Triangle {i / 3}");
            }
            foreach (var n in _mesh.normals) Assert.AreEqual(Vector3.up, n);
        }
    }
}
