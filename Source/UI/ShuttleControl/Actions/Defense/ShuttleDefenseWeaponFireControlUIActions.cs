using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Defense;
using RimWorld;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Defense
{
    internal sealed class ShuttleDefenseWeaponFireControlUIActions :
        IShuttleDefenseWeaponFireControlUIActions
    {
        private readonly IShuttleCommandExecutor commandExecutor;

        internal ShuttleDefenseWeaponFireControlUIActions(
            IShuttleCommandExecutor commandExecutor)
        {
            this.commandExecutor = commandExecutor;
        }

        public bool CanToggleHoldFire(ShuttleDefenseWeaponActionTarget weapon)
        {
            return this.commandExecutor != null &&
                weapon != null &&
                weapon.CanToggleHoldFire &&
                !string.IsNullOrEmpty(weapon.ModuleInstanceID);
        }

        public bool ToggleHoldFire(ShuttleDefenseWeaponActionTarget weapon)
        {
            if (!this.CanToggleHoldFire(weapon))
            {
                this.ShowReject(this.GetHoldFireTooltip(weapon));
                return false;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(
                new SetWeaponHoldFireCommand(
                    weapon.ModuleInstanceID,
                    !weapon.HoldFire));
            return this.ShowResult(result);
        }

        public string GetHoldFireTooltip(ShuttleDefenseWeaponActionTarget weapon)
        {
            if (this.commandExecutor == null)
            {
                return this.Tr("CT_Shuttle_Command_ExecutorUnavailable");
            }

            if (weapon == null || string.IsNullOrEmpty(weapon.ModuleInstanceID))
            {
                return this.Tr("CT_Shuttle_Command_ContextUnavailable");
            }

            if (!weapon.CanToggleHoldFire)
            {
                return this.Tr("CT_Shuttle_Defense_WeaponControlDetailsUnavailable");
            }

            return this.Tr("CT_Shuttle_WeaponHoldFire");
        }

        public bool CanToggleWeaponFireControlLink(
            ShuttleDefenseWeaponActionTarget weapon)
        {
            return this.commandExecutor != null &&
                weapon != null &&
                weapon.CanToggleFireControlLink &&
                !string.IsNullOrEmpty(weapon.ModuleInstanceID);
        }

        public bool ToggleWeaponFireControlLink(
            ShuttleDefenseWeaponActionTarget weapon)
        {
            if (!this.CanToggleWeaponFireControlLink(weapon))
            {
                this.ShowReject(this.GetFireControlLinkTooltip(weapon));
                return false;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(
                new SetWeaponFireControlLinkedCommand(
                    weapon.ModuleInstanceID,
                    !weapon.FireControlLinked));
            return this.ShowResult(result);
        }

        public string GetFireControlLinkTooltip(
            ShuttleDefenseWeaponActionTarget weapon)
        {
            if (this.commandExecutor == null)
            {
                return this.Tr("CT_Shuttle_Command_ExecutorUnavailable");
            }

            if (weapon == null || string.IsNullOrEmpty(weapon.ModuleInstanceID))
            {
                return this.Tr("CT_Shuttle_FireControl_Unavailable_Generic");
            }

            if (!weapon.CanToggleFireControlLink)
            {
                return string.IsNullOrEmpty(weapon.FireControlUnavailableReason)
                    ? this.Tr("CT_Shuttle_FireControl_Unavailable_Generic")
                    : weapon.FireControlUnavailableReason;
            }

            if (weapon.FireControlLinked)
            {
                return weapon.HasFireControlRadar
                    ? this.Tr("CT_Shuttle_FireControl_DisableTooltip_WithRadar")
                    : this.Tr("CT_Shuttle_FireControl_EnableTooltip_NoRadar");
            }

            return weapon.HasFireControlRadar
                ? this.Tr("CT_Shuttle_FireControl_EnableTooltip_WithRadar")
                : this.Tr("CT_Shuttle_FireControl_DisableTooltip_NoRadar");
        }

        public bool SetWeaponFireControlMode(
            ShuttleDefenseWeaponActionTarget weapon,
            ShuttleWeaponFireControlMode mode)
        {
            if (this.commandExecutor == null ||
                weapon == null ||
                !weapon.CanSetFireControlMode ||
                string.IsNullOrEmpty(weapon.ModuleInstanceID))
            {
                this.ShowReject(this.GetFireControlSettingsTooltip(weapon));
                return false;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(
                new SetWeaponFireControlModeCommand(weapon.ModuleInstanceID, mode));
            return this.ShowResult(result);
        }

        public bool SetWeaponTargetPriority(
            ShuttleDefenseWeaponActionTarget weapon,
            ShuttleWeaponTargetPriority priority)
        {
            if (this.commandExecutor == null ||
                weapon == null ||
                !weapon.CanSetTargetPriority ||
                string.IsNullOrEmpty(weapon.ModuleInstanceID))
            {
                this.ShowReject(this.GetFireControlSettingsTooltip(weapon));
                return false;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(
                new SetWeaponTargetPriorityCommand(
                    weapon.ModuleInstanceID,
                    priority));
            return this.ShowResult(result);
        }

        public bool SetWeaponAutoFireEnabled(
            ShuttleDefenseWeaponActionTarget weapon,
            bool enabled)
        {
            if (this.commandExecutor == null ||
                weapon == null ||
                !weapon.CanSetAutoFire ||
                string.IsNullOrEmpty(weapon.ModuleInstanceID))
            {
                this.ShowReject(this.GetFireControlSettingsTooltip(weapon));
                return false;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(
                new SetWeaponAutoFireCommand(weapon.ModuleInstanceID, enabled));
            return this.ShowResult(result);
        }

        private string GetFireControlSettingsTooltip(
            ShuttleDefenseWeaponActionTarget weapon)
        {
            if (this.commandExecutor == null)
            {
                return this.Tr("CT_Shuttle_Command_ExecutorUnavailable");
            }

            if (weapon == null || string.IsNullOrEmpty(weapon.ModuleInstanceID))
            {
                return this.Tr("CT_Shuttle_FireControl_Unavailable_Generic");
            }

            if (!weapon.CanSetFireControlMode ||
                !weapon.CanSetTargetPriority ||
                !weapon.CanSetAutoFire)
            {
                return string.IsNullOrEmpty(weapon.FireControlUnavailableReason)
                    ? this.Tr("CT_Shuttle_FireControl_Unavailable_Generic")
                    : weapon.FireControlUnavailableReason;
            }

            return weapon.HasFireControlRadar
                ? this.Tr("CT_Shuttle_FireControl_SettingsTooltip")
                : this.Tr("CT_Shuttle_FireControl_PresetWithoutRadar");
        }

        private bool ShowResult(ShuttleCommandResult result)
        {
            return ShuttleUICommandFeedback.ShowResult(result, MessageTypeDefOf.NeutralEvent);
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
