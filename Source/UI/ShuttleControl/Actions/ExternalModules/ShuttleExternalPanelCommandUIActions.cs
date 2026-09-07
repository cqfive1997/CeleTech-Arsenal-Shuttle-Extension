using System;
using CeleTech.ShuttleExtension.ModularShuttle.API.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.API.UI;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation.External;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.ExternalModules;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.ExternalModules
{
    internal sealed class ShuttleExternalPanelCommandUIActions :
        IShuttleExternalPanelCommandUIActions
    {
        private readonly IShuttleCommandExecutor commandExecutor;

        internal ShuttleExternalPanelCommandUIActions(
            IShuttleCommandExecutor commandExecutor)
        {
            this.commandExecutor = commandExecutor;
        }

        public bool CanExecutePanelCommand(
            ExternalModuleUIReadModel model,
            ShuttleExternalPanelCommandContribution contribution)
        {
            return string.IsNullOrEmpty(
                this.GetPanelCommandDisabledReason(model, contribution));
        }

        public bool ExecutePanelCommand(
            ExternalModuleUIReadModel model,
            ShuttleExternalPanelCommandContribution contribution,
            Action onCommandCompleted)
        {
            string rejectReason =
                this.GetPanelCommandDisabledReason(model, contribution);
            if (!string.IsNullOrEmpty(rejectReason))
            {
                ShuttleUICommandFeedback.ShowReject(rejectReason, false);
                return false;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(
                new ShuttleExternalCommand(
                    contribution.CommandKey,
                    model.RuntimeSystemKey,
                    model.ModuleInstanceID,
                    contribution.Arguments));
            if (result == null || !result.Success)
            {
                ShuttleUICommandFeedback.ShowReject(
                    result != null && !string.IsNullOrEmpty(result.Message)
                        ? result.Message
                        : this.Tr("CT_Shuttle_ExternalRuntime_ExternalCommandFailed"),
                    false);
                return false;
            }

            ShuttleUICommandFeedback.ShowSuccess(
                !string.IsNullOrEmpty(result.Message)
                    ? result.Message
                    : this.Tr("CT_Shuttle_ExternalRuntime_ExternalCommandCompleted"));

            if (onCommandCompleted != null)
            {
                onCommandCompleted();
            }

            return true;
        }

        public string GetPanelCommandDisabledReason(
            ExternalModuleUIReadModel model,
            ShuttleExternalPanelCommandContribution contribution)
        {
            if (this.commandExecutor == null)
            {
                return this.Tr("CT_Shuttle_ExternalRuntime_CommandExecutorUnavailable");
            }

            if (model == null)
            {
                return this.Tr("CT_Shuttle_ExternalRuntime_NoRuntimeSelected");
            }

            if (contribution == null)
            {
                return this.Tr("CT_Shuttle_ExternalRuntime_CommandContributionUnavailable");
            }

            if (!contribution.Enabled)
            {
                return !string.IsNullOrEmpty(contribution.DisabledReason)
                    ? contribution.DisabledReason
                    : this.Tr("CT_Shuttle_ExternalRuntime_CommandUnavailable");
            }

            if (string.IsNullOrEmpty(contribution.CommandKey))
            {
                return this.Tr("CT_Shuttle_ExternalRuntime_CommandKeyMissing");
            }

            if (string.IsNullOrEmpty(model.RuntimeSystemKey))
            {
                return this.Tr("CT_Shuttle_ExternalRuntime_RuntimeKeyMissing");
            }

            if (string.IsNullOrEmpty(model.ModuleInstanceID))
            {
                return this.Tr("CT_Shuttle_ExternalRuntime_ModuleInstanceMissing");
            }

            return null;
        }

        private string Tr(string key)
        {
            return ShuttleUIText.Tr(key);
        }
    }
}
