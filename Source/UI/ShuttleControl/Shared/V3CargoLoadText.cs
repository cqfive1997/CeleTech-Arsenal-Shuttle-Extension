using System;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal static class V3CargoLoadText
    {
        internal static string GetRowDetailText(
            V3CargoLoadSelectionModel selectionModel,
            TransferableOneWay transferable,
            V3CargoLoadTransferableMetrics metrics)
        {
            string type = metrics != null ? metrics.TypeLabel : "-";
            bool isPawn = metrics != null && metrics.DisplayThing is Pawn;
            if (isPawn)
            {
                return selectionModel != null &&
                    selectionModel.IsHeldPassenger(transferable)
                        ? Tr("CT_Shuttle_LoadCargo_HeldPassengerStatus")
                        : type;
            }

            string destination = GetDestinationText(selectionModel, transferable);
            return string.IsNullOrEmpty(destination) || destination == "-"
                ? string.Empty
                : destination;
        }

        internal static string GetDestinationText(
            V3CargoLoadSelectionModel selectionModel,
            TransferableOneWay transferable)
        {
            ShuttleCargoLoadAdmissionResult admission =
                selectionModel != null ? selectionModel.GetAdmission(transferable) : null;
            if (admission == null)
            {
                return "-";
            }

            if (admission.Kind == ShuttleCargoLoadAdmissionKind.RegularCargo)
            {
                return Tr("CT_Shuttle_LoadCargo_DestinationRegular");
            }

            string refrigeratedLabel = !string.IsNullOrEmpty(admission.RefrigeratedModuleLabel)
                ? admission.RefrigeratedModuleLabel
                : Tr("CT_Shuttle_Module_RefrigeratedCargo_Name");
            if (admission.Kind == ShuttleCargoLoadAdmissionKind.RefrigeratedAutoTransfer)
            {
                return Tr(
                    "CT_Shuttle_LoadCargo_DestinationRefrigerated",
                    refrigeratedLabel);
            }

            if (admission.Kind == ShuttleCargoLoadAdmissionKind.Both)
            {
                return Tr(
                    "CT_Shuttle_LoadCargo_DestinationColdPreferred",
                    refrigeratedLabel);
            }

            return Tr("CT_Shuttle_LoadCargo_DestinationUnavailable");
        }

        internal static string GetAdmissionReasonText(
            V3CargoLoadSelectionModel selectionModel,
            TransferableOneWay transferable)
        {
            ShuttleCargoLoadAdmissionResult admission =
                selectionModel != null ? selectionModel.GetAdmission(transferable) : null;
            if (admission == null)
            {
                return string.Empty;
            }

            if (!admission.Accepted)
            {
                return Tr("CT_Shuttle_LoadCargo_AdmissionRejected");
            }

            if (admission.Kind == ShuttleCargoLoadAdmissionKind.RegularCargo)
            {
                return Tr("CT_Shuttle_LoadCargo_AdmissionRegular");
            }

            if (admission.Kind == ShuttleCargoLoadAdmissionKind.RefrigeratedAutoTransfer)
            {
                return Tr("CT_Shuttle_LoadCargo_AdmissionRefrigerated");
            }

            return Tr("CT_Shuttle_LoadCargo_AdmissionColdPreferred");
        }

        internal static string GetAvailabilityStatusText(bool selected, bool canAddOne)
        {
            if (selected)
            {
                return Tr("CT_Shuttle_LoadCargo_Selected");
            }

            return canAddOne
                ? Tr("CT_Shuttle_LoadCargo_Loadable")
                : Tr("CT_Shuttle_LoadCargo_OverMass");
        }

        internal static string GetCountMassText(
            TransferableOneWay transferable,
            V3CargoLoadTransferableMetrics metrics)
        {
            int selectedCount = transferable != null ? transferable.CountToTransfer : 0;
            string countText = Tr(
                "CT_Shuttle_Cargo_CountFormat",
                selectedCount,
                FormatCount(metrics));
            float selectedMass = V3CargoLoadTransferableMetricsResolver.CalculateMass(
                metrics != null ? metrics.UnitMass : 0f,
                selectedCount);
            return countText + " / " +
                Tr("CT_Shuttle_Cargo_Mass") + " " +
                ShuttleUIMetricFormatter.FormatKgCompact(selectedMass);
        }

        internal static string GetSelectedRowCountText(
            TransferableOneWay transferable,
            V3CargoLoadTransferableMetrics metrics)
        {
            int selectedCount = transferable != null ? transferable.CountToTransfer : 0;
            float selectedMass = V3CargoLoadTransferableMetricsResolver.CalculateMass(
                metrics != null ? metrics.UnitMass : 0f,
                selectedCount);
            return Tr("CT_Shuttle_Cargo_StackCountFormat", selectedCount) +
                " / " +
                ShuttleUIMetricFormatter.FormatKgCompact(selectedMass);
        }

        internal static string GetTransferableTooltip(
            V3CargoLoadSelectionModel selectionModel,
            TransferableOneWay transferable,
            V3CargoLoadTransferableMetrics metrics)
        {
            ShuttleCargoLoadAdmissionResult admission =
                selectionModel != null ? selectionModel.GetAdmission(transferable) : null;
            string details = metrics != null
                ? metrics.Label + "\n" +
                    metrics.TypeLabel + "\n" +
                    GetCountMassText(transferable, metrics)
                : "-";
            if (admission != null)
            {
                string destination = GetDestinationText(selectionModel, transferable);
                if (!string.IsNullOrEmpty(destination) && destination != "-")
                {
                    details += "\n" + destination;
                }

                string reason = GetAdmissionReasonText(selectionModel, transferable);
                if (!string.IsNullOrEmpty(reason))
                {
                    details += "\n" + reason;
                }
            }

            if (selectionModel != null &&
                selectionModel.IsHeldPassenger(transferable))
            {
                details += "\n" +
                    Tr("CT_Shuttle_LoadCargo_HeldPassengerTooltip");
            }

            return details + "\n" + Tr("CT_Shuttle_LoadCargo_SelectionTooltip");
        }

        internal static string GetSelectedSummaryText(V3CargoLoadSelectionModel selectionModel)
        {
            if (selectionModel == null)
            {
                return "-";
            }

            return Tr(
                "CT_Shuttle_LoadCargo_CompactSelectedSummary",
                selectionModel.SelectedEntryCount(),
                selectionModel.SelectedCount(),
                ShuttleUIMetricFormatter.FormatKgCompact(selectionModel.SelectedMass()));
        }

        internal static string GetAllowedSummaryText(
            V3CargoLoadSelectionModel selectionModel,
            int passengerCount,
            int cargoCount)
        {
            if (selectionModel == null)
            {
                return "-";
            }

            return Tr(
                "CT_Shuttle_UI_LoadCargoAllowedSummary",
                passengerCount,
                cargoCount,
                ShuttleUIMetricFormatter.FormatKgCompact(selectionModel.SelectedMass()),
                ShuttleUIMetricFormatter.FormatKgCompact(selectionModel.MassCapacity()));
        }

        internal static string GetCapacitySummaryText(V3CargoLoadSelectionModel selectionModel)
        {
            if (selectionModel == null)
            {
                return "-";
            }

            return Tr(
                "CT_Shuttle_LoadCargo_CompactCapacitySummary",
                ShuttleUIMetricFormatter.FormatKgCompact(selectionModel.SelectedMass()),
                ShuttleUIMetricFormatter.FormatKgCompact(selectionModel.AvailableMass()),
                ShuttleUIMetricFormatter.FormatKgCompact(selectionModel.RemainingMass()));
        }

        internal static string GetCurrentLoadSummary(ShuttleLoadCargoReadModel readModel)
        {
            if (readModel == null)
            {
                return "-";
            }

            return Tr(
                "CT_Shuttle_UI_CurrentLoadSummary",
                readModel.LoadedStackCount,
                readModel.LoadedThingCount,
                readModel.QueuedStackCount,
                readModel.QueuedThingCount,
                ShuttleUIMetricFormatter.FormatKgCompact(
                    readModel.ExistingMassUsageKg +
                    readModel.RefrigeratedAutoTransferUsedMassKg),
                ShuttleUIMetricFormatter.FormatKgCompact(
                    readModel.RefrigeratedMassSharesOverallCapacity
                        ? readModel.MassCapacityKg
                        : readModel.MassCapacityKg +
                            readModel.RefrigeratedAutoTransferCapacityKg));
        }

        internal static string GetCurrentQueueChipText(ShuttleLoadCargoReadModel readModel)
        {
            if (readModel == null)
            {
                return "-";
            }

            return Tr("CT_Shuttle_LoadCargo_CurrentPrefix") +
                readModel.LoadedStackCount +
                " / " +
                Tr("CT_Shuttle_LoadCargo_QueuePrefix") +
                readModel.QueuedStackCount;
        }

        internal static string GetCurrentLoadPreviewText(ShuttleLoadCargoReadModel readModel)
        {
            if (readModel == null ||
                readModel.CurrentLoadPreviewLines == null ||
                readModel.CurrentLoadPreviewLines.Count == 0)
            {
                return string.Empty;
            }

            return string.Join("   ", readModel.CurrentLoadPreviewLines.ToArray());
        }

        internal static string FormatCount(V3CargoLoadTransferableMetrics metrics)
        {
            if (metrics == null)
            {
                return "0";
            }

            return metrics.RawCountLong > int.MaxValue
                ? int.MaxValue + "+"
                : metrics.MaxCount.ToString();
        }

        internal static string Tr(string key)
        {
            return ShuttleUIText.Tr(key);
        }

        internal static string Tr(string key, object arg0)
        {
            return ShuttleUIText.Tr(key, arg0);
        }

        internal static string Tr(string key, object arg0, object arg1)
        {
            return ShuttleUIText.Tr(key, arg0, arg1);
        }

        internal static string Tr(string key, object arg0, object arg1, object arg2)
        {
            return ShuttleUIText.Tr(key, arg0, arg1, arg2);
        }

        internal static string Tr(string key, object arg0, object arg1, object arg2, object arg3)
        {
            string text = ShuttleUIText.Tr(key);
            try
            {
                return string.Format(text, arg0, arg1, arg2, arg3);
            }
            catch (FormatException)
            {
                return text;
            }
        }

        internal static string Tr(
            string key,
            object arg0,
            object arg1,
            object arg2,
            object arg3,
            object arg4,
            object arg5)
        {
            string text = ShuttleUIText.Tr(key);
            try
            {
                return string.Format(text, arg0, arg1, arg2, arg3, arg4, arg5);
            }
            catch (FormatException)
            {
                return text;
            }
        }
    }
}
