using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Presentation
{
    /// <summary>
    /// UI-facing shuttle mech charger snapshot. It exposes containment and charge display only;
    /// charging state remains owned by CompShuttleMechChargerOccupancy.
    /// </summary>
    public sealed class ShuttleMechChargerReadModel
    {
        public bool HasMechCharger;
        public bool MechChargerPoweredKnown;
        public bool MechChargerPowered;
        public int MechChargeSlots;
        public int ChargingMechCount;
        public int FreeChargingSlots;
        public bool HasChargingMechs;
        public IReadOnlyList<ShuttleMechChargingPawnReadModel> ChargingMechs =
            new List<ShuttleMechChargingPawnReadModel>();
    }

    public sealed class ShuttleMechChargingPawnReadModel
    {
        public int PawnThingID;
        public string PawnLabel;
        public Thing DisplayThing;
        public float EnergyPct = -1f;
    }
}
