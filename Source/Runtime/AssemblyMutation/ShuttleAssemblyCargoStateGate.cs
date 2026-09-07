using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyMutation
{
    /// <summary>
    /// Read-only cargo/runtime gate used by the authoritative assembly mutation guard.
    /// It distinguishes stable payload from loading or unloading that is still in progress.
    /// </summary>
    internal sealed class ShuttleAssemblyCargoStateGate
    {
        internal bool VerifyNoCargoOperationsInProgress(
            ShuttleCommandContext context,
            out string failureReason)
        {
            failureReason = null;
            if (context == null ||
                context.Host == null ||
                context.AssemblyState == null)
            {
                failureReason = "CT_Shuttle_Command_CannotVerifyCargoBeforeAssemblyChange"
                    .Translate()
                    .ToString();
                return false;
            }

            ShuttleRuntimeState runtimeState = context.GetRuntimeState();
            if (runtimeState == null)
            {
                failureReason = "CT_Shuttle_Command_ShuttleRuntimeStateUnavailable"
                    .Translate()
                    .ToString();
                return false;
            }

            if ((runtimeState.CargoUnload != null && runtimeState.CargoUnload.IsActive) ||
                this.HasPendingPassengerBoardingIntent(context, runtimeState))
            {
                failureReason = "CT_Shuttle_Command_CannotModifyAssemblyWithCargoOperation"
                    .Translate()
                    .ToString();
                return false;
            }

            if (context.CargoBackend == null)
            {
                failureReason = "CT_Shuttle_Command_CannotVerifyQueuedCargoBeforeAssemblyChange"
                    .Translate()
                    .ToString();
                return false;
            }

            if (context.CargoBackend.HasPendingLoadQueue(context.Host))
            {
                failureReason = "CT_Shuttle_Command_CannotModifyAssemblyWithCargoOperation"
                    .Translate()
                    .ToString();
                return false;
            }

            return true;
        }

        private bool HasPendingPassengerBoardingIntent(
            ShuttleCommandContext context,
            ShuttleRuntimeState runtimeState)
        {
            ShuttlePassengerBoardingIntentState intentState =
                runtimeState != null ? runtimeState.PassengerBoardingIntents : null;
            if (intentState == null || !intentState.HasAny)
            {
                return false;
            }

            IShuttlePassengerBoardingBackend boardingBackend =
                context != null ? context.CargoBackend as IShuttlePassengerBoardingBackend : null;
            if (boardingBackend == null)
            {
                return true;
            }

            for (int i = 0; i < intentState.RecordsForReading.Count; i++)
            {
                ShuttlePassengerBoardingIntentRecord record =
                    intentState.RecordsForReading[i];
                if (record == null ||
                    record.Pawn == null ||
                    !boardingBackend.ContainsPassenger(context.Host, record.Pawn))
                {
                    return true;
                }
            }

            return false;
        }

        internal bool VerifyNoStablePayload(
            ShuttleCommandContext context,
            out string failureReason)
        {
            failureReason = null;
            if (context == null ||
                context.Host == null ||
                context.AssemblyState == null)
            {
                failureReason = "CT_Shuttle_Command_CannotVerifyCargoBeforeAssemblyChange"
                    .Translate()
                    .ToString();
                return false;
            }

            ShuttleCargoSnapshot snapshot = context.BuildCargoSnapshot();
            if (snapshot == null)
            {
                failureReason = "CT_Shuttle_Command_CannotVerifyCargoBeforeAssemblyChange"
                    .Translate()
                    .ToString();
                return false;
            }

            if (!snapshot.HasTransporter)
            {
                failureReason = "CT_Shuttle_Command_CannotVerifyCargoTransporterMissing"
                    .Translate()
                    .ToString();
                return false;
            }

            bool hasStablePayload =
                snapshot.LoadedStackCount > 0 ||
                snapshot.LoadedThingCount > 0 ||
                snapshot.MedicalBayPatientCount > 0 ||
                snapshot.RefrigeratedStackCount > 0 ||
                snapshot.RefrigeratedThingCount > 0;
            if (hasStablePayload)
            {
                failureReason = "CT_Shuttle_Command_CannotModifyAssemblyWithCargo"
                    .Translate()
                    .ToString();
                return false;
            }

            return true;
        }
    }
}
