using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Tutorial;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal sealed class Dialog_ShuttleCargoUnloadV3 : Window
    {
        private const int ProgressRefreshTicks = 30;

        private readonly IShuttleCargoReadPort cargoReadPort;
        private readonly IShuttleCargoUnloadReadPort unloadReadPort;
        private readonly ShuttleCargoUnloadUIActions actions;
        private readonly Action onClosed;
        private readonly V3CargoLoadCategoryResolver categoryResolver =
            new V3CargoLoadCategoryResolver();
        private readonly V3CargoUnloadModelBuilder modelBuilder =
            new V3CargoUnloadModelBuilder();
        private readonly V3CargoUnloadSelectionModel selection =
            new V3CargoUnloadSelectionModel();
        private readonly V3CargoUnloadRowDrawer rowDrawer;
        private readonly V3CargoUnloadFilterController filterController;
        private readonly V3CargoUnloadBatchMenu batchMenu;
        private readonly V3CargoUnloadPanelDrawer panelDrawer;
        private readonly ShuttleDialogTutorialController tutorial =
            new ShuttleDialogTutorialController(
                new ShuttleCargoUnloadDialogTutorialDefinitionBuilder().Build(),
                ShuttleCargoUnloadDialogTutorialDefinitionBuilder.ProgressKey);

        private V3CargoUnloadModel model;
        private ShuttleCargoUnloadProgressSnapshot progress;
        private int nextProgressRefreshTick;
        private int lastProgressRevision = -1;
        private bool observedActiveUnload;

        internal Dialog_ShuttleCargoUnloadV3(
            IShuttleCargoReadPort cargoReadPort,
            IShuttleCargoUnloadReadPort unloadReadPort,
            ShuttleCargoUnloadUIActions actions,
            Action onClosed)
        {
            this.cargoReadPort = cargoReadPort;
            this.unloadReadPort = unloadReadPort;
            this.actions = actions;
            this.onClosed = onClosed;
            this.rowDrawer = new V3CargoUnloadRowDrawer(
                this.selection,
                this.SetCount,
                delegate { return !this.IsActive; });
            this.filterController = new V3CargoUnloadFilterController(this.categoryResolver);
            this.batchMenu = new V3CargoUnloadBatchMenu(
                this.categoryResolver,
                this.filterController,
                this.selection,
                this.OnBatchSelectionChanged);
            this.panelDrawer = new V3CargoUnloadPanelDrawer(this.selection, this.rowDrawer);
            this.RefreshCargoModel();
            this.RefreshProgress(true);
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
                float width = Mathf.Min(1200f, Mathf.Max(680f, Verse.UI.screenWidth - 80f));
                float height = Mathf.Min(820f, Mathf.Max(540f, Verse.UI.screenHeight - 80f));
                return new Vector2(width, height);
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            this.tutorial.BeginFrame(inRect);
            this.forcePause = this.tutorial.ShouldPauseSimulation;
            this.RefreshProgress(false);
            V3CargoLoadDialogStyle.DrawSurface(inRect);

            Rect header = new Rect(inRect.x + 10f, inRect.y + 6f, inRect.width - 20f, 52f);
            Rect toolbar = new Rect(header.x, header.yMax + 6f, header.width, 34f);
            Rect footer = new Rect(header.x, inRect.yMax - 42f, header.width, 34f);
            Rect body = new Rect(header.x, toolbar.yMax + 8f, header.width, footer.y - toolbar.yMax - 16f);

            this.DrawHeader(header);
            this.DrawToolbar(toolbar);
            this.DrawBody(body);
            this.DrawFooter(footer);
            this.tutorial.EndFrame(inRect);
            this.forcePause = this.tutorial.ShouldPauseSimulation;
        }

        public override void PostClose()
        {
            base.PostClose();
            if (this.onClosed != null)
            {
                this.onClosed();
            }
        }

        private void DrawHeader(Rect rect)
        {
            float statusWidth = rect.width < 820f ? 300f : 400f;
            float titleWidth = Mathf.Max(180f, rect.width - statusWidth - 12f);
            Text.Font = GameFont.Medium;
            GUI.color = V3CargoLoadDialogStyle.AccentColor;
            ShuttleUILayout.SafeLabel(
                new Rect(rect.x, rect.y, titleWidth, 28f),
                V3CargoLoadText.Tr("CT_Shuttle_Unload_ManagerTitle"));
            GUI.color = Color.white;
            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.SafeLabel(
                new Rect(rect.x, rect.y + 30f, titleWidth, 18f),
                V3CargoLoadText.Tr("CT_Shuttle_Unload_ManagerSubtitle"));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            Rect status = new Rect(rect.xMax - statusWidth, rect.y + 2f, statusWidth, 46f);
            this.tutorial.Register(
                ShuttleTutorialTargetIds.UnloadDialogLoadBar,
                status);
            this.DrawCargoLoadBar(status);
        }

        private void DrawCargoLoadBar(Rect rect)
        {
            float currentMassKg = this.model != null ? this.model.TotalMassKg : 0f;
            float capacityKg = this.model != null ? this.model.CapacityKg : 0f;
            float value01 = capacityKg > 0.0001f
                ? currentMassKg / capacityKg
                : currentMassKg > 0.0001f ? 1f : 0f;
            Color accent = currentMassKg > capacityKg + 0.0001f
                ? V3CargoLoadDialogStyle.RedColor
                : this.IsActive
                    ? V3CargoLoadDialogStyle.YellowColor
                    : V3CargoLoadDialogStyle.AccentColor;

            V3CargoLoadDialogStyle.DrawSurface(rect);
            string summary = V3CargoLoadText.Tr(
                "CT_Shuttle_Unload_LoadBarSummary",
                ShuttleUIMetricFormatter.FormatKgCompact(currentMassKg),
                ShuttleUIMetricFormatter.FormatKgCompact(capacityKg));
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x + 8f, rect.y + 3f, rect.width - 16f, 21f),
                summary,
                GameFont.Small,
                GameFont.Tiny,
                accent,
                null,
                TextAnchor.MiddleCenter);
            Rect meterRect = new Rect(
                rect.x + 8f,
                rect.yMax - 15f,
                rect.width - 16f,
                10f);
            ShuttleUILayout.DrawLinearMeter(meterRect, value01, accent);

            string tooltip = this.IsActive
                ? V3CargoLoadText.Tr(
                    "CT_Shuttle_Unload_Progress",
                    this.progress.CompletedStackCount,
                    this.progress.TotalStackCount,
                    this.progress.PendingThingCount)
                : V3CargoLoadText.Tr(
                    "CT_Shuttle_Unload_InventorySummary",
                    this.model != null ? this.model.Rows.Count : 0,
                    this.model != null ? this.model.TotalThingCount : 0,
                    ShuttleUIMetricFormatter.FormatKgCompact(currentMassKg));
            TooltipHandler.TipRegion(rect, tooltip);
        }

        private void DrawToolbar(Rect rect)
        {
            this.tutorial.Register(
                ShuttleTutorialTargetIds.UnloadDialogToolbar,
                rect);
            bool compact = rect.width < 820f;
            float clearWidth = compact ? 44f : 54f;
            float sourceWidth = compact ? 112f : 142f;
            float categoryWidth = compact ? 132f : 170f;
            float searchWidth = compact
                ? Mathf.Max(150f, rect.width - 430f)
                : Mathf.Max(200f, rect.width - 550f);
            Rect searchRect = new Rect(rect.x, rect.y, searchWidth, rect.height);
            Rect clearRect = new Rect(searchRect.xMax + 4f, rect.y, clearWidth, rect.height);
            Rect sourceRect = new Rect(clearRect.xMax + 8f, rect.y, sourceWidth, rect.height);
            Rect categoryRect = new Rect(sourceRect.xMax + 8f, rect.y, categoryWidth, rect.height);
            Rect batchRect = new Rect(
                categoryRect.xMax + 8f,
                rect.y,
                Mathf.Max(72f, rect.xMax - categoryRect.xMax - 8f),
                rect.height);

            V3CargoLoadDialogStyle.DrawSurface(searchRect);
            string before = this.filterController.Search;
            this.filterController.Search = Widgets.TextField(searchRect.ContractedBy(4f), before);
            if (!string.Equals(before, this.filterController.Search, StringComparison.Ordinal))
            {
                this.MarkFilterDirty();
            }

            if (string.IsNullOrEmpty(this.filterController.Search))
            {
                Text.Font = GameFont.Tiny;
                GUI.color = ShuttleUIStyle.MutedTextColor;
                ShuttleUILayout.SafeLabel(
                    searchRect.ContractedBy(9f),
                    V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_SearchPlaceholder"));
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
            }

            if (V3CargoLoadDialogStyle.DrawButton(
                clearRect,
                V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_SearchClear"),
                !string.IsNullOrEmpty(this.filterController.Search),
                ShuttleUIStyle.BorderColor,
                V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_SearchClearTooltip")))
            {
                this.filterController.Search = string.Empty;
                this.MarkFilterDirty();
            }

            if (V3CargoLoadDialogStyle.DrawButton(
                sourceRect,
                V3CargoLoadText.Tr("CT_Shuttle_Unload_SourceButton", this.filterController.SourceLabel),
                true,
                V3CargoLoadDialogStyle.AccentColor,
                V3CargoLoadText.Tr("CT_Shuttle_Unload_SourceTooltip")))
            {
                this.filterController.OpenSourceMenu(this.MarkFilterDirty);
            }

            if (V3CargoLoadDialogStyle.DrawButton(
                categoryRect,
                V3CargoLoadText.Tr(
                    "CT_Shuttle_LoadCargo_FilterButton",
                    this.filterController.ActiveCategoryLabel),
                true,
                V3CargoLoadDialogStyle.AccentColor,
                V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_FilterTooltip")))
            {
                this.filterController.OpenCategoryMenu();
                this.MarkFilterDirty();
            }

            if (V3CargoLoadDialogStyle.DrawButton(
                batchRect,
                V3CargoLoadText.Tr("CT_Shuttle_Unload_BatchButton"),
                !this.IsActive && this.model != null && this.model.Rows.Count > 0,
                V3CargoLoadDialogStyle.AccentColor,
                V3CargoLoadText.Tr("CT_Shuttle_Unload_BatchTooltip")))
            {
                this.batchMenu.Open(this.model);
            }
        }

        private void DrawBody(Rect rect)
        {
            float selectedWidth = Mathf.Clamp(rect.width * 0.34f, 320f, 410f);
            Rect availableRect = new Rect(
                rect.x,
                rect.y,
                rect.width - selectedWidth - 10f,
                rect.height);
            Rect selectedRect = new Rect(
                availableRect.xMax + 10f,
                rect.y,
                selectedWidth,
                rect.height);
            this.tutorial.Register(
                ShuttleTutorialTargetIds.UnloadDialogAvailablePanel,
                availableRect);
            this.tutorial.Register(
                ShuttleTutorialTargetIds.UnloadDialogSelectedPanel,
                selectedRect);
            this.panelDrawer.Draw(rect, this.filterController.FilteredIndices);
        }

        private void DrawFooter(Rect rect)
        {
            Rect clear = new Rect(rect.x, rect.y, 150f, rect.height);
            Rect close = new Rect(rect.xMax - 112f, rect.y, 112f, rect.height);
            Rect primary = new Rect(close.x - 158f, rect.y, 150f, rect.height);
            Rect all = new Rect(primary.x - 158f, rect.y, 150f, rect.height);
            this.tutorial.Register(
                ShuttleTutorialTargetIds.UnloadDialogActionButtons,
                new Rect(all.x, rect.y, primary.xMax - all.x, rect.height));

            if (V3CargoLoadDialogStyle.DrawButton(
                clear,
                V3CargoLoadText.Tr("CT_Shuttle_Unload_ClearSelection"),
                !this.IsActive && this.selection.SelectedStackCount(this.model) > 0,
                V3CargoLoadDialogStyle.RedColor,
                V3CargoLoadText.Tr("CT_Shuttle_Unload_ClearSelectionTooltip")))
            {
                this.selection.Clear(this.model);
                this.panelDrawer.MarkSelectedDirty();
            }

            if (V3CargoLoadDialogStyle.DrawButton(
                all,
                V3CargoLoadText.Tr("CT_Shuttle_Unload_All"),
                !this.IsActive && this.model != null && this.model.Rows.Count > 0,
                V3CargoLoadDialogStyle.YellowColor,
                V3CargoLoadText.Tr("CT_Shuttle_Unload_AllTooltip")))
            {
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    V3CargoLoadText.Tr("CT_Shuttle_Unload_AllConfirm"),
                    this.BeginAll));
            }

            string primaryLabel = this.IsActive
                ? V3CargoLoadText.Tr("CT_Shuttle_Unload_CancelTask")
                : V3CargoLoadText.Tr("CT_Shuttle_Unload_BeginSelected");
            bool primaryEnabled = this.IsActive || this.selection.SelectedStackCount(this.model) > 0;
            if (V3CargoLoadDialogStyle.DrawButton(
                primary,
                primaryLabel,
                primaryEnabled,
                this.IsActive ? V3CargoLoadDialogStyle.RedColor : V3CargoLoadDialogStyle.AccentColor,
                this.IsActive
                    ? V3CargoLoadText.Tr("CT_Shuttle_Unload_CancelTaskTooltip")
                    : V3CargoLoadText.Tr("CT_Shuttle_Unload_BeginSelectedTooltip")))
            {
                if (this.IsActive)
                {
                    this.Cancel();
                }
                else
                {
                    this.BeginSelected();
                }
            }

            if (V3CargoLoadDialogStyle.DrawButton(
                close,
                V3CargoLoadText.Tr("CT_Shuttle_UI_Close"),
                true,
                ShuttleUIStyle.BorderColor,
                string.Empty))
            {
                this.Close();
            }
        }

        private void OnBatchSelectionChanged()
        {
            this.panelDrawer.MarkSelectedDirty();
        }

        private void BeginAll()
        {
            this.selection.Clear(this.model);
            for (int i = 0; this.model != null && i < this.model.Rows.Count; i++)
            {
                this.selection.Set(this.model.Rows[i], this.model.Rows[i].AvailableCount);
            }

            this.panelDrawer.MarkSelectedDirty();
            this.BeginSelected();
        }

        private void BeginSelected()
        {
            if (this.actions != null && this.actions.Begin(this.selection.BuildIntents(this.model)))
            {
                this.RefreshProgress(true);
            }
        }

        private void Cancel()
        {
            if (this.actions != null && this.actions.Cancel())
            {
                this.RefreshProgress(true);
                this.RefreshCargoModel();
            }
        }

        private void RefreshCargoModel()
        {
            this.categoryResolver.Clear();
            this.model = this.modelBuilder.Build(this.cargoReadPort, this.categoryResolver);
            this.selection.Clear(this.model);
            this.filterController.SetModel(this.model);
            this.panelDrawer.SetModel(this.model);
        }

        private void RefreshProgress(bool force)
        {
            int tick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (!force && (Event.current == null || Event.current.type != EventType.Repaint || tick < this.nextProgressRefreshTick))
            {
                return;
            }

            this.nextProgressRefreshTick = tick + ProgressRefreshTicks;
            ShuttleCargoUnloadProgressSnapshot next = this.unloadReadPort != null
                ? this.unloadReadPort.BuildCargoUnloadProgressSnapshot()
                : new ShuttleCargoUnloadProgressSnapshot();
            bool wasActive = this.progress != null && this.progress.Active;
            this.progress = next ?? new ShuttleCargoUnloadProgressSnapshot();
            if (this.progress.Active)
            {
                this.observedActiveUnload = true;
            }

            if (this.progress.Revision != this.lastProgressRevision)
            {
                this.lastProgressRevision = this.progress.Revision;
                this.RefreshCurrentCapacity();
            }

            if (wasActive && !this.progress.Active && this.observedActiveUnload)
            {
                this.observedActiveUnload = false;
                this.RefreshCargoModel();
            }
        }

        private void RefreshCurrentCapacity()
        {
            if (this.model == null)
            {
                return;
            }

            float currentMassKg;
            float capacityKg;
            if (this.modelBuilder.TryReadCurrentCapacity(
                this.cargoReadPort,
                out currentMassKg,
                out capacityKg))
            {
                this.model.TotalMassKg = currentMassKg;
                this.model.CapacityKg = capacityKg;
            }
        }

        private void SetCount(V3CargoUnloadRowModel row, int count)
        {
            if (this.IsActive)
            {
                return;
            }

            this.selection.Set(row, count);
            this.panelDrawer.MarkSelectedDirty();
        }

        private void MarkFilterDirty()
        {
            this.filterController.MarkDirty();
            this.panelDrawer.ResetAvailableScroll();
        }

        private bool IsActive
        {
            get { return this.progress != null && this.progress.Active; }
        }
    }
}
