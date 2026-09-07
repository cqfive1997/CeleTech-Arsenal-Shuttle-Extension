using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal sealed class SurfaceShieldFallbackResidualEffectsRenderer
    {
        private readonly SurfaceShieldFallbackDrawPrimitives drawPrimitives;
        private readonly CompModularShuttleSurfaceShieldVisual comp;
        private readonly CompProperties_ModularShuttleSurfaceShieldVisual props;

        internal SurfaceShieldFallbackResidualEffectsRenderer(
            SurfaceShieldFallbackDrawPrimitives drawPrimitives,
            CompModularShuttleSurfaceShieldVisual comp,
            CompProperties_ModularShuttleSurfaceShieldVisual props)
        {
            this.drawPrimitives = drawPrimitives;
            this.comp = comp;
            this.props = props;
        }

        internal void DrawArcCrawl(
            ShieldHitPulse pulse,
            Vector3 center,
            float targetRadius,
            float age,
            float strengthFactor,
            PulseStrengthTier tier,
            float tierAlpha,
            float globalAlpha,
            float breakAlphaMultiplier,
            float lowShieldFlicker,
            float lowInstability)
        {
            float crawlWindow = pulse.BrokeShield ? 0.32f : 0.42f;
            if (age < 0.05f || age > crawlWindow)
            {
                return;
            }

            float crawlAge = Mathf.Clamp01((age - 0.05f) / Mathf.Max(0.01f, crawlWindow - 0.05f));
            float alpha = 0.34f *
                SurfaceShieldFallbackTuning.GetFallbackCrawlAlpha(this.props) *
                globalAlpha *
                strengthFactor *
                tierAlpha *
                SurfaceShieldFallbackMath.FadeOutQuadratic(crawlAge) *
                (pulse.BrokeShield ? breakAlphaMultiplier * 1.18f : 1f) *
                (pulse.WasLowShieldAtHit ? Mathf.Lerp(0.7f, 1.05f, lowShieldFlicker) : 1f);
            if (alpha <= 0f)
            {
                return;
            }

            int count = tier == PulseStrengthTier.Break ? 5 : tier == PulseStrengthTier.Heavy ? 3 : 2;
            if (pulse.WasLowShieldAtHit && !pulse.BrokeShield)
            {
                count++;
            }

            for (int i = 0; i < count; i++)
            {
                int side = i % 2 == 0 ? -1 : 1;
                int lane = i / 2;
                float minAngle = pulse.BrokeShield ? 52f : 24f;
                float maxAngle = pulse.BrokeShield ? 86f : 58f;
                float offsetAngle = side * Mathf.Lerp(minAngle, maxAngle, lane / 2f) +
                    SurfaceShieldFallbackMath.PseudoRandomSigned(pulse.Seed + i * 211 + 101) * 7f;
                Vector2 direction = SurfaceShieldHullGeometry.RotateDirection(pulse.ImpactDirectionXZ, offsetAngle);
                float distance = Mathf.Lerp(targetRadius * 0.06f, targetRadius * 0.38f, SurfaceShieldFallbackMath.EaseOutSqrt(crawlAge));
                float length = Mathf.Lerp(0.12f, 0.26f, Mathf.Clamp01(pulse.Strength / 2f)) *
                    (pulse.BrokeShield ? 1.26f : 1f) *
                    (pulse.WasLowShieldAtHit ? 0.78f : 1f);
                float width = Mathf.Lerp(0.022f, 0.04f, SurfaceShieldFallbackMath.PseudoRandom01(pulse.Seed + i * 227 + 109));
                float angleDeg = SurfaceShieldHullGeometry.DirectionAngleDeg(direction);
                Vector3 crawlCenter = center + SurfaceShieldHullGeometry.DirectionToWorld(pulse.ImpactDirectionXZ) * targetRadius * 0.05f;
                Color color = new Color(0.72f, 0.9f, 1f, Mathf.Clamp01(alpha * Mathf.Lerp(0.72f, 1.1f, SurfaceShieldFallbackMath.PseudoRandom01(pulse.Seed + i * 239 + 113))));
                this.drawPrimitives.DrawShard(crawlCenter, angleDeg, distance, length, width, color);
            }
        }

        internal void DrawResidualGlowPatch(
            ShieldHitPulse pulse,
            Vector3 center,
            float targetRadius,
            float age,
            float strengthFactor,
            PulseStrengthTier tier,
            float globalAlpha,
            float breakAlphaMultiplier,
            float lowShieldFlicker,
            float lowInstability)
        {
            float residualWindow = pulse.BrokeShield ? 0.34f : 0.52f;
            float residualAge = Mathf.Clamp01((age - 0.08f) / residualWindow);
            if (pulse.BrokeShield && residualAge >= 1f)
            {
                return;
            }

            float tierScale = tier == PulseStrengthTier.Light ? 0.72f : tier == PulseStrengthTier.Heavy ? 1.18f : 1f;
            float alpha = 0.2f *
                SurfaceShieldFallbackTuning.GetFallbackResidualAlpha(this.props) *
                globalAlpha *
                strengthFactor *
                tierScale *
                SurfaceShieldFallbackMath.FadeOutSmooth(residualAge) *
                (pulse.BrokeShield ? breakAlphaMultiplier * 0.95f : 1f) *
                (pulse.WasLowShieldAtHit ? Mathf.Lerp(0.35f, 0.62f, lowShieldFlicker) : 1f);
            if (alpha <= 0f)
            {
                return;
            }

            Vector3 direction = SurfaceShieldHullGeometry.DirectionToWorld(pulse.ImpactDirectionXZ);
            Vector3 glowCenter = center + direction * targetRadius * 0.06f;
            float angleDeg = SurfaceShieldHullGeometry.DirectionAngleDeg(pulse.ImpactDirectionXZ);
            this.drawPrimitives.DrawMesh(
                SurfaceShieldMeshCache.GetFallbackDiscMesh(),
                glowCenter,
                Quaternion.Euler(0f, angleDeg, 0f),
                new Vector3(targetRadius * 0.78f, 1f, targetRadius * 0.46f),
                new Color(0.12f, 0.34f, 0.58f, Mathf.Clamp01(alpha)),
                0.17f);
        }

        internal void DrawFallbackSplashDots(
            ShieldHitPulse pulse,
            Vector3 center,
            float targetRadius,
            float age,
            float strengthFactor,
            float globalAlpha,
            float breakAlphaMultiplier)
        {
            int count = Mathf.Clamp(pulse.SplashCount, 0, 12);
            if (count <= 0)
            {
                return;
            }

            float splashAge = Mathf.Clamp01(age / 0.45f);
            float splashAlpha = SurfaceShieldFallbackTuning.GetFallbackSplashAlpha(this.props) *
                globalAlpha *
                strengthFactor *
                SurfaceShieldFallbackMath.FadeOutQuadratic(splashAge) *
                breakAlphaMultiplier;
            if (splashAlpha <= 0f)
            {
                return;
            }

            float spread = targetRadius * SurfaceShieldFallbackTuning.GetFallbackSplashRadiusFactor(this.props);
            for (int i = 0; i < count; i++)
            {
                Vector2 direction = this.GetSplashDirection(pulse.Seed, i, count, pulse.SplashRotation);
                float distanceJitter = Mathf.Lerp(0.55f, 1.15f, SurfaceShieldFallbackMath.PseudoRandom01(pulse.Seed + i * 37 + 5));
                float distance = Mathf.Lerp(0.08f, spread * distanceJitter, SurfaceShieldFallbackMath.EaseOutSqrt(splashAge));
                float dotRadius = Mathf.Lerp(0.045f, pulse.BrokeShield ? 0.13f : 0.095f, SurfaceShieldFallbackMath.PseudoRandom01(pulse.Seed + i * 53 + 11));
                Vector3 dotCenter = center + new Vector3(direction.x * distance, 0.015f + i * 0.0008f, direction.y * distance);
                float dotAlpha = splashAlpha * Mathf.Lerp(0.55f, 1.05f, SurfaceShieldFallbackMath.PseudoRandom01(pulse.Seed + i * 71 + 19));
                Color dotColor = pulse.BrokeShield
                    ? new Color(0.86f, 0.96f, 1f, Mathf.Clamp01(dotAlpha))
                    : new Color(0.42f, 0.78f, 0.9f, Mathf.Clamp01(dotAlpha));
                this.drawPrimitives.DrawDisc(dotCenter, dotRadius, dotColor);
            }
        }

        private Vector2 GetSplashDirection(int seed, int index, int count, float rotation)
        {
            return SurfaceShieldHullGeometry.GetSplashDirection(
                this.comp,
                seed,
                index,
                count,
                rotation);
        }
    }
}
