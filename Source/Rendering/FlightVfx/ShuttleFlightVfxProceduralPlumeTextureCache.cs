using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx
{
    internal sealed class ShuttleFlightVfxProceduralPlumeTextureCache
    {
        internal static readonly ShuttleFlightVfxProceduralPlumeTextureCache Shared =
            new ShuttleFlightVfxProceduralPlumeTextureCache();

        private const int TextureWidth = 64;
        private const int TextureHeight = 256;

        private Texture2D tailMainGlowTexture;
        private Texture2D tailMainCoreTexture;
        private Texture2D vtolGlowTexture;
        private Texture2D vtolCoreTexture;

        internal Texture2D GetGlowTexture(ShuttleThrusterKind kind)
        {
            if (kind == ShuttleThrusterKind.TailMain)
            {
                return this.GetOrCreate(
                    ref this.tailMainGlowTexture,
                    0.14f,
                    0.38f,
                    0.06f,
                    0.90f,
                    0.72f,
                    "CMC_TailMainGlowPlume");
            }

            return this.GetOrCreate(
                ref this.vtolGlowTexture,
                0.16f,
                0.48f,
                0.14f,
                0.82f,
                0.62f,
                "CMC_VtolGlowPlume");
        }

        internal Texture2D GetCoreTexture(ShuttleThrusterKind kind)
        {
            if (kind == ShuttleThrusterKind.TailMain)
            {
                return this.GetOrCreate(
                    ref this.tailMainCoreTexture,
                    0.08f,
                    0.16f,
                    0.03f,
                    1.00f,
                    0.54f,
                    "CMC_TailMainCorePlume");
            }

            return this.GetOrCreate(
                ref this.vtolCoreTexture,
                0.08f,
                0.22f,
                0.04f,
                1.00f,
                0.52f,
                "CMC_VtolCorePlume");
        }

        private Texture2D GetOrCreate(
            ref Texture2D texture,
            float rootWidth,
            float midWidth,
            float tipWidth,
            float maxAlpha,
            float fadeOutStart,
            string name)
        {
            if (texture == null)
            {
                texture = this.CreatePlumeTexture(
                    TextureWidth,
                    TextureHeight,
                    rootWidth,
                    midWidth,
                    tipWidth,
                    maxAlpha,
                    fadeOutStart,
                    name);
            }

            return texture;
        }

        private Texture2D CreatePlumeTexture(
            int width,
            int height,
            float rootWidth,
            float midWidth,
            float tipWidth,
            float maxAlpha,
            float fadeOutStart,
            string name)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
            texture.name = name;

            Color[] pixels = new Color[width * height];
            float maxX = Mathf.Max(1f, width - 1f);
            float maxY = Mathf.Max(1f, height - 1f);

            for (int y = 0; y < height; y++)
            {
                float v = y / maxY;
                float halfWidth = this.ResolveHalfWidth(v, rootWidth, midWidth, tipWidth);
                float lengthAlpha = this.ResolveLengthAlpha(v, fadeOutStart);

                for (int x = 0; x < width; x++)
                {
                    float u = x / maxX;
                    float centered = Mathf.Abs(u - 0.5f);
                    float normalized = centered / halfWidth;
                    float sideAlpha = Mathf.Exp(-normalized * normalized * 3.0f);
                    float alpha = Mathf.Clamp01(maxAlpha * lengthAlpha * sideAlpha);

                    pixels[(y * width) + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.Apply(false, true);
            return texture;
        }

        private float ResolveHalfWidth(float v, float rootWidth, float midWidth, float tipWidth)
        {
            float expand = SmoothStep01(0f, 0.28f, v);
            float contract = SmoothStep01(0.55f, 1f, v);
            float halfWidth = Mathf.Lerp(rootWidth, midWidth, expand);
            halfWidth = Mathf.Lerp(halfWidth, tipWidth, contract);
            return Mathf.Max(0.001f, halfWidth);
        }

        private float ResolveLengthAlpha(float v, float fadeOutStart)
        {
            float rootFade = SmoothStep01(0f, 0.06f, v);
            float tipFade = 1f - SmoothStep01(fadeOutStart, 1f, v);
            return rootFade * tipFade;
        }

        private static float SmoothStep01(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / Mathf.Max(0.0001f, edge1 - edge0));
            return t * t * (3f - (2f * t));
        }
    }
}
