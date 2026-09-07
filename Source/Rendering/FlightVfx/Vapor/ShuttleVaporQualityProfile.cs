namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx.Vapor
{
    internal struct ShuttleVaporQualityProfile
    {
        internal readonly ShuttleVaporQuality Quality;
        internal readonly int MaxAnchors;
        internal readonly int MaxDetailAnchors;
        internal readonly bool UseDetailLayer;
        internal readonly float AlphaMultiplier;
        internal readonly float MotionMultiplier;
        internal readonly float SizeMultiplier;
        internal readonly float MaxAnimationScale;
        internal readonly int MaxQualityRank;
        internal readonly ShuttleVaporShaderQualityMode ShaderQualityMode;

        internal bool ShouldDraw
        {
            get
            {
                return this.Quality != ShuttleVaporQuality.Off &&
                    this.MaxAnchors > 0 &&
                    this.AlphaMultiplier > 0f;
            }
        }

        private ShuttleVaporQualityProfile(
            ShuttleVaporQuality quality,
            int maxAnchors,
            int maxDetailAnchors,
            bool useDetailLayer,
            float alphaMultiplier,
            float motionMultiplier,
            float sizeMultiplier,
            float maxAnimationScale,
            int maxQualityRank,
            ShuttleVaporShaderQualityMode shaderQualityMode)
        {
            this.Quality = quality;
            this.MaxAnchors = maxAnchors;
            this.MaxDetailAnchors = maxDetailAnchors;
            this.UseDetailLayer = useDetailLayer;
            this.AlphaMultiplier = alphaMultiplier;
            this.MotionMultiplier = motionMultiplier;
            this.SizeMultiplier = sizeMultiplier;
            this.MaxAnimationScale = maxAnimationScale;
            this.MaxQualityRank = maxQualityRank;
            this.ShaderQualityMode = shaderQualityMode;
        }

        internal static ShuttleVaporQualityProfile ForQuality(ShuttleVaporQuality quality)
        {
            if (quality == ShuttleVaporQuality.Off)
            {
                return new ShuttleVaporQualityProfile(
                    ShuttleVaporQuality.Off,
                    0,
                    0,
                    false,
                    0f,
                    0f,
                    0f,
                    0f,
                    -1,
                    ShuttleVaporShaderQualityMode.Simple);
            }

            return new ShuttleVaporQualityProfile(
                ShuttleVaporQuality.High,
                6,
                0,
                false,
                1.18f,
                1.00f,
                1.06f,
                1.12f,
                2,
                ShuttleVaporShaderQualityMode.Detailed);
        }
    }
}
