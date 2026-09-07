using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Weapons;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Validates muzzle-relative range, line of sight, roof and Verb hit geometry.
    /// It does not decide hostility, fire-control mode or target priority.
    /// </summary>
    internal sealed class ShuttleWeaponBallisticTargetValidator
    {
        private readonly ShuttleWeaponMuzzleResolver muzzleResolver;

        internal ShuttleWeaponBallisticTargetValidator(
            ShuttleWeaponMuzzleResolver muzzleResolver)
        {
            this.muzzleResolver = muzzleResolver;
        }

        internal ShuttleWeaponEngagementResult Evaluate(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state,
            Verb attackVerb,
            LocalTargetInfo target)
        {
            ThingWithComps host = context != null ? context.Host : null;
            Map map = host != null ? host.Map : null;
            if (host == null || map == null || weaponDef == null || state == null || attackVerb == null)
            {
                return ShuttleWeaponEngagementResult.Rejected(
                    ShuttleWeaponEngagementFailure.WeaponUnavailable);
            }

            ShuttleWeaponMuzzleSource source = this.ResolveValidationSource(
                context,
                weaponDef,
                state,
                attackVerb,
                target);
            IntVec3 sourceCell = source.Cell.IsValid ? source.Cell : host.Position;
            float distanceSquared = (target.Cell - sourceCell).LengthHorizontalSquared;
            float maxRange = attackVerb.EffectiveRange > 0f
                ? attackVerb.EffectiveRange
                : attackVerb.verbProps != null ? attackVerb.verbProps.range : 0f;
            if (maxRange <= 0f || distanceSquared > maxRange * maxRange)
            {
                return ShuttleWeaponEngagementResult.Rejected(
                    ShuttleWeaponEngagementFailure.TargetOutOfRange);
            }

            float minRange = attackVerb.verbProps != null
                ? attackVerb.verbProps.EffectiveMinRange(target, host)
                : 0f;
            if (minRange > 0f && distanceSquared < minRange * minRange)
            {
                return ShuttleWeaponEngagementResult.Rejected(
                    ShuttleWeaponEngagementFailure.TargetInsideMinimumRange);
            }

            if (this.NeedsLineOfSight(attackVerb) &&
                !this.HasLineOfSightToTarget(sourceCell, target, map))
            {
                return ShuttleWeaponEngagementResult.Rejected(
                    ShuttleWeaponEngagementFailure.TargetBlockedByLineOfSight);
            }

            if (attackVerb.ProjectileFliesOverhead())
            {
                RoofDef targetRoof = map.roofGrid.RoofAt(target.Cell);
                if (targetRoof != null && targetRoof.isThickRoof)
                {
                    return ShuttleWeaponEngagementResult.Rejected(
                        ShuttleWeaponEngagementFailure.TargetUnderThickRoof);
                }
            }

            IShuttleWeaponValidationMuzzle validationMuzzle =
                attackVerb as IShuttleWeaponValidationMuzzle;
            if (validationMuzzle != null &&
                !validationMuzzle.TrySetValidationMuzzle(source))
            {
                return ShuttleWeaponEngagementResult.Rejected(
                    ShuttleWeaponEngagementFailure.VerbCannotHitTarget);
            }

            return attackVerb.CanHitTargetFrom(sourceCell, target)
                ? ShuttleWeaponEngagementResult.Allowed()
                : ShuttleWeaponEngagementResult.Rejected(
                    ShuttleWeaponEngagementFailure.VerbCannotHitTarget);
        }

        internal ShuttleWeaponMuzzleSource ResolveValidationSource(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state,
            Verb attackVerb,
            LocalTargetInfo target)
        {
            ThingWithComps host = context != null ? context.Host : null;
            if (host == null)
            {
                return ShuttleWeaponMuzzleSource.Fallback(
                    IntVec3.Invalid,
                    UnityEngine.Vector3.zero);
            }

            if (attackVerb is IShuttleMuzzleVerb ||
                attackVerb is IShuttleWeaponValidationMuzzle)
            {
                return this.muzzleResolver.Resolve(context, weaponDef, state, target);
            }

            return ShuttleWeaponMuzzleSource.Fallback(host.Position, host.DrawPos);
        }

        private bool NeedsLineOfSight(Verb attackVerb)
        {
            return attackVerb == null ||
                attackVerb.verbProps == null ||
                attackVerb.verbProps.requireLineOfSight;
        }

        private bool HasLineOfSightToTarget(
            IntVec3 sourceCell,
            LocalTargetInfo target,
            Map map)
        {
            if (target.HasThing && target.Thing != null)
            {
                return GenSight.LineOfSightToThing(sourceCell, target.Thing, map, true);
            }

            return GenSight.LineOfSight(sourceCell, target.Cell, map, true);
        }
    }
}
