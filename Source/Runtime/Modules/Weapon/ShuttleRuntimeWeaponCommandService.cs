using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    internal sealed class ShuttleRuntimeWeaponCommandService
    {
        internal bool TrySetWeaponForcedTarget(
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            LocalTargetInfo target,
            out string failReason)
        {
            failReason = null;

            ShuttleWeaponModuleDef weaponDef;
            ShuttleWeaponRuntimeState state;
            if (!this.TryGetWeaponRuntimeControlState(
                assemblyState,
                runtimeState,
                moduleInstanceID,
                out weaponDef,
                out state,
                out failReason))
            {
                return false;
            }

            ShuttleModule module = assemblyState.GetModule(moduleInstanceID);
            ShuttleModuleRuntimeContext runtimeContext = this.BuildRuntimeContext(
                host,
                assemblyState,
                profile,
                runtimeState,
                null,
                null,
                module,
                state);
            ShuttleWeaponEngagementResult engagement =
                ShuttleWeaponRuntimeSystem.Instance.EvaluateForcedTarget(runtimeContext, target);
            if (!engagement.IsAllowed)
            {
                failReason = engagement.Failure ==
                    ShuttleWeaponEngagementFailure.ForcedTargetUnsupported
                        ? "CT_Shuttle_Command_WeaponForcedTargetUnsupported".Translate().ToString()
                        : "CT_Shuttle_Command_WeaponTargetRejected".Translate(
                            ShuttleWeaponEngagementFailureText.Translate(engagement.Failure)).ToString();
                return false;
            }

            state.SetForcedTargetForRuntimeOnly(target);
            state.ResetCurrentTargetForRuntimeOnly();
            return true;
        }

        internal bool TryClearWeaponForcedTarget(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            out string failReason)
        {
            failReason = null;

            ShuttleWeaponModuleDef weaponDef;
            ShuttleWeaponRuntimeState state;
            if (!this.TryGetWeaponRuntimeControlState(
                assemblyState,
                runtimeState,
                moduleInstanceID,
                out weaponDef,
                out state,
                out failReason))
            {
                return false;
            }

            state.ClearForcedTargetForRuntimeOnly();
            state.ClearLastForcedTargetFailureForRuntimeOnly();
            state.ResetCurrentTargetForRuntimeOnly();
            return true;
        }

        internal bool TrySetWeaponHoldFire(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            bool holdFire,
            out string failReason)
        {
            failReason = null;

            ShuttleWeaponModuleDef weaponDef;
            ShuttleWeaponRuntimeState state;
            if (!this.TryGetWeaponRuntimeControlState(
                assemblyState,
                runtimeState,
                moduleInstanceID,
                out weaponDef,
                out state,
                out failReason))
            {
                return false;
            }

            state.SetHoldFireForRuntimeOnly(holdFire);
            return true;
        }

        internal bool TrySetFireControlLinked(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            bool linked,
            out string failReason)
        {
            failReason = null;

            ShuttleWeaponModuleDef weaponDef;
            ShuttleWeaponRuntimeState state;
            if (!this.TryGetWeaponRuntimeControlState(
                assemblyState,
                runtimeState,
                moduleInstanceID,
                out weaponDef,
                out state,
                out failReason))
            {
                return false;
            }

            state.SetFireControlLinked(linked);
            return true;
        }

        internal bool TrySetFireControlMode(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            ShuttleWeaponFireControlMode mode,
            out string failReason)
        {
            failReason = null;

            ShuttleWeaponModuleDef weaponDef;
            ShuttleWeaponRuntimeState state;
            if (!this.TryGetWeaponRuntimeControlState(
                assemblyState,
                runtimeState,
                moduleInstanceID,
                out weaponDef,
                out state,
                out failReason))
            {
                return false;
            }

            if (mode == ShuttleWeaponFireControlMode.PointDefense &&
                !weaponDef.canInterceptProjectiles)
            {
                failReason = "weapon-point-defense-unsupported";
                return false;
            }

            state.SetFireControlMode(mode);
            return true;
        }

        internal bool TrySetTargetPriority(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            ShuttleWeaponTargetPriority priority,
            out string failReason)
        {
            failReason = null;

            ShuttleWeaponModuleDef weaponDef;
            ShuttleWeaponRuntimeState state;
            if (!this.TryGetWeaponRuntimeControlState(
                assemblyState,
                runtimeState,
                moduleInstanceID,
                out weaponDef,
                out state,
                out failReason))
            {
                return false;
            }

            state.SetTargetPriority(priority);
            return true;
        }

        internal bool TrySetAutoFireEnabled(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            bool enabled,
            out string failReason)
        {
            failReason = null;

            ShuttleWeaponModuleDef weaponDef;
            ShuttleWeaponRuntimeState state;
            if (!this.TryGetWeaponRuntimeControlState(
                assemblyState,
                runtimeState,
                moduleInstanceID,
                out weaponDef,
                out state,
                out failReason))
            {
                return false;
            }

            state.SetAutoFireEnabled(enabled);
            return true;
        }

        internal bool TrySetWeaponAmmo(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            string ammoDefName,
            out string failReason)
        {
            failReason = null;
            ShuttleWeaponModuleDef weaponDef;
            ShuttleWeaponRuntimeState state;
            if (!this.TryGetWeaponRuntimeControlState(
                assemblyState,
                runtimeState,
                moduleInstanceID,
                out weaponDef,
                out state,
                out failReason))
            {
                return false;
            }

            return ShuttleWeaponRuntimeSystem.Instance.TrySelectAmmo(
                moduleInstanceID,
                weaponDef,
                state,
                ammoDefName,
                out failReason);
        }

        internal bool TryStartWeaponReload(
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            IShuttleCargoBackend cargoBackend,
            IShuttleStoredEnergySink storedEnergySink,
            string moduleInstanceID,
            out string failReason)
        {
            failReason = null;
            ShuttleWeaponModuleDef weaponDef;
            ShuttleWeaponRuntimeState state;
            if (!this.TryGetWeaponRuntimeControlState(
                assemblyState,
                runtimeState,
                moduleInstanceID,
                out weaponDef,
                out state,
                out failReason))
            {
                return false;
            }

            return ShuttleWeaponRuntimeSystem.Instance.TryRequestReload(
                moduleInstanceID,
                weaponDef,
                state,
                ShuttleWeaponReloadRequestKind.Manual,
                out failReason);
        }

        internal bool TryCancelWeaponReload(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            out string failReason)
        {
            failReason = null;
            ShuttleWeaponModuleDef weaponDef;
            ShuttleWeaponRuntimeState state;
            if (!this.TryGetWeaponRuntimeControlState(
                assemblyState,
                runtimeState,
                moduleInstanceID,
                out weaponDef,
                out state,
                out failReason))
            {
                return false;
            }

            return ShuttleWeaponRuntimeSystem.Instance.TryCancelReload(
                moduleInstanceID,
                weaponDef,
                state,
                out failReason);
        }

        internal bool TrySetWeaponAutoReload(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            bool enabled,
            out string failReason)
        {
            failReason = null;
            ShuttleWeaponModuleDef weaponDef;
            ShuttleWeaponRuntimeState state;
            if (!this.TryGetWeaponRuntimeControlState(
                assemblyState,
                runtimeState,
                moduleInstanceID,
                out weaponDef,
                out state,
                out failReason))
            {
                return false;
            }

            return ShuttleWeaponRuntimeSystem.Instance.TrySetAutoReloadEnabled(
                moduleInstanceID,
                weaponDef,
                state,
                enabled,
                out failReason);
        }

        internal bool TrySetWeaponManualReloadAllowed(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            bool enabled,
            out string failReason)
        {
            failReason = null;
            ShuttleWeaponModuleDef weaponDef;
            ShuttleWeaponRuntimeState state;
            if (!this.TryGetWeaponRuntimeControlState(
                assemblyState,
                runtimeState,
                moduleInstanceID,
                out weaponDef,
                out state,
                out failReason))
            {
                return false;
            }

            return ShuttleWeaponRuntimeSystem.Instance.TrySetManualReloadAllowed(
                moduleInstanceID,
                weaponDef,
                state,
                enabled,
                out failReason);
        }

        internal bool TrySetWeaponLogisticsAutoFeed(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            bool enabled,
            out string failReason)
        {
            failReason = null;
            ShuttleWeaponModuleDef weaponDef;
            ShuttleWeaponRuntimeState state;
            if (!this.TryGetWeaponRuntimeControlState(
                assemblyState,
                runtimeState,
                moduleInstanceID,
                out weaponDef,
                out state,
                out failReason))
            {
                return false;
            }

            return ShuttleWeaponRuntimeSystem.Instance.TrySetLogisticsAutoFeedEnabled(
                moduleInstanceID,
                weaponDef,
                state,
                enabled,
                out failReason);
        }

        internal bool CanManualReloadJob(
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            IShuttleCargoBackend cargoBackend,
            IShuttleStoredEnergySink storedEnergySink,
            out ShuttleWeaponManualReloadPlan plan,
            out string failReason)
        {
            return this.TryFindManualReloadCandidate(
                host,
                assemblyState,
                profile,
                runtimeState,
                cargoBackend,
                storedEnergySink,
                false,
                null,
                -1,
                null,
                out plan,
                out failReason);
        }

        internal bool TryClaimManualReloadJob(
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            IShuttleCargoBackend cargoBackend,
            IShuttleStoredEnergySink storedEnergySink,
            Pawn pawn,
            int jobLoadID,
            ThingDef carriedAmmoThingDef,
            out ShuttleWeaponManualReloadPlan plan,
            out string failReason)
        {
            return this.TryFindManualReloadCandidate(
                host,
                assemblyState,
                profile,
                runtimeState,
                cargoBackend,
                storedEnergySink,
                true,
                pawn,
                jobLoadID,
                carriedAmmoThingDef,
                out plan,
                out failReason);
        }

        private bool TryFindManualReloadCandidate(
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            IShuttleCargoBackend cargoBackend,
            IShuttleStoredEnergySink storedEnergySink,
            bool markActive,
            Pawn pawn,
            int jobLoadID,
            ThingDef requiredAmmoThingDef,
            out ShuttleWeaponManualReloadPlan plan,
            out string failReason)
        {
            plan = null;
            failReason = null;
            if (assemblyState == null || assemblyState.Modules == null)
            {
                failReason = "CT_Shuttle_Command_WeaponStateUnavailable".Translate().ToString();
                return false;
            }

            for (int i = 0; i < assemblyState.Modules.Count; i++)
            {
                ShuttleModule module = assemblyState.Modules[i];
                if (module == null || !module.IsEnabled || string.IsNullOrEmpty(module.ModuleInstanceID))
                {
                    continue;
                }

                ShuttleWeaponModuleDef weaponDef = module.ModuleDef as ShuttleWeaponModuleDef;
                if (weaponDef == null)
                {
                    continue;
                }

                ShuttleWeaponRuntimeState state;
                string localReason;
                if (!this.TryGetWeaponRuntimeControlState(
                    assemblyState,
                    runtimeState,
                    module.ModuleInstanceID,
                    out weaponDef,
                    out state,
                    out localReason))
                {
                    continue;
                }

                ShuttleModuleRuntimeContext runtimeContext = this.BuildRuntimeContext(
                    host,
                    assemblyState,
                    profile,
                    runtimeState,
                    cargoBackend,
                    storedEnergySink,
                    module,
                    state);
                ShuttleWeaponManualReloadPlan candidate;
                bool accepted = markActive
                    ? ShuttleWeaponRuntimeSystem.Instance.TryClaimManualReload(
                        runtimeContext,
                        pawn,
                        jobLoadID,
                        requiredAmmoThingDef,
                        out candidate,
                        out localReason)
                    : ShuttleWeaponRuntimeSystem.Instance.TryGetManualReloadPlan(
                        runtimeContext,
                        out candidate,
                        out localReason);
                if (!accepted || candidate == null)
                {
                    if (!string.IsNullOrEmpty(localReason))
                    {
                        failReason = localReason;
                    }

                    continue;
                }

                plan = candidate;
                return true;
            }

            if (string.IsNullOrEmpty(failReason))
            {
                failReason = "CT_Shuttle_WeaponAmmo_NoManualReloadJob".Translate().ToString();
            }

            return false;
        }

        internal bool TryCompleteManualReloadJob(
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            IShuttleCargoBackend cargoBackend,
            IShuttleStoredEnergySink storedEnergySink,
            string moduleInstanceID,
            Pawn pawn,
            int jobLoadID,
            out string failReason)
        {
            failReason = null;
            ShuttleWeaponModuleDef weaponDef;
            ShuttleWeaponRuntimeState state;
            if (!this.TryGetWeaponRuntimeControlState(
                assemblyState,
                runtimeState,
                moduleInstanceID,
                out weaponDef,
                out state,
                out failReason))
            {
                return false;
            }

            ShuttleModule module = assemblyState.GetModule(moduleInstanceID);
            ShuttleModuleRuntimeContext runtimeContext = this.BuildRuntimeContext(
                host,
                assemblyState,
                profile,
                runtimeState,
                cargoBackend,
                storedEnergySink,
                module,
                state);
            return ShuttleWeaponRuntimeSystem.Instance.TryCompleteManualReload(
                runtimeContext,
                pawn,
                jobLoadID,
                out failReason);
        }

        internal bool TryReleaseManualReloadJob(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            Pawn pawn,
            int jobLoadID)
        {
            string failReason;
            ShuttleWeaponModuleDef weaponDef;
            ShuttleWeaponRuntimeState state;
            if (!this.TryGetWeaponRuntimeControlState(
                assemblyState,
                runtimeState,
                moduleInstanceID,
                out weaponDef,
                out state,
                out failReason))
            {
                return false;
            }

            ShuttleWeaponAmmoState ammoState = state.AmmoForRuntimeOnly;
            if (ammoState == null)
            {
                return false;
            }

            return ammoState.TryClearManualReloadJobActive(
                pawn != null ? pawn.thingIDNumber : -1,
                jobLoadID);
        }

        private ShuttleModuleRuntimeContext BuildRuntimeContext(
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            IShuttleCargoBackend cargoBackend,
            IShuttleStoredEnergySink storedEnergySink,
            ShuttleModule module,
            IShuttleModuleRuntimeState state)
        {
            ShuttleSegment parentSegment = assemblyState != null && module != null
                ? assemblyState.GetSegment(module.ParentSegmentInstanceID)
                : null;
            IShuttleCargoResourceBroker cargoBroker = cargoBackend != null
                ? new ShuttleCargoResourceBroker(host, profile, runtimeState, cargoBackend)
                : null;
            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : -1;
            return new ShuttleModuleRuntimeContext(
                host,
                profile,
                runtimeState,
                module,
                parentSegment,
                state,
                storedEnergySink,
                null,
                cargoBroker,
                ticksGame);
        }

        private bool TryGetWeaponRuntimeControlState(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            out ShuttleWeaponModuleDef weaponDef,
            out ShuttleWeaponRuntimeState state,
            out string failReason)
        {
            weaponDef = null;
            state = null;
            failReason = null;

            if (string.IsNullOrEmpty(moduleInstanceID))
            {
                failReason = "CT_Shuttle_Command_WeaponMissingModuleID".Translate().ToString();
                return false;
            }

            if (assemblyState == null)
            {
                failReason = "CT_Shuttle_Command_ShuttleAssemblyStateUnavailable".Translate().ToString();
                return false;
            }

            ShuttleModule module = assemblyState.GetModule(moduleInstanceID);
            if (module == null || !this.IsInstalledModuleReferenceCurrent(assemblyState, module))
            {
                failReason = "CT_Shuttle_Command_WeaponModuleNotInstalled".Translate().ToString();
                return false;
            }

            if (!module.IsEnabled)
            {
                failReason = "CT_Shuttle_Command_WeaponModuleDisabled".Translate().ToString();
                return false;
            }

            weaponDef = module.ModuleDef as ShuttleWeaponModuleDef;
            if (weaponDef == null)
            {
                failReason = "CT_Shuttle_Command_WeaponTargetNotWeaponModule".Translate().ToString();
                return false;
            }

            if (runtimeState == null)
            {
                failReason = "CT_Shuttle_Command_ShuttleRuntimeStateUnavailable".Translate().ToString();
                return false;
            }

            IShuttleModuleRuntimeState runtimePayload;
            if (!runtimeState.Modules.TryGetState(
                moduleInstanceID,
                ShuttleWeaponRuntimeSystem.WeaponRuntimeSystemKey,
                out runtimePayload))
            {
                failReason = "CT_Shuttle_Command_WeaponRuntimeStateUnavailable".Translate().ToString();
                return false;
            }

            state = runtimePayload as ShuttleWeaponRuntimeState;
            if (state == null)
            {
                failReason = "CT_Shuttle_Command_WeaponRuntimeStateUnavailable".Translate().ToString();
                return false;
            }

            return true;
        }

        private bool IsInstalledModuleReferenceCurrent(
            ShuttleAssemblyState assemblyState,
            ShuttleModule module)
        {
            if (assemblyState == null || module == null)
            {
                return false;
            }

            ShuttleModuleSlot slot = assemblyState.GetModuleSlot(
                module.ParentSegmentInstanceID,
                module.ParentSlotID);
            return slot != null && slot.InstalledModuleInstanceID == module.ModuleInstanceID;
        }

    }
}
