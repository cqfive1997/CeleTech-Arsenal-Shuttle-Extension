using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.SkyfallerDeploy
{
    internal sealed class ShuttleSkyfallerDeployLayerDrawer
    {
        internal static readonly ShuttleSkyfallerDeployLayerDrawer Shared =
            new ShuttleSkyfallerDeployLayerDrawer();

        internal void Draw(Material material, ShuttleSkyfallerDeployDrawParms drawParms)
        {
            this.Draw(material, drawParms, 0f);
        }

        internal void Draw(Material material, ShuttleSkyfallerDeployDrawParms drawParms, float yOffset)
        {
            if (material == null || material == BaseContent.BadMat || drawParms.Mesh == null)
            {
                return;
            }

            Matrix4x4 matrix = drawParms.Matrix;
            if (Mathf.Abs(yOffset) > 0.0001f)
            {
                matrix.m13 += yOffset;
            }

            Graphics.DrawMesh(drawParms.Mesh, matrix, material, 0);
        }
    }
}
