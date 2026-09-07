using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal static class SurfaceShieldMeshCache
    {
        private const int FallbackMeshSegments = 64;

        private static Mesh sharedFallbackDiscMesh;
        private static Mesh sharedFallbackRingMesh;
        private static Mesh sharedFallbackThinRingMesh;
        private static Mesh sharedFallbackHexMesh;
        private static Mesh sharedFallbackShardMesh;

        internal static Mesh GetShieldMesh(ref Mesh shieldMesh)
        {
            if (shieldMesh != null)
            {
                return shieldMesh;
            }

            Mesh mesh = new Mesh();
            mesh.name = "CeleTech_SurfaceShieldPlane";
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, 0f, -0.5f),
                new Vector3(-0.5f, 0f, 0.5f),
                new Vector3(0.5f, 0f, 0.5f),
                new Vector3(0.5f, 0f, -0.5f)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 0f)
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateNormals();
            shieldMesh = mesh;
            return shieldMesh;
        }

            internal static Mesh TryGetCurrentHullMesh(CompModularShuttleSurfaceShieldVisual comp)
            {
                Graphic graphic = SurfaceShieldMaterialCache.TryGetCurrentHullGraphic(comp);
                if (comp.parent == null || graphic == null)
                {
                    return null;
                }

                return graphic.MeshAt(comp.parent.Rotation);
            }

            internal static Mesh GetFallbackDiscMesh()
            {
                if (sharedFallbackDiscMesh == null)
                {
                    sharedFallbackDiscMesh = BuildDiscMesh(FallbackMeshSegments);
                    sharedFallbackDiscMesh.name = "CeleTech_SurfaceShieldFallbackDisc";
                }

                return sharedFallbackDiscMesh;
            }

            internal static Mesh GetFallbackRingMesh()
            {
                if (sharedFallbackRingMesh == null)
                {
                    sharedFallbackRingMesh = BuildRingMesh(FallbackMeshSegments, 0.72f);
                    sharedFallbackRingMesh.name = "CeleTech_SurfaceShieldFallbackRing";
                }

                return sharedFallbackRingMesh;
            }

            internal static Mesh GetFallbackThinRingMesh()
            {
                if (sharedFallbackThinRingMesh == null)
                {
                    sharedFallbackThinRingMesh = BuildRingMesh(FallbackMeshSegments, 0.86f);
                    sharedFallbackThinRingMesh.name = "CeleTech_SurfaceShieldFallbackThinRing";
                }

                return sharedFallbackThinRingMesh;
            }

            internal static Mesh GetFallbackHexMesh()
            {
                if (sharedFallbackHexMesh == null)
                {
                    sharedFallbackHexMesh = BuildHexMesh();
                    sharedFallbackHexMesh.name = "CeleTech_SurfaceShieldFallbackHex";
                }

                return sharedFallbackHexMesh;
            }

            internal static Mesh GetFallbackShardMesh()
            {
                if (sharedFallbackShardMesh == null)
                {
                    sharedFallbackShardMesh = BuildShardMesh();
                    sharedFallbackShardMesh.name = "CeleTech_SurfaceShieldFallbackShard";
                }

                return sharedFallbackShardMesh;
            }

            internal static Mesh BuildDiscMesh(int segments)
            {
                int safeSegments = Mathf.Max(12, segments);
                Vector3[] vertices = new Vector3[safeSegments + 1];
                Vector2[] uvs = new Vector2[safeSegments + 1];
                int[] triangles = new int[safeSegments * 3];
                vertices[0] = Vector3.zero;
                uvs[0] = new Vector2(0.5f, 0.5f);

                for (int i = 0; i < safeSegments; i++)
                {
                    float angle = Mathf.PI * 2f * i / safeSegments;
                    float x = Mathf.Cos(angle) * 0.5f;
                    float z = Mathf.Sin(angle) * 0.5f;
                    vertices[i + 1] = new Vector3(x, 0f, z);
                    uvs[i + 1] = new Vector2(x + 0.5f, z + 0.5f);
                }

                int triangleIndex = 0;
                for (int i = 0; i < safeSegments; i++)
                {
                    int current = i + 1;
                    int next = i == safeSegments - 1 ? 1 : i + 2;
                    triangles[triangleIndex++] = 0;
                    triangles[triangleIndex++] = next;
                    triangles[triangleIndex++] = current;
                }

                Mesh mesh = new Mesh();
                mesh.vertices = vertices;
                mesh.uv = uvs;
                mesh.triangles = triangles;
                mesh.RecalculateNormals();
                return mesh;
            }

            internal static Mesh BuildHexMesh()
            {
                const int Segments = 6;
                Vector3[] vertices = new Vector3[Segments + 1];
                Vector2[] uvs = new Vector2[Segments + 1];
                int[] triangles = new int[Segments * 3];
                vertices[0] = Vector3.zero;
                uvs[0] = new Vector2(0.5f, 0.5f);

                for (int i = 0; i < Segments; i++)
                {
                    float angle = Mathf.PI * 2f * i / Segments + Mathf.PI / 6f;
                    float x = Mathf.Cos(angle) * 0.5f;
                    float z = Mathf.Sin(angle) * 0.5f;
                    vertices[i + 1] = new Vector3(x, 0f, z);
                    uvs[i + 1] = new Vector2(x + 0.5f, z + 0.5f);
                }

                int triangleIndex = 0;
                for (int i = 0; i < Segments; i++)
                {
                    int current = i + 1;
                    int next = i == Segments - 1 ? 1 : i + 2;
                    triangles[triangleIndex++] = 0;
                    triangles[triangleIndex++] = next;
                    triangles[triangleIndex++] = current;
                }

                Mesh mesh = new Mesh();
                mesh.vertices = vertices;
                mesh.uv = uvs;
                mesh.triangles = triangles;
                mesh.RecalculateNormals();
                return mesh;
            }

            internal static Mesh BuildShardMesh()
            {
                Mesh mesh = new Mesh();
                mesh.vertices = new[]
                {
                    new Vector3(0f, 0f, 0.5f),
                    new Vector3(0.5f, 0f, 0f),
                    new Vector3(0f, 0f, -0.5f),
                    new Vector3(-0.5f, 0f, 0f)
                };
                mesh.uv = new[]
                {
                    new Vector2(0.5f, 1f),
                    new Vector2(1f, 0.5f),
                    new Vector2(0.5f, 0f),
                    new Vector2(0f, 0.5f)
                };
                mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
                mesh.RecalculateNormals();
                return mesh;
            }

            internal static Mesh BuildRingMesh(int segments, float innerRadius01)
            {
                int safeSegments = Mathf.Max(12, segments);
                float innerRadius = Mathf.Clamp01(innerRadius01) * 0.5f;
                Vector3[] vertices = new Vector3[safeSegments * 2];
                Vector2[] uvs = new Vector2[safeSegments * 2];
                int[] triangles = new int[safeSegments * 6];

                for (int i = 0; i < safeSegments; i++)
                {
                    float angle = Mathf.PI * 2f * i / safeSegments;
                    float cos = Mathf.Cos(angle);
                    float sin = Mathf.Sin(angle);
                    Vector3 outer = new Vector3(cos * 0.5f, 0f, sin * 0.5f);
                    Vector3 inner = new Vector3(cos * innerRadius, 0f, sin * innerRadius);
                    int vertexIndex = i * 2;
                    vertices[vertexIndex] = outer;
                    vertices[vertexIndex + 1] = inner;
                    uvs[vertexIndex] = new Vector2(outer.x + 0.5f, outer.z + 0.5f);
                    uvs[vertexIndex + 1] = new Vector2(inner.x + 0.5f, inner.z + 0.5f);
                }

                int triangleIndex = 0;
                for (int i = 0; i < safeSegments; i++)
                {
                    int next = i == safeSegments - 1 ? 0 : i + 1;
                    int outer0 = i * 2;
                    int inner0 = outer0 + 1;
                    int outer1 = next * 2;
                    int inner1 = outer1 + 1;

                    triangles[triangleIndex++] = outer0;
                    triangles[triangleIndex++] = inner0;
                    triangles[triangleIndex++] = outer1;
                    triangles[triangleIndex++] = outer1;
                    triangles[triangleIndex++] = inner0;
                    triangles[triangleIndex++] = inner1;
                }

                Mesh mesh = new Mesh();
                mesh.vertices = vertices;
                mesh.uv = uvs;
                mesh.triangles = triangles;
                mesh.RecalculateNormals();
                return mesh;
            }
    }
}
