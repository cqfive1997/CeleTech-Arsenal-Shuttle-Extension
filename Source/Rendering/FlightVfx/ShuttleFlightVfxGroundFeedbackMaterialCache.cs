using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx
{
    internal sealed class ShuttleFlightVfxGroundFeedbackMaterialCache
    {
        internal static readonly ShuttleFlightVfxGroundFeedbackMaterialCache Shared =
            new ShuttleFlightVfxGroundFeedbackMaterialCache(ShuttleFlightVfxGroundFeedbackTextureCache.Shared);

        private readonly ShuttleFlightVfxGroundFeedbackTextureCache textureCache;
        private Material blueGlowMaterial;
        private Material blueHazeMaterial;
        private Material shockRingMaterial;

        private ShuttleFlightVfxGroundFeedbackMaterialCache(
            ShuttleFlightVfxGroundFeedbackTextureCache textureCache)
        {
            this.textureCache = textureCache;
        }

        internal Material BlueGlowMaterial
        {
            get
            {
                return this.GetOrCreate(
                    ref this.blueGlowMaterial,
                    this.textureCache.SoftDisc,
                    new Color(0.25f, 0.85f, 1f, 0.42f));
            }
        }

        internal Material BlueHazeMaterial
        {
            get
            {
                return this.GetOrCreate(
                    ref this.blueHazeMaterial,
                    this.textureCache.SoftHaze,
                    new Color(0.55f, 0.85f, 1f, 0.18f));
            }
        }

        internal Material ShockRingMaterial
        {
            get
            {
                return this.GetOrCreate(
                    ref this.shockRingMaterial,
                    this.textureCache.SoftRing,
                    new Color(0.65f, 0.95f, 1f, 0.30f));
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
