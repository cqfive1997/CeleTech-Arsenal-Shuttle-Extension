using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal static class HabitatOccupancyDebugFormatter
    {
        internal static string FormatDiningRecordPreservedWithoutMap(
            ShuttleHabitatDiningOccupantRecord record,
            bool pawnMissing,
            bool foodMissing)
        {
            return "[CeleTech Shuttle] Preserving invalid Habitat dining record because no map is available for safe eject/drop. " +
                "pawnMissing=" + pawnMissing +
                " foodMissing=" + foodMissing +
                " pawn=" + (record != null && record.Pawn != null ? record.Pawn.LabelShort : "null") +
                " food=" + (record != null && record.Food != null ? record.Food.LabelShort : "null");
        }

        internal static string FormatOrphanDiningFoodPreservedWithoutMap(Thing food)
        {
            return "[CeleTech Shuttle] Preserving orphan Habitat dining food because no map is available for safe drop. food=" +
                (food != null ? food.LabelShort : "null");
        }

        internal static string FormatUnknownHeldPawnPreservedWithoutMap(Pawn pawn)
        {
            return "[CeleTech Shuttle] Preserving unclassified Habitat held pawn because no map is available for safe eject. pawn=" +
                (pawn != null ? pawn.LabelShort : "null");
        }

        internal static string FormatJoyRecordClearedWithoutMap(
            ShuttleHabitatJoyOccupantRecord record,
            bool pawnDead,
            bool joyKindMissing)
        {
            return "[CeleTech Shuttle] Clearing invalid Habitat joy record without a map; the pawn remains in the holder for later safe eject. " +
                "pawnDead=" + pawnDead +
                " joyKindMissing=" + joyKindMissing +
                " pawn=" + (record != null && record.Pawn != null ? record.Pawn.LabelShort : "null") +
                " joyKind=" + (record != null && record.JoyKind != null ? record.JoyKind.defName : "null");
        }
    }
}
