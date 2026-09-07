using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Cargo;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal static class V3CargoTransferText
    {
        internal static string GetTitle(V3CargoTransferSelectionModel model)
        {
            return model != null && model.IsToLoadedCargo()
                ? Tr("CT_Shuttle_Cargo_Action_TransferToNormal")
                : Tr("CT_Shuttle_Cargo_Action_TransferToCold");
        }

        internal static string GetDirectionText(V3CargoTransferSelectionModel model)
        {
            if (model == null)
            {
                return Tr("CT_Shuttle_Cargo_Unknown");
            }

            return GetSourceText(model.Stack) + " > " + GetDestinationText(model);
        }

        internal static string GetDestinationText(V3CargoTransferSelectionModel model)
        {
            if (model == null)
            {
                return Tr("CT_Shuttle_Cargo_Unknown");
            }

            if (model.IsToLoadedCargo())
            {
                return Tr("CT_Shuttle_Cargo_SourceLoaded");
            }

            ShuttleCargoBayActionTarget bay = model.GetSelectedDestinationBay();
            return bay != null && !string.IsNullOrEmpty(bay.Label)
                ? bay.Label
                : Tr("CT_Shuttle_Cargo_Cold");
        }

        internal static string GetSourceText(ShuttleCargoStackActionTarget stack)
        {
            if (stack != null &&
                stack.SourceKind == ShuttleCargoStackActionSourceKind.RefrigeratedCargo)
            {
                return Tr("CT_Shuttle_Cargo_SourceRefrigerated");
            }

            if (stack != null &&
                stack.SourceKind == ShuttleCargoStackActionSourceKind.LoadedCargo)
            {
                return Tr("CT_Shuttle_Cargo_SourceLoaded");
            }

            return Tr("CT_Shuttle_Cargo_Unknown");
        }

        internal static string GetStackLabel(ShuttleCargoStackActionTarget stack)
        {
            return stack != null && !string.IsNullOrEmpty(stack.Label)
                ? stack.Label
                : Tr("CT_Shuttle_Cargo_LoadedEntryUnavailable");
        }

        internal static string GetStackMassText(ShuttleCargoStackActionTarget stack)
        {
            int count = stack != null ? Mathf.Max(0, stack.StackCount) : 0;
            float mass = stack != null ? Mathf.Max(0f, stack.MassKg) : 0f;
            return Tr(
                "CT_Shuttle_Cargo_StackMassFormat",
                count,
                mass.ToString("0.#"));
        }

        internal static string GetCountPreviewText(
            V3CargoTransferSelectionModel model)
        {
            return Tr(
                "CT_Shuttle_Cargo_CountFormat",
                model != null ? model.Count : 0,
                model != null ? model.MaxCount() : 0);
        }

        internal static string GetSelectedMassText(
            V3CargoTransferSelectionModel model)
        {
            return Tr("CT_Shuttle_Cargo_Mass") + " " +
                ShuttleUIMetricFormatter.FormatKgCompact(
                    model != null ? model.SelectedMassKg() : 0f);
        }

        internal static string GetCapacityText(V3CargoTransferSelectionModel model)
        {
            if (model == null)
            {
                return Tr("CT_Shuttle_Cargo_CapacityUnavailable");
            }

            return Tr("CT_Shuttle_Cargo_Mass") + " " +
                ShuttleUIMetricFormatter.FormatKgPair(
                    model.DestinationUsedMassKg(),
                    model.DestinationCapacityKg()) +
                " / " +
                ShuttleUIMetricFormatter.FormatKgCompact(
                    model.DestinationAvailableMassKg());
        }

        internal static string GetDestinationStatusText(
            V3CargoTransferSelectionModel model)
        {
            if (model == null)
            {
                return Tr("CT_Shuttle_Cargo_Unknown");
            }

            if (model.IsToLoadedCargo())
            {
                return Tr("CT_Shuttle_Cargo_SourceLoaded");
            }

            ShuttleCargoBayActionTarget bay = model.GetSelectedDestinationBay();
            return bay != null && !string.IsNullOrEmpty(bay.StatusText)
                ? bay.StatusText
                : Tr("CT_Shuttle_Cargo_Unknown");
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
    }
}
