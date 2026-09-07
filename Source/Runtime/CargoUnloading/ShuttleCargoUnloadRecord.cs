using System;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.CargoUnloading
{
    public sealed class ShuttleCargoUnloadRecord : IExposable
    {
        private ShuttleCargoUnloadSourceKind sourceKind;
        private int transporterIndex = -1;
        private int loadedIndex = -1;
        private string moduleInstanceID;
        private int coldIndex = -1;
        private int thingIDNumber;
        private string expectedDefName;
        private int count;

        public ShuttleCargoUnloadRecord()
        {
        }

        public ShuttleCargoUnloadRecord(
            ShuttleCargoUnloadSourceKind sourceKind,
            int transporterIndex,
            int loadedIndex,
            string moduleInstanceID,
            int coldIndex,
            int thingIDNumber,
            string expectedDefName,
            int count)
        {
            this.sourceKind = sourceKind;
            this.transporterIndex = transporterIndex;
            this.loadedIndex = loadedIndex;
            this.moduleInstanceID = moduleInstanceID;
            this.coldIndex = coldIndex;
            this.thingIDNumber = thingIDNumber;
            this.expectedDefName = expectedDefName;
            this.count = count;
        }

        public ShuttleCargoUnloadSourceKind SourceKind
        {
            get { return this.sourceKind; }
        }

        public int TransporterIndex
        {
            get { return this.transporterIndex; }
        }

        public int LoadedIndex
        {
            get { return this.loadedIndex; }
        }

        public string ModuleInstanceID
        {
            get { return this.moduleInstanceID; }
        }

        public int ColdIndex
        {
            get { return this.coldIndex; }
        }

        public int ThingIDNumber
        {
            get { return this.thingIDNumber; }
        }

        public string ExpectedDefName
        {
            get { return this.expectedDefName; }
        }

        public int Count
        {
            get { return this.count; }
        }

        public bool IsValid
        {
            get
            {
                if (this.thingIDNumber <= 0 ||
                    string.IsNullOrEmpty(this.expectedDefName) ||
                    this.count <= 0)
                {
                    return false;
                }

                if (this.sourceKind == ShuttleCargoUnloadSourceKind.LoadedCargo)
                {
                    return this.transporterIndex >= 0 && this.loadedIndex >= 0;
                }

                if (this.sourceKind == ShuttleCargoUnloadSourceKind.RefrigeratedCargo)
                {
                    return !string.IsNullOrEmpty(this.moduleInstanceID) &&
                        this.coldIndex >= 0;
                }

                return false;
            }
        }

        public ShuttleCargoUnloadRecord Copy()
        {
            return new ShuttleCargoUnloadRecord(
                this.sourceKind,
                this.transporterIndex,
                this.loadedIndex,
                this.moduleInstanceID,
                this.coldIndex,
                this.thingIDNumber,
                this.expectedDefName,
                this.count);
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.sourceKind, "sourceKind", ShuttleCargoUnloadSourceKind.Unknown);
            Scribe_Values.Look(ref this.transporterIndex, "transporterIndex", -1);
            Scribe_Values.Look(ref this.loadedIndex, "loadedIndex", -1);
            Scribe_Values.Look(ref this.moduleInstanceID, "moduleInstanceID");
            Scribe_Values.Look(ref this.coldIndex, "coldIndex", -1);
            Scribe_Values.Look(ref this.thingIDNumber, "thingIDNumber", 0);
            Scribe_Values.Look(ref this.expectedDefName, "expectedDefName");
            Scribe_Values.Look(ref this.count, "count", 0);
        }
    }
}
