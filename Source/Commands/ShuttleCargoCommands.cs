using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Commands
{
    /// <summary>
    /// Starts the vanilla transporter loading flow from selections made in the custom UI.
    /// The command carries selected Transferables, but the handler resolves host comps and side effects.
    /// </summary>
    public sealed class BeginLoadCargoCommand : IShuttleCommand
    {
        public const string ID = "begin-load-cargo";

        public BeginLoadCargoCommand(
            List<TransferableOneWay> passengerTransferables,
            List<TransferableOneWay> cargoTransferables,
            bool replaceExistingQueue)
        {
            this.PassengerTransferables = CopyTransferables(passengerTransferables);
            this.CargoTransferables = CopyTransferables(cargoTransferables);
            this.ReplaceExistingQueue = replaceExistingQueue;
        }

        public string CommandID
        {
            get
            {
                return ID;
            }
        }

        public List<TransferableOneWay> PassengerTransferables { get; private set; }
        public List<TransferableOneWay> CargoTransferables { get; private set; }
        public bool ReplaceExistingQueue { get; private set; }

        private static List<TransferableOneWay> CopyTransferables(List<TransferableOneWay> source)
        {
            List<TransferableOneWay> result = new List<TransferableOneWay>();
            if (source == null)
            {
                return result;
            }

            for (int i = 0; i < source.Count; i++)
            {
                TransferableOneWay copy = CopyTransferable(source[i]);
                if (copy != null)
                {
                    result.Add(copy);
                }
            }

            return result;
        }

        private static TransferableOneWay CopyTransferable(TransferableOneWay source)
        {
            if (source == null)
            {
                return null;
            }

            TransferableOneWay copy = new TransferableOneWay();
            if (source.things != null)
            {
                copy.things.AddRange(source.things);
            }

            if (source.CountToTransfer > 0)
            {
                copy.AdjustTo(source.CountToTransfer);
            }

            return copy;
        }
    }

    /// <summary>
    /// Clears all pending cargo backend load entries for the shuttle.
    /// </summary>
    public sealed class ClearQueuedLoadCommand : IShuttleCommand
    {
        public const string ID = "clear-queued-load";

        public string CommandID
        {
            get
            {
                return ID;
            }
        }
    }

    /// <summary>
    /// Cancels all or part of one queued load row identified by a snapshot index pair.
    /// </summary>
    public sealed class CancelQueuedLoadEntryCommand : IShuttleCommand
    {
        public const string ID = "cancel-queued-load-entry";

        public CancelQueuedLoadEntryCommand(int transporterIndex, int queueIndex, int count)
        {
            this.TransporterIndex = transporterIndex;
            this.QueueIndex = queueIndex;
            this.Count = count;
        }

        public string CommandID
        {
            get
            {
                return ID;
            }
        }

        public int TransporterIndex { get; private set; }
        public int QueueIndex { get; private set; }
        public int Count { get; private set; }
    }

    /// <summary>
    /// Unloads one already loaded transporter row back near the shuttle.
    /// Runtime cargo truth stays in the cargo backend; this command only names
    /// the snapshot row the backend should resolve and drop.
    /// </summary>
    public sealed class UnloadLoadedCargoEntryCommand : IShuttleCommand
    {
        public const string ID = "unload-loaded-cargo-entry";

        public UnloadLoadedCargoEntryCommand(
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

        public string CommandID
        {
            get
            {
                return ID;
            }
        }

        public int TransporterIndex { get; private set; }
        public int LoadedIndex { get; private set; }
        public int ThingIDNumber { get; private set; }
        public string DefName { get; private set; }
        public int Count { get; private set; }
    }

    /// <summary>
    /// Unloads a set of already loaded transporter rows from one displayed cargo bay.
    /// The command carries snapshot identities only; the cargo backend re-resolves
    /// actual holder contents before moving anything.
    /// </summary>
    public sealed class UnloadLoadedCargoBayCommand : IShuttleCommand
    {
        public const string ID = "unload-loaded-cargo-bay";

        public UnloadLoadedCargoBayCommand(List<UnloadLoadedCargoBayEntry> entries)
        {
            this.Entries = CopyEntries(entries);
        }

        public string CommandID
        {
            get
            {
                return ID;
            }
        }

        public List<UnloadLoadedCargoBayEntry> Entries { get; private set; }

        private static List<UnloadLoadedCargoBayEntry> CopyEntries(
            List<UnloadLoadedCargoBayEntry> source)
        {
            List<UnloadLoadedCargoBayEntry> result =
                new List<UnloadLoadedCargoBayEntry>();
            if (source == null)
            {
                return result;
            }

            for (int i = 0; i < source.Count; i++)
            {
                UnloadLoadedCargoBayEntry entry = source[i];
                if (entry != null)
                {
                    result.Add(new UnloadLoadedCargoBayEntry(
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

    public sealed class UnloadLoadedCargoBayEntry
    {
        public UnloadLoadedCargoBayEntry(
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

    /// <summary>
    /// Writes edited cargo-region filter settings back into durable assembly configuration.
    /// </summary>
    public sealed class UpdateCargoRegionSettingsCommand : IShuttleCommand
    {
        public const string ID = "update-cargo-region-settings";

        public UpdateCargoRegionSettingsCommand(
            int regionIndex,
            string label,
            bool allowHumans,
            bool allowAnimals,
            bool allowMechs,
            ThingFilter itemFilter)
        {
            this.RegionIndex = regionIndex;
            this.Label = label;
            this.AllowHumans = allowHumans;
            this.AllowAnimals = allowAnimals;
            this.AllowMechs = allowMechs;
            this.ItemFilter = CopyFilter(itemFilter);
        }

        public string CommandID
        {
            get
            {
                return ID;
            }
        }

        public int RegionIndex { get; private set; }
        public string Label { get; private set; }
        public bool AllowHumans { get; private set; }
        public bool AllowAnimals { get; private set; }
        public bool AllowMechs { get; private set; }
        public ThingFilter ItemFilter { get; private set; }

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
}
