using System;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation.External;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.ExternalModules;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.ExternalModules;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.ExternalModules
{
    internal sealed class ShuttleExternalModuleDetailsUIActions :
        IShuttleExternalModuleDetailsUIActions
    {
        private readonly IV3ExternalModulePanelModelProvider providerModelProvider;
        private readonly IShuttleExternalPanelCommandUIActions panelCommandActions;

        internal ShuttleExternalModuleDetailsUIActions(
            IV3ExternalModulePanelModelProvider providerModelProvider,
            IShuttleExternalPanelCommandUIActions panelCommandActions)
        {
            this.providerModelProvider = providerModelProvider;
            this.panelCommandActions = panelCommandActions;
        }

        public bool CanOpenDetails(ExternalModuleUIReadModel model)
        {
            return string.IsNullOrEmpty(this.GetOpenDetailsDisabledReason(model));
        }

        public void OpenDetailsDialog(
            ExternalModuleUIReadModel model,
            Action onCommandCompleted)
        {
            string disabledReason = this.GetOpenDetailsDisabledReason(model);
            if (!string.IsNullOrEmpty(disabledReason))
            {
                ShuttleUICommandFeedback.ShowReject(disabledReason, false);
                return;
            }

            Find.WindowStack.Add(new Dialog_ShuttleExternalModuleDetailsV3(
                model,
                this.providerModelProvider,
                this.panelCommandActions,
                onCommandCompleted));
        }

        public string GetOpenDetailsDisabledReason(ExternalModuleUIReadModel model)
        {
            if (model == null)
            {
                return ShuttleUIText.Tr("CT_Shuttle_ExternalRuntime_NoRuntimeSelected");
            }

            if (!model.HasExternalPanel)
            {
                return ShuttleUIText.Tr("CT_Shuttle_ExternalRuntime_NoPanelProvider");
            }

            if (this.providerModelProvider == null)
            {
                return ShuttleUIText.Tr("CT_Shuttle_ExternalRuntime_PanelStateUnavailable");
            }

            return null;
        }
    }
}
