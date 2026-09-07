using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal enum V3QueuedLoadRowCommand
    {
        None,
        CancelPartial,
        CancelAll
    }

    internal sealed class V3QueuedLoadRowDrawer
    {
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

            Rect iconRect = new Rect(rect.x + 8f, rect.y + 10f, 42f, 42f);
            V3CargoLoadDialogStyle.DrawThingIcon(iconRect, row.DisplayThing);

            Rect labelRect = new Rect(iconRect.xMax + 8f, rect.y + 7f, rect.width - 236f, 22f);
            Rect typeRect = new Rect(iconRect.xMax + 8f, labelRect.yMax + 2f, rect.width - 236f, 20f);
            Rect detailRect = new Rect(iconRect.xMax + 8f, typeRect.yMax + 2f, rect.width - 236f, 20f);
            Rect statusRect = new Rect(rect.xMax - 166f, rect.y + 8f, 158f, 20f);

            ShuttleUILayout.SafeLabel(
                labelRect,
                V3CargoLoadDialogStyle.FitLabelText(this.GetLabel(row), labelRect.width));

            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.SafeLabel(
                typeRect,
                V3CargoLoadDialogStyle.FitLabelText(this.GetKindLabel(row) + " | " + this.GetRegionLabel(row), typeRect.width));
            ShuttleUILayout.SafeLabel(
                detailRect,
                V3CargoLoadDialogStyle.FitLabelText(this.GetDetailText(row), detailRect.width));
            GUI.color = row.CanCancel
                ? V3CargoLoadDialogStyle.AccentColor
                : V3CargoLoadDialogStyle.YellowColor;
            ShuttleUILayout.SafeLabel(
                statusRect,
                V3CargoLoadDialogStyle.FitLabelText(this.GetStatusText(row), statusRect.width));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            V3QueuedLoadRowCommand command = hovered
                ? this.DrawButtons(rect, row)
                : V3QueuedLoadRowCommand.None;
            if (hovered)
            {
                TooltipHandler.TipRegion(rect, this.GetTooltip(row));
            }

            return command;
        }

        private V3QueuedLoadRowCommand DrawButtons(Rect rect, V3QueuedLoadRowModel row)
        {
            bool canCancel = row != null && row.CanCancel;
            Rect cancelAllRect = new Rect(
                rect.xMax - 104f,
                rect.yMax - 34f,
                96f,
                V3CargoLoadDialogStyle.ButtonHeight - 4f);

            V3QueuedLoadRowCommand command = V3QueuedLoadRowCommand.None;
            if (row != null && !row.IsPawn && row.StackCount > 1)
            {
                Rect cancelPartialRect = new Rect(
                    cancelAllRect.x - V3CargoLoadDialogStyle.Gap - 96f,
                    cancelAllRect.y,
                    96f,
                    cancelAllRect.height);
                if (V3CargoLoadDialogStyle.DrawButton(
                    cancelPartialRect,
                    V3CargoLoadText.Tr("CT_Shuttle_Cargo_CancelPartial"),
                    canCancel,
                    V3CargoLoadDialogStyle.YellowColor,
                    V3CargoLoadText.Tr("CT_Shuttle_Cargo_QuantityModifierTooltip")))
                {
                    command = V3QueuedLoadRowCommand.CancelPartial;
                }
            }

            if (V3CargoLoadDialogStyle.DrawButton(
                cancelAllRect,
                V3CargoLoadText.Tr("CT_Shuttle_Cargo_CancelAll"),
                canCancel,
                V3CargoLoadDialogStyle.RedColor,
                V3CargoLoadText.Tr("CT_Shuttle_QueuedLoad_EntryTooltip")))
            {
                command = V3QueuedLoadRowCommand.CancelAll;
            }

            return command;
        }

        private string GetLabel(V3QueuedLoadRowModel row)
        {
            if (row == null || string.IsNullOrEmpty(row.Label))
            {
                return V3CargoLoadText.Tr("CT_Shuttle_Cargo_QueuedEntryUnavailable");
            }

            if (row.StackCount > 1)
            {
                return row.Label + " " +
                    V3CargoLoadText.Tr("CT_Shuttle_Cargo_StackCountFormat", row.StackCount);
            }

            return row.Label;
        }

        private string GetDetailText(V3QueuedLoadRowModel row)
        {
            int stackCount = row != null ? Mathf.Max(0, row.StackCount) : 0;
            float massKg = row != null ? Mathf.Max(0f, row.MassKg) : 0f;
            return V3CargoLoadText.Tr("CT_Shuttle_Cargo_StackCountFormat", stackCount) +
                "   " +
                ShuttleUIMetricFormatter.FormatKgCompact(massKg);
        }

        private string GetKindLabel(V3QueuedLoadRowModel row)
        {
            if (row == null)
            {
                return V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_Unavailable");
            }

            return row.IsPawn
                ? V3CargoLoadText.Tr("CT_Shuttle_Cargo_Pawns")
                : V3CargoLoadText.Tr("CT_Shuttle_Cargo_Items");
        }

        private string GetRegionLabel(V3QueuedLoadRowModel row)
        {
            if (row == null || row.CargoRegionIndex < 0)
            {
                return V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_Unavailable");
            }

            return V3CargoLoadText.Tr("CT_Shuttle_Cargo_Region", row.CargoRegionIndex + 1);
        }

        private string GetStatusText(V3QueuedLoadRowModel row)
        {
            return row != null && row.CanCancel
                ? V3CargoLoadText.Tr("CT_Shuttle_Cargo_Queued")
                : V3CargoLoadText.Tr("CT_Shuttle_Cargo_QueuedEntryUnavailable");
        }

        private string GetTooltip(V3QueuedLoadRowModel row)
        {
            if (row == null)
            {
                return string.Empty;
            }

            return this.GetLabel(row) + "\n" +
                this.GetKindLabel(row) + "\n" +
                this.GetDetailText(row) + "\n" +
                this.GetStatusText(row) + "\n" +
                this.GetRegionLabel(row) + "\n" +
                V3CargoLoadText.Tr("CT_Shuttle_QueuedLoad_EntryTooltip");
        }
    }
}
