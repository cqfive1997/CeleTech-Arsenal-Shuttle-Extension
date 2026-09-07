using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx.Vapor
{
    internal sealed class ShuttleFlightVfxVaporFleckEmitter
    {
        internal static readonly ShuttleFlightVfxVaporFleckEmitter Shared =
            new ShuttleFlightVfxVaporFleckEmitter();

        private const string LogCategory = "ShuttleFlightVfxVapor";
        private const float MinEnvelopeIntensity = 0.02f;
        private const float FleckYOffset = 0.092f;
        private const int PruneAgeTicks = 900;
        private const int MaxTrackedSkyfallers = 96;

        private readonly Dictionary<int, int> lastEmitTickByAnchor =
            new Dictionary<int, int>();
        private readonly Dictionary<int, Vector3> lastRootWorldByAnchor =
            new Dictionary<int, Vector3>();

        private ShuttleFlightVfxVaporFleckEmitter()
        {
        }

        internal void Emit(
            Skyfaller skyfaller,
            Vector3 drawLoc,
            Rot4 drawRotation,
            float extraRotation,
            ShuttleFlightVfxMode mode,
            float envelopeIntensity,
            ShuttleVaporQualityProfile profile)
        {
            if (!this.CanEmit(skyfaller, envelopeIntensity, profile))
            {
                return;
            }

            Map map = skyfaller.Map;
            int tick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            this.PruneOldEntries(tick);

            Vector2 drawSize = ShuttleFlightVfxDrawSizeResolver.ResolveDrawSize(skyfaller);
            Quaternion rotation = ShuttleFlightVfxVaporAnchorFrameResolver.BuildRotation(
                skyfaller.Graphic,
                drawRotation,
                extraRotation);
            Vector3 origin = ShuttleFlightVfxVaporAnchorFrameResolver.ResolveOrigin(skyfaller, drawLoc, drawRotation);

            ShuttleFlightVfxVaporAnchor[] anchors = ShuttleFlightVfxVaporLayoutProvider.Anchors;
            for (int i = 0; i < anchors.Length; i++)
            {
                ShuttleFlightVfxVaporAnchor anchor = anchors[i];
                if (!ShuttleFlightVfxVaporLayoutProvider.IsFleckEmitterAnchor(anchor) ||
                    !ShuttleFlightVfxVaporDebugSettings.ShouldDrawKind(anchor.Kind) ||
                    !ShuttleFlightVfxVaporDebugSettings.ShouldDrawAnchorId(anchor.Id))
                {
                    continue;
                }

                int intervalTicks = this.ResolveEmitIntervalTicks(anchor, profile);
                int key = this.ResolveThrottleKey(skyfaller, anchor);
                int lastTick;
                int ticksSinceLastEmit = 1;
                if (this.lastEmitTickByAnchor.TryGetValue(key, out lastTick) &&
                    tick - lastTick < intervalTicks)
                {
                    continue;
                }

                if (this.lastEmitTickByAnchor.TryGetValue(key, out lastTick))
                {
                    ticksSinceLastEmit = Mathf.Max(1, tick - lastTick);
                }

                this.lastEmitTickByAnchor[key] = tick;

                float fleckStreamwiseOffset = this.ResolveFleckStreamwiseOffset(anchor);
                ShuttleFlightVfxVaporAnchorFrame frame =
                    ShuttleFlightVfxVaporAnchorFrameResolver.ResolveCalibratedFrame(
                        drawSize,
                        origin,
                        rotation,
                        anchor,
                        anchor.CenterOffsetLocal,
                        fleckStreamwiseOffset);
                Vector3 previousRootWorld;
                bool hasPreviousRoot = this.lastRootWorldByAnchor.TryGetValue(key, out previousRootWorld);
                float motionDistance = hasPreviousRoot
                    ? Vector3.Distance(previousRootWorld, frame.RootWorld)
                    : 0f;
                bool canBackfillMotion = hasPreviousRoot &&
                    motionDistance > 0.04f &&
                    motionDistance < 4f;
                this.EmitForAnchor(
                    map,
                    anchor,
                    frame,
                    previousRootWorld,
                    canBackfillMotion,
                    canBackfillMotion ? motionDistance : 0f,
                    ticksSinceLastEmit,
                    envelopeIntensity,
                    profile);
                this.lastRootWorldByAnchor[key] = frame.RootWorld;
            }
        }

        private bool CanEmit(
            Skyfaller skyfaller,
            float envelopeIntensity,
            ShuttleVaporQualityProfile profile)
        {
            return skyfaller != null &&
                skyfaller.Map != null &&
                skyfaller.Map.flecks != null &&
                skyfaller.Graphic != null &&
                profile.ShouldDraw &&
                envelopeIntensity > MinEnvelopeIntensity &&
                !ShuttleFlightVfxVaporDebugSettings.ShouldDrawCalibrationOverlay;
        }

        private void EmitForAnchor(
            Map map,
            ShuttleFlightVfxVaporAnchor anchor,
            ShuttleFlightVfxVaporAnchorFrame frame,
            Vector3 previousRootWorld,
            bool canBackfillMotion,
            float motionDistance,
            int ticksSinceLastEmit,
            float envelopeIntensity,
            ShuttleVaporQualityProfile profile)
        {
            FleckDef fleckDef = CMCFleckDefOf.CMC_ShuttleCondensationPuff;
            if (fleckDef == null)
            {
                ShuttleLog.WarnOnce(
                    LogCategory,
                    "condensation-fleckdef-fallback",
                    "CMC_ShuttleCondensationPuff FleckDef was not loaded; falling back to RimWorld AirPuff for shuttle vapor diagnostics.");
                fleckDef = FleckDefOf.AirPuff;
            }

            int fleckCount = this.ResolveFlecksPerAnchor(anchor, profile) +
                this.ResolveMotionFleckBonus(anchor, motionDistance);
            float trailLength = this.ResolveTrailLength(anchor, profile);
            float crossWidth = this.ResolveCrossWidth(anchor, profile);
            float qualityScale = this.ResolveScaleMultiplier(profile);
            float visibleEnvelope = envelopeIntensity > MinEnvelopeIntensity
                ? Mathf.Max(envelopeIntensity, 0.95f)
                : envelopeIntensity;
            float intensity = Mathf.Clamp01(visibleEnvelope * Mathf.Max(1.00f, profile.AlphaMultiplier));
            float scaleStart = this.ResolveScaleStart(anchor);
            float scaleEnd = this.ResolveScaleEnd(anchor);
            float alphaStart = this.ResolveAlphaStart(anchor);
            float alphaEnd = this.ResolveAlphaEnd(anchor);
            float alphaMin = this.ResolveAlphaClampMin(anchor);
            float alphaMax = this.ResolveAlphaClampMax(anchor);
            int rootFleckCount = this.ResolveRootFleckCount(anchor, fleckCount);
            int trailFleckCount = Mathf.Max(0, fleckCount - rootFleckCount);
            int bridgeFleckCount = canBackfillMotion
                ? this.ResolveBridgeFleckCount(anchor, motionDistance, ticksSinceLastEmit)
                : 0;

            for (int i = 0; i < fleckCount; i++)
            {
                bool rootBand = i < rootFleckCount;
                int bandIndex = rootBand ? i : i - rootFleckCount;
                int bandCount = rootBand ? rootFleckCount : trailFleckCount;
                float bandUnit = this.ResolveStratifiedUnit(bandIndex, bandCount);
                float t = this.ResolveTrailSample(anchor, rootBand, bandUnit);
                float localCrossWidth = rootBand ? crossWidth * 0.34f : crossWidth * 0.58f;
                float crossOffset = this.ResolveCenteredCrossOffset(localCrossWidth);
                float motionSample = this.ResolveMotionSample(rootBand, motionDistance, bandUnit);
                Vector3 baseRoot = canBackfillMotion
                    ? Vector3.Lerp(frame.RootWorld, previousRootWorld, motionSample)
                    : frame.RootWorld;
                Vector3 jitter = new Vector3(
                    Rand.Range(rootBand ? -0.018f : -0.014f, rootBand ? 0.018f : 0.014f),
                    0f,
                    Rand.Range(rootBand ? -0.018f : -0.014f, rootBand ? 0.018f : 0.014f));
                Vector3 pos =
                    baseRoot +
                    (frame.TangentWorld * (t * trailLength)) +
                    (frame.NormalWorld * crossOffset) +
                    jitter;
                pos.y += FleckYOffset + (rootBand ? 0.020f : 0f) + Rand.Range(-0.004f, 0.006f);

                IntVec3 cell = pos.ToIntVec3();
                if (!cell.InBounds(map))
                {
                    continue;
                }

                float scale = Mathf.Lerp(scaleStart, scaleEnd, t) *
                    (rootBand ? 1.45f : 1f) *
                    Rand.Range(0.88f, 1.14f) *
                    qualityScale;
                float alpha = Mathf.Lerp(alphaStart, alphaEnd, t) *
                    (rootBand ? 1.65f : 1f) *
                    intensity *
                    Rand.Range(0.88f, 1.12f);
                float localAlphaMax = rootBand ? this.ResolveRootAlphaClampMax(anchor) : alphaMax;
                Color color = new Color(0.92f, 0.98f, 1.00f, Mathf.Clamp(alpha, alphaMin, localAlphaMax));

                FleckCreationData data = FleckMaker.GetDataStatic(pos, map, fleckDef, scale);
                data.rotation = Rand.Range(0f, 360f);
                data.rotationRate = Rand.Range(-7f, 7f);
                Vector3 driftDirection = this.ResolveFleckDriftDirection(frame, rootBand);
                data.velocityAngle = this.AngleFromVector(driftDirection);
                data.velocitySpeed = Mathf.Lerp(
                    this.ResolveVelocityStart(anchor),
                    this.ResolveVelocityEnd(anchor),
                    t) *
                    (rootBand ? 0.45f : 1f) *
                    Mathf.Max(0.55f, profile.MotionMultiplier) *
                    Rand.Range(0.75f, 1.25f);
                data.instanceColor = color;
                map.flecks.CreateFleck(data);
            }

            for (int i = 0; i < bridgeFleckCount; i++)
            {
                float motionUnit = this.ResolveStratifiedUnit(i, bridgeFleckCount);
                int trailBands = anchor.Kind == ShuttleFlightVfxVaporKind.CurlPuff ? 4 : 5;
                float trailUnit = this.ResolveStratifiedUnit(i % trailBands, trailBands);
                float t = this.ResolveBridgeTrailSample(anchor, trailUnit);
                float crossOffset = this.ResolveCenteredCrossOffset(crossWidth * 0.42f);
                Vector3 baseRoot = Vector3.Lerp(frame.RootWorld, previousRootWorld, motionUnit);
                Vector3 jitter = new Vector3(
                    Rand.Range(-0.010f, 0.010f),
                    0f,
                    Rand.Range(-0.010f, 0.010f));
                Vector3 pos =
                    baseRoot +
                    (frame.TangentWorld * (t * trailLength)) +
                    (frame.NormalWorld * crossOffset) +
                    jitter;
                pos.y += FleckYOffset + 0.014f + Rand.Range(-0.003f, 0.005f);

                IntVec3 cell = pos.ToIntVec3();
                if (!cell.InBounds(map))
                {
                    continue;
                }

                float scale = Mathf.Lerp(scaleStart, scaleEnd, t) *
                    Rand.Range(0.78f, 1.04f) *
                    qualityScale;
                float alpha = Mathf.Lerp(alphaStart, alphaEnd, t) *
                    intensity *
                    Rand.Range(0.92f, 1.16f);
                Color color = new Color(0.92f, 0.98f, 1.00f, Mathf.Clamp(alpha, alphaMin, alphaMax));

                FleckCreationData data = FleckMaker.GetDataStatic(pos, map, fleckDef, scale);
                data.rotation = Rand.Range(0f, 360f);
                data.rotationRate = Rand.Range(-5f, 5f);
                Vector3 driftDirection =
                    (frame.TangentWorld * Rand.Range(0.24f, 0.48f)) +
                    (frame.NormalWorld * Rand.Range(-0.08f, 0.08f));
                data.velocityAngle = this.AngleFromVector(driftDirection);
                data.velocitySpeed = Mathf.Lerp(
                    this.ResolveVelocityStart(anchor),
                    this.ResolveVelocityEnd(anchor),
                    t) *
                    0.72f *
                    Mathf.Max(0.55f, profile.MotionMultiplier) *
                    Rand.Range(0.80f, 1.12f);
                data.instanceColor = color;
                map.flecks.CreateFleck(data);
            }
        }

        private int ResolveEmitIntervalTicks(
            ShuttleFlightVfxVaporAnchor anchor,
            ShuttleVaporQualityProfile profile)
        {
            if (anchor.Kind == ShuttleFlightVfxVaporKind.CurlPuff)
            {
                return 1;
            }

            return 1;
        }

        private int ResolveFlecksPerAnchor(
            ShuttleFlightVfxVaporAnchor anchor,
            ShuttleVaporQualityProfile profile)
        {
            if (anchor.Kind == ShuttleFlightVfxVaporKind.CurlPuff)
            {
                return Rand.RangeInclusive(8, 11);
            }

            return Rand.RangeInclusive(15, 20);
        }

        private float ResolveTrailLength(
            ShuttleFlightVfxVaporAnchor anchor,
            ShuttleVaporQualityProfile profile)
        {
            float effectiveLength = this.ResolveEffectiveLength(anchor, profile);
            if (anchor.Kind == ShuttleFlightVfxVaporKind.CurlPuff)
            {
                return Mathf.Clamp(effectiveLength * 0.28f, 0.55f, 1.05f);
            }

            return Mathf.Clamp(effectiveLength * 0.24f, 0.85f, 1.45f);
        }

        private float ResolveCrossWidth(
            ShuttleFlightVfxVaporAnchor anchor,
            ShuttleVaporQualityProfile profile)
        {
            float effectiveWidth = this.ResolveEffectiveWidth(anchor, profile);
            if (anchor.Kind == ShuttleFlightVfxVaporKind.CurlPuff)
            {
                return Mathf.Clamp(effectiveWidth * 0.24f, 0.08f, 0.18f);
            }

            return Mathf.Clamp(effectiveWidth * 0.22f, 0.10f, 0.22f);
        }

        private float ResolveEffectiveLength(
            ShuttleFlightVfxVaporAnchor anchor,
            ShuttleVaporQualityProfile profile)
        {
            float length =
                anchor.BaseSize.x *
                anchor.SizeMultiplier *
                anchor.LengthMultiplier *
                profile.SizeMultiplier;
            if (anchor.Kind == ShuttleFlightVfxVaporKind.ShoulderPuff)
            {
                length *= 0.55f;
            }
            else if (anchor.Kind == ShuttleFlightVfxVaporKind.CurlPuff)
            {
                length *= 0.75f;
            }

            return length;
        }

        private float ResolveEffectiveWidth(
            ShuttleFlightVfxVaporAnchor anchor,
            ShuttleVaporQualityProfile profile)
        {
            float width =
                anchor.BaseSize.y *
                anchor.SizeMultiplier *
                anchor.WidthMultiplier *
                profile.SizeMultiplier;
            if (anchor.Kind == ShuttleFlightVfxVaporKind.ShoulderPuff)
            {
                width *= 0.85f;
            }
            else if (anchor.Kind == ShuttleFlightVfxVaporKind.CurlPuff)
            {
                width *= 0.90f;
            }

            return width;
        }

        private float ResolveScaleStart(ShuttleFlightVfxVaporAnchor anchor)
        {
            return anchor.Kind == ShuttleFlightVfxVaporKind.CurlPuff ? 0.22f : 0.34f;
        }

        private float ResolveScaleEnd(ShuttleFlightVfxVaporAnchor anchor)
        {
            return anchor.Kind == ShuttleFlightVfxVaporKind.CurlPuff ? 0.40f : 0.56f;
        }

        private float ResolveAlphaStart(ShuttleFlightVfxVaporAnchor anchor)
        {
            return anchor.Kind == ShuttleFlightVfxVaporKind.CurlPuff ? 0.34f : 0.46f;
        }

        private float ResolveAlphaEnd(ShuttleFlightVfxVaporAnchor anchor)
        {
            return anchor.Kind == ShuttleFlightVfxVaporKind.CurlPuff ? 0.12f : 0.18f;
        }

        private float ResolveAlphaClampMin(ShuttleFlightVfxVaporAnchor anchor)
        {
            return anchor.Kind == ShuttleFlightVfxVaporKind.CurlPuff ? 0.05f : 0.08f;
        }

        private float ResolveAlphaClampMax(ShuttleFlightVfxVaporAnchor anchor)
        {
            return anchor.Kind == ShuttleFlightVfxVaporKind.CurlPuff ? 0.44f : 0.58f;
        }

        private float ResolveRootAlphaClampMax(ShuttleFlightVfxVaporAnchor anchor)
        {
            return anchor.Kind == ShuttleFlightVfxVaporKind.CurlPuff ? 0.64f : 0.82f;
        }

        private float ResolveVelocityStart(ShuttleFlightVfxVaporAnchor anchor)
        {
            return anchor.Kind == ShuttleFlightVfxVaporKind.CurlPuff ? 0.002f : 0.0025f;
        }

        private float ResolveVelocityEnd(ShuttleFlightVfxVaporAnchor anchor)
        {
            return anchor.Kind == ShuttleFlightVfxVaporKind.CurlPuff ? 0.010f : 0.015f;
        }

        private int ResolveRootFleckCount(
            ShuttleFlightVfxVaporAnchor anchor,
            int fleckCount)
        {
            int target = anchor.Kind == ShuttleFlightVfxVaporKind.CurlPuff ? 4 : 7;
            return Mathf.Clamp(target, 1, Mathf.Max(1, fleckCount - 1));
        }

        private int ResolveMotionFleckBonus(
            ShuttleFlightVfxVaporAnchor anchor,
            float motionDistance)
        {
            if (motionDistance <= 0.04f)
            {
                return 0;
            }

            int maxBonus = anchor.Kind == ShuttleFlightVfxVaporKind.CurlPuff ? 16 : 28;
            float density = anchor.Kind == ShuttleFlightVfxVaporKind.CurlPuff ? 10.0f : 17.0f;
            return Mathf.Clamp(Mathf.CeilToInt(motionDistance * density), 0, maxBonus);
        }

        private int ResolveBridgeFleckCount(
            ShuttleFlightVfxVaporAnchor anchor,
            float motionDistance,
            int ticksSinceLastEmit)
        {
            if (motionDistance <= 0.04f)
            {
                return 0;
            }

            float speedPerTick = motionDistance / Mathf.Max(1, ticksSinceLastEmit);
            float speedFactor = Mathf.Clamp01((speedPerTick - 0.08f) / 0.42f);
            float wideSpacing = anchor.Kind == ShuttleFlightVfxVaporKind.CurlPuff ? 0.070f : 0.060f;
            float tightSpacing = anchor.Kind == ShuttleFlightVfxVaporKind.CurlPuff ? 0.040f : 0.034f;
            float spacing = Mathf.Lerp(wideSpacing, tightSpacing, speedFactor);
            int maxCount = anchor.Kind == ShuttleFlightVfxVaporKind.CurlPuff ? 30 : 48;
            return Mathf.Clamp(Mathf.CeilToInt(motionDistance / Mathf.Max(0.01f, spacing)), 0, maxCount);
        }

        private float ResolveStratifiedUnit(
            int index,
            int count)
        {
            if (count <= 1)
            {
                return 0.5f;
            }

            float sample = (index + 0.5f + Rand.Range(-0.28f, 0.28f)) / count;
            return Mathf.Clamp01(sample);
        }

        private float ResolveTrailSample(
            ShuttleFlightVfxVaporAnchor anchor,
            bool rootBand,
            float bandUnit)
        {
            if (rootBand)
            {
                return Mathf.Clamp01(Mathf.Lerp(0f, 0.11f, bandUnit) + Rand.Range(-0.010f, 0.012f));
            }

            if (anchor.Kind == ShuttleFlightVfxVaporKind.CurlPuff)
            {
                return Mathf.Clamp01(Mathf.Lerp(0.08f, 0.68f, bandUnit) + Rand.Range(-0.018f, 0.018f));
            }

            return Mathf.Clamp01(Mathf.Lerp(0.10f, 0.82f, bandUnit) + Rand.Range(-0.018f, 0.018f));
        }

        private float ResolveBridgeTrailSample(
            ShuttleFlightVfxVaporAnchor anchor,
            float bandUnit)
        {
            if (anchor.Kind == ShuttleFlightVfxVaporKind.CurlPuff)
            {
                return Mathf.Clamp01(Mathf.Lerp(0.05f, 0.48f, bandUnit) + Rand.Range(-0.014f, 0.014f));
            }

            return Mathf.Clamp01(Mathf.Lerp(0.06f, 0.58f, bandUnit) + Rand.Range(-0.014f, 0.014f));
        }

        private float ResolveCenteredCrossOffset(float localCrossWidth)
        {
            float unit = Rand.Range(-1f, 1f);
            float centerBias = Rand.Value < 0.72f
                ? 0.42f
                : 1.0f;
            return unit * localCrossWidth * centerBias;
        }

        private float ResolveMotionSample(
            bool rootBand,
            float motionDistance,
            float bandUnit)
        {
            if (motionDistance <= 0.04f)
            {
                return 0f;
            }

            if (rootBand)
            {
                return Mathf.Clamp01((bandUnit * 0.46f) + Rand.Range(-0.035f, 0.035f));
            }

            return Mathf.Clamp01(bandUnit + Rand.Range(-0.055f, 0.055f));
        }

        private float ResolveFleckStreamwiseOffset(ShuttleFlightVfxVaporAnchor anchor)
        {
            if (anchor.Kind == ShuttleFlightVfxVaporKind.ShoulderPuff)
            {
                return anchor.StreamwiseOffsetLocal * 0.28f;
            }

            if (anchor.Kind == ShuttleFlightVfxVaporKind.CurlPuff)
            {
                return anchor.StreamwiseOffsetLocal * 0.45f;
            }

            return anchor.StreamwiseOffsetLocal;
        }

        private Vector3 ResolveFleckDriftDirection(
            ShuttleFlightVfxVaporAnchorFrame frame,
            bool rootBand)
        {
            Vector3 drift;
            if (rootBand)
            {
                drift =
                    (frame.NormalWorld * Rand.Range(-0.16f, 0.22f)) +
                    (frame.TangentWorld * Rand.Range(0.04f, 0.16f)) +
                    new Vector3(Rand.Range(-0.08f, 0.08f), 0f, Rand.Range(-0.08f, 0.08f));
            }
            else
            {
                drift =
                    (frame.TangentWorld * Rand.Range(0.58f, 1.00f)) +
                    (frame.NormalWorld * Rand.Range(-0.12f, 0.12f)) +
                    new Vector3(Rand.Range(-0.06f, 0.06f), 0f, Rand.Range(-0.06f, 0.06f));
            }

            drift.y = 0f;
            if (drift.sqrMagnitude <= 0.0001f)
            {
                return rootBand ? frame.NormalWorld : frame.TangentWorld;
            }

            drift.Normalize();
            return drift;
        }

        private int ResolveThrottleKey(
            Skyfaller skyfaller,
            ShuttleFlightVfxVaporAnchor anchor)
        {
            unchecked
            {
                int anchorHash = string.IsNullOrEmpty(anchor.Id) ? (int)anchor.Kind : anchor.Id.GetHashCode();
                return (skyfaller.thingIDNumber * 397) ^ anchorHash;
            }
        }

        private float ResolveScaleMultiplier(ShuttleVaporQualityProfile profile)
        {
            return 1f;
        }

        private float AngleFromVector(Vector3 direction)
        {
            Vector3 flat = direction;
            flat.y = 0f;
            if (flat.sqrMagnitude <= 0.0001f)
            {
                return 0f;
            }

            flat.Normalize();
            return Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
        }

        private void PruneOldEntries(int currentTick)
        {
            if (this.lastEmitTickByAnchor.Count <= MaxTrackedSkyfallers)
            {
                return;
            }

            List<int> removeKeys = null;
            foreach (KeyValuePair<int, int> entry in this.lastEmitTickByAnchor)
            {
                if (currentTick - entry.Value <= PruneAgeTicks)
                {
                    continue;
                }

                if (removeKeys == null)
                {
                    removeKeys = new List<int>();
                }

                removeKeys.Add(entry.Key);
            }

            if (removeKeys == null)
            {
                return;
            }

            for (int i = 0; i < removeKeys.Count; i++)
            {
                this.lastEmitTickByAnchor.Remove(removeKeys[i]);
                this.lastRootWorldByAnchor.Remove(removeKeys[i]);
            }
        }
    }
}
