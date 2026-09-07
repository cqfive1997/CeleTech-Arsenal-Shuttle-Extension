using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal sealed class V3CargoLoadSelectedPanel
    {
        private readonly V3CargoLoadSelectedPanelState state;
        private readonly V3CargoLoadTransferableMetricsResolver metricsResolver;
        private readonly V3CargoLoadSelectionModel selectionModel;
        private readonly Action<TransferableOneWay, int> adjustSelection;
        private readonly Action<TransferableOneWay> clearSelection;
        private readonly Func<bool> compactIconModeProvider;
        private readonly Action clearAllSelections;

        internal V3CargoLoadSelectedPanel(
            V3CargoLoadSelectedPanelState state,
            V3CargoLoadTransferableMetricsResolver metricsResolver,
            V3CargoLoadSelectionModel selectionModel,
            Action<TransferableOneWay, int> adjustSelection,
            Action<TransferableOneWay> clearSelection,
            Func<bool> compactIconModeProvider,
            Action clearAllSelections)
        {
            this.state = state;
            this.metricsResolver = metricsResolver;
            this.selectionModel = selectionModel;
            this.adjustSelection = adjustSelection;
            this.clearSelection = clearSelection;
            this.compactIconModeProvider = compactIconModeProvider;
            this.clearAllSelections = clearAllSelections;
        }

        internal void Draw(Rect rect)
        {
            V3CargoLoadDialogStyle.DrawSurface(rect);
            V3CargoLoadDialogStyle.DrawSectionHeader(
                rect,
                V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_PlanSection"));

            List<TransferableOneWay> selected = this.selectionModel.GetSelectedTransferables();
            Rect clearRect = new Rect(rect.xMax - 106f, rect.y + 3f, 96f, 24f);
            if (V3CargoLoadDialogStyle.DrawTextAction(
                clearRect,
                V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_ClearPlan"),
                selected.Count > 0,
                V3CargoLoadDialogStyle.RedColor,
                V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_BatchClearAllTooltip")))
            {
                if (this.clearAllSelections != null)
                {
                    this.clearAllSelections();
                }
            }

            Rect listRect = new Rect(
                rect.x + 10f,
                rect.y + 40f,
                rect.width - 20f,
                Mathf.Max(0f, rect.height - 50f));
            if (selected.Count == 0)
            {
                V3CargoLoadDialogStyle.DrawEmptyText(
                    listRect,
                    V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_NothingSelected"));
                return;
            }

            this.DrawRows(listRect, selected);
        }

        private void DrawRows(Rect rect, List<TransferableOneWay> selected)
        {
            float rowStride = V3CargoLoadDialogStyle.SelectedRowHeight + 4f;
            Rect viewRect = new Rect(
                0f,
                0f,
                rect.width - 16f,
                Mathf.Max(rect.height, selected.Count * rowStride));
            Widgets.BeginScrollView(rect, ref this.state.Scroll, viewRect);
            int firstIndex;
            int lastIndexExclusive;
            V3CargoLoadDialogStyle.GetVisibleRowRange(
                this.state.Scroll,
                rect.height,
                selected.Count,
                rowStride,
                out firstIndex,
                out lastIndexExclusive);
            for (int i = firstIndex; i < lastIndexExclusive; i++)
            {
                Rect rowRect = new Rect(
                    0f,
                    i * rowStride,
                    viewRect.width,
                    V3CargoLoadDialogStyle.SelectedRowHeight);
                this.DrawSelectedRow(rowRect, selected[i]);
            }

            Widgets.EndScrollView();
        }

        private void DrawSelectedRow(Rect rect, TransferableOneWay transferable)
        {
            V3CargoLoadTransferableMetrics metrics =
                this.metricsResolver.GetMetrics(transferable);
            ShuttleUILayout.DrawCardBackground(
                rect,
                false,
                false,
                ShuttleUIStyle.LeftColumnCardColor);
            bool hovered = Mouse.IsOver(rect);
            if (hovered)
            {
                Widgets.DrawHighlight(rect);
            }

            Rect iconRect = new Rect(rect.x + 7f, rect.y + 8f, 30f, 30f);
            V3CargoLoadDialogStyle.DrawThingIcon(
                iconRect,
                metrics.DisplayThing,
                this.UseCompactIconMode());
            Rect labelRect = new Rect(
                iconRect.xMax + 7f,
                rect.y + 3f,
                Mathf.Max(40f, rect.width - 164f),
                20f);
            Rect countRect = new Rect(
                iconRect.xMax + 7f,
                labelRect.yMax,
                labelRect.width,
                18f);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            ShuttleUILayout.SafeLabel(
                labelRect,
                V3CargoLoadDialogStyle.FitLabelText(
                    metrics.LabelWithoutCount,
                    labelRect.width));
            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.SafeLabel(
                countRect,
                V3CargoLoadDialogStyle.FitLabelText(
                    V3CargoLoadText.GetSelectedRowCountText(transferable, metrics),
                    countRect.width));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            if (hovered)
            {
                this.DrawSelectedRowButtons(rect, transferable);
            }
            if (hovered)
            {
                TooltipHandler.TipRegion(
                    rect,
                    V3CargoLoadText.GetTransferableTooltip(
                        this.selectionModel,
                        transferable,
                        metrics));
            }
        }

        private void DrawSelectedRowButtons(Rect rect, TransferableOneWay transferable)
        {
            Rect minusRect = new Rect(rect.xMax - 102f, rect.y + 9f, 30f, 28f);
            Rect clearRect = new Rect(minusRect.xMax + 6f, minusRect.y, 66f, 28f);
            if (V3CargoLoadDialogStyle.DrawTextAction(
                minusRect,
                "-",
                true,
                V3CargoLoadDialogStyle.AccentColor,
                V3CargoLoadText.Tr("CT_Shuttle_Cargo_QuantityModifierTooltip")))
            {
                if (this.adjustSelection != null)
                {
                    this.adjustSelection(
                        transferable,
                        -V3CargoLoadDialogStyle.GetStepFromCurrentEvent());
                }
            }

            if (V3CargoLoadDialogStyle.DrawTextAction(
                clearRect,
                V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_Remove"),
                true,
                V3CargoLoadDialogStyle.RedColor,
                null))
            {
                if (this.clearSelection != null)
                {
                    this.clearSelection(transferable);
                }
            }
        }

        private bool UseCompactIconMode()
        {
            return this.compactIconModeProvider != null && this.compactIconModeProvider();
        }
    }
}
