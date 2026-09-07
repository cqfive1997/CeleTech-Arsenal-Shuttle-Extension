using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo
{
    /// <summary>
    /// Read-only projection of pawns held by built-in shuttle module holders.
    /// </summary>
    internal static class ShuttleHeldPassengerQuery
    {
        internal static ShuttleHeldPassengerSnapshot Build(ThingWithComps host)
        {
            ShuttleHeldPassengerSnapshot snapshot = new ShuttleHeldPassengerSnapshot();
            if (host == null)
            {
                return snapshot;
            }

            CompShuttleHabitatOccupancy habitat =
                host.TryGetComp<CompShuttleHabitatOccupancy>();
            snapshot.AddRange(
                habitat != null ? habitat.OccupantsForReading : null);

            CompShuttleMedicalBayOccupancy medicalBay =
                host.TryGetComp<CompShuttleMedicalBayOccupancy>();
            snapshot.AddRange(
                medicalBay != null ? medicalBay.HeldPatients : null);

            CompShuttlePrisonCellOccupancy prisonCell =
                host.TryGetComp<CompShuttlePrisonCellOccupancy>();
            snapshot.AddRange(
                prisonCell != null ? prisonCell.HeldPrisonersForReading : null);

            CompShuttleMechChargerOccupancy mechCharger =
                host.TryGetComp<CompShuttleMechChargerOccupancy>();
            snapshot.AddRange(
                mechCharger != null ? mechCharger.HeldChargingMechs : null);

            return snapshot;
        }
    }

    internal sealed class ShuttleHeldPassengerSnapshot
    {
        private readonly List<Pawn> pawns = new List<Pawn>();
        private readonly HashSet<int> pawnThingIDs = new HashSet<int>();

        internal IReadOnlyList<Pawn> Pawns
        {
            get { return this.pawns; }
        }

        internal bool Contains(Pawn pawn)
        {
            return pawn != null && this.pawnThingIDs.Contains(pawn.thingIDNumber);
        }

        internal bool ContainsThingID(int thingIDNumber)
        {
            return this.pawnThingIDs.Contains(thingIDNumber);
        }

        internal void AddRange(IEnumerable<Pawn> source)
        {
            if (source == null)
            {
                return;
            }

            foreach (Pawn pawn in source)
            {
                if (pawn == null ||
                    pawn.Destroyed ||
                    pawn.Dead ||
                    !this.pawnThingIDs.Add(pawn.thingIDNumber))
                {
                    continue;
                }

                this.pawns.Add(pawn);
            }
        }
    }
}
