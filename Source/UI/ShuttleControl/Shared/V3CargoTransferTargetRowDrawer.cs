using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Cargo;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal sealed class V3CargoTransferTargetRowDrawer
    {
        internal bool Draw(
            Rect rect,
            V3CargoTransferSelectionModel model,
            int index)
        {
            ShuttleCargoBayActionTarget bay =
                model != null ? model.GetDestinationBay(index) : null;
            if (bay == null)
            {
                return false;
            }

            string blocker = model.GetBayBlocker(bay, model.Count);
            bool disabled = !string.IsNullOrEmpty(blocker);
            V3CargoTransferDialogStyle.DrawRowBackground(
                rect,
                model.IsSelectedDestination(index),
                disabled);

            Rect titleRect = new Rect(rect.x + 10f, rect.y + 7f, rect.width - 20f, 20f);
            Rect statusRect = new Rect(rect.x + 10f, rect.y + 30f, rect.width - 20f, 17f);
            Rect massRect = new Rect(rect.xMax - 150f, rect.y + 30f, 140f, 17f);

            Text.Font = GameFont.Small;
            GUI.color = disabled
                ? ShuttleUIStyle.MutedTextColor
                : V3CargoTransferDialogStyle.ColdColor;
            ShuttleUILayout.SafeLabel(
                titleRect,
                V3CargoTransferDialogStyle.FitLabelText(
                    this.GetBayLabel(bay),
                    titleRect.width));
            Text.Font = GameFont.Tiny;
            GUI.color = disabled
                ? V3CargoTransferDialogStyle.YellowColor
                : ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.SafeLabel(
                statusRect,
                V3CargoTransferDialogStyle.FitLabelText(
                    !string.IsNullOrEmpty(blocker) ? blocker : this.GetBayStatus(bay),
                    statusRect.width - 148f));
            GUI.color = Color.white;
            ShuttleUILayout.SafeLabel(
                massRect,
                V3CargoTransferDialogStyle.FitLabelText(
                    ShuttleUIMetricFormatter.FormatKgPair(
                        Mathf.Max(0f, bay.UsedMassKg),
                        Mathf.Max(0f, bay.CapacityKg)),
                    massRect.width));
            Text.Font = GameFont.Small;

            TooltipHandler.TipRegion(
                rect,
                this.GetBayLabel(bay) + "\n" +
                this.GetBayStatus(bay) + "\n" +
                ShuttleUIMetricFormatter.FormatKgPair(
                    Mathf.Max(0f, bay.UsedMassKg),
                    Mathf.Max(0f, bay.CapacityKg)) +
                (!string.IsNullOrEmpty(blocker) ? "\n" + blocker : string.Empty));

            Event evt = Event.current;
            if (evt != null &&
                evt.type == EventType.MouseDown &&
                evt.button == 0 &&
                rect.Contains(evt.mousePosition))
            {
                evt.Use();
                return true;
            }

            return false;
        }

        private string GetBayLabel(ShuttleCargoBayActionTarget bay)
        {
            return bay != null && !string.IsNullOrEmpty(bay.Label)
                ? bay.Label
                : V3CargoTransferText.Tr("CT_Shuttle_Cargo_Cold");
        }

        private string GetBayStatus(ShuttleCargoBayActionTarget bay)
        {
            return bay != null && !string.IsNullOrEmpty(bay.StatusText)
                ? bay.StatusText
                : V3CargoTransferText.Tr("CT_Shuttle_Cargo_Unknown");
        }
    }
}
