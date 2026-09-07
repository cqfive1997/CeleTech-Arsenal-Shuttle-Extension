using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Defense;
using RimWorld;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Defense
{
    internal sealed class ShuttleDefenseWeaponLogisticsUIActions :
        IShuttleDefenseWeaponLogisticsUIActions
    {
        private readonly IShuttleCommandExecutor commandExecutor;

        internal ShuttleDefenseWeaponLogisticsUIActions(
            IShuttleCommandExecutor commandExecutor)
        {
            this.commandExecutor = commandExecutor;
        }

        public bool SetWeaponLogisticsAutoFeed(
            ShuttleDefenseWeaponActionTarget weapon,
            bool enabled)
        {
            if (this.commandExecutor == null ||
                weapon == null ||
                !weapon.CanToggleLogisticsAutoFeed ||
                string.IsNullOrEmpty(weapon.ModuleInstanceID))
            {
                this.ShowReject(this.Tr("CT_Shuttle_WeaponAmmo_LogisticsUnavailable"));
                return false;
            }

            return this.ShowResult(this.commandExecutor.Execute(
                new SetShuttleWeaponLogisticsAutoFeedCommand(
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
