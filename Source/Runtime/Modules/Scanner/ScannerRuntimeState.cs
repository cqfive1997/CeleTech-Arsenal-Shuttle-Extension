using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Scanner
{
    /// <summary>
    /// Minimal diagnostic payload for the built-in scanner smoke runtime.
    /// It proves module runtime save/load and ticking without scanner gameplay.
    /// </summary>
    internal sealed class ScannerRuntimeState : IShuttleModuleRuntimeState
    {
        private const int CurrentSaveVersion = 1;

        private int saveVersion = CurrentSaveVersion;
        private int lastReconcileTick = -1;
        private int lastTick = -1;

        public int LastReconcileTick
        {
            get
            {
                return this.lastReconcileTick;
            }
        }

        public int LastTick
        {
            get
            {
                return this.lastTick;
            }
        }

        public void EnsureInitialized()
        {
        }

        internal void MarkReconciled(int ticksGame)
        {
            this.lastReconcileTick = ticksGame;
        }

        internal void MarkTicked(int ticksGame)
        {
            this.lastTick = ticksGame;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.saveVersion, "saveVersion", 0);
            Scribe_Values.Look(ref this.lastReconcileTick, "lastReconcileTick", -1);
            Scribe_Values.Look(ref this.lastTick, "lastTick", -1);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                int loadedVersion = this.saveVersion;

                this.EnsureInitialized();
                this.MigratePostLoad(loadedVersion);
                this.saveVersion = CurrentSaveVersion;
            }
        }

        private void MigratePostLoad(int loadedVersion)
        {
            if (loadedVersion > CurrentSaveVersion)
            {
                Log.Warning("[CeleTech Shuttle] Loaded ScannerRuntimeState save version " +
                    loadedVersion + " newer than supported version " + CurrentSaveVersion +
                    ". Keeping loaded scanner diagnostics intact.");
            }

            // Version 0 predates this explicit schema marker. Its diagnostic tick fields are
            // already read with safe defaults, so no destructive migration is needed.
            if (loadedVersion < 1)
            {
                return;
            }
        }
    }
}
