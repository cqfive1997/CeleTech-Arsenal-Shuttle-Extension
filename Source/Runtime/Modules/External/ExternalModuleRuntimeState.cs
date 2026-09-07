using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CeleTech.ShuttleExtension.ModularShuttle.API.Runtime;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalModuleRuntimeState : IShuttleModuleRuntimeState
    {
        internal const int MaxValueCount = 128;
        internal const int MaxKeyLength = 64;
        internal const int MaxValueLength = 2048;
        private const int MaxFailureMessageLength = 1024;

        private string ownerPackageId;
        private string runtimeSystemKey;
        private int schemaVersion = 1;
        private bool initialized;
        private bool initializationFailed;
        private bool migrationFailed;
        private bool runtimeExecutionFailed;
        private string lastFailureMessage;
        private int lastFailureTick = -1;
        private Dictionary<string, string> values = new Dictionary<string, string>();
        private ShuttleExternalModuleInfo cachedPowerDemandModuleInfo;
        private ExternalRuntimeStateReader cachedPowerDemandStateReader;

        internal ExternalModuleRuntimeState()
        {
        }

        internal ExternalModuleRuntimeState(
            string ownerPackageId,
            string runtimeSystemKey,
            int schemaVersion)
        {
            this.EnsureInitialized();
            this.ownerPackageId = NormalizeKey(ownerPackageId);
            this.runtimeSystemKey = NormalizeKey(runtimeSystemKey);
            this.schemaVersion = schemaVersion > 0 ? schemaVersion : 1;
        }

        internal string OwnerPackageId
        {
            get
            {
                return this.ownerPackageId;
            }
        }

        internal string RuntimeSystemKey
        {
            get
            {
                return this.runtimeSystemKey;
            }
        }

        internal int SchemaVersion
        {
            get
            {
                return this.schemaVersion;
            }
        }

        internal bool Initialized
        {
            get
            {
                return this.initialized;
            }
        }

        internal bool InitializationFailed
        {
            get
            {
                return this.initializationFailed;
            }
        }

        internal bool MigrationFailed
        {
            get
            {
                return this.migrationFailed;
            }
        }

        internal bool RuntimeExecutionFailed
        {
            get
            {
                return this.runtimeExecutionFailed;
            }
        }

        internal bool HasRuntimeFailure
        {
            get
            {
                return this.initializationFailed || this.migrationFailed || this.runtimeExecutionFailed;
            }
        }

        internal string LastFailureMessage
        {
            get
            {
                return this.lastFailureMessage;
            }
        }

        internal int LastFailureTick
        {
            get
            {
                return this.lastFailureTick;
            }
        }

        internal IReadOnlyDictionary<string, string> ValuesForDebug
        {
            get
            {
                this.EnsureInitialized();
                return new ReadOnlyDictionary<string, string>(
                    new Dictionary<string, string>(this.values));
            }
        }

        public void EnsureInitialized()
        {
            if (this.values == null)
            {
                this.values = new Dictionary<string, string>();
            }

            if (this.schemaVersion <= 0)
            {
                this.schemaVersion = 1;
            }
        }

        internal void EnsureIdentityDefaults(
            string ownerPackageId,
            string runtimeSystemKey)
        {
            this.EnsureInitialized();

            if (string.IsNullOrWhiteSpace(this.ownerPackageId))
            {
                this.ownerPackageId = NormalizeKey(ownerPackageId);
            }

            if (string.IsNullOrWhiteSpace(this.runtimeSystemKey))
            {
                this.runtimeSystemKey = NormalizeKey(runtimeSystemKey);
            }
        }

        internal void MarkInitialized()
        {
            this.initialized = true;
        }

        internal void MarkInitializationFailed(string reason, int ticksGame)
        {
            this.initializationFailed = true;
            this.RecordFailure(reason, ticksGame);
        }

        internal void MarkMigrationFailed(string reason, int ticksGame)
        {
            this.migrationFailed = true;
            this.RecordFailure(reason, ticksGame);
        }

        internal void MarkRuntimeExecutionFailed(string reason, int ticksGame)
        {
            this.runtimeExecutionFailed = true;
            this.RecordFailure(reason, ticksGame);
        }

        internal void SetSchemaVersion(int schemaVersion)
        {
            if (schemaVersion > 0)
            {
                this.schemaVersion = schemaVersion;
            }
        }

        internal void ClearRuntimeFailureForDevReset()
        {
            this.EnsureInitialized();
            this.initialized = false;
            this.initializationFailed = false;
            this.migrationFailed = false;
            this.runtimeExecutionFailed = false;
            this.lastFailureMessage = null;
            this.lastFailureTick = -1;
            this.values.Clear();
            this.InvalidatePowerDemandDispatchCache();
        }

        internal void RefreshPowerDemandDispatchCache(ShuttleExternalModuleInfo moduleInfo)
        {
            this.cachedPowerDemandModuleInfo = moduleInfo;
            this.cachedPowerDemandStateReader = moduleInfo != null
                ? new ExternalRuntimeStateReader(this)
                : null;
        }

        internal bool TryGetPowerDemandDispatchCache(
            out ShuttleExternalModuleInfo moduleInfo,
            out IShuttleExternalRuntimeStateReader stateReader)
        {
            moduleInfo = this.cachedPowerDemandModuleInfo;
            stateReader = this.cachedPowerDemandStateReader;
            return moduleInfo != null && stateReader != null;
        }

        internal void InvalidatePowerDemandDispatchCache()
        {
            this.cachedPowerDemandModuleInfo = null;
            this.cachedPowerDemandStateReader = null;
        }

        internal void DevSchemaReset(int schemaVersion)
        {
            this.ClearRuntimeFailureForDevReset();
            this.SetSchemaVersion(schemaVersion);
        }

        internal bool TryGetRaw(string key, out string value)
        {
            value = null;
            string normalizedKey = NormalizeKey(key);
            if (normalizedKey == null)
            {
                return false;
            }

            this.EnsureInitialized();
            return this.values.TryGetValue(normalizedKey, out value);
        }

        internal void SetRaw(string key, string value)
        {
            string failureReason;
            if (!this.TrySetRaw(key, value, out failureReason))
            {
                this.LogRejectedWriteOnce(key, failureReason);
            }
        }

        internal bool TrySetRaw(string key, string value, out string failureReason)
        {
            return this.TrySetRaw(key, value, false, out failureReason);
        }

        internal void SetSystemRaw(string key, string value)
        {
            string failureReason;
            if (!this.TrySetRaw(key, value, true, out failureReason))
            {
                this.LogRejectedWriteOnce(key, failureReason);
            }
        }

        private bool TrySetRaw(
            string key,
            string value,
            bool allowSystemKey,
            out string failureReason)
        {
            failureReason = null;
            string normalizedKey = NormalizeKey(key);
            if (normalizedKey == null)
            {
                failureReason = "key is empty";
                return false;
            }

            this.EnsureInitialized();
            if (!allowSystemKey &&
                ExternalRuntimeStateKeys.IsReservedKey(normalizedKey))
            {
                failureReason = "key is reserved for CeleTech Shuttle runtime metadata";
                return false;
            }

            if (value == null)
            {
                this.values.Remove(normalizedKey);
                return true;
            }

            if (!IsValidWriteKey(normalizedKey, out failureReason))
            {
                return false;
            }

            if (value.Length > MaxValueLength)
            {
                failureReason = "value length " + value.Length +
                    " exceeds max " + MaxValueLength;
                return false;
            }

            if (!this.values.ContainsKey(normalizedKey) &&
                this.CountUserValues() >= MaxValueCount &&
                !allowSystemKey)
            {
                failureReason = "user value count " + this.CountUserValues() +
                    " has reached max " + MaxValueCount;
                return false;
            }

            this.values[normalizedKey] = value;
            return true;
        }

        internal bool ContainsKey(string key)
        {
            string normalizedKey = NormalizeKey(key);
            if (normalizedKey == null)
            {
                return false;
            }

            this.EnsureInitialized();
            return this.values.ContainsKey(normalizedKey);
        }

        internal void Remove(string key)
        {
            string failureReason;
            if (!this.TryRemove(key, false, out failureReason))
            {
                this.LogRejectedWriteOnce(key, failureReason);
            }
        }

        internal bool TryRemove(string key, out string failureReason)
        {
            return this.TryRemove(key, false, out failureReason);
        }

        internal void RemoveSystemRaw(string key)
        {
            string failureReason;
            if (!this.TryRemove(key, true, out failureReason))
            {
                this.LogRejectedWriteOnce(key, failureReason);
            }
        }

        private bool TryRemove(
            string key,
            bool allowSystemKey,
            out string failureReason)
        {
            failureReason = null;
            string normalizedKey = NormalizeKey(key);
            if (normalizedKey == null)
            {
                failureReason = "key is empty";
                return false;
            }

            this.EnsureInitialized();
            if (!allowSystemKey &&
                ExternalRuntimeStateKeys.IsReservedKey(normalizedKey))
            {
                failureReason = "key is reserved for CeleTech Shuttle runtime metadata";
                return false;
            }

            this.values.Remove(normalizedKey);
            return true;
        }

        internal void Clear()
        {
            this.ClearUserValues();
        }

        internal void ClearUserValues()
        {
            this.EnsureInitialized();
            if (this.values.Count == 0)
            {
                return;
            }

            List<string> keysToRemove = new List<string>();
            foreach (string key in this.values.Keys)
            {
                if (!ExternalRuntimeStateKeys.IsReservedKey(key))
                {
                    keysToRemove.Add(key);
                }
            }

            for (int i = 0; i < keysToRemove.Count; i++)
            {
                this.values.Remove(keysToRemove[i]);
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.ownerPackageId, "ownerPackageId");
            Scribe_Values.Look(ref this.runtimeSystemKey, "runtimeSystemKey");
            Scribe_Values.Look(ref this.schemaVersion, "schemaVersion", 1);
            Scribe_Values.Look(ref this.initialized, "initialized", false);
            Scribe_Values.Look(ref this.initializationFailed, "initializationFailed", false);
            Scribe_Values.Look(ref this.migrationFailed, "migrationFailed", false);
            Scribe_Values.Look(ref this.runtimeExecutionFailed, "runtimeExecutionFailed", false);
            Scribe_Values.Look(ref this.lastFailureMessage, "lastFailureMessage");
            Scribe_Values.Look(ref this.lastFailureTick, "lastFailureTick", -1);
            Scribe_Collections.Look(
                ref this.values,
                "values",
                LookMode.Value,
                LookMode.Value);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.InvalidatePowerDemandDispatchCache();
                this.EnsureInitialized();
                this.SanitizeValuesAfterLoad();
            }
        }

        private void SanitizeValuesAfterLoad()
        {
            this.EnsureInitialized();
            if (this.values == null || this.values.Count == 0)
            {
                return;
            }

            int removedCount = 0;
            HashSet<string> seenKeys = new HashSet<string>();
            List<KeyValuePair<string, string>> systemValues =
                new List<KeyValuePair<string, string>>();
            List<KeyValuePair<string, string>> userValues =
                new List<KeyValuePair<string, string>>();

            foreach (KeyValuePair<string, string> pair in this.values)
            {
                string normalizedKey = NormalizeKey(pair.Key);
                string failureReason;
                if (normalizedKey == null ||
                    !IsValidWriteKey(normalizedKey, out failureReason) ||
                    pair.Value == null ||
                    pair.Value.Length > MaxValueLength ||
                    !seenKeys.Add(normalizedKey))
                {
                    removedCount++;
                    continue;
                }

                if (ExternalRuntimeStateKeys.IsReservedKey(normalizedKey))
                {
                    systemValues.Add(
                        new KeyValuePair<string, string>(normalizedKey, pair.Value));
                }
                else
                {
                    userValues.Add(
                        new KeyValuePair<string, string>(normalizedKey, pair.Value));
                }
            }

            systemValues.Sort(CompareSanitizedEntries);
            userValues.Sort(CompareSanitizedEntries);

            Dictionary<string, string> rebuiltValues = new Dictionary<string, string>();
            for (int i = 0; i < systemValues.Count; i++)
            {
                rebuiltValues.Add(systemValues[i].Key, systemValues[i].Value);
            }

            for (int i = 0; i < userValues.Count; i++)
            {
                if (i >= MaxValueCount)
                {
                    removedCount++;
                    continue;
                }

                rebuiltValues.Add(userValues[i].Key, userValues[i].Value);
            }

            this.values = rebuiltValues;
            if (removedCount > 0)
            {
                this.LogSanitizedValuesOnce(removedCount);
            }
        }

        private static int CompareSanitizedEntries(
            KeyValuePair<string, string> left,
            KeyValuePair<string, string> right)
        {
            return string.CompareOrdinal(left.Key, right.Key);
        }

        private static string NormalizeKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            return key.Trim();
        }

        private static bool IsValidWriteKey(string normalizedKey, out string failureReason)
        {
            failureReason = null;
            if (string.IsNullOrWhiteSpace(normalizedKey))
            {
                failureReason = "key is empty";
                return false;
            }

            if (normalizedKey.Length > MaxKeyLength)
            {
                failureReason = "key length " + normalizedKey.Length +
                    " exceeds max " + MaxKeyLength;
                return false;
            }

            for (int i = 0; i < normalizedKey.Length; i++)
            {
                if (char.IsControl(normalizedKey[i]))
                {
                    failureReason = "key contains control characters";
                    return false;
                }
            }

            return true;
        }

        private int CountUserValues()
        {
            this.EnsureInitialized();
            int count = 0;
            foreach (string key in this.values.Keys)
            {
                if (!ExternalRuntimeStateKeys.IsReservedKey(key))
                {
                    count++;
                }
            }

            return count;
        }

        private void RecordFailure(string reason, int ticksGame)
        {
            this.lastFailureMessage = SanitizeFailureMessage(reason);
            this.lastFailureTick = ticksGame;
        }

        private static string SanitizeFailureMessage(string reason)
        {
            if (string.IsNullOrEmpty(reason))
            {
                return null;
            }

            string trimmed = reason.Trim();
            return trimmed.Length <= MaxFailureMessageLength
                ? trimmed
                : trimmed.Substring(0, MaxFailureMessageLength);
        }

        private void LogRejectedWriteOnce(string key, string failureReason)
        {
            string normalizedKey = NormalizeKey(key) ?? "null";
            string message = ExternalShuttleRuntimeRegistry.LogPrefix +
                "state write rejected for runtime '" +
                (this.runtimeSystemKey ?? "unknown") +
                "', owner '" + (this.ownerPackageId ?? "unknown") +
                "', key '" + normalizedKey + "': " +
                (failureReason ?? "unknown reason") + ".";
            Log.WarningOnce(
                message,
                MakeWarningHash(
                    "external-state-write-rejected",
                    this.runtimeSystemKey,
                    this.ownerPackageId,
                    failureReason));
        }

        private void LogSanitizedValuesOnce(int removedCount)
        {
            Log.WarningOnce(
                ExternalShuttleRuntimeRegistry.LogPrefix +
                "dev-stage external runtime state sanitize removed " +
                removedCount + " invalid or excess value entries for runtime '" +
                (this.runtimeSystemKey ?? "unknown") +
                "', owner '" + (this.ownerPackageId ?? "unknown") + "'.",
                MakeWarningHash(
                    "external-state-sanitized",
                    this.runtimeSystemKey,
                    this.ownerPackageId,
                    removedCount.ToString()));
        }

        private static int MakeWarningHash(
            string prefix,
            string runtimeKey,
            string scopeKey,
            string reason)
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + (prefix != null ? prefix.GetHashCode() : 0);
                hash = (hash * 31) + (runtimeKey != null ? runtimeKey.GetHashCode() : 0);
                hash = (hash * 31) + (scopeKey != null ? scopeKey.GetHashCode() : 0);
                hash = (hash * 31) + (reason != null ? reason.GetHashCode() : 0);
                return hash;
            }
        }
    }
}
