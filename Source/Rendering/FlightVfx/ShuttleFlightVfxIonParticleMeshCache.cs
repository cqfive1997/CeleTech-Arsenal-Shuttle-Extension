using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx
{
    internal sealed class ShuttleFlightVfxIonParticleMeshCache
    {
        internal static readonly ShuttleFlightVfxIonParticleMeshCache Shared =
            new ShuttleFlightVfxIonParticleMeshCache();

        private Mesh quadMesh;

        internal Mesh QuadMesh
        {
            get
            {
                if (this.quadMesh == null)
                {
                    this.quadMesh = this.CreateQuadMesh();
                }

                return this.quadMesh;
            }
        }

        private Mesh CreateQuadMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "CMC_IonParticleQuad";
            mesh.vertices = new Vector3[]
            {
                new Vector3(-0.5f, 0f, -0.5f),
                new Vector3(0.5f, 0f, -0.5f),
                new Vector3(-0.5f, 0f, 0.5f),
                new Vector3(0.5f, 0f, 0.5f)
            };
            mesh.triangles = new int[]
            {
                0, 2, 1,
                2, 3, 1
            };
            mesh.uv = new Vector2[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            };
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
