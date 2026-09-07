using System;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal enum PulseStrengthTier
    {
        Light,
        Medium,
        Heavy,
        Break
    }

    internal sealed class SurfaceShieldFallbackImpactSparkRenderer
    {
        private readonly Func<float> getMinPulseRadius;
        private readonly Func<float> getMaxPulseRadius;
        private readonly Func<float> getFallbackBreakRadiusMultiplier;
        private readonly Func<ShieldHitPulse, float, float> getSurfaceBoundFallbackRadius;
        private readonly Func<ShieldHitPulse, PulseStrengthTier> resolvePulseStrengthTier;
        private readonly Func<PulseStrengthTier, float> getTierAlphaMultiplier;
        private readonly Func<float> getFallbackBreakAlphaMultiplier;
        private readonly Func<int, float> getVisibilityMultiplier;
        private readonly Func<float> getFallbackBaseAlpha;
        private readonly Func<ShieldHitPulse, int, float> getLowShieldFlicker;
        private readonly DrawOutwardSparkBurstDelegate drawOutwardSparkBurst;
        private readonly DrawEnergyShardsDelegate drawEnergyShards;

        internal SurfaceShieldFallbackImpactSparkRenderer(
            Func<float> getMinPulseRadius,
            Func<float> getMaxPulseRadius,
            Func<float> getFallbackBreakRadiusMultiplier,
            Func<ShieldHitPulse, float, float> getSurfaceBoundFallbackRadius,
            Func<ShieldHitPulse, PulseStrengthTier> resolvePulseStrengthTier,
            Func<PulseStrengthTier, float> getTierAlphaMultiplier,
            Func<float> getFallbackBreakAlphaMultiplier,
            Func<int, float> getVisibilityMultiplier,
            Func<float> getFallbackBaseAlpha,
            Func<ShieldHitPulse, int, float> getLowShieldFlicker,
            DrawOutwardSparkBurstDelegate drawOutwardSparkBurst,
            DrawEnergyShardsDelegate drawEnergyShards)
        {
            this.getMinPulseRadius = getMinPulseRadius;
            this.getMaxPulseRadius = getMaxPulseRadius;
            this.getFallbackBreakRadiusMultiplier = getFallbackBreakRadiusMultiplier;
            this.getSurfaceBoundFallbackRadius = getSurfaceBoundFallbackRadius;
            this.resolvePulseStrengthTier = resolvePulseStrengthTier;
            this.getTierAlphaMultiplier = getTierAlphaMultiplier;
            this.getFallbackBreakAlphaMultiplier = getFallbackBreakAlphaMultiplier;
            this.getVisibilityMultiplier = getVisibilityMultiplier;
            this.getFallbackBaseAlpha = getFallbackBaseAlpha;
            this.getLowShieldFlicker = getLowShieldFlicker;
            this.drawOutwardSparkBurst = drawOutwardSparkBurst;
            this.drawEnergyShards = drawEnergyShards;
        }

        internal void DrawImpactSparksOverShader(
            CompModularShuttleSurfaceShieldVisual comp,
            ShieldHitPulse[] pulses,
            int activePulseCount,
            int ticksGame,
            float currentShieldStrength,
            bool currentShieldBroken)
        {
            if (pulses == null || pulses.Length == 0 || activePulseCount <= 0)
            {
                return;
            }

            CompProperties_ModularShuttleSurfaceShieldVisual props = this.GetProps(comp);
            float alphaMultiplier = Mathf.Clamp01(props.shaderOverlaySparkAlphaMultiplier);
            if (alphaMultiplier <= 0f)
            {
                return;
            }

            int maxPulses = Mathf.Clamp(props.maxShaderOverlaySparkPulses, 0, 4);
            if (maxPulses <= 0)
            {
                return;
            }

            int selected0 = -1;
            int selected1 = -1;
            int selected2 = -1;
            int selected3 = -1;
            for (int drawn = 0; drawn < maxPulses; drawn++)
            {
                int pulseIndex = this.FindLatestValidPulseIndexExcluding(
                    pulses,
                    selected0,
                    selected1,
                    selected2,
                    selected3);
                if (pulseIndex < 0)
                {
                    return;
                }

                if (drawn == 0)
                {
                    selected0 = pulseIndex;
                }
                else if (drawn == 1)
                {
                    selected1 = pulseIndex;
                }
                else if (drawn == 2)
                {
                    selected2 = pulseIndex;
                }
                else
                {
                    selected3 = pulseIndex;
                }

                this.DrawImpactSparkShardOverlay(
                    comp,
                    pulses[pulseIndex],
                    ticksGame,
                    currentShieldStrength,
                    currentShieldBroken,
                    alphaMultiplier);
            }
        }

        private int FindLatestValidPulseIndexExcluding(
            ShieldHitPulse[] pulses,
            int excluded0,
            int excluded1,
            int excluded2,
            int excluded3)
        {
            if (pulses == null)
            {
                return -1;
            }

            int bestIndex = -1;
            int bestStartTick = int.MinValue;
            for (int i = 0; i < pulses.Length; i++)
            {
                if (!pulses[i].Valid ||
                    i == excluded0 ||
                    i == excluded1 ||
                    i == excluded2 ||
                    i == excluded3)
                {
                    continue;
                }

                int startTick = pulses[i].StartTick;
                if (startTick > bestStartTick)
                {
                    bestStartTick = startTick;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        private void DrawImpactSparkShardOverlay(
            CompModularShuttleSurfaceShieldVisual comp,
            ShieldHitPulse pulse,
            int ticksGame,
            float currentShieldStrength,
            bool currentShieldBroken,
            float alphaMultiplier)
        {
            if (!pulse.Valid || comp.parent == null)
            {
                return;
            }

            CompProperties_ModularShuttleSurfaceShieldVisual props = this.GetProps(comp);
            int lifetime = Mathf.Max(1, props.pulseLifetimeTicks);
            float age = Mathf.Clamp01((ticksGame - pulse.StartTick) / (float)lifetime);
            float shardWindow = pulse.BrokeShield ? 0.28f : 0.35f;
            float sparkWindow = pulse.BrokeShield ? 0.34f : Mathf.Clamp(props.impactSparkLifetimeFactor, 0.08f, 0.6f);
            if (age > Mathf.Max(shardWindow, sparkWindow))
            {
                return;
            }

            float targetRadius = Mathf.Max(this.getMinPulseRadius(), pulse.Radius);
            if (pulse.BrokeShield)
            {
                targetRadius = Mathf.Max(targetRadius * this.getFallbackBreakRadiusMultiplier(), this.getMaxPulseRadius() * 0.9f);
            }

            targetRadius = this.getSurfaceBoundFallbackRadius(pulse, targetRadius);

            Vector3 center = pulse.WorldPosition;
            if (center == Vector3.zero)
            {
                center = comp.parent.DrawPos;
            }

            PulseStrengthTier tier = this.resolvePulseStrengthTier(pulse);
            float strengthFactor = Mathf.Clamp(pulse.Strength, 0.85f, 2f);
            float tierAlpha = this.getTierAlphaMultiplier(tier);
            float breakAlphaMultiplier = pulse.BrokeShield ? this.getFallbackBreakAlphaMultiplier() : 1f;
            float visibilityMultiplier = this.getVisibilityMultiplier(ticksGame);
            float globalAlpha = this.getFallbackBaseAlpha() *
                visibilityMultiplier *
                Mathf.Lerp(0.88f, 1f, Mathf.Clamp01(currentShieldStrength)) *
                alphaMultiplier;
            if (currentShieldBroken)
            {
                globalAlpha *= 0.9f;
            }

            bool lowShield = pulse.WasLowShieldAtHit && !pulse.BrokeShield;
            float lowShieldFlicker = lowShield ? this.getLowShieldFlicker(pulse, ticksGame) : 1f;
            float lowInstability = lowShield ? 1f - Mathf.Clamp01(pulse.ShieldStrengthAtHit) : 0f;

            if (age <= sparkWindow)
            {
                this.drawOutwardSparkBurst(
                    pulse,
                    center,
                    targetRadius,
                    age,
                    strengthFactor,
                    tier,
                    tierAlpha,
                    globalAlpha,
                    breakAlphaMultiplier,
                    lowShieldFlicker);
            }

            if (age <= shardWindow)
            {
                this.drawEnergyShards(
                    pulse,
                    center,
                    targetRadius,
                    age,
                    strengthFactor,
                    tier,
                    tierAlpha,
                    globalAlpha,
                    breakAlphaMultiplier,
                    lowShieldFlicker,
                    lowInstability);
            }
        }

        private CompProperties_ModularShuttleSurfaceShieldVisual GetProps(
            CompModularShuttleSurfaceShieldVisual comp)
        {
            return (CompProperties_ModularShuttleSurfaceShieldVisual)comp.props;
        }

        internal delegate void DrawOutwardSparkBurstDelegate(
            ShieldHitPulse pulse,
            Vector3 center,
            float targetRadius,
            float age,
            float strengthFactor,
            PulseStrengthTier tier,
            float tierAlpha,
            float globalAlpha,
            float breakAlphaMultiplier,
            float lowShieldFlicker);

        internal delegate void DrawEnergyShardsDelegate(
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
            float lowInstability);
    }
}
