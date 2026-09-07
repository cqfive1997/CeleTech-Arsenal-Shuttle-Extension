using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx
{
    internal sealed class ShuttleFlightVfxAirflowTextureCache
    {
        internal static readonly ShuttleFlightVfxAirflowTextureCache Shared =
            new ShuttleFlightVfxAirflowTextureCache();

        private Texture2D streakTexture;
        private Texture2D wakeTexture;
        private Texture2D sonicCloudTexture;

        internal Texture2D StreakTexture
        {
            get
            {
                if (this.streakTexture == null)
                {
                    this.streakTexture = this.CreateBandTexture(
                        "CMC_AirflowStreak",
                        64,
                        256,
                        0.085f,
                        1.00f,
                        5.2f,
                        0.08f,
                        0.88f);
                }

                return this.streakTexture;
            }
        }

        internal Texture2D WakeTexture
        {
            get
            {
                if (this.wakeTexture == null)
                {
                    this.wakeTexture = this.CreateBandTexture(
                        "CMC_AirflowWake",
                        64,
                        256,
                        0.28f,
                        0.78f,
                        1.6f,
                        0.05f,
                        0.82f);
                }

                return this.wakeTexture;
            }
        }

        internal Texture2D SonicCloudTexture
        {
            get
            {
                if (this.sonicCloudTexture == null)
                {
                    this.sonicCloudTexture = this.CreateSonicCloudTexture("CMC_AirflowSonicCloud", 128);
                }

                return this.sonicCloudTexture;
            }
        }

        private Texture2D CreateBandTexture(
            string name,
            int width,
            int height,
            float halfWidth,
            float maxAlpha,
            float sideFalloff,
            float fadeInEnd,
            float fadeOutStart)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
            texture.name = name;

            Color[] pixels = new Color[width * height];
            float maxX = Mathf.Max(1f, width - 1f);
            float maxY = Mathf.Max(1f, height - 1f);

            for (int y = 0; y < height; y++)
            {
                float v = y / maxY;
                float lengthAlpha = SmoothStep01(0f, fadeInEnd, v) *
                    (1f - SmoothStep01(fadeOutStart, 1f, v));

                for (int x = 0; x < width; x++)
                {
                    float u = x / maxX;
                    float normalized = Mathf.Abs(u - 0.5f) / Mathf.Max(0.001f, halfWidth);
                    float sideAlpha = Mathf.Exp(-normalized * normalized * sideFalloff);
                    float alpha = Mathf.Clamp01(maxAlpha * lengthAlpha * sideAlpha);
                    pixels[(y * width) + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            return this.FinalizeTexture(texture, pixels);
        }

        private Texture2D CreateSonicCloudTexture(string name, int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
            texture.name = name;

            Color[] pixels = new Color[size * size];
            float max = Mathf.Max(1f, size - 1f);

            for (int y = 0; y < size; y++)
            {
                float v = (y / max) - 0.5f;
                for (int x = 0; x < size; x++)
                {
                    float u = (x / max) - 0.5f;
                    float radius = Mathf.Sqrt((u * u) + (v * v)) * 2f;
                    float ring = Mathf.Exp(-Mathf.Pow((radius - 0.56f) / 0.22f, 2f));
                    float softBody = 0.36f * Mathf.Exp(-radius * radius * 1.6f);
                    float centerCut = SmoothStep01(0.08f, 0.30f, radius);
                    float outerFade = 1f - SmoothStep01(0.90f, 1f, radius);
                    float alpha = Mathf.Clamp01(((ring * 0.95f) + softBody) * centerCut * outerFade);
                    pixels[(y * size) + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            return this.FinalizeTexture(texture, pixels);
        }

        private Texture2D FinalizeTexture(Texture2D texture, Color[] pixels)
        {
            texture.SetPixels(pixels);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.Apply(false, true);
            return texture;
        }

        private static float SmoothStep01(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / Mathf.Max(0.0001f, edge1 - edge0));
            return t * t * (3f - (2f * t));
        }
    }
}
