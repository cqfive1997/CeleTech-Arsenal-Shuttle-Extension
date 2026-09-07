using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    public sealed class CompProperties_ShuttleRefrigeratedCargoRegistry : CompProperties
    {
        public CompProperties_ShuttleRefrigeratedCargoRegistry()
        {
            this.compClass = typeof(CompShuttleRefrigeratedCargoRegistry);
        }
    }

    public sealed class CompShuttleRefrigeratedCargoRegistry : ThingComp, IThingHolder
    {
        private const int CurrentSaveVersion = 1;

        private int saveVersion = CurrentSaveVersion;
        private int inventoryProjectionRevision;
        private ThingOwner<Thing> directlyHeldThings;
        private List<RefrigeratedCargoRecord> records = new List<RefrigeratedCargoRecord>();

        public CompShuttleRefrigeratedCargoRegistry()
        {
            this.directlyHeldThings = new ThingOwner<Thing>(this, false, LookMode.Deep, true);
        }

        internal IReadOnlyList<RefrigeratedCargoRecord> Records
        {
            get
            {
                this.EnsureInitialized();
                return this.records;
            }
        }

        internal int RecordCount
        {
            get
            {
                this.EnsureInitialized();
                return this.records.Count;
            }
        }

        internal int InventoryProjectionRevision
        {
            get { return this.inventoryProjectionRevision; }
        }

        internal void Reconcile(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState)
        {
            this.EnsureInitialized();
            int previousProjectionSignature = this.BuildInventoryProjectionSignature();
            this.MarkAllRecordsOrphaned();

            if (assemblyState != null && assemblyState.Modules != null)
            {
                for (int i = 0; i < assemblyState.Modules.Count; i++)
                {
                    ShuttleModule module = assemblyState.Modules[i];
                    ShuttleRefrigeratedCargoModuleDef moduleDef =
                        module != null ? module.ModuleDef as ShuttleRefrigeratedCargoModuleDef : null;
                    if (module == null ||
                        moduleDef == null ||
                        string.IsNullOrEmpty(module.ModuleInstanceID))
                    {
                        continue;
                    }

                    this.GetOrCreateRecordForInstalledModule(module, moduleDef, runtimeState);
                }
            }

            this.RemoveEmptyOrphanedRecords(assemblyState);
            if (previousProjectionSignature != this.BuildInventoryProjectionSignature())
            {
                this.inventoryProjectionRevision++;
            }
        }

        internal bool TryGetRecord(string moduleInstanceID, out RefrigeratedCargoRecord record)
        {
            record = null;
            if (string.IsNullOrEmpty(moduleInstanceID))
            {
                return false;
            }

            this.EnsureInitialized();
            for (int i = 0; i < this.records.Count; i++)
            {
                RefrigeratedCargoRecord candidate = this.records[i];
                if (candidate != null && candidate.ModuleInstanceID == moduleInstanceID)
                {
                    record = candidate;
                    return true;
                }
            }

            return false;
        }

        internal bool HasColdCargo(string moduleInstanceID)
        {
            RefrigeratedCargoRecord record;
            return this.TryGetRecord(moduleInstanceID, out record) &&
                record != null &&
                record.HasContents;
        }

        internal bool TryGetAnyColdCargo(out RefrigeratedCargoCoolingStatus status)
        {
            status = null;
            this.EnsureInitialized();

            for (int i = 0; i < this.records.Count; i++)
            {
                RefrigeratedCargoRecord record = this.records[i];
                if (record == null || !record.HasContents)
                {
                    continue;
                }

                status = this.BuildCoolingStatus(record);
                return true;
            }

            return false;
        }

        internal float GetColdCargoMassKg(string moduleInstanceID)
        {
            RefrigeratedCargoRecord record;
            if (!this.TryGetRecord(moduleInstanceID, out record) || record == null)
            {
                return 0f;
            }

            return record.StoredMassKg;
        }

        internal bool TryGetCoolingStatus(
            string moduleInstanceID,
            out RefrigeratedCargoCoolingStatus status)
        {
            status = null;

            RefrigeratedCargoRecord record;
            if (!this.TryGetRecord(moduleInstanceID, out record) || record == null)
            {
                return false;
            }

            status = this.BuildCoolingStatus(record);
            return true;
        }

        internal bool CanRemoveModule(ShuttleModule module, out string failureReason)
        {
            failureReason = null;

            ShuttleRefrigeratedCargoModuleDef moduleDef =
                module != null ? module.ModuleDef as ShuttleRefrigeratedCargoModuleDef : null;
            if (module == null ||
                moduleDef == null ||
                !moduleDef.blockRemovalWhenColdCargoNotEmpty)
            {
                return true;
            }

            RefrigeratedCargoCoolingStatus status;
            if (!this.TryGetCoolingStatus(module.ModuleInstanceID, out status) ||
                status == null ||
                !status.HasColdCargo)
            {
                return true;
            }

            failureReason = "CT_Shuttle_Command_CannotRemoveRefrigeratedCargoModuleWithColdCargo"
                .Translate(
                    this.GetModuleLabel(moduleDef, status.ModuleDefName),
                    status.ModuleInstanceID,
                    status.StackCount)
                .ToString();
            return false;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.Look(ref this.saveVersion, "saveVersion", 0);
            Scribe_Deep.Look(ref this.directlyHeldThings, "directlyHeldThings", new object[] { this });
            Scribe_Collections.Look(
                ref this.records,
                "refrigeratedCargoRecords",
                LookMode.Deep,
                System.Array.Empty<object>());

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.EnsureInitialized();
                if (this.saveVersion <= 0)
                {
                    this.saveVersion = CurrentSaveVersion;
                }
            }
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            this.EnsureInitialized();
            return this.directlyHeldThings;
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            this.EnsureInitialized();
            if (outChildren == null)
            {
                return;
            }

            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, this.directlyHeldThings);
            for (int i = 0; i < this.records.Count; i++)
            {
                RefrigeratedCargoRecord record = this.records[i];
                if (record != null)
                {
                    outChildren.Add(record);
                }
            }
        }

        private RefrigeratedCargoRecord GetOrCreateRecordForInstalledModule(
            ShuttleModule module,
            ShuttleRefrigeratedCargoModuleDef moduleDef,
            ShuttleRuntimeState runtimeState)
        {
            string moduleInstanceID = module != null ? module.ModuleInstanceID : null;
            RefrigeratedCargoRecord record;
            if (this.TryGetRecord(moduleInstanceID, out record))
            {
                record.BindOrRefresh(module, moduleDef, runtimeState);
                return record;
            }

            record = new RefrigeratedCargoRecord();
            record.SetParentHolder(this);
            record.BindOrRefresh(module, moduleDef, runtimeState);
            this.records.Add(record);
            return record;
        }

        private RefrigeratedCargoCoolingStatus BuildCoolingStatus(RefrigeratedCargoRecord record)
        {
            return new RefrigeratedCargoCoolingStatus(
                record.ModuleInstanceID,
                record.ModuleDefName,
                record.CoolingActive,
                record.InactiveReason,
                record.TargetTemperatureC,
                record.ColdCargoMassCapacityKg,
                record.StoredMassKg,
                record.StackCount);
        }

        private ShuttleRefrigeratedCargoModuleDef ResolveModuleDef(string moduleDefName)
        {
            return !string.IsNullOrEmpty(moduleDefName)
                ? DefDatabase<ShuttleRefrigeratedCargoModuleDef>.GetNamedSilentFail(moduleDefName)
                : null;
        }

        private string GetModuleLabel(ShuttleRefrigeratedCargoModuleDef moduleDef, string moduleDefName)
        {
            if (moduleDef != null)
            {
                return moduleDef.LabelCap.ToString();
            }

            return !string.IsNullOrEmpty(moduleDefName) ? moduleDefName : "unknown";
        }

        private void MarkAllRecordsOrphaned()
        {
            for (int i = 0; i < this.records.Count; i++)
            {
                RefrigeratedCargoRecord record = this.records[i];
                if (record != null)
                {
                    record.EnsureInitialized();
                    record.MarkOrphaned();
                }
            }
        }

        private void RemoveEmptyOrphanedRecords(ShuttleAssemblyState assemblyState)
        {
            for (int i = this.records.Count - 1; i >= 0; i--)
            {
                RefrigeratedCargoRecord record = this.records[i];
                if (record == null)
                {
                    this.records.RemoveAt(i);
                    continue;
                }

                if (record.HasContents)
                {
                    continue;
                }

                if (assemblyState == null ||
                    string.IsNullOrEmpty(record.ModuleInstanceID) ||
                    assemblyState.GetModule(record.ModuleInstanceID) == null)
                {
                    this.records.RemoveAt(i);
                }
            }
        }

        private void EnsureInitialized()
        {
            if (this.directlyHeldThings == null)
            {
                this.directlyHeldThings = new ThingOwner<Thing>(this, false, LookMode.Deep, true);
            }

            if (this.records == null)
            {
                this.records = new List<RefrigeratedCargoRecord>();
            }

            for (int i = 0; i < this.records.Count; i++)
            {
                if (this.records[i] != null)
                {
                    this.records[i].EnsureInitialized();
                    this.records[i].SetParentHolder(this);
                }
            }
        }

        private int BuildInventoryProjectionSignature()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 397) ^ (this.records != null ? this.records.Count : 0);
                for (int i = 0; this.records != null && i < this.records.Count; i++)
                {
                    RefrigeratedCargoRecord record = this.records[i];
                    if (record == null)
                    {
                        hash = (hash * 397);
                        continue;
                    }

                    hash = (hash * 397) ^
                        (!string.IsNullOrEmpty(record.ModuleInstanceID)
                            ? record.ModuleInstanceID.GetHashCode()
                            : 0);
                    hash = (hash * 397) ^ (record.CoolingActive ? 1 : 0);
                }

                return hash;
            }
        }
    }
}
