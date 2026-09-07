using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.CargoUnloading
{
    internal sealed class ShuttleCargoUnloadSystem
    {
        private const int ProcessIntervalTicks = 15;
        private const int MaxEntriesPerPulse = 2;
        private const int HostUnavailableRetryTicks = 60;

        internal bool Tick(
            ThingWithComps host,
            ShuttleRuntimeState runtimeState,
            IShuttleCargoBackend cargoBackend,
            IShuttleCargoColdTransferService coldTransferService,
            int ticksGame)
        {
            if (runtimeState == null)
            {
                return false;
            }

            runtimeState.EnsureInitialized();
            ShuttleCargoUnloadState state = runtimeState.CargoUnload;
            if (state == null || !state.IsActive)
            {
                return false;
            }

            if (host == null || !host.Spawned || host.Map == null)
            {
                state.ScheduleNext(ticksGame + HostUnavailableRetryTicks);
                return false;
            }

            if (state.NextProcessTick > ticksGame)
            {
                return false;
            }

            bool changed = false;
            for (int i = 0; i < MaxEntriesPerPulse && state.IsActive; i++)
            {
                ShuttleCargoUnloadRecord record = state.PeekNext();
                if (record == null)
                {
                    break;
                }

                int unloadedCount;
                string failureReason;
                if (this.TryUnloadRecord(
                    host,
                    cargoBackend,
                    coldTransferService,
                    record,
                    out unloadedCount,
                    out failureReason))
                {
                    this.RemovePassengerBoardingIntent(runtimeState, record);
                    state.CompleteNext(unloadedCount);
                }
                else
                {
                    state.SkipNext(failureReason);
                }

                changed = true;
            }

            state.ScheduleNext(ticksGame + ProcessIntervalTicks);
            if (state.CompleteIfFinished(ticksGame))
            {
                this.NotifyCompleted(host, state);
                changed = true;
            }

            return changed;
        }

        private bool TryUnloadRecord(
            ThingWithComps host,
            IShuttleCargoBackend cargoBackend,
            IShuttleCargoColdTransferService coldTransferService,
            ShuttleCargoUnloadRecord record,
            out int unloadedCount,
            out string failureReason)
        {
            unloadedCount = 0;
            failureReason = null;
            if (record == null || !record.IsValid)
            {
                failureReason = "CT_Shuttle_Cargo_UnloadEntryUnavailable".Translate().ToString();
                return false;
            }

            if (record.SourceKind == ShuttleCargoUnloadSourceKind.LoadedCargo)
            {
                if (cargoBackend == null)
                {
                    failureReason = "CT_Shuttle_Command_TransporterBackendUnavailable".Translate().ToString();
                    return false;
                }

                return cargoBackend.UnloadLoadedCargoEntry(
                    host,
                    record.TransporterIndex,
                    record.LoadedIndex,
                    record.ThingIDNumber,
                    record.ExpectedDefName,
                    record.Count,
                    out unloadedCount,
                    out failureReason);
            }

            if (record.SourceKind == ShuttleCargoUnloadSourceKind.RefrigeratedCargo)
            {
                if (coldTransferService == null)
                {
                    failureReason = "CT_Shuttle_Command_RefrigeratedCargoServiceUnavailable".Translate().ToString();
                    return false;
                }

                return coldTransferService.TryUnloadColdCargoEntry(
                    record.ModuleInstanceID,
                    record.ColdIndex,
                    record.ThingIDNumber,
                    record.ExpectedDefName,
                    record.Count,
                    "global-unload",
                    out unloadedCount,
                    out failureReason);
            }

            failureReason = "CT_Shuttle_Cargo_UnloadEntryUnavailable".Translate().ToString();
            return false;
        }

        private void RemovePassengerBoardingIntent(
            ShuttleRuntimeState runtimeState,
            ShuttleCargoUnloadRecord record)
        {
            if (runtimeState == null ||
                record == null ||
                record.SourceKind != ShuttleCargoUnloadSourceKind.LoadedCargo ||
                record.ThingIDNumber <= 0 ||
                runtimeState.PassengerBoardingIntents == null)
            {
                return;
            }

            runtimeState.PassengerBoardingIntents.RemoveByThingID(
                record.ThingIDNumber);
        }

        private void NotifyCompleted(
            ThingWithComps host,
            ShuttleCargoUnloadState state)
        {
            if (state == null)
            {
                return;
            }

            string message = state.SkippedStackCount > 0
                ? "CT_Shuttle_Cargo_UnloadCompletedWithSkipped".Translate(
                    state.CompletedStackCount,
                    state.CompletedThingCount,
                    state.SkippedStackCount).ToString()
                : "CT_Shuttle_Cargo_UnloadCompleted".Translate(
                    state.CompletedStackCount,
                    state.CompletedThingCount).ToString();
            MessageTypeDef messageType = state.SkippedStackCount > 0
                ? MessageTypeDefOf.NeutralEvent
                : MessageTypeDefOf.PositiveEvent;
            Messages.Message(message, host, messageType, false);
        }
    }
}
