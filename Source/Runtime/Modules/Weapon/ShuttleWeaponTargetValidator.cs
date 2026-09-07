using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    internal sealed class ShuttleWeaponTargetValidator
    {
        private readonly ShuttleWeaponBallisticTargetValidator ballisticValidator;

        internal ShuttleWeaponTargetValidator(
            ShuttleWeaponBallisticTargetValidator ballisticValidator)
        {
            this.ballisticValidator = ballisticValidator;
        }

        internal bool IsValidTargetNow(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state,
            Verb attackVerb,
            LocalTargetInfo target)
        {
            LocalTargetInfo forcedTarget = state != null
                ? state.GetForcedTargetForRuntimeOnly()
                : LocalTargetInfo.Invalid;
            ShuttleWeaponEngagementResult result =
                forcedTarget.IsValid && this.IsSameTarget(target, forcedTarget)
                    ? this.EvaluateForcedTargetNow(
                        context,
                        weaponDef,
                        state,
                        attackVerb,
                        target)
                    : this.EvaluateAutomaticTargetNow(
                        context,
                        weaponDef,
                        state,
                        attackVerb,
                        target);
            return result.IsAllowed;
        }

        internal ShuttleWeaponEngagementResult EvaluateForcedTargetNow(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state,
            Verb attackVerb,
            LocalTargetInfo target)
        {
            return this.EvaluateTargetNow(
                context,
                weaponDef,
                state,
                attackVerb,
                target,
                false);
        }

        internal ShuttleWeaponEngagementResult EvaluateAutomaticTargetNow(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state,
            Verb attackVerb,
            LocalTargetInfo target)
        {
            return this.EvaluateTargetNow(
                context,
                weaponDef,
                state,
                attackVerb,
                target,
                true);
        }

        private ShuttleWeaponEngagementResult EvaluateTargetNow(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state,
            Verb attackVerb,
            LocalTargetInfo target,
            bool requireHostility)
        {
            ThingWithComps host = context != null ? context.Host : null;
            Map map = host != null ? host.Map : null;
            if (host == null || map == null || weaponDef == null || state == null || attackVerb == null)
            {
                return ShuttleWeaponEngagementResult.Rejected(
                    ShuttleWeaponEngagementFailure.WeaponUnavailable);
            }

            if (!target.IsValid || !target.Cell.IsValid)
            {
                return ShuttleWeaponEngagementResult.Rejected(
                    ShuttleWeaponEngagementFailure.TargetInvalid);
            }

            if (!target.Cell.InBounds(map))
            {
                return ShuttleWeaponEngagementResult.Rejected(
                    ShuttleWeaponEngagementFailure.TargetOutOfBounds);
            }

            if (map.fogGrid != null && map.fogGrid.IsFogged(target.Cell))
            {
                return ShuttleWeaponEngagementResult.Rejected(
                    ShuttleWeaponEngagementFailure.TargetFogged);
            }

            if (target.HasThing)
            {
                Thing thing = target.Thing;
                if (thing == null || thing.Destroyed || !thing.Spawned || thing.Map != map)
                {
                    return ShuttleWeaponEngagementResult.Rejected(
                        ShuttleWeaponEngagementFailure.TargetUnavailable);
                }

                Pawn pawn = thing as Pawn;
                if (pawn != null)
                {
                    if (pawn.Dead || pawn.Downed ||
                        (requireHostility && !this.IsHostilePawnToShuttle(host, pawn)))
                    {
                        return ShuttleWeaponEngagementResult.Rejected(
                            pawn.Dead || pawn.Downed
                                ? ShuttleWeaponEngagementFailure.TargetUnavailable
                                : ShuttleWeaponEngagementFailure.TargetNotHostile);
                    }
                }
                else if (requireHostility && !this.IsHostileThingToShuttle(host, thing))
                {
                    return ShuttleWeaponEngagementResult.Rejected(
                        ShuttleWeaponEngagementFailure.TargetNotHostile);
                }
            }
            else if (!this.WeaponRoleAllowsLocationTarget(weaponDef.weaponRole))
            {
                return ShuttleWeaponEngagementResult.Rejected(
                    ShuttleWeaponEngagementFailure.LocationTargetUnsupported);
            }

            return this.ballisticValidator.Evaluate(
                context,
                weaponDef,
                state,
                attackVerb,
                target);
        }

        internal bool IsHostilePawnToShuttle(ThingWithComps host, Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            Faction faction = host != null && host.Faction != null
                ? host.Faction
                : Faction.OfPlayer;
            return faction != null && pawn.HostileTo(faction);
        }

        internal bool IsHostileThingToShuttle(ThingWithComps host, Thing thing)
        {
            if (thing == null)
            {
                return false;
            }

            Pawn pawn = thing as Pawn;
            if (pawn != null)
            {
                return this.IsHostilePawnToShuttle(host, pawn);
            }

            Faction faction = host != null && host.Faction != null
                ? host.Faction
                : Faction.OfPlayer;
            return faction != null &&
                thing.Faction != null &&
                thing.Faction.HostileTo(faction);
        }

        internal bool IsSameTarget(LocalTargetInfo a, LocalTargetInfo b)
        {
            if (!a.IsValid || !b.IsValid)
            {
                return false;
            }

            if (a.HasThing || b.HasThing)
            {
                return a.HasThing && b.HasThing && a.Thing == b.Thing;
            }

            return a.Cell == b.Cell;
        }

        internal bool WeaponRoleAllowsLocationTarget(ShuttleWeaponRole role)
        {
            return role == ShuttleWeaponRole.Rocket ||
                role == ShuttleWeaponRole.Missile ||
                role == ShuttleWeaponRole.Artillery;
        }

    }
}
