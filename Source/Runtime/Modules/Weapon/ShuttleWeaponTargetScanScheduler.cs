using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Owns only automatic-target scan eligibility, cadence and phase scheduling.
    /// </summary>
    internal sealed class ShuttleWeaponTargetScanScheduler
    {
        private const int DefaultScanIntervalTicks = 30;
        private const int IdleTargetScanIntervalTicks = 10;

        private readonly ShuttleWeaponTickFastPathPolicy fastPathPolicy;
        private readonly CoreMagazineDriver coreMagazine;
        private readonly ShuttleWeaponFireControlPolicy fireControlPolicy;

        internal ShuttleWeaponTargetScanScheduler(
            ShuttleWeaponTickFastPathPolicy fastPathPolicy,
            CoreMagazineDriver coreMagazine,
            ShuttleWeaponFireControlPolicy fireControlPolicy)
        {
            this.fastPathPolicy = fastPathPolicy;
            this.coreMagazine = coreMagazine;
            this.fireControlPolicy = fireControlPolicy;
        }

        internal bool IsIdleAutoScanEligible(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state,
            Verb attackVerb,
            ShuttleWeaponAmmoState ammoState,
            int cooldownTicks)
        {
            if (context == null || weaponDef == null || state == null || attackVerb == null)
            {
                return false;
            }

            if (attackVerb.state == VerbState.Bursting ||
                state.GetWarmupTicksForRuntimeOnly() > 0 ||
                cooldownTicks > 0)
            {
                return false;
            }

            if (state.GetForcedTargetForRuntimeOnly().IsValid ||
                state.GetCurrentTargetForRuntimeOnly().IsValid ||
                attackVerb.CurrentTarget.IsValid)
            {
                return false;
            }

            if (ammoState != null &&
                (ammoState.ReloadInProgress ||
                 ammoState.ManualReloadJobActive ||
                 this.fastPathPolicy.DoesReloadRequestBlock(weaponDef, ammoState)))
            {
                return false;
            }

            if (!this.coreMagazine.CanFire(weaponDef, ammoState))
            {
                return false;
            }

            if (this.fireControlPolicy.IsFireControlHardStopped(context, state) ||
                state.TargetPriority == ShuttleWeaponTargetPriority.ForcedTargetOnly)
            {
                return false;
            }

            return this.fireControlPolicy.CanUseAutomaticFireControl(
                context,
                weaponDef,
                state);
        }

        internal bool ShouldRunIdleScanThisTick(
            ShuttleWeaponRuntimeState state,
            string moduleInstanceID,
            int ticksGame,
            ShuttleWeaponModuleDef weaponDef)
        {
            int interval = this.GetEffectiveIdleTargetScanInterval(weaponDef);
            if (interval <= 1)
            {
                return true;
            }

            int phase = state != null
                ? state.GetIdleTargetScanPhaseForRuntimeOnly(moduleInstanceID, interval)
                : 0;
            int tickPhase = ticksGame % interval;
            if (tickPhase < 0)
            {
                tickPhase += interval;
            }

            return tickPhase == phase;
        }

        internal bool ShouldRunRegularScan(
            ThingWithComps host,
            ShuttleWeaponModuleDef weaponDef,
            int ticksGame)
        {
            int interval = this.GetEffectiveWeaponScanInterval(weaponDef);
            if (interval <= 1)
            {
                return true;
            }

            if (host != null && host.Spawned)
            {
                return host.IsHashIntervalTick(interval);
            }

            return ticksGame % interval == 0;
        }

        internal WeaponFullPipelineReason GetFullPipelineReason(
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state,
            Verb attackVerb,
            ShuttleWeaponAmmoState ammoState)
        {
            if (ammoState != null &&
                (ammoState.ReloadInProgress ||
                 ammoState.ManualReloadJobActive ||
                 this.fastPathPolicy.DoesReloadRequestBlock(weaponDef, ammoState)))
            {
                return WeaponFullPipelineReason.ReloadRelated;
            }

            if ((state != null &&
                 (state.GetForcedTargetForRuntimeOnly().IsValid ||
                  state.GetCurrentTargetForRuntimeOnly().IsValid)) ||
                (attackVerb != null && attackVerb.CurrentTarget.IsValid))
            {
                return WeaponFullPipelineReason.ForcedOrCurrentTarget;
            }

            return WeaponFullPipelineReason.Other;
        }

        private int GetEffectiveIdleTargetScanInterval(ShuttleWeaponModuleDef weaponDef)
        {
            int weaponInterval = this.GetEffectiveWeaponScanInterval(weaponDef);
            return weaponInterval > IdleTargetScanIntervalTicks
                ? weaponInterval
                : IdleTargetScanIntervalTicks;
        }

        private int GetEffectiveWeaponScanInterval(ShuttleWeaponModuleDef weaponDef)
        {
            int interval = weaponDef != null && weaponDef.scanIntervalTicks > 0
                ? weaponDef.scanIntervalTicks
                : DefaultScanIntervalTicks;
            return CeleTechShuttleMod.EffectiveCombatTuning.ApplyTicksMultiplier(
                interval,
                CeleTechShuttleMod.EffectiveCombatTuning.WeaponScanIntervalMultiplier,
                1,
                int.MaxValue);
        }
    }
}
