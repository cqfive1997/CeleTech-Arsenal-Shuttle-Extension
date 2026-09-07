using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx
{
    internal sealed class ShuttleFlightVfxAirflowMaterialCache
    {
        internal static readonly ShuttleFlightVfxAirflowMaterialCache Shared =
            new ShuttleFlightVfxAirflowMaterialCache(ShuttleFlightVfxAirflowTextureCache.Shared);

        private readonly ShuttleFlightVfxAirflowTextureCache textureCache;
        private Material streakMaterial;
        private Material wakeMaterial;
        private Material sonicCloudMaterial;

        private ShuttleFlightVfxAirflowMaterialCache(
            ShuttleFlightVfxAirflowTextureCache textureCache)
        {
            this.textureCache = textureCache;
        }

        internal Material StreakMaterial
        {
            get
            {
                return this.GetOrCreate(
                    ref this.streakMaterial,
                    this.textureCache.StreakTexture,
                    new Color(0.72f, 1f, 1f, 0.48f));
            }
        }

        internal Material WakeMaterial
        {
            get
            {
                return this.GetOrCreate(
                    ref this.wakeMaterial,
                    this.textureCache.WakeTexture,
                    new Color(0.30f, 0.90f, 1f, 0.26f));
            }
        }

        internal Material SonicCloudMaterial
        {
            get
            {
                return this.GetOrCreate(
                    ref this.sonicCloudMaterial,
                    this.textureCache.SonicCloudTexture,
                    new Color(0.95f, 1f, 1f, 0.34f));
            }
        }

        private Material GetOrCreate(ref Material material, Texture2D texture, Color color)
        {
            if (material == null || material == BaseContent.BadMat)
            {
                material = MaterialPool.MatFrom(texture ?? BaseContent.WhiteTex, ShaderDatabase.Transparent, color);
            }

            return material;
        }
    }
}
