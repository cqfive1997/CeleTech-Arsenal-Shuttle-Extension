using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal static class SurfaceShieldFallbackTuning
    {
        internal const int LightTier = 0;
        internal const int MediumTier = 1;
        internal const int HeavyTier = 2;
        internal const int BreakTier = 3;

        internal static float NormalizePulseRadius(
            float pulseRadius,
            float width,
            float depth,
            float maxHitUvRadius)
        {
            float denominator = Mathf.Max(0.001f, Mathf.Min(width, depth));
            return Mathf.Clamp(
                pulseRadius / denominator,
                0.035f,
                Mathf.Clamp(maxHitUvRadius, 0.04f, 0.35f));
        }

        internal static int ResolveHexCountForBreakState(int configuredCount, bool brokeShield)
        {
            int count = Mathf.Clamp(configuredCount, 1, 10);
            return brokeShield ? Mathf.Clamp(count + 2, 7, 10) : Mathf.Clamp(count, 5, 7);
        }

        internal static int ResolveHexCountForTier(int configuredCount, int tier)
        {
            int count = Mathf.Clamp(configuredCount, 1, 10);
            if (tier == LightTier)
            {
                return Mathf.Clamp(count - 1, 5, 6);
            }

            if (tier == HeavyTier)
            {
                return Mathf.Clamp(count + 1, 7, 9);
            }

            if (tier == BreakTier)
            {
                return Mathf.Clamp(count + 3, 8, 10);
            }

            return Mathf.Clamp(count, 6, 7);
        }

        internal static int ResolveShardCount(int tier)
        {
            if (tier == LightTier)
            {
                return 4;
            }

            if (tier == HeavyTier)
            {
                return 7;
            }

            if (tier == BreakTier)
            {
                return 10;
            }

            return 6;
        }

        internal static int ResolvePulseStrengthTier(bool brokeShield, float strength)
        {
            if (brokeShield)
            {
                return BreakTier;
            }

            if (strength >= 1.35f)
            {
                return HeavyTier;
            }

            if (strength >= 1f)
            {
                return MediumTier;
            }

            return LightTier;
        }

        internal static float GetTierAlphaMultiplier(int tier)
        {
            if (tier == LightTier)
            {
                return 0.82f;
            }

            if (tier == HeavyTier)
            {
                return 1.18f;
            }

            if (tier == BreakTier)
            {
                return 1.28f;
            }

            return 1f;
        }

        internal static Color GetImpactCoreColor(float alpha, bool brokeShield)
        {
            return brokeShield
                ? new Color(0.94f, 0.99f, 1f, Mathf.Clamp01(alpha * 1.2f))
                : new Color(0.86f, 0.96f, 1f, Mathf.Clamp01(alpha));
        }

        internal static Color GetPrimaryRingColor(float alpha, bool brokeShield, bool lowShield)
        {
            return brokeShield
                ? new Color(0.74f, 0.9f, 1f, Mathf.Clamp01(alpha * 1.15f))
                : lowShield
                    ? new Color(0.68f, 0.84f, 0.96f, Mathf.Clamp01(alpha))
                    : new Color(0.34f, 0.72f, 0.9f, Mathf.Clamp01(alpha));
        }

        internal static Color GetSecondaryRingColor(float alpha)
        {
            return new Color(0.14f, 0.42f, 0.68f, Mathf.Clamp01(alpha));
        }

        internal static float GetFallbackBaseAlpha(CompProperties_ModularShuttleSurfaceShieldVisual props)
        {
            return Mathf.Clamp01(props.fallbackBaseAlpha);
        }

        internal static float GetFallbackFlashAlpha(CompProperties_ModularShuttleSurfaceShieldVisual props)
        {
            return Mathf.Max(0f, props.fallbackFlashAlpha);
        }

        internal static float GetFallbackRingAlpha(CompProperties_ModularShuttleSurfaceShieldVisual props)
        {
            return Mathf.Max(0f, props.fallbackRingAlpha);
        }

        internal static float GetFallbackSecondRingAlpha(CompProperties_ModularShuttleSurfaceShieldVisual props)
        {
            return Mathf.Max(0f, props.fallbackSecondRingAlpha);
        }

        internal static float GetFallbackSplashAlpha(CompProperties_ModularShuttleSurfaceShieldVisual props)
        {
            return Mathf.Max(0f, props.fallbackSplashAlpha);
        }

        internal static float GetFallbackHexAlpha(CompProperties_ModularShuttleSurfaceShieldVisual props)
        {
            return Mathf.Max(0f, props.fallbackHexAlpha);
        }

        internal static float GetFallbackShardAlpha(CompProperties_ModularShuttleSurfaceShieldVisual props)
        {
            return Mathf.Max(0f, props.fallbackShardAlpha);
        }

        internal static float GetFallbackResidualAlpha(CompProperties_ModularShuttleSurfaceShieldVisual props)
        {
            return Mathf.Max(0f, props.fallbackResidualAlpha);
        }

        internal static float GetFallbackDirectionalAlpha(CompProperties_ModularShuttleSurfaceShieldVisual props)
        {
            return Mathf.Max(0f, props.fallbackDirectionalAlpha);
        }

        internal static float GetFallbackCrawlAlpha(CompProperties_ModularShuttleSurfaceShieldVisual props)
        {
            return Mathf.Max(0f, props.fallbackCrawlAlpha);
        }

        internal static float GetFallbackSplashRadiusFactor(CompProperties_ModularShuttleSurfaceShieldVisual props)
        {
            return Mathf.Max(0f, props.fallbackSplashRadiusFactor);
        }

        internal static float GetFallbackBreakAlphaMultiplier(CompProperties_ModularShuttleSurfaceShieldVisual props)
        {
            return Mathf.Max(0f, props.fallbackBreakAlphaMultiplier);
        }

        internal static float GetFallbackBreakRadiusMultiplier(CompProperties_ModularShuttleSurfaceShieldVisual props)
        {
            return Mathf.Max(0f, props.fallbackBreakRadiusMultiplier);
        }
    }
}
