using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    /// <summary>
    /// Dining record for a pawn plus the exact food Thing being eaten. Launch
    /// transfer must preserve this pair as one activity.
    /// </summary>
    internal sealed class ShuttleHabitatDiningOccupantRecord : IExposable
    {
        private Pawn pawn;
        private Thing food;
        private int diningStartTick = -1;
        private int chewTicksLeft;
        private int chewTicksTotal;
        private bool isDining = true;
        private bool finalized;

        public ShuttleHabitatDiningOccupantRecord()
        {
        }

        internal ShuttleHabitatDiningOccupantRecord(
            Pawn pawn,
            Thing food,
            int diningStartTick,
            int chewTicksTotal)
        {
            this.pawn = pawn;
            this.food = food;
            this.diningStartTick = diningStartTick;
            this.chewTicksTotal = chewTicksTotal > 0 ? chewTicksTotal : 1;
            this.chewTicksLeft = this.chewTicksTotal;
            this.isDining = true;
        }

        internal ShuttleHabitatDiningOccupantRecord(
            Pawn pawn,
            Thing food,
            int diningStartTick,
            int chewTicksLeft,
            int chewTicksTotal,
            bool isDining,
            bool finalized)
        {
            this.pawn = pawn;
            this.food = food;
            this.diningStartTick = diningStartTick;
            this.chewTicksLeft = chewTicksLeft;
            this.chewTicksTotal = chewTicksTotal;
            this.isDining = isDining;
            this.finalized = finalized;
            this.Sanitize();
        }

        internal Pawn Pawn
        {
            get
            {
                return this.pawn;
            }
        }

        internal Thing Food
        {
            get
            {
                return this.food;
            }
        }

        internal int DiningStartTick
        {
            get
            {
                return this.diningStartTick;
            }
        }

        internal int ChewTicksLeft
        {
            get
            {
                return this.chewTicksLeft;
            }
        }

        internal int ChewTicksTotal
        {
            get
            {
                return this.chewTicksTotal;
            }
        }

        internal bool IsDining
        {
            get
            {
                return this.isDining;
            }
        }

        internal bool IsFinalized
        {
            get
            {
                return this.finalized;
            }
        }

        internal void TickChew()
        {
            if (this.chewTicksLeft > 0)
            {
                this.chewTicksLeft--;
            }
        }

        internal void StopDining()
        {
            this.isDining = false;
        }

        internal void MarkFinalized()
        {
            this.finalized = true;
        }

        internal void Sanitize()
        {
            if (this.diningStartTick < -1)
            {
                this.diningStartTick = -1;
            }

            if (this.chewTicksTotal < 1)
            {
                this.chewTicksTotal = 1;
            }

            if (this.chewTicksLeft < 0)
            {
                this.chewTicksLeft = 0;
            }

            if (this.chewTicksLeft > this.chewTicksTotal)
            {
                this.chewTicksLeft = this.chewTicksTotal;
            }
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref this.pawn, "pawn");
            Scribe_References.Look(ref this.food, "food");
            Scribe_Values.Look(ref this.diningStartTick, "diningStartTick", -1);
            Scribe_Values.Look(ref this.chewTicksLeft, "chewTicksLeft", 0);
            Scribe_Values.Look(ref this.chewTicksTotal, "chewTicksTotal", 1);
            Scribe_Values.Look(ref this.isDining, "isDining", true);
            Scribe_Values.Look(ref this.finalized, "finalized", false);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.Sanitize();
            }
        }
    }
}
