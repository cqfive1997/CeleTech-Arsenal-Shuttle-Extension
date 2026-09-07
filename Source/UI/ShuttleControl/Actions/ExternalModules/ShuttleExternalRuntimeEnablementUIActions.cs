using System;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Commands.External;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation.External;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.ExternalModules;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.ExternalModules
{
    internal sealed class ShuttleExternalRuntimeEnablementUIActions :
        IShuttleExternalRuntimeEnablementUIActions
    {
        private readonly IShuttleCommandExecutor commandExecutor;

        internal ShuttleExternalRuntimeEnablementUIActions(
            IShuttleCommandExecutor commandExecutor)
        {
            this.commandExecutor = commandExecutor;
        }

        public bool CanSetRuntimeEnabled(
            ExternalModuleUIReadModel model,
            bool enabled)
        {
            return string.IsNullOrEmpty(
                this.GetRuntimeEnablementDisabledReason(model, enabled));
        }

        public bool SetRuntimeEnabled(
            ExternalModuleUIReadModel model,
            bool enabled,
            Action onCommandCompleted)
        {
            string disabledReason =
                this.GetRuntimeEnablementDisabledReason(model, enabled);
            if (!string.IsNullOrEmpty(disabledReason))
            {
                ShuttleUICommandFeedback.ShowReject(disabledReason, false);
                return false;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(
                new SetExternalRuntimeEnabledCommand(
                    model.ModuleInstanceID,
                    model.RuntimeSystemKey,
                    enabled));
            if (result == null || !result.Success)
            {
                ShuttleUICommandFeedback.ShowReject(
                    result != null && !string.IsNullOrEmpty(result.Message)
                        ? result.Message
                        : this.Tr("CT_Shuttle_ExternalRuntime_CommandFailed"),
                    false);
                return false;
            }

            ShuttleUICommandFeedback.ShowSuccess(
                !string.IsNullOrEmpty(result.Message)
                    ? result.Message
                    : (enabled
                        ? this.Tr("CT_Shuttle_ExternalRuntime_EnabledMessage")
                        : this.Tr("CT_Shuttle_ExternalRuntime_DisabledMessage")));

            if (onCommandCompleted != null)
            {
                onCommandCompleted();
            }

            return true;
        }

        public string GetRuntimeEnablementDisabledReason(
            ExternalModuleUIReadModel model,
            bool enabled)
        {
            if (model == null)
            {
                return this.Tr("CT_Shuttle_ExternalRuntime_NoRuntimeSelected");
            }

            if (!model.RuntimeRegistered)
            {
                return this.Tr("CT_Shuttle_ExternalRuntime_RuntimeNotRegistered");
            }

            if (!model.RuntimeStateExists)
            {
                return this.Tr("CT_Shuttle_ExternalRuntime_RuntimeStateMissing");
            }

            if (!model.RuntimeStateEnvelopeValid)
            {
                return this.Tr("CT_Shuttle_ExternalRuntime_RuntimeStateEnvelopeInvalid");
            }

            if (this.commandExecutor == null)
            {
                return this.Tr("CT_Shuttle_ExternalRuntime_CommandExecutorUnavailable");
            }

            if (string.IsNullOrEmpty(model.ModuleInstanceID) ||
                string.IsNullOrEmpty(model.RuntimeSystemKey))
            {
                return this.Tr("CT_Shuttle_ExternalRuntime_RuntimeIdentityIncomplete");
            }

            return null;
        }

        private string Tr(string key)
        {
            return ShuttleUIText.Tr(key);
        }
    }
}
