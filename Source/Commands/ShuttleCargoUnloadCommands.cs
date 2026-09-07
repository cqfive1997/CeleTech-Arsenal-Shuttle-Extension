using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.CargoUnloading;

namespace CeleTech.ShuttleExtension.ModularShuttle.Commands
{
    internal sealed class BeginCargoUnloadCommand : IShuttleCommand
    {
        internal const string ID = "begin-cargo-unload";

        internal BeginCargoUnloadCommand(
            IList<ShuttleCargoUnloadIntent> entries)
        {
            this.Entries = CopyEntries(entries);
        }

        public string CommandID { get { return ID; } }

        internal List<ShuttleCargoUnloadIntent> Entries { get; private set; }

        private static List<ShuttleCargoUnloadIntent> CopyEntries(
            IList<ShuttleCargoUnloadIntent> source)
        {
            List<ShuttleCargoUnloadIntent> result =
                new List<ShuttleCargoUnloadIntent>();
            for (int i = 0; source != null && i < source.Count; i++)
            {
                ShuttleCargoUnloadIntent entry = source[i];
                if (entry != null)
                {
                    result.Add(entry.Copy());
                }
            }

            return result;
        }
    }

    internal sealed class CancelCargoUnloadCommand : IShuttleCommand
    {
        internal const string ID = "cancel-cargo-unload";

        public string CommandID { get { return ID; } }
    }

    internal sealed class ShuttleCargoUnloadIntent
    {
        internal ShuttleCargoUnloadIntent(
            ShuttleCargoUnloadSourceKind sourceKind,
            int transporterIndex,
            int loadedIndex,
            string moduleInstanceID,
            int coldIndex,
            int thingIDNumber,
            string expectedDefName,
            int count)
        {
            this.SourceKind = sourceKind;
            this.TransporterIndex = transporterIndex;
            this.LoadedIndex = loadedIndex;
            this.ModuleInstanceID = moduleInstanceID;
            this.ColdIndex = coldIndex;
            this.ThingIDNumber = thingIDNumber;
            this.ExpectedDefName = expectedDefName;
            this.Count = count;
        }

        internal ShuttleCargoUnloadSourceKind SourceKind { get; private set; }
        internal int TransporterIndex { get; private set; }
        internal int LoadedIndex { get; private set; }
        internal string ModuleInstanceID { get; private set; }
        internal int ColdIndex { get; private set; }
        internal int ThingIDNumber { get; private set; }
        internal string ExpectedDefName { get; private set; }
        internal int Count { get; private set; }

        internal ShuttleCargoUnloadIntent Copy()
        {
            return new ShuttleCargoUnloadIntent(
                this.SourceKind,
                this.TransporterIndex,
                this.LoadedIndex,
                this.ModuleInstanceID,
                this.ColdIndex,
                this.ThingIDNumber,
                this.ExpectedDefName,
                this.Count);
        }
    }
}
