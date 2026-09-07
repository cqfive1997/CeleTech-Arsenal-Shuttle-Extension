using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    public sealed partial class CompShuttlePrisonCellOccupancy
    {
        internal static class PrisonCellOccupancyReconciler
        {
            internal static void ReconcilePrisonerRecords(CompShuttlePrisonCellOccupancy owner)
            {
                owner.EnsureInitialized();
                RecoverUnexpectedNonPawnThings(owner);

                HashSet<int> seenThingIDs = new HashSet<int>();
                for (int i = 0; i < owner.prisonerRecords.Count; i++)
                {
                    ShuttlePrisonerRecord record = owner.prisonerRecords[i];
                    if (record == null)
                    {
                        owner.prisonerRecords.RemoveAt(i);
                        i--;
                        continue;
                    }

                    record.Sanitize();
                    Pawn heldPawn = owner.FindHeldPrisonerByThingIDNoReconcile(record.PawnThingIDNumber);
                    if (heldPawn == null && record.Prisoner != null && owner.IsHeldPawn(record.Prisoner))
                    {
                        heldPawn = record.Prisoner;
                    }

                    if (heldPawn == null)
                    {
                        owner.prisonerRecords.RemoveAt(i);
                        i--;
                        continue;
                    }

                    int thingID = heldPawn.thingIDNumber;
                    if (thingID <= 0 || seenThingIDs.Contains(thingID))
                    {
                        owner.prisonerRecords.RemoveAt(i);
                        i--;
                        continue;
                    }

                    record.RefreshFromPawn(heldPawn);
                    seenThingIDs.Add(thingID);
                }

                for (int i = 0; i < owner.prisonCellHeldThings.Count; i++)
                {
                    Pawn pawn = owner.prisonCellHeldThings[i] as Pawn;
                    if (pawn == null || pawn.Destroyed)
                    {
                        continue;
                    }

                    int thingID = pawn.thingIDNumber;
                    if (thingID <= 0 || seenThingIDs.Contains(thingID))
                    {
                        continue;
                    }

                    ShuttlePrisonerAdmissionContext context =
                        ShuttlePrisonerAdmissionContext.FromPawn(
                            pawn,
                            pawn.guest != null ? pawn.guest.HostFaction : null);
                    owner.prisonerRecords.Add(ShuttlePrisonerRecord.FromPawn(
                        pawn,
                        context.HostFaction,
                        context.InteractionMode,
                        context.WasPrisonerOnAdmission,
                        context.AdmissionTick));
                    seenThingIDs.Add(thingID);
                }
            }

            private static void RecoverUnexpectedNonPawnThings(CompShuttlePrisonCellOccupancy owner)
            {
                if (owner.prisonCellHeldThings == null || owner.prisonCellHeldThings.Count == 0)
                {
                    return;
                }

                Map map = owner.parent != null ? owner.parent.Map : null;
                for (int i = owner.prisonCellHeldThings.Count - 1; i >= 0; i--)
                {
                    Thing thing = owner.prisonCellHeldThings[i];
                    if (thing == null || thing.Destroyed || thing is Pawn)
                    {
                        continue;
                    }

                    if (map == null)
                    {
                        Log.Warning("[CeleTech Shuttle] Prison Cell holder contains non-pawn thing but no map is available for recovery: " + thing);
                        continue;
                    }

                    Thing resultingThing;
                    if (!owner.prisonCellHeldThings.TryDrop(
                        thing,
                        owner.GetEjectCell(map),
                        map,
                        ThingPlaceMode.Near,
                        out resultingThing,
                        null,
                        null))
                    {
                        Log.Warning("[CeleTech Shuttle] Prison Cell could not recover non-pawn thing from holder: " + thing);
                    }
                }
            }
        }
    }
}
