using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal sealed class SurfaceShieldFallbackFrontPatchRenderer
    {
        private readonly SurfaceShieldFallbackDrawPrimitives drawPrimitives;
        private readonly CompProperties_ModularShuttleSurfaceShieldVisual props;

        internal SurfaceShieldFallbackFrontPatchRenderer(
            SurfaceShieldFallbackDrawPrimitives drawPrimitives,
            CompProperties_ModularShuttleSurfaceShieldVisual props)
        {
            this.drawPrimitives = drawPrimitives;
            this.props = props;
        }

        internal void DrawAnimatedHexPatch(
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
            float activeWindow = pulse.BrokeShield ? 0.42f : 0.38f;
            float hexAge = Mathf.Clamp01(age / activeWindow);
            float hexAlpha = SurfaceShieldFallbackTuning.GetFallbackHexAlpha(this.props) *
                globalAlpha *
                strengthFactor *
                tierAlpha *
                (pulse.BrokeShield ? breakAlphaMultiplier * 1.15f : 1f) *
                (pulse.WasLowShieldAtHit ? Mathf.Lerp(0.58f, 0.92f, lowShieldFlicker) : 1f);
            if (hexAlpha <= 0f)
            {
                return;
            }

            int count = this.ResolveHexCount(tier);
            if (pulse.WasLowShieldAtHit && !pulse.BrokeShield)
            {
                count = Mathf.Max(4, count - 2);
            }

            float patchRadius = targetRadius * Mathf.Lerp(0.18f, 0.35f, SurfaceShieldFallbackMath.EaseOutSqrt(hexAge));
            float cellRadius = Mathf.Lerp(0.095f, 0.205f, Mathf.Clamp01(pulse.Strength / 2f));
            if (pulse.BrokeShield)
            {
                cellRadius *= 1.15f;
            }

            float rotationDeg = SurfaceShieldFallbackMath.PseudoRandomSigned(pulse.Seed + 211) * 18f;
            Vector3 directionalBias = SurfaceShieldHullGeometry.DirectionToWorld(pulse.ImpactDirectionXZ) *
                targetRadius *
                (tier == PulseStrengthTier.Light ? 0.04f : 0.08f);
            for (int i = 0; i < count; i++)
            {
                float localDelay = i * (pulse.BrokeShield ? 0.012f : 0.018f);
                if (age < localDelay)
                {
                    continue;
                }

                float localT = Mathf.Clamp01((age - localDelay) / Mathf.Max(0.01f, activeWindow - localDelay));
                float localFade = SurfaceShieldFallbackMath.FadeInFast(localT) *
                    SurfaceShieldFallbackMath.FadeOutQuadratic(localT);
                if (localFade <= 0f)
                {
                    continue;
                }

                Vector3 cellOffset = i == 0
                    ? Vector3.zero
                    : this.GetHexPatchOffset(i - 1, pulse.Seed + 307, patchRadius, rotationDeg);
                if (pulse.BrokeShield)
                {
                    cellOffset *= 1f + hexAge * 0.35f;
                    if (hexAge > 0.22f && SurfaceShieldFallbackMath.PseudoRandom01(pulse.Seed + i * 223 + 131) < hexAge * 0.32f)
                    {
                        continue;
                    }
                }

                float scaleJitter = Mathf.Lerp(0.82f, 1.18f, SurfaceShieldFallbackMath.PseudoRandom01(pulse.Seed + i * 173 + 83));
                float alphaJitter = Mathf.Lerp(0.58f, 1.08f, SurfaceShieldFallbackMath.PseudoRandom01(pulse.Seed + i * 191 + 97));
                if (pulse.BrokeShield)
                {
                    alphaJitter *= Mathf.Lerp(0.55f, 1.28f, SurfaceShieldFallbackMath.PseudoRandom01(pulse.Seed + i * 241 + 137));
                }

                Color color = pulse.BrokeShield
                    ? new Color(0.68f, 0.9f, 1f, Mathf.Clamp01(hexAlpha * localFade * alphaJitter * 1.12f))
                    : new Color(0.42f, 0.78f, 0.88f, Mathf.Clamp01(hexAlpha * localFade * alphaJitter));
                this.drawPrimitives.DrawHexCell(
                    center + directionalBias + cellOffset,
                    cellRadius * scaleJitter,
                    rotationDeg + i * 11f,
                    color);
            }
        }

        internal void DrawCompressionFront(
            ShieldHitPulse pulse,
            Vector3 center,
            float targetRadius,
            float age,
            float strengthFactor,
            float tierAlpha,
            float globalAlpha,
            float breakAlphaMultiplier,
            float lowShieldFlicker,
            float lowInstability)
        {
            float compressionAge = Mathf.Clamp01(age / 0.15f);
            float alpha = 0.18f *
                SurfaceShieldFallbackTuning.GetFallbackDirectionalAlpha(this.props) *
                globalAlpha *
                strengthFactor *
                tierAlpha *
                SurfaceShieldFallbackMath.FadeOutQuadratic(compressionAge) *
                (pulse.BrokeShield ? breakAlphaMultiplier : 1f) *
                (pulse.WasLowShieldAtHit ? Mathf.Lerp(0.62f, 0.95f, lowShieldFlicker) : 1f);
            if (alpha <= 0f)
            {
                return;
            }

            Vector3 direction = SurfaceShieldHullGeometry.DirectionToWorld(pulse.ImpactDirectionXZ);
            Vector3 frontCenter = center + direction * Mathf.Lerp(targetRadius * 0.04f, targetRadius * 0.16f, SurfaceShieldFallbackMath.EaseOutSqrt(compressionAge));
            float angleDeg = SurfaceShieldHullGeometry.DirectionAngleDeg(pulse.ImpactDirectionXZ);
            this.drawPrimitives.DrawMesh(
                SurfaceShieldMeshCache.GetFallbackDiscMesh(),
                frontCenter,
                Quaternion.Euler(0f, angleDeg, 0f),
                new Vector3(targetRadius * 0.5f, 1f, targetRadius * 0.18f),
                new Color(0.48f, 0.74f, 0.9f, Mathf.Clamp01(alpha)),
                0.2f);
        }

        private Vector3 GetHexPatchOffset(int index, int seed, float patchRadius, float rotationDeg)
        {
            int ringIndex = index % 6;
            int outerRing = index / 6;
            float angleDeg = rotationDeg + ringIndex * 60f + SurfaceShieldFallbackMath.PseudoRandomSigned(seed + index * 43) * 8f;
            float distance = patchRadius * Mathf.Lerp(
                outerRing == 0 ? 0.55f : 0.82f,
                outerRing == 0 ? 0.82f : 1.08f,
                SurfaceShieldFallbackMath.PseudoRandom01(seed + index * 59 + 7));
            float angle = angleDeg * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(angle) * distance, 0.03f + index * 0.0007f, Mathf.Cos(angle) * distance);
        }

        private int ResolveHexCount(PulseStrengthTier tier)
        {
            return SurfaceShieldFallbackTuning.ResolveHexCountForTier(
                this.props.fallbackHexCount,
                this.GetFallbackTierCode(tier));
        }

        private int GetFallbackTierCode(PulseStrengthTier tier)
        {
            if (tier == PulseStrengthTier.Light)
            {
                return SurfaceShieldFallbackTuning.LightTier;
            }

            if (tier == PulseStrengthTier.Heavy)
            {
                return SurfaceShieldFallbackTuning.HeavyTier;
            }

            if (tier == PulseStrengthTier.Break)
            {
                return SurfaceShieldFallbackTuning.BreakTier;
            }

            return SurfaceShieldFallbackTuning.MediumTier;
        }
    }
}
