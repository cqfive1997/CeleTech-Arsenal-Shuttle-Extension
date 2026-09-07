using System;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Cargo;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal sealed class Dialog_ShuttleQueuedLoadV3 : Window
    {
        private const float InitialWidth = 900f;
        private const float InitialHeight = 620f;

        private readonly IShuttleCargoReadPort cargoReadPort;
        private readonly IShuttleCargoLoadUIActions cargoLoadActions;
        private readonly Action onClosed;
        private readonly Action onCommandCompleted;
        private readonly V3QueuedLoadDialogModelBuilder modelBuilder =
            new V3QueuedLoadDialogModelBuilder();
        private readonly V3QueuedLoadRowDrawer rowDrawer =
            new V3QueuedLoadRowDrawer();
        private readonly V3QueuedLoadSummaryDrawer summaryDrawer =
            new V3QueuedLoadSummaryDrawer();
        private Vector2 queueScroll;

        internal Dialog_ShuttleQueuedLoadV3(
            IShuttleCargoReadPort cargoReadPort,
            IShuttleCargoLoadUIActions cargoLoadActions,
            Action onClosed,
            Action onCommandCompleted)
        {
            this.cargoReadPort = cargoReadPort;
            this.cargoLoadActions = cargoLoadActions;
            this.onClosed = onClosed;
            this.onCommandCompleted = onCommandCompleted;
            this.forcePause = false;
            this.doCloseX = true;
            this.closeOnClickedOutside = false;
            this.absorbInputAroundWindow = false;
            this.draggable = true;
            this.resizeable = false;
        }

        public override Vector2 InitialSize
        {
            get { return new Vector2(InitialWidth, InitialHeight); }
        }

        public override void DoWindowContents(Rect inRect)
        {
            ShuttleUILayout.DrawPanelBackground(inRect);
            Rect contentRect = inRect.ContractedBy(12f);
            V3QueuedLoadDialogModel model = this.modelBuilder.Build(this.cargoReadPort);

            Rect headerRect = new Rect(
                contentRect.x,
                contentRect.y,
                contentRect.width,
                V3CargoLoadDialogStyle.HeaderHeight);
            Rect footerRect = new Rect(
                contentRect.x,
                contentRect.yMax - V3CargoLoadDialogStyle.FooterHeight,
                contentRect.width,
                V3CargoLoadDialogStyle.FooterHeight);
            Rect bodyRect = new Rect(
                contentRect.x,
                headerRect.yMax + V3CargoLoadDialogStyle.Gap,
                contentRect.width,
                Mathf.Max(
                    0f,
                    footerRect.y - headerRect.yMax - (V3CargoLoadDialogStyle.Gap * 2f)));

            this.DrawHeader(headerRect, model);
            this.DrawBody(bodyRect, model);
            this.DrawFooter(footerRect, model);
        }

        public override void PostClose()
        {
            base.PostClose();
            if (this.onClosed != null)
            {
                this.onClosed();
            }
        }

        private void DrawHeader(Rect rect, V3QueuedLoadDialogModel model)
        {
            Rect titleRect = new Rect(rect.x + 4f, rect.y + 3f, rect.width - 252f, 26f);
            Rect hintRect = new Rect(rect.x + 4f, titleRect.yMax + 2f, rect.width - 252f, 22f);
            Rect currentRect = new Rect(rect.xMax - 232f, rect.y + 8f, 228f, 34f);

            Text.Font = GameFont.Medium;
            GUI.color = V3CargoLoadDialogStyle.AccentColor;
            ShuttleUILayout.SafeLabel(
                titleRect,
                V3CargoLoadDialogStyle.FitLabelText(
                    V3CargoLoadText.Tr("CT_Shuttle_UI_TaskQueue"),
                    titleRect.width));
            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.SafeLabel(
                hintRect,
                V3CargoLoadDialogStyle.FitLabelText(
                    V3CargoLoadText.Tr("CT_Shuttle_Header_QueuedLoadTooltip"),
                    hintRect.width));
            ShuttleUILayout.DrawCardBackground(
                currentRect,
                false,
                false,
                ShuttleUIStyle.TimeChipColor);
            GUI.color = Color.white;
            ShuttleUILayout.SafeLabel(
                new Rect(currentRect.x + 8f, currentRect.y + 7f, currentRect.width - 16f, 20f),
                V3CargoLoadDialogStyle.FitLabelText(
                    this.GetHeaderChipText(model),
                    currentRect.width - 16f));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private void DrawBody(Rect rect, V3QueuedLoadDialogModel model)
        {
            Rect listRect = new Rect(
                rect.x,
                rect.y,
                rect.width - V3CargoLoadDialogStyle.SummaryWidth - V3CargoLoadDialogStyle.Gap,
                rect.height);
            Rect summaryRect = new Rect(
                listRect.xMax + V3CargoLoadDialogStyle.Gap,
                rect.y,
                V3CargoLoadDialogStyle.SummaryWidth,
                rect.height);

            this.DrawQueueList(listRect, model);
            this.summaryDrawer.Draw(summaryRect, model);
        }

        private void DrawQueueList(Rect rect, V3QueuedLoadDialogModel model)
        {
            V3CargoLoadDialogStyle.DrawSurface(rect);
            V3CargoLoadDialogStyle.DrawSectionHeader(
                rect,
                V3CargoLoadText.Tr("CT_Shuttle_QueuedLoad_CurrentQueue"));

            Rect listRect = new Rect(rect.x + 10f, rect.y + 40f, rect.width - 20f, rect.height - 50f);
            if (model == null || !model.SnapshotAvailable)
            {
                V3CargoLoadDialogStyle.DrawEmptyText(
                    listRect,
                    V3CargoLoadText.Tr("CT_Shuttle_Command_ContextUnavailable"));
                return;
            }

            if (!model.HasRows)
            {
                V3CargoLoadDialogStyle.DrawEmptyText(
                    listRect,
                    V3CargoLoadText.Tr("CT_Shuttle_UI_NoQueuedLoadingEntries"));
                return;
            }

            float rowStride = V3CargoLoadDialogStyle.RowHeight + V3CargoLoadDialogStyle.Gap;
            Rect viewRect = new Rect(
                0f,
                0f,
                listRect.width - 16f,
                Mathf.Max(listRect.height, model.Rows.Count * rowStride));

            Widgets.BeginScrollView(listRect, ref this.queueScroll, viewRect);
            int firstIndex;
            int lastIndexExclusive;
            V3CargoLoadDialogStyle.GetVisibleRowRange(
                this.queueScroll,
                listRect.height,
                model.Rows.Count,
                rowStride,
                out firstIndex,
                out lastIndexExclusive);

            for (int i = firstIndex; i < lastIndexExclusive; i++)
            {
                Rect rowRect = new Rect(
                    0f,
                    i * rowStride,
                    viewRect.width,
                    V3CargoLoadDialogStyle.RowHeight);
                this.HandleRowCommand(
                    this.rowDrawer.Draw(rowRect, model.Rows[i]),
                    model.Rows[i]);
            }

            Widgets.EndScrollView();
        }

        private void DrawFooter(Rect rect, V3QueuedLoadDialogModel model)
        {
            Rect clearRect = new Rect(
                rect.x,
                rect.y + 7f,
                132f,
                V3CargoLoadDialogStyle.ButtonHeight);
            Rect closeRect = new Rect(
                rect.xMax - 116f,
                clearRect.y,
                116f,
                V3CargoLoadDialogStyle.ButtonHeight);

            bool canClear = model != null && model.HasRows && model.QueuedThingCount > 0;
            if (V3CargoLoadDialogStyle.DrawButton(
                clearRect,
                V3CargoLoadText.Tr("CT_Shuttle_UI_ClearQueue"),
                canClear,
                V3CargoLoadDialogStyle.RedColor,
                V3CargoLoadText.Tr("CT_Shuttle_QueuedLoad_ClearQueueTooltip")))
            {
                this.ClearQueuedLoad();
            }

            if (V3CargoLoadDialogStyle.DrawButton(
                closeRect,
                V3CargoLoadText.Tr("CT_Shuttle_UI_Close"),
                true,
                ShuttleUIStyle.BorderColor,
                null))
            {
                this.Close(false);
            }
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
                this.NotifyCommandCompleted();
            }
        }

        private void CancelEntry(V3QueuedLoadRowModel row, int count)
        {
            if (!this.CanCancel(row, count))
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
                this.NotifyCommandCompleted();
            }
        }

        private bool CanCancel(V3QueuedLoadRowModel row, int count)
        {
            return row != null && row.CanCancel && count > 0;
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

        private string GetHeaderChipText(V3QueuedLoadDialogModel model)
        {
            if (model == null || !model.SnapshotAvailable)
            {
                return V3CargoLoadText.Tr("CT_Shuttle_Command_ContextUnavailable");
            }

            return V3CargoLoadText.Tr(
                    "CT_Shuttle_QueuedLoad_HeaderChip",
                    model.RowCount,
                    model.QueuedThingCount);
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
