using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    /// <summary>
    /// Sleep record for one pawn held by the Habitat holder. This record stores
    /// rest-progress metadata only; the Pawn itself remains in habitatHeldThings.
    /// </summary>
    internal sealed class ShuttleHabitatOccupantRecord : IExposable
    {
        private Pawn pawn;
        private int restStartTick = -1;
        private int restedTicks;
        private bool isSleeping = true;

        public ShuttleHabitatOccupantRecord()
        {
        }

        internal ShuttleHabitatOccupantRecord(Pawn pawn, int restStartTick)
        {
            this.pawn = pawn;
            this.restStartTick = restStartTick;
            this.restedTicks = 0;
            this.isSleeping = true;
        }

        internal ShuttleHabitatOccupantRecord(Pawn pawn, int restStartTick, int restedTicks, bool isSleeping)
        {
            this.pawn = pawn;
            this.restStartTick = restStartTick;
            this.restedTicks = restedTicks;
            this.isSleeping = isSleeping;
            this.Sanitize();
        }

        internal Pawn Pawn
        {
            get
            {
                return this.pawn;
            }
        }

        internal int RestStartTick
        {
            get
            {
                return this.restStartTick;
            }
        }

        internal int RestedTicks
        {
            get
            {
                return this.restedTicks;
            }
        }

        internal bool IsSleeping
        {
            get
            {
                return this.isSleeping;
            }
        }

        internal void TickRested()
        {
            if (this.restedTicks < int.MaxValue)
            {
                this.restedTicks++;
            }
        }

        internal void StopSleeping()
        {
            this.isSleeping = false;
        }

        internal void Sanitize()
        {
            if (this.restStartTick < -1)
            {
                this.restStartTick = -1;
            }

            if (this.restedTicks < 0)
            {
                this.restedTicks = 0;
            }
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref this.pawn, "pawn");
            Scribe_Values.Look(ref this.restStartTick, "restStartTick", -1);
            Scribe_Values.Look(ref this.restedTicks, "restedTicks", 0);
            Scribe_Values.Look(ref this.isSleeping, "isSleeping", true);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.Sanitize();
            }
        }
    }
}
