using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    /// <summary>
    /// Lightweight record for one mech currently contained by the shuttle mech charger holder.
    /// The pawn itself lives in the holder; this record stores stable lookup and module assignment.
    /// </summary>
    internal sealed class ShuttleMechChargingRecord : IExposable
    {
        private Pawn pawn;
        private int pawnThingID = -1;
        private string pawnLabel;
        private string moduleInstanceID;
        private float chargeRateFactor = 1f;
        private int entryTick = -1;
        private int lastChargeTick = -1;

        public ShuttleMechChargingRecord()
        {
        }

        internal ShuttleMechChargingRecord(
            Pawn pawn,
            string moduleInstanceID,
            float chargeRateFactor,
            int entryTick)
            : this(pawn, moduleInstanceID, chargeRateFactor, entryTick, -1)
        {
        }

        internal ShuttleMechChargingRecord(
            Pawn pawn,
            string moduleInstanceID,
            float chargeRateFactor,
            int entryTick,
            int lastChargeTick)
        {
            this.pawn = pawn;
            this.pawnThingID = pawn != null ? pawn.thingIDNumber : -1;
            this.pawnLabel = pawn != null ? pawn.LabelShort : string.Empty;
            this.moduleInstanceID = moduleInstanceID;
            this.chargeRateFactor = chargeRateFactor;
            this.entryTick = entryTick;
            this.lastChargeTick = lastChargeTick;
            this.Sanitize();
        }

        internal Pawn Pawn
        {
            get
            {
                return this.pawn;
            }
        }

        internal int PawnThingID
        {
            get
            {
                return this.pawnThingID;
            }
        }

        internal string PawnLabel
        {
            get
            {
                return this.pawnLabel;
            }
        }

        internal string ModuleInstanceID
        {
            get
            {
                return this.moduleInstanceID;
            }
        }

        internal float ChargeRateFactor
        {
            get
            {
                return this.chargeRateFactor;
            }
        }

        internal int EntryTick
        {
            get
            {
                return this.entryTick;
            }
        }

        internal int LastChargeTick
        {
            get
            {
                return this.lastChargeTick;
            }
        }

        internal void UpdateChargeTick(int tick)
        {
            this.lastChargeTick = tick;
        }

        internal void SetModuleAssignment(string moduleInstanceID, float chargeRateFactor)
        {
            this.moduleInstanceID = moduleInstanceID;
            this.chargeRateFactor = chargeRateFactor;
            this.Sanitize();
        }

        internal void SetPawn(Pawn pawn)
        {
            this.pawn = pawn;
            this.Sanitize();
        }

        internal void Sanitize()
        {
            if (this.pawn != null)
            {
                this.pawnThingID = this.pawn.thingIDNumber;
                if (string.IsNullOrEmpty(this.pawnLabel))
                {
                    this.pawnLabel = this.pawn.LabelShort;
                }
            }

            if (this.pawnThingID < -1)
            {
                this.pawnThingID = -1;
            }

            if (this.pawnLabel == null)
            {
                this.pawnLabel = string.Empty;
            }

            if (this.moduleInstanceID == null)
            {
                this.moduleInstanceID = string.Empty;
            }

            if (float.IsNaN(this.chargeRateFactor) ||
                float.IsInfinity(this.chargeRateFactor) ||
                this.chargeRateFactor <= 0f)
            {
                this.chargeRateFactor = 1f;
            }

            if (this.entryTick < -1)
            {
                this.entryTick = -1;
            }

            if (this.lastChargeTick < -1)
            {
                this.lastChargeTick = -1;
            }
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref this.pawn, "pawn");
            Scribe_Values.Look(ref this.pawnThingID, "pawnThingID", -1);
            Scribe_Values.Look(ref this.pawnLabel, "pawnLabel", string.Empty);
            Scribe_Values.Look(ref this.moduleInstanceID, "moduleInstanceID", string.Empty);
            Scribe_Values.Look(ref this.chargeRateFactor, "chargeRateFactor", 1f);
            Scribe_Values.Look(ref this.entryTick, "entryTick", -1);
            Scribe_Values.Look(ref this.lastChargeTick, "lastChargeTick", -1);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.Sanitize();
            }
        }
    }
}
