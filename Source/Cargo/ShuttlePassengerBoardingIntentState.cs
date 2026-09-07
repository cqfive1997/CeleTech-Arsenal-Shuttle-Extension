using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo
{
    /// <summary>
    /// Durable launch-plan intent for pawns that were already inside shuttle module holders
    /// when the player confirmed loading. It never owns or moves the pawns.
    /// </summary>
    internal sealed class ShuttlePassengerBoardingIntentState : IExposable
    {
        private List<ShuttlePassengerBoardingIntentRecord> records =
            new List<ShuttlePassengerBoardingIntentRecord>();

        private int lastReconcileTick = int.MinValue;

        internal IReadOnlyList<ShuttlePassengerBoardingIntentRecord> RecordsForReading
        {
            get
            {
                this.EnsureInitialized();
                return this.records;
            }
        }

        internal bool HasAny
        {
            get
            {
                this.EnsureInitialized();
                return this.records.Count > 0;
            }
        }

        internal void Replace(IEnumerable<Pawn> pawns)
        {
            this.EnsureInitialized();
            this.records.Clear();

            HashSet<int> seenThingIDs = new HashSet<int>();
            if (pawns != null)
            {
                foreach (Pawn pawn in pawns)
                {
                    if (!IsValidPawn(pawn) || !seenThingIDs.Add(pawn.thingIDNumber))
                    {
                        continue;
                    }

                    this.records.Add(new ShuttlePassengerBoardingIntentRecord(pawn));
                }
            }

            this.lastReconcileTick = int.MinValue;
        }

        internal bool Ensure(Pawn pawn)
        {
            this.EnsureInitialized();
            if (!IsValidPawn(pawn))
            {
                return false;
            }

            if (!this.ContainsThingID(pawn.thingIDNumber))
            {
                this.records.Add(new ShuttlePassengerBoardingIntentRecord(pawn));
            }

            this.lastReconcileTick = int.MinValue;
            return true;
        }

        internal bool RemoveByThingID(int thingIDNumber)
        {
            this.EnsureInitialized();
            bool removed = false;
            for (int i = this.records.Count - 1; i >= 0; i--)
            {
                ShuttlePassengerBoardingIntentRecord record = this.records[i];
                if (record != null && record.PawnThingID == thingIDNumber)
                {
                    this.records.RemoveAt(i);
                    removed = true;
                }
            }

            return removed;
        }

        internal bool ContainsThingID(int thingIDNumber)
        {
            this.EnsureInitialized();
            if (thingIDNumber < 0)
            {
                return false;
            }

            for (int i = 0; i < this.records.Count; i++)
            {
                ShuttlePassengerBoardingIntentRecord record = this.records[i];
                if (record != null && record.PawnThingID == thingIDNumber)
                {
                    return true;
                }
            }

            return false;
        }

        internal void RemoveInvalidRecords()
        {
            this.EnsureInitialized();
            HashSet<int> seenThingIDs = new HashSet<int>();
            for (int i = this.records.Count - 1; i >= 0; i--)
            {
                ShuttlePassengerBoardingIntentRecord record = this.records[i];
                Pawn pawn = record != null ? record.Pawn : null;
                if (record == null ||
                    pawn == null ||
                    pawn.Destroyed ||
                    pawn.Dead ||
                    !seenThingIDs.Add(record.PawnThingID))
                {
                    this.records.RemoveAt(i);
                }
            }
        }

        internal bool ShouldReconcile(int ticksGame, int intervalTicks)
        {
            this.EnsureInitialized();
            if (this.records.Count == 0)
            {
                return false;
            }

            if (ticksGame < 0 ||
                this.lastReconcileTick == int.MinValue ||
                ticksGame < this.lastReconcileTick ||
                ticksGame - this.lastReconcileTick >= intervalTicks)
            {
                this.lastReconcileTick = ticksGame;
                return true;
            }

            return false;
        }

        internal void Clear()
        {
            this.EnsureInitialized();
            this.records.Clear();
            this.lastReconcileTick = int.MinValue;
        }

        internal void EnsureInitialized()
        {
            if (this.records == null)
            {
                this.records = new List<ShuttlePassengerBoardingIntentRecord>();
            }
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(
                ref this.records,
                "records",
                LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.EnsureInitialized();
                this.RemoveInvalidRecords();
                this.lastReconcileTick = int.MinValue;
            }
        }

        private static bool IsValidPawn(Pawn pawn)
        {
            return pawn != null &&
                !pawn.Destroyed &&
                !pawn.Dead &&
                pawn.thingIDNumber >= 0;
        }
    }

    internal sealed class ShuttlePassengerBoardingIntentRecord : IExposable
    {
        private Pawn pawn;
        private int pawnThingID = -1;
        private string pawnLabel = string.Empty;

        public ShuttlePassengerBoardingIntentRecord()
        {
        }

        internal ShuttlePassengerBoardingIntentRecord(Pawn pawn)
        {
            this.pawn = pawn;
            this.RefreshIdentity();
        }

        internal Pawn Pawn
        {
            get { return this.pawn; }
        }

        internal int PawnThingID
        {
            get
            {
                return this.pawn != null
                    ? this.pawn.thingIDNumber
                    : this.pawnThingID;
            }
        }

        internal string PawnLabel
        {
            get
            {
                return this.pawn != null
                    ? this.pawn.LabelShortCap
                    : this.pawnLabel;
            }
        }

        public void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                this.RefreshIdentity();
            }

            Scribe_References.Look(ref this.pawn, "pawn");
            Scribe_Values.Look(ref this.pawnThingID, "pawnThingID", -1);
            Scribe_Values.Look(ref this.pawnLabel, "pawnLabel", string.Empty);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (this.pawnLabel == null)
                {
                    this.pawnLabel = string.Empty;
                }

                if (this.pawn != null)
                {
                    this.RefreshIdentity();
                }
            }
        }

        private void RefreshIdentity()
        {
            if (this.pawn == null)
            {
                return;
            }

            this.pawnThingID = this.pawn.thingIDNumber;
            this.pawnLabel = this.pawn.LabelShortCap;
        }
    }
}
