using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal sealed class SurfaceShieldFallbackShardBurstRenderer
    {
        private readonly SurfaceShieldFallbackDrawPrimitives drawPrimitives;
        private readonly CompModularShuttleSurfaceShieldVisual comp;
        private readonly CompProperties_ModularShuttleSurfaceShieldVisual props;

        internal SurfaceShieldFallbackShardBurstRenderer(
            SurfaceShieldFallbackDrawPrimitives drawPrimitives,
            CompModularShuttleSurfaceShieldVisual comp,
            CompProperties_ModularShuttleSurfaceShieldVisual props)
        {
            this.drawPrimitives = drawPrimitives;
            this.comp = comp;
            this.props = props;
        }

        internal void DrawEnergyShards(
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
            int shardCount = this.ResolveShardCount(tier);
            if (pulse.WasLowShieldAtHit && !pulse.BrokeShield)
            {
                shardCount += 2;
            }

            float shardWindow = pulse.BrokeShield ? 0.28f : 0.35f;
            float shardAge = Mathf.Clamp01(age / shardWindow);
            float alpha = 0.42f *
                SurfaceShieldFallbackTuning.GetFallbackShardAlpha(this.props) *
                globalAlpha *
                strengthFactor *
                tierAlpha *
                SurfaceShieldFallbackMath.FadeOutQuadratic(shardAge) *
                breakAlphaMultiplier *
                (pulse.WasLowShieldAtHit ? Mathf.Lerp(0.72f, 1.08f, lowShieldFlicker) : 1f);
            if (alpha <= 0f)
            {
                return;
            }

            for (int i = 0; i < shardCount; i++)
            {
                Vector2 direction = this.GetSplashDirection(pulse.Seed + 131, i, shardCount, pulse.SplashRotation);
                float angleDeg = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
                float jitter = Mathf.Lerp(0.85f, 1.2f, SurfaceShieldFallbackMath.PseudoRandom01(pulse.Seed + i * 89 + 31));
                float distance = Mathf.Lerp(0.05f, targetRadius * 0.45f * jitter, SurfaceShieldFallbackMath.EaseOutSqrt(shardAge));
                float length = Mathf.Lerp(0.1f, 0.22f, Mathf.Clamp01(pulse.Strength / 2f)) *
                    (pulse.BrokeShield ? 1.2f : 1f) *
                    (pulse.WasLowShieldAtHit ? 0.72f : 1f) *
                    Mathf.Lerp(0.85f, 1.18f, SurfaceShieldFallbackMath.PseudoRandom01(pulse.Seed + i * 107 + 43));
                float width = Mathf.Lerp(0.03f, 0.05f, SurfaceShieldFallbackMath.PseudoRandom01(pulse.Seed + i * 127 + 47));
                float shardAlpha = alpha * Mathf.Lerp(0.65f, 1.15f, SurfaceShieldFallbackMath.PseudoRandom01(pulse.Seed + i * 151 + 59));
                Color color = pulse.BrokeShield
                    ? new Color(0.84f, 0.94f, 1f, Mathf.Clamp01(shardAlpha * 1.15f))
                    : new Color(0.7f, 0.88f, 0.98f, Mathf.Clamp01(shardAlpha));
                this.drawPrimitives.DrawShard(center, angleDeg, distance, length, width, color);
            }
        }

        internal void DrawOutwardSparkBurst(
            ShieldHitPulse pulse,
            Vector3 center,
            float targetRadius,
            float age,
            float strengthFactor,
            PulseStrengthTier tier,
            float tierAlpha,
            float globalAlpha,
            float breakAlphaMultiplier,
            float lowShieldFlicker)
        {
            int sparkCount = Mathf.Clamp(this.props.impactSparkCount + (pulse.BrokeShield ? 3 : 0), 0, 16);
            float sparkAlphaProp = Mathf.Max(0f, this.props.impactSparkAlpha);
            if (sparkCount <= 0 || sparkAlphaProp <= 0f)
            {
                return;
            }

            float sparkWindow = pulse.BrokeShield ? 0.34f : Mathf.Clamp(this.props.impactSparkLifetimeFactor, 0.08f, 0.6f);
            if (age > sparkWindow)
            {
                return;
            }

            Vector2 outward = this.ResolveHullSurfaceOutwardDirection(center, pulse.ImpactDirectionXZ);
            if (outward.sqrMagnitude <= 0.0001f)
            {
                outward = pulse.ImpactDirectionXZ.sqrMagnitude > 0.0001f ? pulse.ImpactDirectionXZ.normalized : new Vector2(0f, 1f);
            }

            float sparkAge = Mathf.Clamp01(age / Mathf.Max(0.01f, sparkWindow));
            float baseAlpha = 0.62f *
                sparkAlphaProp *
                globalAlpha *
                strengthFactor *
                tierAlpha *
                SurfaceShieldFallbackMath.FadeOutQuadratic(sparkAge) *
                breakAlphaMultiplier *
                (pulse.WasLowShieldAtHit ? Mathf.Lerp(0.74f, 1.12f, lowShieldFlicker) : 1f);
            if (baseAlpha <= 0f)
            {
                return;
            }

            float speed = Mathf.Max(0.05f, this.props.impactSparkSpeed);
            float spread = Mathf.Clamp(this.props.impactSparkSpreadAngle, 0f, 90f);
            float configuredLength = Mathf.Max(0.02f, this.props.impactSparkLength);
            for (int i = 0; i < sparkCount; i++)
            {
                float lane = sparkCount <= 1 ? 0f : (i / (float)(sparkCount - 1)) * 2f - 1f;
                float jitterAngle = SurfaceShieldFallbackMath.PseudoRandomSigned(pulse.Seed + i * 313 + 157) * spread * 0.22f;
                float angleOffset = lane * spread + jitterAngle;
                Vector2 direction = SurfaceShieldHullGeometry.RotateDirection(outward, angleOffset);
                float distanceJitter = Mathf.Lerp(0.82f, 1.24f, SurfaceShieldFallbackMath.PseudoRandom01(pulse.Seed + i * 337 + 163));
                float distance = Mathf.Lerp(
                    targetRadius * 0.035f,
                    targetRadius * 0.52f * speed * distanceJitter,
                    SurfaceShieldFallbackMath.EaseOutSqrt(sparkAge));
                float length = configuredLength *
                    Mathf.Lerp(0.78f, 1.28f, SurfaceShieldFallbackMath.PseudoRandom01(pulse.Seed + i * 359 + 179)) *
                    Mathf.Lerp(1f, 0.62f, sparkAge) *
                    (pulse.BrokeShield ? 1.18f : 1f);
                float width = Mathf.Clamp(length * Mathf.Lerp(0.1f, 0.16f, SurfaceShieldFallbackMath.PseudoRandom01(pulse.Seed + i * 383 + 191)), 0.018f, 0.055f);
                float sparkAlpha = baseAlpha * Mathf.Lerp(0.72f, 1.24f, SurfaceShieldFallbackMath.PseudoRandom01(pulse.Seed + i * 401 + 199));
                Color color = pulse.BrokeShield
                    ? new Color(0.94f, 1f, 1f, Mathf.Clamp01(sparkAlpha * 1.12f))
                    : new Color(0.78f, 1f, 1f, Mathf.Clamp01(sparkAlpha));
                this.drawPrimitives.DrawShard(center, SurfaceShieldHullGeometry.DirectionAngleDeg(direction), distance, length, width, color);
            }
        }

        private int ResolveShardCount(PulseStrengthTier tier)
        {
            return SurfaceShieldFallbackTuning.ResolveShardCount(this.GetFallbackTierCode(tier));
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

        private Vector2 ResolveHullSurfaceOutwardDirection(Vector3 hullSurfaceWorld, Vector2 impactDirectionXZ)
        {
            return SurfaceShieldHullGeometry.ResolveHullSurfaceOutwardDirection(
                this.comp,
                hullSurfaceWorld,
                impactDirectionXZ);
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
