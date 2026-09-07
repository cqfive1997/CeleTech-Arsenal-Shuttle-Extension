using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Commands
{
    public sealed class TransferLoadedCargoToRefrigeratedCargoCommand : IShuttleCommand
    {
        public const string ID = "transfer-loaded-cargo-to-refrigerated-cargo";

        public TransferLoadedCargoToRefrigeratedCargoCommand(
            string moduleInstanceID,
            int transporterIndex,
            int loadedIndex,
            int thingIDNumber,
            string defName,
            int count)
        {
            this.ModuleInstanceID = moduleInstanceID;
            this.TransporterIndex = transporterIndex;
            this.LoadedIndex = loadedIndex;
            this.ThingIDNumber = thingIDNumber;
            this.DefName = defName;
            this.Count = count;
        }

        public string CommandID
        {
            get
            {
                return ID;
            }
        }

        public string ModuleInstanceID { get; private set; }
        public int TransporterIndex { get; private set; }
        public int LoadedIndex { get; private set; }
        public int ThingIDNumber { get; private set; }
        public string DefName { get; private set; }
        public int Count { get; private set; }
    }

    public sealed class TransferRefrigeratedCargoToLoadedCargoCommand : IShuttleCommand
    {
        public const string ID = "transfer-refrigerated-cargo-to-loaded-cargo";

        public TransferRefrigeratedCargoToLoadedCargoCommand(
            string moduleInstanceID,
            int coldIndex,
            int thingIDNumber,
            string defName,
            int count)
        {
            this.ModuleInstanceID = moduleInstanceID;
            this.ColdIndex = coldIndex;
            this.ThingIDNumber = thingIDNumber;
            this.DefName = defName;
            this.Count = count;
        }

        public string CommandID
        {
            get
            {
                return ID;
            }
        }

        public string ModuleInstanceID { get; private set; }
        public int ColdIndex { get; private set; }
        public int ThingIDNumber { get; private set; }
        public string DefName { get; private set; }
        public int Count { get; private set; }
    }

    public sealed class TransferLoadedCargoGroupToRefrigeratedCargoCommand : IShuttleCommand
    {
        public const string ID = "transfer-loaded-cargo-group-to-refrigerated-cargo";

        public TransferLoadedCargoGroupToRefrigeratedCargoCommand(
            string moduleInstanceID,
            List<TransferLoadedCargoGroupEntry> entries)
        {
            this.ModuleInstanceID = moduleInstanceID;
            this.Entries = CopyEntries(entries);
        }

        public string CommandID
        {
            get
            {
                return ID;
            }
        }

        public string ModuleInstanceID { get; private set; }
        public List<TransferLoadedCargoGroupEntry> Entries { get; private set; }

        private static List<TransferLoadedCargoGroupEntry> CopyEntries(
            List<TransferLoadedCargoGroupEntry> source)
        {
            List<TransferLoadedCargoGroupEntry> result =
                new List<TransferLoadedCargoGroupEntry>();
            for (int i = 0; source != null && i < source.Count; i++)
            {
                TransferLoadedCargoGroupEntry entry = source[i];
                if (entry != null)
                {
                    result.Add(new TransferLoadedCargoGroupEntry(
                        entry.TransporterIndex,
                        entry.LoadedIndex,
                        entry.ThingIDNumber,
                        entry.DefName,
                        entry.Count));
                }
            }

            return result;
        }
    }

    public sealed class TransferLoadedCargoGroupEntry
    {
        public TransferLoadedCargoGroupEntry(
            int transporterIndex,
            int loadedIndex,
            int thingIDNumber,
            string defName,
            int count)
        {
            this.TransporterIndex = transporterIndex;
            this.LoadedIndex = loadedIndex;
            this.ThingIDNumber = thingIDNumber;
            this.DefName = defName;
            this.Count = count;
        }

        public int TransporterIndex { get; private set; }
        public int LoadedIndex { get; private set; }
        public int ThingIDNumber { get; private set; }
        public string DefName { get; private set; }
        public int Count { get; private set; }
    }

    public sealed class TransferRefrigeratedCargoGroupToLoadedCargoCommand : IShuttleCommand
    {
        public const string ID = "transfer-refrigerated-cargo-group-to-loaded-cargo";

        public TransferRefrigeratedCargoGroupToLoadedCargoCommand(
            string moduleInstanceID,
            List<TransferRefrigeratedCargoGroupEntry> entries)
        {
            this.ModuleInstanceID = moduleInstanceID;
            this.Entries = CopyEntries(entries);
        }

        public string CommandID
        {
            get
            {
                return ID;
            }
        }

        public string ModuleInstanceID { get; private set; }
        public List<TransferRefrigeratedCargoGroupEntry> Entries { get; private set; }

        private static List<TransferRefrigeratedCargoGroupEntry> CopyEntries(
            List<TransferRefrigeratedCargoGroupEntry> source)
        {
            List<TransferRefrigeratedCargoGroupEntry> result =
                new List<TransferRefrigeratedCargoGroupEntry>();
            for (int i = 0; source != null && i < source.Count; i++)
            {
                TransferRefrigeratedCargoGroupEntry entry = source[i];
                if (entry != null)
                {
                    result.Add(new TransferRefrigeratedCargoGroupEntry(
                        entry.ColdIndex,
                        entry.ThingIDNumber,
                        entry.DefName,
                        entry.Count));
                }
            }

            return result;
        }
    }

    public sealed class TransferRefrigeratedCargoGroupEntry
    {
        public TransferRefrigeratedCargoGroupEntry(
            int coldIndex,
            int thingIDNumber,
            string defName,
            int count)
        {
            this.ColdIndex = coldIndex;
            this.ThingIDNumber = thingIDNumber;
            this.DefName = defName;
            this.Count = count;
        }

        public int ColdIndex { get; private set; }
        public int ThingIDNumber { get; private set; }
        public string DefName { get; private set; }
        public int Count { get; private set; }
    }

    public sealed class UnloadRefrigeratedCargoBayCommand : IShuttleCommand
    {
        public const string ID = "unload-refrigerated-cargo-bay";

        public UnloadRefrigeratedCargoBayCommand(
            string moduleInstanceID,
            List<UnloadRefrigeratedCargoBayEntry> entries)
        {
            this.ModuleInstanceID = moduleInstanceID;
            this.Entries = CopyEntries(entries);
        }

        public string CommandID
        {
            get
            {
                return ID;
            }
        }

        public string ModuleInstanceID { get; private set; }
        public List<UnloadRefrigeratedCargoBayEntry> Entries { get; private set; }

        private static List<UnloadRefrigeratedCargoBayEntry> CopyEntries(
            List<UnloadRefrigeratedCargoBayEntry> source)
        {
            List<UnloadRefrigeratedCargoBayEntry> result =
                new List<UnloadRefrigeratedCargoBayEntry>();
            if (source == null)
            {
                return result;
            }

            for (int i = 0; i < source.Count; i++)
            {
                UnloadRefrigeratedCargoBayEntry entry = source[i];
                if (entry != null)
                {
                    result.Add(new UnloadRefrigeratedCargoBayEntry(
                        entry.ColdIndex,
                        entry.ThingIDNumber,
                        entry.DefName,
                        entry.Count));
                }
            }

            return result;
        }
    }

    public sealed class UnloadRefrigeratedCargoBayEntry
    {
        public UnloadRefrigeratedCargoBayEntry(
            int coldIndex,
            int thingIDNumber,
            string defName,
            int count)
        {
            this.ColdIndex = coldIndex;
            this.ThingIDNumber = thingIDNumber;
            this.DefName = defName;
            this.Count = count;
        }

        public int ColdIndex { get; private set; }
        public int ThingIDNumber { get; private set; }
        public string DefName { get; private set; }
        public int Count { get; private set; }
    }

    public sealed class SetRefrigeratedCargoAutoTransferEnabledCommand : IShuttleCommand
    {
        public const string ID = "set-refrigerated-cargo-auto-transfer-enabled";

        public SetRefrigeratedCargoAutoTransferEnabledCommand(
            string moduleInstanceID,
            bool enabled)
        {
            this.ModuleInstanceID = moduleInstanceID;
            this.Enabled = enabled;
        }

        public string CommandID
        {
            get
            {
                return ID;
            }
        }

        public string ModuleInstanceID { get; private set; }
        public bool Enabled { get; private set; }
    }

    public sealed class SetRefrigeratedCargoAutoTransferFilterCommand : IShuttleCommand
    {
        public const string ID = "set-refrigerated-cargo-auto-transfer-filter";

        public SetRefrigeratedCargoAutoTransferFilterCommand(
            string moduleInstanceID,
            ThingFilter autoTransferFilter)
        {
            this.ModuleInstanceID = moduleInstanceID;
            this.AutoTransferFilter = CopyFilter(autoTransferFilter);
        }

        public string CommandID
        {
            get
            {
                return ID;
            }
        }

        public string ModuleInstanceID { get; private set; }
        public ThingFilter AutoTransferFilter { get; private set; }

        private static ThingFilter CopyFilter(ThingFilter source)
        {
            ThingFilter copy = ThingFilter.CreateOnlyEverStorableThingFilter();
            if (source != null)
            {
                copy.CopyAllowancesFrom(source);
            }

            return copy;
        }
    }

    public sealed class ClearRefrigeratedCargoAutoTransferFilterCommand : IShuttleCommand
    {
        public const string ID = "clear-refrigerated-cargo-auto-transfer-filter";

        public ClearRefrigeratedCargoAutoTransferFilterCommand(string moduleInstanceID)
        {
            this.ModuleInstanceID = moduleInstanceID;
        }

        public string CommandID
        {
            get
            {
                return ID;
            }
        }

        public string ModuleInstanceID { get; private set; }
    }

    public sealed class SetRefrigeratedCargoLabelCommand : IShuttleCommand
    {
        public const string ID = "set-refrigerated-cargo-label";

        public SetRefrigeratedCargoLabelCommand(string moduleInstanceID, string label)
        {
            this.ModuleInstanceID = moduleInstanceID;
            this.Label = label;
        }

        public string CommandID
        {
            get
            {
                return ID;
            }
        }

        public string ModuleInstanceID { get; private set; }
        public string Label { get; private set; }
    }
}
