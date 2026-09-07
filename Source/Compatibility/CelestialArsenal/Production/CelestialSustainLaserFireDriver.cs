using System;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CelestialArsenal
{
    /// <summary>
    /// Owns only exact-muzzle injection and one ammunition-free sustained-laser cast entry.
    /// </summary>
    internal sealed class CelestialSustainLaserFireDriver : IShuttleWeaponFireDriver
    {
        private readonly CelestialSustainLaserHost host;
        private readonly ShuttleWeaponMuzzleResolver muzzleResolver;
        private readonly ShuttleWeaponCyclePolicy cyclePolicy;

        internal CelestialSustainLaserFireDriver(
            CelestialSustainLaserHost host,
            ShuttleWeaponMuzzleResolver muzzleResolver,
            ShuttleWeaponCyclePolicy cyclePolicy)
        {
            this.host = host;
            this.muzzleResolver = muzzleResolver;
            this.cyclePolicy = cyclePolicy;
        }

        public bool IsBusy(ShuttleModuleRuntimeContext context)
        {
            ShuttleWeaponRuntimeState state = context != null
                ? context.State as ShuttleWeaponRuntimeState
                : null;
            Verb verb = this.host != null ? this.host.GetVerb(context) : null;
            return state != null &&
                (state.HasWarmupTicksForRuntimeOnly() ||
                 state.HasCooldownTicksForRuntimeOnly() ||
                 state.HasActivePowerTicksForRuntimeOnly() ||
                 (verb != null && verb.state == VerbState.Bursting));
        }

        public bool TryStartReservedCast(
            ShuttleModuleRuntimeContext context,
            LocalTargetInfo target,
            out string failureReason)
        {
            failureReason = null;
            ShuttleWeaponModuleDef weaponDef = context != null
                ? context.ModuleDef as ShuttleWeaponModuleDef
                : null;
            ShuttleWeaponRuntimeState state = context != null
                ? context.State as ShuttleWeaponRuntimeState
                : null;
            if (context == null || context.Host == null || !context.Host.Spawned ||
                weaponDef == null || state == null || this.host == null ||
                this.muzzleResolver == null || !context.IsEnabled)
            {
                failureReason = "celestial-sustain-fire-context-unavailable";
                return false;
            }

            if (!target.IsValid)
            {
                failureReason = "celestial-sustain-fire-target-invalid";
                return false;
            }

            if (!context.InternalBusPowered || state.IsHoldFireForRuntimeOnly())
            {
                failureReason = "celestial-sustain-fire-control-stopped";
                return false;
            }

            Verb_ShuttleCelestialSustainLaser verb = this.host.GetVerb(context);
            if (verb == null || !verb.Available())
            {
                failureReason = "celestial-sustain-fire-verb-unavailable";
                return false;
            }

            if (verb.state == VerbState.Bursting)
            {
                failureReason = "celestial-sustain-fire-verb-busy";
                return false;
            }

            ShuttleWeaponMuzzleSource source = this.muzzleResolver.ResolveAndAdvance(
                context,
                weaponDef,
                state,
                target);
            if (!source.Cell.IsValid)
            {
                failureReason = "celestial-sustain-fire-muzzle-invalid";
                return false;
            }

            state.SetLastResolvedMuzzleForRuntimeOnly(source.Cell, source.DrawPos);
            verb.SetShuttleMuzzleSource(source.Cell, source.DrawPos);
            verb.SetShuttleFireControlTuning(1f, 0f, 0f, 1f);
            if (!verb.TryStartCastOn(target, false, true))
            {
                failureReason = "celestial-sustain-fire-cast-rejected";
                return false;
            }

            state.SetActivePowerTicksForRuntimeOnly(
                Math.Max(1, state.GetActivePowerTicksForRuntimeOnly()));
            return true;
        }

        internal void NotifyCastComplete(
            ShuttleWeaponRuntimeState state,
            ShuttleWeaponModuleDef weaponDef,
            Verb verb)
        {
            if (state == null)
            {
                return;
            }

            state.SetCooldownTicksForRuntimeOnly(
                this.cyclePolicy != null
                    ? this.cyclePolicy.GetCooldownTicks(weaponDef, verb)
                    : 1);
            state.SetActivePowerTicksForRuntimeOnly(
                Math.Max(1, state.GetActivePowerTicksForRuntimeOnly()));
            if (verb != null && verb.CurrentTarget.IsValid)
            {
                state.SetCurrentTargetForRuntimeOnly(verb.CurrentTarget);
            }
            else
            {
                state.ResetCurrentTargetForRuntimeOnly();
            }

            ShuttleWeaponCycleBaselineRecorder.RecordCastComplete(
                state,
                weaponDef,
                verb);
        }
    }
}
