using System;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation.External;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Settings;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Crew;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Composition;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.State;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell
{
    /// <summary>
    /// Shuttle control shell coordinator. It prepares shared context, handles page transitions,
    /// and leaves actual page drawing to the registered page.
    /// </summary>
    internal sealed class ShuttleControlHost
    {
        private readonly ShuttleControlState state;
        private readonly IShuttleIconService iconService;
        private readonly IShuttlePageReadModelContextBinder pageReadModelContextBinder;
        private readonly IShuttleControlUIReadModelInvalidationPort readModelInvalidationPort;
        private readonly ShuttlePageRegistry pageRegistry;
        private readonly ShuttlePageFrame pageFrame;
        private readonly ShuttlePageDrawContext context = new ShuttlePageDrawContext();
        private readonly IShuttleProfileInvalidationPort profileInvalidationPort;
        private readonly Func<Rot4> getPreviewRotation;
        private bool enteredInitialPage;

        internal ShuttleControlHost(
            ShuttleControlState state,
            IShuttleIconService iconService,
            IShuttlePageReadModelContextBinder pageReadModelContextBinder,
            IShuttleControlUIReadModelInvalidationPort readModelInvalidationPort,
            ShuttlePageRegistry pageRegistry,
            ShuttlePageFrame pageFrame,
            ShuttleUIModalService modalService,
            IShuttleTutorialTargetService tutorialTargets,
            IShuttleProcessingDialogLauncher processingDialogLauncher,
            IShuttleCrewDialogLauncher crewDialogLauncher,
            IShuttleCrewUnloadDialogActions crewUnloadDialogActions,
            IShuttleMedicalDialogLauncher medicalDialogLauncher,
            IShuttleSettingsDialogLauncher settingsDialogLauncher,
            IShuttleExternalModuleUIReadPort externalModuleUIReadPort,
            IShuttlePerformanceCaptureControlPort performanceCaptureControlPort,
            IShuttlePerformanceDashboardReadPort performanceDashboardReadPort,
            IShuttleCommandExecutor commandExecutor,
            IShuttleProfileInvalidationPort profileInvalidationPort,
            IShuttleModuleInstallEligibilityReadPort moduleInstallEligibilityReadPort,
            IShuttleMedicalBayActionPort medicalBayActionPort,
            Func<int> getResearchFingerprint,
            Action closeWindow,
            IShuttlePaintPreviewService paintPreviewProvider,
            Func<Rot4> getPreviewRotation)
        {
            this.state = state ?? new ShuttleControlState();
            this.iconService = iconService;
            this.pageReadModelContextBinder = pageReadModelContextBinder;
            this.readModelInvalidationPort = readModelInvalidationPort;
            this.pageRegistry = pageRegistry ?? new ShuttlePageRegistry();
            this.pageFrame = pageFrame ?? new ShuttlePageFrame();
            this.profileInvalidationPort = profileInvalidationPort;
            this.getPreviewRotation = getPreviewRotation;

            if (modalService != null)
            {
                // Main-page external runtime navigation is page selection, not a modal.
                modalService.OpenExternalRuntimeAction = this.OpenExternalRuntimeFromMainPage;
            }

            // Context is allocated once and refilled each draw to avoid per-frame service bundles.
            new ShuttleControlContextComposition().Bind(
                this.context,
                this.state,
                this.iconService,
                modalService,
                externalModuleUIReadPort,
                performanceCaptureControlPort,
                performanceDashboardReadPort,
                paintPreviewProvider,
                tutorialTargets,
                this.MarkDirty,
                this.MarkExternalModuleCommandCompleted);
            new ShuttleControlActionComposition(
                processingDialogLauncher,
                crewDialogLauncher,
                crewUnloadDialogActions,
                medicalDialogLauncher,
                settingsDialogLauncher).Bind(
                this.context,
                commandExecutor,
                moduleInstallEligibilityReadPort,
                medicalBayActionPort,
                getResearchFingerprint,
                closeWindow,
                modalService,
                this.OnSettingsChangedFromSettingsPage);
        }

        internal void Draw(Rect rect, ShuttleControlReadModel controlModel)
        {
            ShuttleControlPageId currentPage = this.state.CurrentPage;
            this.HandlePageExitBeforeContextRefresh(currentPage);
            this.context.PreviewRotation = this.getPreviewRotation != null
                ? this.getPreviewRotation()
                : Rot4.East;

            if (this.pageReadModelContextBinder != null)
            {
                this.pageReadModelContextBinder.FillContext(this.context, currentPage, controlModel);
            }
            else
            {
                this.context.ReadModels.ControlModel = controlModel;
            }

            this.EnsureCurrentPageEntered(currentPage);
            IShuttleControlPageV3 page = this.pageRegistry.GetPage(currentPage);
            this.pageFrame.Draw(rect, page, this.context);
            this.state.ClearDirty();
        }

        internal void ReleasePageCache()
        {
            this.pageRegistry.ReleasePageCache(this.context);
            this.enteredInitialPage = false;
        }

        private void HandlePageExitBeforeContextRefresh(ShuttleControlPageId currentPage)
        {
            if (currentPage == this.state.LastPage)
            {
                return;
            }

            // At present only the external modules adapter owns a page cache, but
            // the hook is here for later native V3 pages.
            IShuttleControlPageV3 previousPage = this.pageRegistry.GetPage(this.state.LastPage);
            if (this.enteredInitialPage && previousPage != null)
            {
                previousPage.OnExit(this.context);
            }

            this.MarkDirty();
            this.state.LastPage = currentPage;
            this.enteredInitialPage = false;
        }

        private void EnsureCurrentPageEntered(ShuttleControlPageId currentPage)
        {
            if (this.enteredInitialPage)
            {
                return;
            }

            IShuttleControlPageV3 page = this.pageRegistry.GetPage(currentPage);
            if (page != null)
            {
                page.OnEnter(this.context);
            }

            this.enteredInitialPage = true;
        }

        private void MarkDirty()
        {
            if (this.readModelInvalidationPort != null)
            {
                this.readModelInvalidationPort.MarkDirty();
            }

            if (this.state != null)
            {
                this.state.IsDirty = true;
            }
        }

        private void OnSettingsChangedFromSettingsPage(ShuttleSettingsChangeKind changeKind)
        {
            this.MarkDirty();
            if (changeKind == ShuttleSettingsChangeKind.CombatTuning &&
                this.profileInvalidationPort != null)
            {
                this.profileInvalidationPort.MarkCombatTuningSettingsChanged();
            }
            else if (changeKind == ShuttleSettingsChangeKind.Other &&
                this.profileInvalidationPort != null)
            {
                this.profileInvalidationPort.MarkOtherSettingsChanged();
            }
        }

        private void MarkExternalModuleCommandCompleted()
        {
            this.MarkDirty();
        }

        private void OpenExternalRuntimeFromMainPage(
            string moduleInstanceID,
            string runtimeSystemKey)
        {
            if (string.IsNullOrEmpty(moduleInstanceID) || string.IsNullOrEmpty(runtimeSystemKey))
            {
                return;
            }

            if (this.state.ExternalModules != null)
            {
                this.state.ExternalModules.SelectedModuleInstanceID = moduleInstanceID;
                this.state.ExternalModules.SelectedRuntimeSystemKey = runtimeSystemKey;
            }

            this.state.SetCurrentPage(ShuttleControlPageId.ExternalModules);
            this.MarkDirty();
        }
    }
}
