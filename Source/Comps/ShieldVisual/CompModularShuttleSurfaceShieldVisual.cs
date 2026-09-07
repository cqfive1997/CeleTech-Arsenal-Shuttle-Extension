using System.Collections.Generic;
using System.IO;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Shield.Surface;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    public sealed class CompProperties_ModularShuttleSurfaceShieldVisual : CompProperties
    {
        // Pulse buffer and shared fallback timing.
        public int maxPulses = 8;
        public int pulseLifetimeTicks = 45;
        public float minPulseRadius = 0.35f;
        public float maxPulseRadius = 2.5f;

        // Debug / logging.
        public bool devLogPulses = false;
        public bool enableShaderRendering = true;
        public bool drawWhenShieldOffline = false;
        public bool drawIdleSurfaceEffect = true;
        public bool drawFallbackPulsesOverShader = false;
        public bool drawImpactSparksOverShader = true;
        public float shaderOverlaySparkAlphaMultiplier = 0.45f;
        public int maxShaderOverlaySparkPulses = 4;

        // Legacy XML knobs; no longer used after dev visual preview cleanup.
        public bool devHighVisibilityMode = false;
        public float devHighVisibilityMultiplier = 2.25f;

        // Fallback hit response.
        public float fallbackBaseAlpha = 0.85f;
        public float fallbackFlashAlpha = 0.62f;
        public float fallbackRingAlpha = 0.95f;
        public float fallbackSecondRingAlpha = 0.55f;
        public float fallbackSplashAlpha = 0.48f;
        public int fallbackSplashCount = 6;
        public float fallbackSplashRadiusFactor = 0.38f;

        // Break overload.
        public float fallbackBreakAlphaMultiplier = 1.72f;
        public float fallbackBreakRadiusMultiplier = 1.12f;

        // Panel / hex activation. Primary tuning: idlePanelAlpha,
        // idlePanelDensity, impactPanelAlphaMultiplier.
        public float fallbackHexAlpha = 0.45f;
        public int fallbackHexCount = 7;
        public float idlePanelAlpha = 0.025f;
        public float idlePanelDensity = 0.09f;
        public float impactPanelAlphaMultiplier = 0.85f;

        // Arc crawl. Primary tuning: impactArcAlphaMultiplier.
        public float fallbackShardAlpha = 0.78f;
        public float fallbackResidualAlpha = 0.28f;
        public float fallbackDirectionalAlpha = 0.72f;
        public float fallbackCrawlAlpha = 0.65f;
        public float impactArcAlphaMultiplier = 0.55f;

        // Impact spark / shard burst. Visual-only shards emitted from the
        // corrected hull-attached impact point along the outward surface normal.
        public float impactSparkAlpha = 0.55f;
        public int impactSparkCount = 8;
        public float impactSparkSpeed = 1f;
        public float impactSparkLength = 0.22f;
        public float impactSparkSpreadAngle = 34f;
        public float impactSparkLifetimeFactor = 0.28f;

        // Legacy shader/fallback knobs kept for XML compatibility.
        public float meshPadding = 0.5f;
        public float hullOverlayBaseAlpha = 0.075f;
        public float hullOverlayEdgeGlowAlpha = 0.22f;
        public float hullOverlaySweepAlpha = 0.055f;
        public float hullOverlayImpactAlpha = 0.92f;
        public float hullOverlayLowShieldFlicker = 0.8f;

        // Idle surface. Primary tuning: idleSurfaceBaseAlpha.
        public float idleSurfaceBaseAlpha = 0.04f;
        public float idleSurfaceLowShieldFlickerAlpha = 0.13f;

        // Edge glow. Primary tuning: idleSurfaceEdgeGlowAlpha, edgeGlowWidth.
        public float idleSurfaceEdgeGlowAlpha = 0.52f;
        public float edgeGlowWidth = 1.1f;

        // Outline / GPU edge glow. Primary tuning:
        // hullOutlineGlowAlpha, hullOutlineEdgeDetectScale.
        public bool enableGpuHullEdgeGlow = true;
        public float hullOutlineAlphaThreshold = 0.1f;
        public float hullOutlineGlowAlpha = 0.58f;
        public float hullOutlineEdgeDetectScale = 13f;
        public float hullOutlineLowShieldFlicker = 0.18f;
        public float hullOutlineSampleOffsetScale = 3.6f;
        public float hullOutlineHitBoost = 0.35f;
        public float hullOutlineEdgeSoftness = 0.16f;

        // Legacy sweep knobs kept for XML compatibility. Sweep is now only a
        // very weak auxiliary flow signal; the primary read is membrane/hex/noise.
        public float idleSurfaceSweepAlpha = 0.015f;
        public float sweepBandSpeed = 0.0008f;
        public float sweepBandSpacing = 0.26f;
        public float sweepBandWidth = 0.04f;
        public float sweepBandSharpness = 2.4f;
        public float sweepBandAngle = 0.55f;
        public float sweepBandHexBoost = 0.18f;
        public float sweepBandEdgeBoost = 0.05f;
        public float sweepMinVisibility = 0f;
        public float hexScale = 20f;
        public float hexLineAlpha = 0.31f;
        public float hexFillAlpha = 0.006f;
        public float hexBreathAlpha = 0.026f;
        public float hexPulseStrength = 1.18f;
        public float hexLineWidth = 0.016f;
        public float hexSweepBoost = 0.18f;
        public float hexHitBoost = 1.85f;
        public float noiseScale = 18f;
        public float noiseAlpha = 0.035f;
        public float noiseSpeed = 0.003f;
        public float noiseDistortion = 0.014f;
        public float lowShieldNoiseBoost = 1.65f;

        // Cold gray-cyan membrane with electric cyan structure/accent.
        public Color edgeColor = new Color(0.36f, 0.9f, 1f, 1f);
        public Color sweepColor = new Color(0.54f, 1f, 0.98f, 1f);
        public Color impactColor = new Color(0.9f, 1f, 0.98f, 1f);
        public Color hexColor = new Color(0.02f, 0.82f, 0.95f, 1f);
        public Color hexFillColor = new Color(0.1f, 0.13f, 0.14f, 1f);
        public Color hexEdgeAccentColor = new Color(0.72f, 0.57f, 0.34f, 1f);
        public float hexEdgeAccentStrength = 0.045f;

        // Impact response. Primary tuning: impactCoreAlphaMultiplier,
        // impactRingAlphaMultiplier, maxHitUvRadius.
        public float impactCoreAlphaMultiplier = 1.55f;
        public float impactAfterglowAlpha = 0.75f;
        public float impactRingAlphaMultiplier = 1.05f;
        public float hullOverlayRippleWidth = 0.032f;
        public float maxHitUvRadius = 0.14f;

        // Hit position clamp. Primary tuning: hullSurfaceShieldOffset,
        // maxImpactSurfaceVisualOffset, horizontalImpactSurfaceOffsetScale,
        // verticalImpactSurfaceOffsetScale.
        public bool snapHitToHullTextureAlpha = true;
        public int hullAlphaMaskResolution = 64;
        public float hullAlphaThreshold = 0.1f;
        public int hullAlphaLocalSearchRadius = 6;
        public int hullAlphaMaxSearchRadius = 18;
        public float hullSurfaceShieldOffset = 0.04f;
        public float maxImpactSurfaceVisualOffset = 0.055f;
        public float horizontalImpactSurfaceOffsetScale = 1f;
        public float verticalImpactSurfaceOffsetScale = 0.6f;
        public float verticalImpactInwardBiasWorld = 2f;
        public float verticalExtraInwardBiasWorld = 0.5f;
        public float verticalImpactInwardBiasThreshold = 0.65f;
        public float shieldOverlayHeightOffset = 0.16f;

        public CompProperties_ModularShuttleSurfaceShieldVisual()
        {
            this.compClass = typeof(CompModularShuttleSurfaceShieldVisual);
        }
    }

    /// <summary>
    /// Transient Surface Shield presentation seam.
    /// It records short-lived hit pulses only; gameplay absorption is owned by the
    /// Surface Shield runtime service and remains correct if this comp is missing.
    /// </summary>
    [StaticConstructorOnStartup]
    public sealed partial class CompModularShuttleSurfaceShieldVisual : ThingComp
    {
        private static readonly ShuttleSurfaceShieldVisualAssetLoader SharedAssetLoader =
            new ShuttleSurfaceShieldVisualAssetLoader();
        private static readonly int ShieldStrengthProperty = Shader.PropertyToID("_ShieldStrength");
        private static readonly int GameTickProperty = Shader.PropertyToID("_GameTick");
        private static readonly int ShieldColorProperty = Shader.PropertyToID("_ShieldColor");
        private static readonly int EdgeColorProperty = Shader.PropertyToID("_EdgeColor");
        private static readonly int SweepColorProperty = Shader.PropertyToID("_SweepColor");
        private static readonly int ImpactColorProperty = Shader.PropertyToID("_ImpactColor");
        private static readonly int HexColorProperty = Shader.PropertyToID("_HexColor");
        private static readonly int HexFillColorProperty = Shader.PropertyToID("_HexFillColor");
        private static readonly int HexEdgeAccentColorProperty = Shader.PropertyToID("_HexEdgeAccentColor");
        private static readonly int HexEdgeAccentStrengthProperty = Shader.PropertyToID("_HexEdgeAccentStrength");
        private static readonly int HitCountProperty = Shader.PropertyToID("_HitCount");
        private static readonly int Hit0Property = Shader.PropertyToID("_Hit0");
        private static readonly int Hit1Property = Shader.PropertyToID("_Hit1");
        private static readonly int Hit2Property = Shader.PropertyToID("_Hit2");
        private static readonly int Hit3Property = Shader.PropertyToID("_Hit3");
        private static readonly int HitRadius0Property = Shader.PropertyToID("_HitRadius0");
        private static readonly int HitRadius1Property = Shader.PropertyToID("_HitRadius1");
        private static readonly int HitRadius2Property = Shader.PropertyToID("_HitRadius2");
        private static readonly int HitRadius3Property = Shader.PropertyToID("_HitRadius3");
        private static readonly int PulseLifetimeTicksProperty = Shader.PropertyToID("_PulseLifetimeTicks");
        private static readonly int SelectedRadiusProperty = Shader.PropertyToID("_SelectedRadius");
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private static readonly int MainTexProperty = Shader.PropertyToID("_MainTex");
        private static readonly int HullTexProperty = Shader.PropertyToID("_HullTex");
        private static readonly int SurfaceMaskTexProperty = Shader.PropertyToID("_SurfaceMaskTex");
        private static readonly int NoiseTexProperty = Shader.PropertyToID("_NoiseTex");
        private static readonly int AlphaThresholdProperty = Shader.PropertyToID("_AlphaThreshold");
        private static readonly int BaseAlphaProperty = Shader.PropertyToID("_BaseAlpha");
        private static readonly int EdgeGlowAlphaProperty = Shader.PropertyToID("_EdgeGlowAlpha");
        private static readonly int EdgeWidthProperty = Shader.PropertyToID("_EdgeWidth");
        private static readonly int EnableGpuHullEdgeGlowProperty = Shader.PropertyToID("_EnableGpuHullEdgeGlow");
        private static readonly int EdgeDetectScaleProperty = Shader.PropertyToID("_EdgeDetectScale");
        private static readonly int EdgeSampleOffsetProperty = Shader.PropertyToID("_EdgeSampleOffset");
        private static readonly int EdgePulseStrengthProperty = Shader.PropertyToID("_EdgePulseStrength");
        private static readonly int EdgeSoftnessProperty = Shader.PropertyToID("_EdgeSoftness");
        private static readonly int SweepAlphaProperty = Shader.PropertyToID("_SweepAlpha");
        private static readonly int SweepSpeedProperty = Shader.PropertyToID("_SweepSpeed");
        private static readonly int SweepSpacingProperty = Shader.PropertyToID("_SweepSpacing");
        private static readonly int SweepWidthProperty = Shader.PropertyToID("_SweepWidth");
        private static readonly int SweepSharpnessProperty = Shader.PropertyToID("_SweepSharpness");
        private static readonly int SweepAngleProperty = Shader.PropertyToID("_SweepAngle");
        private static readonly int SweepHexBoostProperty = Shader.PropertyToID("_SweepHexBoost");
        private static readonly int SweepEdgeBoostProperty = Shader.PropertyToID("_SweepEdgeBoost");
        private static readonly int SweepMinVisibilityProperty = Shader.PropertyToID("_SweepMinVisibility");
        private static readonly int HexScaleProperty = Shader.PropertyToID("_HexScale");
        private static readonly int HexLineAlphaProperty = Shader.PropertyToID("_HexLineAlpha");
        private static readonly int HexFillAlphaProperty = Shader.PropertyToID("_HexFillAlpha");
        private static readonly int HexBreathAlphaProperty = Shader.PropertyToID("_HexBreathAlpha");
        private static readonly int HexPulseStrengthProperty = Shader.PropertyToID("_HexPulseStrength");
        private static readonly int HexLineWidthProperty = Shader.PropertyToID("_HexLineWidth");
        private static readonly int HexSweepBoostProperty = Shader.PropertyToID("_HexSweepBoost");
        private static readonly int HexHitBoostProperty = Shader.PropertyToID("_HexHitBoost");
        private static readonly int NoiseScaleProperty = Shader.PropertyToID("_NoiseScale");
        private static readonly int NoiseAlphaProperty = Shader.PropertyToID("_NoiseAlpha");
        private static readonly int NoiseSpeedProperty = Shader.PropertyToID("_NoiseSpeed");
        private static readonly int NoiseDistortionProperty = Shader.PropertyToID("_NoiseDistortion");
        private static readonly int LowShieldNoiseBoostProperty = Shader.PropertyToID("_LowShieldNoiseBoost");
        private static readonly int LowShieldFlickerProperty = Shader.PropertyToID("_LowShieldFlicker");
        private static readonly int ImpactCoreAlphaMultiplierProperty = Shader.PropertyToID("_ImpactCoreAlphaMultiplier");
        private static readonly int ImpactAfterglowAlphaProperty = Shader.PropertyToID("_ImpactAfterglowAlpha");
        private static readonly int IdlePanelAlphaProperty = Shader.PropertyToID("_IdlePanelAlpha");
        private static readonly int IdlePanelDensityProperty = Shader.PropertyToID("_IdlePanelDensity");
        private static readonly int ImpactPanelAlphaMultiplierProperty = Shader.PropertyToID("_ImpactPanelAlphaMultiplier");
        private static readonly int ImpactArcAlphaMultiplierProperty = Shader.PropertyToID("_ImpactArcAlphaMultiplier");
        private static readonly int RippleAlphaProperty = Shader.PropertyToID("_RippleAlpha");
        private static readonly int RippleWidthProperty = Shader.PropertyToID("_RippleWidth");
        private static Texture2D sharedNoiseTexture;
        private Texture2D cachedSurfaceMaskTexture;
        private string cachedSurfaceMaskTexturePath;
        private bool cachedSurfaceMaskTextureResolved;
        private ShieldHitPulse[] pulses;
        private int nextPulseIndex;
        private int activePulseCount;
        private MaterialPropertyBlock propertyBlock;
        private string lastLoggedUnconfiguredModuleID;
        private string lastLoggedMissingMaterialKey;
        private string lastLoggedDrawFailedMaterialKey;
        private string lastLoggedActiveMaterialKey;
        private bool loggedMissingHullTexture;
        private bool loggedHullOverlayUnavailable;
        private bool loggedHullOverlayGeometryMode;
        private HullAlphaMaskCache hullAlphaMaskCache;
        private Texture2D lastLoggedUnreadableHullAlphaTexture;

        private CompProperties_ModularShuttleSurfaceShieldVisual Props
        {
            get
            {
                return (CompProperties_ModularShuttleSurfaceShieldVisual)this.props;
            }
        }

        internal HullAlphaMaskCache CachedHullAlphaMask
        {
            get
            {
                return this.hullAlphaMaskCache;
            }

            set
            {
                this.hullAlphaMaskCache = value;
            }
        }

        internal void LogHullAlphaMaskFailureOnce(Texture2D texture, string reason)
        {
            SurfaceShieldVisualDiagnostics.LogHullAlphaMaskFailureOnce(
                this.Props.devLogPulses,
                ref this.lastLoggedUnreadableHullAlphaTexture,
                texture,
                reason);
        }

        internal int ActivePulseCount
        {
            get
            {
                this.CleanupExpiredPulses(this.CurrentTick);
                return this.activePulseCount;
            }
        }

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            this.EnsurePulseBuffer();
        }

        public override void CompTick()
        {
            base.CompTick();
            this.CleanupExpiredPulses(this.CurrentTick);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }

            foreach (Gizmo gizmo in SurfaceShieldVisualDevGizmoProvider.GetGizmos(this))
            {
                yield return gizmo;
            }
        }

        public override void PostDraw()
        {
            base.PostDraw();

            int ticksGame = this.CurrentTick;
            this.CleanupExpiredPulses(ticksGame);
            if (!this.Props.enableShaderRendering || this.parent == null || !this.parent.Spawned)
            {
                return;
            }

            ShuttleSurfaceShieldVisualConfigSnapshot config;
            bool hasVisualConfig = SurfaceShieldVisualConfigResolver.TryGetVisualConfig(this, out config) &&
                config != null;
            bool shieldOnline = hasVisualConfig && config.Online;
            bool currentShieldBroken = SurfaceShieldVisualConfigResolver.IsBrokenSurfaceShieldStatus(config);
            bool idleSurfaceRequested =
                this.Props.drawIdleSurfaceEffect &&
                shieldOnline &&
                !currentShieldBroken;
            float currentShieldStrength =
                SurfaceShieldVisualConfigResolver.GetEffectiveShieldStrengthForVisual(config);

            if (this.activePulseCount <= 0 &&
                !this.Props.drawWhenShieldOffline &&
                !idleSurfaceRequested)
            {
                return;
            }

            if (hasVisualConfig && config.HasConfig)
            {
                string failureReason;
                Material material = SharedAssetLoader.TryGetMaterial(
                    config.SurfaceShaderBundlePath,
                    config.SurfaceMaterialAssetName,
                    out failureReason);

                if (material != null)
                {
                    SurfaceShieldVisualDiagnostics.LogHullOverlayMaterialActiveOnce(
                        ref this.lastLoggedActiveMaterialKey,
                        config);
                    if (this.DrawHullMaskedShieldOverlay(material, config, ticksGame, currentShieldStrength))
                    {
                        if (this.activePulseCount > 0)
                        {
                            if (this.Props.drawFallbackPulsesOverShader)
                            {
                                SurfaceShieldFallbackRenderer.DrawFallbackPulses(
                                    this,
                                    this.pulses,
                                    this.activePulseCount,
                                    ticksGame,
                                    currentShieldStrength,
                                    currentShieldBroken,
                                    ref this.propertyBlock);
                            }
                            else if (this.Props.drawImpactSparksOverShader)
                            {
                                SurfaceShieldFallbackRenderer.DrawImpactSparksOverShader(
                                    this,
                                    this.pulses,
                                    this.activePulseCount,
                                    ticksGame,
                                    currentShieldStrength,
                                    currentShieldBroken,
                                    ref this.propertyBlock);
                            }
                        }

                        return;
                    }

                    SurfaceShieldVisualDiagnostics.LogHullOverlayDrawFailedOnce(
                        ref this.lastLoggedDrawFailedMaterialKey,
                        config);
                    SurfaceShieldVisualDiagnostics.LogHullOverlayUnavailableOnce(
                        ref this.loggedHullOverlayUnavailable);
                }
                else
                {
                    SurfaceShieldVisualDiagnostics.LogMissingMaterialOnce(
                        ref this.lastLoggedMissingMaterialKey,
                        config,
                        failureReason);
                    SurfaceShieldVisualDiagnostics.LogHullOverlayUnavailableOnce(
                        ref this.loggedHullOverlayUnavailable);
                }
            }
            else if (hasVisualConfig)
            {
                SurfaceShieldVisualDiagnostics.LogVisualResourcesNotConfiguredOnce(
                    ref this.lastLoggedUnconfiguredModuleID,
                    config);
                SurfaceShieldVisualDiagnostics.LogHullOverlayUnavailableOnce(
                    ref this.loggedHullOverlayUnavailable);
            }

            if (idleSurfaceRequested)
            {
                SurfaceShieldFallbackRenderer.DrawFallbackHullAttachedIdleSurface(
                    this,
                    ticksGame,
                    currentShieldStrength,
                    ref this.propertyBlock);
            }

            if (this.activePulseCount > 0)
            {
                SurfaceShieldFallbackRenderer.DrawFallbackPulses(
                    this,
                    this.pulses,
                    this.activePulseCount,
                    ticksGame,
                    currentShieldStrength,
                    currentShieldBroken,
                    ref this.propertyBlock);
            }
        }

        internal void NotifyShieldHit(ShuttleSurfaceShieldAbsorbResult result)
        {
            if (result == null || !result.Handled)
            {
                return;
            }

            float strength = this.CalculatePulseStrength(result);
            float radius = Mathf.Lerp(
                this.GetMinPulseRadius(),
                this.GetMaxPulseRadius(),
                Mathf.Clamp01(strength / 2f));
            if (result.BrokeShield)
            {
                radius = Mathf.Max(radius, this.GetMaxPulseRadius() * 0.85f);
            }

            this.NotifyShieldHit(
                result.HitWorldPosition,
                strength,
                radius,
                result.BrokeShield,
                result.DamageCategory);
        }

        internal void NotifyShieldHit(
            Vector3 worldPosition,
            float strength,
            float radius,
            bool brokeShield,
            string damageCategory)
        {
            float shieldStrength = brokeShield
                ? 0f
                : SurfaceShieldVisualConfigResolver.GetCurrentShieldStrengthForVisual(this);
            this.NotifyShieldHit(
                worldPosition,
                strength,
                radius,
                brokeShield,
                damageCategory,
                shieldStrength,
                shieldStrength < 0.35f);
        }

        private int CurrentTick
        {
            get
            {
                return Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            }
        }

        private float GetVisibilityMultiplier(int ticksGame)
        {
            return 1f;
        }
    }
}
