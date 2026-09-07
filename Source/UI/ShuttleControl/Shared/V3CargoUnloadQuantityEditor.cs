using System;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    /// <summary>
    /// Mirrors the load-manifest quantity editor for exact unload-stack intent.
    /// Controls are only drawn for a hovered or actively edited visible row.
    /// </summary>
    internal sealed class V3CargoUnloadQuantityEditor
    {
        internal const float Width = 184f;

        private const float ButtonWidth = 25f;
        private const float FieldWidth = 56f;
        private const float Gap = 4f;
        private const float ControlHeight = 26f;

        private readonly V3CargoUnloadSelectionModel selection;
        private readonly Action<V3CargoUnloadRowModel, int> setCount;
        private string activeRowKey;

        internal V3CargoUnloadQuantityEditor(
            V3CargoUnloadSelectionModel selection,
            Action<V3CargoUnloadRowModel, int> setCount)
        {
            this.selection = selection;
            this.setCount = setCount;
        }

        internal void Draw(
            Rect rect,
            V3CargoUnloadRowModel row,
            bool hovered,
            bool canEdit)
        {
            if (row == null)
            {
                return;
            }

            int selectedCount = this.selection != null
                ? this.selection.Get(row)
                : 0;
            if (!canEdit)
            {
                this.DrawStaticCount(rect, selectedCount);
                return;
            }

            this.UpdateActiveRow(rect, row);
            if (!hovered && !string.Equals(
                    this.activeRowKey,
                    row.Key,
                    StringComparison.Ordinal))
            {
                this.DrawStaticCount(rect, selectedCount);
                return;
            }

            this.DrawControls(rect, row, selectedCount);
        }

        private void UpdateActiveRow(Rect rect, V3CargoUnloadRowModel row)
        {
            Event evt = Event.current;
            if (evt == null || evt.type != EventType.MouseDown || evt.button != 0)
            {
                return;
            }

            if (rect.Contains(evt.mousePosition))
            {
                this.activeRowKey = row.Key;
                return;
            }

            if (string.Equals(
                    this.activeRowKey,
                    row.Key,
                    StringComparison.Ordinal))
            {
                this.activeRowKey = null;
            }
        }

        private void DrawStaticCount(Rect rect, int selectedCount)
        {
            const float staticWidth = 62f;
            const float staticHeight = 24f;
            Rect countRect = new Rect(
                rect.center.x - staticWidth / 2f,
                rect.center.y - staticHeight / 2f,
                staticWidth,
                staticHeight);
            ShuttleUILayout.DrawCardBackground(
                countRect,
                selectedCount > 0,
                false,
                ShuttleUIStyle.LeftColumnCardColor);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                countRect.ContractedBy(4f),
                selectedCount.ToString(),
                GameFont.Small,
                GameFont.Tiny,
                selectedCount > 0
                    ? V3CargoLoadDialogStyle.AccentColor
                    : ShuttleUIStyle.MutedTextColor,
                null,
                TextAnchor.MiddleCenter);
        }

        private void DrawControls(
            Rect rect,
            V3CargoUnloadRowModel row,
            int selectedCount)
        {
            float controlsWidth = ButtonWidth * 4f + FieldWidth + Gap * 4f;
            float x = rect.x + Mathf.Max(0f, (rect.width - controlsWidth) / 2f);
            float y = rect.y + Mathf.Max(0f, (rect.height - ControlHeight) / 2f);
            Rect clearRect = new Rect(x, y, ButtonWidth, ControlHeight);
            Rect decrementRect = new Rect(clearRect.xMax + Gap, y, ButtonWidth, ControlHeight);
            Rect fieldRect = new Rect(decrementRect.xMax + Gap, y, FieldWidth, ControlHeight);
            Rect incrementRect = new Rect(fieldRect.xMax + Gap, y, ButtonWidth, ControlHeight);
            Rect maximumRect = new Rect(incrementRect.xMax + Gap, y, ButtonWidth, ControlHeight);
            string modifierTooltip =
                V3CargoLoadText.Tr("CT_Shuttle_Unload_QuantityControlsTooltip");

            if (V3CargoLoadDialogStyle.DrawButton(
                clearRect,
                "<<",
                selectedCount > 0,
                V3CargoLoadDialogStyle.RedColor,
                V3CargoLoadText.Tr("CT_Shuttle_Unload_RemoveSelectionTooltip")))
            {
                this.Set(row, 0);
            }

            if (V3CargoLoadDialogStyle.DrawButton(
                decrementRect,
                "<",
                selectedCount > 0,
                V3CargoLoadDialogStyle.AccentColor,
                modifierTooltip))
            {
                this.Set(
                    row,
                    selectedCount - V3CargoLoadDialogStyle.GetStepFromCurrentEvent());
            }

            this.DrawExactField(fieldRect, row, selectedCount);

            if (V3CargoLoadDialogStyle.DrawButton(
                incrementRect,
                ">",
                selectedCount < row.AvailableCount,
                V3CargoLoadDialogStyle.AccentColor,
                modifierTooltip))
            {
                this.Set(
                    row,
                    selectedCount + V3CargoLoadDialogStyle.GetStepFromCurrentEvent());
            }

            if (V3CargoLoadDialogStyle.DrawButton(
                maximumRect,
                ">>",
                selectedCount < row.AvailableCount,
                V3CargoLoadDialogStyle.GreenColor,
                V3CargoLoadText.Tr("CT_Shuttle_Unload_RowMaximumTooltip")))
            {
                this.Set(row, row.AvailableCount);
            }
        }

        private void DrawExactField(
            Rect rect,
            V3CargoUnloadRowModel row,
            int selectedCount)
        {
            int editedCount = selectedCount;
            string editBuffer = row.EditBuffer;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.TextFieldNumeric<int>(
                rect,
                ref editedCount,
                ref editBuffer,
                0f,
                row.AvailableCount);
            Text.Anchor = TextAnchor.UpperLeft;
            row.EditBuffer = editBuffer;
            if (editedCount != selectedCount)
            {
                this.Set(row, editedCount);
            }

            TooltipHandler.TipRegion(
                rect,
                V3CargoLoadText.Tr("CT_Shuttle_Unload_QuantityControlsTooltip"));
        }

        private void Set(V3CargoUnloadRowModel row, int count)
        {
            if (this.setCount != null)
            {
                this.setCount(row, count);
            }
        }
    }
}
