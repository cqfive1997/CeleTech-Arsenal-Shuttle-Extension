using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Defense;
using RimWorld;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Defense
{
    internal sealed class ShuttleDefenseShieldUIActions : IShuttleDefenseShieldUIActions
    {
        private readonly IShuttleCommandExecutor commandExecutor;

        internal ShuttleDefenseShieldUIActions(
            IShuttleCommandExecutor commandExecutor)
        {
            this.commandExecutor = commandExecutor;
        }

        public bool CanApplyShieldRange(
            ShuttleDefenseShieldActionTarget shield,
            float selectedRange)
        {
            return this.commandExecutor != null &&
                shield != null &&
                shield.HasShield &&
                shield.CanSetRange &&
                !string.IsNullOrEmpty(shield.ModuleInstanceID) &&
                shield.MaxRange >= shield.MinRange &&
                selectedRange >= shield.MinRange &&
                selectedRange <= shield.MaxRange;
        }

        public bool ApplyShieldRange(
            ShuttleDefenseShieldActionTarget shield,
            float selectedRange)
        {
            if (!this.CanApplyShieldRange(shield, selectedRange))
            {
                this.ShowReject(this.GetShieldRangeTooltip(shield));
                return false;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(
                new SetShuttleShieldRadiusCommand(
                    shield.ModuleInstanceID,
                    selectedRange));
            return this.ShowResult(result);
        }

        public string GetShieldRangeTooltip(ShuttleDefenseShieldActionTarget shield)
        {
            if (this.commandExecutor == null)
            {
                return this.Tr("CT_Shuttle_Command_ExecutorUnavailable");
            }

            if (shield == null || !shield.HasShield)
            {
                return this.Tr("CT_Shuttle_Defense_NoShieldModule");
            }

            if (!shield.CanSetRange)
            {
                return this.Tr("CT_Shuttle_Defense_ShieldRangeUnsupportedTooltip");
            }

            if (string.IsNullOrEmpty(shield.ModuleInstanceID))
            {
                return this.Tr("CT_Shuttle_Command_ContextUnavailable");
            }

            return this.Tr("CT_Shuttle_Defense_ShieldRangeAdjustTooltip");
        }

        public bool CanApplySurfaceShieldRechargeSpeed(
            ShuttleDefenseShieldActionTarget shield,
            float multiplier)
        {
            return this.commandExecutor != null &&
                shield != null &&
                shield.HasShield &&
                shield.IsSurfaceShield &&
                shield.SupportsRechargeSpeedControl &&
                multiplier >= shield.MinRechargeSpeedMultiplier &&
                multiplier <= shield.MaxRechargeSpeedMultiplier;
        }

        public bool ApplySurfaceShieldRechargeSpeed(
            ShuttleDefenseShieldActionTarget shield,
            float multiplier)
        {
            if (!this.CanApplySurfaceShieldRechargeSpeed(shield, multiplier))
            {
                this.ShowReject(this.GetSurfaceShieldRechargeSpeedTooltip(shield));
                return false;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(
                new SetShuttleSurfaceShieldRechargeSpeedCommand(multiplier));
            return this.ShowResult(result);
        }

        public string GetSurfaceShieldRechargeSpeedTooltip(
            ShuttleDefenseShieldActionTarget shield)
        {
            if (this.commandExecutor == null)
            {
                return this.Tr("CT_Shuttle_Command_ExecutorUnavailable");
            }

            if (shield == null || !shield.HasShield || !shield.IsSurfaceShield)
            {
                return this.Tr("CT_Shuttle_SurfaceShield_RechargeSpeedTooltip");
            }

            return string.IsNullOrEmpty(shield.RechargeSpeedTooltip)
                ? this.Tr("CT_Shuttle_SurfaceShield_RechargeSpeedTooltip")
                : shield.RechargeSpeedTooltip;
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
