using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Crew;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Crew
{
    internal sealed class ShuttleCrewJoyUIActions : IShuttleCrewJoyUIActions
    {
        private readonly IShuttleCommandExecutor commandExecutor;
        private readonly Action<IShuttleCrewJoyUIActions, ShuttleControlReadModel> openHabitatJoyConfig;

        internal ShuttleCrewJoyUIActions(
            IShuttleCommandExecutor commandExecutor,
            Action<IShuttleCrewJoyUIActions, ShuttleControlReadModel> openHabitatJoyConfig)
        {
            this.commandExecutor = commandExecutor;
            this.openHabitatJoyConfig = openHabitatJoyConfig;
        }

        public bool CanOpenHabitatJoyConfig(ShuttleControlReadModel model)
        {
            return this.commandExecutor != null &&
                model != null &&
                model.Habitat != null &&
                model.Habitat.HasHabitat &&
                model.Habitat.SupportsJoy &&
                model.Habitat.JoyConfigs != null &&
                model.Habitat.JoyConfigs.Count > 0;
        }

        public bool OpenHabitatJoyConfig(ShuttleControlReadModel model)
        {
            if (!this.CanOpenHabitatJoyConfig(model))
            {
                this.ShowReject(this.GetHabitatJoyConfigTooltip(model));
                return false;
            }

            if (this.openHabitatJoyConfig == null)
            {
                this.ShowReject(this.Tr("CT_Shuttle_Command_ExecutorUnavailable"));
                return false;
            }

            this.openHabitatJoyConfig(this, model);
            return true;
        }

        public string GetHabitatJoyConfigTooltip(ShuttleControlReadModel model)
        {
            if (this.commandExecutor == null)
            {
                return this.Tr("CT_Shuttle_Command_ExecutorUnavailable");
            }

            if (model == null || model.Habitat == null || !model.Habitat.HasHabitat)
            {
                return this.Tr("CT_Shuttle_HabitatPanel_NotInstalled");
            }

            if (!model.Habitat.SupportsJoy ||
                model.Habitat.JoyConfigs == null ||
                model.Habitat.JoyConfigs.Count == 0)
            {
                return this.Tr("CT_Shuttle_HabitatJoyConfig_NoModules");
            }

            return this.Tr("CT_Shuttle_HabitatJoy_HeaderTooltip");
        }

        public bool CanSaveHabitatJoyKinds(
            ShuttleHabitatJoyConfigReadModel config,
            IReadOnlyList<string> joyKindDefNames)
        {
            return this.commandExecutor != null &&
                config != null &&
                !string.IsNullOrEmpty(config.ModuleInstanceID) &&
                joyKindDefNames != null &&
                joyKindDefNames.Count > 0 &&
                joyKindDefNames.Count <= config.Capacity;
        }

        public bool SaveHabitatJoyKinds(
            ShuttleHabitatJoyConfigReadModel config,
            IReadOnlyList<string> joyKindDefNames)
        {
            if (!this.CanSaveHabitatJoyKinds(config, joyKindDefNames))
            {
                this.ShowReject(this.GetHabitatJoySaveTooltip(config, joyKindDefNames));
                return false;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(
                new SetHabitatJoyKindsCommand(
                    config.ModuleInstanceID,
                    joyKindDefNames));
            return this.ShowResult(result);
        }

        public string GetHabitatJoySaveTooltip(
            ShuttleHabitatJoyConfigReadModel config,
            IReadOnlyList<string> joyKindDefNames)
        {
            if (this.commandExecutor == null)
            {
                return this.Tr("CT_Shuttle_Command_ExecutorUnavailable");
            }

            if (config == null || string.IsNullOrEmpty(config.ModuleInstanceID))
            {
                return this.Tr("CT_Shuttle_Command_SetHabitatJoyKindsMissing");
            }

            if (joyKindDefNames == null || joyKindDefNames.Count == 0)
            {
                return this.Tr("CT_Shuttle_Command_HabitatJoyKindsEmpty");
            }

            if (joyKindDefNames.Count > config.Capacity)
            {
                return this.Tr("CT_Shuttle_HabitatJoyConfig_CapacityReached");
            }

            return this.Tr("CT_Shuttle_HabitatJoy_SaveTooltip");
        }

        private bool ShowResult(ShuttleCommandResult result)
        {
            return ShuttleUICommandFeedback.ShowResult(result);
        }

        private void ShowReject(string message)
        {
            ShuttleUICommandFeedback.ShowReject(message);
        }

        private string Tr(string key)
        {
            return ShuttleUIText.Tr(key);
        }
    }
}
