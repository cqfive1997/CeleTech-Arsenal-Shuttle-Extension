using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal sealed class V3CargoUnloadPanelDrawer
    {
        private const float RowHeight = 38f;
        private const float RowGap = 1f;
        private readonly V3CargoUnloadSelectionModel selection;
        private readonly V3CargoUnloadRowDrawer rowDrawer;
        private readonly V3CargoRowSweepSelectionGesture rowSweepSelection =
            new V3CargoRowSweepSelectionGesture();
        private readonly List<int> selectedIndices = new List<int>();
        private V3CargoUnloadModel model;
        private Vector2 availableScroll;
        private Vector2 selectedScroll;
        private bool selectedDirty = true;

        internal V3CargoUnloadPanelDrawer(
            V3CargoUnloadSelectionModel selection,
            V3CargoUnloadRowDrawer rowDrawer)
        {
            this.selection = selection;
            this.rowDrawer = rowDrawer;
        }

        internal void SetModel(V3CargoUnloadModel model)
        {
            this.model = model;
            this.availableScroll = Vector2.zero;
            this.selectedScroll = Vector2.zero;
            this.selectedDirty = true;
        }

        internal void ResetAvailableScroll()
        {
            this.availableScroll = Vector2.zero;
        }

        internal void MarkSelectedDirty()
        {
            this.selectedDirty = true;
        }

        internal void Draw(Rect rect, IList<int> filteredIndices)
        {
            float selectedWidth = Mathf.Clamp(rect.width * 0.34f, 320f, 410f);
            Rect available = new Rect(rect.x, rect.y, rect.width - selectedWidth - 10f, rect.height);
            Rect selected = new Rect(available.xMax + 10f, rect.y, selectedWidth, rect.height);
            this.DrawAvailablePanel(available, filteredIndices);
            this.DrawSelectedPanel(selected);
        }

        private void DrawAvailablePanel(Rect rect, IList<int> filteredIndices)
        {
            V3CargoLoadDialogStyle.DrawSurface(rect);
            Rect title = new Rect(rect.x + 8f, rect.y + 5f, rect.width - 16f, 24f);
            Rect header = new Rect(title.x, title.yMax + 3f, title.width, 22f);
            Rect list = new Rect(title.x, header.yMax + 2f, title.width, rect.yMax - header.yMax - 10f);
            int filteredCount = filteredIndices != null ? filteredIndices.Count : 0;
            V3CargoLoadDialogStyle.DrawSectionHeader(
                title,
                V3CargoLoadText.Tr(
                    "CT_Shuttle_Unload_AvailableTitle",
                    filteredCount,
                    this.model != null ? this.model.Rows.Count : 0));
            this.DrawAvailableHeader(header);
            this.DrawAvailableRows(list, filteredIndices);
        }

        private void DrawAvailableHeader(Rect rect)
        {
            V3CargoUnloadTableLayout layout = V3CargoUnloadTableLayout.Create(rect);
            this.DrawHeaderCell(layout.NameRect, V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_TableItem"), TextAnchor.MiddleLeft);
            this.DrawHeaderCell(layout.AvailableRect, V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_TableAvailable"), TextAnchor.MiddleRight);
            if (layout.ShowUnitMass)
            {
                this.DrawHeaderCell(layout.UnitMassRect, V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_TableUnitMass"), TextAnchor.MiddleRight);
            }

            if (layout.ShowSource)
            {
                this.DrawHeaderCell(layout.SourceRect, V3CargoLoadText.Tr("CT_Shuttle_Unload_Source"), TextAnchor.MiddleCenter);
            }

            this.DrawHeaderCell(layout.ActionRect, V3CargoLoadText.Tr("CT_Shuttle_Unload_Selected"), TextAnchor.MiddleCenter);
        }

        private void DrawAvailableRows(Rect rect, IList<int> filteredIndices)
        {
            this.rowSweepSelection.BeginFrame();
            int filteredCount = filteredIndices != null ? filteredIndices.Count : 0;
            if (filteredCount == 0)
            {
                V3CargoLoadDialogStyle.DrawEmptyText(rect, V3CargoLoadText.Tr("CT_Shuttle_Unload_NoMatchingCargo"));
                return;
            }

            float stride = RowHeight + RowGap;
            Rect view = new Rect(0f, 0f, rect.width - 16f, Mathf.Max(rect.height, filteredCount * stride));
            Widgets.BeginScrollView(rect, ref this.availableScroll, view);
            int first;
            int last;
            V3CargoLoadDialogStyle.GetVisibleRowRange(this.availableScroll, rect.height, filteredCount, stride, out first, out last);
            for (int i = first; i < last; i++)
            {
                int sourceIndex = filteredIndices[i];
                if (this.model == null || sourceIndex < 0 || sourceIndex >= this.model.Rows.Count)
                {
                    continue;
                }

                Rect rowRect = new Rect(0f, i * stride, view.width, RowHeight);
                V3CargoUnloadRowModel row = this.model.Rows[sourceIndex];
                this.rowDrawer.DrawAvailable(rowRect, row, i);
                this.HandleRowSweep(
                    rowRect,
                    filteredIndices,
                    row,
                    i);
            }

            Widgets.EndScrollView();
        }

        private void HandleRowSweep(
            Rect rowRect,
            IList<int> filteredIndices,
            V3CargoUnloadRowModel current,
            int filteredRowIndex)
        {
            int currentCount = current != null ? this.selection.Get(current) : 0;
            bool canEdit = this.rowDrawer.CanEditSelection();
            bool shouldSelect;
            int first;
            int last;
            if (!this.rowSweepSelection.TryHandleRow(
                    rowRect,
                    filteredRowIndex,
                    canEdit && currentCount > 0,
                    canEdit &&
                        current != null &&
                        current.AvailableCount > currentCount,
                    out shouldSelect,
                    out first,
                    out last))
            {
                return;
            }

            if (!canEdit)
            {
                return;
            }

            for (int i = first; i <= last; i++)
            {
                if (!this.rowSweepSelection.TryMarkRowForApplication(i) ||
                    filteredIndices == null ||
                    i < 0 ||
                    i >= filteredIndices.Count)
                {
                    continue;
                }

                int sourceIndex = filteredIndices[i];
                if (this.model == null ||
                    sourceIndex < 0 ||
                    sourceIndex >= this.model.Rows.Count)
                {
                    continue;
                }

                V3CargoUnloadRowModel row = this.model.Rows[sourceIndex];
                int selectedCount = this.selection.Get(row);
                if (shouldSelect && selectedCount < row.AvailableCount)
                {
                    this.selection.Set(row, row.AvailableCount);
                    this.selectedDirty = true;
                }
                else if (!shouldSelect && selectedCount > 0)
                {
                    this.selection.Set(row, 0);
                    this.selectedDirty = true;
                }
            }
        }

        private void DrawSelectedPanel(Rect rect)
        {
            V3CargoLoadDialogStyle.DrawSurface(rect);
            this.EnsureSelected();
            Rect title = new Rect(rect.x + 8f, rect.y + 5f, rect.width - 16f, 24f);
            Rect summary = new Rect(title.x, title.yMax + 3f, title.width, 38f);
            Rect list = new Rect(title.x, summary.yMax + 5f, title.width, rect.yMax - summary.yMax - 12f);
            V3CargoLoadDialogStyle.DrawSectionHeader(title, V3CargoLoadText.Tr("CT_Shuttle_Unload_ManifestTitle"));
            V3CargoLoadDialogStyle.DrawSurface(summary);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                summary.ContractedBy(6f),
                V3CargoLoadText.Tr(
                    "CT_Shuttle_Unload_SelectionSummary",
                    this.selection.SelectedStackCount(this.model),
                    this.selection.SelectedThingCount(this.model),
                    ShuttleUIMetricFormatter.FormatKgCompact(this.selection.SelectedMassKg(this.model))),
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.MutedTextColor,
                null,
                TextAnchor.MiddleCenter);
            this.DrawSelectedRows(list);
        }

        private void DrawSelectedRows(Rect rect)
        {
            if (this.selectedIndices.Count == 0)
            {
                V3CargoLoadDialogStyle.DrawEmptyText(rect, V3CargoLoadText.Tr("CT_Shuttle_Unload_NoSelection"));
                return;
            }

            float stride = 48f;
            Rect view = new Rect(0f, 0f, rect.width - 16f, Mathf.Max(rect.height, this.selectedIndices.Count * stride));
            Widgets.BeginScrollView(rect, ref this.selectedScroll, view);
            int first;
            int last;
            V3CargoLoadDialogStyle.GetVisibleRowRange(this.selectedScroll, rect.height, this.selectedIndices.Count, stride, out first, out last);
            for (int i = first; i < last; i++)
            {
                int sourceIndex = this.selectedIndices[i];
                if (this.model == null || sourceIndex < 0 || sourceIndex >= this.model.Rows.Count)
                {
                    continue;
                }

                this.rowDrawer.DrawSelected(new Rect(0f, i * stride, view.width, 46f), this.model.Rows[sourceIndex], i);
            }

            Widgets.EndScrollView();
        }

        private void EnsureSelected()
        {
            if (!this.selectedDirty)
            {
                return;
            }

            this.selectedIndices.Clear();
            for (int i = 0; this.model != null && i < this.model.Rows.Count; i++)
            {
                if (this.selection.Get(this.model.Rows[i]) > 0)
                {
                    this.selectedIndices.Add(i);
                }
            }

            this.selectedDirty = false;
        }

        private void DrawHeaderCell(Rect rect, string label, TextAnchor anchor)
        {
            ShuttleUILayout.DrawFittedSingleLineLabel(rect.ContractedBy(2f), label, GameFont.Tiny, GameFont.Tiny, ShuttleUIStyle.MutedTextColor, null, anchor);
        }
    }
}
