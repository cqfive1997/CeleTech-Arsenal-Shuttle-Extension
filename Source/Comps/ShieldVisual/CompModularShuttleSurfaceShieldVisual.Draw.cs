using System;
using System.Collections.Generic;
using System.IO;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Rendering.CustomShaders;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Shield.Surface;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    public sealed partial class CompModularShuttleSurfaceShieldVisual
    {
        private static readonly string[] SurfaceMaskRotationSuffixes =
        {
            "_north",
            "_east",
            "_south",
            "_west"
        };

        private bool DrawHullMaskedShieldOverlay(
            Material material,
            ShuttleSurfaceShieldVisualConfigSnapshot config,
            int ticksGame,
            float strengthForVisual)
        {
            if (material == null || config == null || this.parent == null)
            {
                return false;
            }

            CustomShaderMaterialValidationResult validation =
                UnityCustomShaderMaterialValidator.Validate(
                    material,
                    ShuttleCustomShaderMaterialContracts.SurfaceShield);
            if (!ShuttleCustomShaderDrawGate.AllowsCustomRendering(validation))
            {
                ShuttleCustomShaderDiagnostics.LogMaterialValidation(
                    "surface-shield-draw-gate",
                    config.SurfaceShaderBundlePath,
                    config.SurfaceMaterialAssetName,
                    material,
                    validation,
                    "fallback");
                return false;
            }

            Texture2D hullTexture = SurfaceShieldMaterialCache.TryGetCurrentHullTexture(this);
            if (hullTexture == null)
            {
                SurfaceShieldVisualDiagnostics.LogMissingHullTextureOnce(
                    ref this.loggedMissingHullTexture);
                return false;
            }
            Texture2D surfaceMaskTexture = this.ResolveSurfaceMaskTexture(config, hullTexture);

            Mesh mesh = SurfaceShieldMeshCache.TryGetCurrentHullMesh(this);
            if (mesh == null)
            {
                return false;
            }
            SurfaceShieldVisualDiagnostics.LogHullOverlayGeometryModeOnce(
                ref this.loggedHullOverlayGeometryMode,
                mesh,
                SurfaceShieldHullGeometry.GetCurrentHullDrawSize(this),
                SurfaceShieldHullGeometry.GetCurrentHullDrawScale());

            if (this.propertyBlock == null)
            {
                this.propertyBlock = new MaterialPropertyBlock();
            }

            if (material.renderQueue < 3605)
            {
                material.renderQueue = 3605;
            }

            Vector2 meshWorldSize = SurfaceShieldHullGeometry.GetCurrentHullMeshWorldSize(this);
            float drawWidth = Mathf.Max(0.001f, meshWorldSize.x);
            float drawDepth = Mathf.Max(0.001f, meshWorldSize.y);
            float visibilityMultiplier = this.GetVisibilityMultiplier(ticksGame);
            this.propertyBlock.Clear();
            this.propertyBlock.SetTexture(HullTexProperty, hullTexture);
            this.propertyBlock.SetTexture(SurfaceMaskTexProperty, surfaceMaskTexture ?? hullTexture);
            this.propertyBlock.SetTexture(NoiseTexProperty, GetSharedNoiseTexture() ?? BaseContent.WhiteTex);
            this.propertyBlock.SetFloat(ShieldStrengthProperty, strengthForVisual);
            this.propertyBlock.SetFloat(GameTickProperty, ticksGame);
            this.propertyBlock.SetColor(ShieldColorProperty, this.GetCurrentShieldOverlayColor());
            this.propertyBlock.SetColor(EdgeColorProperty, this.Props.edgeColor);
            this.propertyBlock.SetColor(SweepColorProperty, this.Props.sweepColor);
            this.propertyBlock.SetColor(ImpactColorProperty, this.Props.impactColor);
            this.propertyBlock.SetColor(HexColorProperty, this.Props.hexColor);
            this.propertyBlock.SetColor(HexFillColorProperty, this.Props.hexFillColor);
            this.propertyBlock.SetColor(HexEdgeAccentColorProperty, this.Props.hexEdgeAccentColor);
            this.propertyBlock.SetFloat(HexEdgeAccentStrengthProperty, Mathf.Clamp01(this.Props.hexEdgeAccentStrength));
            this.propertyBlock.SetFloat(PulseLifetimeTicksProperty, Mathf.Max(1, this.Props.pulseLifetimeTicks));
            this.propertyBlock.SetFloat(SelectedRadiusProperty, Mathf.Max(0f, config.SelectedRadius));
            this.propertyBlock.SetFloat(
                AlphaThresholdProperty,
                Mathf.Clamp01(SurfaceShieldHullAlphaMaskCache.GetHullOutlineAlphaThreshold(this)));
            this.propertyBlock.SetFloat(BaseAlphaProperty, Mathf.Clamp01(this.Props.idleSurfaceBaseAlpha * visibilityMultiplier));
            this.propertyBlock.SetFloat(
                EdgeGlowAlphaProperty,
                Mathf.Clamp01(SurfaceShieldHullAlphaMaskCache.GetHullOutlineGlowAlpha(this) * visibilityMultiplier));
            this.propertyBlock.SetFloat(EdgeWidthProperty, Mathf.Clamp(this.Props.edgeGlowWidth, 0.75f, 5f));
            this.propertyBlock.SetFloat(EnableGpuHullEdgeGlowProperty, this.Props.enableGpuHullEdgeGlow ? 1f : 0f);
            this.propertyBlock.SetFloat(EdgeDetectScaleProperty, Mathf.Clamp(this.Props.hullOutlineEdgeDetectScale, 0f, 16f));
            this.propertyBlock.SetFloat(
                EdgeSampleOffsetProperty,
                SurfaceShieldHullAlphaMaskCache.GetEffectiveHullOutlineSampleOffset(this));
            this.propertyBlock.SetFloat(EdgePulseStrengthProperty, Mathf.Clamp01(this.Props.hullOutlineHitBoost));
            this.propertyBlock.SetFloat(EdgeSoftnessProperty, Mathf.Clamp(this.Props.hullOutlineEdgeSoftness, 0.02f, 0.8f));
            this.propertyBlock.SetFloat(SweepAlphaProperty, Mathf.Clamp01(this.Props.idleSurfaceSweepAlpha * visibilityMultiplier));
            this.propertyBlock.SetFloat(SweepSpeedProperty, Mathf.Max(0f, this.Props.sweepBandSpeed));
            this.propertyBlock.SetFloat(SweepSpacingProperty, Mathf.Clamp(this.Props.sweepBandSpacing, 0.08f, 0.8f));
            this.propertyBlock.SetFloat(SweepWidthProperty, Mathf.Clamp(this.Props.sweepBandWidth, 0.005f, 0.12f));
            this.propertyBlock.SetFloat(SweepSharpnessProperty, Mathf.Clamp(this.Props.sweepBandSharpness, 0.5f, 8f));
            this.propertyBlock.SetFloat(SweepAngleProperty, this.Props.sweepBandAngle);
            this.propertyBlock.SetFloat(SweepHexBoostProperty, Mathf.Clamp(this.Props.sweepBandHexBoost, 0f, 2f));
            this.propertyBlock.SetFloat(SweepEdgeBoostProperty, Mathf.Clamp01(this.Props.sweepBandEdgeBoost));
            this.propertyBlock.SetFloat(SweepMinVisibilityProperty, Mathf.Clamp01(this.Props.sweepMinVisibility));
            this.propertyBlock.SetFloat(HexScaleProperty, Mathf.Clamp(this.Props.hexScale, 8f, 96f));
            this.propertyBlock.SetFloat(HexLineAlphaProperty, Mathf.Clamp01(this.Props.hexLineAlpha));
            this.propertyBlock.SetFloat(HexFillAlphaProperty, Mathf.Clamp01(this.Props.hexFillAlpha));
            this.propertyBlock.SetFloat(HexBreathAlphaProperty, Mathf.Clamp01(this.Props.hexBreathAlpha));
            this.propertyBlock.SetFloat(HexPulseStrengthProperty, Mathf.Clamp(this.Props.hexPulseStrength, 0f, 3f));
            this.propertyBlock.SetFloat(HexLineWidthProperty, Mathf.Clamp(this.Props.hexLineWidth, 0.002f, 0.08f));
            this.propertyBlock.SetFloat(HexSweepBoostProperty, Mathf.Clamp(this.Props.hexSweepBoost, 0f, 3f));
            this.propertyBlock.SetFloat(HexHitBoostProperty, Mathf.Clamp(this.Props.hexHitBoost, 0f, 3f));
            this.propertyBlock.SetFloat(NoiseScaleProperty, Mathf.Clamp(this.Props.noiseScale, 1f, 96f));
            this.propertyBlock.SetFloat(NoiseAlphaProperty, Mathf.Clamp01(this.Props.noiseAlpha));
            this.propertyBlock.SetFloat(NoiseSpeedProperty, Mathf.Max(0f, this.Props.noiseSpeed));
            this.propertyBlock.SetFloat(NoiseDistortionProperty, Mathf.Clamp(this.Props.noiseDistortion, 0f, 0.25f));
            this.propertyBlock.SetFloat(LowShieldNoiseBoostProperty, Mathf.Clamp(this.Props.lowShieldNoiseBoost, 0f, 4f));
            this.propertyBlock.SetFloat(
                LowShieldFlickerProperty,
                Mathf.Clamp01(SurfaceShieldHullAlphaMaskCache.GetHullOutlineLowShieldFlicker(this) * visibilityMultiplier));
            this.propertyBlock.SetFloat(ImpactCoreAlphaMultiplierProperty, Mathf.Max(0f, this.Props.impactCoreAlphaMultiplier * visibilityMultiplier));
            this.propertyBlock.SetFloat(ImpactAfterglowAlphaProperty, Mathf.Clamp(this.Props.impactAfterglowAlpha * visibilityMultiplier, 0f, 2f));
            this.propertyBlock.SetFloat(IdlePanelAlphaProperty, Mathf.Clamp01(this.Props.idlePanelAlpha * visibilityMultiplier));
            this.propertyBlock.SetFloat(IdlePanelDensityProperty, Mathf.Clamp01(this.Props.idlePanelDensity));
            this.propertyBlock.SetFloat(ImpactPanelAlphaMultiplierProperty, Mathf.Max(0f, this.Props.impactPanelAlphaMultiplier * visibilityMultiplier));
            this.propertyBlock.SetFloat(ImpactArcAlphaMultiplierProperty, Mathf.Max(0f, this.Props.impactArcAlphaMultiplier * visibilityMultiplier));
            this.propertyBlock.SetFloat(RippleAlphaProperty, Mathf.Max(0f, this.Props.impactRingAlphaMultiplier * visibilityMultiplier));
            this.propertyBlock.SetFloat(RippleWidthProperty, Mathf.Clamp(this.Props.hullOverlayRippleWidth, 0.005f, 0.2f));
            this.SetHitProperties(drawWidth, drawDepth);

            Matrix4x4 matrix = Matrix4x4.TRS(
                SurfaceShieldHullGeometry.GetCurrentHullDrawLoc(this),
                SurfaceShieldHullGeometry.GetCurrentHullRotation(this),
                SurfaceShieldHullGeometry.GetCurrentHullDrawScale());
            Graphics.DrawMesh(
                mesh,
                matrix,
                material,
                0,
                null,
                0,
                this.propertyBlock);
            return true;
        }

        private void SetHitProperties(float width, float depth)
        {
            Vector4 hit0;
            Vector4 hit1;
            Vector4 hit2;
            Vector4 hit3;
            float radius0;
            float radius1;
            float radius2;
            float radius3;
            int count = this.BuildLatestHitShaderValues(
                width,
                depth,
                out hit0,
                out hit1,
                out hit2,
                out hit3,
                out radius0,
                out radius1,
                out radius2,
                out radius3);

            this.propertyBlock.SetFloat(HitCountProperty, count);
            this.propertyBlock.SetVector(Hit0Property, hit0);
            this.propertyBlock.SetVector(Hit1Property, hit1);
            this.propertyBlock.SetVector(Hit2Property, hit2);
            this.propertyBlock.SetVector(Hit3Property, hit3);
            this.propertyBlock.SetFloat(HitRadius0Property, radius0);
            this.propertyBlock.SetFloat(HitRadius1Property, radius1);
            this.propertyBlock.SetFloat(HitRadius2Property, radius2);
            this.propertyBlock.SetFloat(HitRadius3Property, radius3);
        }

        private int BuildLatestHitShaderValues(
            float width,
            float depth,
            out Vector4 hit0,
            out Vector4 hit1,
            out Vector4 hit2,
            out Vector4 hit3,
            out float radius0,
            out float radius1,
            out float radius2,
            out float radius3)
        {
            int index0 = -1;
            int index1 = -1;
            int index2 = -1;
            int index3 = -1;
            hit0 = Vector4.zero;
            hit1 = Vector4.zero;
            hit2 = Vector4.zero;
            hit3 = Vector4.zero;
            radius0 = 0f;
            radius1 = 0f;
            radius2 = 0f;
            radius3 = 0f;

            if (this.pulses == null || this.pulses.Length == 0)
            {
                return 0;
            }

            for (int i = 0; i < this.pulses.Length; i++)
            {
                if (!this.pulses[i].Valid)
                {
                    continue;
                }

                int startTick = this.pulses[i].StartTick;
                if (index0 < 0 || startTick > this.pulses[index0].StartTick)
                {
                    index3 = index2;
                    index2 = index1;
                    index1 = index0;
                    index0 = i;
                }
                else if (index1 < 0 || startTick > this.pulses[index1].StartTick)
                {
                    index3 = index2;
                    index2 = index1;
                    index1 = i;
                }
                else if (index2 < 0 || startTick > this.pulses[index2].StartTick)
                {
                    index3 = index2;
                    index2 = i;
                }
                else if (index3 < 0 || startTick > this.pulses[index3].StartTick)
                {
                    index3 = i;
                }
            }

            int count = 0;
            if (index0 >= 0)
            {
                hit0 = this.BuildHitVector(this.pulses[index0], width, depth);
                radius0 = SurfaceShieldFallbackTuning.NormalizePulseRadius(
                    this.pulses[index0].Radius,
                    width,
                    depth,
                    this.Props.maxHitUvRadius);
                count++;
            }

            if (index1 >= 0)
            {
                hit1 = this.BuildHitVector(this.pulses[index1], width, depth);
                radius1 = SurfaceShieldFallbackTuning.NormalizePulseRadius(
                    this.pulses[index1].Radius,
                    width,
                    depth,
                    this.Props.maxHitUvRadius);
                count++;
            }

            if (index2 >= 0)
            {
                hit2 = this.BuildHitVector(this.pulses[index2], width, depth);
                radius2 = SurfaceShieldFallbackTuning.NormalizePulseRadius(
                    this.pulses[index2].Radius,
                    width,
                    depth,
                    this.Props.maxHitUvRadius);
                count++;
            }

            if (index3 >= 0)
            {
                hit3 = this.BuildHitVector(this.pulses[index3], width, depth);
                radius3 = SurfaceShieldFallbackTuning.NormalizePulseRadius(
                    this.pulses[index3].Radius,
                    width,
                    depth,
                    this.Props.maxHitUvRadius);
                count++;
            }

            return count;
        }

        private Vector4 BuildHitVector(ShieldHitPulse pulse, float width, float depth)
        {
            Vector2 uv = pulse.HasHullUv
                ? pulse.HullUv
                : SurfaceShieldHullGeometry.WorldPositionToHullUv(this, pulse.WorldPosition, new Vector2(width, depth));

            // Surface shield impacts are shader-side hull UV events, not world
            // radius discs. WorldPosition remains for procedural fallback drawing.
            return new Vector4(
                uv.x,
                uv.y,
                pulse.StartTick,
                pulse.Strength);
        }

        private Color GetCurrentShieldOverlayColor()
        {
            return new Color(0.42f, 0.68f, 0.74f, 1f);
        }

        private Texture2D ResolveSurfaceMaskTexture(
            ShuttleSurfaceShieldVisualConfigSnapshot config,
            Texture2D fallbackTexture)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.SurfaceMaskTexturePath))
            {
                return fallbackTexture;
            }

            string configuredPath = config.SurfaceMaskTexturePath.Trim();
            string texturePath = this.ResolveDirectionalSurfaceMaskTexturePath(configuredPath);
            if (this.cachedSurfaceMaskTextureResolved &&
                string.Equals(this.cachedSurfaceMaskTexturePath, texturePath, StringComparison.Ordinal))
            {
                return this.cachedSurfaceMaskTexture ?? fallbackTexture;
            }

            this.cachedSurfaceMaskTexturePath = texturePath;
            this.cachedSurfaceMaskTexture = null;
            this.cachedSurfaceMaskTextureResolved = true;
            try
            {
                Texture2D texture = ContentFinder<Texture2D>.Get(texturePath, false);
                if (texture != null)
                {
                    this.cachedSurfaceMaskTexture = texture;
                    return texture;
                }

                this.LogSurfaceMaskFallbackOnce(texturePath, "missing texture");
            }
            catch (Exception exception)
            {
                this.LogSurfaceMaskFallbackOnce(texturePath, exception.Message);
            }

            return fallbackTexture;
        }

        private string ResolveDirectionalSurfaceMaskTexturePath(string texturePath)
        {
            if (this.parent == null || string.IsNullOrWhiteSpace(texturePath))
            {
                return texturePath;
            }

            int rotationIndex = this.parent.Rotation.AsInt;
            if (rotationIndex < 0 || rotationIndex >= SurfaceMaskRotationSuffixes.Length)
            {
                return texturePath;
            }

            string suffix = SurfaceMaskRotationSuffixes[rotationIndex];
            if (string.IsNullOrEmpty(suffix) ||
                texturePath.EndsWith(suffix, StringComparison.Ordinal))
            {
                return texturePath;
            }

            string directionalPath = texturePath + suffix;
            try
            {
                return ContentFinder<Texture2D>.Get(directionalPath, false) != null
                    ? directionalPath
                    : texturePath;
            }
            catch
            {
                return texturePath;
            }
        }

        private void LogSurfaceMaskFallbackOnce(string texturePath, string reason)
        {
            if (!Prefs.DevMode)
            {
                return;
            }

            ShuttleLog.WarnOnce(
                "SurfaceShieldVisual",
                "surface-mask-fallback|" + (texturePath ?? "<null>") + "|" + (reason ?? "unknown"),
                "Surface shield mask texture unavailable; using hull texture alpha as visual fallback. path=" +
                (texturePath ?? "<null>") +
                ", reason=" +
                (reason ?? "unknown"));
        }

        private static Texture2D GetSharedNoiseTexture()
        {
            if (sharedNoiseTexture != null)
            {
                return sharedNoiseTexture;
            }

            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "CT_SurfaceShield_ProceduralNoise";
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Bilinear;
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float value = HashNoise01(x, y);
                    pixels[y * size + x] = new Color(value, value, value, 1f);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            sharedNoiseTexture = texture;
            return sharedNoiseTexture;
        }

        private static float HashNoise01(int x, int y)
        {
            unchecked
            {
                uint hash = (uint)(x * 374761393 + y * 668265263);
                hash = (hash ^ (hash >> 13)) * 1274126177u;
                hash ^= hash >> 16;
                return (hash & 0x00FFFFFF) / 16777215f;
            }
        }
    }
}
