using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    /// <summary>
    /// Recreation record for one pawn using a selected JoyKind inside the shared
    /// Habitat holder.
    /// </summary>
    internal sealed class ShuttleHabitatJoyOccupantRecord : IExposable
    {
        private Pawn pawn;
        private JoyKindDef joyKind;
        private int joyStartTick = -1;
        private int joyTicks;
        private float joyGainRate = 1f;
        private int maxJoyTicks = 4000;
        private bool isJoying = true;

        public ShuttleHabitatJoyOccupantRecord()
        {
        }

        internal ShuttleHabitatJoyOccupantRecord(
            Pawn pawn,
            JoyKindDef joyKind,
            int joyStartTick,
            float joyGainRate,
            int maxJoyTicks)
        {
            this.pawn = pawn;
            this.joyKind = joyKind;
            this.joyStartTick = joyStartTick;
            this.joyGainRate = joyGainRate;
            this.maxJoyTicks = maxJoyTicks;
            this.isJoying = true;
            this.Sanitize();
        }

        internal ShuttleHabitatJoyOccupantRecord(
            Pawn pawn,
            JoyKindDef joyKind,
            int joyStartTick,
            int joyTicks,
            float joyGainRate,
            int maxJoyTicks,
            bool isJoying)
        {
            this.pawn = pawn;
            this.joyKind = joyKind;
            this.joyStartTick = joyStartTick;
            this.joyTicks = joyTicks;
            this.joyGainRate = joyGainRate;
            this.maxJoyTicks = maxJoyTicks;
            this.isJoying = isJoying;
            this.Sanitize();
        }

        internal Pawn Pawn
        {
            get
            {
                return this.pawn;
            }
        }

        internal JoyKindDef JoyKind
        {
            get
            {
                return this.joyKind;
            }
        }

        internal int JoyStartTick
        {
            get
            {
                return this.joyStartTick;
            }
        }

        internal int JoyTicks
        {
            get
            {
                return this.joyTicks;
            }
        }

        internal float JoyGainRate
        {
            get
            {
                return this.joyGainRate;
            }
        }

        internal int MaxJoyTicks
        {
            get
            {
                return this.maxJoyTicks;
            }
        }

        internal bool IsJoying
        {
            get
            {
                return this.isJoying;
            }
        }

        internal void TickJoy()
        {
            if (this.joyTicks < int.MaxValue)
            {
                this.joyTicks++;
            }
        }

        internal void StopJoying()
        {
            this.isJoying = false;
        }

        internal void Sanitize()
        {
            if (this.joyStartTick < -1)
            {
                this.joyStartTick = -1;
            }

            if (this.joyTicks < 0)
            {
                this.joyTicks = 0;
            }

            if (this.joyGainRate <= 0f || float.IsNaN(this.joyGainRate) || float.IsInfinity(this.joyGainRate))
            {
                this.joyGainRate = 1f;
            }

            if (this.maxJoyTicks < 1)
            {
                this.maxJoyTicks = 4000;
            }
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref this.pawn, "pawn");
            Scribe_Defs.Look(ref this.joyKind, "joyKind");
            Scribe_Values.Look(ref this.joyStartTick, "joyStartTick", -1);
            Scribe_Values.Look(ref this.joyTicks, "joyTicks", 0);
            Scribe_Values.Look(ref this.joyGainRate, "joyGainRate", 1f);
            Scribe_Values.Look(ref this.maxJoyTicks, "maxJoyTicks", 4000);
            Scribe_Values.Look(ref this.isJoying, "isJoying", true);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.Sanitize();
            }
        }
    }
}
