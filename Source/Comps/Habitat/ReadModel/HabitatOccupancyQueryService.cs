using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal static class HabitatOccupancyQueryService
    {
        internal static bool IsCompletelyIdle(
            ThingOwner<Thing> habitatHeldThings,
            List<ShuttleHabitatOccupantRecord> sleepingOccupants,
            List<ShuttleHabitatDiningOccupantRecord> diningOccupants,
            List<ShuttleHabitatJoyOccupantRecord> joyOccupants)
        {
            return (habitatHeldThings == null || habitatHeldThings.Count == 0) &&
                (sleepingOccupants == null || sleepingOccupants.Count == 0) &&
                (diningOccupants == null || diningOccupants.Count == 0) &&
                (joyOccupants == null || joyOccupants.Count == 0);
        }

        internal static bool IsHeldThing(ThingOwner<Thing> habitatHeldThings, Thing thing)
        {
            return thing != null &&
                habitatHeldThings != null &&
                habitatHeldThings.Contains(thing);
        }

        internal static bool IsHeldPawn(ThingOwner<Thing> habitatHeldThings, Pawn pawn)
        {
            return pawn != null && IsHeldThing(habitatHeldThings, pawn);
        }

        internal static Pawn FindHeldPawnByThingID(
            ThingOwner<Thing> habitatHeldThings,
            int pawnThingID)
        {
            if (pawnThingID <= 0 || habitatHeldThings == null)
            {
                return null;
            }

            for (int i = 0; i < habitatHeldThings.Count; i++)
            {
                Pawn pawn = habitatHeldThings[i] as Pawn;
                if (pawn != null && pawn.thingIDNumber == pawnThingID)
                {
                    return pawn;
                }
            }

            return null;
        }

        internal static bool IsContainedPawn(
            ThingOwner<Thing> habitatHeldThings,
            List<ShuttleHabitatOccupantRecord> sleepingOccupants,
            Pawn pawn)
        {
            return pawn != null &&
                IsHeldThing(habitatHeldThings, pawn) &&
                HasRecordFor(sleepingOccupants, pawn);
        }

        internal static bool IsContainedDiningPawn(
            ThingOwner<Thing> habitatHeldThings,
            List<ShuttleHabitatDiningOccupantRecord> diningOccupants,
            Pawn pawn)
        {
            return pawn != null &&
                IsHeldThing(habitatHeldThings, pawn) &&
                HasDiningRecordFor(diningOccupants, pawn);
        }

        internal static bool IsContainedJoyPawn(
            ThingOwner<Thing> habitatHeldThings,
            List<ShuttleHabitatJoyOccupantRecord> joyOccupants,
            Pawn pawn)
        {
            return pawn != null &&
                IsHeldThing(habitatHeldThings, pawn) &&
                HasJoyRecordFor(joyOccupants, pawn);
        }

        internal static bool IsContainedDiningFood(
            ThingOwner<Thing> habitatHeldThings,
            Thing food)
        {
            return food != null &&
                !food.Destroyed &&
                IsHeldThing(habitatHeldThings, food);
        }

        internal static bool IsValidSleepingRecord(
            ThingOwner<Thing> habitatHeldThings,
            List<ShuttleHabitatOccupantRecord> sleepingOccupants,
            ShuttleHabitatOccupantRecord record)
        {
            return record != null &&
                record.IsSleeping &&
                IsContainedPawn(habitatHeldThings, sleepingOccupants, record.Pawn);
        }

        internal static bool HasRecordFor(
            List<ShuttleHabitatOccupantRecord> sleepingOccupants,
            Pawn pawn)
        {
            for (int i = 0; i < sleepingOccupants.Count; i++)
            {
                ShuttleHabitatOccupantRecord record = sleepingOccupants[i];
                if (record != null && record.Pawn == pawn)
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool HasDiningRecordFor(
            List<ShuttleHabitatDiningOccupantRecord> diningOccupants,
            Pawn pawn)
        {
            for (int i = 0; i < diningOccupants.Count; i++)
            {
                ShuttleHabitatDiningOccupantRecord record = diningOccupants[i];
                if (record != null && record.Pawn == pawn)
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool HasJoyRecordFor(
            List<ShuttleHabitatJoyOccupantRecord> joyOccupants,
            Pawn pawn)
        {
            for (int i = 0; i < joyOccupants.Count; i++)
            {
                ShuttleHabitatJoyOccupantRecord record = joyOccupants[i];
                if (record != null && record.Pawn == pawn)
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool HasDiningRecordForFood(
            List<ShuttleHabitatDiningOccupantRecord> diningOccupants,
            Thing food)
        {
            for (int i = 0; i < diningOccupants.Count; i++)
            {
                ShuttleHabitatDiningOccupantRecord record = diningOccupants[i];
                if (record != null && record.Food == food)
                {
                    return true;
                }
            }

            return false;
        }

        internal static Thing FindUnassignedDiningFood(
            ThingOwner<Thing> habitatHeldThings,
            List<ShuttleHabitatDiningOccupantRecord> diningOccupants)
        {
            for (int i = 0; i < habitatHeldThings.Count; i++)
            {
                Thing food = habitatHeldThings[i];
                if (food != null &&
                    !(food is Pawn) &&
                    !food.Destroyed &&
                    !HasDiningRecordForFood(diningOccupants, food))
                {
                    return food;
                }
            }

            return null;
        }

        internal static int CountValidSleepingRecords(
            ThingOwner<Thing> habitatHeldThings,
            List<ShuttleHabitatOccupantRecord> sleepingOccupants)
        {
            int count = 0;
            for (int i = 0; i < sleepingOccupants.Count; i++)
            {
                ShuttleHabitatOccupantRecord record = sleepingOccupants[i];
                if (record != null && record.IsSleeping && IsHeldPawn(habitatHeldThings, record.Pawn))
                {
                    count++;
                }
            }

            return count;
        }

        internal static int CountValidDiningRecords(
            ThingOwner<Thing> habitatHeldThings,
            List<ShuttleHabitatDiningOccupantRecord> diningOccupants)
        {
            int count = 0;
            for (int i = 0; i < diningOccupants.Count; i++)
            {
                ShuttleHabitatDiningOccupantRecord record = diningOccupants[i];
                if (record != null && record.IsDining && IsHeldPawn(habitatHeldThings, record.Pawn))
                {
                    count++;
                }
            }

            return count;
        }

        internal static int CountValidJoyRecords(
            ThingOwner<Thing> habitatHeldThings,
            List<ShuttleHabitatJoyOccupantRecord> joyOccupants)
        {
            int count = 0;
            for (int i = 0; i < joyOccupants.Count; i++)
            {
                ShuttleHabitatJoyOccupantRecord record = joyOccupants[i];
                if (record != null && record.IsJoying && IsHeldPawn(habitatHeldThings, record.Pawn))
                {
                    count++;
                }
            }

            return count;
        }

        internal static int CountHeldPawns(ThingOwner<Thing> habitatHeldThings)
        {
            int count = 0;
            for (int i = 0; i < habitatHeldThings.Count; i++)
            {
                if (habitatHeldThings[i] is Pawn)
                {
                    count++;
                }
            }

            return count;
        }

        internal static List<Pawn> GetSleepingOccupantsForReading(
            ThingOwner<Thing> habitatHeldThings,
            List<ShuttleHabitatOccupantRecord> sleepingOccupants)
        {
            List<Pawn> occupants = new List<Pawn>();
            for (int i = 0; i < sleepingOccupants.Count; i++)
            {
                ShuttleHabitatOccupantRecord record = sleepingOccupants[i];
                Pawn pawn = record != null ? record.Pawn : null;
                if (pawn != null && IsHeldPawn(habitatHeldThings, pawn))
                {
                    occupants.Add(pawn);
                }
            }

            return occupants;
        }

        internal static List<Pawn> GetDiningOccupantsForReading(
            ThingOwner<Thing> habitatHeldThings,
            List<ShuttleHabitatDiningOccupantRecord> diningOccupants)
        {
            List<Pawn> occupants = new List<Pawn>();
            for (int i = 0; i < diningOccupants.Count; i++)
            {
                ShuttleHabitatDiningOccupantRecord record = diningOccupants[i];
                Pawn pawn = record != null ? record.Pawn : null;
                if (pawn != null && IsHeldPawn(habitatHeldThings, pawn))
                {
                    occupants.Add(pawn);
                }
            }

            return occupants;
        }

        internal static List<Pawn> GetJoyOccupantsForReading(
            ThingOwner<Thing> habitatHeldThings,
            List<ShuttleHabitatJoyOccupantRecord> joyOccupants)
        {
            List<Pawn> occupants = new List<Pawn>();
            for (int i = 0; i < joyOccupants.Count; i++)
            {
                ShuttleHabitatJoyOccupantRecord record = joyOccupants[i];
                Pawn pawn = record != null ? record.Pawn : null;
                if (pawn != null && IsHeldPawn(habitatHeldThings, pawn))
                {
                    occupants.Add(pawn);
                }
            }

            return occupants;
        }

        internal static List<Pawn> GetOccupantsForReading(
            ThingOwner<Thing> habitatHeldThings,
            List<ShuttleHabitatOccupantRecord> sleepingOccupants,
            List<ShuttleHabitatDiningOccupantRecord> diningOccupants,
            List<ShuttleHabitatJoyOccupantRecord> joyOccupants)
        {
            List<Pawn> occupants = GetSleepingOccupantsForReading(habitatHeldThings, sleepingOccupants);
            for (int i = 0; i < diningOccupants.Count; i++)
            {
                ShuttleHabitatDiningOccupantRecord record = diningOccupants[i];
                Pawn pawn = record != null ? record.Pawn : null;
                if (pawn != null && IsHeldPawn(habitatHeldThings, pawn) && !occupants.Contains(pawn))
                {
                    occupants.Add(pawn);
                }
            }

            for (int i = 0; i < joyOccupants.Count; i++)
            {
                ShuttleHabitatJoyOccupantRecord record = joyOccupants[i];
                Pawn pawn = record != null ? record.Pawn : null;
                if (pawn != null && IsHeldPawn(habitatHeldThings, pawn) && !occupants.Contains(pawn))
                {
                    occupants.Add(pawn);
                }
            }

            for (int i = 0; i < habitatHeldThings.Count; i++)
            {
                Pawn pawn = habitatHeldThings[i] as Pawn;
                if (pawn != null && !occupants.Contains(pawn))
                {
                    occupants.Add(pawn);
                }
            }

            return occupants;
        }

        internal static bool TryGetDiningFoodForPawn(
            ThingOwner<Thing> habitatHeldThings,
            List<ShuttleHabitatDiningOccupantRecord> diningOccupants,
            Pawn pawn,
            out Thing food)
        {
            food = null;
            if (pawn == null)
            {
                return false;
            }

            for (int i = 0; i < diningOccupants.Count; i++)
            {
                ShuttleHabitatDiningOccupantRecord record = diningOccupants[i];
                if (record == null || record.Pawn != pawn)
                {
                    continue;
                }

                Thing recordFood = record.Food;
                if (IsContainedDiningFood(habitatHeldThings, recordFood))
                {
                    food = recordFood;
                    return true;
                }
            }

            return false;
        }

        internal static bool TryGetJoyKindForPawn(
            ThingOwner<Thing> habitatHeldThings,
            List<ShuttleHabitatJoyOccupantRecord> joyOccupants,
            Pawn pawn,
            out JoyKindDef joyKind)
        {
            joyKind = null;
            if (pawn == null)
            {
                return false;
            }

            for (int i = 0; i < joyOccupants.Count; i++)
            {
                ShuttleHabitatJoyOccupantRecord record = joyOccupants[i];
                if (record == null ||
                    record.Pawn != pawn ||
                    !IsContainedJoyPawn(habitatHeldThings, joyOccupants, pawn))
                {
                    continue;
                }

                joyKind = record.JoyKind;
                return joyKind != null;
            }

            return false;
        }
    }
}
