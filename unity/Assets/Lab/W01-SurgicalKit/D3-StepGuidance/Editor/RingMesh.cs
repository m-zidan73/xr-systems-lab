using UnityEngine;

namespace Lab.W01.StepGuidance.EditorTools
{
    /// <summary>A flat ring in the XZ plane, facing up, with an outer radius of 1. Scale it to fit.</summary>
    public static class RingMesh
    {
        public static Mesh Build(float innerFraction, int segments)
        {
            var vertices = new Vector3[segments * 2];
            var normals = new Vector3[segments * 2];
            var uv = new Vector2[segments * 2];
            var triangles = new int[segments * 6];
            for (var i = 0; i < segments; i++)
            {
                var angle = i * 2f * Mathf.PI / segments;
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                vertices[i * 2] = direction;
                vertices[i * 2 + 1] = direction * innerFraction;
                normals[i * 2] = normals[i * 2 + 1] = Vector3.up;
                uv[i * 2] = new Vector2((float)i / segments, 1f);
                uv[i * 2 + 1] = new Vector2((float)i / segments, 0f);

                // Clockwise seen from above, which is Unity's front face.
                var outer = i * 2;
                var inner = outer + 1;
                var nextOuter = (outer + 2) % (segments * 2);
                var nextInner = nextOuter + 1;
                var t = i * 6;
                triangles[t] = outer;
                triangles[t + 1] = inner;
                triangles[t + 2] = nextOuter;
                triangles[t + 3] = inner;
                triangles[t + 4] = nextInner;
                triangles[t + 5] = nextOuter;
            }

            var mesh = new Mesh { name = "Highlight Ring", vertices = vertices, normals = normals, uv = uv, triangles = triangles };
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
