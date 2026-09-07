using RimWorld;
using RimWorld.Planet;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.World
{
    /// <summary>
    /// Immutable-for-launch metadata passed from the electric launch service to the skyfaller seam.
    /// It is not persisted cargo truth and it does not authorize launch by itself.
    /// </summary>
    public sealed class ShuttleLaunchPayload
    {
        // Host and world targets are captured at launch time for the skyfaller handoff only.
        public ThingWithComps Host;
        public PlanetTile OriginTile;
        public PlanetTile DestinationTile;
        public TransportersArrivalAction ArrivalAction;
        public WorldObjectDef WorldObjectDef;
        public ThingDef LeavingSkyfallerDef;
        public ThingDef IncomingSkyfallerDef;

        // Diagnostics copied from the launch transaction. Energy has already been spent before
        // the payload reaches the skyfaller.
        public float EnergySpentWd;
        public float LoadedCargoMassKg;
        public int ProfileRevision;

        // The vanilla transporter group id is still needed by the current RimWorld handoff.
        public int GroupID;
    }
}
