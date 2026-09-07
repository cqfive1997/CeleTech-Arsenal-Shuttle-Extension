using System;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Backend-neutral timing and firing-power policy. It owns no Verb, target, ammo or runtime
    /// state and is shared by Legacy and typed compatibility backends.
    /// </summary>
    internal sealed class ShuttleWeaponCyclePolicy
    {
        private const int DefaultCooldownTicks = 60;

        internal int GetWarmupTicks(
            ShuttleWeaponModuleDef weaponDef,
            Verb attackVerb)
        {
            int warmupTicks = weaponDef != null ? weaponDef.warmupTicks : 0;
            if (warmupTicks <= 0 && attackVerb != null && attackVerb.verbProps != null)
            {
                warmupTicks = attackVerb.verbProps.warmupTime.SecondsToTicks();
            }

            return CeleTechShuttleMod.EffectiveCombatTuning.ApplyTicksMultiplier(
                Math.Max(1, warmupTicks),
                CeleTechShuttleMod.EffectiveCombatTuning.WeaponWarmupMultiplier,
                1,
                int.MaxValue);
        }

        internal int GetCooldownTicks(
            ShuttleWeaponModuleDef weaponDef,
            Verb attackVerb)
        {
            int cooldownTicks = weaponDef != null ? weaponDef.cooldownTicks : 0;
            if (cooldownTicks <= 0 && attackVerb != null && attackVerb.verbProps != null)
            {
                cooldownTicks = attackVerb.verbProps.defaultCooldownTime.SecondsToTicks();
            }

            cooldownTicks = cooldownTicks > 0 ? cooldownTicks : DefaultCooldownTicks;
            return CeleTechShuttleMod.EffectiveCombatTuning.ApplyTicksMultiplier(
                cooldownTicks,
                CeleTechShuttleMod.EffectiveCombatTuning.WeaponCooldownMultiplier,
                1,
                int.MaxValue);
        }

        internal float GetFiringPowerDrawWatts(ShuttleWeaponModuleDef weaponDef)
        {
            float baseWatts = weaponDef != null && weaponDef.firingPowerDrawWatts > 0f
                ? weaponDef.firingPowerDrawWatts
                : 0f;
            return CeleTechShuttleMod.EffectiveCombatTuning.ApplyFloatMultiplier(
                baseWatts,
                CeleTechShuttleMod.EffectiveCombatTuning.WeaponPowerDrawMultiplier,
                0f,
                float.MaxValue);
        }

        internal bool ShouldReportFiringPowerDemand(
            ShuttleWeaponRuntimeState state,
            Verb attackVerb)
        {
            if (state == null)
            {
                return false;
            }

            if (attackVerb != null && attackVerb.state == VerbState.Bursting)
            {
                return true;
            }

            if (state.IsHoldFireForRuntimeOnly())
            {
                return false;
            }

            return state.HasWarmupTicksForRuntimeOnly() ||
                state.HasActivePowerTicksForRuntimeOnly();
        }
    }
}
