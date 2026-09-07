using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Main;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Main;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Main
{
    internal sealed class ShuttleMainModuleInstallUIActions :
        IShuttleMainModuleInstallUIActions
    {
        private readonly IShuttleCommandExecutor commandExecutor;
        private readonly Func<ShuttleAssemblyConstructionReadModel> getConstructionModel;
        private readonly Func<int> getResearchFingerprint;
        private readonly V3MainModuleInstallCandidateBuilder candidateBuilder;
        private readonly ShuttleMainModuleInstallSelectionHandler selectionHandler;

        internal ShuttleMainModuleInstallUIActions(
            IShuttleCommandExecutor commandExecutor,
            IShuttleModuleInstallEligibilityReadPort moduleInstallEligibilityReadPort,
            Func<ShuttleAssemblyConstructionReadModel> getConstructionModel,
            Func<int> getResearchFingerprint)
        {
            this.commandExecutor = commandExecutor;
            this.getConstructionModel = getConstructionModel;
            this.getResearchFingerprint = getResearchFingerprint;
            this.candidateBuilder =
                new V3MainModuleInstallCandidateBuilder(moduleInstallEligibilityReadPort);
            this.selectionHandler =
                new ShuttleMainModuleInstallSelectionHandler(
                    commandExecutor,
                    moduleInstallEligibilityReadPort);
        }

        public bool CanInstallModule(ShuttleControlModuleSlotModel moduleSlot)
        {
            return ShuttleMainInstallPolicy.CanRequestModuleInstall(
                this.commandExecutor != null,
                this.HasActiveConstructionOrder(),
                moduleSlot);
        }

        public void OpenInstallModule(
            ShuttleControlSegmentSlotModel segment,
            ShuttleControlModuleSlotModel moduleSlot)
        {
            if (!this.CanInstallModule(moduleSlot))
            {
                this.ShowReject(this.GetModuleInstallTooltip(segment, moduleSlot));
                return;
            }

            Find.WindowStack.Add(new Dialog_ShuttleMainModuleInstallV3(
                delegate
                {
                    return this.BuildCandidates(segment, moduleSlot);
                },
                this.GetInstallCandidateFingerprint,
                delegate(V3MainInstallCandidateModel candidate)
                {
                    this.selectionHandler.InstallSelectedCandidate(
                        segment,
                        moduleSlot,
                        candidate,
                        false);
                }));
        }

        public string GetModuleInstallTooltip(
            ShuttleControlSegmentSlotModel segment,
            ShuttleControlModuleSlotModel moduleSlot)
        {
            bool canInstall = this.CanInstallModule(moduleSlot);
            return ShuttleMainInstallText.GetModuleInstallTooltip(
                this.commandExecutor != null,
                this.HasActiveConstructionOrder(),
                segment,
                moduleSlot,
                canInstall);
        }

        private List<V3MainInstallCandidateModel> BuildCandidates(
            ShuttleControlSegmentSlotModel segment,
            ShuttleControlModuleSlotModel moduleSlot)
        {
            return this.candidateBuilder.BuildCandidates(
                this.commandExecutor != null,
                this.HasActiveConstructionOrder(),
                segment,
                moduleSlot,
                false);
        }

        private bool HasActiveConstructionOrder()
        {
            ShuttleAssemblyConstructionReadModel model =
                this.getConstructionModel != null ? this.getConstructionModel() : null;
            return model != null && model.HasActiveOrder;
        }

        private int GetInstallCandidateFingerprint()
        {
            unchecked
            {
                int fingerprint = this.getResearchFingerprint != null
                    ? this.getResearchFingerprint()
                    : 0;
                ShuttleAssemblyConstructionReadModel model =
                    this.getConstructionModel != null ? this.getConstructionModel() : null;
                fingerprint = (fingerprint * 31) +
                    (model != null && model.HasActiveOrder ? 1 : 0);
                if (model != null && !string.IsNullOrEmpty(model.OrderID))
                {
                    fingerprint = (fingerprint * 31) + model.OrderID.GetHashCode();
                }

                return fingerprint;
            }
        }

        private void ShowReject(string message)
        {
            ShuttleUICommandFeedback.ShowReject(message, false);
        }
    }
}
