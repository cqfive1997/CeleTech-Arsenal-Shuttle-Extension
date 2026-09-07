namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal static class HabitatSleepReconcileService
    {
        internal static void RemoveInvalidRecords(HabitatSleepReconcileAccess access)
        {
            for (int i = access.SleepingRecordCount - 1; i >= 0; i--)
            {
                ShuttleHabitatOccupantRecord record = access.GetSleepingRecord(i);
                if (record == null)
                {
                    access.RemoveSleepingRecordAt(i);
                    continue;
                }

                record.Sanitize();
                if (record.Pawn == null || !access.IsHeldPawn(record.Pawn))
                {
                    access.RemoveSleepingRecordAt(i);
                }
            }
        }
    }
}
