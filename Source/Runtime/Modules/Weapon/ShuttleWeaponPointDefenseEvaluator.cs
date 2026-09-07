using System;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Validates one projectile-domain target. Ordinary hostility and pawn/building rules are
    /// intentionally not reused here.
    /// </summary>
    internal sealed class ShuttleWeaponPointDefenseEvaluator
    {
        private readonly ShuttleProjectileThreatAdapterRegistry threatAdapters;
        private readonly ShuttleWeaponBallisticTargetValidator ballisticValidator;
        private readonly ShuttleWeaponTargetRangePolicy targetRangePolicy;
        private readonly ShuttleWeaponCyclePolicy cyclePolicy;

        internal ShuttleWeaponPointDefenseEvaluator(
            ShuttleProjectileThreatAdapterRegistry threatAdapters,
            ShuttleWeaponBallisticTargetValidator ballisticValidator,
            ShuttleWeaponTargetRangePolicy targetRangePolicy,
            ShuttleWeaponCyclePolicy cyclePolicy)
        {
            this.threatAdapters = threatAdapters;
            this.ballisticValidator = ballisticValidator;
            this.targetRangePolicy = targetRangePolicy;
            this.cyclePolicy = cyclePolicy;
        }

        internal ShuttleWeaponEngagementResult Evaluate(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state,
            Verb attackVerb,
            LocalTargetInfo target)
        {
            ShuttleProjectileThreat threat;
            ShuttleWeaponEngagementResult result;
            return this.TryEvaluate(
                context,
                weaponDef,
                state,
                attackVerb,
                target,
                out threat,
                out result)
                    ? ShuttleWeaponEngagementResult.Allowed()
                    : result;
        }

        internal bool TryEvaluate(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state,
            Verb attackVerb,
            LocalTargetInfo target,
            out ShuttleProjectileThreat threat,
            out ShuttleWeaponEngagementResult result)
        {
            threat = null;
            result = ShuttleWeaponEngagementResult.Rejected(
                ShuttleWeaponEngagementFailure.ProjectileThreatInvalid);
            ThingWithComps host = context != null ? context.Host : null;
            Map map = host != null ? host.Map : null;
            if (host == null || map == null || weaponDef == null || state == null ||
                attackVerb == null || this.threatAdapters == null ||
                this.ballisticValidator == null || this.targetRangePolicy == null ||
                this.cyclePolicy == null)
            {
                result = ShuttleWeaponEngagementResult.Rejected(
                    ShuttleWeaponEngagementFailure.WeaponUnavailable);
                return false;
            }

            if (!weaponDef.canInterceptProjectiles ||
                state.FireControlMode != ShuttleWeaponFireControlMode.PointDefense)
            {
                result = ShuttleWeaponEngagementResult.Rejected(
                    ShuttleWeaponEngagementFailure.ProjectileInterceptionUnsupported);
                return false;
            }

            if (!target.HasThing ||
                !this.threatAdapters.TryCreateThreat(target.Thing, out threat) ||
                threat.Projectile == null || threat.Projectile.Destroyed ||
                !threat.Projectile.Spawned || threat.Projectile.Map != map)
            {
                return false;
            }

            if (weaponDef.pointDefenseExplosiveOrOverheadOnly &&
                !threat.ExplosiveOrOverhead)
            {
                return false;
            }

            if (weaponDef.pointDefenseMaxIncomingSpeed > 0f &&
                threat.SpeedCellsPerSecond > weaponDef.pointDefenseMaxIncomingSpeed)
            {
                result = ShuttleWeaponEngagementResult.Rejected(
                    ShuttleWeaponEngagementFailure.ProjectileThreatTooFast);
                return false;
            }

            if (!this.IsHostileOrUnattributed(host, threat.Launcher) ||
                !this.IsImpactInsideProtectedArea(host, weaponDef, threat.IntendedImpactCell))
            {
                result = ShuttleWeaponEngagementResult.Rejected(
                    ShuttleWeaponEngagementFailure.ProjectileThreatNotIncoming);
                return false;
            }

            if (threat.TicksToImpact <= 0)
            {
                return false;
            }

            if (!this.targetRangePolicy.IsWithinPointDefenseRadius(
                    context,
                    attackVerb,
                    threat.Projectile.Position))
            {
                result = ShuttleWeaponEngagementResult.Rejected(
                    ShuttleWeaponEngagementFailure.TargetOutsidePointDefenseRadius);
                return false;
            }

            ShuttleWeaponEngagementResult ballistic = this.ballisticValidator.Evaluate(
                context,
                weaponDef,
                state,
                attackVerb,
                target);
            if (!ballistic.IsAllowed)
            {
                result = ballistic;
                return false;
            }

            if (this.EstimateInterceptTicks(
                    context,
                    weaponDef,
                    state,
                    attackVerb,
                    target) > threat.TicksToImpact)
            {
                result = ShuttleWeaponEngagementResult.Rejected(
                    ShuttleWeaponEngagementFailure.ProjectileThreatTooLate);
                return false;
            }

            result = ShuttleWeaponEngagementResult.Allowed();
            return true;
        }

        private bool IsHostileOrUnattributed(ThingWithComps host, Thing launcher)
        {
            if (launcher == null)
            {
                return true;
            }

            if (launcher == host)
            {
                return false;
            }

            Faction defendingFaction = host.Faction ?? Faction.OfPlayer;
            return launcher.Faction == null ||
                defendingFaction == null ||
                launcher.Faction.HostileTo(defendingFaction);
        }

        private bool IsImpactInsideProtectedArea(
            ThingWithComps host,
            ShuttleWeaponModuleDef weaponDef,
            IntVec3 impactCell)
        {
            if (host == null || weaponDef == null || !impactCell.IsValid)
            {
                return false;
            }

            float radius = weaponDef.pointDefenseProtectionRadius;
            if (radius <= 0f)
            {
                return false;
            }

            float distanceSquared = (impactCell - host.Position).LengthHorizontalSquared;
            return distanceSquared <= radius * radius;
        }

        private int EstimateInterceptTicks(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state,
            Verb attackVerb,
            LocalTargetInfo target)
        {
            Verb_LaunchProjectile projectileVerb = attackVerb as Verb_LaunchProjectile;
            ThingDef outgoingProjectile = projectileVerb != null
                ? projectileVerb.Projectile
                : null;
            ProjectileProperties projectileProperties = outgoingProjectile != null
                ? outgoingProjectile.projectile
                : null;
            float speedTilesPerTick = projectileProperties != null
                ? projectileProperties.SpeedTilesPerTick
                : 0f;
            if (speedTilesPerTick <= 0f || float.IsNaN(speedTilesPerTick) ||
                float.IsInfinity(speedTilesPerTick))
            {
                return int.MaxValue;
            }

            ShuttleWeaponMuzzleSource source = this.ballisticValidator.ResolveValidationSource(
                context,
                weaponDef,
                state,
                attackVerb,
                target);
            Vector3 targetPosition = target.Thing != null
                ? target.Thing.DrawPos
                : target.Cell.ToVector3Shifted();
            Vector3 remaining = targetPosition - source.DrawPos;
            float horizontalDistance = (float)Math.Sqrt(
                remaining.x * remaining.x + remaining.z * remaining.z);
            int flightTicks = Math.Max(
                1,
                (int)Math.Ceiling(horizontalDistance / speedTilesPerTick));

            LocalTargetInfo currentTarget = state.GetCurrentTargetForRuntimeOnly();
            bool alreadyTracking = currentTarget.HasThing &&
                currentTarget.Thing == target.Thing;
            int warmupTicks = alreadyTracking
                ? state.GetWarmupTicksForRuntimeOnly()
                : this.cyclePolicy.GetWarmupTicks(weaponDef, attackVerb);
            if (warmupTicks > int.MaxValue - flightTicks)
            {
                return int.MaxValue;
            }

            return warmupTicks + flightTicks;
        }
    }
}
