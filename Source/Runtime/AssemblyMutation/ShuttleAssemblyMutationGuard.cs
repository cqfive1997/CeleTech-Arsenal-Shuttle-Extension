using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyMutation
{
    internal sealed class ShuttleAssemblyMutationGuard
    {
        private readonly ShuttleAssemblyCargoStateGate cargoStateGate =
            new ShuttleAssemblyCargoStateGate();

        internal bool CanInstallSegment(
            ShuttleCommandContext context,
            string segmentSlotID,
            ShuttleSegmentBaseDef segmentDef,
            out string failureReason)
        {
            if (!this.TryValidateContext(context, out failureReason))
            {
                return false;
            }

            if (segmentDef == null)
            {
                failureReason = "CT_Shuttle_Command_SegmentInstallFailed".Translate().ToString();
                return false;
            }

            if (!context.AssemblyMutationController.CanInstallSegment(
                context.AssemblyState,
                segmentSlotID,
                segmentDef,
                out failureReason))
            {
                return false;
            }

            return this.VerifyNoAssemblyOperationsInProgress(context, out failureReason);
        }

        internal bool CanReplaceSegment(
            ShuttleCommandContext context,
            string segmentSlotID,
            ShuttleSegmentBaseDef segmentDef,
            out string failureReason)
        {
            if (!this.TryValidateContext(context, out failureReason))
            {
                return false;
            }

            if (segmentDef == null)
            {
                failureReason = "CT_Shuttle_Command_SegmentReplaceMissingDef".Translate().ToString();
                return false;
            }

            if (!context.AssemblyMutationController.CanReplaceSegment(
                context.AssemblyState,
                segmentSlotID,
                segmentDef,
                out failureReason))
            {
                return false;
            }

            return this.VerifyNoLoadedOrQueuedCargo(context, out failureReason);
        }

        internal bool CanInstallModule(
            ShuttleCommandContext context,
            string segmentInstanceID,
            string moduleSlotID,
            ShuttleModuleBaseDef moduleDef,
            bool replacing,
            out string failureReason)
        {
            if (replacing)
            {
                return this.CanReplaceModule(
                    context,
                    segmentInstanceID,
                    moduleSlotID,
                    moduleDef,
                    ShuttleTickUtility.TicksGameOrMinusOne(),
                    out failureReason);
            }

            if (!this.CanInstallModuleTarget(
                context,
                segmentInstanceID,
                moduleSlotID,
                moduleDef,
                false,
                out failureReason))
            {
                return false;
            }

            if (!this.VerifyNoAssemblyOperationsInProgress(context, out failureReason))
            {
                return false;
            }

            return true;
        }

        internal bool CanReplaceModule(
            ShuttleCommandContext context,
            string segmentInstanceID,
            string moduleSlotID,
            ShuttleModuleBaseDef newModuleDef,
            int ticksGame,
            out string failureReason)
        {
            if (!this.TryValidateContext(context, out failureReason))
            {
                return false;
            }

            ShuttleModule oldModule = this.FindInstalledModule(
                context.AssemblyState,
                segmentInstanceID,
                moduleSlotID);
            if (oldModule == null)
            {
                failureReason = "CT_Shuttle_Command_ModuleReplaceTargetMissing".Translate().ToString();
                return false;
            }

            if (!this.CanInstallModuleTarget(
                context,
                segmentInstanceID,
                moduleSlotID,
                newModuleDef,
                true,
                out failureReason))
            {
                return false;
            }

            if (!this.VerifyNoAssemblyOperationsInProgress(context, out failureReason))
            {
                return false;
            }

            return this.CanRemoveModule(
                context,
                oldModule,
                ticksGame,
                true,
                out failureReason);
        }

        internal bool CanRemoveModule(
            ShuttleCommandContext context,
            ShuttleModule module,
            int ticksGame,
            bool allowRequiredSlotReplacement,
            out string failureReason)
        {
            if (!this.TryValidateContext(context, out failureReason))
            {
                return false;
            }

            if (module == null)
            {
                failureReason = "CT_Shuttle_Command_ModuleRemoveMissing".Translate().ToString();
                return false;
            }

            if (!this.VerifyNoAssemblyOperationsInProgress(context, out failureReason))
            {
                return false;
            }

            ShuttleModuleSlot slot = context.AssemblyState.GetModuleSlot(
                module.ParentSegmentInstanceID,
                module.ParentSlotID);
            if (slot == null || string.IsNullOrEmpty(slot.InstalledModuleInstanceID))
            {
                failureReason = "CT_Shuttle_Command_ModuleRemoveMissing".Translate().ToString();
                return false;
            }

            if (slot.InstalledModuleInstanceID != module.ModuleInstanceID)
            {
                failureReason = "CT_Shuttle_Command_ModuleTargetChanged".Translate().ToString();
                return false;
            }

            if (!module.IsInstalledIn(module.ParentSegmentInstanceID, module.ParentSlotID))
            {
                failureReason = "CT_Shuttle_Command_ModuleRemoveMissing".Translate().ToString();
                return false;
            }

            if (slot.IsLocked || (!allowRequiredSlotReplacement && slot.IsRequired))
            {
                failureReason = "CT_Shuttle_Command_ModuleRequiredOrLocked".Translate().ToString();
                return false;
            }

            if (!this.VerifyRefrigeratedCargoModuleCanBeRemoved(context, module, out failureReason))
            {
                return false;
            }

            if (!this.VerifyMedicalBayModuleCanBeRemoved(context, module, out failureReason))
            {
                return false;
            }

            if (!this.VerifyMechChargerModuleCanBeRemoved(context, module, out failureReason))
            {
                return false;
            }

            if (!this.VerifyHabitatModuleCanBeRemoved(context, module, out failureReason))
            {
                return false;
            }

            if (!this.VerifyPrisonCellModuleCanBeRemoved(context, module, out failureReason))
            {
                return false;
            }

            return this.VerifyModuleRuntimeCanRemove(
                context,
                module,
                ticksGame,
                out failureReason);
        }

        internal bool CanRemoveSegment(
            ShuttleCommandContext context,
            ShuttleSegmentSlot slot,
            ShuttleSegment segment,
            out string failureReason)
        {
            if (!this.TryValidateContext(context, out failureReason))
            {
                return false;
            }

            if (slot == null || segment == null)
            {
                failureReason = "CT_Shuttle_Command_SegmentRemoveMissing".Translate().ToString();
                return false;
            }

            if (string.IsNullOrEmpty(slot.InstalledSegmentInstanceID) ||
                slot.InstalledSegmentInstanceID != segment.SegmentInstanceID)
            {
                failureReason = "CT_Shuttle_Command_SegmentRemoveMissing".Translate().ToString();
                return false;
            }

            if (!this.VerifyNoLoadedOrQueuedCargo(context, out failureReason))
            {
                return false;
            }

            if (slot.IsFixed || slot.IsLocked || slot.IsRequired)
            {
                failureReason = "CT_Shuttle_Command_SegmentRemovalFailed".Translate().ToString();
                return false;
            }

            if (segment.SegmentDef != null && segment.SegmentDef.isNonRemovable)
            {
                failureReason = "CT_Shuttle_Command_SegmentRemovalFailed".Translate().ToString();
                return false;
            }

            if (segment.GetModuleCount() > 0)
            {
                failureReason = "CT_Shuttle_Command_SegmentRemovalFailed".Translate().ToString();
                return false;
            }

            return true;
        }

        internal bool VerifyNoLoadedOrQueuedCargo(
            ShuttleCommandContext context,
            out string failureReason)
        {
            if (!this.VerifyNoAssemblyOperationsInProgress(context, out failureReason))
            {
                return false;
            }

            return this.cargoStateGate.VerifyNoStablePayload(context, out failureReason);
        }

        private bool VerifyNoAssemblyOperationsInProgress(
            ShuttleCommandContext context,
            out string failureReason)
        {
            if (!this.VerifyNoActiveOrRecoveryTransfer(context, out failureReason))
            {
                return false;
            }

            return this.cargoStateGate.VerifyNoCargoOperationsInProgress(
                context,
                out failureReason);
        }

        internal bool VerifyNoActiveOrRecoveryTransfer(
            ShuttleCommandContext context,
            out string failureReason)
        {
            failureReason = null;
            if (context == null || context.Host == null)
            {
                failureReason = "CT_Shuttle_Command_ContextUnavailable".Translate().ToString();
                return false;
            }

            CompShuttleHolderLaunchTransferState transferState =
                context.Host.TryGetComp<CompShuttleHolderLaunchTransferState>();
            if (transferState == null || !transferState.HasAnyActiveOrRecoveryTransfer)
            {
                return true;
            }

            failureReason = "CT_Shuttle_Command_CannotModifyAssemblyWithActiveTransfer"
                .Translate(transferState.DescribeActiveOrRecoveryTransferBlocker())
                .ToString();
            return false;
        }

        private bool CanInstallModuleTarget(
            ShuttleCommandContext context,
            string segmentInstanceID,
            string moduleSlotID,
            ShuttleModuleBaseDef moduleDef,
            bool allowOccupiedSlot,
            out string failureReason)
        {
            if (!this.TryValidateContext(context, out failureReason))
            {
                return false;
            }

            if (moduleDef == null)
            {
                failureReason = "CT_Shuttle_Command_ModuleDefUnavailable".Translate().ToString();
                return false;
            }

            return context.AssemblyMutationController.CanInstallModule(
                context.AssemblyState,
                segmentInstanceID,
                moduleSlotID,
                moduleDef,
                allowOccupiedSlot,
                out failureReason);
        }

        private bool TryValidateContext(
            ShuttleCommandContext context,
            out string failureReason)
        {
            failureReason = null;
            if (context == null || context.Host == null)
            {
                failureReason = "CT_Shuttle_Command_ContextUnavailable".Translate().ToString();
                return false;
            }

            if (context.AssemblyState == null)
            {
                failureReason = "CT_Shuttle_Command_AssemblyStateUnavailable".Translate().ToString();
                return false;
            }

            if (context.AssemblyMutationController == null)
            {
                failureReason = "CT_Shuttle_Command_ContextUnavailable".Translate().ToString();
                return false;
            }

            context.AssemblyState.EnsureInitialized();
            if (context.AssemblyState.TryGetBlockingIntegrityFailureReason(out failureReason))
            {
                return false;
            }

            return true;
        }

        private bool VerifyRefrigeratedCargoModuleCanBeRemoved(
            ShuttleCommandContext context,
            ShuttleModule module,
            out string failureReason)
        {
            failureReason = null;
            ShuttleRefrigeratedCargoModuleDef moduleDef =
                module != null ? module.ModuleDef as ShuttleRefrigeratedCargoModuleDef : null;
            if (module == null || moduleDef == null)
            {
                return true;
            }

            CompShuttleHolderLaunchTransferState transferState =
                context.Host.TryGetComp<CompShuttleHolderLaunchTransferState>();
            string activeTransferReason;
            if (transferState != null &&
                transferState.HasActiveRefrigeratedLaunchTransferForModule(
                    module.ModuleInstanceID,
                    out activeTransferReason))
            {
                failureReason = "CT_Shuttle_Command_CannotRemoveRefrigeratedCargoModuleWithActiveTransfer"
                    .Translate(
                        moduleDef.LabelCap,
                        module.ModuleInstanceID,
                        activeTransferReason ?? "-")
                    .ToString();
                return false;
            }

            CompShuttleRefrigeratedCargoRegistry registry =
                context.Host.TryGetComp<CompShuttleRefrigeratedCargoRegistry>();
            if (registry == null)
            {
                failureReason = "CT_Shuttle_Command_RefrigeratedCargoRegistryUnavailable"
                    .Translate()
                    .ToString();
                return false;
            }

            registry.Reconcile(context.AssemblyState, context.GetRuntimeState());
            RefrigeratedCargoCoolingStatus status;
            if (registry.TryGetCoolingStatus(module.ModuleInstanceID, out status) &&
                status != null &&
                status.HasColdCargo)
            {
                failureReason = "CT_Shuttle_Command_CannotRemoveRefrigeratedCargoModuleWithColdCargo"
                    .Translate(
                        moduleDef.LabelCap,
                        status.ModuleInstanceID,
                        status.StackCount)
                    .ToString();
                return false;
            }

            return true;
        }

        private bool VerifyMedicalBayModuleCanBeRemoved(
            ShuttleCommandContext context,
            ShuttleModule module,
            out string failureReason)
        {
            failureReason = null;
            if (module == null || !(module.ModuleDef is ShuttleMedicalBayModuleDef))
            {
                return true;
            }

            CompShuttleHolderLaunchTransferState transferState =
                context.Host.TryGetComp<CompShuttleHolderLaunchTransferState>();
            string activeTransferReason;
            if (transferState != null &&
                transferState.HasActiveMedicalBayPatientLocalTransfer(out activeTransferReason))
            {
                failureReason = "CT_Shuttle_Command_CannotRemoveMedicalBayModuleWithActiveTransfer"
                    .Translate(activeTransferReason ?? "-")
                    .ToString();
                return false;
            }

            CompShuttleMedicalBayOccupancy occupancy =
                context.Host.TryGetComp<CompShuttleMedicalBayOccupancy>();
            if (occupancy == null)
            {
                failureReason = "CT_Shuttle_Command_CannotVerifyMedicalBayOccupancyBeforeRemoval"
                    .Translate()
                    .ToString();
                return false;
            }

            if (occupancy != null &&
                (occupancy.HasPatients || occupancy.CountPendingAdmissionReservations(null) > 0))
            {
                failureReason = "CT_Shuttle_Command_CannotRemoveMedicalBayModuleWithPatients"
                    .Translate()
                    .ToString();
                return false;
            }

            return true;
        }

        private bool VerifyMechChargerModuleCanBeRemoved(
            ShuttleCommandContext context,
            ShuttleModule module,
            out string failureReason)
        {
            failureReason = null;
            if (module == null || !(module.ModuleDef is ShuttleMechChargerModuleDef))
            {
                return true;
            }

            CompShuttleMechChargerOccupancy occupancy =
                context.Host.TryGetComp<CompShuttleMechChargerOccupancy>();
            if (occupancy == null)
            {
                failureReason = "CT_Shuttle_Command_CannotVerifyMechChargerOccupancyBeforeRemoval"
                    .Translate()
                    .ToString();
                return false;
            }

            if (occupancy != null &&
                (occupancy.HasChargingMechs || occupancy.CountPendingChargingReservations(null) > 0))
            {
                failureReason = "CT_Shuttle_Command_CannotRemoveMechChargerModuleWithMechs"
                    .Translate()
                    .ToString();
                return false;
            }

            return true;
        }

        private bool VerifyHabitatModuleCanBeRemoved(
            ShuttleCommandContext context,
            ShuttleModule module,
            out string failureReason)
        {
            failureReason = null;
            if (module == null || !(module.ModuleDef is ShuttleHabitatModuleDef))
            {
                return true;
            }

            CompShuttleHabitatOccupancy occupancy =
                context.Host.TryGetComp<CompShuttleHabitatOccupancy>();
            if (occupancy == null)
            {
                failureReason = "CT_Shuttle_Command_CannotVerifyHabitatOccupancyBeforeRemoval"
                    .Translate()
                    .ToString();
                return false;
            }

            if (occupancy != null && occupancy.HasAnyOccupants)
            {
                failureReason = "CT_Shuttle_Command_CannotRemoveHabitatModuleWithOccupants"
                    .Translate()
                    .ToString();
                return false;
            }

            return true;
        }

        private bool VerifyPrisonCellModuleCanBeRemoved(
            ShuttleCommandContext context,
            ShuttleModule module,
            out string failureReason)
        {
            failureReason = null;
            if (module == null || !(module.ModuleDef is ShuttlePrisonCellModuleDef))
            {
                return true;
            }

            CompShuttlePrisonCellOccupancy occupancy =
                context.Host.TryGetComp<CompShuttlePrisonCellOccupancy>();
            if (occupancy == null)
            {
                failureReason = "CT_Shuttle_Command_CannotVerifyPrisonCellOccupancyBeforeRemoval"
                    .Translate()
                    .ToString();
                return false;
            }

            if (occupancy != null && occupancy.HasPrisoners)
            {
                failureReason = "CT_Shuttle_Command_CannotRemovePrisonCellModuleWithPrisoners"
                    .Translate()
                    .ToString();
                return false;
            }

            return true;
        }

        private bool VerifyModuleRuntimeCanRemove(
            ShuttleCommandContext context,
            ShuttleModule module,
            int ticksGame,
            out string failureReason)
        {
            failureReason = null;
            if (context.ModuleRuntimeCoordinator == null)
            {
                failureReason = "CT_Shuttle_Command_ShuttleRuntimeStateUnavailable".Translate().ToString();
                return false;
            }

            if (!context.ModuleRuntimeCoordinator.CanRemoveModule(
                context.Host,
                context.AssemblyState,
                context.GetProfileForRead(),
                context.GetRuntimeState(),
                module,
                context.StoredEnergySink,
                ticksGame,
                out failureReason))
            {
                if (string.IsNullOrEmpty(failureReason))
                {
                    failureReason = "CT_Shuttle_Command_ModuleRemovalFailed".Translate().ToString();
                }

                return false;
            }

            return true;
        }

        private ShuttleModule FindInstalledModule(
            ShuttleAssemblyState state,
            string segmentInstanceID,
            string moduleSlotID)
        {
            if (state == null)
            {
                return null;
            }

            ShuttleModuleSlot slot = state.GetModuleSlot(segmentInstanceID, moduleSlotID);
            if (slot == null || string.IsNullOrEmpty(slot.InstalledModuleInstanceID))
            {
                return null;
            }

            return state.GetModule(slot.InstalledModuleInstanceID);
        }

    }
}
