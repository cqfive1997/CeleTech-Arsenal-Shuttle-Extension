using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx
{
    internal sealed class ShuttleFlightVfxPlaceholderMaterialCache
    {
        internal static readonly ShuttleFlightVfxPlaceholderMaterialCache Shared =
            new ShuttleFlightVfxPlaceholderMaterialCache(ShuttleFlightVfxProceduralPlumeTextureCache.Shared);

        private readonly ShuttleFlightVfxProceduralPlumeTextureCache textureCache;
        private Material tailMainGlowMaterial;
        private Material tailMainCoreMaterial;
        private Material vtolGlowMaterial;
        private Material vtolCoreMaterial;

        private ShuttleFlightVfxPlaceholderMaterialCache(
            ShuttleFlightVfxProceduralPlumeTextureCache textureCache)
        {
            this.textureCache = textureCache;
        }

        internal Material GetGlowMaterial(ShuttleThrusterKind kind)
        {
            if (kind == ShuttleThrusterKind.TailMain)
            {
                return this.GetOrCreate(
                    ref this.tailMainGlowMaterial,
                    this.textureCache.GetGlowTexture(kind),
                    new Color(0.20f, 0.85f, 1f, 0.55f));
            }

            return this.GetOrCreate(
                ref this.vtolGlowMaterial,
                this.textureCache.GetGlowTexture(kind),
                new Color(0.20f, 0.75f, 1f, 0.52f));
        }

        internal Material GetCoreMaterial(ShuttleThrusterKind kind)
        {
            if (kind == ShuttleThrusterKind.TailMain)
            {
                return this.GetOrCreate(
                    ref this.tailMainCoreMaterial,
                    this.textureCache.GetCoreTexture(kind),
                    new Color(0.78f, 1f, 1f, 0.96f));
            }

            return this.GetOrCreate(
                ref this.vtolCoreMaterial,
                this.textureCache.GetCoreTexture(kind),
                new Color(0.72f, 1f, 1f, 0.92f));
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
