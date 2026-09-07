using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    /// <summary>
    /// Narrow service boundary for external Habitat occupancy commands. UI and command handlers
    /// use this instead of reaching into CompShuttleHabitatOccupancy or its holders directly.
    /// </summary>
    internal sealed class ShuttleHabitatOccupancyService
    {
        internal bool TryEjectAllHabitatOccupants(
            ThingWithComps shuttleHost,
            out bool hadOccupants,
            out string failReason)
        {
            hadOccupants = false;
            failReason = null;

            if (shuttleHost == null)
            {
                failReason = "CT_Shuttle_Command_EjectHabitatOccupants_Failed".Translate().ToString();
                return false;
            }

            CompShuttleHabitatOccupancy occupancy = shuttleHost.TryGetComp<CompShuttleHabitatOccupancy>();
            if (occupancy == null || !occupancy.HasAnyOccupants)
            {
                return true;
            }

            if (shuttleHost.Map == null)
            {
                failReason = "CT_Shuttle_Command_EjectHabitatOccupants_Failed".Translate().ToString();
                return false;
            }

            hadOccupants = true;
            if (!occupancy.TryEjectAllToMap(shuttleHost.Map, false))
            {
                failReason = "CT_Shuttle_Command_EjectHabitatOccupants_Failed".Translate().ToString();
                return false;
            }

            return true;
        }

        internal bool TryEjectHabitatOccupant(
            ThingWithComps shuttleHost,
            int pawnThingID,
            out bool hadOccupant,
            out string failReason)
        {
            hadOccupant = false;
            failReason = null;

            if (shuttleHost == null || pawnThingID <= 0)
            {
                failReason = "CT_Shuttle_Command_EjectHabitatOccupant_Failed".Translate().ToString();
                return false;
            }

            CompShuttleHabitatOccupancy occupancy = shuttleHost.TryGetComp<CompShuttleHabitatOccupancy>();
            if (occupancy == null || !occupancy.HasAnyOccupants)
            {
                return true;
            }

            if (shuttleHost.Map == null)
            {
                failReason = "CT_Shuttle_Command_EjectHabitatOccupant_Failed".Translate().ToString();
                return false;
            }

            hadOccupant = true;
            if (!occupancy.TryEjectOccupantByThingID(
                pawnThingID,
                shuttleHost.Map,
                out hadOccupant,
                out failReason))
            {
                if (string.IsNullOrEmpty(failReason))
                {
                    failReason = "CT_Shuttle_Command_EjectHabitatOccupant_Failed".Translate().ToString();
                }

                return false;
            }

            return true;
        }
    }
}
