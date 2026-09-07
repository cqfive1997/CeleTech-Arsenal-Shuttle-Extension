using System;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Computes point-defense and no-radar automatic range caps.
    /// It does not validate target identity, line of sight or hostility.
    /// </summary>
    internal sealed class ShuttleWeaponTargetRangePolicy
    {
        internal bool IsWithinPointDefenseRadius(
            ShuttleModuleRuntimeContext context,
            Verb attackVerb,
            IntVec3 cell)
        {
            ThingWithComps host = context != null ? context.Host : null;
            if (host == null || !cell.IsValid)
            {
                return false;
            }

            float radius = this.GetPointDefenseRadius(context, attackVerb);
            if (radius <= 0f)
            {
                return false;
            }

            float distanceSquared = (cell - host.Position).LengthHorizontalSquared;
            return distanceSquared <= radius * radius;
        }

        internal bool IsWithinFallbackAutoFireRange(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            Verb attackVerb,
            IntVec3 cell)
        {
            ThingWithComps host = context != null ? context.Host : null;
            if (host == null || !cell.IsValid)
            {
                return false;
            }

            float range = this.GetFallbackAutoFireRange(weaponDef, attackVerb);
            if (range <= 0f)
            {
                return false;
            }

            float distanceSquared = (cell - host.Position).LengthHorizontalSquared;
            return distanceSquared <= range * range;
        }

        private float GetPointDefenseRadius(
            ShuttleModuleRuntimeContext context,
            Verb attackVerb)
        {
            float radius = context != null &&
                context.Profile != null &&
                context.Profile.FireControl != null
                    ? context.Profile.FireControl.MaxPointDefenseRadius
                    : 0f;
            if (radius > 0f && IsFiniteFloat(radius))
            {
                return radius;
            }

            float weaponRange = this.GetWeaponMaxRange(attackVerb);
            return weaponRange > 0f ? weaponRange : 30f;
        }

        private float GetFallbackAutoFireRange(
            ShuttleWeaponModuleDef weaponDef,
            Verb attackVerb)
        {
            float weaponRange = this.GetWeaponMaxRange(attackVerb);
            float fallbackRange = weaponDef != null && weaponDef.fallbackAutoFireRange > 0f
                ? weaponDef.fallbackAutoFireRange
                : weaponRange;
            if (fallbackRange <= 0f || !IsFiniteFloat(fallbackRange))
            {
                return weaponRange;
            }

            fallbackRange = CeleTechShuttleMod.EffectiveCombatTuning.ApplyFloatMultiplier(
                fallbackRange,
                CeleTechShuttleMod.EffectiveCombatTuning.WeaponAutoFireRangeMultiplier,
                0f,
                float.MaxValue);
            return weaponRange > 0f && IsFiniteFloat(weaponRange)
                ? Math.Min(weaponRange, fallbackRange)
                : fallbackRange;
        }

        private float GetWeaponMaxRange(Verb attackVerb)
        {
            if (attackVerb == null)
            {
                return 0f;
            }

            return attackVerb.EffectiveRange > 0f
                ? attackVerb.EffectiveRange
                : attackVerb.verbProps != null ? attackVerb.verbProps.range : 0f;
        }

        private static bool IsFiniteFloat(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
