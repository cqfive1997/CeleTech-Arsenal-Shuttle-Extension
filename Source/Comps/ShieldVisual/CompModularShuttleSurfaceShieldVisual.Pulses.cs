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
    public sealed partial class CompModularShuttleSurfaceShieldVisual
    {

        private void NotifyShieldHit(
            Vector3 worldPosition,
            float strength,
            float radius,
            bool brokeShield,
            string damageCategory,
            float shieldStrengthAtHit,
            bool wasLowShieldAtHit)
        {
            this.EnsurePulseBuffer();
            if (this.pulses == null || this.pulses.Length == 0)
            {
                return;
            }

            Vector3 resolvedPosition = worldPosition;
            if (resolvedPosition == Vector3.zero && this.parent != null)
            {
                resolvedPosition = this.parent.DrawPos;
            }

            Vector3 rawPosition = resolvedPosition;
            int index = this.nextPulseIndex;
            int rawSeed = this.BuildPulseSeed(rawPosition, index);
            Vector2 impactDirection = SurfaceShieldHullGeometry.ResolveImpactDirectionXZ(this, rawPosition, rawSeed);
            bool snappedToHull;
            bool hullMaskAvailable;
            bool directionalSurfaceHit;
            bool nearestSolidFallback;
            bool impactClampedToHull;
            Vector2 hullUv;
            resolvedPosition = SurfaceShieldHullGeometry.ResolveVisualHitPosition(
                this,
                resolvedPosition,
                impactDirection,
                out hullUv,
                out snappedToHull,
                out hullMaskAvailable,
                out directionalSurfaceHit,
                out nearestSolidFallback,
                out impactClampedToHull);
            Vector3 correctedBeforeVerticalBias = resolvedPosition;
            bool verticalInwardBiasApplied;
            bool finalHullUvSolid;
            resolvedPosition = SurfaceShieldHullGeometry.ApplyVerticalImpactInwardBias(
                this,
                resolvedPosition,
                impactDirection,
                rawPosition.y,
                ref hullUv,
                out verticalInwardBiasApplied,
                out finalHullUvSolid);
            if (verticalInwardBiasApplied)
            {
                snappedToHull = true;
                impactClampedToHull |= finalHullUvSolid;
            }

            string clampMethod = SurfaceShieldVisualDiagnostics.ResolveImpactClampMethod(
                hullMaskAvailable,
                directionalSurfaceHit,
                nearestSolidFallback,
                impactClampedToHull,
                snappedToHull);
            if (verticalInwardBiasApplied)
            {
                clampMethod += "+verticalInwardBias";
            }

            Vector3 hullSurfaceWorld = SurfaceShieldHullGeometry.HullUvToWorldPosition(
                this,
                hullUv,
                SurfaceShieldHullGeometry.GetCurrentHullDrawSize(this),
                rawPosition.y);
            float surfaceOffsetDistance = new Vector2(
                resolvedPosition.x - hullSurfaceWorld.x,
                resolvedPosition.z - hullSurfaceWorld.z).magnitude;
            Vector2 surfaceOutward = SurfaceShieldHullGeometry.ResolveHullSurfaceOutwardDirection(
                this,
                hullSurfaceWorld,
                impactDirection);
            bool verticalTighteningApplied = SurfaceShieldHullGeometry.IsVerticalImpactTighteningApplied(
                this,
                surfaceOutward);

            int seed = this.BuildPulseSeed(resolvedPosition, index);
            bool wasValid = this.pulses[index].Valid;
            this.pulses[index] = new ShieldHitPulse
            {
                Valid = true,
                WorldPosition = resolvedPosition,
                StartTick = this.CurrentTick,
                Strength = Mathf.Clamp(strength, 0f, 2f),
                Radius = Mathf.Clamp(radius, this.GetMinPulseRadius(), this.GetMaxPulseRadius()),
                BrokeShield = brokeShield,
                DamageCategory = damageCategory,
                Seed = seed,
                ImpactDirectionXZ = impactDirection,
                HullUv = hullUv,
                HasHullUv = true,
                ImpactClampedToHull = impactClampedToHull,
                DirectionalSurfaceHit = directionalSurfaceHit,
                NearestSolidFallback = nearestSolidFallback,
                HullMaskAvailable = hullMaskAvailable,
                ClampMethod = clampMethod,
                SurfaceOffsetDistance = surfaceOffsetDistance,
                VerticalTighteningApplied = verticalTighteningApplied,
                VerticalInwardBiasApplied = verticalInwardBiasApplied,
                CorrectedBeforeVerticalInwardBias = correctedBeforeVerticalBias,
                ShieldStrengthAtHit = Mathf.Clamp01(shieldStrengthAtHit),
                WasLowShieldAtHit = wasLowShieldAtHit && !brokeShield,
                SplashRotation = SurfaceShieldFallbackMath.PseudoRandom01(seed + 17) * Mathf.PI * 2f,
                SplashCount = this.ResolveSplashCount(brokeShield)
            };

            this.nextPulseIndex = (index + 1) % this.pulses.Length;
            if (!wasValid && this.activePulseCount < this.pulses.Length)
            {
                this.activePulseCount++;
            }

            if (Prefs.DevMode || this.Props.devLogPulses)
            {
                ShuttleLog.Debug(
                    "SurfaceShieldVisual",
                    "pulse category=" + (damageCategory ?? "<none>") +
                    ", strength=" + this.pulses[index].Strength.ToString("0.##") +
                    ", radius=" + this.pulses[index].Radius.ToString("0.##") +
                    ", broke=" + brokeShield +
                    ", position=" + resolvedPosition +
                    ", raw=" + rawPosition +
                    ", corrected=" + resolvedPosition +
                    ", correctedBeforeVerticalBias=" + correctedBeforeVerticalBias +
                    ", finalCorrected=" + resolvedPosition +
                    ", impactDir=" + impactDirection +
                    ", snapped=" + snappedToHull +
                    ", directionalSurface=" + directionalSurfaceHit +
                    ", nearestFallback=" + nearestSolidFallback +
                    ", hullMask=" + hullMaskAvailable +
                    ", hullUv=" + hullUv +
                    ", clampMethod=" + clampMethod +
                    ", solidAlphaFound=" + impactClampedToHull +
                    ", surfaceOffsetDistance=" + surfaceOffsetDistance.ToString("0.###") +
                    ", verticalTightening=" + verticalTighteningApplied +
                    ", verticalInwardBiasApplied=" + verticalInwardBiasApplied +
                    ", verticalImpactInwardBiasWorld=" + this.Props.verticalImpactInwardBiasWorld.ToString("0.###") +
                    ", verticalExtraInwardBiasWorld=" + this.Props.verticalExtraInwardBiasWorld.ToString("0.###") +
                    ", verticalImpactInwardBiasThreshold=" + this.Props.verticalImpactInwardBiasThreshold.ToString("0.###") +
                    ", finalHullUv=" + hullUv +
                    ", horizontalOffsetScale=" + this.Props.horizontalImpactSurfaceOffsetScale.ToString("0.###") +
                    ", verticalOffsetScale=" + this.Props.verticalImpactSurfaceOffsetScale.ToString("0.###") +
                    ", sparkEnabled=" + (this.Props.impactSparkCount > 0 && this.Props.impactSparkAlpha > 0f) +
                    ", sparkCount=" + this.Props.impactSparkCount +
                    ", sparkDirection=" + impactDirection +
                    ", clampedToHull=" + impactClampedToHull +
                    ", active=" + this.activePulseCount);
            }
        }

        private void EnsurePulseBuffer()
        {
            int capacity = Mathf.Max(0, this.Props.maxPulses);
            if (this.pulses != null && this.pulses.Length == capacity)
            {
                return;
            }

            this.pulses = capacity > 0 ? new ShieldHitPulse[capacity] : new ShieldHitPulse[0];
            this.nextPulseIndex = 0;
            this.activePulseCount = 0;
        }

        private void CleanupExpiredPulses(int ticksGame)
        {
            if (this.pulses == null || this.pulses.Length == 0 || this.activePulseCount <= 0)
            {
                return;
            }

            int lifetime = Mathf.Max(1, this.Props.pulseLifetimeTicks);
            int active = 0;
            for (int i = 0; i < this.pulses.Length; i++)
            {
                if (!this.pulses[i].Valid)
                {
                    continue;
                }

                if (ticksGame - this.pulses[i].StartTick > lifetime)
                {
                    this.pulses[i].Valid = false;
                    continue;
                }

                active++;
            }

            this.activePulseCount = active;
        }

        private float CalculatePulseStrength(ShuttleSurfaceShieldAbsorbResult result)
        {
            float incoming = Mathf.Max(0f, result.IncomingDamageAmount);
            float shieldDamage = Mathf.Max(0, result.ShieldDamageApplied);
            float normalized = Mathf.Clamp01(Mathf.Max(incoming, shieldDamage) / 300f);
            float strength = Mathf.Lerp(0.85f, 1.35f, normalized);
            if (result.BrokeShield)
            {
                strength = Mathf.Max(1.45f, Mathf.Lerp(1.45f, 2f, normalized));
            }

            return Mathf.Clamp(strength, 0.85f, 2f);
        }

        private int BuildPulseSeed(Vector3 worldPosition, int index)
        {
            int tick = this.CurrentTick;
            int x = Mathf.RoundToInt(worldPosition.x * 13f);
            int z = Mathf.RoundToInt(worldPosition.z * 17f);
            unchecked
            {
                int seed = 486187739;
                seed = seed * 31 + tick;
                seed = seed * 31 + index;
                seed = seed * 31 + x;
                seed = seed * 31 + z;
                return seed;
            }
        }

        private int ResolveSplashCount(bool brokeShield)
        {
            int count = Mathf.Max(0, this.Props.fallbackSplashCount);
            return brokeShield ? Mathf.Min(12, count + 3) : Mathf.Min(12, count);
        }

        private float GetMinPulseRadius()
        {
            return Mathf.Max(0f, this.Props.minPulseRadius);
        }

        private float GetMaxPulseRadius()
        {
            return Mathf.Max(this.GetMinPulseRadius(), this.Props.maxPulseRadius);
        }
    }
}
