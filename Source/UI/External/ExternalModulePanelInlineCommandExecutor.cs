using System;
using CeleTech.ShuttleExtension.ModularShuttle.API.UI;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation.External;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.ExternalModules;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.External
{
    internal sealed class ExternalModulePanelInlineCommandExecutor :
        IShuttleExternalPanelCommandExecutor
    {
        private readonly ExternalModuleUIReadModel model;
        private readonly IShuttleExternalPanelCommandUIActions actions;
        private readonly Action onCommandCompleted;

        internal ExternalModulePanelInlineCommandExecutor(
            ExternalModuleUIReadModel model,
            IShuttleExternalPanelCommandUIActions actions,
            Action onCommandCompleted)
        {
            this.model = model;
            this.actions = actions;
            this.onCommandCompleted = onCommandCompleted;
        }

        public bool CanExecute(
            ShuttleExternalPanelCommandContribution contribution,
            out string disabledReason)
        {
            disabledReason = this.actions != null
                ? this.actions.GetPanelCommandDisabledReason(this.model, contribution)
                : ShuttleUIText.Tr("CT_Shuttle_ExternalRuntime_CommandExecutorUnavailable");
            return string.IsNullOrEmpty(disabledReason);
        }

        public bool Execute(ShuttleExternalPanelCommandContribution contribution)
        {
            return this.actions != null &&
                this.actions.ExecutePanelCommand(
                    this.model,
                    contribution,
                    this.onCommandCompleted);
        }
    }
}
