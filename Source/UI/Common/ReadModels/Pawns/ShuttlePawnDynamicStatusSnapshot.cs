using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels.Pawns
{
    internal sealed class ShuttlePawnDynamicStatusSnapshot
    {
        internal static readonly ShuttlePawnDynamicStatusSnapshot Empty =
            new ShuttlePawnDynamicStatusSnapshot(
                new List<ShuttlePawnDynamicStatusRecord>(),
                0,
                0);

        private readonly List<ShuttlePawnDynamicStatusRecord> records;

        internal ShuttlePawnDynamicStatusSnapshot(
            List<ShuttlePawnDynamicStatusRecord> records,
            int profileRevision,
            int cargoSnapshotRevision)
        {
            this.records =
                records ?? new List<ShuttlePawnDynamicStatusRecord>();
            this.ProfileRevision = profileRevision;
            this.CargoSnapshotRevision = cargoSnapshotRevision;
        }

        internal IReadOnlyList<ShuttlePawnDynamicStatusRecord> Records
        {
            get { return this.records; }
        }

        internal int ProfileRevision { get; private set; }

        internal int CargoSnapshotRevision { get; private set; }

        internal ShuttlePawnDynamicStatusRecord FindByPawnThingID(
            int pawnThingID)
        {
            if (pawnThingID <= 0)
            {
                return null;
            }

            for (int i = 0; i < this.records.Count; i++)
            {
                ShuttlePawnDynamicStatusRecord record = this.records[i];
                if (record != null && record.PawnThingID == pawnThingID)
                {
                    return record;
                }
            }

            return null;
        }
    }
}
