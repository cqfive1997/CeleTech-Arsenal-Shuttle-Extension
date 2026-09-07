using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Commands
{
    /// <summary>
    /// Handles shuttle-owned cargo commands while keeping vanilla transporter internals behind
    /// IShuttleCargoBackend.
    /// </summary>
    internal sealed class ShuttleCargoCommandHandler : IShuttleCommandHandler
    {
        public bool CanHandle(IShuttleCommand command)
        {
            return command is BeginLoadCargoCommand ||
                   command is ClearQueuedLoadCommand ||
                   command is CancelQueuedLoadEntryCommand ||
                   command is UnloadLoadedCargoEntryCommand ||
                   command is UnloadLoadedCargoBayCommand ||
                   command is UpdateCargoRegionSettingsCommand;
        }

        public ShuttleCommandResult Execute(IShuttleCommand command, ShuttleCommandContext context)
        {
            if (context == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ContextUnavailable".Translate().ToString());
            }

            BeginLoadCargoCommand beginLoadCargo = command as BeginLoadCargoCommand;
            if (beginLoadCargo != null)
            {
                return this.ExecuteBeginLoadCargo(context, beginLoadCargo);
            }

            ClearQueuedLoadCommand clearQueuedLoad = command as ClearQueuedLoadCommand;
            if (clearQueuedLoad != null)
            {
                return this.ExecuteClearQueuedLoad(context, clearQueuedLoad);
            }

            CancelQueuedLoadEntryCommand cancelQueuedLoadEntry = command as CancelQueuedLoadEntryCommand;
            if (cancelQueuedLoadEntry != null)
            {
                return this.ExecuteCancelQueuedLoadEntry(context, cancelQueuedLoadEntry);
            }

            UnloadLoadedCargoEntryCommand unloadLoadedCargoEntry = command as UnloadLoadedCargoEntryCommand;
            if (unloadLoadedCargoEntry != null)
            {
                return this.ExecuteUnloadLoadedCargoEntry(context, unloadLoadedCargoEntry);
            }

            UnloadLoadedCargoBayCommand unloadLoadedCargoBay =
                command as UnloadLoadedCargoBayCommand;
            if (unloadLoadedCargoBay != null)
            {
                return this.ExecuteUnloadLoadedCargoBay(context, unloadLoadedCargoBay);
            }

            UpdateCargoRegionSettingsCommand updateCargoRegionSettings = command as UpdateCargoRegionSettingsCommand;
            if (updateCargoRegionSettings != null)
            {
                return this.ExecuteUpdateCargoRegionSettings(context, updateCargoRegionSettings);
            }

            return ShuttleCommandResult.Failed("CT_Shuttle_Command_UnsupportedCargo".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteBeginLoadCargo(ShuttleCommandContext context, BeginLoadCargoCommand command)
        {
            if (command == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_BeginLoadCargoMissing".Translate().ToString());
            }

            if (this.HasActiveGlobalUnload(context))
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_Cargo_LoadBlockedByUnloading".Translate().ToString());
            }

            string failureReason = null;
            ShuttleProfile currentProfile = context.ReconcileProfileToHost();
            if (context.CargoBackend == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_TransporterBackendUnavailable".Translate().ToString());
            }

            ShuttleCargoSnapshot cargoSnapshot = context.BuildCargoSnapshot();
            ShuttleRuntimeState runtimeState = context.GetRuntimeState();
            ShuttlePassengerBoardingSelection passengerSelection =
                ShuttlePassengerBoardingSelection.Build(
                    context.Host,
                    command.PassengerTransferables);
            float heldPassengerMassKg =
                this.GetPawnMassKg(passengerSelection.HeldPawns);
            float reservedExternalMassKg = cargoSnapshot != null
                ? cargoSnapshot.ExternalRuntimeMassKg
                : 0f;
            reservedExternalMassKg += heldPassengerMassKg;

            int ordinarySelectedCount =
                context.CargoBackend.GetSelectedCount(
                    passengerSelection.QueueTransferables,
                    command.CargoTransferables);
            if (ordinarySelectedCount <= 0)
            {
                if (passengerSelection.HeldPawns.Count <= 0)
                {
                    return ShuttleCommandResult.Failed(
                        "CT_Shuttle_Cargo_NoCargoSelected".Translate().ToString());
                }

                if (!this.CanFitHeldPassengerPlan(
                    cargoSnapshot,
                    heldPassengerMassKg,
                    command.ReplaceExistingQueue))
                {
                    return ShuttleCommandResult.Failed(
                        "CT_Shuttle_Cargo_SelectedExceedsCapacity".Translate().ToString());
                }

                if (command.ReplaceExistingQueue &&
                    !context.CargoBackend.ClearQueuedLoad(
                        context.Host,
                        out failureReason))
                {
                    return ShuttleCommandResult.Failed(failureReason);
                }
            }
            else if (!context.CargoBackend.BeginLoading(
                context.Host,
                currentProfile,
                context.AssemblyState,
                runtimeState,
                passengerSelection.QueueTransferables,
                command.CargoTransferables,
                command.ReplaceExistingQueue,
                reservedExternalMassKg,
                out failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            if (runtimeState != null &&
                runtimeState.PassengerBoardingIntents != null)
            {
                runtimeState.PassengerBoardingIntents.Replace(
                    passengerSelection.HeldPawns);
            }

            return ShuttleCommandResult.Succeeded("CT_Shuttle_Command_LoadingStarted".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteClearQueuedLoad(ShuttleCommandContext context, ClearQueuedLoadCommand command)
        {
            string failureReason = null;
            context.ReconcileProfileToHost();
            if (context.CargoBackend == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_TransporterBackendUnavailable".Translate().ToString());
            }

            if (!context.CargoBackend.ClearQueuedLoad(context.Host, out failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            ShuttleRuntimeState runtimeState = context.GetRuntimeState();
            if (runtimeState != null &&
                runtimeState.PassengerBoardingIntents != null)
            {
                runtimeState.PassengerBoardingIntents.Clear();
            }

            return ShuttleCommandResult.Succeeded("CT_Shuttle_Command_QueuedLoadCleared".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteCancelQueuedLoadEntry(ShuttleCommandContext context, CancelQueuedLoadEntryCommand command)
        {
            if (command == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_CancelQueuedLoadMissing".Translate().ToString());
            }

            string failureReason = null;
            context.ReconcileProfileToHost();
            if (context.CargoBackend == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_TransporterBackendUnavailable".Translate().ToString());
            }

            int queuedPawnThingID = this.FindQueuedPawnThingID(
                context.BuildCargoSnapshot(),
                command.TransporterIndex,
                command.QueueIndex);
            if (!context.CargoBackend.CancelQueuedLoadEntry(
                    context.Host,
                    context.GetRuntimeState(),
                    command.TransporterIndex,
                    command.QueueIndex,
                    command.Count,
                    out failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            ShuttleRuntimeState runtimeState = context.GetRuntimeState();
            if (queuedPawnThingID >= 0 &&
                runtimeState != null &&
                runtimeState.PassengerBoardingIntents != null)
            {
                runtimeState.PassengerBoardingIntents.RemoveByThingID(
                    queuedPawnThingID);
            }

            return ShuttleCommandResult.Succeeded("CT_Shuttle_Command_QueuedLoadEntryCanceled".Translate().ToString());
        }

        private bool CanFitHeldPassengerPlan(
            ShuttleCargoSnapshot cargoSnapshot,
            float heldPassengerMassKg,
            bool replaceExistingQueue)
        {
            if (cargoSnapshot == null ||
                float.IsNaN(heldPassengerMassKg) ||
                float.IsInfinity(heldPassengerMassKg) ||
                heldPassengerMassKg < 0f)
            {
                return false;
            }

            float existingMassKg =
                cargoSnapshot.LoadedMassKg +
                cargoSnapshot.ExternalRuntimeMassKg;
            if (!replaceExistingQueue)
            {
                existingMassKg += cargoSnapshot.QueuedMassKg;
            }

            return existingMassKg + heldPassengerMassKg <=
                cargoSnapshot.MassCapacity + 0.001f;
        }

        private float GetPawnMassKg(List<Pawn> pawns)
        {
            float massKg = 0f;
            if (pawns == null)
            {
                return massKg;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn != null)
                {
                    float pawnMassKg =
                        pawn.GetStatValue(StatDefOf.Mass, true, -1);
                    if (pawnMassKg > 0f &&
                        !float.IsNaN(pawnMassKg) &&
                        !float.IsInfinity(pawnMassKg))
                    {
                        massKg += pawnMassKg;
                    }
                }
            }

            return massKg;
        }

        private int FindQueuedPawnThingID(
            ShuttleCargoSnapshot snapshot,
            int transporterIndex,
            int queueIndex)
        {
            if (snapshot == null || snapshot.Items == null)
            {
                return -1;
            }

            for (int i = 0; i < snapshot.Items.Count; i++)
            {
                ShuttleCargoItemSnapshot item = snapshot.Items[i];
                if (item != null &&
                    item.IsAssignedToLoad &&
                    item.IsPawn &&
                    item.TransporterIndex == transporterIndex &&
                    item.QueueIndex == queueIndex)
                {
                    return item.ThingIDNumber;
                }
            }

            return -1;
        }

        private ShuttleCommandResult ExecuteUnloadLoadedCargoEntry(ShuttleCommandContext context, UnloadLoadedCargoEntryCommand command)
        {
            if (command == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_UnloadCargoMissing".Translate().ToString());
            }

            if (this.HasActiveGlobalUnload(context))
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_Cargo_ManualMoveBlockedByUnloading".Translate().ToString());
            }

            string failureReason = null;
            context.ReconcileProfileToHost();
            if (context.CargoBackend == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_TransporterBackendUnavailable".Translate().ToString());
            }

            if (!context.CargoBackend.UnloadLoadedCargoEntry(
                    context.Host,
                    command.TransporterIndex,
                    command.LoadedIndex,
                    command.ThingIDNumber,
                    command.DefName,
                    command.Count,
                    out failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            this.RemovePassengerBoardingIntent(
                context.GetRuntimeState(),
                command.ThingIDNumber);
            return ShuttleCommandResult.Succeeded("CT_Shuttle_Command_CargoUnloaded".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteUnloadLoadedCargoBay(
            ShuttleCommandContext context,
            UnloadLoadedCargoBayCommand command)
        {
            if (command == null || command.Entries == null || command.Entries.Count == 0)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Cargo_BayUnloadNoContents".Translate().ToString());
            }

            if (this.HasActiveGlobalUnload(context))
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_Cargo_ManualMoveBlockedByUnloading".Translate().ToString());
            }

            string failureReason = null;
            context.ReconcileProfileToHost();
            if (context.CargoBackend == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_TransporterBackendUnavailable".Translate().ToString());
            }

            int unloadedStackCount;
            int unloadedThingCount;
            if (!context.CargoBackend.UnloadLoadedCargoBay(
                    context.Host,
                    this.BuildLoadedCargoUnloadTargets(command.Entries),
                    out unloadedStackCount,
                    out unloadedThingCount,
                    out failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            return ShuttleCommandResult.Succeeded(
                "CT_Shuttle_Command_CargoBayUnloaded".Translate(
                    unloadedStackCount,
                    unloadedThingCount).ToString());
        }

        private ShuttleCommandResult ExecuteUpdateCargoRegionSettings(ShuttleCommandContext context, UpdateCargoRegionSettingsCommand command)
        {
            if (command == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_CargoRegionSettingsMissing".Translate().ToString());
            }

            ShuttleProfile currentProfile = context.GetProfileForRead();
            int activeRegionCount = currentProfile != null && currentProfile.Cargo != null
                ? (int)currentProfile.Cargo.CargoRegionCount
                : 0;
            if (command.RegionIndex < 0 || command.RegionIndex >= activeRegionCount)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_CargoRegionUnavailable".Translate().ToString());
            }

            if (context.AssemblyState == null || context.AssemblyState.CargoRegionConfig == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_CargoRegionSettingsUnavailable".Translate().ToString());
            }

            context.AssemblyState.CargoRegionConfig.EnsureRegionCount(activeRegionCount);
            ShuttleCargoRegionSettings settings = context.AssemblyState.CargoRegionConfig.GetRegion(command.RegionIndex);
            if (settings == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_CargoRegionSettingsUnavailable".Translate().ToString());
            }

            settings.Label = command.Label;
            settings.AllowHumans = true;
            settings.AllowAnimals = true;
            settings.AllowMechs = true;
            if (command.ItemFilter != null)
            {
                settings.SetItemFilter(command.ItemFilter);
            }

            return ShuttleCommandResult.Succeeded("CT_Shuttle_Command_CargoRegionSettingsUpdated".Translate().ToString());
        }

        private void RemovePassengerBoardingIntent(
            ShuttleRuntimeState runtimeState,
            int pawnThingID)
        {
            if (pawnThingID <= 0 ||
                runtimeState == null ||
                runtimeState.PassengerBoardingIntents == null)
            {
                return;
            }

            runtimeState.PassengerBoardingIntents.RemoveByThingID(pawnThingID);
        }

        private List<ShuttleLoadedCargoUnloadTarget> BuildLoadedCargoUnloadTargets(
            List<UnloadLoadedCargoBayEntry> entries)
        {
            List<ShuttleLoadedCargoUnloadTarget> targets =
                new List<ShuttleLoadedCargoUnloadTarget>();
            for (int i = 0; entries != null && i < entries.Count; i++)
            {
                UnloadLoadedCargoBayEntry entry = entries[i];
                if (entry != null)
                {
                    targets.Add(new ShuttleLoadedCargoUnloadTarget(
                        entry.TransporterIndex,
                        entry.LoadedIndex,
                        entry.ThingIDNumber,
                        entry.DefName,
                        entry.Count));
                }
            }

            return targets;
        }

        private bool HasActiveGlobalUnload(ShuttleCommandContext context)
        {
            ShuttleRuntimeState runtimeState = context != null
                ? context.GetRuntimeState()
                : null;
            return runtimeState != null &&
                runtimeState.CargoUnload != null &&
                runtimeState.CargoUnload.IsActive;
        }
    }
}
