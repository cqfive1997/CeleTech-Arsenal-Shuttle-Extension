using System;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Defense;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Defense
{
    internal sealed class ShuttleDefenseWeaponTargetingUIActions :
        IShuttleDefenseWeaponTargetingUIActions
    {
        private readonly IShuttleCommandExecutor commandExecutor;
        private readonly IShuttleDefenseTargeterService targeterService;
        private readonly Action markDirty;

        internal ShuttleDefenseWeaponTargetingUIActions(
            IShuttleCommandExecutor commandExecutor,
            IShuttleDefenseTargeterService targeterService,
            Action markDirty)
        {
            this.commandExecutor = commandExecutor;
            this.targeterService = targeterService;
            this.markDirty = markDirty;
        }

        public bool CanUseForcedTarget(ShuttleDefenseWeaponActionTarget weapon)
        {
            if (weapon == null)
            {
                return false;
            }

            return weapon.HasForcedTarget
                ? this.CanClearForcedTarget(weapon)
                : this.CanSetForcedTarget(weapon);
        }

        public bool UseForcedTarget(ShuttleDefenseWeaponActionTarget weapon)
        {
            if (weapon == null)
            {
                this.ShowReject(this.GetForcedTargetTooltip(weapon));
                return false;
            }

            return weapon.HasForcedTarget
                ? this.ClearForcedTarget(weapon)
                : this.BeginForcedTargeting(weapon);
        }

        public string GetForcedTargetTooltip(ShuttleDefenseWeaponActionTarget weapon)
        {
            if (weapon != null && weapon.HasForcedTarget)
            {
                return this.GetClearForcedTargetTooltip(weapon);
            }

            return this.GetSetForcedTargetTooltip(weapon);
        }

        private bool CanSetForcedTarget(ShuttleDefenseWeaponActionTarget weapon)
        {
            return this.commandExecutor != null &&
                this.targeterService != null &&
                weapon != null &&
                weapon.CanSetForcedTarget &&
                !string.IsNullOrEmpty(weapon.ModuleInstanceID);
        }

        private bool BeginForcedTargeting(ShuttleDefenseWeaponActionTarget weapon)
        {
            if (!this.CanSetForcedTarget(weapon))
            {
                this.ShowReject(this.GetSetForcedTargetTooltip(weapon));
                return false;
            }

            return this.targeterService.BeginForcedTargeting(
                weapon,
                delegate(LocalTargetInfo target)
                {
                    this.SetWeaponForcedTarget(weapon.ModuleInstanceID, target);
                });
        }

        private string GetSetForcedTargetTooltip(ShuttleDefenseWeaponActionTarget weapon)
        {
            if (this.commandExecutor == null)
            {
                return this.Tr("CT_Shuttle_Command_ExecutorUnavailable");
            }

            if (this.targeterService == null ||
                weapon == null ||
                string.IsNullOrEmpty(weapon.ModuleInstanceID))
            {
                return this.Tr("CT_Shuttle_Command_ContextUnavailable");
            }

            if (!weapon.CanSetForcedTarget)
            {
                return this.Tr("CT_Shuttle_Command_WeaponForcedTargetUnsupported");
            }

            string tooltip = this.Tr("CT_Shuttle_Defense_Target");
            if (!string.IsNullOrEmpty(weapon.LastForcedTargetFailureReason))
            {
                tooltip += "\n" +
                    this.Tr("CT_Shuttle_Defense_LastForcedTargetFailure") + ": " +
                    weapon.LastForcedTargetFailureReason;
            }

            return tooltip;
        }

        private bool CanClearForcedTarget(ShuttleDefenseWeaponActionTarget weapon)
        {
            return this.commandExecutor != null &&
                weapon != null &&
                weapon.CanClearForcedTarget &&
                weapon.HasForcedTarget &&
                !string.IsNullOrEmpty(weapon.ModuleInstanceID);
        }

        private bool ClearForcedTarget(ShuttleDefenseWeaponActionTarget weapon)
        {
            if (!this.CanClearForcedTarget(weapon))
            {
                this.ShowReject(this.GetClearForcedTargetTooltip(weapon));
                return false;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(
                new ClearWeaponForcedTargetCommand(weapon.ModuleInstanceID));
            return this.ShowResult(result);
        }

        private string GetClearForcedTargetTooltip(ShuttleDefenseWeaponActionTarget weapon)
        {
            if (this.commandExecutor == null)
            {
                return this.Tr("CT_Shuttle_Command_ExecutorUnavailable");
            }

            if (weapon == null || string.IsNullOrEmpty(weapon.ModuleInstanceID))
            {
                return this.Tr("CT_Shuttle_Command_ContextUnavailable");
            }

            if (!weapon.HasForcedTarget)
            {
                return this.Tr("CT_Shuttle_WeaponForcedTargetNone");
            }

            if (!weapon.CanClearForcedTarget)
            {
                return this.Tr("CT_Shuttle_Command_WeaponForcedTargetUnsupported");
            }

            return this.Tr("CT_Shuttle_Defense_ClearTarget");
        }

        private void SetWeaponForcedTarget(
            string moduleInstanceID,
            LocalTargetInfo target)
        {
            if (this.commandExecutor == null || string.IsNullOrEmpty(moduleInstanceID))
            {
                return;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(
                new SetWeaponForcedTargetCommand(moduleInstanceID, target));
            this.ShowResult(result);
        }

        private bool ShowResult(ShuttleCommandResult result)
        {
            bool success =
                ShuttleUICommandFeedback.ShowResult(result, MessageTypeDefOf.NeutralEvent);
            if (success && this.markDirty != null)
            {
                this.markDirty();
            }

            return success;
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
