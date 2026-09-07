using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx
{
    internal sealed class ShuttleFlightVfxLaunchVaporTextureCache
    {
        internal static readonly ShuttleFlightVfxLaunchVaporTextureCache Shared =
            new ShuttleFlightVfxLaunchVaporTextureCache();

        private Texture2D vaporPlumeTexture;
        private Texture2D vaporHazeTexture;
        private Texture2D vaporCurlTexture;

        internal Texture2D VaporPlumeTexture
        {
            get
            {
                if (this.vaporPlumeTexture == null)
                {
                    this.vaporPlumeTexture = this.CreateVaporTexture(
                        "CMC_LaunchVaporPlume",
                        128,
                        128,
                        0.34f,
                        0.22f,
                        0.85f,
                        0.75f);
                }

                return this.vaporPlumeTexture;
            }
        }

        internal Texture2D VaporHazeTexture
        {
            get
            {
                if (this.vaporHazeTexture == null)
                {
                    this.vaporHazeTexture = this.CreateVaporTexture(
                        "CMC_LaunchVaporHaze",
                        128,
                        128,
                        0.45f,
                        0.32f,
                        0.55f,
                        0.55f);
                }

                return this.vaporHazeTexture;
            }
        }

        internal Texture2D VaporCurlTexture
        {
            get
            {
                if (this.vaporCurlTexture == null)
                {
                    this.vaporCurlTexture = this.CreateVaporTexture(
                        "CMC_LaunchVaporCurl",
                        128,
                        128,
                        0.30f,
                        0.28f,
                        0.78f,
                        0.85f);
                }

                return this.vaporCurlTexture;
            }
        }

        private Texture2D CreateVaporTexture(
            string name,
            int width,
            int height,
            float horizontalScale,
            float verticalScale,
            float maxAlpha,
            float turbulenceStrength)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
            texture.name = name;

            Color[] pixels = new Color[width * height];
            float maxX = Mathf.Max(1f, width - 1f);
            float maxY = Mathf.Max(1f, height - 1f);

            for (int y = 0; y < height; y++)
            {
                float v = y / maxY;
                for (int x = 0; x < width; x++)
                {
                    float u = x / maxX;
                    float cx = (u - 0.5f) / Mathf.Max(0.001f, horizontalScale);
                    float cy = (v - 0.5f) / Mathf.Max(0.001f, verticalScale);
                    float cloud = Mathf.Exp(-((cx * cx) + (cy * cy)) * 2.2f);
                    float wave = this.ResolveTurbulence(u, v, turbulenceStrength);
                    float edgeFade = this.ResolveEdgeFade(u, v);
                    float alpha = Mathf.Clamp01(maxAlpha * cloud * wave * edgeFade);

                    pixels[(y * width) + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.Apply(false, true);
            return texture;
        }

        private float ResolveTurbulence(float u, float v, float turbulenceStrength)
        {
            float wave =
                Mathf.Sin((u * 17.0f) + (v * 9.0f)) *
                Mathf.Sin((u * 5.0f) - (v * 13.0f));
            return Mathf.Clamp01(0.75f + (turbulenceStrength * 0.25f * wave));
        }

        private float ResolveEdgeFade(float u, float v)
        {
            return SmoothStep01(0f, 0.08f, u) *
                (1f - SmoothStep01(0.92f, 1f, u)) *
                SmoothStep01(0f, 0.08f, v) *
                (1f - SmoothStep01(0.92f, 1f, v));
        }

        private static float SmoothStep01(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / Mathf.Max(0.0001f, edge1 - edge0));
            return t * t * (3f - (2f * t));
        }
    }
}
