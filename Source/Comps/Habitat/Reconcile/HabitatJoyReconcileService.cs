using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal static class HabitatJoyReconcileService
    {
        internal static bool TryRemoveNullRecord(
            HabitatJoyReconcileAccess access,
            int index,
            out ShuttleHabitatJoyOccupantRecord record)
        {
            record = access.GetJoyRecord(index);
            if (record != null)
            {
                return false;
            }

            access.RemoveJoyRecordAt(index);
            return true;
        }

        internal static void SanitizeRecord(ShuttleHabitatJoyOccupantRecord record)
        {
            record.Sanitize();
        }

        internal static bool IsInvalidRecord(
            HabitatJoyReconcileAccess access,
            ShuttleHabitatJoyOccupantRecord record,
            out Pawn pawn,
            out bool pawnMissing,
            out bool pawnDead,
            out bool pawnDestroyed,
            out bool joyKindMissing)
        {
            pawn = record.Pawn;
            pawnMissing = pawn == null || !access.IsHeldPawn(pawn);
            pawnDead = pawn != null && pawn.Dead;
            pawnDestroyed = pawn != null && pawn.Destroyed;
            joyKindMissing = record.JoyKind == null;
            return pawnMissing || pawnDead || pawnDestroyed || joyKindMissing;
        }

        internal static void StopAndRemoveRecord(
            HabitatJoyReconcileAccess access,
            ShuttleHabitatJoyOccupantRecord record,
            int index)
        {
            record.StopJoying();
            access.RemoveJoyRecordAt(index);
        }
    }
}
