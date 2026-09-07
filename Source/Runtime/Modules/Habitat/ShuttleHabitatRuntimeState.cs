using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Habitat
{
    /// <summary>
    /// Minimal durable payload for one installed habitat module.
    /// It records only module-local usage timestamps; no pawn, cargo, slot, or profile data lives here.
    /// </summary>
    internal sealed class ShuttleHabitatRuntimeState : IShuttleModuleRuntimeState
    {
        private const int NeverUsedTick = -1;

        private int lastUsedTick = NeverUsedTick;
        private int lastSleepTick = NeverUsedTick;
        private int lastEatTick = NeverUsedTick;

        public void EnsureInitialized()
        {
            this.SanitizeForRuntimeOnly();
        }

        internal void MarkUsed(int tick)
        {
            this.lastUsedTick = SanitizeTick(tick);
        }

        internal void MarkSleepUsed(int tick)
        {
            tick = SanitizeTick(tick);
            this.lastSleepTick = tick;
            this.lastUsedTick = Max(this.lastUsedTick, tick);
        }

        internal void MarkEatUsed(int tick)
        {
            tick = SanitizeTick(tick);
            this.lastEatTick = tick;
            this.lastUsedTick = Max(this.lastUsedTick, tick);
        }

        internal void SanitizeForRuntimeOnly()
        {
            this.lastUsedTick = SanitizeTick(this.lastUsedTick);
            this.lastSleepTick = SanitizeTick(this.lastSleepTick);
            this.lastEatTick = SanitizeTick(this.lastEatTick);
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.lastUsedTick, "lastUsedTick", NeverUsedTick);
            Scribe_Values.Look(ref this.lastSleepTick, "lastSleepTick", NeverUsedTick);
            Scribe_Values.Look(ref this.lastEatTick, "lastEatTick", NeverUsedTick);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.SanitizeForRuntimeOnly();
            }
        }

        private static int SanitizeTick(int tick)
        {
            return tick < 0 ? NeverUsedTick : tick;
        }

        private static int Max(int a, int b)
        {
            return a > b ? a : b;
        }
    }
}
