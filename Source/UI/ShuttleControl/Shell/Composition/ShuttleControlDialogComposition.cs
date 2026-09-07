using System;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Crew;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Crew.Dialogs;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Medical.Dialogs;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Processing.Dialogs;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Settings.Dialogs;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Boot;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Icons;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.ModalLaunchers;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Navigation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Paint;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Research;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Tutorial;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.State;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Composition
{
    internal sealed class ShuttleControlDialogComposition : IDisposable
    {
        private readonly ShuttleControlReadModelCache readModelCache;
        private readonly ShuttlePageRegistry pageRegistry;
        private ShuttleControlPageNavigation pageNavigation;
        private ShuttleControlResearchInvalidationService researchInvalidationService;
        private ShuttleControlTutorialOverlayService tutorialOverlayService;
        private ShuttleControlModalComposition modalComposition;
        private readonly ShuttleUIModalService modalService;
        private readonly ShuttleControlTutorialTargetService tutorialTargetService =
            new ShuttleControlTutorialTargetService();
        private readonly ShuttleControlPaintPreviewService paintPreviewService;
        private readonly ShuttleControlResearchFingerprintService researchFingerprintService;
        private readonly Func<Rot4> getPreviewRotation;

        internal ShuttleControlDialogComposition(
            ShuttleControlState state,
            IShuttleControlReadPort controlReadPort,
            IShuttleCargoReadPort cargoReadPort,
            IShuttleLoadCargoReadPort loadCargoReadPort,
            IShuttleCommandExecutor commandExecutor,
            IShuttleWeaponBayReadPort weaponBayReadPort,
            IShuttleAutoWorkTableReadPort autoWorkTableReadPort,
            IShuttleControlBootAnimationPort bootAnimationPort,
            Func<Rot4> getPreviewRotation,
            Action<bool> setChildModalOpen,
            Action closeWindow)
        {
            this.State = state ?? new ShuttleControlState();
            this.getPreviewRotation = getPreviewRotation;
            this.modalService = new ShuttleUIModalService();
            this.pageRegistry = new ShuttlePageRegistry();
            this.researchFingerprintService = new ShuttleControlResearchFingerprintService();

            IShuttleProfileInvalidationPort profileInvalidationPort =
                commandExecutor as IShuttleProfileInvalidationPort ??
                controlReadPort as IShuttleProfileInvalidationPort;
            this.readModelCache =
                this.CreateReadModelCache(
                    controlReadPort,
                    cargoReadPort,
                    weaponBayReadPort,
                    autoWorkTableReadPort);
            ShuttleUIReadModelHub readModelHub = new ShuttleUIReadModelHub(
                this.readModelCache,
                cargoReadPort,
                controlReadPort as IShuttleExternalModuleUIReadPort);
            this.ReadModelSource = readModelHub;

            IShuttleControlUIReadModelInvalidationPort readModelInvalidationPort =
                readModelHub;
            IShuttlePageReadModelContextBinder pageReadModelContextBinder =
                readModelHub;

            IShuttleCommandExecutor invalidatingCommandExecutor =
                this.CreateInvalidatingCommandExecutor(
                    commandExecutor,
                    readModelInvalidationPort);
            this.BootAnimation = new ShuttleControlBootAnimationService(bootAnimationPort);
            this.paintPreviewService = new ShuttleControlPaintPreviewService();
            this.BindShellServices(
                invalidatingCommandExecutor,
                loadCargoReadPort,
                cargoReadPort,
                controlReadPort as IShuttleAssemblyEventReadPort,
                setChildModalOpen,
                closeWindow);

            this.Host = this.CreateHost(
                pageReadModelContextBinder,
                readModelInvalidationPort,
                controlReadPort as IShuttleExternalModuleUIReadPort,
                controlReadPort as IShuttlePerformanceCaptureControlPort,
                controlReadPort as IShuttlePerformanceDashboardReadPort,
                invalidatingCommandExecutor,
                profileInvalidationPort,
                controlReadPort as IShuttleModuleInstallEligibilityReadPort,
                commandExecutor as IShuttleMedicalBayActionPort,
                closeWindow);
        }

        internal ShuttleControlState State { get; private set; }

        internal IShuttleControlBootAnimation BootAnimation { get; private set; }

        internal IShuttleControlUIReadModelSource ReadModelSource { get; private set; }

        internal ShuttleControlHost Host { get; private set; }

        internal bool ShouldPauseSimulation
        {
            get
            {
                return this.tutorialOverlayService != null &&
                    this.tutorialOverlayService.ShouldPauseSimulation;
            }
        }

        internal bool TrySwitchToPage(ShuttleControlPageId page)
        {
            return this.pageNavigation != null &&
                this.pageNavigation.TrySwitchToPage(page);
        }

        internal void CheckResearchInvalidation()
        {
            if (this.researchInvalidationService != null)
            {
                this.researchInvalidationService.CheckResearchInvalidation();
            }
        }

        internal ShuttleControlPageId EnsureCurrentPageAvailableAndSync(
            ShuttleControlReadModel controlModel)
        {
            if (this.pageNavigation == null)
            {
                return ShuttleControlPageId.Main;
            }

            return this.pageNavigation.EnsureCurrentPageAvailable(controlModel);
        }

        internal void DrawWithTutorial(
            Rect rect,
            ShuttleControlPageId currentPage,
            ShuttleControlReadModel controlModel,
            Action drawAction)
        {
            if (this.tutorialOverlayService == null)
            {
                if (drawAction != null)
                {
                    drawAction();
                }

                return;
            }

            this.tutorialOverlayService.DrawWithTutorial(
                rect,
                currentPage,
                controlModel,
                drawAction);
        }

        public void Dispose()
        {
            if (this.paintPreviewService != null)
            {
                this.paintPreviewService.Dispose();
            }
        }

        private ShuttleControlReadModelCache CreateReadModelCache(
            IShuttleControlReadPort controlReadPort,
            IShuttleCargoReadPort cargoReadPort,
            IShuttleWeaponBayReadPort weaponBayReadPort,
            IShuttleAutoWorkTableReadPort autoWorkTableReadPort)
        {
            return new ShuttleControlReadModelCache(
                controlReadPort,
                cargoReadPort,
                weaponBayReadPort,
                autoWorkTableReadPort);
        }

        private IShuttleCommandExecutor CreateInvalidatingCommandExecutor(
            IShuttleCommandExecutor commandExecutor,
            IShuttleControlUIReadModelInvalidationPort readModelInvalidationPort)
        {
            if (commandExecutor == null)
            {
                return null;
            }

            return new ShuttleControlInvalidatingCommandExecutor(
                commandExecutor,
                readModelInvalidationPort != null
                    ? new Action(readModelInvalidationPort.MarkDirty)
                    : null);
        }

        private void BindShellServices(
            IShuttleCommandExecutor commandExecutor,
            IShuttleLoadCargoReadPort loadCargoReadPort,
            IShuttleCargoReadPort cargoReadPort,
            IShuttleAssemblyEventReadPort assemblyEventReadPort,
            Action<bool> setChildModalOpen,
            Action closeWindow)
        {
            this.researchInvalidationService =
                new ShuttleControlResearchInvalidationService(
                    this.researchFingerprintService.GetFingerprint,
                    this.MarkDirty);
            this.pageNavigation = new ShuttleControlPageNavigation(
                this.State,
                this.pageRegistry,
                this.readModelCache,
                this.modalService,
                this.MarkDirty);
            this.modalComposition = new ShuttleControlModalComposition(
                this.modalService,
                commandExecutor,
                loadCargoReadPort,
                cargoReadPort,
                this.MarkDirty,
                setChildModalOpen,
                closeWindow);
            this.tutorialOverlayService = new ShuttleControlTutorialOverlayService(
                assemblyEventReadPort,
                this.State,
                this.tutorialTargetService);
        }

        private ShuttleControlHost CreateHost(
            IShuttlePageReadModelContextBinder pageReadModelContextBinder,
            IShuttleControlUIReadModelInvalidationPort readModelInvalidationPort,
            IShuttleExternalModuleUIReadPort externalModuleUIReadPort,
            IShuttlePerformanceCaptureControlPort performanceCaptureControlPort,
            IShuttlePerformanceDashboardReadPort performanceDashboardReadPort,
            IShuttleCommandExecutor commandExecutor,
            IShuttleProfileInvalidationPort profileInvalidationPort,
            IShuttleModuleInstallEligibilityReadPort moduleInstallEligibilityReadPort,
            IShuttleMedicalBayActionPort medicalBayActionPort,
            Action closeWindow)
        {
            return new ShuttleControlHost(
                this.State,
                new ShuttleControlIconService(),
                pageReadModelContextBinder,
                readModelInvalidationPort,
                this.pageRegistry,
                new ShuttlePageFrame(),
                this.modalService,
                this.tutorialTargetService,
                new ShuttleProcessingDialogLauncher(),
                new ShuttleCrewDialogLauncher(),
                new ShuttleCrewUnloadDialogLauncher(),
                new ShuttleMedicalDialogLauncher(),
                new ShuttleSettingsDialogLauncher(this.getPreviewRotation),
                externalModuleUIReadPort,
                performanceCaptureControlPort,
                performanceDashboardReadPort,
                commandExecutor,
                profileInvalidationPort,
                moduleInstallEligibilityReadPort,
                medicalBayActionPort,
                this.researchFingerprintService.GetFingerprint,
                closeWindow,
                this.paintPreviewService,
                this.getPreviewRotation);
        }

        private void MarkDirty()
        {
            if (this.readModelCache != null)
            {
                this.readModelCache.MarkDirty();
            }
        }

    }
}
