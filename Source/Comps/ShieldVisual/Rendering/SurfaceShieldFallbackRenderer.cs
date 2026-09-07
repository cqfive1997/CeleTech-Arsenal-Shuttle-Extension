using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal sealed class SurfaceShieldFallbackRenderer
    {
        private readonly CompModularShuttleSurfaceShieldVisual comp;
        private readonly ShieldHitPulse[] pulses;
        private readonly int activePulseCount;
        private readonly SurfaceShieldFallbackDrawPrimitives drawPrimitives;
        private readonly SurfaceShieldFallbackBreakOverloadRenderer breakOverloadRenderer;
        private readonly SurfaceShieldFallbackFrontPatchRenderer frontPatchRenderer;
        private readonly SurfaceShieldFallbackImpactSparkRenderer impactSparkRenderer;
        private readonly SurfaceShieldFallbackResidualEffectsRenderer residualEffectsRenderer;
        private readonly SurfaceShieldFallbackShardBurstRenderer shardBurstRenderer;

        private SurfaceShieldFallbackRenderer(
            CompModularShuttleSurfaceShieldVisual comp,
            ShieldHitPulse[] pulses,
            int activePulseCount,
            MaterialPropertyBlock propertyBlock)
        {
            this.comp = comp;
            this.pulses = pulses;
            this.activePulseCount = activePulseCount;
            this.drawPrimitives = new SurfaceShieldFallbackDrawPrimitives(propertyBlock);
            this.breakOverloadRenderer =
                new SurfaceShieldFallbackBreakOverloadRenderer(this.drawPrimitives);
            this.frontPatchRenderer =
                new SurfaceShieldFallbackFrontPatchRenderer(this.drawPrimitives, this.Props);
            this.residualEffectsRenderer =
                new SurfaceShieldFallbackResidualEffectsRenderer(this.drawPrimitives, this.comp, this.Props);
            this.shardBurstRenderer =
                new SurfaceShieldFallbackShardBurstRenderer(this.drawPrimitives, this.comp, this.Props);
            this.impactSparkRenderer = new SurfaceShieldFallbackImpactSparkRenderer(
                this.GetMinPulseRadius,
                this.GetMaxPulseRadius,
                this.GetFallbackBreakRadiusMultiplier,
                this.GetSurfaceBoundFallbackRadius,
                this.ResolvePulseStrengthTier,
                this.GetTierAlphaMultiplier,
                this.GetFallbackBreakAlphaMultiplier,
                this.GetVisibilityMultiplier,
                this.GetFallbackBaseAlpha,
                this.GetLowShieldFlicker,
                this.shardBurstRenderer.DrawOutwardSparkBurst,
                this.shardBurstRenderer.DrawEnergyShards);
        }

        private MaterialPropertyBlock PropertyBlock
        {
            get { return this.drawPrimitives.PropertyBlock; }
        }

        internal static void DrawFallbackHullAttachedIdleSurface(
            CompModularShuttleSurfaceShieldVisual comp,
            int ticksGame,
            float shieldStrength,
            ref MaterialPropertyBlock propertyBlock)
        {
            SurfaceShieldFallbackRenderer renderer = new SurfaceShieldFallbackRenderer(comp, null, 0, propertyBlock);
            renderer.DrawFallbackHullAttachedIdleSurface(ticksGame, shieldStrength);
            propertyBlock = renderer.PropertyBlock;
        }

        internal static void DrawFallbackPulses(
            CompModularShuttleSurfaceShieldVisual comp,
            ShieldHitPulse[] pulses,
            int activePulseCount,
            int ticksGame,
            float currentShieldStrength,
            bool currentShieldBroken,
            ref MaterialPropertyBlock propertyBlock)
        {
            SurfaceShieldFallbackRenderer renderer = new SurfaceShieldFallbackRenderer(comp, pulses, activePulseCount, propertyBlock);
            renderer.DrawFallbackPulses(ticksGame, currentShieldStrength, currentShieldBroken);
            propertyBlock = renderer.PropertyBlock;
        }

        internal static void DrawImpactSparksOverShader(
            CompModularShuttleSurfaceShieldVisual comp,
            ShieldHitPulse[] pulses,
            int activePulseCount,
            int ticksGame,
            float currentShieldStrength,
            bool currentShieldBroken,
            ref MaterialPropertyBlock propertyBlock)
        {
            SurfaceShieldFallbackRenderer renderer = new SurfaceShieldFallbackRenderer(comp, pulses, activePulseCount, propertyBlock);
            renderer.DrawImpactSparksOverShader(ticksGame, currentShieldStrength, currentShieldBroken);
            propertyBlock = renderer.PropertyBlock;
        }

        private void DrawFallbackHullAttachedIdleSurface(int ticksGame, float shieldStrength)
        {
            if (this.comp.parent == null || !this.comp.parent.Spawned)
            {
                return;
            }

            Material material = SurfaceShieldMaterialCache.GetFallbackHullOverlayMaterial();
            Texture2D hullTexture = this.TryGetCurrentHullTexture();
            Mesh mesh = this.TryGetCurrentHullMesh();
            if (material == null || hullTexture == null || mesh == null)
            {
                return;
            }

            float strength = Mathf.Clamp01(shieldStrength);
            if (strength <= 0f)
            {
                return;
            }

            float lowShield = Mathf.Clamp01((0.35f - strength) / 0.35f);
            float flicker = 1f;
            if (lowShield > 0f)
            {
                float coarse = this.PseudoRandom01((ticksGame / 4) * 977 + 101);
                float fine = this.PseudoRandom01(ticksGame * 331 + 107);
                float flickerDepth = Mathf.Clamp01(this.Props.idleSurfaceLowShieldFlickerAlpha);
                flicker = Mathf.Lerp(1f, Mathf.Clamp(1f - flickerDepth + coarse * flickerDepth * 0.72f + fine * flickerDepth * 0.38f, 0.48f, 1.08f), lowShield);
            }

            float visibilityMultiplier = this.GetVisibilityMultiplier(ticksGame);
            float alpha = Mathf.Clamp01(this.Props.idleSurfaceBaseAlpha * Mathf.Lerp(0.45f, 1f, strength) * flicker * visibilityMultiplier);
            Color baseColor = this.GetCurrentShieldOverlayColor();
            Color color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            material.mainTexture = hullTexture;
            MaterialPropertyBlock block =
                this.drawPrimitives.ConfigureTexturedColorBlock(hullTexture, color);

            Matrix4x4 matrix = Matrix4x4.TRS(
                this.GetCurrentHullDrawLoc(),
                this.GetCurrentHullRotation(),
                this.GetCurrentHullDrawScale());
            Graphics.DrawMesh(
                mesh,
                matrix,
                material,
                0,
                null,
                0,
                block);
        }

        private void DrawFallbackPulses(int ticksGame, float currentShieldStrength, bool currentShieldBroken)
        {
            if (this.pulses == null || this.pulses.Length == 0 || this.activePulseCount <= 0)
            {
                return;
            }

            for (int i = 0; i < this.pulses.Length; i++)
            {
                if (!this.pulses[i].Valid)
                {
                    continue;
                }

                this.DrawFallbackPulse(this.pulses[i], ticksGame, currentShieldStrength, currentShieldBroken);
            }
        }

        private void DrawImpactSparksOverShader(
            int ticksGame,
            float currentShieldStrength,
            bool currentShieldBroken)
        {
            this.impactSparkRenderer.DrawImpactSparksOverShader(
                this.comp,
                this.pulses,
                this.activePulseCount,
                ticksGame,
                currentShieldStrength,
                currentShieldBroken);
        }

        private void DrawFallbackPulse(
            ShieldHitPulse pulse,
            int ticksGame,
            float currentShieldStrength,
            bool currentShieldBroken)
        {
            if (!pulse.Valid || this.comp.parent == null)
            {
                return;
            }

            if (!this.drawPrimitives.HasPulseMaterial())
            {
                return;
            }

            int lifetime = Mathf.Max(1, this.Props.pulseLifetimeTicks);
            float age = Mathf.Clamp01((ticksGame - pulse.StartTick) / (float)lifetime);
            float fade = 1f - age;
            if (fade <= 0f)
            {
                return;
            }

            float targetRadius = Mathf.Max(this.GetMinPulseRadius(), pulse.Radius);
            if (pulse.BrokeShield)
            {
                targetRadius = Mathf.Max(targetRadius * this.GetFallbackBreakRadiusMultiplier(), this.GetMaxPulseRadius() * 0.9f);
            }

            targetRadius = this.GetSurfaceBoundFallbackRadius(pulse, targetRadius);

            Vector3 center = pulse.WorldPosition;
            if (center == Vector3.zero)
            {
                center = this.comp.parent.DrawPos;
            }

            PulseStrengthTier tier = this.ResolvePulseStrengthTier(pulse);
            float strengthFactor = Mathf.Clamp(pulse.Strength, 0.85f, 2f);
            float tierAlpha = this.GetTierAlphaMultiplier(tier);
            float breakAlphaMultiplier = pulse.BrokeShield ? this.GetFallbackBreakAlphaMultiplier() : 1f;
            float visibilityMultiplier = this.GetVisibilityMultiplier(ticksGame);
            float globalAlpha = this.GetFallbackBaseAlpha() * visibilityMultiplier;
            float impactCoreAlpha = Mathf.Max(0f, this.Props.impactCoreAlphaMultiplier);
            float impactRingAlpha = Mathf.Max(0f, this.Props.impactRingAlphaMultiplier);
            bool lowShield = pulse.WasLowShieldAtHit && !pulse.BrokeShield;
            float lowShieldFlicker = lowShield ? this.GetLowShieldFlicker(pulse, ticksGame) : 1f;
            float lowInstability = lowShield ? 1f - Mathf.Clamp01(pulse.ShieldStrengthAtHit) : 0f;
            float currentStateDim = currentShieldBroken ? 0.9f : Mathf.Lerp(0.88f, 1f, Mathf.Clamp01(currentShieldStrength));

            if (age < 0.15f)
            {
                this.frontPatchRenderer.DrawCompressionFront(
                    pulse,
                    center,
                    targetRadius,
                    age,
                    strengthFactor,
                    tierAlpha,
                    globalAlpha * currentStateDim,
                    breakAlphaMultiplier,
                    lowShieldFlicker,
                    lowInstability);
            }

            if (pulse.BrokeShield && age < 0.16f)
            {
                this.breakOverloadRenderer.DrawBreakOverloadFlash(
                    this.Props,
                    pulse,
                    center,
                    targetRadius,
                    age,
                    strengthFactor,
                    globalAlpha);
            }

            float coreWindow = lowShield ? 0.16f : 0.22f;
            if (age < coreWindow)
            {
                float flashAge = Mathf.Clamp01(age / coreWindow);
                float coreScale = tier == PulseStrengthTier.Light ? 0.18f : 0.22f;
                if (tier == PulseStrengthTier.Heavy || tier == PulseStrengthTier.Break)
                {
                    coreScale += 0.04f;
                }
                if (lowShield)
                {
                    coreScale *= 0.82f;
                }

                float flashRadius = Mathf.Lerp(0.06f, targetRadius * coreScale, this.EaseOutSqrt(flashAge));
                float flashAlpha = 0.85f *
                    this.GetFallbackFlashAlpha() *
                    globalAlpha *
                    impactCoreAlpha *
                    strengthFactor *
                    tierAlpha *
                    this.FadeOutQuadratic(flashAge) *
                    breakAlphaMultiplier *
                    (lowShield ? lowShieldFlicker * 0.88f : 1f);
                this.DrawImpactCore(center, flashRadius, flashAlpha, pulse.BrokeShield);
            }

            float primaryRadius = Mathf.Lerp(0.1f, targetRadius, this.EaseOutSqrt(age));
            float primaryAlpha = 0.58f *
                this.GetFallbackRingAlpha() *
                globalAlpha *
                impactRingAlpha *
                strengthFactor *
                tierAlpha *
                this.FadeOutQuadratic(age) *
                breakAlphaMultiplier *
                (lowShield ? Mathf.Lerp(0.62f, 0.86f, lowShieldFlicker) : 1f);
            if (pulse.BrokeShield)
            {
                primaryRadius *= 1.12f;
                primaryAlpha *= 1.18f;
            }

            this.DrawPrimaryRing(center, primaryRadius, primaryAlpha, pulse.BrokeShield, lowShield);

            if (age >= 0.1f)
            {
                float secondaryAge = Mathf.Clamp01((age - 0.1f) / 0.9f);
                float secondaryRadius = Mathf.Lerp(0.08f, targetRadius * 0.78f, this.EaseOutSqrt(secondaryAge));
                float secondaryAlpha = 0.22f *
                    this.GetFallbackSecondRingAlpha() *
                    globalAlpha *
                    strengthFactor *
                    tierAlpha *
                    this.FadeOutQuadratic(secondaryAge) *
                    (pulse.BrokeShield ? breakAlphaMultiplier : 1f) *
                    (lowShield ? lowShieldFlicker * 0.75f : 1f);
                this.DrawSecondaryRing(center, secondaryRadius, secondaryAlpha);
            }

            if (age < 0.42f)
            {
                this.frontPatchRenderer.DrawAnimatedHexPatch(
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

            if (age < 0.28f)
            {
                this.breakOverloadRenderer.DrawDirectionalShockCrescent(
                    this.Props,
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

            float shardWindow = pulse.BrokeShield ? 0.28f : 0.35f;
            if (age < shardWindow)
            {
                this.shardBurstRenderer.DrawOutwardSparkBurst(pulse, center, targetRadius, age, strengthFactor, tier, tierAlpha, globalAlpha, breakAlphaMultiplier, lowShieldFlicker);
                this.shardBurstRenderer.DrawEnergyShards(pulse, center, targetRadius, age, strengthFactor, tier, tierAlpha, globalAlpha, breakAlphaMultiplier, lowShieldFlicker, lowInstability);
                this.residualEffectsRenderer.DrawArcCrawl(pulse, center, targetRadius, age, strengthFactor, tier, tierAlpha, globalAlpha, breakAlphaMultiplier, lowShieldFlicker, lowInstability);
            }

            if (age >= 0.08f && age < 0.6f)
            {
                this.residualEffectsRenderer.DrawResidualGlowPatch(pulse, center, targetRadius, age, strengthFactor, tier, globalAlpha, breakAlphaMultiplier, lowShieldFlicker, lowInstability);
            }

            if (tier == PulseStrengthTier.Heavy && age < 0.38f)
            {
                float heavyAge = Mathf.Clamp01(age / 0.38f);
                float heavyAlpha = 0.14f *
                    this.GetFallbackSecondRingAlpha() *
                    globalAlpha *
                    strengthFactor *
                    this.FadeOutQuadratic(heavyAge);
                this.drawPrimitives.DrawThinRing(
                    center,
                    Mathf.Lerp(targetRadius * 0.18f, targetRadius * 1.04f, this.EaseOutSqrt(heavyAge)),
                    new Color(0.42f, 0.74f, 0.92f, Mathf.Clamp01(heavyAlpha)));
            }

            if (pulse.BrokeShield && age < 0.55f)
            {
                this.breakOverloadRenderer.DrawBreakOverloadShockRing(
                    this.Props,
                    pulse,
                    center,
                    targetRadius,
                    age,
                    strengthFactor,
                    globalAlpha,
                    breakAlphaMultiplier);
            }
        }

        private void DrawImpactCore(Vector3 center, float radius, float alpha, bool brokeShield)
        {
            Color color = SurfaceShieldFallbackTuning.GetImpactCoreColor(alpha, brokeShield);
            this.drawPrimitives.DrawDisc(center, radius, color);
        }

        private void DrawPrimaryRing(Vector3 center, float radius, float alpha, bool brokeShield, bool lowShield)
        {
            Color color = SurfaceShieldFallbackTuning.GetPrimaryRingColor(alpha, brokeShield, lowShield);
            this.drawPrimitives.DrawThinRing(center, radius, color);
        }

        private void DrawSecondaryRing(Vector3 center, float radius, float alpha)
        {
            this.drawPrimitives.DrawThinRing(
                center,
                radius,
                SurfaceShieldFallbackTuning.GetSecondaryRingColor(alpha));
        }

        private float GetSurfaceBoundFallbackRadius(ShieldHitPulse pulse, float requestedRadius)
        {
            if (!pulse.HasHullUv)
            {
                return requestedRadius;
            }

            Vector2 hullSize = this.GetCurrentHullMeshWorldSize();
            float hullMinor = Mathf.Max(0.001f, Mathf.Min(hullSize.x, hullSize.y));
            float maxSurfaceRadius = hullMinor * (pulse.BrokeShield ? 0.24f : 0.16f);
            return Mathf.Clamp(requestedRadius, this.GetMinPulseRadius(), Mathf.Max(this.GetMinPulseRadius(), maxSurfaceRadius));
        }

        private PulseStrengthTier ResolvePulseStrengthTier(ShieldHitPulse pulse)
        {
            int tier = SurfaceShieldFallbackTuning.ResolvePulseStrengthTier(pulse.BrokeShield, pulse.Strength);
            if (tier == SurfaceShieldFallbackTuning.BreakTier)
            {
                return PulseStrengthTier.Break;
            }

            if (tier == SurfaceShieldFallbackTuning.HeavyTier)
            {
                return PulseStrengthTier.Heavy;
            }

            if (tier == SurfaceShieldFallbackTuning.MediumTier)
            {
                return PulseStrengthTier.Medium;
            }

            return PulseStrengthTier.Light;
        }

        private float GetTierAlphaMultiplier(PulseStrengthTier tier)
        {
            return SurfaceShieldFallbackTuning.GetTierAlphaMultiplier(this.GetFallbackTierCode(tier));
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

        private float EaseOutSqrt(float value)
        {
            return SurfaceShieldFallbackMath.EaseOutSqrt(value);
        }

        private float FadeOutQuadratic(float value)
        {
            return SurfaceShieldFallbackMath.FadeOutQuadratic(value);
        }

        private float PseudoRandom01(int seed)
        {
            return SurfaceShieldFallbackMath.PseudoRandom01(seed);
        }

        private float GetLowShieldFlicker(ShieldHitPulse pulse, int ticksGame)
        {
            return SurfaceShieldFallbackMath.GetLowShieldFlicker(pulse.Seed, ticksGame);
        }

        private float GetFallbackBaseAlpha()
        {
            return SurfaceShieldFallbackTuning.GetFallbackBaseAlpha(this.Props);
        }

        private float GetFallbackFlashAlpha()
        {
            return SurfaceShieldFallbackTuning.GetFallbackFlashAlpha(this.Props);
        }

        private float GetFallbackRingAlpha()
        {
            return SurfaceShieldFallbackTuning.GetFallbackRingAlpha(this.Props);
        }

        private float GetFallbackSecondRingAlpha()
        {
            return SurfaceShieldFallbackTuning.GetFallbackSecondRingAlpha(this.Props);
        }

        private float GetFallbackBreakAlphaMultiplier()
        {
            return SurfaceShieldFallbackTuning.GetFallbackBreakAlphaMultiplier(this.Props);
        }

        private float GetFallbackBreakRadiusMultiplier()
        {
            return SurfaceShieldFallbackTuning.GetFallbackBreakRadiusMultiplier(this.Props);
        }

        private float GetVisibilityMultiplier(int ticksGame)
        {
            return 1f;
        }

        private CompProperties_ModularShuttleSurfaceShieldVisual Props
        {
            get
            {
                return (CompProperties_ModularShuttleSurfaceShieldVisual)this.comp.props;
            }
        }

        private Texture2D TryGetCurrentHullTexture()
        {
            return SurfaceShieldMaterialCache.TryGetCurrentHullTexture(this.comp);
        }

        private Mesh TryGetCurrentHullMesh()
        {
            return SurfaceShieldMeshCache.TryGetCurrentHullMesh(this.comp);
        }

        private Vector3 GetCurrentHullDrawLoc()
        {
            return SurfaceShieldHullGeometry.GetCurrentHullDrawLoc(this.comp);
        }

        private Quaternion GetCurrentHullRotation()
        {
            return SurfaceShieldHullGeometry.GetCurrentHullRotation(this.comp);
        }

        private Vector3 GetCurrentHullDrawScale()
        {
            return SurfaceShieldHullGeometry.GetCurrentHullDrawScale();
        }

        private Vector2 GetCurrentHullMeshWorldSize()
        {
            return SurfaceShieldHullGeometry.GetCurrentHullMeshWorldSize(this.comp);
        }

        private float GetMinPulseRadius()
        {
            return Mathf.Max(0f, this.Props.minPulseRadius);
        }

        private float GetMaxPulseRadius()
        {
            return Mathf.Max(this.GetMinPulseRadius(), this.Props.maxPulseRadius);
        }

        private Color GetCurrentShieldOverlayColor()
        {
            return new Color(0.42f, 0.68f, 0.74f, 1f);
        }
    }
}
