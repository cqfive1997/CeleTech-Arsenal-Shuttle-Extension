using System;
using CeleTech.ShuttleExtension.ModularShuttle;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using CombatExtended;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    /// <summary>
    /// Advances one CE firing cycle and delegates idle reload work to the focused reload driver.
    /// </summary>
    internal sealed class CeWeaponBackendTickDriver : IShuttleWeaponTickDriver
    {
        private const int DefaultScanIntervalTicks = 30;
        private const int IdleTargetScanIntervalTicks = 10;

        private readonly CeWeaponBackendLifecycleDriver lifecycle;
        private readonly CeWeaponCycleDriver cycleDriver;
        private readonly CeWeaponReloadDriver reloadDriver;

        internal CeWeaponBackendTickDriver(
            CeWeaponBackendLifecycleDriver lifecycle,
            CeWeaponCycleDriver cycleDriver,
            CeWeaponReloadDriver reloadDriver)
        {
            this.lifecycle = lifecycle;
            this.cycleDriver = cycleDriver;
            this.reloadDriver = reloadDriver;
        }

        public void Tick(ShuttleModuleRuntimeContext context)
        {
            ShuttleWeaponTickDiagnostics diagnostics =
                ShuttleWeaponTickDiagnostics.Create(context);
            diagnostics.Begin();
            try
            {
                if (this.lifecycle == null ||
                    !this.lifecycle.EnsureReady(context))
                {
                    diagnostics.Complete(
                        ShuttleWeaponRuntimeProfiler.SectionCeLifecycle);
                    return;
                }

                string failureReason = null;
                ShuttleWeaponModuleDef weaponDef = context != null
                    ? context.ModuleDef as ShuttleWeaponModuleDef
                    : null;
                ShuttleWeaponRuntimeState state = context != null
                    ? context.State as ShuttleWeaponRuntimeState
                    : null;
                Thing gun = state != null ? state.GunForRuntimeOnly : null;
                CeWeaponMuzzleVerb verb = CeWeaponRuntimeGunAccess.GetMuzzleVerb(gun);
                CompAmmoUser magazine = CeWeaponRuntimeGunAccess.GetMagazine(gun);
                if (weaponDef == null || state == null || verb == null || magazine == null ||
                    state.MagazineAuthorityBackendIdForRuntimeOnly != CeWeaponBackendFactory.Id)
                {
                    this.StopCycle(state);
                    diagnostics.Complete(
                        ShuttleWeaponRuntimeProfiler.SectionCeLifecycle);
                    return;
                }

                diagnostics.CompleteAndStart(
                    ShuttleWeaponRuntimeProfiler.SectionCeLifecycle);
                int shotsPerBurst = verb.ShotsPerBurst;
                bool cycleIdle = verb.state != VerbState.Bursting &&
                    state.GetWarmupTicksForRuntimeOnly() <= 0 &&
                    state.GetCooldownTicksForRuntimeOnly() <= 0;
                ShuttleWeaponAmmoState commonState = state.AmmoForRuntimeOnly;
                if (cycleIdle && this.reloadDriver != null &&
                    this.reloadDriver.Tick(
                        context,
                        weaponDef,
                        state,
                        magazine,
                        out failureReason))
                {
                    this.StopCycle(state);
                    if (!string.IsNullOrEmpty(failureReason))
                    {
                        Log.WarningOnce(
                            "[CeleTech Shuttle][CE Reload] Reload deferred: " +
                            failureReason + ".",
                            FailureKey(context, "reload|" + failureReason));
                    }

                    diagnostics.Complete(
                        ShuttleWeaponRuntimeProfiler.SectionCeReload);
                    return;
                }

                if (cycleIdle && (shotsPerBurst <= 1 || magazine.CurMagCount < shotsPerBurst))
                {
                    this.StopCycle(state);
                    if (magazine.CurMagCount < Math.Max(1, shotsPerBurst) &&
                        commonState.AutoReloadEnabled &&
                        (!commonState.ReloadRequested ||
                         commonState.ReloadRequestKind ==
                            ShuttleWeaponReloadRequestKind.AutoTopOff))
                    {
                        if (this.reloadDriver != null)
                        {
                            this.reloadDriver.TryRequest(
                                weaponDef,
                                state,
                                ShuttleWeaponReloadRequestKind.RequiredForFire,
                                out failureReason);
                        }
                        else
                        {
                            commonState.RequestReload(
                                ShuttleWeaponReloadRequestKind.RequiredForFire);
                        }
                    }

                    diagnostics.Complete(
                        ShuttleWeaponRuntimeProfiler.SectionCeReload);
                    return;
                }

                diagnostics.CompleteAndStart(
                    ShuttleWeaponRuntimeProfiler.SectionCeReload);
                if (cycleIdle && this.ShouldDeferIdleTargetScan(context, weaponDef, state))
                {
                    diagnostics.Complete(
                        ShuttleWeaponRuntimeProfiler.SectionCeCycle);
                    return;
                }

                if (this.cycleDriver == null ||
                    !this.cycleDriver.Tick(context, verb, out failureReason))
                {
                    this.StopCycle(state);
                    if (!string.IsNullOrEmpty(failureReason))
                    {
                        Log.ErrorOnce(
                            "[CeleTech Shuttle][CE Backend] Weapon tick stopped: " +
                            failureReason + ".",
                            FailureKey(context, failureReason));
                    }
                }

                diagnostics.Complete(
                    ShuttleWeaponRuntimeProfiler.SectionCeCycle);
            }
            finally
            {
                diagnostics.Finish();
            }
        }

        private bool ShouldDeferIdleTargetScan(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state)
        {
            if (state.GetForcedTargetForRuntimeOnly().IsValid ||
                state.GetCurrentTargetForRuntimeOnly().IsValid ||
                state.IsHoldFireForRuntimeOnly() ||
                state.FireControlMode == ShuttleWeaponFireControlMode.Offline)
            {
                return false;
            }

            int authoredInterval = weaponDef != null && weaponDef.scanIntervalTicks > 0
                ? weaponDef.scanIntervalTicks
                : DefaultScanIntervalTicks;
            int interval = CeleTechShuttleMod.EffectiveCombatTuning.ApplyTicksMultiplier(
                authoredInterval,
                CeleTechShuttleMod.EffectiveCombatTuning.WeaponScanIntervalMultiplier,
                1,
                int.MaxValue);
            if (interval < IdleTargetScanIntervalTicks)
            {
                interval = IdleTargetScanIntervalTicks;
            }

            if (interval <= 1)
            {
                return false;
            }

            int phase = state.GetIdleTargetScanPhaseForRuntimeOnly(
                context.ModuleInstanceID,
                interval);
            int tickPhase = context.TicksGame % interval;
            if (tickPhase < 0)
            {
                tickPhase += interval;
            }

            return tickPhase != phase;
        }

        private void StopCycle(ShuttleWeaponRuntimeState state)
        {
            if (state == null)
            {
                return;
            }

            state.ResetCurrentTargetForRuntimeOnly();
            state.SetWarmupTicksForRuntimeOnly(0);
            state.SetActivePowerTicksForRuntimeOnly(0);
        }

        private static int FailureKey(
            ShuttleModuleRuntimeContext context,
            string failureReason)
        {
            unchecked
            {
                string moduleId = context != null
                    ? context.ModuleInstanceID
                    : string.Empty;
                string value = moduleId + "|" + (failureReason ?? string.Empty);
                int hash = 1944816211;
                for (int i = 0; i < value.Length; i++)
                {
                    hash = (hash * 397) ^ value[i];
                }

                return hash;
            }
        }
    }
}
