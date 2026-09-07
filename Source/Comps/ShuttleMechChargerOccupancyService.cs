using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    /// <summary>
    /// Narrow command/service boundary for mech charging bay occupancy operations.
    /// UI and commands use this instead of touching the charging holder directly.
    /// </summary>
    internal sealed class ShuttleMechChargerOccupancyService
    {
        internal bool TryEjectChargingMech(
            ThingWithComps shuttleHost,
            int mechThingID,
            out bool hadMech,
            out string failReason)
        {
            hadMech = false;
            failReason = null;

            if (shuttleHost == null || mechThingID <= 0)
            {
                failReason = "CT_Shuttle_MechCharger_EjectFailed".Translate().ToString();
                return false;
            }

            CompShuttleMechChargerOccupancy occupancy =
                shuttleHost.TryGetComp<CompShuttleMechChargerOccupancy>();
            if (occupancy == null || !occupancy.HasChargingMechs)
            {
                return true;
            }

            Pawn mech = this.FindChargingMechByThingID(occupancy, mechThingID);
            if (mech == null)
            {
                return true;
            }

            hadMech = true;
            return occupancy.TryEjectChargingMech(mech, out failReason);
        }

        private Pawn FindChargingMechByThingID(
            CompShuttleMechChargerOccupancy occupancy,
            int mechThingID)
        {
            if (occupancy == null || mechThingID <= 0)
            {
                return null;
            }

            System.Collections.Generic.List<Pawn> mechs = occupancy.HeldChargingMechs;
            for (int i = 0; i < mechs.Count; i++)
            {
                Pawn mech = mechs[i];
                if (mech != null && mech.thingIDNumber == mechThingID)
                {
                    return mech;
                }
            }

            return null;
        }
    }
}
