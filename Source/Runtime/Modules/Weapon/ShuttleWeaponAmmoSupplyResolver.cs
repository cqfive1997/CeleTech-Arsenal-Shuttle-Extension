using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Selects one narrow ammunition supply for an explicit reload request. It owns no supply,
    /// magazine or retry state and does not consume ammunition.
    /// </summary>
    internal sealed class ShuttleWeaponAmmoSupplyResolver
    {
        internal IShuttleWeaponAmmoSupply Resolve(
            ShuttleModuleRuntimeContext context,
            bool logisticsOnly)
        {
            ShuttleWeaponCargoAmmoSupply cargoSupply =
                new ShuttleWeaponCargoAmmoSupply(
                    context != null ? context.CargoResourceBroker : null);
            if (!logisticsOnly)
            {
                return cargoSupply;
            }

            return new ShuttleWeaponLogisticsAmmoSupply(context, cargoSupply);
        }

        internal bool IsLogisticsAvailable(ShuttleModuleRuntimeContext context)
        {
            IShuttleWeaponAmmoSupply supply = this.Resolve(context, true);
            return supply != null && supply.IsAvailable;
        }

        internal IShuttleWeaponAmmoSupply ResolvePawn(Pawn pawn)
        {
            return new ShuttleWeaponPawnCarriedAmmoSupply(pawn);
        }
    }
}
