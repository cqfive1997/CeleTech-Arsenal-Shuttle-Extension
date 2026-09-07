using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx
{
    internal sealed class ShuttleFlightVfxLaunchVaporMaterialCache
    {
        internal static readonly ShuttleFlightVfxLaunchVaporMaterialCache Shared =
            new ShuttleFlightVfxLaunchVaporMaterialCache(ShuttleFlightVfxLaunchVaporTextureCache.Shared);

        private readonly ShuttleFlightVfxLaunchVaporTextureCache textureCache;
        private Material plumeMaterial;
        private Material hazeMaterial;
        private Material curlMaterial;

        private ShuttleFlightVfxLaunchVaporMaterialCache(
            ShuttleFlightVfxLaunchVaporTextureCache textureCache)
        {
            this.textureCache = textureCache;
        }

        internal Material PlumeMaterial
        {
            get
            {
                return this.GetOrCreate(
                    ref this.plumeMaterial,
                    this.textureCache.VaporPlumeTexture,
                    new Color(0.72f, 0.98f, 1f, 0.42f));
            }
        }

        internal Material HazeMaterial
        {
            get
            {
                return this.GetOrCreate(
                    ref this.hazeMaterial,
                    this.textureCache.VaporHazeTexture,
                    new Color(0.45f, 0.85f, 1f, 0.22f));
            }
        }

        internal Material CurlMaterial
        {
            get
            {
                return this.GetOrCreate(
                    ref this.curlMaterial,
                    this.textureCache.VaporCurlTexture,
                    new Color(0.86f, 1f, 1f, 0.38f));
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
