using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal static class HabitatOccupancyReconciler
    {
        internal static void ReconcileRecordsToContainedPawns(HabitatOccupancyReconcileAccess access)
        {
            HabitatSleepReconcileService.RemoveInvalidRecords(access.CreateSleepAccess());
        }

        internal static void ReconcileJoyRecordsToContainedPawns(HabitatOccupancyReconcileAccess access)
        {
            Map map = access.HostMap;
            RemoveInvalidJoyRecords(access, map);
            ReconcileUnknownHeldPawns(access, map);
        }

        internal static void ReconcileDiningRecordsToContainedThings(HabitatOccupancyReconcileAccess access)
        {
            Map map = access.HostMap;
            RemoveInvalidDiningRecords(access, map);

            for (int i = 0; i < access.HeldThingCount; i++)
            {
                Pawn pawn = access.GetHeldThing(i) as Pawn;
                if (pawn != null &&
                    !access.HasDiningRecordFor(pawn) &&
                    !access.HasRecordFor(pawn) &&
                    !access.HasJoyRecordFor(pawn))
                {
                    Thing food = access.FindUnassignedDiningFood();
                    if (food == null)
                    {
                        continue;
                    }

                    access.AddDiningRecord(
                        pawn,
                        food,
                        Find.TickManager != null ? Find.TickManager.TicksGame : -1,
                        access.ComputeDiningChewTicks(pawn, food));
                }
            }

            ReconcileOrphanDiningFood(access, map);
            ReconcileUnknownHeldPawns(access, map);
        }

        private static void RemoveInvalidDiningRecords(HabitatOccupancyReconcileAccess access, Map map)
        {
            for (int i = access.DiningRecordCount - 1; i >= 0; i--)
            {
                ShuttleHabitatDiningOccupantRecord record = access.GetDiningRecord(i);
                if (record == null)
                {
                    access.RemoveDiningRecordAt(i);
                    continue;
                }

                record.Sanitize();
                bool pawnMissing = record.Pawn == null || !access.IsHeldPawn(record.Pawn);
                bool foodMissing = record.Food == null || record.Food.Destroyed || !access.IsContainedDiningFood(record.Food);
                if (pawnMissing || foodMissing)
                {
                    if (map == null)
                    {
                        access.LogDiningRecordPreservedWithoutMap(record, pawnMissing, foodMissing);
                        continue;
                    }

                    if (!access.TryResolveDiningStopPair(record, map, "Habitat dining record reconcile"))
                    {
                        Log.Error("[CeleTech Shuttle] Habitat dining reconcile found an invalid dining record but could not safely resolve pawn/food pair; record preserved. pawnMissing=" +
                            pawnMissing +
                            " foodMissing=" +
                            foodMissing +
                            " " +
                            access.DescribeDiningStopState(record));
                        continue;
                    }

                    record.StopDining();
                    access.RemoveDiningRecordAt(i);
                }
            }
        }

        private static void RemoveInvalidJoyRecords(HabitatOccupancyReconcileAccess access, Map map)
        {
            HabitatJoyReconcileAccess joyAccess = access.CreateJoyAccess();

            for (int i = joyAccess.JoyRecordCount - 1; i >= 0; i--)
            {
                ShuttleHabitatJoyOccupantRecord record;
                if (HabitatJoyReconcileService.TryRemoveNullRecord(joyAccess, i, out record))
                {
                    continue;
                }

                HabitatJoyReconcileService.SanitizeRecord(record);
                Pawn pawn;
                bool pawnMissing;
                bool pawnDead;
                bool pawnDestroyed;
                bool joyKindMissing;
                if (HabitatJoyReconcileService.IsInvalidRecord(
                    joyAccess,
                    record,
                    out pawn,
                    out pawnMissing,
                    out pawnDead,
                    out pawnDestroyed,
                    out joyKindMissing))
                {
                    if (pawnDestroyed)
                    {
                        RemoveDestroyedHeldPawn(access, pawn);
                    }
                    else if (!pawnMissing && map != null)
                    {
                        Pawn droppedPawn;
                        access.TryDropJoyPawn(record, map, out droppedPawn);
                    }
                    else if (!pawnMissing && map == null)
                    {
                        access.LogJoyRecordClearedWithoutMap(record, pawnDead, joyKindMissing);
                    }

                    HabitatJoyReconcileService.StopAndRemoveRecord(joyAccess, record, i);
                }
            }
        }

        private static void ReconcileOrphanDiningFood(HabitatOccupancyReconcileAccess access, Map map)
        {
            for (int i = access.HeldThingCount - 1; i >= 0; i--)
            {
                Thing food = access.GetHeldThing(i);
                if (food == null || food.Destroyed || food is Pawn || access.HasDiningRecordForFood(food))
                {
                    continue;
                }

                if (map == null)
                {
                    access.LogOrphanDiningFoodPreservedWithoutMap(food);
                    continue;
                }

                Thing resultingThing;
                access.TryDropHeldThing(food, map, out resultingThing);
            }
        }

        private static void ReconcileUnknownHeldPawns(HabitatOccupancyReconcileAccess access, Map map)
        {
            for (int i = access.HeldThingCount - 1; i >= 0; i--)
            {
                Pawn pawn = access.GetHeldThing(i) as Pawn;
                if (pawn == null ||
                    access.HasRecordFor(pawn) ||
                    access.HasDiningRecordFor(pawn) ||
                    access.HasJoyRecordFor(pawn))
                {
                    continue;
                }

                if (map == null)
                {
                    access.LogUnknownHeldPawnPreservedWithoutMap(pawn);
                    continue;
                }

                Thing resultingThing;
                access.TryDropHeldThing(pawn, map, out resultingThing);
            }
        }

        private static void RemoveDestroyedHeldPawn(HabitatOccupancyReconcileAccess access, Pawn pawn)
        {
            if (pawn == null || !pawn.Destroyed || !access.HasHabitatHolder())
            {
                return;
            }

            access.RemoveHeldPawn(pawn);
        }
    }
}
