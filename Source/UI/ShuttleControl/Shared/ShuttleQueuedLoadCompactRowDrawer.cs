using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal sealed class ShuttleQueuedLoadCompactRowDrawer
    {
        private const float ActionRailWidth = 82f;

        internal V3QueuedLoadRowCommand Draw(Rect rect, V3QueuedLoadRowModel row)
        {
            if (row == null)
            {
                return V3QueuedLoadRowCommand.None;
            }

            bool disabled = !row.CanCancel;
            bool hovered = Mouse.IsOver(rect);
            V3CargoLoadDialogStyle.DrawRowBackground(rect, false, disabled);
            if (hovered)
            {
                Widgets.DrawHighlight(rect);
            }

            Rect iconRect = new Rect(rect.x + 7f, rect.y + 8f, 30f, 30f);
            V3CargoLoadDialogStyle.DrawThingIcon(iconRect, row.DisplayThing);

            float contentX = iconRect.xMax + 7f;
            float contentWidth = Mathf.Max(30f, rect.xMax - contentX - ActionRailWidth - 8f);
            Rect labelRect = new Rect(contentX, rect.y + 3f, contentWidth, 20f);
            Rect detailRect = new Rect(contentX, rect.y + 23f, contentWidth, 18f);
            Rect actionRailRect = new Rect(
                rect.xMax - ActionRailWidth - 6f,
                rect.y + 3f,
                ActionRailWidth,
                rect.height - 6f);

            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            ShuttleUILayout.SafeLabel(
                labelRect,
                V3CargoLoadDialogStyle.FitLabelText(
                    this.GetLabel(row),
                    labelRect.width));
            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.SafeLabel(
                detailRect,
                V3CargoLoadDialogStyle.FitLabelText(
                    this.GetDetailText(row),
                    detailRect.width));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            V3QueuedLoadRowCommand command = hovered
                ? this.DrawActions(actionRailRect, row)
                : this.DrawStatus(actionRailRect, row);
            if (hovered)
            {
                TooltipHandler.TipRegion(rect, this.GetTooltip(row));
            }

            return command;
        }

        private V3QueuedLoadRowCommand DrawActions(Rect rect, V3QueuedLoadRowModel row)
        {
            if (row == null)
            {
                return V3QueuedLoadRowCommand.None;
            }

            const float gap = 4f;
            const float partialWidth = 24f;
            bool hasPartialAction = !row.IsPawn && row.StackCount > 1;
            Rect cancelRect = hasPartialAction
                ? new Rect(
                    rect.x + partialWidth + gap,
                    rect.y,
                    Mathf.Max(0f, rect.width - partialWidth - gap),
                    rect.height)
                : rect;
            V3QueuedLoadRowCommand command = V3QueuedLoadRowCommand.None;
            if (hasPartialAction)
            {
                Rect partialRect = new Rect(rect.x, rect.y, partialWidth, rect.height);
                if (V3CargoLoadDialogStyle.DrawTextAction(
                    partialRect,
                    "-",
                    row.CanCancel,
                    V3CargoLoadDialogStyle.YellowColor,
                    V3CargoLoadText.Tr("CT_Shuttle_Cargo_QuantityModifierTooltip")))
                {
                    command = V3QueuedLoadRowCommand.CancelPartial;
                }
            }

            if (V3CargoLoadDialogStyle.DrawTextAction(
                cancelRect,
                V3CargoLoadText.Tr("CT_Shuttle_Cargo_CancelAll"),
                row.CanCancel,
                V3CargoLoadDialogStyle.RedColor,
                V3CargoLoadText.Tr("CT_Shuttle_QueuedLoad_EntryTooltip")))
            {
                command = V3QueuedLoadRowCommand.CancelAll;
            }

            return command;
        }

        private V3QueuedLoadRowCommand DrawStatus(Rect rect, V3QueuedLoadRowModel row)
        {
            ShuttleUILayout.DrawFittedSingleLineLabel(
                rect,
                row != null && row.CanCancel
                    ? V3CargoLoadText.Tr("CT_Shuttle_Cargo_Queued")
                    : V3CargoLoadText.Tr("CT_Shuttle_Cargo_QueuedEntryUnavailable"),
                GameFont.Tiny,
                GameFont.Tiny,
                row != null && row.CanCancel
                    ? V3CargoLoadDialogStyle.AccentColor
                    : V3CargoLoadDialogStyle.YellowColor,
                null,
                TextAnchor.MiddleRight);
            return V3QueuedLoadRowCommand.None;
        }

        private string GetLabel(V3QueuedLoadRowModel row)
        {
            if (row == null || string.IsNullOrEmpty(row.Label))
            {
                return V3CargoLoadText.Tr("CT_Shuttle_Cargo_QueuedEntryUnavailable");
            }

            return row.StackCount > 1
                ? row.Label + " " +
                    V3CargoLoadText.Tr("CT_Shuttle_Cargo_StackCountFormat", row.StackCount)
                : row.Label;
        }

        private string GetDetailText(V3QueuedLoadRowModel row)
        {
            float massKg = row != null ? Mathf.Max(0f, row.MassKg) : 0f;
            string region = row != null && row.CargoRegionIndex >= 0
                ? V3CargoLoadText.Tr("CT_Shuttle_Cargo_Region", row.CargoRegionIndex + 1)
                : V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_Unavailable");
            return ShuttleUIMetricFormatter.FormatKgCompact(massKg) +
                "  ·  " +
                region;
        }

        private string GetTooltip(V3QueuedLoadRowModel row)
        {
            if (row == null)
            {
                return string.Empty;
            }

            return this.GetLabel(row) + "\n" +
                this.GetDetailText(row) + "\n" +
                V3CargoLoadText.Tr("CT_Shuttle_QueuedLoad_EntryTooltip");
        }
    }
}
