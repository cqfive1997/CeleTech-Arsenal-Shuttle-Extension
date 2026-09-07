using System;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules
{
    /// <summary>
    /// Saved pairing of an installed module instance and one built-in runtime system payload.
    /// Unknown or missing payload types leave the record inert instead of deleting identity data.
    /// </summary>
    internal sealed class ShuttleModuleRuntimeStateRecord : IExposable
    {
        private const int CurrentSaveVersion = 1;

        private int saveVersion = CurrentSaveVersion;
        private string moduleInstanceID;
        private string runtimeSystemKey;
        private IShuttleModuleRuntimeState state;

        public ShuttleModuleRuntimeStateRecord()
        {
        }

        public ShuttleModuleRuntimeStateRecord(
            string moduleInstanceID,
            string runtimeSystemKey,
            IShuttleModuleRuntimeState state)
        {
            this.moduleInstanceID = moduleInstanceID;
            this.runtimeSystemKey = ShuttleRuntimeSystemKeyUtility.Normalize(runtimeSystemKey);
            this.state = state;
            this.EnsureInitialized();
        }

        public string ModuleInstanceID
        {
            get
            {
                return this.moduleInstanceID;
            }
        }

        public string RuntimeSystemKey
        {
            get
            {
                return this.runtimeSystemKey;
            }
        }

        public IShuttleModuleRuntimeState State
        {
            get
            {
                return this.state;
            }
        }

        public bool Matches(string moduleInstanceID, string runtimeSystemKey)
        {
            return this.moduleInstanceID == moduleInstanceID
                && this.runtimeSystemKey == ShuttleRuntimeSystemKeyUtility.Normalize(runtimeSystemKey);
        }

        internal void NormalizeRuntimeSystemKeyForCompatibility()
        {
            this.runtimeSystemKey = ShuttleRuntimeSystemKeyUtility.Normalize(this.runtimeSystemKey);
        }

        internal void SetState(IShuttleModuleRuntimeState value)
        {
            // Called by bucket reconcile/install paths when a previously inert record gets a payload.
            this.state = value;
            this.EnsureInitialized();
        }

        public void EnsureInitialized()
        {
            if (this.state != null)
            {
                this.state.EnsureInitialized();
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.saveVersion, "saveVersion", 0);
            Scribe_Values.Look(ref this.moduleInstanceID, "moduleInstanceID");
            Scribe_Values.Look(ref this.runtimeSystemKey, "runtimeSystemKey");

            try
            {
                // Scribe persists the concrete runtime state type behind the interface.
                // Renaming concrete state classes requires migration.
                Scribe_Deep.Look(ref this.state, "state");
            }
            catch (Exception exception)
            {
                if (Scribe.mode == LoadSaveMode.LoadingVars ||
                    Scribe.mode == LoadSaveMode.PostLoadInit)
                {
                    Log.Warning("[CeleTech Shuttle] Failed to load module runtime state for module " +
                        this.moduleInstanceID + " / runtime " + this.runtimeSystemKey +
                        ". Keeping the runtime record inert. Exception: " + exception);
                    this.state = null;
                    return;
                }

                Log.Error("[CeleTech Shuttle] Failed to expose module runtime state for module " +
                    this.moduleInstanceID + " / runtime " + this.runtimeSystemKey +
                    ". Runtime state was not cleared. Exception: " + exception);
            }

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                int loadedVersion = this.saveVersion;

                this.NormalizeRuntimeSystemKeyForCompatibility();
                this.EnsureInitialized();
                this.MigratePostLoad(loadedVersion);
                this.saveVersion = CurrentSaveVersion;
            }
        }

        private void MigratePostLoad(int loadedVersion)
        {
            if (loadedVersion > CurrentSaveVersion)
            {
                Log.Warning("[CeleTech Shuttle] Loaded ShuttleModuleRuntimeStateRecord save version " +
                    loadedVersion + " newer than supported version " + CurrentSaveVersion +
                    ". Keeping loaded runtime record intact.");
            }
        }
    }
}
