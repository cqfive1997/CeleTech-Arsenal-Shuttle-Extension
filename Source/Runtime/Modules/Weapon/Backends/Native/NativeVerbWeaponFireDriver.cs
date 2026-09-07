using System;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Weapons;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Native cast boundary. The caller must reserve authoritative ammunition before entering and
    /// must roll it back when this method reports failure.
    /// </summary>
    internal sealed class NativeVerbWeaponFireDriver : IShuttleWeaponFireDriver
    {
        private readonly NativeVerbWeaponHost host;
        private readonly ShuttleWeaponMuzzleResolver muzzleResolver;
        private readonly ShuttleWeaponFireControlPolicy fireControlPolicy;
        private readonly ShuttleWeaponCyclePolicy cyclePolicy;

        internal NativeVerbWeaponFireDriver(
            NativeVerbWeaponHost host,
            ShuttleWeaponMuzzleResolver muzzleResolver,
            ShuttleWeaponFireControlPolicy fireControlPolicy,
            ShuttleWeaponCyclePolicy cyclePolicy)
        {
            this.host = host;
            this.muzzleResolver = muzzleResolver;
            this.fireControlPolicy = fireControlPolicy;
            this.cyclePolicy = cyclePolicy;
        }

        public bool IsBusy(ShuttleModuleRuntimeContext context)
        {
            ShuttleWeaponRuntimeState state = context != null
                ? context.State as ShuttleWeaponRuntimeState
                : null;
            Verb verb = this.host != null ? this.host.GetPrimaryVerb(context) : null;
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
            if (context == null || weaponDef == null || state == null ||
                this.host == null || this.muzzleResolver == null ||
                context.Host == null || !context.Host.Spawned || !context.IsEnabled)
            {
                failureReason = "native-fire-context-unavailable";
                return false;
            }

            if (!target.IsValid)
            {
                failureReason = "native-fire-target-invalid";
                return false;
            }

            if (!context.InternalBusPowered || state.IsHoldFireForRuntimeOnly())
            {
                failureReason = "native-fire-control-stopped";
                return false;
            }

            Verb attackVerb = this.host.GetPrimaryVerb(context);
            if (attackVerb == null || !attackVerb.Available())
            {
                failureReason = "native-fire-verb-unavailable";
                return false;
            }

            if (attackVerb.state == VerbState.Bursting)
            {
                failureReason = "native-fire-verb-busy";
                return false;
            }

            ShuttleWeaponMuzzleSource source = this.muzzleResolver.ResolveAndAdvance(
                context,
                weaponDef,
                state,
                target);
            state.SetLastResolvedMuzzleForRuntimeOnly(source.Cell, source.DrawPos);
            this.ApplyMuzzleTuning(context, weaponDef, state, attackVerb, source);

            if (!attackVerb.TryStartCastOn(target, false, true))
            {
                failureReason = "native-fire-cast-rejected";
                return false;
            }

            state.SetActivePowerTicksForRuntimeOnly(
                Math.Max(1, state.GetActivePowerTicksForRuntimeOnly()));
            return true;
        }

        internal void NotifyCastComplete(
            ShuttleWeaponRuntimeState state,
            ShuttleWeaponModuleDef weaponDef,
            Verb attackVerb)
        {
            if (state == null)
            {
                return;
            }

            state.SetCooldownTicksForRuntimeOnly(
                this.cyclePolicy != null
                    ? this.cyclePolicy.GetCooldownTicks(weaponDef, attackVerb)
                    : 1);
            state.SetActivePowerTicksForRuntimeOnly(
                Math.Max(1, state.GetActivePowerTicksForRuntimeOnly()));
            if (attackVerb != null && attackVerb.CurrentTarget.IsValid)
            {
                state.SetCurrentTargetForRuntimeOnly(attackVerb.CurrentTarget);
            }
            else
            {
                state.ResetCurrentTargetForRuntimeOnly();
            }

            ShuttleWeaponCycleBaselineRecorder.RecordCastComplete(
                state,
                weaponDef,
                attackVerb);
        }

        private void ApplyMuzzleTuning(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state,
            Verb attackVerb,
            ShuttleWeaponMuzzleSource source)
        {
            IShuttleMuzzleVerb muzzleVerb = attackVerb as IShuttleMuzzleVerb;
            if (muzzleVerb == null)
            {
                return;
            }

            bool accuracyLink = this.fireControlPolicy != null &&
                state.FireControlMode != ShuttleWeaponFireControlMode.Offline &&
                this.fireControlPolicy.HasEffectiveFireControlAccuracyLink(
                    context,
                    weaponDef,
                    state);
            if (accuracyLink)
            {
                muzzleVerb.SetShuttleFireControlTuning(
                    context.Profile.FireControl.DirectFireAccuracyMultiplier,
                    context.Profile.FireControl.DirectFireAccuracyBonus,
                    context.Profile.FireControl.DirectFireAccuracyFloor,
                    context.Profile.FireControl.ForcedMissRadiusMultiplier);
            }
            else
            {
                muzzleVerb.SetShuttleFireControlTuning(1f, 0f, 0f, 1f);
            }

            muzzleVerb.SetShuttleMuzzleSource(source.Cell, source.DrawPos);
        }
    }
}
