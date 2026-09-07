using CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx;
using CeleTech.ShuttleExtension.ModularShuttle.Rendering.CustomShaders;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx.Vapor
{
    internal sealed class ShuttleFlightVfxVaporDrawer
    {
        internal static readonly ShuttleFlightVfxVaporDrawer Shared =
            new ShuttleFlightVfxVaporDrawer(
                ShuttleFlightVfxVaporMeshCache.Shared,
                ShuttleFlightVfxVaporMaterialCache.Shared,
                ShuttleFlightVfxVaporCalibrationDrawer.Shared);

        private const string LogCategory = "ShuttleFlightVfxVapor";
        private const float MinIntensity = 0.01f;
        private const float OverHullYOffset = 0.058f;
        private const float UnderHullYOffset = -0.012f;
        private const float EdgeRibbonYOffset = 0.046f;
        private const float RootCoreYOffset = 0.148f;
        private const float ForceVisibleYOffset = 0.10f;
        private const float ForceVisibleDissolve = 0.20f;
        private const float ForceVisibleFinalAlpha = 0.60f;
        private const int ForceVisibleAnchorCount = 6;
        private const bool DrawMainVaporRibbonMesh = false;
        private const bool EnableCoreTuningDetailLayers = false;

        private readonly ShuttleFlightVfxVaporMeshCache meshCache;
        private readonly ShuttleFlightVfxVaporMaterialCache materialCache;
        private readonly ShuttleFlightVfxVaporCalibrationDrawer calibrationDrawer;
        private readonly MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

        private ShuttleFlightVfxVaporDrawer(
            ShuttleFlightVfxVaporMeshCache meshCache,
            ShuttleFlightVfxVaporMaterialCache materialCache,
            ShuttleFlightVfxVaporCalibrationDrawer calibrationDrawer)
        {
            this.meshCache = meshCache;
            this.materialCache = materialCache;
            this.calibrationDrawer = calibrationDrawer;
        }

        internal void Draw(
            Skyfaller skyfaller,
            Vector3 drawLoc,
            Rot4 drawRotation,
            float extraRotation,
            ShuttleFlightVfxMode mode,
            float timeInAnimation,
            ShuttleVaporQualityProfile profile)
        {
            string skyfallerKey = SkyfallerKey(skyfaller);
            bool calibrationMode = ShuttleFlightVfxVaporDebugSettings.ShouldDrawCalibrationOverlay;
            bool forceVisible = ShuttleFlightVfxVaporDebugSettings.ShouldForceVisibleVapor;
            bool drawRibbon = ShuttleFlightVfxVaporDebugSettings.ShouldDrawVaporRibbon;
            bool drawFlecks = ShuttleFlightVfxVaporDebugSettings.ShouldDrawVaporFlecks;
            ShuttleVaporQualityProfile effectiveProfile = forceVisible
                ? ShuttleVaporQualityProfile.ForQuality(ShuttleVaporQuality.High)
                : profile;

            this.LogDrawEntryOnce(skyfallerKey, skyfaller, mode, timeInAnimation, profile, effectiveProfile, forceVisible, calibrationMode);

            if (!calibrationMode && !forceVisible && !profile.ShouldDraw)
            {
                this.LogDrawSkipOnce(
                    skyfallerKey,
                    mode,
                    "skip:profile-should-draw-false",
                    profile,
                    effectiveProfile,
                    forceVisible);
                return;
            }

            if (!this.CanDraw(skyfaller, drawRibbon))
            {
                this.LogDrawSkipOnce(
                    skyfallerKey,
                    mode,
                    "skip:can-draw-false",
                    profile,
                    effectiveProfile,
                    forceVisible);
                return;
            }

            if (calibrationMode)
            {
                if (this.calibrationDrawer != null)
                {
                    this.calibrationDrawer.Draw(skyfaller, drawLoc, drawRotation, extraRotation);
                }

                return;
            }

            if (!drawRibbon && !drawFlecks)
            {
                return;
            }

            Material material = null;
            if (drawRibbon)
            {
                material = this.materialCache.GetVaporMaterial();
                CustomShaderMaterialValidationResult validation =
                    UnityCustomShaderMaterialValidator.Validate(
                        material,
                        ShuttleCustomShaderMaterialContracts.Vapor);
                if (!validation.IsValid)
                {
                    ShuttleCustomShaderDiagnostics.LogMaterialValidation(
                        "vapor-draw-gate",
                        ShuttleFlightVfxVaporBundleLoader.SelectedBundleRelativePath,
                        ShuttleFlightVfxVaporBundleContract.MaterialName,
                        material,
                        validation,
                        "skip");
                    this.LogDrawSkipOnce(
                        skyfallerKey,
                        mode,
                        "skip:material-invalid",
                        profile,
                        effectiveProfile,
                        forceVisible);

                    if (!drawFlecks)
                    {
                        return;
                    }

                    drawRibbon = false;
                }
            }

            float normalizedTime = Mathf.Clamp01(timeInAnimation);
            float envelopeIntensity = forceVisible
                ? 1.0f
                : ShuttleFlightVfxVaporProgress.Evaluate(skyfaller, mode, timeInAnimation);
            this.LogEnvelopeOnce(
                skyfallerKey,
                skyfaller,
                mode,
                timeInAnimation,
                normalizedTime,
                envelopeIntensity,
                profile,
                effectiveProfile,
                forceVisible);

            if (!forceVisible && envelopeIntensity <= MinIntensity)
            {
                this.LogDrawSkipOnce(
                    skyfallerKey,
                    mode,
                    "skip:envelope-intensity-below-min",
                    profile,
                    effectiveProfile,
                    forceVisible);
                return;
            }

            Vector2 drawSize = ShuttleFlightVfxDrawSizeResolver.ResolveDrawSize(skyfaller);
            Quaternion rotation = ShuttleFlightVfxVaporAnchorFrameResolver.BuildRotation(
                skyfaller.Graphic,
                drawRotation,
                extraRotation);
            Vector3 origin = ShuttleFlightVfxVaporAnchorFrameResolver.ResolveOrigin(skyfaller, drawLoc, drawRotation);
            float ticks = Find.TickManager != null ? Find.TickManager.TicksGame : 0f;
            float timePhase = ticks / 60f;

            ShuttleFlightVfxVaporAnchor[] anchors = ShuttleFlightVfxVaporLayoutProvider.Anchors;
            int selectedAnchors = 0;
            int detailAnchors = 0;
            if (drawRibbon)
            {
                for (int i = 0; i < anchors.Length; i++)
                {
                    ShuttleFlightVfxVaporAnchor anchor = anchors[i];
                    if (!this.ShouldDrawAnchor(anchor, effectiveProfile, selectedAnchors, i, forceVisible))
                    {
                        this.LogAnchorFilteredOnce(
                            skyfallerKey,
                            anchor,
                            mode,
                            selectedAnchors,
                            i,
                            effectiveProfile,
                            forceVisible);
                        continue;
                    }

                    this.DrawAnchor(
                        skyfallerKey,
                        anchor,
                        envelopeIntensity,
                        drawSize,
                        origin,
                        rotation,
                        timePhase,
                        material,
                        effectiveProfile,
                        false,
                        mode,
                        forceVisible);
                    selectedAnchors++;

                    if (EnableCoreTuningDetailLayers &&
                        effectiveProfile.UseDetailLayer &&
                        detailAnchors < effectiveProfile.MaxDetailAnchors)
                    {
                        this.DrawAnchor(
                            skyfallerKey,
                            anchor,
                            envelopeIntensity,
                            drawSize,
                            origin,
                            rotation,
                            timePhase,
                            material,
                            effectiveProfile,
                            true,
                            mode,
                            forceVisible);
                        detailAnchors++;
                    }
                }
            }

            this.LogDrawSummaryOnce(
                skyfallerKey,
                mode,
                anchors.Length,
                selectedAnchors,
                detailAnchors,
                envelopeIntensity,
                effectiveProfile,
                forceVisible);

            if (drawFlecks)
            {
                ShuttleFlightVfxVaporFleckEmitter.Shared.Emit(
                    skyfaller,
                    drawLoc,
                    drawRotation,
                    extraRotation,
                    mode,
                    envelopeIntensity,
                    effectiveProfile);
            }
        }

        private bool ShouldDrawAnchor(
            ShuttleFlightVfxVaporAnchor anchor,
            ShuttleVaporQualityProfile profile,
            int drawnAnchors,
            int anchorIndex,
            bool forceVisible)
        {
            if (!ShuttleFlightVfxVaporLayoutProvider.IsRibbonDrawAnchor(anchor))
            {
                return false;
            }

            if (!ShuttleFlightVfxVaporDebugSettings.ShouldDrawKind(anchor.Kind))
            {
                return false;
            }

            if (!ShuttleFlightVfxVaporDebugSettings.ShouldDrawAnchorId(anchor.Id))
            {
                return false;
            }

            if (forceVisible)
            {
                return drawnAnchors < ForceVisibleAnchorCount;
            }

            return drawnAnchors < profile.MaxAnchors &&
                anchor.QualityRank <= profile.MaxQualityRank;
        }

        private bool CanDraw(Skyfaller skyfaller, bool drawRibbon)
        {
            return skyfaller != null &&
                skyfaller.Graphic != null &&
                (!drawRibbon ||
                    (this.meshCache != null &&
                        this.materialCache != null));
        }

        private void DrawAnchor(
            string skyfallerKey,
            ShuttleFlightVfxVaporAnchor anchor,
            float envelopeIntensity,
            Vector2 drawSize,
            Vector3 origin,
            Quaternion rotation,
            float timePhase,
            Material material,
            ShuttleVaporQualityProfile profile,
            bool detailLayer,
            ShuttleFlightVfxMode mode,
            bool forceVisible)
        {
            float layerAlpha = detailLayer ? 0.48f : 1f;
            float layerScale = detailLayer ? 0.58f : 1f;
            float visualEnvelope = envelopeIntensity;
            if (!forceVisible && visualEnvelope > MinIntensity)
            {
                visualEnvelope = Mathf.Max(visualEnvelope, 0.52f);
            }

            float intensity = visualEnvelope * anchor.Strength * profile.AlphaMultiplier * layerAlpha;
            if (forceVisible)
            {
                visualEnvelope = 1f;
                intensity = Mathf.Max(1f, intensity);
            }

            if (!forceVisible && intensity <= MinIntensity)
            {
                this.LogAnchorDecisionOnce(
                    skyfallerKey,
                    anchor,
                    mode,
                    detailLayer,
                    "skip:intensity-below-min",
                    envelopeIntensity,
                    visualEnvelope,
                    intensity,
                    null,
                    0f,
                    0f,
                    Vector3.zero,
                    material,
                    profile,
                    forceVisible);
                return;
            }

            Mesh mesh = this.meshCache.GetMesh(anchor.Kind);
            this.LogMeshDiagnosticsOnce(anchor.Kind, mesh);
            if (mesh == null)
            {
                this.LogAnchorDecisionOnce(
                    skyfallerKey,
                    anchor,
                    mode,
                    detailLayer,
                    "skip:mesh-null",
                    envelopeIntensity,
                    visualEnvelope,
                    intensity,
                    mesh,
                    0f,
                    0f,
                    Vector3.zero,
                    material,
                    profile,
                    forceVisible);
                return;
            }

            bool edgeRibbon = anchor.Kind == ShuttleFlightVfxVaporKind.EdgeRibbon;
            bool curlPuff = anchor.Kind == ShuttleFlightVfxVaporKind.CurlPuff;
            float waveSpeed = edgeRibbon ? 0.36f : curlPuff ? 0.72f : 0.55f;
            float slowWaveSpeed = edgeRibbon ? 0.28f : curlPuff ? 0.58f : 0.42f;
            float wave = 0.5f + (0.5f * Mathf.Sin((timePhase * waveSpeed) + anchor.PhaseOffset));
            float slowWave = 0.5f + (0.5f * Mathf.Sin((timePhase * slowWaveSpeed) + (anchor.PhaseOffset * 1.20f)));
            float alphaWave = forceVisible
                ? 1f
                : Mathf.Lerp(
                    edgeRibbon ? 0.94f : curlPuff ? 0.92f : 0.94f,
                    edgeRibbon ? 1.04f : curlPuff ? 1.08f : 1.06f,
                    wave);
            float lengthBreathAmp = edgeRibbon ? 0.025f : curlPuff ? 0.045f : 0.035f;
            float widthBreathAmp = edgeRibbon ? 0.020f : curlPuff ? 0.035f : 0.030f;
            float normalDriftAmp = edgeRibbon ? 0.012f : curlPuff ? 0.030f : 0.022f;
            float trailingShiftAmp = edgeRibbon ? 0.024f : curlPuff ? 0.026f : 0.032f;
            float animatedLengthScale = Mathf.Min(
                1f + (lengthBreathAmp * Mathf.Sin((timePhase * waveSpeed) + anchor.PhaseOffset)),
                profile.MaxAnimationScale);
            float animatedWidthScale = Mathf.Min(
                1f + (widthBreathAmp * Mathf.Sin((timePhase * slowWaveSpeed) + (anchor.PhaseOffset * 1.20f))),
                profile.MaxAnimationScale);
            float length = anchor.BaseSize.x * anchor.SizeMultiplier * anchor.LengthMultiplier * animatedLengthScale * profile.SizeMultiplier * layerScale;
            float width = anchor.BaseSize.y * anchor.SizeMultiplier * anchor.WidthMultiplier * animatedWidthScale * profile.SizeMultiplier * layerScale;
            if (edgeRibbon)
            {
                length *= 1.00f;
                width *= 1.00f;
            }
            else if (anchor.Kind == ShuttleFlightVfxVaporKind.ShoulderPuff)
            {
                length *= 0.55f;
                width *= 0.85f;
            }
            else if (curlPuff)
            {
                length *= 0.75f;
                width *= 0.90f;
            }

            float normalDrift = forceVisible
                ? 0f
                : normalDriftAmp * Mathf.Sin((timePhase * 0.55f) + (anchor.PhaseOffset * 1.70f));
            float trailingShift = forceVisible
                ? 0f
                : trailingShiftAmp * Mathf.Sin((timePhase * 0.48f) + (anchor.PhaseOffset * 1.19f));
            float streamwiseOffset = anchor.StreamwiseOffsetLocal + (edgeRibbon ? 0f : trailingShift);
            ShuttleFlightVfxVaporAnchorFrame anchorFrame =
                ShuttleFlightVfxVaporAnchorFrameResolver.ResolveCalibratedFrame(
                    drawSize,
                    origin,
                    rotation,
                    anchor,
                    anchor.CenterOffsetLocal + normalDrift,
                    streamwiseOffset);
            Vector2 drawLengthAxis = anchorFrame.TangentLocal;
            Vector3 rootLocal = anchorFrame.RootLocal;
            Vector3 centerLocal;
            if (edgeRibbon)
            {
                Vector3 halfLengthLocal = new Vector3(
                    drawLengthAxis.x * length * 0.5f,
                    0f,
                    drawLengthAxis.y * length * 0.5f);
                centerLocal = rootLocal + halfLengthLocal;
            }
            else
            {
                centerLocal = rootLocal;
            }

            Vector3 center = origin + (rotation * centerLocal);
            if (edgeRibbon)
            {
                center.y += EdgeRibbonYOffset;
            }
            else
            {
                center.y += anchor.DrawOverHull ? OverHullYOffset : UnderHullYOffset;
            }

            if (detailLayer)
            {
                center.y += 0.010f;
            }

            if (forceVisible)
            {
                center.y += ForceVisibleYOffset;
            }

            Quaternion meshRotation = anchorFrame.MeshRotation;

            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(center, meshRotation, new Vector3(width, 1f, length));

            float finalIntensity = intensity * alphaWave;
            if (edgeRibbon)
            {
                finalIntensity *= 1.35f;
            }
            else if (anchor.Kind == ShuttleFlightVfxVaporKind.ShoulderPuff)
            {
                finalIntensity *= 1.85f;
            }
            else if (curlPuff)
            {
                finalIntensity *= 1.70f;
            }

            bool drawMainVaporMesh = this.ShouldDrawMainVaporMesh(anchor);
            if (edgeRibbon && drawMainVaporMesh)
            {
                Matrix4x4 haloMatrix = default(Matrix4x4);
                haloMatrix.SetTRS(center, meshRotation, new Vector3(width * 1.45f, 1f, length * 0.92f));
                this.ApplyEdgeRibbonHaloShaderParams(
                    anchor,
                    finalIntensity * 0.55f,
                    timePhase,
                    profile,
                    forceVisible);
                Graphics.DrawMesh(mesh, haloMatrix, material, 0, null, 0, this.propertyBlock);
            }

            if (drawMainVaporMesh)
            {
                this.ApplyShaderParams(anchor, finalIntensity, timePhase, profile, detailLayer, forceVisible);
                Graphics.DrawMesh(mesh, matrix, material, 0, null, 0, this.propertyBlock);
            }

            if (!edgeRibbon && !detailLayer)
            {
                this.DrawAnchorRootCore(
                    anchor,
                    envelopeIntensity,
                    drawSize,
                    origin,
                    rotation,
                    timePhase,
                    mesh,
                    material,
                    profile,
                    forceVisible,
                    normalDrift,
                    streamwiseOffset,
                    length,
                    width,
                    finalIntensity);
            }

            this.LogAnchorDecisionOnce(
                skyfallerKey,
                anchor,
                mode,
                detailLayer,
                forceVisible ? "draw:force-visible" : "draw:mesh",
                envelopeIntensity,
                visualEnvelope,
                finalIntensity,
                mesh,
                length,
                width,
                center,
                material,
                profile,
                forceVisible);
        }

        private bool ShouldDrawMainVaporMesh(ShuttleFlightVfxVaporAnchor anchor)
        {
            return DrawMainVaporRibbonMesh ||
                anchor.Kind == ShuttleFlightVfxVaporKind.EdgeRibbon;
        }

        private void DrawAnchorRootCore(
            ShuttleFlightVfxVaporAnchor anchor,
            float envelopeIntensity,
            Vector2 drawSize,
            Vector3 origin,
            Quaternion rotation,
            float timePhase,
            Mesh mesh,
            Material material,
            ShuttleVaporQualityProfile profile,
            bool forceVisible,
            float normalDrift,
            float streamwiseOffset,
            float length,
            float width,
            float finalIntensity)
        {
            if (mesh == null || material == null || material == BaseContent.BadMat)
            {
                return;
            }

            bool curlPuff = anchor.Kind == ShuttleFlightVfxVaporKind.CurlPuff;
            float coreLength = curlPuff
                ? Mathf.Clamp(length * 0.36f, 0.48f, 0.85f)
                : Mathf.Clamp(length * 0.28f, 0.72f, 1.18f);
            float coreWidth = curlPuff
                ? Mathf.Clamp(width * 1.55f, 0.52f, 0.82f)
                : Mathf.Clamp(width * 1.85f, 0.72f, 1.08f);
            float coreStreamwiseOffset = this.ResolveRootCoreStreamwiseOffset(anchor, streamwiseOffset);
            float coreNormalOffset = this.ResolveRootCoreNormalOffset(anchor, normalDrift);
            ShuttleFlightVfxVaporAnchorFrame coreFrame =
                ShuttleFlightVfxVaporAnchorFrameResolver.ResolveCalibratedFrame(
                    drawSize,
                    origin,
                    rotation,
                    anchor,
                    coreNormalOffset,
                    coreStreamwiseOffset);
            Vector3 coreCenterLocal =
                coreFrame.RootLocal +
                new Vector3(
                    coreFrame.TangentLocal.x * coreLength * 0.34f,
                    0f,
                    coreFrame.TangentLocal.y * coreLength * 0.34f);
            Vector3 coreCenter = origin + (rotation * coreCenterLocal);
            coreCenter.y += this.ResolveRootCoreYOffset(anchor);
            if (forceVisible)
            {
                coreCenter.y += ForceVisibleYOffset;
            }

            Matrix4x4 coreMatrix = default(Matrix4x4);
            coreMatrix.SetTRS(coreCenter, coreFrame.MeshRotation, new Vector3(coreWidth, 1f, coreLength));
            this.ApplyRootCoreShaderParams(anchor, finalIntensity, timePhase, profile, forceVisible);
            Graphics.DrawMesh(mesh, coreMatrix, material, 0, null, 0, this.propertyBlock);
        }

        private float ResolveRootCoreStreamwiseOffset(
            ShuttleFlightVfxVaporAnchor anchor,
            float fallbackStreamwiseOffset)
        {
            if (anchor.Kind == ShuttleFlightVfxVaporKind.ShoulderPuff)
            {
                return anchor.StreamwiseOffsetLocal * 0.28f;
            }

            if (anchor.Kind == ShuttleFlightVfxVaporKind.CurlPuff)
            {
                return anchor.StreamwiseOffsetLocal * 0.45f;
            }

            return fallbackStreamwiseOffset;
        }

        private float ResolveRootCoreNormalOffset(
            ShuttleFlightVfxVaporAnchor anchor,
            float normalDrift)
        {
            float offset = anchor.CenterOffsetLocal + normalDrift;
            if (anchor.Id == "UpperRearPodOuterShoulder")
            {
                return offset;
            }

            if (anchor.Id == "LowerRearPodOuterShoulder")
            {
                return offset + 0.24f;
            }

            if (anchor.Id == "UpperPodRootCurl")
            {
                return offset + 0.18f;
            }

            if (anchor.Id == "LowerPodRootCurl")
            {
                return offset + 0.22f;
            }

            return offset;
        }

        private float ResolveRootCoreYOffset(ShuttleFlightVfxVaporAnchor anchor)
        {
            if (anchor.Id == "UpperRearPodOuterShoulder")
            {
                return RootCoreYOffset;
            }

            if (anchor.Id == "LowerRearPodOuterShoulder")
            {
                return RootCoreYOffset + 0.28f;
            }

            if (anchor.Id == "UpperPodRootCurl")
            {
                return RootCoreYOffset + 0.26f;
            }

            if (anchor.Id == "LowerPodRootCurl")
            {
                return RootCoreYOffset + 0.14f;
            }

            return RootCoreYOffset;
        }

        private void ApplyShaderParams(
            ShuttleFlightVfxVaporAnchor anchor,
            float intensity,
            float timePhase,
            ShuttleVaporQualityProfile profile,
            bool detailLayer,
            bool forceVisible)
        {
            ShuttleFlightVfxVaporParams parameters =
                ShuttleFlightVfxVaporParams.ForKind(anchor.Kind);

            float clampedIntensity = Mathf.Clamp01(intensity);
            float noiseScaleMultiplier = this.ResolveNoiseScaleMultiplier(profile, detailLayer);
            float noiseScaleX = parameters.NoiseScaleX * noiseScaleMultiplier;
            float noiseScaleY = parameters.NoiseScaleY * noiseScaleMultiplier;
            float noiseStrengthFront = this.ResolveNoiseStrength(
                parameters.NoiseStrengthFront,
                profile,
                detailLayer);
            float noiseStrengthTail = this.ResolveNoiseStrength(
                parameters.NoiseStrengthTail,
                profile,
                detailLayer);
            float flowSpeed = parameters.FlowSpeed * profile.MotionMultiplier * (detailLayer ? 1.10f : 1f);
            float curlAmp = parameters.CurlAmp * profile.MotionMultiplier * (detailLayer ? 1.08f : 1f);
            Color color = forceVisible
                ? new Color(1f, 0.88f, 0.96f, 1f)
                : this.ResolveAnchorColor(anchor, parameters.Color);

            this.propertyBlock.Clear();
            this.propertyBlock.SetColor(ShuttleFlightVfxVaporShaderPropertyIds.Color, color);
            float alpha = parameters.Alpha * clampedIntensity;
            float dissolve = Mathf.Lerp(
                Mathf.Min(parameters.Dissolve + 0.08f, 0.72f),
                parameters.Dissolve,
                clampedIntensity);
            if (forceVisible)
            {
                dissolve = ForceVisibleDissolve;
                alpha = Mathf.Max(alpha, ForceVisibleFinalAlpha / Mathf.Max(0.001f, dissolve));
            }

            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.Alpha, alpha);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.TimePhase, timePhase);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.WidthStart, parameters.WidthStart);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.WidthPeak, parameters.WidthPeak);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.WidthEnd, parameters.WidthEnd);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.PeakPos, parameters.PeakPos);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.RootFade, parameters.RootFade);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.TailFadeStart, parameters.TailFadeStart);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.CenterPower, parameters.CenterPower);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.NoiseScaleX, noiseScaleX);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.NoiseScaleY, noiseScaleY);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.NoiseStrengthFront, noiseStrengthFront);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.NoiseStrengthTail, noiseStrengthTail);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.TailNoiseStart, parameters.TailNoiseStart);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.FlowSpeed, flowSpeed);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.Dissolve, dissolve);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.EdgeFeather, parameters.EdgeFeather);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.TailBreakup, parameters.TailBreakup);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.PhaseOffset, anchor.PhaseOffset);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.CurlAmp, curlAmp);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.CurlFreq, parameters.CurlFreq);
            // Motion scaling is pre-applied to flow/curl parameters above.
            // Keep this shader property neutral so it cannot accidentally double-scale motion.
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.MotionMultiplier, 1f);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.DetailLayer, detailLayer ? 1f : 0f);
        }

        private void ApplyRootCoreShaderParams(
            ShuttleFlightVfxVaporAnchor anchor,
            float intensity,
            float timePhase,
            ShuttleVaporQualityProfile profile,
            bool forceVisible)
        {
            ShuttleFlightVfxVaporParams parameters =
                ShuttleFlightVfxVaporParams.ForKind(anchor.Kind);
            bool curlPuff = anchor.Kind == ShuttleFlightVfxVaporKind.CurlPuff;
            float clampedIntensity = Mathf.Clamp01(intensity * (curlPuff ? 1.20f : 1.35f));
            float alpha = Mathf.Clamp(parameters.Alpha * clampedIntensity * (curlPuff ? 2.10f : 2.35f), 0f, 0.95f);
            if (forceVisible)
            {
                alpha = Mathf.Max(alpha, ForceVisibleFinalAlpha);
            }

            this.propertyBlock.Clear();
            this.propertyBlock.SetColor(
                ShuttleFlightVfxVaporShaderPropertyIds.Color,
                new Color(0.94f, 0.985f, 1.00f, 1f));
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.Alpha, alpha);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.TimePhase, timePhase);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.WidthStart, curlPuff ? 0.40f : 0.46f);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.WidthPeak, curlPuff ? 0.58f : 0.66f);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.WidthEnd, curlPuff ? 0.34f : 0.42f);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.PeakPos, 0.24f);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.RootFade, 0.012f);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.TailFadeStart, 0.62f);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.CenterPower, 0.50f);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.NoiseScaleX, parameters.NoiseScaleX * 0.70f);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.NoiseScaleY, parameters.NoiseScaleY * 0.82f);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.NoiseStrengthFront, 0.02f);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.NoiseStrengthTail, 0.08f);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.TailNoiseStart, 0.72f);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.FlowSpeed, parameters.FlowSpeed * profile.MotionMultiplier * 0.55f);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.Dissolve, 0.01f);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.EdgeFeather, curlPuff ? 0.20f : 0.24f);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.TailBreakup, 0.06f);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.PhaseOffset, anchor.PhaseOffset);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.CurlAmp, parameters.CurlAmp * 0.45f);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.CurlFreq, parameters.CurlFreq);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.MotionMultiplier, 1f);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.DetailLayer, 0f);
        }

        private void ApplyEdgeRibbonHaloShaderParams(
            ShuttleFlightVfxVaporAnchor anchor,
            float intensity,
            float timePhase,
            ShuttleVaporQualityProfile profile,
            bool forceVisible)
        {
            ShuttleFlightVfxVaporParams parameters =
                ShuttleFlightVfxVaporParams.ForKind(anchor.Kind);

            float clampedIntensity = Mathf.Clamp01(intensity);
            float noiseScaleMultiplier = this.ResolveNoiseScaleMultiplier(profile, false);
            float noiseScaleX = parameters.NoiseScaleX * noiseScaleMultiplier;
            float noiseScaleY = parameters.NoiseScaleY * noiseScaleMultiplier;
            float noiseStrengthFront = this.ResolveNoiseStrength(
                parameters.NoiseStrengthFront,
                profile,
                false);
            float noiseStrengthTail = this.ResolveNoiseStrength(0.08f, profile, false);
            float flowSpeed = parameters.FlowSpeed * profile.MotionMultiplier;
            float curlAmp = parameters.CurlAmp * profile.MotionMultiplier;
            Color color = forceVisible
                ? new Color(1f, 0.88f, 0.96f, 1f)
                : this.ResolveAnchorColor(anchor, parameters.Color);

            this.propertyBlock.Clear();
            this.propertyBlock.SetColor(ShuttleFlightVfxVaporShaderPropertyIds.Color, color);
            float alpha = parameters.Alpha * 0.65f * clampedIntensity;
            float dissolve = Mathf.Lerp(
                Mathf.Min(parameters.Dissolve + 0.05f, 0.60f),
                parameters.Dissolve,
                clampedIntensity);
            if (forceVisible)
            {
                dissolve = ForceVisibleDissolve;
                alpha = Mathf.Max(alpha, ForceVisibleFinalAlpha * 0.40f);
            }

            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.Alpha, alpha);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.TimePhase, timePhase);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.WidthStart, parameters.WidthStart * 1.35f);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.WidthPeak, parameters.WidthPeak * 1.45f);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.WidthEnd, parameters.WidthEnd * 1.35f);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.PeakPos, parameters.PeakPos);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.RootFade, parameters.RootFade);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.TailFadeStart, parameters.TailFadeStart);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.CenterPower, 0.55f);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.NoiseScaleX, noiseScaleX);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.NoiseScaleY, noiseScaleY);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.NoiseStrengthFront, noiseStrengthFront);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.NoiseStrengthTail, noiseStrengthTail);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.TailNoiseStart, parameters.TailNoiseStart);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.FlowSpeed, flowSpeed);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.Dissolve, dissolve);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.EdgeFeather, parameters.EdgeFeather * 1.60f);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.TailBreakup, 0.08f);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.PhaseOffset, anchor.PhaseOffset);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.CurlAmp, curlAmp);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.CurlFreq, parameters.CurlFreq);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.MotionMultiplier, 1f);
            this.propertyBlock.SetFloat(ShuttleFlightVfxVaporShaderPropertyIds.DetailLayer, 0f);
        }

        private Color ResolveAnchorColor(ShuttleFlightVfxVaporAnchor anchor, Color defaultColor)
        {
            if (!ShuttleFlightVfxVaporDebugSettings.ShouldTintCoreAnchors)
            {
                return defaultColor;
            }

            if (anchor.Id == "UpperRearPodOuterShoulder")
            {
                return new Color(1.00f, 0.82f, 0.82f, defaultColor.a);
            }

            if (anchor.Id == "UpperSideEdgeRibbon")
            {
                return new Color(0.76f, 0.88f, 1.00f, defaultColor.a);
            }

            if (anchor.Id == "LowerSideEdgeRibbon")
            {
                return new Color(0.80f, 1.00f, 0.92f, defaultColor.a);
            }

            if (anchor.Id == "LowerRearPodOuterShoulder")
            {
                return new Color(0.82f, 1.00f, 0.84f, defaultColor.a);
            }

            if (anchor.Id == "UpperPodRootCurl")
            {
                return new Color(0.78f, 0.88f, 1.00f, defaultColor.a);
            }

            if (anchor.Id == "LowerPodRootCurl")
            {
                return new Color(1.00f, 0.96f, 0.76f, defaultColor.a);
            }

            return defaultColor;
        }

        private float ResolveNoiseScaleMultiplier(
            ShuttleVaporQualityProfile profile,
            bool detailLayer)
        {
            float scale = 1f;
            if (profile.ShaderQualityMode == ShuttleVaporShaderQualityMode.Simple)
            {
                scale *= 0.72f;
            }
            else if (profile.ShaderQualityMode == ShuttleVaporShaderQualityMode.Detailed)
            {
                scale *= detailLayer ? 1.28f : 1.08f;
            }

            return scale;
        }

        private float ResolveNoiseStrength(
            float baseStrength,
            ShuttleVaporQualityProfile profile,
            bool detailLayer)
        {
            float strength = baseStrength;
            if (profile.ShaderQualityMode == ShuttleVaporShaderQualityMode.Simple)
            {
                strength *= 0.70f;
            }
            else if (profile.ShaderQualityMode == ShuttleVaporShaderQualityMode.Detailed)
            {
                strength = Mathf.Min(1f, strength * (detailLayer ? 1.12f : 1.04f));
            }

            return strength;
        }

        private void LogDrawEntryOnce(
            string skyfallerKey,
            Skyfaller skyfaller,
            ShuttleFlightVfxMode mode,
            float timeInAnimation,
            ShuttleVaporQualityProfile requestedProfile,
            ShuttleVaporQualityProfile effectiveProfile,
            bool forceVisible,
            bool calibrationMode)
        {
            if (!ShuttleFlightVfxVaporDebugSettings.ShouldLogDrawDecisions)
            {
                return;
            }

            ShuttleLog.WarnOnce(
                LogCategory,
                "draw-entry|" + skyfallerKey + "|" + mode,
                "VaporDrawer.Draw called. mode=" +
                mode +
                " skyfaller=" +
                skyfallerKey +
                " timeInAnimation=" +
                FormatFloat(timeInAnimation) +
                " ticksToImpact=" +
                (skyfaller != null ? skyfaller.ticksToImpact.ToString() : "null") +
                " requestedQuality=" +
                requestedProfile.Quality +
                " requestedShouldDraw=" +
                requestedProfile.ShouldDraw +
                " effectiveQuality=" +
                effectiveProfile.Quality +
                " forceVisible=" +
                forceVisible +
                " calibrationMode=" +
                calibrationMode +
                " availabilityBeforeMaterialFetch=" +
                this.AvailabilityText());
        }

        private void LogEnvelopeOnce(
            string skyfallerKey,
            Skyfaller skyfaller,
            ShuttleFlightVfxMode mode,
            float timeInAnimation,
            float normalizedTime,
            float envelopeIntensity,
            ShuttleVaporQualityProfile requestedProfile,
            ShuttleVaporQualityProfile effectiveProfile,
            bool forceVisible)
        {
            if (!ShuttleFlightVfxVaporDebugSettings.ShouldLogDrawDecisions)
            {
                return;
            }

            ShuttleLog.WarnOnce(
                LogCategory,
                "envelope|" + skyfallerKey + "|" + mode,
                "Vapor envelope diagnostics. mode=" +
                mode +
                " skyfaller=" +
                skyfallerKey +
                " timeInAnimation=" +
                FormatFloat(timeInAnimation) +
                " normalizedT=" +
                FormatFloat(normalizedTime) +
                " envelopeIntensity=" +
                FormatFloat(envelopeIntensity) +
                " ticksToImpact=" +
                (skyfaller != null ? skyfaller.ticksToImpact.ToString() : "null") +
                " leaveMapAfterTicks=" +
                (skyfaller != null ? skyfaller.LeaveMapAfterTicks.ToString() : "null") +
                " requestedQuality=" +
                requestedProfile.Quality +
                " effectiveQuality=" +
                effectiveProfile.Quality +
                " profileShouldDraw=" +
                effectiveProfile.ShouldDraw +
                " profileMaxAnchors=" +
                effectiveProfile.MaxAnchors +
                " profileMaxQualityRank=" +
                effectiveProfile.MaxQualityRank +
                " forceVisible=" +
                forceVisible +
                " availability=" +
                this.AvailabilityText());
        }

        private void LogDrawSkipOnce(
            string skyfallerKey,
            ShuttleFlightVfxMode mode,
            string reason,
            ShuttleVaporQualityProfile requestedProfile,
            ShuttleVaporQualityProfile effectiveProfile,
            bool forceVisible)
        {
            if (!ShuttleFlightVfxVaporDebugSettings.ShouldLogDrawDecisions)
            {
                return;
            }

            ShuttleLog.WarnOnce(
                LogCategory,
                "draw-skip|" + skyfallerKey + "|" + mode + "|" + reason,
                "Vapor draw skipped. reason=" +
                reason +
                " mode=" +
                mode +
                " skyfaller=" +
                skyfallerKey +
                " requestedQuality=" +
                requestedProfile.Quality +
                " requestedShouldDraw=" +
                requestedProfile.ShouldDraw +
                " effectiveQuality=" +
                effectiveProfile.Quality +
                " effectiveShouldDraw=" +
                effectiveProfile.ShouldDraw +
                " forceVisible=" +
                forceVisible +
                " availability=" +
                this.AvailabilityText() +
                " materialCacheNull=" +
                (this.materialCache == null) +
                " meshCacheNull=" +
                (this.meshCache == null));
        }

        private void LogAnchorFilteredOnce(
            string skyfallerKey,
            ShuttleFlightVfxVaporAnchor anchor,
            ShuttleFlightVfxMode mode,
            int selectedAnchors,
            int anchorIndex,
            ShuttleVaporQualityProfile profile,
            bool forceVisible)
        {
            if (!ShuttleFlightVfxVaporDebugSettings.ShouldLogDrawDecisions)
            {
                return;
            }

            bool drawAnchor = ShuttleFlightVfxVaporLayoutProvider.IsRibbonDrawAnchor(anchor);
            bool kindAllowed = ShuttleFlightVfxVaporDebugSettings.ShouldDrawKind(anchor.Kind);
            string reason = !drawAnchor
                ? "skip:secondary-anchor-disabled"
                : !kindAllowed
                    ? "skip:debug-kind-filter"
                : forceVisible && selectedAnchors >= ForceVisibleAnchorCount
                    ? "skip:force-visible-anchor-limit"
                    : selectedAnchors >= profile.MaxAnchors
                        ? "skip:max-anchors"
                        : "skip:quality-rank";
            ShuttleLog.WarnOnce(
                LogCategory,
                "anchor-filter|" + skyfallerKey + "|" + mode + "|" + anchor.Id + "|" + reason,
                "Vapor anchor skipped. reason=" +
                reason +
                " mode=" +
                mode +
                " skyfaller=" +
                skyfallerKey +
                " anchorId=" +
                SafeId(anchor.Id) +
                " kind=" +
                anchor.Kind +
                " qualityRank=" +
                anchor.QualityRank +
                " selectedAnchors=" +
                selectedAnchors +
                " anchorIndex=" +
                anchorIndex +
                " profileMaxAnchors=" +
                profile.MaxAnchors +
                " profileMaxQualityRank=" +
                profile.MaxQualityRank +
                " forceVisible=" +
                forceVisible);
        }

        private void LogAnchorDecisionOnce(
            string skyfallerKey,
            ShuttleFlightVfxVaporAnchor anchor,
            ShuttleFlightVfxMode mode,
            bool detailLayer,
            string decision,
            float envelopeIntensity,
            float visualEnvelope,
            float finalIntensity,
            Mesh mesh,
            float length,
            float width,
            Vector3 center,
            Material material,
            ShuttleVaporQualityProfile profile,
            bool forceVisible)
        {
            if (!ShuttleFlightVfxVaporDebugSettings.ShouldLogDrawDecisions)
            {
                return;
            }

            ShuttleLog.WarnOnce(
                LogCategory,
                "anchor-decision|" + skyfallerKey + "|" + mode + "|" + anchor.Id + "|" + detailLayer + "|" + decision,
                "Vapor anchor draw decision. decision=" +
                decision +
                " mode=" +
                mode +
                " skyfaller=" +
                skyfallerKey +
                " anchorId=" +
                SafeId(anchor.Id) +
                " kind=" +
                anchor.Kind +
                " qualityRank=" +
                anchor.QualityRank +
                " envelopeIntensity=" +
                FormatFloat(envelopeIntensity) +
                " visualEnvelope=" +
                FormatFloat(visualEnvelope) +
                " finalIntensity=" +
                FormatFloat(finalIntensity) +
                " meshNull=" +
                (mesh == null) +
                " meshName=" +
                MeshName(mesh) +
                " length=" +
                FormatFloat(length) +
                " width=" +
                FormatFloat(width) +
                " sizeMultiplier=" +
                FormatFloat(anchor.SizeMultiplier) +
                " lengthMultiplier=" +
                FormatFloat(anchor.LengthMultiplier) +
                " widthMultiplier=" +
                FormatFloat(anchor.WidthMultiplier) +
                " streamwiseOffset=" +
                FormatFloat(anchor.StreamwiseOffsetLocal) +
                " center=" +
                FormatVector(center) +
                " material=" +
                MaterialName(material) +
                " shader=" +
                ShaderName(material) +
                " detailLayer=" +
                detailLayer +
                " profileQuality=" +
                profile.Quality +
                " profileMaxAnchors=" +
                profile.MaxAnchors +
                " profileMaxQualityRank=" +
                profile.MaxQualityRank +
                " forceVisible=" +
                forceVisible +
                " availability=" +
                this.AvailabilityText());
        }

        private void LogDrawSummaryOnce(
            string skyfallerKey,
            ShuttleFlightVfxMode mode,
            int availableAnchors,
            int selectedAnchors,
            int detailAnchors,
            float envelopeIntensity,
            ShuttleVaporQualityProfile profile,
            bool forceVisible)
        {
            if (!ShuttleFlightVfxVaporDebugSettings.ShouldLogDrawDecisions)
            {
                return;
            }

            ShuttleLog.WarnOnce(
                LogCategory,
                "draw-summary|" + skyfallerKey + "|" + mode,
                "Vapor draw summary. mode=" +
                mode +
                " skyfaller=" +
                skyfallerKey +
                " availableAnchors=" +
                availableAnchors +
                " selectedAnchors=" +
                selectedAnchors +
                " detailAnchors=" +
                detailAnchors +
                " envelopeIntensity=" +
                FormatFloat(envelopeIntensity) +
                " profileQuality=" +
                profile.Quality +
                " forceVisible=" +
                forceVisible +
                " availability=" +
                this.AvailabilityText());
        }

        private void LogMeshDiagnosticsOnce(ShuttleFlightVfxVaporKind kind, Mesh mesh)
        {
            if (!ShuttleFlightVfxVaporDebugSettings.ShouldLogMeshDiagnostics || mesh == null)
            {
                return;
            }

            Vector3[] vertices = mesh.vertices;
            Vector2[] uvs = mesh.uv;
            float minY = 0f;
            float maxY = 0f;
            bool localYZero = true;
            if (vertices != null && vertices.Length > 0)
            {
                minY = vertices[0].y;
                maxY = vertices[0].y;
                for (int i = 0; i < vertices.Length; i++)
                {
                    minY = Mathf.Min(minY, vertices[i].y);
                    maxY = Mathf.Max(maxY, vertices[i].y);
                    if (Mathf.Abs(vertices[i].y) > 0.0001f)
                    {
                        localYZero = false;
                    }
                }
            }

            float minU = 0f;
            float maxU = 0f;
            float minV = 0f;
            float maxV = 0f;
            bool uvInside01 = true;
            if (uvs != null && uvs.Length > 0)
            {
                minU = uvs[0].x;
                maxU = uvs[0].x;
                minV = uvs[0].y;
                maxV = uvs[0].y;
                for (int i = 0; i < uvs.Length; i++)
                {
                    minU = Mathf.Min(minU, uvs[i].x);
                    maxU = Mathf.Max(maxU, uvs[i].x);
                    minV = Mathf.Min(minV, uvs[i].y);
                    maxV = Mathf.Max(maxV, uvs[i].y);
                    if (uvs[i].x < -0.0001f ||
                        uvs[i].x > 1.0001f ||
                        uvs[i].y < -0.0001f ||
                        uvs[i].y > 1.0001f)
                    {
                        uvInside01 = false;
                    }
                }
            }

            Bounds bounds = mesh.bounds;
            ShuttleLog.WarnOnce(
                LogCategory,
                "mesh|" + kind + "|" + mesh.name,
                "Vapor mesh diagnostics. kind=" +
                kind +
                " meshName=" +
                MeshName(mesh) +
                " vertexCount=" +
                mesh.vertexCount +
                " triangleIndexCount=" +
                (mesh.triangles != null ? mesh.triangles.Length : 0) +
                " boundsCenter=" +
                FormatVector(bounds.center) +
                " boundsSize=" +
                FormatVector(bounds.size) +
                " localYRange=" +
                FormatFloat(minY) +
                ".." +
                FormatFloat(maxY) +
                " localYZero=" +
                localYZero +
                " uvRange=(" +
                FormatFloat(minU) +
                ".." +
                FormatFloat(maxU) +
                ", " +
                FormatFloat(minV) +
                ".." +
                FormatFloat(maxV) +
                ")" +
                " uvInside01=" +
                uvInside01 +
                " vertexUv=" +
                BuildVertexUvDiagnostics(vertices, uvs));
        }

        private static string BuildVertexUvDiagnostics(Vector3[] vertices, Vector2[] uvs)
        {
            if (vertices == null || vertices.Length == 0)
            {
                return "<none>";
            }

            string result = string.Empty;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                Vector2 uv = uvs != null && i < uvs.Length ? uvs[i] : Vector2.zero;
                if (i > 0)
                {
                    result += "; ";
                }

                result +=
                    "#" +
                    i +
                    "(x=" +
                    FormatFloat(vertex.x) +
                    ", z=" +
                    FormatFloat(vertex.z) +
                    ", uv.x=" +
                    FormatFloat(uv.x) +
                    ", uv.y=" +
                    FormatFloat(uv.y) +
                    ")";
            }

            return result;
        }

        private string AvailabilityText()
        {
            return this.materialCache != null
                ? this.materialCache.Availability.ToString()
                : "material-cache-null";
        }

        private static string SkyfallerKey(Skyfaller skyfaller)
        {
            if (skyfaller == null)
            {
                return "null";
            }

            return skyfaller.GetType().Name + "#" + skyfaller.thingIDNumber;
        }

        private static string SafeId(string id)
        {
            return string.IsNullOrEmpty(id) ? "null" : id;
        }

        private static string MeshName(Mesh mesh)
        {
            if (mesh == null)
            {
                return "null";
            }

            return string.IsNullOrEmpty(mesh.name) ? "<unnamed>" : mesh.name;
        }

        private static string MaterialName(Material material)
        {
            if (material == null)
            {
                return "null";
            }

            return string.IsNullOrEmpty(material.name) ? "<unnamed>" : material.name;
        }

        private static string ShaderName(Material material)
        {
            if (material == null || material.shader == null)
            {
                return "null";
            }

            return string.IsNullOrEmpty(material.shader.name) ? "<unnamed>" : material.shader.name;
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("0.###");
        }

        private static string FormatVector(Vector3 value)
        {
            return "(" +
                FormatFloat(value.x) +
                ", " +
                FormatFloat(value.y) +
                ", " +
                FormatFloat(value.z) +
                ")";
        }
    }
}
