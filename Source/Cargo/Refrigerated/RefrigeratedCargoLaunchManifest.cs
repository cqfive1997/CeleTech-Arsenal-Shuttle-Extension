using System.Collections.Generic;
using System.Text;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated
{
    internal static class RefrigeratedCargoLaunchManifestConstants
    {
        internal const int CurrentManifestVersion = 1;
        internal const string PhaseNone = "None";
        internal const string PhaseExported = "Exported";
        internal const string PhaseInFlight = "InFlight";
        internal const string PhaseRestored = "Restored";
        internal const string PhaseRolledBack = "RolledBack";
        internal const string PhaseNormalCargoFallback = "NormalCargoFallback";
        internal const string PhaseImpactFallback = "ImpactFallback";
        internal const string PhaseQuarantined = "Quarantined";
        internal const string PhaseFailed = "Failed";
    }

    internal enum RefrigeratedCargoRestoreStatus
    {
        AllResolved,
        Quarantined,
        FatalUnresolved
    }

    internal sealed class RefrigeratedCargoRestoreResult
    {
        internal RefrigeratedCargoRestoreResult(
            RefrigeratedCargoRestoreStatus status,
            string failureReason,
            string debugDump,
            bool anyFallback,
            bool anyQuarantined)
        {
            this.Status = status;
            this.FailureReason = failureReason;
            this.DebugDump = debugDump;
            this.AnyFallback = anyFallback;
            this.AnyQuarantined = anyQuarantined;
        }

        internal RefrigeratedCargoRestoreStatus Status { get; private set; }

        internal string FailureReason { get; private set; }

        internal string DebugDump { get; private set; }

        internal bool AnyFallback { get; private set; }

        internal bool AnyQuarantined { get; private set; }

        internal bool AllowsImpact
        {
            get
            {
                return this.Status != RefrigeratedCargoRestoreStatus.FatalUnresolved;
            }
        }
    }

    internal sealed class RefrigeratedCargoLaunchManifest : IExposable
    {
        internal int ManifestVersion = RefrigeratedCargoLaunchManifestConstants.CurrentManifestVersion;
        internal List<RefrigeratedCargoLaunchManifestEntry> Entries =
            new List<RefrigeratedCargoLaunchManifestEntry>();

        internal bool HasEntries
        {
            get
            {
                this.EnsureInitialized();
                return this.Entries.Count > 0;
            }
        }

        internal int EntryCount
        {
            get
            {
                this.EnsureInitialized();
                return this.Entries.Count;
            }
        }

        internal RefrigeratedCargoLaunchManifestEntry AddExportEntry(
            string moduleInstanceID,
            string sourceModuleDefName,
            Thing thing,
            int originalStackCount,
            float massKg,
            float sourceTemperatureC,
            float targetTemperatureC,
            int exportTick,
            string phase,
            string debugNotes)
        {
            this.EnsureInitialized();
            RefrigeratedCargoLaunchManifestEntry entry =
                new RefrigeratedCargoLaunchManifestEntry
                {
                    ManifestVersion = this.ManifestVersion,
                    ModuleInstanceID = moduleInstanceID,
                    SourceModuleDefName = sourceModuleDefName,
                    ThingIDNumber = thing != null ? thing.thingIDNumber : -1,
                    ThingDefName = thing != null && thing.def != null ? thing.def.defName : null,
                    Label = thing != null ? thing.LabelCapNoCount : null,
                    OriginalStackCount = originalStackCount,
                    MassKg = massKg,
                    SourceTemperatureC = sourceTemperatureC,
                    TargetTemperatureC = targetTemperatureC,
                    ExportTick = exportTick,
                    TransferPhase = phase,
                    DebugNotes = debugNotes
                };
            entry.EnsureInitialized();
            this.Entries.Add(entry);
            return entry;
        }

        internal RefrigeratedCargoLaunchManifestEntry FindEntry(
            string moduleInstanceID,
            int thingIDNumber)
        {
            this.EnsureInitialized();
            if (string.IsNullOrEmpty(moduleInstanceID) || thingIDNumber <= 0)
            {
                return null;
            }

            for (int i = 0; i < this.Entries.Count; i++)
            {
                RefrigeratedCargoLaunchManifestEntry entry = this.Entries[i];
                if (entry != null &&
                    entry.ModuleInstanceID == moduleInstanceID &&
                    entry.ThingIDNumber == thingIDNumber)
                {
                    return entry;
                }
            }

            return null;
        }

        internal List<RefrigeratedCargoLaunchManifestEntry> FindEntriesByModule(
            string moduleInstanceID)
        {
            this.EnsureInitialized();
            List<RefrigeratedCargoLaunchManifestEntry> result =
                new List<RefrigeratedCargoLaunchManifestEntry>();
            if (string.IsNullOrEmpty(moduleInstanceID))
            {
                return result;
            }

            for (int i = 0; i < this.Entries.Count; i++)
            {
                RefrigeratedCargoLaunchManifestEntry entry = this.Entries[i];
                if (entry != null && entry.ModuleInstanceID == moduleInstanceID)
                {
                    result.Add(entry);
                }
            }

            return result;
        }

        internal void Clear()
        {
            this.EnsureInitialized();
            this.Entries.Clear();
            this.ManifestVersion = RefrigeratedCargoLaunchManifestConstants.CurrentManifestVersion;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.ManifestVersion, "manifestVersion", RefrigeratedCargoLaunchManifestConstants.CurrentManifestVersion);
            Scribe_Collections.Look(
                ref this.Entries,
                "entries",
                LookMode.Deep,
                System.Array.Empty<object>());

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.EnsureInitialized();
            }
        }

        internal void EnsureInitialized()
        {
            if (this.ManifestVersion <= 0)
            {
                this.ManifestVersion = RefrigeratedCargoLaunchManifestConstants.CurrentManifestVersion;
            }

            if (this.Entries == null)
            {
                this.Entries = new List<RefrigeratedCargoLaunchManifestEntry>();
            }

            for (int i = 0; i < this.Entries.Count; i++)
            {
                RefrigeratedCargoLaunchManifestEntry entry = this.Entries[i];
                if (entry != null)
                {
                    entry.EnsureInitialized();
                }
            }
        }

        internal string DumpForDebug()
        {
            this.EnsureInitialized();
            StringBuilder builder = new StringBuilder();
            builder.Append("refrigeratedManifestVersion=");
            builder.Append(this.ManifestVersion);
            builder.Append(" entries=");
            builder.Append(this.Entries.Count);
            for (int i = 0; i < this.Entries.Count; i++)
            {
                builder.AppendLine();
                builder.Append("  [");
                builder.Append(i);
                builder.Append("] ");
                builder.Append(this.Entries[i] != null ? this.Entries[i].DumpForDebug() : "null");
            }

            return builder.ToString();
        }
    }

    internal sealed class RefrigeratedCargoLaunchManifestEntry : IExposable
    {
        internal int ManifestVersion = RefrigeratedCargoLaunchManifestConstants.CurrentManifestVersion;
        internal string ModuleInstanceID;
        internal string SourceModuleDefName;
        internal int ThingIDNumber = -1;
        internal string ThingDefName;
        internal string Label;
        internal int OriginalStackCount;
        internal float MassKg;
        internal float SourceTemperatureC;
        internal float TargetTemperatureC;
        internal int ExportTick = -1;
        internal string TransferPhase = RefrigeratedCargoLaunchManifestConstants.PhaseNone;
        internal string DebugNotes;

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.ManifestVersion, "manifestVersion", RefrigeratedCargoLaunchManifestConstants.CurrentManifestVersion);
            Scribe_Values.Look(ref this.ModuleInstanceID, "moduleInstanceID", null);
            Scribe_Values.Look(ref this.SourceModuleDefName, "sourceModuleDefName", null);
            Scribe_Values.Look(ref this.ThingIDNumber, "thingIDNumber", -1);
            Scribe_Values.Look(ref this.ThingDefName, "thingDefName", null);
            Scribe_Values.Look(ref this.Label, "label", null);
            Scribe_Values.Look(ref this.OriginalStackCount, "originalStackCount", 0);
            Scribe_Values.Look(ref this.MassKg, "massKg", 0f);
            Scribe_Values.Look(ref this.SourceTemperatureC, "sourceTemperatureC", 0f);
            Scribe_Values.Look(ref this.TargetTemperatureC, "targetTemperatureC", 0f);
            Scribe_Values.Look(ref this.ExportTick, "exportTick", -1);
            Scribe_Values.Look(ref this.TransferPhase, "transferPhase", RefrigeratedCargoLaunchManifestConstants.PhaseNone);
            Scribe_Values.Look(ref this.DebugNotes, "debugNotes", null);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.EnsureInitialized();
            }
        }

        internal void EnsureInitialized()
        {
            if (this.ManifestVersion <= 0)
            {
                this.ManifestVersion = RefrigeratedCargoLaunchManifestConstants.CurrentManifestVersion;
            }

            if (this.ThingIDNumber < -1)
            {
                this.ThingIDNumber = -1;
            }

            if (this.OriginalStackCount < 0)
            {
                this.OriginalStackCount = 0;
            }

            if (this.ExportTick < -1)
            {
                this.ExportTick = -1;
            }

            if (string.IsNullOrEmpty(this.TransferPhase))
            {
                this.TransferPhase = RefrigeratedCargoLaunchManifestConstants.PhaseNone;
            }
        }

        internal string DumpForDebug()
        {
            this.EnsureInitialized();
            return "moduleInstanceID=" + (this.ModuleInstanceID ?? "null") +
                " sourceModuleDefName=" + (this.SourceModuleDefName ?? "null") +
                " thingID=" + this.ThingIDNumber +
                " thingDefName=" + (this.ThingDefName ?? "null") +
                " label=" + (this.Label ?? "null") +
                " stackCount=" + this.OriginalStackCount +
                " massKg=" + this.MassKg.ToString("0.###") +
                " sourceTemperatureC=" + this.SourceTemperatureC.ToString("0.###") +
                " targetTemperatureC=" + this.TargetTemperatureC.ToString("0.###") +
                " exportTick=" + this.ExportTick +
                " phase=" + (this.TransferPhase ?? "null") +
                " notes=" + (this.DebugNotes ?? "null");
        }
    }
}
