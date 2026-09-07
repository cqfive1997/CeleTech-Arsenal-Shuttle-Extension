using System;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal sealed class V3CargoLoadQuantityEditor
    {
        internal const float Width = 184f;

        private const float ButtonWidth = 25f;
        private const float FieldWidth = 56f;
        private const float Gap = 4f;
        private const float ControlHeight = 26f;

        private readonly V3CargoLoadSelectionModel selectionModel;
        private readonly Action<TransferableOneWay, int> adjustSelection;
        private readonly Action<TransferableOneWay, int> setSelectionCount;
        private readonly Action<TransferableOneWay> clearSelection;
        private readonly Action<TransferableOneWay> selectMaximumFit;
        private TransferableOneWay activeTransferable;

        internal V3CargoLoadQuantityEditor(
            V3CargoLoadSelectionModel selectionModel,
            Action<TransferableOneWay, int> adjustSelection,
            Action<TransferableOneWay, int> setSelectionCount,
            Action<TransferableOneWay> clearSelection,
            Action<TransferableOneWay> selectMaximumFit)
        {
            this.selectionModel = selectionModel;
            this.adjustSelection = adjustSelection;
            this.setSelectionCount = setSelectionCount;
            this.clearSelection = clearSelection;
            this.selectMaximumFit = selectMaximumFit;
        }

        internal void Draw(
            Rect rect,
            TransferableOneWay transferable,
            V3CargoLoadTransferableMetrics metrics,
            bool rowHovered,
            bool selectionBlocked)
        {
            if (transferable == null || metrics == null)
            {
                return;
            }

            int selectedCount = transferable.CountToTransfer;
            Pawn pawn = metrics.DisplayThing as Pawn;
            if (pawn != null && metrics.MaxCount == 1)
            {
                if (!rowHovered)
                {
                    this.DrawStaticPawnSelection(
                        rect,
                        selectedCount > 0,
                        selectionBlocked);
                    return;
                }

                int pawnMaximumSelectable = this.selectionModel != null
                    ? this.selectionModel.GetMaximumSelectableCount(transferable)
                    : selectedCount;
                this.DrawPawnCheckbox(
                    rect,
                    transferable,
                    selectedCount,
                    pawnMaximumSelectable);
                return;
            }

            this.UpdateActiveTransferable(rect, transferable);
            if (!rowHovered && !object.ReferenceEquals(this.activeTransferable, transferable))
            {
                this.DrawStaticCount(rect, selectedCount);
                return;
            }

            int maximumSelectable = this.selectionModel != null
                ? this.selectionModel.GetMaximumSelectableCount(transferable)
                : selectedCount;
            this.DrawCountControls(
                rect,
                transferable,
                selectedCount,
                maximumSelectable);
        }

        private void UpdateActiveTransferable(
            Rect rect,
            TransferableOneWay transferable)
        {
            Event evt = Event.current;
            if (evt == null || evt.type != EventType.MouseDown || evt.button != 0)
            {
                return;
            }

            if (rect.Contains(evt.mousePosition))
            {
                this.activeTransferable = transferable;
                return;
            }

            if (object.ReferenceEquals(this.activeTransferable, transferable))
            {
                this.activeTransferable = null;
            }
        }

        private void DrawStaticCount(Rect rect, int selectedCount)
        {
            const float StaticWidth = 62f;
            const float StaticHeight = 24f;
            Rect countRect = new Rect(
                rect.center.x - StaticWidth / 2f,
                rect.center.y - StaticHeight / 2f,
                StaticWidth,
                StaticHeight);
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

        private void DrawStaticPawnSelection(
            Rect rect,
            bool selected,
            bool disabled)
        {
            const float CheckboxSize = 24f;
            float x = rect.center.x - CheckboxSize / 2f;
            float y = rect.center.y - CheckboxSize / 2f;
            Widgets.CheckboxDraw(
                x,
                y,
                selected,
                disabled,
                CheckboxSize);
        }

        private void DrawPawnCheckbox(
            Rect rect,
            TransferableOneWay transferable,
            int selectedCount,
            int maximumSelectable)
        {
            bool selected = selectedCount > 0;
            bool before = selected;
            const float CheckboxSize = 24f;
            Vector2 position = new Vector2(
                rect.center.x - CheckboxSize / 2f,
                rect.center.y - CheckboxSize / 2f);
            Widgets.Checkbox(
                position,
                ref selected,
                CheckboxSize,
                !before && maximumSelectable <= 0);
            if (selected != before)
            {
                if (selected)
                {
                    this.SetExact(transferable, 1);
                }
                else
                {
                    this.Clear(transferable);
                }
            }

            TooltipHandler.TipRegion(
                rect,
                V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_QuantityExactTooltip"));
        }

        private void DrawCountControls(
            Rect rect,
            TransferableOneWay transferable,
            int selectedCount,
            int maximumSelectable)
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
                V3CargoLoadText.Tr("CT_Shuttle_Cargo_QuantityModifierTooltip");

            if (V3CargoLoadDialogStyle.DrawButton(
                clearRect,
                "<<",
                selectedCount > 0,
                V3CargoLoadDialogStyle.RedColor,
                V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_QuantityClearTooltip")))
            {
                this.Clear(transferable);
            }

            if (V3CargoLoadDialogStyle.DrawButton(
                decrementRect,
                "<",
                selectedCount > 0,
                V3CargoLoadDialogStyle.AccentColor,
                modifierTooltip))
            {
                this.Adjust(
                    transferable,
                    -V3CargoLoadDialogStyle.GetStepFromCurrentEvent());
            }

            this.DrawExactField(
                fieldRect,
                transferable,
                selectedCount,
                maximumSelectable);

            if (V3CargoLoadDialogStyle.DrawButton(
                incrementRect,
                ">",
                selectedCount < maximumSelectable,
                V3CargoLoadDialogStyle.AccentColor,
                modifierTooltip))
            {
                this.Adjust(
                    transferable,
                    V3CargoLoadDialogStyle.GetStepFromCurrentEvent());
            }

            if (V3CargoLoadDialogStyle.DrawButton(
                maximumRect,
                ">>",
                selectedCount < maximumSelectable,
                V3CargoLoadDialogStyle.GreenColor,
                V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_FitTooltip")))
            {
                if (this.selectMaximumFit != null)
                {
                    this.selectMaximumFit(transferable);
                }
            }
        }

        private void DrawExactField(
            Rect rect,
            TransferableOneWay transferable,
            int selectedCount,
            int maximumSelectable)
        {
            int editedCount = selectedCount;
            string editBuffer = transferable.EditBuffer;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.TextFieldNumeric<int>(
                rect,
                ref editedCount,
                ref editBuffer,
                0f,
                Mathf.Max(selectedCount, maximumSelectable));
            Text.Anchor = TextAnchor.UpperLeft;
            transferable.EditBuffer = editBuffer;
            if (editedCount != selectedCount)
            {
                this.SetExact(transferable, editedCount);
            }

            TooltipHandler.TipRegion(
                rect,
                V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_QuantityExactTooltip"));
        }

        private void Adjust(TransferableOneWay transferable, int delta)
        {
            if (this.adjustSelection != null)
            {
                this.adjustSelection(transferable, delta);
            }
        }

        private void SetExact(TransferableOneWay transferable, int count)
        {
            if (this.setSelectionCount != null)
            {
                this.setSelectionCount(transferable, count);
            }
        }

        private void Clear(TransferableOneWay transferable)
        {
            if (this.clearSelection != null)
            {
                this.clearSelection(transferable);
            }
        }
    }
}
