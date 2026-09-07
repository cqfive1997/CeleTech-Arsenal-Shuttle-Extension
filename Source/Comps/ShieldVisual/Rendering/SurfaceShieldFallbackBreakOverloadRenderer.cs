using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal sealed class SurfaceShieldFallbackBreakOverloadRenderer
    {
        private readonly SurfaceShieldFallbackDrawPrimitives drawPrimitives;

        internal SurfaceShieldFallbackBreakOverloadRenderer(
            SurfaceShieldFallbackDrawPrimitives drawPrimitives)
        {
            this.drawPrimitives = drawPrimitives;
        }

        internal void DrawBreakOverloadFlash(
            CompProperties_ModularShuttleSurfaceShieldVisual props,
            ShieldHitPulse pulse,
            Vector3 center,
            float targetRadius,
            float age,
            float strengthFactor,
            float globalAlpha)
        {
            float overloadAge = Mathf.Clamp01(age / 0.16f);
            float radius = Mathf.Lerp(0.18f, targetRadius * 0.42f, SurfaceShieldFallbackMath.EaseOutSqrt(overloadAge));
            float alpha = 0.95f *
                SurfaceShieldFallbackTuning.GetFallbackFlashAlpha(props) *
                globalAlpha *
                strengthFactor *
                SurfaceShieldFallbackMath.FadeOutQuadratic(overloadAge);
            this.drawPrimitives.DrawDisc(
                center,
                radius,
                new Color(0.92f, 0.98f, 1f, Mathf.Clamp01(alpha)));
        }

        internal void DrawDirectionalShockCrescent(
            CompProperties_ModularShuttleSurfaceShieldVisual props,
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
            float shockAge = Mathf.Clamp01(age / 0.28f);
            float alpha = 0.32f *
                SurfaceShieldFallbackTuning.GetFallbackDirectionalAlpha(props) *
                globalAlpha *
                strengthFactor *
                tierAlpha *
                SurfaceShieldFallbackMath.FadeOutQuadratic(shockAge) *
                (pulse.BrokeShield ? breakAlphaMultiplier * 1.15f : 1f) *
                (pulse.WasLowShieldAtHit ? Mathf.Lerp(0.56f, 0.92f, lowShieldFlicker) : 1f);
            if (alpha <= 0f)
            {
                return;
            }

            int count = tier == PulseStrengthTier.Break ? 5 : tier == PulseStrengthTier.Heavy ? 4 : 3;
            float baseAngle = SurfaceShieldHullGeometry.DirectionAngleDeg(pulse.ImpactDirectionXZ);
            for (int i = 0; i < count; i++)
            {
                float normalizedIndex = count <= 1 ? 0.5f : i / (float)(count - 1);
                float offsetAngle = Mathf.Lerp(-38f, 38f, normalizedIndex) +
                    SurfaceShieldFallbackMath.PseudoRandomSigned(pulse.Seed + i * 181 + 67) * 8f;
                Vector2 direction = SurfaceShieldHullGeometry.RotateDirection(pulse.ImpactDirectionXZ, offsetAngle);
                float distance = Mathf.Lerp(targetRadius * 0.05f, targetRadius * 0.34f, SurfaceShieldFallbackMath.EaseOutSqrt(shockAge));
                Vector3 cellCenter = center +
                    SurfaceShieldHullGeometry.DirectionToWorld(direction) * distance +
                    SurfaceShieldHullGeometry.DirectionToWorld(pulse.ImpactDirectionXZ) * targetRadius * 0.08f;
                float cellRadius = Mathf.Lerp(0.07f, 0.14f, Mathf.Clamp01(pulse.Strength / 2f)) *
                    Mathf.Lerp(0.85f, 1.15f, SurfaceShieldFallbackMath.PseudoRandom01(pulse.Seed + i * 199 + 71));
                this.drawPrimitives.DrawHexCell(
                    cellCenter,
                    cellRadius,
                    baseAngle + offsetAngle + i * 9f,
                    new Color(0.58f, 0.82f, 0.94f, Mathf.Clamp01(alpha * Mathf.Lerp(0.62f, 1.08f, normalizedIndex))));
            }
        }

        internal void DrawBreakOverloadShockRing(
            CompProperties_ModularShuttleSurfaceShieldVisual props,
            ShieldHitPulse pulse,
            Vector3 center,
            float targetRadius,
            float age,
            float strengthFactor,
            float globalAlpha,
            float breakAlphaMultiplier)
        {
            float breakAge = Mathf.Clamp01(age / 0.62f);
            float radius = Mathf.Lerp(targetRadius * 0.25f, targetRadius * 1.35f, SurfaceShieldFallbackMath.EaseOutSqrt(breakAge));
            float alpha = 0.34f *
                SurfaceShieldFallbackTuning.GetFallbackSecondRingAlpha(props) *
                globalAlpha *
                strengthFactor *
                SurfaceShieldFallbackMath.FadeOutQuadratic(breakAge) *
                breakAlphaMultiplier;
            this.drawPrimitives.DrawThinRing(
                center,
                radius,
                new Color(0.7f, 0.86f, 0.98f, Mathf.Clamp01(alpha)));
        }
    }
}
