using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Core-magazine transaction boundary for one Native cast. It preserves the Legacy contract:
    /// reserve the authored cycle amount once, refund only a rejected cast start, and leave later
    /// failed burst projectiles committed to the accepted cycle.
    /// </summary>
    internal sealed class NativeVerbWeaponBurstStarter : IShuttleWeaponBurstStarter
    {
        private const int FailedStartCooldownTicks = 15;

        private readonly ShuttleWeaponReloadRequestProcessor reloadRequestProcessor;
        private readonly CoreMagazineDriver coreMagazine;
        private readonly ShuttleWeaponTargetingService targetingService;
        private readonly NativeVerbWeaponFireDriver fireDriver;

        internal NativeVerbWeaponBurstStarter(
            ShuttleWeaponReloadRequestProcessor reloadRequestProcessor,
            CoreMagazineDriver coreMagazine,
            ShuttleWeaponTargetingService targetingService,
            NativeVerbWeaponFireDriver fireDriver)
        {
            this.reloadRequestProcessor = reloadRequestProcessor;
            this.coreMagazine = coreMagazine;
            this.targetingService = targetingService;
            this.fireDriver = fireDriver;
        }

        public void BeginBurst(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state,
            Verb attackVerb)
        {
            if (context == null || weaponDef == null || state == null || attackVerb == null ||
                this.reloadRequestProcessor == null || this.coreMagazine == null ||
                this.targetingService == null ||
                this.fireDriver == null)
            {
                return;
            }

            LocalTargetInfo target = state.GetCurrentTargetForRuntimeOnly();
            if (!target.IsValid ||
                !context.InternalBusPowered ||
                !attackVerb.Available() ||
                !this.targetingService.CanContinueCurrentTarget(
                    context,
                    weaponDef,
                    state,
                    attackVerb))
            {
                state.ResetCurrentTargetForRuntimeOnly();
                return;
            }

            int loadedBefore = state.AmmoForRuntimeOnly.LoadedAmmoCount;
            string ammoFailure;
            if (!this.coreMagazine.TryReserveFireCycle(
                    weaponDef,
                    state.AmmoForRuntimeOnly,
                    out ammoFailure))
            {
                state.ResetCurrentTargetForRuntimeOnly();
                state.SetCooldownTicksForRuntimeOnly(FailedStartCooldownTicks);
                return;
            }

            string castFailure;
            if (this.fireDriver.TryStartReservedCast(context, target, out castFailure))
            {
                ShuttleWeaponCycleBaselineRecorder.RecordBurstStart(
                    context,
                    weaponDef,
                    state,
                    attackVerb,
                    true,
                    loadedBefore,
                    state.AmmoForRuntimeOnly.LoadedAmmoCount);
                this.reloadRequestProcessor.TryRequestAutomaticTopOff(
                    context,
                    weaponDef,
                    state.AmmoForRuntimeOnly);
                return;
            }

            if (!this.coreMagazine.TryRestoreFireCycleReservation(
                    weaponDef,
                    state.AmmoForRuntimeOnly))
            {
                if (Prefs.DevMode)
                {
                    Log.WarningOnce(
                        "[CeleTech Shuttle][Native Backend] Cast start rejected after ammunition " +
                        "reservation; the exact reservation could not be restored. reason=" +
                        (castFailure ?? "unknown"),
                        WarningKeyFor(weaponDef.defName, 7101312));
                }
            }

            ShuttleWeaponCycleBaselineRecorder.RecordBurstStart(
                context,
                weaponDef,
                state,
                attackVerb,
                false,
                loadedBefore,
                state.AmmoForRuntimeOnly.LoadedAmmoCount);
            state.ResetCurrentTargetForRuntimeOnly();
            state.SetCooldownTicksForRuntimeOnly(FailedStartCooldownTicks);
        }

        private static int WarningKeyFor(string text, int salt)
        {
            unchecked
            {
                int hash = salt;
                if (!string.IsNullOrEmpty(text))
                {
                    for (int i = 0; i < text.Length; i++)
                    {
                        hash = (hash * 397) ^ text[i];
                    }
                }

                return hash;
            }
        }
    }
}
