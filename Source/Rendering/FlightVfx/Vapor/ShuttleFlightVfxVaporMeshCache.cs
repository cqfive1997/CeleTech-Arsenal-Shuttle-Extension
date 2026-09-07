using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx.Vapor
{
    internal sealed class ShuttleFlightVfxVaporMeshCache
    {
        internal static readonly ShuttleFlightVfxVaporMeshCache Shared =
            new ShuttleFlightVfxVaporMeshCache();

        private Mesh softEllipseQuad;
        private Mesh softRibbonQuad;

        internal Mesh GetMesh(ShuttleFlightVfxVaporKind kind)
        {
            if (kind == ShuttleFlightVfxVaporKind.EdgeRibbon)
            {
                return this.GetOrCreateRibbon(ref this.softRibbonQuad);
            }

            return this.GetOrCreate(ref this.softEllipseQuad, "CMC_VaporSoftEllipseQuad");
        }

        private Mesh GetOrCreateRibbon(ref Mesh mesh)
        {
            if (mesh == null)
            {
                mesh = this.CreateEdgeRibbonMesh();
            }

            return mesh;
        }

        private Mesh GetOrCreate(ref Mesh mesh, string name)
        {
            if (mesh == null)
            {
                mesh = this.CreateQuadMesh(name);
            }

            return mesh;
        }

        private Mesh CreateQuadMesh(string name)
        {
            Mesh mesh = new Mesh();
            mesh.name = name;
            mesh.vertices = new Vector3[]
            {
                new Vector3(-0.5f, 0f, -0.5f),
                new Vector3(0.5f, 0f, -0.5f),
                new Vector3(-0.5f, 0f, 0.5f),
                new Vector3(0.5f, 0f, 0.5f)
            };
            mesh.triangles = new int[]
            {
                0,
                2,
                1,
                2,
                3,
                1
            };
            mesh.uv = new Vector2[]
            {
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f)
            };
            mesh.RecalculateBounds();
            return mesh;
        }

        private Mesh CreateEdgeRibbonMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "CMC_VaporSoftRibbonQuad";
            mesh.vertices = new Vector3[]
            {
                new Vector3(-0.5f, 0f, -0.5f),
                new Vector3(0.5f, 0f, -0.5f),
                new Vector3(-0.5f, 0f, 0.5f),
                new Vector3(0.5f, 0f, 0.5f)
            };
            mesh.uv = new Vector2[]
            {
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f)
            };
            mesh.triangles = new int[]
            {
                0,
                2,
                1,
                2,
                3,
                1
            };
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
