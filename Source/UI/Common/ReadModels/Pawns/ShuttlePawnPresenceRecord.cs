using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels.Pawns
{
    internal sealed class ShuttlePawnPresenceRecord
    {
        internal int PawnThingID;
        internal string StableKey;
        internal Pawn Pawn;
        internal Thing DisplayThing;
        internal string Label;
        internal ShuttlePawnPresenceKind Kind;
        internal string SourceKey;
        internal string SourceLabel;
        internal bool IsHumanlike;
        internal bool IsColonist;
        internal bool IsPrisoner;
        internal bool IsSlave;
        internal bool IsAnimal;
        internal bool IsMech;
        internal bool IsLoadedCargo;
        internal bool IsAssignedToLoad;
        internal bool IsMedicalPatient;
        internal bool IsOccupant;
        internal int CargoRegionIndex = -1;
        internal int TransporterIndex = -1;
        internal int LoadedIndex = -1;
        internal int QueueIndex = -1;
        internal int SourceIndex = -1;
        internal object SourceModel;

        internal bool HasKind(ShuttlePawnPresenceKind kind)
        {
            return kind != ShuttlePawnPresenceKind.None &&
                (this.Kind & kind) == kind;
        }
    }
}
