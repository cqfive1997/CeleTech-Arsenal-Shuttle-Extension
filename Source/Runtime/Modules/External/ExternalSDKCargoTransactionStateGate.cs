using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalSDKCargoTransactionStateGate
    {
        internal bool TryPass(
            ShuttleController controller,
            out ShuttleExternalCargoTransactionFailureReason failureReason,
            out string message)
        {
            failureReason = ShuttleExternalCargoTransactionFailureReason.None;
            message = null;

            if (controller == null)
            {
                failureReason = ShuttleExternalCargoTransactionFailureReason.HostUnavailable;
                message = "shuttle controller is unavailable";
                return false;
            }

            ThingWithComps host = controller.ShuttleHost;
            if (host == null || host.Destroyed)
            {
                failureReason = ShuttleExternalCargoTransactionFailureReason.HostUnavailable;
                message = "shuttle host is unavailable";
                return false;
            }

            if (host.Map == null)
            {
                failureReason = ShuttleExternalCargoTransactionFailureReason.HostUnavailable;
                message = "shuttle host map is unavailable";
                return false;
            }

            if (ShuttleCriticalDamagePolicy.ShouldRestrictPlayerInteractions(host))
            {
                failureReason = ShuttleExternalCargoTransactionFailureReason.RuntimeBlocked;
                message = "shuttle is critically damaged";
                return false;
            }

            CompShuttleHolderLaunchTransferState transferState =
                host.TryGetComp<CompShuttleHolderLaunchTransferState>();
            if (transferState != null && transferState.HasAnyActiveOrRecoveryTransfer)
            {
                failureReason = ShuttleExternalCargoTransactionFailureReason.LaunchTransferActive;
                message = "holder or launch transfer state is active";
                return false;
            }

            string activeLocalTransferReason;
            if (transferState != null &&
                transferState.HasActiveMedicalBayPatientLocalTransfer(out activeLocalTransferReason))
            {
                failureReason = ShuttleExternalCargoTransactionFailureReason.CargoTransferActive;
                message = string.IsNullOrEmpty(activeLocalTransferReason)
                    ? "local holder transfer is active"
                    : activeLocalTransferReason;
                return false;
            }

            ShuttleRuntimeState runtimeState = controller.GetLaunchRuntimeState();
            if (runtimeState != null &&
                runtimeState.ModuleRemoval != null &&
                runtimeState.ModuleRemoval.HasActiveRemoval)
            {
                failureReason = ShuttleExternalCargoTransactionFailureReason.RuntimeBlocked;
                message = "module removal is active";
                return false;
            }

            if (this.HasExternalRuntimeFailure(runtimeState, out message))
            {
                failureReason = ShuttleExternalCargoTransactionFailureReason.RuntimeBlocked;
                return false;
            }

            return true;
        }

        private bool HasExternalRuntimeFailure(
            ShuttleRuntimeState runtimeState,
            out string message)
        {
            message = null;
            if (runtimeState == null || runtimeState.Modules == null)
            {
                return false;
            }

            IReadOnlyList<ShuttleModuleRuntimeStateRecord> records =
                runtimeState.Modules.RecordsForRead;
            for (int i = 0; records != null && i < records.Count; i++)
            {
                ShuttleModuleRuntimeStateRecord record = records[i];
                ExternalModuleRuntimeState externalState =
                    record != null ? record.State as ExternalModuleRuntimeState : null;
                if (externalState == null || !externalState.HasRuntimeFailure)
                {
                    continue;
                }

                message = "external runtime state is failed for " +
                    (record.RuntimeSystemKey ?? "unknown");
                return true;
            }

            return false;
        }
    }
}
