using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Diagnostics;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared.Dialogs;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Tutorial;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal sealed class Dialog_ShuttleCargoLoadV3 : Window
    {
        private const int CompactIconTransferableThreshold = 300;

        private readonly IShuttleCargoLoadUIActions cargoLoadActions;
        private readonly IShuttleLoadCargoReadPort loadCargoReadPort;
        private readonly Action onClosed;
        private readonly V3CargoLoadTransferableMetricsResolver metricsResolver =
            new V3CargoLoadTransferableMetricsResolver();
        private readonly V3CargoLoadCategoryResolver categoryResolver =
            new V3CargoLoadCategoryResolver();
        private readonly V3CargoLoadAvailablePanelState availableState =
            new V3CargoLoadAvailablePanelState();
        private readonly V3CargoLoadSelectedPanelState selectedState =
            new V3CargoLoadSelectedPanelState();
        private readonly V3CargoLoadAvailablePanel availablePanel;
        private readonly V3CargoLoadSelectedPanel selectedPanel;
        private readonly V3CargoLoadSummaryPanel summaryPanel;
        private readonly V3CargoLoadBatchSelectionService batchSelectionService;
        private readonly V3CargoLoadBatchMenu batchMenu;
        private readonly V3QueuedLoadPanel queuedLoadPanel;
        private readonly ShuttleCargoLoadManifestPanel manifestPanel;
        private readonly ShuttleDialogTutorialController tutorial =
            new ShuttleDialogTutorialController(
                new ShuttleCargoLoadDialogTutorialDefinitionBuilder().Build(),
                ShuttleCargoLoadDialogTutorialDefinitionBuilder.ProgressKey);
        private ShuttleLoadCargoReadModel readModel;
        private List<TransferableOneWay> passengerTransferables;
        private List<TransferableOneWay> cargoTransferables;
        private V3CargoLoadSelectionModel selectionModel;
        private bool compactIconMode;

        internal Dialog_ShuttleCargoLoadV3(
            ShuttleLoadCargoReadModel readModel,
            IShuttleLoadCargoReadPort loadCargoReadPort,
            IShuttleCargoReadPort cargoReadPort,
            Action onClosed,
            IShuttleCargoLoadUIActions cargoLoadActions)
        {
            this.readModel = readModel ?? new ShuttleLoadCargoReadModel();
            this.loadCargoReadPort = loadCargoReadPort;
            this.onClosed = onClosed;
            this.cargoLoadActions = cargoLoadActions;
            this.passengerTransferables =
                this.readModel.PassengerTransferables ?? new List<TransferableOneWay>();
            this.cargoTransferables =
                this.readModel.CargoTransferables ?? new List<TransferableOneWay>();
            this.selectionModel = new V3CargoLoadSelectionModel(
                this.readModel,
                this.passengerTransferables,
                this.cargoTransferables,
                this.metricsResolver);
            this.batchSelectionService = new V3CargoLoadBatchSelectionService(
                this.selectionModel);
            this.batchMenu = new V3CargoLoadBatchMenu(
                this.batchSelectionService,
                this.categoryResolver,
                this.metricsResolver,
                this.OnBatchSelectionChanged);
            this.RefreshDerivedDisplayState();
            this.availablePanel = new V3CargoLoadAvailablePanel(
                this.availableState,
                this.metricsResolver,
                this.categoryResolver,
                this.selectionModel,
                this.AdjustSelection,
                this.SetSelectionCount,
                this.ClearSelection,
                this.SelectMaximumFit,
                this.UseCompactIconMode,
                this.OpenBatchMenu,
                this.CanOpenBatchMenuWithoutTargets);
            this.selectedPanel = new V3CargoLoadSelectedPanel(
                this.selectedState,
                this.metricsResolver,
                this.selectionModel,
                this.AdjustSelection,
                this.ClearSelection,
                this.UseCompactIconMode,
                this.ClearAllSelections);
            this.summaryPanel = new V3CargoLoadSummaryPanel(this.selectionModel);
            this.queuedLoadPanel = new V3QueuedLoadPanel(
                cargoReadPort,
                cargoLoadActions,
                this.HasQueuedLoads,
                this.OnQueuedLoadCommandCompleted);
            this.manifestPanel = new ShuttleCargoLoadManifestPanel(
                this.queuedLoadPanel,
                this.selectedPanel);
            this.forcePause = this.tutorial.ShouldPauseSimulation;
            this.doCloseX = true;
            this.closeOnClickedOutside = false;
            this.absorbInputAroundWindow = false;
            this.draggable = true;
            this.resizeable = false;
        }

        public override Vector2 InitialSize
        {
            get
            {
                const float targetWidth = 1200f;
                const float targetHeight = 820f;
                const float minWidth = 1000f;
                const float minHeight = 720f;
                const float screenMargin = 80f;
                float safeWidth = Mathf.Max(640f, Verse.UI.screenWidth - screenMargin);
                float safeHeight = Mathf.Max(520f, Verse.UI.screenHeight - screenMargin);
                float width = Mathf.Min(targetWidth, safeWidth);
                float height = Mathf.Min(targetHeight, safeHeight);
                if (safeWidth >= minWidth)
                {
                    width = Mathf.Max(minWidth, width);
                }

                if (safeHeight >= minHeight)
                {
                    height = Mathf.Max(minHeight, height);
                }

                return new Vector2(width, height);
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            this.tutorial.BeginFrame(inRect);
            this.forcePause = this.tutorial.ShouldPauseSimulation;
            ShuttleUILayout.DrawPanelBackground(inRect);
            Rect contentRect = inRect.ContractedBy(12f);
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
                    footerRect.y -
                    headerRect.yMax -
                    (V3CargoLoadDialogStyle.Gap * 2f)));
            Rect availableRect;
            Rect manifestRect;
            this.CalculateColumnRects(
                bodyRect,
                out availableRect,
                out manifestRect);

            this.DrawHeader(headerRect);
            this.DrawBody(availableRect, manifestRect);
            if (ShuttleCargoOpenPathDiagnosticFlags.LoadCargoSkipSummaryPanel)
            {
                this.DrawDiagnosticSummaryPlaceholder(
                    new Rect(
                        footerRect.x,
                        footerRect.y,
                        availableRect.width,
                        footerRect.height));
            }
            else
            {
                Rect summaryRect = new Rect(
                    footerRect.x,
                    footerRect.y,
                    availableRect.width,
                    footerRect.height);
                this.tutorial.Register(
                    ShuttleTutorialTargetIds.LoadDialogSummary,
                    summaryRect);
                this.summaryPanel.Draw(
                    summaryRect,
                    this.readModel,
                    this.GetPassengerCount(),
                    this.GetCargoCount());
            }

            this.DrawFooter(footerRect, manifestRect);
            this.HandleSubmitHotkey();

            this.tutorial.EndFrame(inRect);
            this.forcePause = this.tutorial.ShouldPauseSimulation;
        }

        public override void PostClose()
        {
            base.PostClose();
            try
            {
                if (this.selectionModel != null)
                {
                    this.selectionModel.ClearAllSelections();
                }
            }
            finally
            {
                if (this.onClosed != null)
                {
                    this.onClosed();
                }
            }
        }

        private void DrawHeader(Rect rect)
        {
            Rect titleRect = new Rect(rect.x + 4f, rect.y + 2f, rect.width - 240f, 26f);
            Rect hintRect = new Rect(rect.x + 4f, titleRect.yMax + 2f, rect.width - 240f, 22f);
            Rect currentRect = new Rect(rect.xMax - 220f, rect.y + 8f, 216f, 34f);

            Text.Font = GameFont.Medium;
            GUI.color = V3CargoLoadDialogStyle.AccentColor;
            ShuttleUILayout.SafeLabel(
                titleRect,
                V3CargoLoadDialogStyle.FitLabelText(
                    V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_ManagerTitle"),
                    titleRect.width));
            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.SafeLabel(
                hintRect,
                V3CargoLoadDialogStyle.FitLabelText(
                    V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_ManagerTooltip"),
                    hintRect.width));
            ShuttleUILayout.DrawFittedSingleLineLabel(
                currentRect,
                V3CargoLoadText.GetCurrentQueueChipText(this.readModel),
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.MutedTextColor,
                V3CargoLoadText.GetCurrentLoadSummary(this.readModel),
                TextAnchor.MiddleRight);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private void CalculateColumnRects(
            Rect rect,
            out Rect availableRect,
            out Rect manifestRect)
        {
            float usableWidth = Mathf.Max(0f, rect.width - V3CargoLoadDialogStyle.Gap);
            float leftWidth;
            float manifestWidth;
            if (usableWidth >= 900f)
            {
                leftWidth = Mathf.Max(560f, usableWidth * 0.64f);
                manifestWidth = usableWidth - leftWidth;
                if (manifestWidth < 320f)
                {
                    manifestWidth = 320f;
                    leftWidth = Mathf.Max(0f, usableWidth - manifestWidth);
                }
            }
            else
            {
                manifestWidth = Mathf.Max(220f, usableWidth * 0.38f);
                leftWidth = Mathf.Max(0f, usableWidth - manifestWidth);
            }

            availableRect = new Rect(rect.x, rect.y, leftWidth, rect.height);
            manifestRect = new Rect(
                availableRect.xMax + V3CargoLoadDialogStyle.Gap,
                rect.y,
                manifestWidth,
                rect.height);
        }

        private void DrawBody(Rect availableRect, Rect manifestRect)
        {
            this.tutorial.Register(
                ShuttleTutorialTargetIds.LoadDialogAvailablePanel,
                availableRect);

            this.availablePanel.Draw(
                availableRect,
                this.passengerTransferables,
                this.cargoTransferables);
            this.manifestPanel.Draw(manifestRect);
            this.tutorial.Register(
                ShuttleTutorialTargetIds.LoadDialogQueuePanel,
                this.manifestPanel.QueueRect);
            this.tutorial.Register(
                ShuttleTutorialTargetIds.LoadDialogPlanPanel,
                this.manifestPanel.PlanRect);
        }

        private void DrawDiagnosticSummaryPlaceholder(Rect rect)
        {
            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.SafeLabel(
                new Rect(rect.x, rect.y + 12f, rect.width, 18f),
                V3CargoLoadDialogStyle.FitLabelText(
                    "Load Cargo summary skipped by diagnostic mode",
                    rect.width));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private void DrawFooter(Rect rect, Rect manifestRect)
        {
            float buttonWidth = Mathf.Max(
                90f,
                Mathf.Min(
                    116f,
                    (manifestRect.width - V3CargoLoadDialogStyle.Gap) * 0.5f));
            Rect closeRect = new Rect(
                manifestRect.xMax - buttonWidth,
                rect.y + 7f,
                buttonWidth,
                V3CargoLoadDialogStyle.ButtonHeight);
            Rect loadRect = new Rect(
                closeRect.x - V3CargoLoadDialogStyle.Gap - buttonWidth,
                closeRect.y,
                buttonWidth,
                V3CargoLoadDialogStyle.ButtonHeight);

            this.tutorial.Register(
                ShuttleTutorialTargetIds.LoadDialogStartButton,
                loadRect);

            if (V3CargoLoadDialogStyle.DrawButton(
                loadRect,
                this.HasQueuedLoads()
                    ? V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_ReplaceAndStart")
                    : V3CargoLoadText.Tr("CT_Shuttle_UI_LoadCargo"),
                this.CanStartLoading(),
                V3CargoLoadDialogStyle.AccentColor,
                this.GetStartTooltip()))
            {
                this.TryAccept();
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

        private void OnQueuedLoadCommandCompleted()
        {
            this.RefreshReadModelSummary();
        }

        private void TryAccept()
        {
            if (this.readModel == null || !this.readModel.HasTransporter)
            {
                ShuttleUICommandFeedback.ShowReject(
                    V3CargoLoadText.Tr("CT_Shuttle_Cargo_NoTransporterAvailable"));
                return;
            }

            if (this.selectionModel.SelectedCount() <= 0)
            {
                ShuttleUICommandFeedback.ShowReject(
                    V3CargoLoadText.Tr("CT_Shuttle_Cargo_NoCargoSelected"));
                return;
            }

            if (this.selectionModel.HasSelectionOverCapacity())
            {
                ShuttleUICommandFeedback.ShowReject(
                    V3CargoLoadText.Tr("CT_Shuttle_Cargo_SelectedExceedsCapacity"));
                return;
            }

            if (this.HasQueuedLoads())
            {
                Dialog_ShuttleCargoLoadConfirmV3.Open(
                    V3CargoLoadText.Tr("CT_Shuttle_UI_LoadCargo"),
                    V3CargoLoadText.Tr("CT_Shuttle_UI_ExistingQueueWillBeReplaced"),
                    V3CargoLoadText.Tr("CT_Shuttle_UI_LoadCargo"),
                    ShuttleV3DialogButtonKind.Primary,
                    delegate { this.BeginLoading(true); });
                return;
            }

            this.BeginLoading(false);
        }

        private void BeginLoading(bool replaceExistingQueue)
        {
            if (this.cargoLoadActions == null)
            {
                ShuttleUICommandFeedback.ShowReject(
                    V3CargoLoadText.Tr("CT_Shuttle_Command_ExecutorUnavailable"));
                return;
            }

            if (this.cargoLoadActions.BeginLoadCargo(
                this.passengerTransferables,
                this.cargoTransferables,
                replaceExistingQueue))
            {
                this.Close(false);
            }
        }

        private bool CanStartLoading()
        {
            return this.readModel != null &&
                this.readModel.HasTransporter &&
                this.HasAnyLoadableObjects() &&
                this.selectionModel.SelectedCount() > 0 &&
                !this.selectionModel.HasSelectionOverCapacity();
        }

        private string GetStartTooltip()
        {
            if (this.readModel == null || !this.readModel.HasTransporter)
            {
                return V3CargoLoadText.Tr("CT_Shuttle_Cargo_NoTransporterAvailable");
            }

            if (!this.HasAnyLoadableObjects())
            {
                return V3CargoLoadText.Tr("CT_Shuttle_Cargo_NoItems");
            }

            if (this.selectionModel.SelectedCount() <= 0)
            {
                return V3CargoLoadText.Tr("CT_Shuttle_Cargo_NoCargoSelected");
            }

            if (this.selectionModel.HasSelectionOverCapacity())
            {
                return V3CargoLoadText.Tr("CT_Shuttle_Cargo_SelectedExceedsCapacity");
            }

            return V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_HeaderTooltip");
        }

        private void AdjustSelection(TransferableOneWay transferable, int delta)
        {
            int before = transferable != null ? transferable.CountToTransfer : 0;
            string rejectMessageKey;
            this.selectionModel.AdjustSelection(transferable, delta, out rejectMessageKey);
            if (transferable != null && transferable.CountToTransfer != before)
            {
                this.batchSelectionService.ClearUndo();
            }

            if (!string.IsNullOrEmpty(rejectMessageKey))
            {
                ShuttleUICommandFeedback.ShowReject(V3CargoLoadText.Tr(rejectMessageKey));
            }
        }

        private void SelectMaximumFit(TransferableOneWay transferable)
        {
            int before = transferable != null ? transferable.CountToTransfer : 0;
            int applied = this.selectionModel.SelectMaximumFit(transferable);
            if (applied != before)
            {
                this.batchSelectionService.ClearUndo();
            }

            if (transferable != null && applied == before)
            {
                ShuttleUICommandFeedback.ShowReject(
                    V3CargoLoadText.Tr("CT_Shuttle_Cargo_SelectedExceedsCapacity"));
            }
        }

        private void SetSelectionCount(
            TransferableOneWay transferable,
            int requestedCount)
        {
            int before = transferable != null ? transferable.CountToTransfer : 0;
            int appliedCount;
            bool applied = this.selectionModel.TrySetSelectionCountExact(
                transferable,
                requestedCount,
                out appliedCount);
            if (appliedCount != before)
            {
                this.batchSelectionService.ClearUndo();
            }

            if (!applied)
            {
                ShuttleUICommandFeedback.ShowReject(
                    V3CargoLoadText.Tr("CT_Shuttle_Cargo_SelectedExceedsCapacity"));
            }
        }

        private void ClearSelection(TransferableOneWay transferable)
        {
            if (transferable == null)
            {
                return;
            }

            int appliedCount;
            this.selectionModel.TrySetSelectionCountExact(
                transferable,
                0,
                out appliedCount);
            this.batchSelectionService.ClearUndo();
        }

        private void ClearAllSelections()
        {
            if (this.batchMenu != null)
            {
                this.batchMenu.ClearAllSelections();
            }
        }

        private void OpenBatchMenu(
            List<TransferableOneWay> targets,
            bool passengers)
        {
            if (this.batchMenu != null)
            {
                this.batchMenu.Open(targets, passengers);
            }
        }

        private bool CanOpenBatchMenuWithoutTargets()
        {
            return this.batchMenu != null && this.batchMenu.CanOpenWithoutTargets;
        }

        private void OnBatchSelectionChanged()
        {
            this.selectedState.Scroll.y = Mathf.Max(0f, this.selectedState.Scroll.y);
        }

        private void RefreshReadModelSummary()
        {
            if (this.loadCargoReadPort == null)
            {
                return;
            }

            Dictionary<string, int> selectedCountsByKey =
                this.selectionModel.CaptureSelectedCountsByKey();
            ShuttleLoadCargoReadModel refreshed = this.loadCargoReadPort.RefreshLoadCargoReadModel();
            if (refreshed == null)
            {
                return;
            }

            this.readModel = refreshed;
            this.passengerTransferables =
                this.readModel.PassengerTransferables ?? new List<TransferableOneWay>();
            this.cargoTransferables =
                this.readModel.CargoTransferables ?? new List<TransferableOneWay>();
            this.metricsResolver.Clear();
            this.categoryResolver.Clear();
            this.selectionModel.Rebind(
                this.readModel,
                this.passengerTransferables,
                this.cargoTransferables);
            this.selectionModel.RestoreSelectedCountsByKey(selectedCountsByKey);
            this.batchSelectionService.ClearUndo();
            this.RefreshDerivedDisplayState();
            this.availableState.MarkFilterDirty();
            this.availableState.Scroll.y = Mathf.Max(0f, this.availableState.Scroll.y);
            this.selectedState.Scroll.y = Mathf.Max(0f, this.selectedState.Scroll.y);
        }

        private void RefreshDerivedDisplayState()
        {
            int passengerCount = this.passengerTransferables != null ? this.passengerTransferables.Count : 0;
            int cargoCount = this.cargoTransferables != null ? this.cargoTransferables.Count : 0;
            this.compactIconMode =
                passengerCount + cargoCount > CompactIconTransferableThreshold;
        }

        private bool UseCompactIconMode()
        {
            return this.compactIconMode;
        }

        private bool HasQueuedLoads()
        {
            return this.loadCargoReadPort != null
                ? this.loadCargoReadPort.HasQueuedLoads()
                : this.readModel != null && this.readModel.HasQueuedLoads;
        }

        private bool HasAnyLoadableObjects()
        {
            return this.GetPassengerCount() > 0 || this.GetCargoCount() > 0;
        }

        private int GetPassengerCount()
        {
            return this.passengerTransferables != null ? this.passengerTransferables.Count : 0;
        }

        private int GetCargoCount()
        {
            return this.cargoTransferables != null ? this.cargoTransferables.Count : 0;
        }

        private void HandleSubmitHotkey()
        {
            Event evt = Event.current;
            if (evt == null ||
                evt.type != EventType.KeyDown ||
                evt.keyCode != KeyCode.Return)
            {
                return;
            }

            if (GUI.GetNameOfFocusedControl() == V3CargoLoadDialogStyle.SearchControlName)
            {
                return;
            }

            this.TryAccept();
            evt.Use();
        }
    }

    internal sealed class Dialog_ShuttleCargoLoadConfirmV3 : Window
    {
        private const float WindowWidth = 460f;
        private const float WindowHeight = 210f;
        private const float ButtonWidth = 132f;
        private const float ButtonHeight = 32f;

        private readonly string title;
        private readonly string message;
        private readonly string confirmLabel;
        private readonly ShuttleV3DialogButtonKind confirmKind;
        private readonly Action onConfirm;

        internal Dialog_ShuttleCargoLoadConfirmV3(
            string title,
            string message,
            string confirmLabel,
            ShuttleV3DialogButtonKind confirmKind,
            Action onConfirm)
        {
            this.title = string.IsNullOrEmpty(title) ? "-" : title;
            this.message = string.IsNullOrEmpty(message) ? "-" : message;
            this.confirmLabel = string.IsNullOrEmpty(confirmLabel) ? "OK".Translate().ToString() : confirmLabel;
            this.confirmKind = confirmKind;
            this.onConfirm = onConfirm;
            this.forcePause = false;
            this.doCloseX = true;
            this.closeOnClickedOutside = false;
            this.absorbInputAroundWindow = true;
            this.draggable = true;
            this.resizeable = false;
        }

        public override Vector2 InitialSize
        {
            get { return new Vector2(WindowWidth, WindowHeight); }
        }

        internal static void Open(
            string title,
            string message,
            string confirmLabel,
            ShuttleV3DialogButtonKind confirmKind,
            Action onConfirm)
        {
            if (Find.WindowStack == null)
            {
                if (onConfirm != null)
                {
                    onConfirm();
                }

                return;
            }

            Find.WindowStack.Add(new Dialog_ShuttleCargoLoadConfirmV3(
                title,
                message,
                confirmLabel,
                confirmKind,
                onConfirm));
        }

        public override void DoWindowContents(Rect inRect)
        {
            Color oldColor = GUI.color;
            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            bool oldWordWrap = Text.WordWrap;

            ShuttleV3DialogLayout.DrawPanelBackground(inRect);
            Rect contentRect = inRect.ContractedBy(14f);
            Rect titleRect = new Rect(contentRect.x + 2f, contentRect.y + 2f, contentRect.width - 4f, 28f);
            Rect lineRect = new Rect(contentRect.x, titleRect.yMax + 7f, contentRect.width, 1f);
            Rect messageRect = new Rect(
                contentRect.x + 4f,
                lineRect.yMax + 14f,
                contentRect.width - 8f,
                72f);
            Rect buttonRow = new Rect(
                contentRect.x,
                contentRect.yMax - ButtonHeight,
                contentRect.width,
                ButtonHeight);
            Rect cancelRect = new Rect(
                buttonRow.xMax - ButtonWidth,
                buttonRow.y,
                ButtonWidth,
                ButtonHeight);
            Rect confirmRect = new Rect(
                cancelRect.x - ShuttleV3DialogStyle.Gap - ButtonWidth,
                buttonRow.y,
                ButtonWidth,
                ButtonHeight);

            Text.Font = GameFont.Medium;
            GUI.color = ShuttleV3DialogStyle.HeaderTitleTextColor;
            ShuttleV3DialogLayout.SafeLabel(
                titleRect,
                ShuttleV3DialogLayout.FitSingleLineText(this.title, titleRect.width));
            Widgets.DrawBoxSolid(lineRect, ShuttleV3DialogStyle.SubtleBorderColor);

            this.DrawMessage(messageRect);

            if (ShuttleV3DialogLayout.DrawDialogButton(
                confirmRect,
                this.confirmLabel,
                true,
                this.confirmKind,
                null))
            {
                this.Close(false);
                if (this.onConfirm != null)
                {
                    this.onConfirm();
                }
            }

            if (ShuttleV3DialogLayout.DrawDialogButton(
                cancelRect,
                V3CargoLoadText.Tr("CT_Shuttle_UI_Cancel"),
                true,
                ShuttleV3DialogButtonKind.Normal,
                null))
            {
                this.Close(false);
            }

            GUI.color = oldColor;
            Text.Font = oldFont;
            Text.Anchor = oldAnchor;
            Text.WordWrap = oldWordWrap;
        }

        private void DrawMessage(Rect rect)
        {
            Color oldColor = GUI.color;
            GameFont oldFont = Text.Font;
            bool oldWordWrap = Text.WordWrap;
            try
            {
                Text.Font = GameFont.Small;
                Text.WordWrap = true;
                GUI.color = ShuttleV3DialogStyle.MutedTextColor;
                Widgets.Label(rect, this.message);
            }
            finally
            {
                GUI.color = oldColor;
                Text.Font = oldFont;
                Text.WordWrap = oldWordWrap;
            }
        }
    }
}
