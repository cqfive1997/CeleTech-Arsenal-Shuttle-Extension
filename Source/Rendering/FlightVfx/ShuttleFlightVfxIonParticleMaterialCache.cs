using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx
{
    internal sealed class ShuttleFlightVfxIonParticleMaterialCache
    {
        internal static readonly ShuttleFlightVfxIonParticleMaterialCache Shared =
            new ShuttleFlightVfxIonParticleMaterialCache(ShuttleFlightVfxShaderMaterialCache.Shared);

        private readonly ShuttleFlightVfxShaderMaterialCache shaderMaterialCache;

        private ShuttleFlightVfxIonParticleMaterialCache(
            ShuttleFlightVfxShaderMaterialCache shaderMaterialCache)
        {
            this.shaderMaterialCache = shaderMaterialCache;
        }

        internal bool IsReady
        {
            get
            {
                return this.shaderMaterialCache != null && this.shaderMaterialCache.IsReady;
            }
        }

        internal Material GetParticleMaterial(ShuttleThrusterKind kind)
        {
            if (!this.IsReady)
            {
                return null;
            }

            return this.shaderMaterialCache.GetGlowMaterial(kind);
        }
    }
}
