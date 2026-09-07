using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx
{
    internal sealed class ShuttleFlightVfxFlameMeshCache
    {
        internal static readonly ShuttleFlightVfxFlameMeshCache Shared =
            new ShuttleFlightVfxFlameMeshCache();

        private Mesh tailMainGlowMesh;
        private Mesh tailMainCoreMesh;
        private Mesh rearVtolGlowMesh;
        private Mesh rearVtolCoreMesh;
        private Mesh bellyVtolGlowMesh;
        private Mesh bellyVtolCoreMesh;

        internal Mesh GetMesh(ShuttleThrusterKind kind, bool glowLayer)
        {
            if (kind == ShuttleThrusterKind.TailMain)
            {
                return glowLayer ?
                    this.GetOrCreate(ref this.tailMainGlowMesh, 0.95f, 0.22f, 0.36f, 20, "CMC_TailMainGlowFlame") :
                    this.GetOrCreate(ref this.tailMainCoreMesh, 0.42f, 0.06f, 0.42f, 20, "CMC_TailMainCoreFlame");
            }

            if (kind == ShuttleThrusterKind.BellyVtol)
            {
                return glowLayer ?
                    this.GetOrCreate(ref this.bellyVtolGlowMesh, 0.74f, 0.18f, 0.16f, 16, "CMC_BellyVtolGlowFlame") :
                    this.GetOrCreate(ref this.bellyVtolCoreMesh, 0.34f, 0.10f, 0.18f, 16, "CMC_BellyVtolCoreFlame");
            }

            return glowLayer ?
                this.GetOrCreate(ref this.rearVtolGlowMesh, 0.74f, 0.18f, 0.16f, 16, "CMC_RearVtolGlowFlame") :
                this.GetOrCreate(ref this.rearVtolCoreMesh, 0.34f, 0.10f, 0.18f, 16, "CMC_RearVtolCoreFlame");
        }

        private Mesh GetOrCreate(
            ref Mesh mesh,
            float cylinderHalfWidth,
            float tipHalfWidth,
            float cylinderEnd,
            int segments,
            string name)
        {
            if (mesh == null)
            {
                mesh = this.CreateProfiledPlumeMesh(
                    cylinderHalfWidth,
                    tipHalfWidth,
                    cylinderEnd,
                    segments,
                    name);
            }

            return mesh;
        }

        private Mesh CreateProfiledPlumeMesh(
            float cylinderHalfWidth,
            float tipHalfWidth,
            float cylinderEnd,
            int segments,
            string name)
        {
            int segmentCount = Mathf.Max(1, segments);
            Mesh mesh = new Mesh();
            mesh.name = name;

            Vector3[] vertices = new Vector3[(segmentCount + 1) * 2];
            Vector2[] uv = new Vector2[vertices.Length];
            int[] triangles = new int[segmentCount * 6];

            for (int i = 0; i <= segmentCount; i++)
            {
                float v = (float)i / segmentCount;
                float radius = ResolveRadiusAt(v, cylinderHalfWidth, tipHalfWidth, cylinderEnd);
                float z = Mathf.Lerp(-0.5f, 0.5f, v);
                int vertexIndex = i * 2;
                vertices[vertexIndex] = new Vector3(-radius, 0f, z);
                vertices[vertexIndex + 1] = new Vector3(radius, 0f, z);
                uv[vertexIndex] = new Vector2(0f, v);
                uv[vertexIndex + 1] = new Vector2(1f, v);
            }

            for (int i = 0; i < segmentCount; i++)
            {
                int left0 = i * 2;
                int right0 = left0 + 1;
                int left1 = (i + 1) * 2;
                int right1 = left1 + 1;
                int triangleIndex = i * 6;

                triangles[triangleIndex] = left0;
                triangles[triangleIndex + 1] = left1;
                triangles[triangleIndex + 2] = right0;
                triangles[triangleIndex + 3] = left1;
                triangles[triangleIndex + 4] = right1;
                triangles[triangleIndex + 5] = right0;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv;
            mesh.RecalculateBounds();
            return mesh;
        }

        private static float ResolveRadiusAt(
            float v,
            float cylinderHalfWidth,
            float tipHalfWidth,
            float cylinderEnd)
        {
            float clampedCylinderEnd = Mathf.Clamp01(cylinderEnd);
            if (v <= clampedCylinderEnd)
            {
                return cylinderHalfWidth;
            }

            float t = SmoothStep01(clampedCylinderEnd, 1f, v);
            return Mathf.Lerp(cylinderHalfWidth, tipHalfWidth, t);
        }

        private static float SmoothStep01(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / Mathf.Max(0.0001f, edge1 - edge0));
            return t * t * (3f - (2f * t));
        }
    }
}
