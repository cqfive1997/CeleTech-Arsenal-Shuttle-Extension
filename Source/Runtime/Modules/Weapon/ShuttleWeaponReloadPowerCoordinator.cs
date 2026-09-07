using CeleTech.ShuttleExtension.ModularShuttle.Defs;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Coordinates reload-power admission between the demand pass and the later module tick.
    /// It owns no magazine, cargo, fire-control, or retry policy beyond bounded power retries.
    /// </summary>
    internal sealed class ShuttleWeaponReloadPowerCoordinator
    {
        private const int RetryDelayTicks = 60;

        internal void CollectDemand(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponAmmoState ammoState,
            ShuttleWeaponModuleAmmoExtension extension)
        {
            if (context == null ||
                ammoState == null ||
                !ammoState.ReloadInProgress ||
                extension == null ||
                extension.reloadPowerDemandWatts <= 0f ||
                !ammoState.CanAttemptReloadPowerDemand(context.TicksGame))
            {
                return;
            }

            ammoState.MarkReloadPowerDemandAttempt(context.TicksGame);
            context.AddInternalPowerDemandWatts(extension.reloadPowerDemandWatts);
        }

        internal bool CanAdvance(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponAmmoState ammoState,
            ShuttleWeaponModuleAmmoExtension extension)
        {
            if (context == null || ammoState == null || !context.IsEnabled)
            {
                return false;
            }

            if (extension == null || extension.reloadPowerDemandWatts <= 0f)
            {
                return context.InternalBusPowered;
            }

            if (!ammoState.WasReloadPowerDemandAttemptedAt(context.TicksGame))
            {
                return false;
            }

            if (!context.InternalBusPowered)
            {
                ammoState.MarkReloadPowerBlocked(context.TicksGame, RetryDelayTicks);
                return false;
            }

            ammoState.ClearReloadPowerAdmission();
            return true;
        }
    }
}
