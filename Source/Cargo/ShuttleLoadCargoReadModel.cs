using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo
{
    /// <summary>
    /// Transient read model for the Load Cargo dialog. It is rebuilt from the
    /// cargo backend on demand and must not become persisted cargo ownership truth.
    /// </summary>
    public sealed class ShuttleLoadCargoReadModel
    {
        private Dictionary<int, ShuttleCargoLoadAdmissionResult> admissionByThingID;
        private Dictionary<string, ShuttleCargoLoadAdmissionResult> admissionByDefName;
        private int admissionLookupRowCount = -1;

        public bool HasTransporter;
        public PlanetTile? MapTile;

        // Vanilla transporter mass snapshot used by the load dialog for display and validation.
        public float ExistingMassUsageKg;
        public float MassCapacityKg;
        public float AvailableMassKg;
        public float RegularAvailableMassKg;
        public float RefrigeratedAutoTransferCapacityKg;
        public float RefrigeratedAutoTransferUsedMassKg;
        public float RefrigeratedAutoTransferAvailableMassKg;
        public bool RefrigeratedMassSharesOverallCapacity;
        public int LoadedStackCount;
        public int LoadedThingCount;
        public int QueuedStackCount;
        public int QueuedThingCount;
        public bool HasQueuedLoads;
        public long SearchCorpusKey;

        // Transferables are vanilla UI selection objects. They are transient dialog input, not
        // durable shuttle cargo state.
        public List<TransferableOneWay> PassengerTransferables = new List<TransferableOneWay>();
        public HashSet<int> HeldPassengerThingIDs = new HashSet<int>();
        public List<TransferableOneWay> CargoTransferables = new List<TransferableOneWay>();
        public List<ShuttleCargoLoadAdmissionResult> CargoAdmissionRows =
            new List<ShuttleCargoLoadAdmissionResult>();

        // Human-readable preview lines only; commands must use transferables or cargo snapshots.
        public List<string> CurrentLoadPreviewLines = new List<string>();

        public bool HasMedicalBay;
        public int MedicalPatientSlots;
        public bool SupportsMedevacPriority;
        public int MedevacCandidateCount;
        public int CriticalMedevacCandidateCount;
        public int SelectedMedevacCandidateCount;
        public string MedevacSummaryLabel;
        public List<ShuttleMedevacCandidateReadModel> MedevacCandidates =
            new List<ShuttleMedevacCandidateReadModel>();

        public void RefreshSearchCorpusKey()
        {
            long key = 1469598103934665603L;
            key = AddLong(key, this.HasTransporter ? 1 : 0);
            key = AddLong(key, this.LoadedStackCount);
            key = AddLong(key, this.LoadedThingCount);
            key = AddLong(key, this.QueuedStackCount);
            key = AddLong(key, this.QueuedThingCount);
            key = AddLong(key, this.HasQueuedLoads ? 1 : 0);
            key = this.AddTransferableListToSearchCorpusKey(key, this.PassengerTransferables);
            key = this.AddThingIDSetToSearchCorpusKey(key, this.HeldPassengerThingIDs);
            key = this.AddTransferableListToSearchCorpusKey(key, this.CargoTransferables);
            key = this.AddAdmissionRowsToSearchCorpusKey(key);
            this.SearchCorpusKey = key;
        }

        public ShuttleCargoLoadAdmissionResult GetCargoAdmissionFor(Thing thing)
        {
            if (thing == null || this.CargoAdmissionRows == null)
            {
                return null;
            }

            this.EnsureCargoAdmissionLookup();
            ShuttleCargoLoadAdmissionResult row;
            if (thing.thingIDNumber > 0 &&
                this.admissionByThingID != null &&
                this.admissionByThingID.TryGetValue(thing.thingIDNumber, out row))
            {
                return row;
            }

            string defName = thing.def != null ? thing.def.defName : null;
            if (!string.IsNullOrEmpty(defName) &&
                this.admissionByDefName != null &&
                this.admissionByDefName.TryGetValue(defName, out row))
            {
                return row;
            }

            return null;
        }

        public void InvalidateCargoAdmissionLookup()
        {
            this.admissionByThingID = null;
            this.admissionByDefName = null;
            this.admissionLookupRowCount = -1;
        }

        public bool IsHeldPassenger(Thing thing)
        {
            return thing != null &&
                this.HeldPassengerThingIDs != null &&
                this.HeldPassengerThingIDs.Contains(thing.thingIDNumber);
        }

        private void EnsureCargoAdmissionLookup()
        {
            int rowCount = this.CargoAdmissionRows != null ? this.CargoAdmissionRows.Count : 0;
            if (this.admissionLookupRowCount == rowCount &&
                this.admissionByThingID != null &&
                this.admissionByDefName != null)
            {
                return;
            }

            this.admissionByThingID = new Dictionary<int, ShuttleCargoLoadAdmissionResult>();
            this.admissionByDefName = new Dictionary<string, ShuttleCargoLoadAdmissionResult>();
            this.admissionLookupRowCount = rowCount;
            if (this.CargoAdmissionRows == null)
            {
                return;
            }

            for (int i = 0; i < this.CargoAdmissionRows.Count; i++)
            {
                ShuttleCargoLoadAdmissionResult row = this.CargoAdmissionRows[i];
                if (row == null)
                {
                    continue;
                }

                if (row.ThingIDNumber > 0 && !this.admissionByThingID.ContainsKey(row.ThingIDNumber))
                {
                    this.admissionByThingID.Add(row.ThingIDNumber, row);
                }

                if (!string.IsNullOrEmpty(row.DefName) && !this.admissionByDefName.ContainsKey(row.DefName))
                {
                    this.admissionByDefName.Add(row.DefName, row);
                }
            }
        }

        private long AddTransferableListToSearchCorpusKey(
            long key,
            List<TransferableOneWay> transferables)
        {
            key = AddLong(key, transferables != null ? transferables.Count : 0);
            if (transferables == null)
            {
                return key;
            }

            for (int i = 0; i < transferables.Count; i++)
            {
                TransferableOneWay transferable = transferables[i];
                key = AddLong(key, i);
                if (transferable == null || transferable.things == null)
                {
                    continue;
                }

                key = AddLong(key, transferable.things.Count);
                int limit = transferable.things.Count;
                if (limit > 12)
                {
                    limit = 12;
                }

                for (int j = 0; j < limit; j++)
                {
                    Thing thing = transferable.things[j];
                    if (thing == null)
                    {
                        continue;
                    }

                    key = AddLong(key, thing.thingIDNumber);
                    key = AddLong(key, thing.stackCount);
                    key = this.AddStringToSearchCorpusKey(key, thing.def != null ? thing.def.defName : null);
                    key = this.AddStringToSearchCorpusKey(key, thing.LabelCap.ToString());
                    Pawn pawn = thing as Pawn;
                    if (pawn != null)
                    {
                        key = this.AddStringToSearchCorpusKey(key, pawn.LabelShortCap);
                        if (pawn.Name != null)
                        {
                            key = this.AddStringToSearchCorpusKey(key, pawn.Name.ToStringFull);
                            key = this.AddStringToSearchCorpusKey(key, pawn.Name.ToStringShort);
                        }

                        key = this.AddStringToSearchCorpusKey(key, pawn.kindDef != null ? pawn.kindDef.defName : null);
                    }
                }
            }

            return key;
        }

        private long AddAdmissionRowsToSearchCorpusKey(long key)
        {
            key = AddLong(key, this.CargoAdmissionRows != null ? this.CargoAdmissionRows.Count : 0);
            if (this.CargoAdmissionRows == null)
            {
                return key;
            }

            for (int i = 0; i < this.CargoAdmissionRows.Count; i++)
            {
                ShuttleCargoLoadAdmissionResult row = this.CargoAdmissionRows[i];
                if (row == null)
                {
                    continue;
                }

                key = AddLong(key, row.ThingIDNumber);
                key = this.AddStringToSearchCorpusKey(key, row.DefName);
                key = this.AddStringToSearchCorpusKey(key, row.DestinationLabel);
                key = this.AddStringToSearchCorpusKey(key, row.Reason);
                key = this.AddStringToSearchCorpusKey(key, row.RefrigeratedModuleLabel);
                key = AddLong(key, row.Accepted ? 1 : 0);
                key = AddLong(key, row.RegularAccepted ? 1 : 0);
                key = AddLong(key, row.RefrigeratedAccepted ? 1 : 0);
            }

            return key;
        }

        private long AddThingIDSetToSearchCorpusKey(
            long key,
            HashSet<int> thingIDs)
        {
            key = AddLong(key, thingIDs != null ? thingIDs.Count : 0);
            if (thingIDs == null)
            {
                return key;
            }

            List<int> orderedThingIDs = new List<int>(thingIDs);
            orderedThingIDs.Sort();
            for (int i = 0; i < orderedThingIDs.Count; i++)
            {
                key = AddLong(key, orderedThingIDs[i]);
            }

            return key;
        }

        private long AddStringToSearchCorpusKey(long key, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return key;
            }

            for (int i = 0; i < value.Length; i++)
            {
                key = AddLong(key, char.ToLowerInvariant(value[i]));
            }

            return key;
        }

        private static long AddLong(long key, long value)
        {
            unchecked
            {
                return (key * 1099511628211L) ^ value;
            }
        }
    }

    public sealed class ShuttleMedevacCandidateReadModel
    {
        public int PawnThingID;
        public string PawnLabel;
        public bool IsCritical;
        public bool IsDowned;
        public int Priority;
        public string ReasonLabel;
    }
}
