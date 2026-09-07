using System;
using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules
{
    /// <summary>
    /// Durable collection of module-owned runtime state records.
    /// It preserves orphans for diagnostics; explicit removal is only used for known module cleanup.
    /// </summary>
    internal sealed class ShuttleModuleRuntimeStateBucket : IExposable
    {
        private const int CurrentSaveVersion = 1;

        private static readonly IReadOnlyList<ShuttleModuleRuntimeStateRecord> EmptyRecords =
            new ShuttleModuleRuntimeStateRecord[0];

        private int saveVersion = CurrentSaveVersion;
        private List<ShuttleModuleRuntimeStateRecord> records =
            new List<ShuttleModuleRuntimeStateRecord>();
        private int dispatchRevision;

        internal int DispatchRevision
        {
            get
            {
                return this.dispatchRevision;
            }
        }

        public IReadOnlyList<ShuttleModuleRuntimeStateRecord> RecordsForRead
        {
            get
            {
                // Read callers must not normalize or repair the bucket implicitly.
                return this.records ?? EmptyRecords;
            }
        }

        public void EnsureInitialized()
        {
            // Container repair is safe during load/reconcile, but does not remove orphan records.
            if (this.records == null)
            {
                this.records = new List<ShuttleModuleRuntimeStateRecord>();
                this.MarkDispatchRevisionChanged();
            }
            for (int i = 0; i < this.records.Count; i++)
            {
                if (this.records[i] != null)
                {
                    this.records[i].EnsureInitialized();
                }
            }
        }

        public IShuttleModuleRuntimeState GetOrCreateState(
            string moduleInstanceID,
            string runtimeSystemKey,
            Func<IShuttleModuleRuntimeState> factory)
        {
            // This is intended for reconcile/install paths. Tick and launch validation should
            // use TryGetState so failed factories do not retry every tick.
            this.EnsureInitialized();

            if (string.IsNullOrEmpty(moduleInstanceID) || string.IsNullOrEmpty(runtimeSystemKey))
            {
                return null;
            }

            string normalizedRuntimeSystemKey = ShuttleRuntimeSystemKeyUtility.Normalize(runtimeSystemKey);
            if (string.IsNullOrEmpty(normalizedRuntimeSystemKey))
            {
                return null;
            }

            ShuttleModuleRuntimeStateRecord record = this.FindRecord(moduleInstanceID, normalizedRuntimeSystemKey);
            if (record != null)
            {
                if (record.State == null && factory != null)
                {
                    IShuttleModuleRuntimeState replacementState =
                        this.TryCreateState(moduleInstanceID, normalizedRuntimeSystemKey, factory);
                    record.SetState(replacementState);
                    if (replacementState != null)
                    {
                        this.MarkDispatchRevisionChanged();
                    }
                }

                return record.State;
            }

            if (factory == null)
            {
                // A null factory is a caller bug or an intentionally unsupported system.
                // Do not create inert records for that case.
                return null;
            }

            IShuttleModuleRuntimeState state = this.TryCreateState(
                moduleInstanceID,
                normalizedRuntimeSystemKey,
                factory);

            this.records.Add(new ShuttleModuleRuntimeStateRecord(
                moduleInstanceID,
                normalizedRuntimeSystemKey,
                state));
            this.MarkDispatchRevisionChanged();

            return state;
        }

        public bool TryGetState(
            string moduleInstanceID,
            string runtimeSystemKey,
            out IShuttleModuleRuntimeState state)
        {
            // Lookup only. It never creates missing state.
            this.EnsureInitialized();
            state = null;

            string normalizedRuntimeSystemKey = ShuttleRuntimeSystemKeyUtility.Normalize(runtimeSystemKey);
            if (string.IsNullOrEmpty(normalizedRuntimeSystemKey))
            {
                return false;
            }

            ShuttleModuleRuntimeStateRecord record = this.FindRecord(moduleInstanceID, normalizedRuntimeSystemKey);
            if (record == null || record.State == null)
            {
                return false;
            }

            state = record.State;
            return true;
        }

        public int RemoveAllForModule(string moduleInstanceID)
        {
            // Explicit cleanup after confirmed module removal. Reconcile does not call this,
            // which preserves orphan records for diagnostics.
            this.EnsureInitialized();

            if (string.IsNullOrEmpty(moduleInstanceID))
            {
                return 0;
            }

            int removed = 0;
            for (int i = this.records.Count - 1; i >= 0; i--)
            {
                ShuttleModuleRuntimeStateRecord record = this.records[i];
                if (record != null && record.ModuleInstanceID == moduleInstanceID)
                {
                    this.records.RemoveAt(i);
                    removed++;
                }
            }

            if (removed > 0)
            {
                this.MarkDispatchRevisionChanged();
            }

            return removed;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.saveVersion, "saveVersion", 0);
            Scribe_Collections.Look(ref this.records, "records", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                int loadedVersion = this.saveVersion;

                this.EnsureInitialized();
                this.NormalizeAfterLoad();
                this.MigratePostLoad(loadedVersion);
                this.saveVersion = CurrentSaveVersion;
                this.MarkDispatchRevisionChanged();
            }
        }

        public void NormalizeAfterLoad()
        {
            // Post-load normalization removes duplicate/null records only. It does not compare
            // against current AssemblyState, so missing modules remain preserved.
            this.EnsureInitialized();
            this.NormalizeRecordKeysForCompatibility();
            this.RemoveDuplicateRecords();
        }

        private void NormalizeRecordKeysForCompatibility()
        {
            for (int i = 0; i < this.records.Count; i++)
            {
                if (this.records[i] != null)
                {
                    this.records[i].NormalizeRuntimeSystemKeyForCompatibility();
                }
            }
        }

        private ShuttleModuleRuntimeStateRecord FindRecord(
            string moduleInstanceID,
            string runtimeSystemKey)
        {
            for (int i = 0; i < this.records.Count; i++)
            {
                ShuttleModuleRuntimeStateRecord record = this.records[i];
                if (record != null && record.Matches(moduleInstanceID, runtimeSystemKey))
                {
                    return record;
                }
            }

            return null;
        }

        private IShuttleModuleRuntimeState TryCreateState(
            string moduleInstanceID,
            string runtimeSystemKey,
            Func<IShuttleModuleRuntimeState> factory)
        {
            if (factory == null)
            {
                return null;
            }

            try
            {
                IShuttleModuleRuntimeState state = factory();
                if (state != null)
                {
                    state.EnsureInitialized();
                }

                return state;
            }
            catch (Exception exception)
            {
                // Keep an inert record when creation fails after a valid factory was supplied.
                Log.Warning("[CeleTech Shuttle] Failed to create module runtime state for module " +
                    moduleInstanceID + " / runtime " + runtimeSystemKey +
                    ". Keeping the runtime record inert. Exception: " + exception);
                return null;
            }
        }

        private void RemoveDuplicateRecords()
        {
            bool changed = false;
            for (int i = this.records.Count - 1; i >= 0; i--)
            {
                if (this.records[i] == null)
                {
                    this.records.RemoveAt(i);
                    changed = true;
                }
            }

            for (int i = 0; i < this.records.Count; i++)
            {
                ShuttleModuleRuntimeStateRecord first = this.records[i];
                if (first == null)
                {
                    continue;
                }

                for (int j = this.records.Count - 1; j > i; j--)
                {
                    ShuttleModuleRuntimeStateRecord duplicate = this.records[j];
                    if (duplicate == null ||
                        !first.Matches(duplicate.ModuleInstanceID, duplicate.RuntimeSystemKey))
                    {
                        continue;
                    }

                    if (first.State == null && duplicate.State != null)
                    {
                        first.SetState(duplicate.State);
                        changed = true;
                    }

                    this.records.RemoveAt(j);
                    changed = true;
                }
            }

            if (changed)
            {
                this.MarkDispatchRevisionChanged();
            }
        }

        private void MigratePostLoad(int loadedVersion)
        {
            if (loadedVersion > CurrentSaveVersion)
            {
                Log.Warning("[CeleTech Shuttle] Loaded ShuttleModuleRuntimeStateBucket save version " +
                    loadedVersion + " newer than supported version " + CurrentSaveVersion +
                    ". Keeping loaded runtime bucket intact.");
            }
        }

        private void MarkDispatchRevisionChanged()
        {
            unchecked
            {
                this.dispatchRevision++;
            }
        }
    }
}
