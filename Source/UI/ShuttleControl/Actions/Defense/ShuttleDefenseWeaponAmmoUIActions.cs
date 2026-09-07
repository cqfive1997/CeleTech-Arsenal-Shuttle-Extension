using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Defense;
using RimWorld;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Defense
{
    internal sealed class ShuttleDefenseWeaponAmmoUIActions :
        IShuttleDefenseWeaponAmmoUIActions
    {
        private readonly IShuttleCommandExecutor commandExecutor;

        internal ShuttleDefenseWeaponAmmoUIActions(
            IShuttleCommandExecutor commandExecutor)
        {
            this.commandExecutor = commandExecutor;
        }

        public bool ReloadWeapon(ShuttleDefenseWeaponActionTarget weapon)
        {
            if (this.commandExecutor == null ||
                weapon == null ||
                !weapon.HasAmmoSystem ||
                string.IsNullOrEmpty(weapon.ModuleInstanceID))
            {
                this.ShowReject(this.Tr("CT_Shuttle_WeaponAmmo_ReloadFailed"));
                return false;
            }

            int requestedRounds = weapon.MagazineCapacity > weapon.LoadedAmmoCount
                ? weapon.MagazineCapacity - weapon.LoadedAmmoCount
                : 0;
            if (requestedRounds <= 0)
            {
                ShuttleUICommandFeedback.ShowNeutral(
                    this.Tr("CT_Shuttle_WeaponAmmo_Full"));
                return true;
            }

            if (!weapon.CanReload)
            {
                this.ShowReject(this.Tr("CT_Shuttle_WeaponAmmo_ReloadFailed"));
                return false;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(
                new ReloadShuttleWeaponCommand(weapon.ModuleInstanceID));
            if (result == null || !result.Success)
            {
                return this.ShowResult(result);
            }

            string weaponLabel = !string.IsNullOrEmpty(weapon.Label)
                ? weapon.Label
                : !string.IsNullOrEmpty(weapon.ModuleLabel)
                    ? weapon.ModuleLabel
                    : weapon.ModuleInstanceID;
            string ammoLabel = !string.IsNullOrEmpty(weapon.SelectedAmmoLabel)
                ? weapon.SelectedAmmoLabel
                : this.Tr("CT_Shuttle_WeaponAmmo_GenericAmmo");
            ShuttleUICommandFeedback.ShowNeutral(
                ShuttleUIText.Tr(
                    "CT_Shuttle_Command_WeaponReloadRequestedFormat",
                    weaponLabel,
                    requestedRounds,
                    ammoLabel));
            return true;
        }

        public bool CancelWeaponReload(ShuttleDefenseWeaponActionTarget weapon)
        {
            if (this.commandExecutor == null ||
                weapon == null ||
                !weapon.CanCancelReload ||
                string.IsNullOrEmpty(weapon.ModuleInstanceID))
            {
                this.ShowReject(this.Tr("CT_Shuttle_WeaponAmmo_ReloadFailed"));
                return false;
            }

            return this.ShowResult(this.commandExecutor.Execute(
                new CancelShuttleWeaponReloadCommand(weapon.ModuleInstanceID)));
        }

        public bool SetWeaponAmmo(
            ShuttleDefenseWeaponActionTarget weapon,
            string ammoDefName)
        {
            if (this.commandExecutor == null ||
                weapon == null ||
                !weapon.CanSelectAmmo ||
                string.IsNullOrEmpty(weapon.ModuleInstanceID) ||
                string.IsNullOrEmpty(ammoDefName))
            {
                this.ShowReject(this.Tr("CT_Shuttle_WeaponAmmo_Incompatible"));
                return false;
            }

            return this.ShowResult(this.commandExecutor.Execute(
                new SetShuttleWeaponAmmoCommand(
                    weapon.ModuleInstanceID,
                    ammoDefName)));
        }

        public bool SetWeaponAutoReload(
            ShuttleDefenseWeaponActionTarget weapon,
            bool enabled)
        {
            if (this.commandExecutor == null ||
                weapon == null ||
                !weapon.CanToggleAutoReload ||
                string.IsNullOrEmpty(weapon.ModuleInstanceID))
            {
                this.ShowReject(this.Tr("CT_Shuttle_WeaponAmmo_ReloadFailed"));
                return false;
            }

            return this.ShowResult(this.commandExecutor.Execute(
                new SetShuttleWeaponAutoReloadCommand(
                    weapon.ModuleInstanceID,
                    enabled)));
        }

        public bool SetWeaponManualReloadAllowed(
            ShuttleDefenseWeaponActionTarget weapon,
            bool enabled)
        {
            if (this.commandExecutor == null ||
                weapon == null ||
                !weapon.CanToggleManualReloadAllowed ||
                string.IsNullOrEmpty(weapon.ModuleInstanceID))
            {
                this.ShowReject(this.Tr("CT_Shuttle_WeaponAmmo_ManualReloadUnavailable"));
                return false;
            }

            return this.ShowResult(this.commandExecutor.Execute(
                new SetShuttleWeaponManualReloadAllowedCommand(
                    weapon.ModuleInstanceID,
                    enabled)));
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
