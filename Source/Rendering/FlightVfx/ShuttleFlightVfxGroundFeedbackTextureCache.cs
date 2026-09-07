using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx
{
    internal sealed class ShuttleFlightVfxGroundFeedbackTextureCache
    {
        internal static readonly ShuttleFlightVfxGroundFeedbackTextureCache Shared =
            new ShuttleFlightVfxGroundFeedbackTextureCache();

        private const int TextureSize = 128;

        private Texture2D softDisc;
        private Texture2D softHaze;
        private Texture2D softRing;

        internal Texture2D SoftDisc
        {
            get
            {
                if (this.softDisc == null)
                {
                    this.softDisc = this.CreateRadialTexture("CMC_GroundFeedbackSoftDisc", 5.2f);
                }

                return this.softDisc;
            }
        }

        internal Texture2D SoftHaze
        {
            get
            {
                if (this.softHaze == null)
                {
                    this.softHaze = this.CreateRadialTexture("CMC_GroundFeedbackSoftHaze", 2.1f);
                }

                return this.softHaze;
            }
        }

        internal Texture2D SoftRing
        {
            get
            {
                if (this.softRing == null)
                {
                    this.softRing = this.CreateRingTexture("CMC_GroundFeedbackSoftRing", 0.55f, 0.11f);
                }

                return this.softRing;
            }
        }

        private Texture2D CreateRadialTexture(string name, float falloff)
        {
            Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.ARGB32, false);
            texture.name = name;

            Color[] pixels = new Color[TextureSize * TextureSize];
            float max = Mathf.Max(1f, TextureSize - 1f);

            for (int y = 0; y < TextureSize; y++)
            {
                float v = (y / max) - 0.5f;
                for (int x = 0; x < TextureSize; x++)
                {
                    float u = (x / max) - 0.5f;
                    float radius = Mathf.Sqrt((u * u) + (v * v)) * 2f;
                    float alpha = Mathf.Clamp01(Mathf.Exp(-radius * radius * falloff));
                    pixels[(y * TextureSize) + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            return this.FinalizeTexture(texture, pixels);
        }

        private Texture2D CreateRingTexture(string name, float ringRadius, float ringWidth)
        {
            Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.ARGB32, false);
            texture.name = name;

            Color[] pixels = new Color[TextureSize * TextureSize];
            float max = Mathf.Max(1f, TextureSize - 1f);

            for (int y = 0; y < TextureSize; y++)
            {
                float v = (y / max) - 0.5f;
                for (int x = 0; x < TextureSize; x++)
                {
                    float u = (x / max) - 0.5f;
                    float radius = Mathf.Sqrt((u * u) + (v * v)) * 2f;
                    float distance = Mathf.Abs(radius - ringRadius);
                    float normalized = distance / Mathf.Max(0.001f, ringWidth);
                    float alpha = Mathf.Clamp01(Mathf.Exp(-normalized * normalized * 2.8f));
                    alpha *= 1f - SmoothStep01(0.88f, 1f, radius);
                    pixels[(y * TextureSize) + x] = new Color(1f, 1f, 1f, alpha);
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
