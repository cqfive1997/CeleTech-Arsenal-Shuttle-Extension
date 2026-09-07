using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx.Vapor
{
    internal sealed class ShuttleFlightVfxVaporCalibrationMaterialCache
    {
        internal static readonly ShuttleFlightVfxVaporCalibrationMaterialCache Shared =
            new ShuttleFlightVfxVaporCalibrationMaterialCache();

        private Material centerMaterial;
        private Material tangentMaterial;
        private Material normalMaterial;
        private Material outlineMaterial;
        private Material rootEdgeMaterial;

        internal Material Center
        {
            get
            {
                return this.GetOrCreate(ref this.centerMaterial, new Color(1f, 1f, 1f, 0.95f));
            }
        }

        internal Material Tangent
        {
            get
            {
                return this.GetOrCreate(ref this.tangentMaterial, new Color(0.18f, 1f, 0.28f, 0.95f));
            }
        }

        internal Material Normal
        {
            get
            {
                return this.GetOrCreate(ref this.normalMaterial, new Color(0.12f, 0.40f, 1f, 0.95f));
            }
        }

        internal Material Outline
        {
            get
            {
                return this.GetOrCreate(ref this.outlineMaterial, new Color(1f, 0.88f, 0.12f, 0.82f));
            }
        }

        internal Material RootEdge
        {
            get
            {
                return this.GetOrCreate(ref this.rootEdgeMaterial, new Color(1f, 0.12f, 0.08f, 0.95f));
            }
        }

        private Material GetOrCreate(ref Material material, Color color)
        {
            if (material == null || material == BaseContent.BadMat)
            {
                material = MaterialPool.MatFrom(BaseContent.WhiteTex, this.ResolveDebugShader(), color);
            }

            return material;
        }

        private Shader ResolveDebugShader()
        {
            return ShaderDatabase.MetaOverlay != null
                ? ShaderDatabase.MetaOverlay
                : ShaderDatabase.Transparent;
        }
    }
}
