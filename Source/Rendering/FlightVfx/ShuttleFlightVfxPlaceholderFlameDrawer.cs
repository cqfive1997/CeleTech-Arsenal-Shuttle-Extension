using RimWorld;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using CeleTech.ShuttleExtension.ModularShuttle.Rendering.CustomShaders;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx
{
    internal sealed class ShuttleFlightVfxPlaceholderFlameDrawer
    {
        internal static readonly ShuttleFlightVfxPlaceholderFlameDrawer Shared =
            new ShuttleFlightVfxPlaceholderFlameDrawer(
                ShuttleFlightVfxPlaceholderMaterialCache.Shared,
                ShuttleFlightVfxFlameMeshCache.Shared,
                ShuttleFlightVfxShaderMaterialCache.Shared,
                ShuttleFlightVfxIonParticleDrawer.Shared);

        private const float MinIntensity = 0.01f;
        private const float UnderHullYOffset = -0.02f;
        private const float OverHullYOffset = 0.04f;
        private const string LogCategory = "ShuttleFlightVfx";

        private readonly ShuttleFlightVfxPlaceholderMaterialCache materialCache;
        private readonly ShuttleFlightVfxFlameMeshCache meshCache;
        private readonly ShuttleFlightVfxShaderMaterialCache shaderMaterialCache;
        private readonly ShuttleFlightVfxIonParticleDrawer ionParticleDrawer;
        private readonly MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

        private ShuttleFlightVfxPlaceholderFlameDrawer(
            ShuttleFlightVfxPlaceholderMaterialCache materialCache,
            ShuttleFlightVfxFlameMeshCache meshCache,
            ShuttleFlightVfxShaderMaterialCache shaderMaterialCache,
            ShuttleFlightVfxIonParticleDrawer ionParticleDrawer)
        {
            this.materialCache = materialCache;
            this.meshCache = meshCache;
            this.shaderMaterialCache = shaderMaterialCache;
            this.ionParticleDrawer = ionParticleDrawer;
        }

        internal void DrawUnderHull(
            Skyfaller skyfaller,
            Vector3 drawLoc,
            Rot4 drawRotation,
            float extraRotation,
            ShuttleFlightVfxMode mode)
        {
            this.DrawLayer(skyfaller, drawLoc, drawRotation, extraRotation, mode, true);
        }

        internal void DrawOverHull(
            Skyfaller skyfaller,
            Vector3 drawLoc,
            Rot4 drawRotation,
            float extraRotation,
            ShuttleFlightVfxMode mode)
        {
            this.DrawLayer(skyfaller, drawLoc, drawRotation, extraRotation, mode, false);
        }

        private void DrawLayer(
            Skyfaller skyfaller,
            Vector3 drawLoc,
            Rot4 drawRotation,
            float extraRotation,
            ShuttleFlightVfxMode mode,
            bool underHull)
        {
            if (!this.CanDraw(skyfaller))
            {
                return;
            }

            ShuttleFlightVfxState state = ShuttleFlightVfxProgress.Evaluate(skyfaller, mode);
            ShuttleThrusterLayout layout = ShuttleKunPengThrusterLayoutProvider.Layout;
            this.LogLayoutCountsOnce(layout, mode);
            Vector2 drawSize = ShuttleFlightVfxDrawSizeResolver.ResolveDrawSize(skyfaller);
            Vector3 origin = drawLoc + skyfaller.Graphic.DrawOffset(drawRotation);

            for (int i = 0; i < layout.Count; i++)
            {
                ShuttleThrusterAnchor anchor = layout.GetAnchor(i);
                if (this.ShouldDrawAnchor(anchor.Kind, underHull))
                {
                    this.DrawAnchor(
                        anchor,
                        state,
                        drawSize,
                        origin,
                        drawRotation,
                        extraRotation,
                        mode,
                        underHull);
                }
            }
        }

        private bool CanDraw(Skyfaller skyfaller)
        {
            // Flame VFX is a production visual path. ShouldDrawPrototypeVfx gates
            // experimental non-production layers such as vapor/airflow, while
            // debug anchors remain separately controlled.
            return skyfaller != null &&
                skyfaller.Graphic != null &&
                this.materialCache != null &&
                this.meshCache != null &&
                ShuttleKunPengThrusterLayoutProvider.Layout != null;
        }

        private bool ShouldDrawAnchor(ShuttleThrusterKind kind, bool underHull)
        {
            if (underHull)
            {
                return kind == ShuttleThrusterKind.RearVtol ||
                    kind == ShuttleThrusterKind.BellyVtol;
            }

            return kind == ShuttleThrusterKind.TailMain;
        }

        private void DrawAnchor(
            ShuttleThrusterAnchor anchor,
            ShuttleFlightVfxState state,
            Vector2 drawSize,
            Vector3 origin,
            Rot4 drawRotation,
            float extraRotation,
            ShuttleFlightVfxMode mode,
            bool underHull)
        {
            bool forceVisible = this.ShouldForceVisibleAnchor(anchor, underHull);
            float intensity = this.GetIntensity(anchor.Kind, state);
            if (!forceVisible && intensity <= MinIntensity)
            {
                this.LogDrawDecisionOnce(anchor, mode, underHull, "skip:intensity-below-min", intensity, 0f, 0f, 0f, 0f);
                return;
            }

            ShuttleFlightVfxFlameGeometry geometry = ShuttleFlightVfxFlameGeometry.ForKind(anchor.Kind);
            float intensityScale = this.ResolveAnchorIntensityScale(anchor);
            float dynamicIntensityScale = this.ResolveDynamicIntensityScale(anchor, state);
            intensity = forceVisible ? 1f : intensity * intensityScale * dynamicIntensityScale;
            if (!forceVisible)
            {
                intensity = this.ApplyIncomingForwardBellyVisibilityFloor(
                    anchor,
                    state,
                    mode,
                    intensity);
            }

            if (!forceVisible && intensity <= MinIntensity)
            {
                this.LogDrawDecisionOnce(anchor, mode, underHull, "skip:scaled-intensity-below-min", intensity, 0f, 0f, 0f, 0f);
                return;
            }

            float lengthScale = this.GetLengthScale(anchor.Kind, state) *
                this.ResolveAnchorLengthScale(anchor);
            float length = forceVisible ? 0.8f : geometry.BaseLength * lengthScale;
            length *= this.ResolveDirectionalLengthScale(anchor, drawRotation);
            float widthScale = this.GetWidthScale(anchor.Kind, state) *
                this.ResolveDynamicWidthScale(anchor, state);
            if (forceVisible)
            {
                widthScale = 0.25f / Mathf.Max(0.001f, geometry.BaseWidth);
            }

            ShuttleFlightVfxThrusterVisualTuning tuning =
                ShuttleFlightVfxThrusterVisualTuningProvider.ForAnchor(anchor);
            if (!forceVisible && tuning.MinVisibleLength > 0f)
            {
                length = Mathf.Max(length, tuning.MinVisibleLength);
            }

            this.DrawFlame(
                anchor,
                tuning,
                drawSize,
                origin,
                drawRotation,
                extraRotation,
                underHull,
                intensity,
                length,
                geometry.BaseWidth,
                lengthScale,
                widthScale,
                mode,
                forceVisible);
        }

        private void DrawFlame(
            ShuttleThrusterAnchor anchor,
            ShuttleFlightVfxThrusterVisualTuning tuning,
            Vector2 drawSize,
            Vector3 origin,
            Rot4 drawRotation,
            float extraRotation,
            bool underHull,
            float intensity,
            float length,
            float baseWidth,
            float lengthScale,
            float stateWidthScale,
            ShuttleFlightVfxMode mode,
            bool forceVisible)
        {
            Vector3 worldDirection = ShuttleFlightVfxAnchorMapper.ResolveAnchorWorldDirection(
                anchor,
                drawRotation,
                extraRotation);
            if (worldDirection.sqrMagnitude <= 0.0001f || length <= 0f)
            {
                this.LogDrawDecisionOnce(
                    anchor,
                    mode,
                    underHull,
                    worldDirection.sqrMagnitude <= 0.0001f ? "skip:world-direction-zero" : "skip:length-nonpositive",
                    intensity,
                    lengthScale,
                    stateWidthScale,
                    length,
                    0f);
                return;
            }

            worldDirection.Normalize();
            Vector3 anchorWorld = origin +
                ShuttleFlightVfxAnchorMapper.ResolveAnchorWorldOffset(
                    anchor,
                    drawSize,
                    drawRotation,
                    extraRotation);
            Vector3 visualOffset = new Vector3(
                tuning.VisualOffset.x,
                0f,
                tuning.VisualOffset.y);
            if (anchor.Kind == ShuttleThrusterKind.TailMain &&
                ShuttleFlightVfxAnchorMapper.UsesDirectionalTextureUv(anchor, drawRotation))
            {
                visualOffset = Vector3.zero;
            }

            anchorWorld += ShuttleFlightVfxAnchorMapper.ResolveAuthoredWorldOffset(
                anchor,
                visualOffset,
                drawRotation,
                extraRotation);
            if (tuning.VisualOriginInset != 0f)
            {
                anchorWorld -= worldDirection * tuning.VisualOriginInset;
            }

            if (tuning.VisibleStartOffset != 0f)
            {
                anchorWorld += worldDirection * tuning.VisibleStartOffset;
            }

            float widthScale = Mathf.Max(0.05f, stateWidthScale);
            float width = baseWidth * widthScale;
            if (!forceVisible && tuning.MinVisibleWidth > 0f)
            {
                width = Mathf.Max(width, tuning.MinVisibleWidth);
            }

            if (width <= 0f)
            {
                this.LogDrawDecisionOnce(anchor, mode, underHull, "skip:width-nonpositive", intensity, lengthScale, widthScale, length, width);
                return;
            }

            Quaternion meshRotation = Quaternion.LookRotation(worldDirection, Vector3.up);
            if (tuning.VisualYawOffsetDegrees != 0f)
            {
                meshRotation *= Quaternion.Euler(0f, tuning.VisualYawOffsetDegrees, 0f);
            }

            if (this.TryDrawShaderFlame(
                    anchor,
                    tuning.VisualMode,
                    anchor.Kind,
                    anchorWorld,
                    worldDirection,
                    meshRotation,
                    underHull,
                    intensity,
                    length,
                    width,
                    lengthScale,
                    widthScale,
                    mode,
                    forceVisible))
            {
                return;
            }

            this.DrawPlaceholderFlame(
                anchor.Kind,
                tuning.VisualMode,
                anchorWorld,
                worldDirection,
                meshRotation,
                underHull,
                length,
                width,
                anchor,
                mode,
                forceVisible);
        }

        private void DrawPlaceholderFlame(
            ShuttleThrusterKind kind,
            ShuttleFlightVfxThrusterVisualMode visualMode,
            Vector3 anchorWorld,
            Vector3 worldDirection,
            Quaternion meshRotation,
            bool underHull,
            float length,
            float width,
            ShuttleThrusterAnchor anchor,
            ShuttleFlightVfxMode mode,
            bool forceVisible)
        {
            float glowLengthMultiplier;
            float glowWidthMultiplier;
            this.ResolveLayerMultipliers(kind, visualMode, true, out glowLengthMultiplier, out glowWidthMultiplier);
            this.DrawPlaceholderFlameLayer(
                anchorWorld,
                worldDirection,
                meshRotation,
                underHull,
                length * glowLengthMultiplier,
                width * glowWidthMultiplier,
                this.materialCache.GetGlowMaterial(kind),
                kind,
                true,
                anchor,
                mode,
                forceVisible);

            float coreLengthMultiplier;
            float coreWidthMultiplier;
            this.ResolveLayerMultipliers(kind, visualMode, false, out coreLengthMultiplier, out coreWidthMultiplier);
            this.DrawPlaceholderFlameLayer(
                anchorWorld,
                worldDirection,
                meshRotation,
                underHull,
                length * coreLengthMultiplier,
                width * coreWidthMultiplier,
                this.materialCache.GetCoreMaterial(kind),
                kind,
                false,
                anchor,
                mode,
                forceVisible);

            this.LogDrawDecisionOnce(
                anchor,
                mode,
                underHull,
                forceVisible ? "draw:fallback-force-visible" : "draw:fallback",
                1f,
                0f,
                0f,
                length,
                width);
        }

        private bool TryDrawShaderFlame(
            ShuttleThrusterAnchor anchor,
            ShuttleFlightVfxThrusterVisualMode visualMode,
            ShuttleThrusterKind kind,
            Vector3 anchorWorld,
            Vector3 worldDirection,
            Quaternion meshRotation,
            bool underHull,
            float intensity,
            float length,
            float width,
            float lengthScale,
            float widthScale,
            ShuttleFlightVfxMode mode,
            bool forceVisible)
        {
            if (!ShuttleFlightVfxShaderAvailability.IsAvailable || this.shaderMaterialCache == null)
            {
                this.LogDrawDecisionOnce(anchor, mode, underHull, "shader-fallback:shader-unavailable", intensity, lengthScale, widthScale, length, width);
                return false;
            }

            Material glowMaterial = this.shaderMaterialCache.GetGlowMaterial(kind);
            Material coreMaterial = this.shaderMaterialCache.GetCoreMaterial(kind);
            if (!this.IsUsableMaterial(glowMaterial) || !this.IsUsableMaterial(coreMaterial))
            {
                this.LogDrawDecisionOnce(anchor, mode, underHull, "shader-fallback:material-invalid", intensity, lengthScale, widthScale, length, width);
                return false;
            }

            if (kind == ShuttleThrusterKind.TailMain)
            {
                this.DrawShaderTailMainHalo(
                    anchorWorld,
                    worldDirection,
                    meshRotation,
                    underHull,
                    length,
                    width,
                    lengthScale,
                    widthScale,
                    glowMaterial,
                    intensity,
                    anchor,
                    mode,
                    forceVisible);
            }

            float glowLengthMultiplier;
            float glowWidthMultiplier;
            this.ResolveShaderLayerMultipliers(kind, visualMode, true, out glowLengthMultiplier, out glowWidthMultiplier);
            this.DrawShaderFlameLayer(
                anchorWorld,
                worldDirection,
                meshRotation,
                underHull,
                length * glowLengthMultiplier,
                width * glowWidthMultiplier,
                lengthScale * glowLengthMultiplier,
                widthScale * glowWidthMultiplier,
                glowMaterial,
                kind,
                intensity,
                anchor,
                true,
                mode,
                forceVisible);

            if (this.ionParticleDrawer != null)
            {
                this.ionParticleDrawer.DrawOuterParticles(
                    anchor,
                    anchorWorld,
                    worldDirection,
                    meshRotation,
                    underHull,
                    intensity,
                    length,
                    width);
            }

            float coreLengthMultiplier;
            float coreWidthMultiplier;
            this.ResolveShaderLayerMultipliers(kind, visualMode, false, out coreLengthMultiplier, out coreWidthMultiplier);
            this.DrawShaderFlameLayer(
                anchorWorld,
                worldDirection,
                meshRotation,
                underHull,
                length * coreLengthMultiplier,
                width * coreWidthMultiplier,
                lengthScale * coreLengthMultiplier,
                widthScale * coreWidthMultiplier,
                coreMaterial,
                kind,
                intensity,
                anchor,
                false,
                mode,
                forceVisible);

            if (this.ionParticleDrawer != null)
            {
                this.ionParticleDrawer.DrawFilaments(
                    anchor,
                    anchorWorld,
                    worldDirection,
                    meshRotation,
                    underHull,
                    intensity,
                    length,
                    width);
            }

            this.LogDrawDecisionOnce(
                anchor,
                mode,
                underHull,
                forceVisible ? "draw:shader-force-visible" : "draw:shader",
                intensity,
                lengthScale,
                widthScale,
                length,
                width);

            return true;
        }

        private void DrawShaderTailMainHalo(
            Vector3 anchorWorld,
            Vector3 worldDirection,
            Quaternion meshRotation,
            bool underHull,
            float length,
            float width,
            float lengthScale,
            float widthScale,
            Material material,
            float intensity,
            ShuttleThrusterAnchor anchor,
            ShuttleFlightVfxMode mode,
            bool forceVisible)
        {
            this.DrawShaderFlameLayer(
                anchorWorld,
                worldDirection,
                meshRotation,
                underHull,
                length * 0.46f,
                width * 1.58f,
                lengthScale * 0.46f,
                widthScale * 1.58f,
                material,
                ShuttleThrusterKind.TailMain,
                intensity * 0.24f,
                anchor,
                true,
                mode,
                forceVisible);
        }

        private void DrawShaderFlameLayer(
            Vector3 anchorWorld,
            Vector3 worldDirection,
            Quaternion meshRotation,
            bool underHull,
            float length,
            float width,
            float lengthScale,
            float widthScale,
            Material material,
            ShuttleThrusterKind kind,
            float intensity,
            ShuttleThrusterAnchor anchor,
            bool glowLayer,
            ShuttleFlightVfxMode mode,
            bool forceVisible)
        {
            if (!this.IsUsableMaterial(material) || length <= 0f || width <= 0f)
            {
                this.LogDrawDecisionOnce(
                    anchor,
                    mode,
                    underHull,
                    !this.IsUsableMaterial(material) ? "skip:shader-layer-material-invalid" : "skip:shader-layer-size-invalid",
                    intensity,
                    lengthScale,
                    widthScale,
                    length,
                    width);
                return;
            }

            Mesh mesh = this.meshCache.GetMesh(kind, glowLayer);
            if (mesh == null)
            {
                this.LogDrawDecisionOnce(anchor, mode, underHull, "skip:shader-mesh-null", intensity, lengthScale, widthScale, length, width);
                return;
            }

            Vector3 flameCenter = anchorWorld + (worldDirection * length * 0.5f);
            flameCenter.y += forceVisible ? 0.20f : (underHull ? UnderHullYOffset : OverHullYOffset);

            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(flameCenter, meshRotation, new Vector3(width, 1f, length));

            ShuttleFlightVfxFlameShaderParams parameters =
                ShuttleFlightVfxShaderParamResolver.Resolve(
                    anchor,
                    kind,
                    intensity,
                    lengthScale,
                    widthScale);
            this.ApplyShaderParams(parameters);
            Graphics.DrawMesh(mesh, matrix, material, 0, null, 0, this.propertyBlock);
        }

        private void ApplyShaderParams(ShuttleFlightVfxFlameShaderParams parameters)
        {
            this.propertyBlock.Clear();
            this.propertyBlock.SetFloat(ShuttleFlightVfxFlameShaderPropertyIds.Intensity, parameters.Intensity);
            this.propertyBlock.SetFloat(ShuttleFlightVfxFlameShaderPropertyIds.LengthScale, parameters.LengthScale);
            this.propertyBlock.SetFloat(ShuttleFlightVfxFlameShaderPropertyIds.WidthScale, parameters.WidthScale);
            this.propertyBlock.SetFloat(ShuttleFlightVfxFlameShaderPropertyIds.TimePhase, parameters.TimePhase);
            this.propertyBlock.SetFloat(ShuttleFlightVfxFlameShaderPropertyIds.NoiseScroll, parameters.NoiseScroll);
            this.propertyBlock.SetFloat(ShuttleFlightVfxFlameShaderPropertyIds.EdgeSoftness, parameters.EdgeSoftness);
            this.propertyBlock.SetFloat(ShuttleFlightVfxFlameShaderPropertyIds.TipFade, parameters.TipFade);
            this.propertyBlock.SetFloat(ShuttleFlightVfxFlameShaderPropertyIds.RingStrength, parameters.RingStrength);
            this.propertyBlock.SetFloat(ShuttleFlightVfxFlameShaderPropertyIds.RingFrequency, parameters.RingFrequency);
            this.propertyBlock.SetFloat(ShuttleFlightVfxFlameShaderPropertyIds.FlickerStrength, parameters.FlickerStrength);
            this.propertyBlock.SetFloat(ShuttleFlightVfxFlameShaderPropertyIds.FlickerSpeed, parameters.FlickerSpeed);
            this.propertyBlock.SetFloat(ShuttleFlightVfxFlameShaderPropertyIds.PhaseOffset, parameters.PhaseOffset);
            this.propertyBlock.SetFloat(ShuttleFlightVfxFlameShaderPropertyIds.CylinderEnd, parameters.CylinderEnd);
            this.propertyBlock.SetFloat(ShuttleFlightVfxFlameShaderPropertyIds.TipRadius, parameters.TipRadius);
            this.propertyBlock.SetFloat(ShuttleFlightVfxFlameShaderPropertyIds.EdgeNoiseStrength, parameters.EdgeNoiseStrength);
            this.propertyBlock.SetFloat(ShuttleFlightVfxFlameShaderPropertyIds.FlowSpeed, parameters.FlowSpeed);
            this.propertyBlock.SetFloat(ShuttleFlightVfxFlameShaderPropertyIds.FlowScale, parameters.FlowScale);
            this.propertyBlock.SetFloat(ShuttleFlightVfxFlameShaderPropertyIds.FilamentStrength, parameters.FilamentStrength);
            this.propertyBlock.SetFloat(ShuttleFlightVfxFlameShaderPropertyIds.PulseStrength, parameters.PulseStrength);
            this.propertyBlock.SetColor(ShuttleFlightVfxFlameShaderPropertyIds.CoreColor, parameters.CoreColor);
            this.propertyBlock.SetColor(ShuttleFlightVfxFlameShaderPropertyIds.OuterColor, parameters.OuterColor);
            this.propertyBlock.SetColor(ShuttleFlightVfxFlameShaderPropertyIds.HaloColor, parameters.HaloColor);
        }

        private void DrawPlaceholderFlameLayer(
            Vector3 anchorWorld,
            Vector3 worldDirection,
            Quaternion meshRotation,
            bool underHull,
            float length,
            float width,
            Material material,
            ShuttleThrusterKind kind,
            bool glowLayer,
            ShuttleThrusterAnchor anchor,
            ShuttleFlightVfxMode mode,
            bool forceVisible)
        {
            if (material == null || material == BaseContent.BadMat || length <= 0f || width <= 0f)
            {
                this.LogDrawDecisionOnce(
                    anchor,
                    mode,
                    underHull,
                    material == null || material == BaseContent.BadMat ? "skip:fallback-material-invalid" : "skip:fallback-layer-size-invalid",
                    1f,
                    0f,
                    0f,
                    length,
                    width);
                return;
            }

            Mesh mesh = this.meshCache.GetMesh(kind, glowLayer);
            if (mesh == null)
            {
                this.LogDrawDecisionOnce(anchor, mode, underHull, "skip:fallback-mesh-null", 1f, 0f, 0f, length, width);
                return;
            }

            Vector3 flameCenter = anchorWorld + (worldDirection * length * 0.5f);
            flameCenter.y += forceVisible ? 0.20f : (underHull ? UnderHullYOffset : OverHullYOffset);

            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(flameCenter, meshRotation, new Vector3(width, 1f, length));
            Graphics.DrawMesh(mesh, matrix, material, 0);
        }

        private bool IsUsableMaterial(Material material)
        {
            return UnityCustomShaderMaterialValidator.Validate(
                material,
                ShuttleCustomShaderMaterialContracts.Flame).IsValid;
        }

        private bool ShouldForceVisibleAnchor(ShuttleThrusterAnchor anchor, bool underHull)
        {
            return ShuttleFlightVfxDebugSettings.ShouldForceVisibleThrusters &&
                underHull &&
                (anchor.Kind == ShuttleThrusterKind.RearVtol ||
                    anchor.Kind == ShuttleThrusterKind.BellyVtol);
        }

        private void LogLayoutCountsOnce(ShuttleThrusterLayout layout, ShuttleFlightVfxMode mode)
        {
            if (!ShuttleFlightVfxDebugSettings.ShouldLogThrusterLayoutCounts || layout == null)
            {
                return;
            }

            int tailMainCount = 0;
            int rearVtolCount = 0;
            int bellyVtolCount = 0;
            int airflowReferenceCount = 0;
            for (int i = 0; i < layout.Count; i++)
            {
                ShuttleThrusterAnchor anchor = layout.GetAnchor(i);
                if (anchor.Kind == ShuttleThrusterKind.TailMain)
                {
                    tailMainCount++;
                }
                else if (anchor.Kind == ShuttleThrusterKind.RearVtol)
                {
                    rearVtolCount++;
                }
                else if (anchor.Kind == ShuttleThrusterKind.BellyVtol)
                {
                    bellyVtolCount++;
                }
                else if (anchor.Kind == ShuttleThrusterKind.AirflowReference)
                {
                    airflowReferenceCount++;
                }
            }

            ShuttleLog.WarnOnce(
                LogCategory,
                "thruster-layout-counts|" + mode,
                "Flight VFX thruster layout counts. mode=" +
                mode +
                " TailMain=" +
                tailMainCount +
                " RearVtol=" +
                rearVtolCount +
                " BellyVtol=" +
                bellyVtolCount +
                " AirflowReference=" +
                airflowReferenceCount +
                " expected TailMain=2 RearVtol=2 BellyVtol=2.");
        }

        private void LogDrawDecisionOnce(
            ShuttleThrusterAnchor anchor,
            ShuttleFlightVfxMode mode,
            bool underHull,
            string decision,
            float intensity,
            float lengthScale,
            float widthScale,
            float length,
            float width)
        {
            if (!ShuttleFlightVfxDebugSettings.ShouldLogThrusterDrawDecisions)
            {
                return;
            }

            string id = string.IsNullOrEmpty(anchor.Id) ? "null" : anchor.Id;
            string key = "thruster-draw|" +
                mode +
                "|" +
                underHull +
                "|" +
                id +
                "|" +
                decision;
            ShuttleLog.WarnOnce(
                LogCategory,
                key,
                "Flight VFX thruster draw decision. decision=" +
                decision +
                " id=" +
                id +
                " kind=" +
                anchor.Kind +
                " mode=" +
                mode +
                " underHull=" +
                underHull +
                " intensity=" +
                intensity.ToString("0.###") +
                " lengthScale=" +
                lengthScale.ToString("0.###") +
                " widthScale=" +
                widthScale.ToString("0.###") +
                " length=" +
                length.ToString("0.###") +
                " width=" +
                width.ToString("0.###"));
        }

        private void ResolveShaderLayerMultipliers(
            ShuttleThrusterKind kind,
            ShuttleFlightVfxThrusterVisualMode visualMode,
            bool glow,
            out float lengthMultiplier,
            out float widthMultiplier)
        {
            bool tailMain = visualMode == ShuttleFlightVfxThrusterVisualMode.TailMain;
            bool bottomParallel = this.IsBottomParallelVisualMode(visualMode);
            bool forwardBelly = kind == ShuttleThrusterKind.BellyVtol;
            if (glow)
            {
                lengthMultiplier = tailMain ? 1.30f : (bottomParallel ? (forwardBelly ? 1.30f : 1.18f) : 0.98f);
                widthMultiplier = tailMain ? 1.48f : (bottomParallel ? (forwardBelly ? 1.34f : 1.26f) : 1.28f);
                return;
            }

            lengthMultiplier = tailMain ? 0.90f : (bottomParallel ? (forwardBelly ? 0.88f : 0.78f) : 0.58f);
            widthMultiplier = tailMain ? 0.38f : (bottomParallel ? (forwardBelly ? 0.50f : 0.46f) : 0.40f);
        }

        private float ResolveAnchorIntensityScale(ShuttleThrusterAnchor anchor)
        {
            if (anchor.Kind == ShuttleThrusterKind.TailMain || string.IsNullOrEmpty(anchor.Id))
            {
                return 1f;
            }

            if (anchor.Id == "RearVtolLeft")
            {
                return 1.07f;
            }

            if (anchor.Id == "RearVtolRight")
            {
                return 0.96f;
            }

            if (anchor.Id == "BellyVtolLeft")
            {
                return 1.08f;
            }

            if (anchor.Id == "BellyVtolRight")
            {
                return 1.12f;
            }

            return 1f;
        }

        private float ResolveDynamicIntensityScale(
            ShuttleThrusterAnchor anchor,
            ShuttleFlightVfxState state)
        {
            if (anchor.Kind == ShuttleThrusterKind.TailMain)
            {
                return 1f;
            }

            float stabilizerPulse = this.ResolveAnchorPulse(anchor, state.StabilizerPulse, 0.10f, 0.33f);
            float touchdownBoost = state.TouchdownFlare * 0.16f;
            float flickerBoost = state.FlickerBoost * 0.08f;
            float bellyLandingBoost = anchor.Kind == ShuttleThrusterKind.BellyVtol
                ? (state.GroundCoupling * 0.24f) + (state.TouchdownFlare * 0.16f)
                : 0f;
            return 1f + stabilizerPulse + touchdownBoost + flickerBoost + bellyLandingBoost;
        }

        private float ResolveDynamicWidthScale(
            ShuttleThrusterAnchor anchor,
            ShuttleFlightVfxState state)
        {
            if (anchor.Kind == ShuttleThrusterKind.TailMain)
            {
                return 1f;
            }

            float stabilizerPulse = this.ResolveAnchorPulse(anchor, state.StabilizerPulse, 0.04f, 0.27f);
            float touchdownWidth = state.TouchdownFlare * 0.10f;
            float bellyLandingWidth = anchor.Kind == ShuttleThrusterKind.BellyVtol
                ? state.GroundCoupling * 0.10f
                : 0f;
            return 1f + stabilizerPulse + touchdownWidth + bellyLandingWidth;
        }

        private float ApplyIncomingForwardBellyVisibilityFloor(
            ShuttleThrusterAnchor anchor,
            ShuttleFlightVfxState state,
            ShuttleFlightVfxMode mode,
            float intensity)
        {
            if (mode != ShuttleFlightVfxMode.Incoming ||
                anchor.Kind != ShuttleThrusterKind.BellyVtol ||
                state.BellyVtolIntensity < 0.30f)
            {
                return intensity;
            }

            float floor = 0.35f;
            if (state.BellyVtolIntensity >= 0.45f)
            {
                floor = 0.45f;
            }

            if (state.BellyVtolIntensity >= 0.65f)
            {
                floor = 0.65f;
            }

            if (state.TouchdownFlare > 0f)
            {
                floor = Mathf.Max(floor, Mathf.Lerp(0.80f, 1.00f, state.TouchdownFlare));
            }

            return Mathf.Max(intensity, floor);
        }

        private float ResolveAnchorPulse(
            ShuttleThrusterAnchor anchor,
            float pulse,
            float amplitude,
            float speed)
        {
            if (pulse <= 0f)
            {
                return 0f;
            }

            float ticks = Find.TickManager != null ? Find.TickManager.TicksGame : 0f;
            float phase = this.DeterministicAnchorPhase(anchor);
            float wave = 0.5f + (0.5f * Mathf.Sin((ticks * speed) + phase));
            return pulse * amplitude * wave;
        }

        private float ResolveAnchorLengthScale(ShuttleThrusterAnchor anchor)
        {
            if (anchor.Kind == ShuttleThrusterKind.TailMain || string.IsNullOrEmpty(anchor.Id))
            {
                return 1f;
            }

            if (anchor.Id == "RearVtolLeft")
            {
                return 1.20f;
            }

            if (anchor.Id == "RearVtolRight")
            {
                return 1.16f;
            }

            if (anchor.Id == "BellyVtolLeft")
            {
                return 1.20f;
            }

            if (anchor.Id == "BellyVtolRight")
            {
                return 1.24f;
            }

            return 1f;
        }

        private float ResolveDirectionalLengthScale(
            ShuttleThrusterAnchor anchor,
            Rot4 drawRotation)
        {
            if (anchor.Kind == ShuttleThrusterKind.TailMain &&
                drawRotation == Rot4.South)
            {
                return 0.86f;
            }

            return 1f;
        }

        private float GetWidthScale(ShuttleThrusterKind kind, ShuttleFlightVfxState state)
        {
            if (kind == ShuttleThrusterKind.TailMain)
            {
                return state.TailMainWidthScale;
            }

            if (kind == ShuttleThrusterKind.RearVtol)
            {
                return state.RearVtolWidthScale;
            }

            return state.BellyVtolWidthScale;
        }

        private float DeterministicAnchorPhase(ShuttleThrusterAnchor anchor)
        {
            unchecked
            {
                int hash = 23;
                string id = anchor.Id;
                if (!string.IsNullOrEmpty(id))
                {
                    for (int i = 0; i < id.Length; i++)
                    {
                        hash = (hash * 31) + id[i];
                    }
                }

                hash = (hash * 31) + Mathf.RoundToInt(anchor.TextureUv.x * 10000f);
                hash = (hash * 31) + Mathf.RoundToInt(anchor.TextureUv.y * 10000f);
                return ((hash & 0x7fffffff) / 2147483647f) * 6.283185f;
            }
        }

        private void ResolveLayerMultipliers(
            ShuttleThrusterKind kind,
            ShuttleFlightVfxThrusterVisualMode visualMode,
            bool glow,
            out float lengthMultiplier,
            out float widthMultiplier)
        {
            bool tailMain = visualMode == ShuttleFlightVfxThrusterVisualMode.TailMain;
            bool bottomParallel = this.IsBottomParallelVisualMode(visualMode);
            bool forwardBelly = kind == ShuttleThrusterKind.BellyVtol;
            if (glow)
            {
                lengthMultiplier = tailMain ? 1.30f : (bottomParallel ? (forwardBelly ? 1.30f : 1.18f) : 1.08f);
                widthMultiplier = tailMain ? 1.95f : (bottomParallel ? (forwardBelly ? 1.40f : 1.32f) : 1.65f);
                return;
            }

            lengthMultiplier = tailMain ? 0.90f : (bottomParallel ? (forwardBelly ? 0.88f : 0.78f) : 0.62f);
            widthMultiplier = tailMain ? 0.52f : (bottomParallel ? (forwardBelly ? 0.58f : 0.52f) : 0.56f);
        }

        private bool IsBottomParallelVisualMode(
            ShuttleFlightVfxThrusterVisualMode visualMode)
        {
            return visualMode == ShuttleFlightVfxThrusterVisualMode.BellyParallel ||
                visualMode == ShuttleFlightVfxThrusterVisualMode.BellyVtol;
        }

        private float GetIntensity(ShuttleThrusterKind kind, ShuttleFlightVfxState state)
        {
            if (kind == ShuttleThrusterKind.TailMain)
            {
                return state.TailMainIntensity;
            }

            if (kind == ShuttleThrusterKind.RearVtol)
            {
                return state.RearVtolIntensity;
            }

            return state.BellyVtolIntensity;
        }

        private float GetLengthScale(ShuttleThrusterKind kind, ShuttleFlightVfxState state)
        {
            if (kind == ShuttleThrusterKind.TailMain)
            {
                return state.TailMainLengthScale;
            }

            if (kind == ShuttleThrusterKind.RearVtol)
            {
                return state.RearVtolLengthScale;
            }

            return state.BellyVtolLengthScale;
        }
    }
}
