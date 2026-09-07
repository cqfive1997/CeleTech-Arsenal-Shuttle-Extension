using System;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared.Dialogs;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal sealed class V3QueuedLoadPanel
    {
        private const int RefreshIntervalTicks = 60;

        private readonly IShuttleCargoReadPort cargoReadPort;
        private readonly IShuttleCargoLoadUIActions cargoLoadActions;
        private readonly Func<bool> hasQueuedLoads;
        private readonly Action onCommandCompleted;
        private readonly V3QueuedLoadDialogModelBuilder modelBuilder =
            new V3QueuedLoadDialogModelBuilder();
        private readonly ShuttleQueuedLoadCompactRowDrawer rowDrawer =
            new ShuttleQueuedLoadCompactRowDrawer();

        private V3QueuedLoadDialogModel model;
        private Vector2 queueScroll;
        private int lastRefreshTick = int.MinValue;

        internal V3QueuedLoadPanel(
            IShuttleCargoReadPort cargoReadPort,
            IShuttleCargoLoadUIActions cargoLoadActions,
            Func<bool> hasQueuedLoads,
            Action onCommandCompleted)
        {
            this.cargoReadPort = cargoReadPort;
            this.cargoLoadActions = cargoLoadActions;
            this.hasQueuedLoads = hasQueuedLoads;
            this.onCommandCompleted = onCommandCompleted;
        }

        internal V3QueuedLoadDialogModel CurrentModel
        {
            get
            {
                this.RefreshIfNeeded();
                return this.model;
            }
        }

        internal bool CanClear
        {
            get
            {
                V3QueuedLoadDialogModel current = this.CurrentModel;
                return (current != null &&
                        current.HasRows &&
                        current.QueuedThingCount > 0) ||
                    (this.hasQueuedLoads != null && this.hasQueuedLoads());
            }
        }

        internal void Draw(Rect rect)
        {
            V3QueuedLoadDialogModel current = this.CurrentModel;
            this.DrawQueueList(rect, current);
        }

        internal void ConfirmClear()
        {
            if (!this.CanClear)
            {
                return;
            }

            Dialog_ShuttleCargoLoadConfirmV3.Open(
                V3CargoLoadText.Tr("CT_Shuttle_UI_ClearQueue"),
                V3CargoLoadText.Tr("CT_Shuttle_UI_ClearQueuedLoadConfirm"),
                V3CargoLoadText.Tr("CT_Shuttle_UI_ClearQueue"),
                ShuttleV3DialogButtonKind.Danger,
                this.ClearQueuedLoad);
        }

        internal string GetHeaderChipText()
        {
            V3QueuedLoadDialogModel current = this.CurrentModel;
            if (current == null || !current.SnapshotAvailable)
            {
                return V3CargoLoadText.Tr("CT_Shuttle_Command_ContextUnavailable");
            }

            return V3CargoLoadText.Tr(
                "CT_Shuttle_QueuedLoad_HeaderChip",
                current.RowCount,
                current.QueuedThingCount);
        }

        internal void Refresh()
        {
            this.model = this.modelBuilder.Build(this.cargoReadPort);
            this.lastRefreshTick = this.GetTicksGame();
            this.queueScroll.y = Mathf.Max(0f, this.queueScroll.y);
        }

        private void RefreshIfNeeded()
        {
            int ticksGame = this.GetTicksGame();
            bool due = this.model == null ||
                this.lastRefreshTick == int.MinValue ||
                ticksGame < this.lastRefreshTick ||
                ticksGame - this.lastRefreshTick >= RefreshIntervalTicks;
            Event currentEvent = Event.current;
            if (!due ||
                (this.model != null &&
                    currentEvent != null &&
                    currentEvent.type != EventType.Repaint))
            {
                return;
            }

            this.Refresh();
        }

        private void DrawQueueList(
            Rect rect,
            V3QueuedLoadDialogModel current)
        {
            V3CargoLoadDialogStyle.DrawSurface(rect);
            V3CargoLoadDialogStyle.DrawSectionHeader(
                rect,
                V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_QueueSection"));

            Rect clearRect = new Rect(rect.xMax - 92f, rect.y + 3f, 82f, 24f);
            if (V3CargoLoadDialogStyle.DrawTextAction(
                clearRect,
                V3CargoLoadText.Tr("CT_Shuttle_UI_ClearQueue"),
                this.CanClear,
                V3CargoLoadDialogStyle.RedColor,
                V3CargoLoadText.Tr("CT_Shuttle_QueuedLoad_ClearQueueTooltip")))
            {
                this.ConfirmClear();
            }

            Rect summaryRect = new Rect(
                rect.x + 10f,
                rect.y + 32f,
                Mathf.Max(0f, rect.width - 20f),
                16f);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                summaryRect,
                this.GetInlineSummary(current),
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.MutedTextColor,
                null,
                TextAnchor.MiddleLeft);

            Rect listRect = new Rect(
                rect.x + 10f,
                rect.y + 50f,
                rect.width - 20f,
                Mathf.Max(0f, rect.height - 60f));
            if (current == null || !current.SnapshotAvailable)
            {
                V3CargoLoadDialogStyle.DrawEmptyText(
                    listRect,
                    V3CargoLoadText.Tr("CT_Shuttle_Command_ContextUnavailable"));
                return;
            }

            if (!current.HasRows)
            {
                return;
            }

            float rowStride =
                V3CargoLoadDialogStyle.CompactQueueRowHeight +
                V3CargoLoadDialogStyle.CompactQueueRowGap;
            Rect viewRect = new Rect(
                0f,
                0f,
                listRect.width - 16f,
                Mathf.Max(listRect.height, current.Rows.Count * rowStride));

            Widgets.BeginScrollView(listRect, ref this.queueScroll, viewRect);
            int firstIndex;
            int lastIndexExclusive;
            V3CargoLoadDialogStyle.GetVisibleRowRange(
                this.queueScroll,
                listRect.height,
                current.Rows.Count,
                rowStride,
                out firstIndex,
                out lastIndexExclusive);

            for (int i = firstIndex; i < lastIndexExclusive; i++)
            {
                Rect rowRect = new Rect(
                    0f,
                    i * rowStride,
                    viewRect.width,
                    V3CargoLoadDialogStyle.CompactQueueRowHeight);
                V3QueuedLoadRowModel row = current.Rows[i];
                this.HandleRowCommand(this.rowDrawer.Draw(rowRect, row), row);
            }

            Widgets.EndScrollView();
        }

        private string GetInlineSummary(V3QueuedLoadDialogModel current)
        {
            if (current == null || !current.SnapshotAvailable)
            {
                return V3CargoLoadText.Tr("CT_Shuttle_Command_ContextUnavailable");
            }

            if (!current.HasRows)
            {
                return V3CargoLoadText.Tr("CT_Shuttle_UI_NoQueuedLoadingEntries");
            }

            return V3CargoLoadText.Tr(
                "CT_Shuttle_LoadCargo_QueueInlineSummary",
                current.RowCount,
                current.QueuedThingCount,
                ShuttleUIMetricFormatter.FormatKgCompact(current.QueuedMassKg));
        }

        private void HandleRowCommand(
            V3QueuedLoadRowCommand command,
            V3QueuedLoadRowModel row)
        {
            if (command == V3QueuedLoadRowCommand.CancelPartial)
            {
                this.CancelEntry(row, this.GetPartialCancelCount(row));
                return;
            }

            if (command == V3QueuedLoadRowCommand.CancelAll)
            {
                this.CancelEntry(row, row != null ? row.StackCount : 0);
            }
        }

        private void ClearQueuedLoad()
        {
            if (this.cargoLoadActions == null)
            {
                ShuttleUICommandFeedback.ShowReject(
                    V3CargoLoadText.Tr("CT_Shuttle_Command_ExecutorUnavailable"));
                return;
            }

            if (this.cargoLoadActions.ClearQueuedLoad())
            {
                this.Refresh();
                this.NotifyCommandCompleted();
            }
        }

        private void CancelEntry(V3QueuedLoadRowModel row, int count)
        {
            if (row == null || !row.CanCancel || count <= 0)
            {
                ShuttleUICommandFeedback.ShowReject(
                    V3CargoLoadText.Tr("CT_Shuttle_Cargo_QueuedEntryUnavailable"));
                return;
            }

            if (this.cargoLoadActions == null)
            {
                ShuttleUICommandFeedback.ShowReject(
                    V3CargoLoadText.Tr("CT_Shuttle_Command_ExecutorUnavailable"));
                return;
            }

            if (this.cargoLoadActions.CancelQueuedLoadEntry(
                row.TransporterIndex,
                row.QueueIndex,
                count))
            {
                this.Refresh();
                this.NotifyCommandCompleted();
            }
        }

        private int GetPartialCancelCount(V3QueuedLoadRowModel row)
        {
            int stackCount = row != null ? row.StackCount : 0;
            if (stackCount <= 1)
            {
                return 1;
            }

            int step = V3CargoLoadDialogStyle.GetStepFromCurrentEvent();
            return step < stackCount ? step : stackCount;
        }

        private int GetTicksGame()
        {
            return Find.TickManager != null ? Find.TickManager.TicksGame : 0;
        }

        private void NotifyCommandCompleted()
        {
            if (this.onCommandCompleted != null)
            {
                this.onCommandCompleted();
            }
        }
    }
}
