using System;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Cargo
{
    internal sealed class ShuttleCargoBayUIActions : IShuttleCargoBayUIActions
    {
        private readonly IShuttleCommandExecutor commandExecutor;
        private readonly IShuttleCargoStackTransferUIActions stackTransferActions;
        private readonly IShuttleCargoStackUnloadUIActions stackUnloadActions;
        private readonly IShuttleCargoBayUnloadUIActions bayUnloadActions;
        private readonly Action markDirty;

        internal ShuttleCargoBayUIActions(
            IShuttleCommandExecutor commandExecutor,
            IShuttleCargoStackTransferUIActions stackTransferActions,
            IShuttleCargoStackUnloadUIActions stackUnloadActions,
            IShuttleCargoBayUnloadUIActions bayUnloadActions,
            Action markDirty)
        {
            this.commandExecutor = commandExecutor;
            this.stackTransferActions = stackTransferActions;
            this.stackUnloadActions = stackUnloadActions;
            this.bayUnloadActions = bayUnloadActions;
            this.markDirty = markDirty;
        }

        public void OpenBayConfig(ShuttleCargoBayActionTarget bay)
        {
            if (this.commandExecutor == null)
            {
                return;
            }

            Find.WindowStack.Add(new Dialog_ShuttleCargoBayConfigV3(
                bay,
                this));
        }

        public bool CanApplyBayConfig(
            ShuttleCargoBayActionTarget bay,
            ShuttleCargoBayConfigActionTarget config)
        {
            if (this.commandExecutor == null || config == null)
            {
                return false;
            }

            if (bay != null && bay.IsRefrigerated)
            {
                return this.CanApplyRefrigeratedConfig(bay, config);
            }

            return this.CanApplyNormalBayConfig(bay, config);
        }

        public bool ApplyBayConfig(
            ShuttleCargoBayActionTarget bay,
            ShuttleCargoBayConfigActionTarget config)
        {
            if (!this.CanApplyBayConfig(bay, config))
            {
                return this.ShowReject(this.GetBayConfigApplyTooltip(bay, config));
            }

            if (bay != null && bay.IsRefrigerated)
            {
                return this.ApplyRefrigeratedConfig(bay, config);
            }

            return this.ApplyNormalBayConfig(bay, config);
        }

        public string GetBayConfigApplyTooltip(
            ShuttleCargoBayActionTarget bay,
            ShuttleCargoBayConfigActionTarget config)
        {
            if (this.commandExecutor == null)
            {
                return this.Tr("CT_Shuttle_Command_ExecutorUnavailable");
            }

            if (bay != null && bay.IsRefrigerated)
            {
                return this.CanApplyRefrigeratedConfig(bay, config)
                    ? this.Tr("CT_Shuttle_Cargo_ColdApplyTooltip")
                    : this.Tr("CT_Shuttle_Cargo_ColdSettingsUnavailable");
            }

            return this.CanApplyNormalBayConfig(bay, config)
                ? this.Tr("CT_Shuttle_Cargo_FilterCloseSavesTooltip")
                : this.Tr("CT_Shuttle_Cargo_FilterSettingsUnavailable");
        }

        public void OpenBayContents(
            ShuttleCargoBayActionTarget bay,
            ShuttleCargoPageActionContext pageContext)
        {
            if (this.stackTransferActions == null || this.stackUnloadActions == null)
            {
                return;
            }

            Find.WindowStack.Add(new Dialog_ShuttleCargoBayContentsV3(
                bay,
                pageContext,
                this.stackTransferActions,
                this.stackUnloadActions,
                this.bayUnloadActions));
        }

        private bool CanApplyNormalBayConfig(
            ShuttleCargoBayActionTarget bay,
            ShuttleCargoBayConfigActionTarget config)
        {
            return this.commandExecutor != null &&
                bay != null &&
                config != null &&
                !bay.IsRefrigerated &&
                config.RegionIndex >= 0 &&
                config.ItemFilter != null;
        }

        private bool ApplyNormalBayConfig(
            ShuttleCargoBayActionTarget bay,
            ShuttleCargoBayConfigActionTarget config)
        {
            ShuttleCommandResult result = this.commandExecutor.Execute(
                new UpdateCargoRegionSettingsCommand(
                    config.RegionIndex,
                    GetSubmittedLabel(bay, config),
                    true,
                    true,
                    true,
                    ShuttleCargoThingFilterUtility.CopyFilter(config.ItemFilter)));
            return this.ShowResult(result);
        }

        private bool CanApplyRefrigeratedConfig(
            ShuttleCargoBayActionTarget bay,
            ShuttleCargoBayConfigActionTarget config)
        {
            return this.commandExecutor != null &&
                bay != null &&
                config != null &&
                bay.IsRefrigerated &&
                !string.IsNullOrEmpty(GetModuleInstanceID(bay, config));
        }

        private bool ApplyRefrigeratedConfig(
            ShuttleCargoBayActionTarget bay,
            ShuttleCargoBayConfigActionTarget config)
        {
            string moduleInstanceID = GetModuleInstanceID(bay, config);
            bool ok = true;
            bool wroteAny = false;
            string submittedLabel = GetSubmittedLabel(bay, config);
            string originalLabel = bay != null && bay.Label != null
                ? bay.Label.Trim()
                : string.Empty;
            if (!string.Equals(
                submittedLabel ?? string.Empty,
                originalLabel,
                StringComparison.Ordinal))
            {
                wroteAny = true;
                ok = this.ExecuteCommand(new SetRefrigeratedCargoLabelCommand(
                    moduleInstanceID,
                    submittedLabel)) && ok;
            }

            bool autoTransferChanged = bay != null &&
                config.AutoTransferEnabled != bay.AutoTransferEnabled;
            if (autoTransferChanged && !config.AutoTransferEnabled)
            {
                wroteAny = true;
                ok = this.ExecuteCommand(new SetRefrigeratedCargoAutoTransferEnabledCommand(
                    moduleInstanceID,
                    false)) && ok;
            }

            bool originalCustom = bay != null && bay.HasCustomAutoTransferFilter;
            bool filterChanged = !ShuttleCargoThingFilterUtility.FiltersEquivalent(
                bay != null ? bay.AutoTransferFilter : null,
                config.AutoTransferFilter);
            if (originalCustom && !config.HasCustomAutoTransferFilter)
            {
                wroteAny = true;
                ok = this.ExecuteCommand(new ClearRefrigeratedCargoAutoTransferFilterCommand(
                    moduleInstanceID)) && ok;
            }
            else if (config.HasCustomAutoTransferFilter &&
                (!originalCustom || filterChanged))
            {
                wroteAny = true;
                ok = this.ExecuteCommand(new SetRefrigeratedCargoAutoTransferFilterCommand(
                    moduleInstanceID,
                    ShuttleCargoThingFilterUtility.CopyFilter(config.AutoTransferFilter))) && ok;
            }

            if (autoTransferChanged && config.AutoTransferEnabled)
            {
                wroteAny = true;
                ok = this.ExecuteCommand(new SetRefrigeratedCargoAutoTransferEnabledCommand(
                    moduleInstanceID,
                    true)) && ok;
            }

            return ok || !wroteAny;
        }

        private bool ExecuteCommand(IShuttleCommand command)
        {
            ShuttleCommandResult result = this.commandExecutor.Execute(command);
            return this.ShowResult(result);
        }

        private bool ShowResult(ShuttleCommandResult result)
        {
            bool success = ShuttleUICommandFeedback.ShowResult(result);
            if (success && this.markDirty != null)
            {
                this.markDirty();
            }

            return success;
        }

        private bool ShowReject(string message)
        {
            return ShuttleUICommandFeedback.ShowReject(message, false);
        }

        private static string GetSubmittedLabel(
            ShuttleCargoBayActionTarget bay,
            ShuttleCargoBayConfigActionTarget config)
        {
            string trimmed = config != null && config.Label != null
                ? config.Label.Trim()
                : null;
            if (!string.IsNullOrEmpty(trimmed))
            {
                return trimmed;
            }

            return bay != null && !string.IsNullOrEmpty(bay.Label)
                ? bay.Label
                : null;
        }

        private static string GetModuleInstanceID(
            ShuttleCargoBayActionTarget bay,
            ShuttleCargoBayConfigActionTarget config)
        {
            if (config != null && !string.IsNullOrEmpty(config.ModuleInstanceID))
            {
                return config.ModuleInstanceID;
            }

            return bay != null ? bay.ModuleInstanceID : null;
        }

        private string Tr(string key)
        {
            return ShuttleUIText.Tr(key);
        }
    }
}
