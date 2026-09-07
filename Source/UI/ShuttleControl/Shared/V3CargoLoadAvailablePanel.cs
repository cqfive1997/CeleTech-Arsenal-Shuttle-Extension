using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal sealed class V3CargoLoadAvailablePanel
    {
        private const float SearchCountWidth = 78f;
        private const float SearchClearWidth = 58f;
        private const float FilterButtonWidth = 156f;
        private const float ToolbarButtonGap = 8f;
        private const float StackedToolbarThreshold = 560f;

        private readonly V3CargoLoadAvailablePanelState state;
        private readonly V3CargoLoadTransferableMetricsResolver metricsResolver;
        private readonly V3CargoLoadSelectionModel selectionModel;
        private readonly V3CargoLoadAvailableRowDrawer rowDrawer;
        private readonly V3CargoLoadCategoryResolver categoryResolver;
        private readonly V3CargoLoadFilterMenu filterMenu;
        private readonly V3CargoRowSweepSelectionGesture rowSweepSelection =
            new V3CargoRowSweepSelectionGesture();
        private readonly Action<TransferableOneWay> clearSelection;
        private readonly Action<TransferableOneWay> selectMaximumFit;
        private readonly Action<List<TransferableOneWay>, bool> openBatchMenu;
        private readonly Func<bool> canOpenBatchMenuWithoutTargets;

        internal V3CargoLoadAvailablePanel(
            V3CargoLoadAvailablePanelState state,
            V3CargoLoadTransferableMetricsResolver metricsResolver,
            V3CargoLoadCategoryResolver categoryResolver,
            V3CargoLoadSelectionModel selectionModel,
            Action<TransferableOneWay, int> adjustSelection,
            Action<TransferableOneWay, int> setSelectionCount,
            Action<TransferableOneWay> clearSelection,
            Action<TransferableOneWay> selectMaximumFit,
            Func<bool> compactIconModeProvider,
            Action<List<TransferableOneWay>, bool> openBatchMenu,
            Func<bool> canOpenBatchMenuWithoutTargets)
        {
            this.state = state;
            this.metricsResolver = metricsResolver;
            this.categoryResolver = categoryResolver;
            this.selectionModel = selectionModel;
            this.filterMenu = new V3CargoLoadFilterMenu(
                categoryResolver,
                this.SelectFilter);
            this.rowDrawer = new V3CargoLoadAvailableRowDrawer(
                metricsResolver,
                selectionModel,
                adjustSelection,
                setSelectionCount,
                clearSelection,
                selectMaximumFit,
                compactIconModeProvider);
            this.clearSelection = clearSelection;
            this.selectMaximumFit = selectMaximumFit;
            this.openBatchMenu = openBatchMenu;
            this.canOpenBatchMenuWithoutTargets = canOpenBatchMenuWithoutTargets;
        }

        internal void Draw(
            Rect rect,
            List<TransferableOneWay> passengerTransferables,
            List<TransferableOneWay> cargoTransferables)
        {
            V3CargoLoadDialogStyle.DrawSurface(rect);
            V3CargoLoadDialogStyle.DrawSectionHeader(
                rect,
                V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_LoadableObjects"));

            Rect toolbarRect = new Rect(
                rect.x + 10f,
                rect.y + 38f,
                rect.width - 20f,
                V3CargoLoadDialogStyle.ToolbarHeight);
            Rect searchRect;
            Rect filterRect;
            Rect batchRect;
            float tabY;
            if (toolbarRect.width >= StackedToolbarThreshold)
            {
                batchRect = new Rect(
                    toolbarRect.xMax - V3CargoLoadDialogStyle.BatchButtonWidth,
                    toolbarRect.y,
                    V3CargoLoadDialogStyle.BatchButtonWidth,
                    toolbarRect.height);
                filterRect = new Rect(
                    batchRect.x - ToolbarButtonGap - FilterButtonWidth,
                    toolbarRect.y,
                    FilterButtonWidth,
                    toolbarRect.height);
                searchRect = new Rect(
                    toolbarRect.x,
                    toolbarRect.y,
                    Mathf.Max(120f, filterRect.x - toolbarRect.x - ToolbarButtonGap),
                    toolbarRect.height);
                tabY = toolbarRect.yMax + 8f;
            }
            else
            {
                searchRect = toolbarRect;
                float actionY = toolbarRect.yMax + 6f;
                float actionWidth = Mathf.Max(
                    80f,
                    (toolbarRect.width - ToolbarButtonGap) / 2f);
                filterRect = new Rect(
                    toolbarRect.x,
                    actionY,
                    actionWidth,
                    toolbarRect.height);
                batchRect = new Rect(
                    filterRect.xMax + ToolbarButtonGap,
                    actionY,
                    actionWidth,
                    toolbarRect.height);
                tabY = filterRect.yMax + 8f;
            }

            Rect tabRect = new Rect(
                rect.x + 10f,
                tabY,
                rect.width - 20f,
                V3CargoLoadDialogStyle.TabHeight);
            Rect listRect = new Rect(
                rect.x + 10f,
                tabRect.yMax + 8f,
                rect.width - 20f,
                Mathf.Max(0f, rect.yMax - tabRect.yMax - 18f));

            bool filterChanged = this.DrawSearch(searchRect);

            if (this.DrawTabs(tabRect, passengerTransferables, cargoTransferables))
            {
                filterChanged = true;
            }

            if (filterChanged)
            {
                this.state.MarkFilterDirty();
            }

            List<TransferableOneWay> activeTransferables =
                this.selectionModel.GetActiveTransferables(
                    this.state.ActiveTab == V3CargoLoadTab.Passengers);
            this.EnsureFilteredRows(activeTransferables, false);
            this.DrawSearchCount(
                searchRect,
                this.GetFilteredRowCount(activeTransferables),
                Count(activeTransferables));
            this.DrawFilterButton(filterRect, activeTransferables);
            this.DrawBatchButton(
                batchRect,
                activeTransferables);
            this.DrawList(listRect, activeTransferables);
        }

        private void DrawFilterButton(
            Rect rect,
            List<TransferableOneWay> activeTransferables)
        {
            string activeLabel = this.filterMenu.GetActiveLabel(this.state.ActiveFilterId);
            if (!V3CargoLoadDialogStyle.DrawButton(
                    rect,
                    V3CargoLoadText.Tr(
                        "CT_Shuttle_LoadCargo_FilterButton",
                        activeLabel),
                    true,
                    V3CargoLoadDialogStyle.AccentColor,
                    V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_FilterTooltip")))
            {
                return;
            }

            this.filterMenu.Open(
                activeTransferables,
                this.state.ActiveTab == V3CargoLoadTab.Passengers,
                this.state.ActiveFilterId);
        }

        private void DrawBatchButton(
            Rect rect,
            List<TransferableOneWay> activeTransferables)
        {
            bool canOpenWithoutTargets = this.canOpenBatchMenuWithoutTargets != null &&
                this.canOpenBatchMenuWithoutTargets();
            bool enabled = Count(activeTransferables) > 0 || canOpenWithoutTargets;
            if (!V3CargoLoadDialogStyle.DrawButton(
                    rect,
                    V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_BatchByCategoryButton"),
                    enabled,
                    V3CargoLoadDialogStyle.AccentColor,
                    V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_BatchByCategoryTooltip")))
            {
                return;
            }

            if (this.openBatchMenu != null)
            {
                this.openBatchMenu(
                    activeTransferables,
                    this.state.ActiveTab == V3CargoLoadTab.Passengers);
            }
        }

        private bool DrawSearch(Rect rect)
        {
            V3CargoLoadAvailablePanelState panelState = this.state;
            ShuttleUILayout.DrawCardBackground(rect, false, false, ShuttleUIStyle.LeftColumnCardColor);
            Rect clearRect = GetSearchClearRect(rect);
            Rect countRect = GetSearchCountRect(rect);
            Rect fieldRect = GetSearchFieldRect(rect, countRect);
            string before = panelState.SearchText ?? string.Empty;
            GUI.SetNextControlName(V3CargoLoadDialogStyle.SearchControlName);
            panelState.SearchText = Widgets.TextField(fieldRect, before);
            bool changed = !string.Equals(
                before,
                panelState.SearchText ?? string.Empty,
                StringComparison.Ordinal);
            if (!string.Equals(before, panelState.SearchText ?? string.Empty, StringComparison.Ordinal))
            {
                panelState.Scroll = Vector2.zero;
            }

            if (string.IsNullOrEmpty(panelState.SearchText))
            {
                Text.Font = GameFont.Tiny;
                GUI.color = ShuttleUIStyle.MutedTextColor;
                ShuttleUILayout.SafeLabel(
                    new Rect(fieldRect.x + 6f, fieldRect.y + 8f, fieldRect.width - 12f, 18f),
                    V3CargoLoadDialogStyle.FitLabelText(
                        V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_SearchPlaceholder"),
                        fieldRect.width - 12f));
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
            }

            if (V3CargoLoadDialogStyle.DrawButton(
                clearRect,
                V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_SearchClear"),
                !string.IsNullOrEmpty(panelState.SearchText),
                ShuttleUIStyle.BorderColor,
                V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_SearchClearTooltip")))
            {
                panelState.SearchText = string.Empty;
                panelState.Scroll = Vector2.zero;
                GUI.FocusControl(null);
                changed = true;
            }

            if (Mouse.IsOver(rect))
            {
                TooltipHandler.TipRegion(
                    rect,
                    V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_SearchTooltip"));
            }

            return changed;
        }

        private void DrawSearchCount(Rect rect, int matchedCount, int totalCount)
        {
            ShuttleUILayout.DrawFittedSingleLineLabel(
                GetSearchCountRect(rect),
                V3CargoLoadText.Tr(
                    "CT_Shuttle_LoadCargo_SearchCount",
                    matchedCount,
                    totalCount),
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.MutedTextColor,
                null,
                TextAnchor.MiddleRight);
        }

        private bool DrawTabs(
            Rect rect,
            List<TransferableOneWay> passengerTransferables,
            List<TransferableOneWay> cargoTransferables)
        {
            V3CargoLoadTab before = this.state.ActiveTab;
            float width = Mathf.Min(132f, (rect.width - ShuttleUIStyle.SmallGap) / 2f);
            Rect passengerRect = new Rect(rect.x, rect.y, width, rect.height);
            Rect cargoRect = new Rect(passengerRect.xMax + ShuttleUIStyle.SmallGap, rect.y, width, rect.height);
            this.DrawTabButton(
                passengerRect,
                V3CargoLoadText.Tr("CT_Shuttle_UI_Passengers") + " " + Count(passengerTransferables),
                this.state.ActiveTab == V3CargoLoadTab.Passengers,
                V3CargoLoadTab.Passengers);
            this.DrawTabButton(
                cargoRect,
                V3CargoLoadText.Tr("CT_Shuttle_UI_Cargo") + " " + Count(cargoTransferables),
                this.state.ActiveTab == V3CargoLoadTab.Cargo,
                V3CargoLoadTab.Cargo);
            return before != this.state.ActiveTab;
        }

        private void DrawTabButton(
            Rect rect,
            string label,
            bool selected,
            V3CargoLoadTab tab)
        {
            if (V3CargoLoadDialogStyle.DrawButton(
                rect,
                label,
                true,
                selected ? V3CargoLoadDialogStyle.AccentColor : ShuttleUIStyle.BorderColor,
                null))
            {
                this.state.ActiveTab = tab;
                this.state.Scroll = Vector2.zero;
            }
        }

        private void DrawList(Rect rect, List<TransferableOneWay> transferables)
        {
            if (transferables == null || transferables.Count == 0)
            {
                V3CargoLoadDialogStyle.DrawEmptyText(
                    rect,
                    V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_NoLoadableObjects"));
                return;
            }

            int filteredCount = this.GetFilteredRowCount(transferables);
            if (filteredCount == 0)
            {
                V3CargoLoadDialogStyle.DrawEmptyText(
                    rect,
                    string.IsNullOrEmpty(V3CargoLoadSearchFilter.GetSearchNeedle(this.state.SearchText)) &&
                    V3CargoLoadFilterIds.IsAll(this.state.ActiveFilterId)
                        ? V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_NoLoadableObjects")
                        : V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_FilterNoResults"));
                return;
            }

            float headerWidth = Mathf.Max(0f, rect.width - 16f);
            Rect headerRect = new Rect(
                rect.x,
                rect.y,
                headerWidth,
                V3CargoLoadDialogStyle.AvailableHeaderHeight);
            this.DrawTableHeader(headerRect);
            Rect rowsRect = new Rect(
                rect.x,
                headerRect.yMax + 2f,
                rect.width,
                Mathf.Max(0f, rect.yMax - headerRect.yMax - 2f));
            this.DrawRows(rowsRect, transferables);
        }

        private void DrawTableHeader(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, ShuttleUIStyle.LeftColumnCardColor);
            Widgets.DrawBoxSolid(
                new Rect(rect.x, rect.yMax - 1f, rect.width, 1f),
                ShuttleUIStyle.SubtleBorderColor);
            V3CargoLoadAvailableTableLayout layout =
                V3CargoLoadAvailableTableLayout.Create(rect);
            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleUIStyle.MutedTextColor;
            this.DrawHeaderLabel(
                layout.NameRect,
                V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_TableItem"),
                TextAnchor.MiddleLeft);
            this.DrawHeaderLabel(
                layout.AvailableRect,
                V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_TableAvailable"),
                TextAnchor.MiddleRight);
            if (layout.ShowUnitMass)
            {
                this.DrawHeaderLabel(
                    layout.UnitMassRect,
                    V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_TableUnitMass"),
                    TextAnchor.MiddleRight);
            }

            if (layout.ShowDestination)
            {
                string detailHeaderKey =
                    this.state.ActiveTab == V3CargoLoadTab.Passengers
                        ? "CT_Shuttle_LoadCargo_TableType"
                        : "CT_Shuttle_LoadCargo_TableDestination";
                this.DrawHeaderLabel(
                    layout.DestinationRect,
                    V3CargoLoadText.Tr(detailHeaderKey),
                    TextAnchor.MiddleCenter);
            }

            this.DrawHeaderLabel(
                layout.QuantityRect,
                V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_TableSelected"),
                TextAnchor.MiddleCenter);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private void DrawHeaderLabel(Rect rect, string label, TextAnchor anchor)
        {
            ShuttleUILayout.DrawFittedSingleLineLabel(
                rect.ContractedBy(2f),
                label,
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.MutedTextColor,
                null,
                anchor);
        }

        private void DrawRows(Rect rect, List<TransferableOneWay> transferables)
        {
            this.rowSweepSelection.BeginFrame();
            float rowStride =
                V3CargoLoadDialogStyle.AvailableRowHeight +
                V3CargoLoadDialogStyle.AvailableRowGap;
            int filteredCount = this.GetFilteredRowCount(transferables);
            Rect viewRect = new Rect(
                0f,
                0f,
                rect.width - 16f,
                Mathf.Max(rect.height, filteredCount * rowStride));
            Widgets.BeginScrollView(rect, ref this.state.Scroll, viewRect);
            int firstIndex;
            int lastIndexExclusive;
            V3CargoLoadDialogStyle.GetVisibleRowRange(
                this.state.Scroll,
                rect.height,
                filteredCount,
                rowStride,
                out firstIndex,
                out lastIndexExclusive);
            for (int i = firstIndex; i < lastIndexExclusive; i++)
            {
                int sourceIndex = this.GetSourceIndexForFilteredRow(i);
                if (sourceIndex < 0 || sourceIndex >= transferables.Count)
                {
                    continue;
                }

                Rect rowRect = new Rect(
                    0f,
                    i * rowStride,
                    viewRect.width,
                    V3CargoLoadDialogStyle.AvailableRowHeight);
                TransferableOneWay transferable = transferables[sourceIndex];
                this.rowDrawer.Draw(rowRect, transferable, i);
                this.HandleRowSweep(
                    rowRect,
                    transferables,
                    transferable,
                    i);
            }

            Widgets.EndScrollView();
        }

        private void HandleRowSweep(
            Rect rowRect,
            List<TransferableOneWay> transferables,
            TransferableOneWay current,
            int filteredRowIndex)
        {
            int currentCount = current != null ? current.CountToTransfer : 0;
            int currentMaximum = current != null && this.selectionModel != null
                ? this.selectionModel.GetMaximumSelectableCount(current)
                : currentCount;
            bool shouldSelect;
            int first;
            int last;
            if (!this.rowSweepSelection.TryHandleRow(
                    rowRect,
                    filteredRowIndex,
                    currentCount > 0,
                    currentMaximum > currentCount,
                    out shouldSelect,
                    out first,
                    out last))
            {
                return;
            }

            for (int i = first; i <= last; i++)
            {
                if (!this.rowSweepSelection.TryMarkRowForApplication(i))
                {
                    continue;
                }

                int sourceIndex = this.GetSourceIndexForFilteredRow(i);
                if (sourceIndex < 0 || sourceIndex >= transferables.Count)
                {
                    continue;
                }

                TransferableOneWay transferable = transferables[sourceIndex];
                int selectedCount = transferable != null
                    ? transferable.CountToTransfer
                    : 0;
                if (shouldSelect)
                {
                    int maximum = transferable != null && this.selectionModel != null
                        ? this.selectionModel.GetMaximumSelectableCount(transferable)
                        : selectedCount;
                    if (maximum > selectedCount && this.selectMaximumFit != null)
                    {
                        this.selectMaximumFit(transferable);
                    }
                }
                else if (selectedCount > 0 && this.clearSelection != null)
                {
                    this.clearSelection(transferable);
                }
            }
        }

        private static int Count(List<TransferableOneWay> transferables)
        {
            return transferables != null ? transferables.Count : 0;
        }

        private void EnsureFilteredRows(List<TransferableOneWay> transferables, bool force)
        {
            string needle = V3CargoLoadSearchFilter.GetSearchNeedle(this.state.SearchText);
            string categoryFilterId = this.state.ActiveFilterId;
            int count = Count(transferables);
            if (!force &&
                !this.state.FilterDirty &&
                object.ReferenceEquals(this.state.LastFilteredSource, transferables) &&
                this.state.LastFilteredTab == this.state.ActiveTab &&
                this.state.LastFilteredCount == count &&
                string.Equals(this.state.LastSearchNeedle, needle, StringComparison.Ordinal) &&
                string.Equals(
                    this.state.LastCategoryFilterId,
                    categoryFilterId,
                    StringComparison.Ordinal))
            {
                return;
            }

            this.RefreshFilteredRows(transferables, needle, categoryFilterId);
            this.state.LastFilteredSource = transferables;
            this.state.LastFilteredTab = this.state.ActiveTab;
            this.state.LastSearchNeedle = needle;
            this.state.LastCategoryFilterId = categoryFilterId;
            this.state.LastFilteredCount = count;
            this.state.FilterDirty = false;
        }

        private void RefreshFilteredRows(
            List<TransferableOneWay> transferables,
            string needle,
            string categoryFilterId)
        {
            this.state.FilteredRowIndices.Clear();
            this.state.FilterMatchesAllRows = false;
            if (transferables == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(needle) &&
                V3CargoLoadFilterIds.IsAll(categoryFilterId))
            {
                this.state.FilterMatchesAllRows = true;
                return;
            }

            V3CargoLoadRowFilter.BuildFilteredRows(
                transferables,
                needle,
                categoryFilterId,
                this.state.ActiveTab == V3CargoLoadTab.Passengers,
                this.state.FilteredRowIndices,
                this.categoryResolver,
                this.metricsResolver,
                this.selectionModel);
        }

        private void SelectFilter(string filterId)
        {
            if (string.Equals(
                    this.state.ActiveFilterId,
                    filterId,
                    StringComparison.Ordinal))
            {
                return;
            }

            this.state.SetActiveFilterId(filterId);
            this.state.Scroll = Vector2.zero;
            this.state.MarkFilterDirty();
        }

        private int GetFilteredRowCount(List<TransferableOneWay> transferables)
        {
            return this.state.FilterMatchesAllRows
                ? Count(transferables)
                : this.state.FilteredRowIndices.Count;
        }

        private int GetSourceIndexForFilteredRow(int filteredIndex)
        {
            return this.state.FilterMatchesAllRows
                ? filteredIndex
                : this.state.FilteredRowIndices[filteredIndex];
        }

        private static Rect GetSearchClearRect(Rect rect)
        {
            return new Rect(
                rect.xMax - SearchClearWidth - 6f,
                rect.y + 4f,
                SearchClearWidth,
                rect.height - 8f);
        }

        private static Rect GetSearchCountRect(Rect rect)
        {
            Rect clearRect = GetSearchClearRect(rect);
            return new Rect(
                clearRect.x - SearchCountWidth - 6f,
                rect.y + 5f,
                SearchCountWidth,
                rect.height - 10f);
        }

        private static Rect GetSearchFieldRect(Rect rect, Rect countRect)
        {
            return new Rect(
                rect.x + 8f,
                rect.y + 4f,
                Mathf.Max(40f, countRect.x - rect.x - 14f),
                rect.height - 8f);
        }
    }
}
