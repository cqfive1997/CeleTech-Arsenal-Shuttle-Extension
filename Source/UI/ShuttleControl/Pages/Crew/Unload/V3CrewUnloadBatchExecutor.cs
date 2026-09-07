using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Crew;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Crew
{
    internal sealed class V3CrewUnloadBatchExecutor
    {
        internal V3CrewUnloadBatchResult ExecuteCrew(
            List<V3CrewUnloadOption> selectedOptions,
            IShuttleCrewLoadedCrewUIActions loadedCrewActions,
            IShuttleCrewHabitatUIActions habitatActions,
            IShuttleCrewMechChargerUIActions mechChargerActions)
        {
            if (selectedOptions == null || selectedOptions.Count == 0)
            {
                return V3CrewUnloadBatchResult.Failed(
                    "CT_Shuttle_Crew_UnloadSelectionChanged".Translate().ToString(),
                    false);
            }

            if (!this.ValidateCrew(
                    selectedOptions,
                    loadedCrewActions,
                    habitatActions,
                    mechChargerActions))
            {
                return V3CrewUnloadBatchResult.Failed(
                    "CT_Shuttle_Crew_UnloadSelectionChanged".Translate().ToString(),
                    false);
            }

            List<V3CrewUnloadOption> cargoOptions = this.GetCargoOptions(selectedOptions);
            List<V3CrewUnloadOption> habitatOptions = this.GetHabitatOptions(selectedOptions);
            List<V3CrewUnloadOption> mechChargerOptions =
                this.GetMechChargerOptions(selectedOptions);
            cargoOptions.Sort(CompareCargoDescending);

            int completedCount = 0;
            int requestedCount = selectedOptions.Count;
            string failureMessage;
            if (!this.ExecuteCargoOptions(
                    cargoOptions,
                    loadedCrewActions,
                    requestedCount,
                    ref completedCount,
                    out failureMessage))
            {
                return V3CrewUnloadBatchResult.Failed(failureMessage, true);
            }

            if (!this.ExecuteHabitatOptions(
                    habitatOptions,
                    habitatActions,
                    requestedCount,
                    ref completedCount,
                    out failureMessage))
            {
                return V3CrewUnloadBatchResult.Failed(failureMessage, true);
            }

            if (!this.ExecuteMechChargerOptions(
                    mechChargerOptions,
                    mechChargerActions,
                    requestedCount,
                    ref completedCount,
                    out failureMessage))
            {
                return V3CrewUnloadBatchResult.Failed(failureMessage, true);
            }

            return V3CrewUnloadBatchResult.Succeeded(
                "CT_Shuttle_Crew_UnloadCompleted".Translate(selectedOptions.Count.ToString()).ToString(),
                true);
        }

        private bool ValidateCrew(
            List<V3CrewUnloadOption> options,
            IShuttleCrewLoadedCrewUIActions loadedCrewActions,
            IShuttleCrewHabitatUIActions habitatActions,
            IShuttleCrewMechChargerUIActions mechChargerActions)
        {
            for (int i = 0; i < options.Count; i++)
            {
                if (!this.ValidateCrewOption(
                        options[i],
                        loadedCrewActions,
                        habitatActions,
                        mechChargerActions))
                {
                    return false;
                }
            }

            return true;
        }

        private bool ValidateCrewOption(
            V3CrewUnloadOption option,
            IShuttleCrewLoadedCrewUIActions loadedCrewActions,
            IShuttleCrewHabitatUIActions habitatActions,
            IShuttleCrewMechChargerUIActions mechChargerActions)
        {
            if (option == null || !option.CanUnload || option.Card == null)
            {
                return false;
            }

            V3CrewCardModel card = option.Card;
            if (V3CrewUnloadSelectionBuilder.IsLoadedCrewSource(card.SourceKind))
            {
                return loadedCrewActions != null &&
                    loadedCrewActions.CanUnloadLoadedCrew(
                        card.TransporterIndex,
                        card.LoadedIndex,
                        card.ThingIDNumber,
                        card.DefName);
            }

            if (card.SourceKind == V3CrewCardSourceKind.Habitat)
            {
                return habitatActions != null &&
                    habitatActions.CanEjectHabitatOccupant(card.ThingIDNumber);
            }

            if (card.SourceKind == V3CrewCardSourceKind.MechCharger)
            {
                return mechChargerActions != null &&
                    mechChargerActions.CanEjectChargingMech(card.ThingIDNumber);
            }

            return false;
        }

        private List<V3CrewUnloadOption> GetCargoOptions(List<V3CrewUnloadOption> options)
        {
            List<V3CrewUnloadOption> result = new List<V3CrewUnloadOption>();
            for (int i = 0; i < options.Count; i++)
            {
                V3CrewUnloadOption option = options[i];
                if (option != null &&
                    option.Card != null &&
                    V3CrewUnloadSelectionBuilder.IsLoadedCrewSource(option.Card.SourceKind))
                {
                    result.Add(option);
                }
            }

            return result;
        }

        private List<V3CrewUnloadOption> GetHabitatOptions(List<V3CrewUnloadOption> options)
        {
            List<V3CrewUnloadOption> result = new List<V3CrewUnloadOption>();
            for (int i = 0; i < options.Count; i++)
            {
                V3CrewUnloadOption option = options[i];
                if (option != null &&
                    option.Card != null &&
                    option.Card.SourceKind == V3CrewCardSourceKind.Habitat)
                {
                    result.Add(option);
                }
            }

            return result;
        }

        private List<V3CrewUnloadOption> GetMechChargerOptions(
            List<V3CrewUnloadOption> options)
        {
            List<V3CrewUnloadOption> result = new List<V3CrewUnloadOption>();
            for (int i = 0; i < options.Count; i++)
            {
                V3CrewUnloadOption option = options[i];
                if (option != null &&
                    option.Card != null &&
                    option.Card.SourceKind == V3CrewCardSourceKind.MechCharger)
                {
                    result.Add(option);
                }
            }

            return result;
        }

        private bool ExecuteCargoOptions(
            List<V3CrewUnloadOption> options,
            IShuttleCrewLoadedCrewUIActions loadedCrewActions,
            int requestedCount,
            ref int completedCount,
            out string failureMessage)
        {
            failureMessage = null;
            for (int i = 0; i < options.Count; i++)
            {
                V3CrewCardModel card = options[i].Card;
                if (loadedCrewActions == null ||
                    !loadedCrewActions.UnloadLoadedCrew(
                        card.TransporterIndex,
                        card.LoadedIndex,
                        card.ThingIDNumber,
                        card.DefName))
                {
                    failureMessage = this.FormatExecutionFailure(completedCount, requestedCount);
                    return false;
                }

                completedCount++;
            }

            return true;
        }

        private bool ExecuteHabitatOptions(
            List<V3CrewUnloadOption> options,
            IShuttleCrewHabitatUIActions habitatActions,
            int requestedCount,
            ref int completedCount,
            out string failureMessage)
        {
            failureMessage = null;
            for (int i = 0; i < options.Count; i++)
            {
                V3CrewCardModel card = options[i].Card;
                if (habitatActions == null ||
                    !habitatActions.EjectHabitatOccupant(card.ThingIDNumber))
                {
                    failureMessage = this.FormatExecutionFailure(completedCount, requestedCount);
                    return false;
                }

                completedCount++;
            }

            return true;
        }

        private bool ExecuteMechChargerOptions(
            List<V3CrewUnloadOption> options,
            IShuttleCrewMechChargerUIActions mechChargerActions,
            int requestedCount,
            ref int completedCount,
            out string failureMessage)
        {
            failureMessage = null;
            for (int i = 0; i < options.Count; i++)
            {
                V3CrewCardModel card = options[i].Card;
                if (mechChargerActions == null ||
                    !mechChargerActions.EjectChargingMech(card.ThingIDNumber))
                {
                    failureMessage = this.FormatExecutionFailure(completedCount, requestedCount);
                    return false;
                }

                completedCount++;
            }

            return true;
        }

        private string FormatExecutionFailure(int completedCount, int requestedCount)
        {
            return "CT_Shuttle_Crew_UnloadBatchFailed".Translate(
                completedCount.ToString(),
                requestedCount.ToString()).ToString();
        }

        private static int CompareCargoDescending(
            V3CrewUnloadOption left,
            V3CrewUnloadOption right)
        {
            V3CrewCardModel leftCard = left != null ? left.Card : null;
            V3CrewCardModel rightCard = right != null ? right.Card : null;
            int leftTransporter = leftCard != null ? leftCard.TransporterIndex : -1;
            int rightTransporter = rightCard != null ? rightCard.TransporterIndex : -1;
            int transporterResult = rightTransporter.CompareTo(leftTransporter);
            if (transporterResult != 0)
            {
                return transporterResult;
            }

            int leftLoaded = leftCard != null ? leftCard.LoadedIndex : -1;
            int rightLoaded = rightCard != null ? rightCard.LoadedIndex : -1;
            return rightLoaded.CompareTo(leftLoaded);
        }
    }

    internal sealed class V3CrewUnloadBatchResult
    {
        internal readonly bool Success;
        internal readonly bool ShouldCloseDialog;
        internal readonly string Message;
        internal readonly MessageTypeDef MessageType;

        private V3CrewUnloadBatchResult(
            bool success,
            bool shouldCloseDialog,
            string message,
            MessageTypeDef messageType)
        {
            this.Success = success;
            this.ShouldCloseDialog = shouldCloseDialog;
            this.Message = message;
            this.MessageType = messageType;
        }

        internal static V3CrewUnloadBatchResult Succeeded(
            string message,
            bool shouldCloseDialog)
        {
            return new V3CrewUnloadBatchResult(
                true,
                shouldCloseDialog,
                message,
                MessageTypeDefOf.PositiveEvent);
        }

        internal static V3CrewUnloadBatchResult Failed(
            string message,
            bool shouldCloseDialog)
        {
            return new V3CrewUnloadBatchResult(
                false,
                shouldCloseDialog,
                message,
                MessageTypeDefOf.RejectInput);
        }
    }
}
