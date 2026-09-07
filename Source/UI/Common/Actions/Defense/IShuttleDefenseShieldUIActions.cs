namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Defense
{
    internal interface IShuttleDefenseShieldUIActions
    {
        bool CanApplyShieldRange(
            ShuttleDefenseShieldActionTarget shield,
            float selectedRange);

        bool ApplyShieldRange(
            ShuttleDefenseShieldActionTarget shield,
            float selectedRange);

        string GetShieldRangeTooltip(ShuttleDefenseShieldActionTarget shield);

        bool CanApplySurfaceShieldRechargeSpeed(
            ShuttleDefenseShieldActionTarget shield,
            float multiplier);

        bool ApplySurfaceShieldRechargeSpeed(
            ShuttleDefenseShieldActionTarget shield,
            float multiplier);

        string GetSurfaceShieldRechargeSpeedTooltip(
            ShuttleDefenseShieldActionTarget shield);
    }
}
