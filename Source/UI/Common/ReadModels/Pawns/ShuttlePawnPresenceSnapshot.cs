using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels.Pawns
{
    internal sealed class ShuttlePawnPresenceSnapshot
    {
        internal static readonly ShuttlePawnPresenceSnapshot Empty =
            new ShuttlePawnPresenceSnapshot(
                new List<ShuttlePawnPresenceRecord>(),
                0,
                0);

        private readonly List<ShuttlePawnPresenceRecord> records;

        internal ShuttlePawnPresenceSnapshot(
            List<ShuttlePawnPresenceRecord> records,
            int profileRevision,
            int cargoSnapshotRevision)
        {
            this.records = records ?? new List<ShuttlePawnPresenceRecord>();
            this.ProfileRevision = profileRevision;
            this.CargoSnapshotRevision = cargoSnapshotRevision;
        }

        internal IReadOnlyList<ShuttlePawnPresenceRecord> Records
        {
            get { return this.records; }
        }

        internal int ProfileRevision { get; private set; }

        internal int CargoSnapshotRevision { get; private set; }

        internal ShuttlePawnPresenceRecord FindByPawnThingID(int pawnThingID)
        {
            if (pawnThingID <= 0)
            {
                return null;
            }

            for (int i = 0; i < this.records.Count; i++)
            {
                ShuttlePawnPresenceRecord record = this.records[i];
                if (record != null && record.PawnThingID == pawnThingID)
                {
                    return record;
                }
            }

            return null;
        }

        internal ShuttlePawnPresenceRecord FindByPawnThingIDAndKind(
            int pawnThingID,
            ShuttlePawnPresenceKind kind)
        {
            if (pawnThingID <= 0)
            {
                return null;
            }

            for (int i = 0; i < this.records.Count; i++)
            {
                ShuttlePawnPresenceRecord record = this.records[i];
                if (record != null &&
                    record.PawnThingID == pawnThingID &&
                    record.HasKind(kind))
                {
                    return record;
                }
            }

            return null;
        }
    }
}
