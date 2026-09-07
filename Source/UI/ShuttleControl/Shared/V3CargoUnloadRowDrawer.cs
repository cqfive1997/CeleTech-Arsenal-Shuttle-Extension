using System;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal sealed class V3CargoUnloadRowDrawer
    {
        private readonly V3CargoUnloadSelectionModel selection;
        private readonly Action<V3CargoUnloadRowModel, int> setCount;
        private readonly Func<bool> canEdit;
        private readonly V3CargoUnloadQuantityEditor quantityEditor;

        internal V3CargoUnloadRowDrawer(
            V3CargoUnloadSelectionModel selection,
            Action<V3CargoUnloadRowModel, int> setCount,
            Func<bool> canEdit)
        {
            this.selection = selection;
            this.setCount = setCount;
            this.canEdit = canEdit;
            this.quantityEditor = new V3CargoUnloadQuantityEditor(
                selection,
                setCount);
        }

        internal void DrawAvailable(Rect rect, V3CargoUnloadRowModel row, int index)
        {
            if (row == null)
            {
                return;
            }

            int selected = this.selection.Get(row);
            bool hovered = Mouse.IsOver(rect);
            Widgets.DrawBoxSolid(
                rect,
                selected > 0
                    ? V3CargoLoadDialogStyle.SelectedCardColor
                    : V3CargoLoadDialogStyle.CardColor);
            if (index % 2 == 1 && selected <= 0)
            {
                Widgets.DrawLightHighlight(rect);
            }

            if (hovered && this.CanEdit())
            {
                Widgets.DrawHighlight(rect);
            }

            V3CargoUnloadTableLayout layout = V3CargoUnloadTableLayout.Create(rect);
            // Visible unload rows can afford the resolved Thing icon path. Corpse and
            // minified-thing icons often do not have a usable def.uiIcon and would show BadTex
            // when forced through the compact path.
            V3CargoLoadDialogStyle.DrawThingIcon(layout.IconRect, row.DisplayThing, false);
            if (layout.ShowInfoButton && row.DisplayThing != null)
            {
                if (hovered)
                {
                    Widgets.InfoCardButtonCentered(layout.InfoRect, row.DisplayThing);
                }
                else
                {
                    this.DrawCell(
                        layout.InfoRect,
                        "i",
                        ShuttleUIStyle.MutedTextColor,
                        TextAnchor.MiddleCenter,
                        GameFont.Small);
                }
            }

            this.DrawCell(layout.NameRect, row.Label, Color.white, TextAnchor.MiddleLeft, GameFont.Small);
            this.DrawCell(layout.AvailableRect, row.AvailableCount.ToString(), ShuttleUIStyle.MutedTextColor, TextAnchor.MiddleRight);
            if (layout.ShowUnitMass)
            {
                this.DrawCell(layout.UnitMassRect, ShuttleUIMetricFormatter.FormatKgCompact(row.UnitMassKg), ShuttleUIStyle.MutedTextColor, TextAnchor.MiddleRight);
            }

            if (layout.ShowSource)
            {
                this.DrawCell(layout.SourceRect, row.SourceLabel, ShuttleUIStyle.MutedTextColor, TextAnchor.MiddleCenter);
            }

            this.quantityEditor.Draw(
                layout.ActionRect,
                row,
                hovered,
                this.CanEdit());
            if (hovered)
            {
                TooltipHandler.TipRegion(
                    new Rect(rect.x, rect.y, layout.ActionRect.x - rect.x - 3f, rect.height),
                    row.Label + "\n" + row.SourceLabel + "\n" +
                    ShuttleUIMetricFormatter.FormatKgCompact(row.TotalMassKg));
            }
        }

        internal void DrawSelected(Rect rect, V3CargoUnloadRowModel row, int index)
        {
            int selected = this.selection.Get(row);
            if (row == null || selected <= 0)
            {
                return;
            }

            bool hovered = Mouse.IsOver(rect);
            Widgets.DrawBoxSolid(rect, V3CargoLoadDialogStyle.SelectedCardColor);
            if (hovered)
            {
                Widgets.DrawHighlight(rect);
            }

            Rect icon = new Rect(rect.x + 4f, rect.y + 4f, rect.height - 8f, rect.height - 8f);
            Rect minus = new Rect(rect.xMax - 102f, rect.y + 9f, 30f, 28f);
            Rect remove = new Rect(minus.xMax + 6f, minus.y, 66f, 28f);
            Rect name = new Rect(icon.xMax + 7f, rect.y + 2f, minus.x - icon.xMax - 12f, 20f);
            Rect detail = new Rect(name.x, name.yMax, name.width, 17f);
            V3CargoLoadDialogStyle.DrawThingIcon(icon, row.DisplayThing, false);
            this.DrawCell(name, row.Label, Color.white, TextAnchor.MiddleLeft, GameFont.Small);
            this.DrawCell(
                detail,
                "x" + selected + " · " + ShuttleUIMetricFormatter.FormatKgCompact(selected * row.UnitMassKg) +
                    " · " + row.SourceLabel,
                ShuttleUIStyle.MutedTextColor,
                TextAnchor.MiddleLeft);
            if (!hovered || !this.CanEdit())
            {
                return;
            }

            if (V3CargoLoadDialogStyle.DrawButton(
                minus,
                "-",
                selected > 0,
                V3CargoLoadDialogStyle.AccentColor,
                V3CargoLoadText.Tr("CT_Shuttle_Unload_QuantityControlsTooltip")))
            {
                this.Set(
                    row,
                    selected - V3CargoLoadDialogStyle.GetStepFromCurrentEvent());
            }

            if (V3CargoLoadDialogStyle.DrawButton(
                remove,
                V3CargoLoadText.Tr("CT_Shuttle_Unload_RemoveSelection"),
                true,
                V3CargoLoadDialogStyle.RedColor,
                V3CargoLoadText.Tr("CT_Shuttle_Unload_RemoveSelectionTooltip")))
            {
                this.Set(row, 0);
            }
        }

        private void Set(V3CargoUnloadRowModel row, int count)
        {
            if (this.setCount != null)
            {
                this.setCount(row, count);
            }
        }

        private bool CanEdit()
        {
            return this.canEdit == null || this.canEdit();
        }

        internal bool CanEditSelection()
        {
            return this.CanEdit();
        }

        private void DrawCell(
            Rect rect,
            string text,
            Color color,
            TextAnchor anchor,
            GameFont font = GameFont.Tiny)
        {
            ShuttleUILayout.DrawFittedSingleLineLabel(
                rect.ContractedBy(2f),
                string.IsNullOrEmpty(text) ? "-" : text,
                font,
                GameFont.Tiny,
                color,
                null,
                anchor);
        }
    }
}
