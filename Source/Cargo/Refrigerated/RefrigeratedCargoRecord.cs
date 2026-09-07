using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated
{
    internal sealed class RefrigeratedCargoRecord : IExposable, IThingHolder, IShuttleFixedTemperatureHolder
    {
        private const int CurrentSaveVersion = 1;

        private int saveVersion = CurrentSaveVersion;
        private string moduleInstanceID;
        private string moduleDefName;
        private ThingOwner<Thing> contents;
        private IThingHolder parentHolder;
        private bool coolingActive;
        private float targetTemperatureC = -10f;
        private float coldCargoMassCapacityKg;
        private string inactiveReason;

        public RefrigeratedCargoRecord()
        {
            this.contents = new ThingOwner<Thing>(this, false, LookMode.Deep, true);
        }

        internal string ModuleInstanceID
        {
            get
            {
                return this.moduleInstanceID;
            }
        }

        internal string ModuleDefName
        {
            get
            {
                return this.moduleDefName;
            }
        }

        internal bool CoolingActive
        {
            get
            {
                return this.coolingActive;
            }
        }

        internal string InactiveReason
        {
            get
            {
                return this.inactiveReason;
            }
        }

        internal float TargetTemperatureC
        {
            get
            {
                return this.targetTemperatureC;
            }
        }

        internal float ColdCargoMassCapacityKg
        {
            get
            {
                return this.coldCargoMassCapacityKg;
            }
        }

        // Foundation/debug access only. Runtime and UI paths should use the registry service
        // methods instead of mutating refrigerated cargo contents directly.
        internal ThingOwner<Thing> Contents
        {
            get
            {
                this.EnsureInitialized();
                return this.contents;
            }
        }

        public IThingHolder ParentHolder
        {
            get
            {
                return this.parentHolder;
            }
        }

        internal bool HasContents
        {
            get
            {
                return this.contents != null && this.contents.Count > 0;
            }
        }

        internal int StackCount
        {
            get
            {
                return this.contents != null ? this.contents.Count : 0;
            }
        }

        internal float StoredMassKg
        {
            get
            {
                this.EnsureInitialized();
                float massKg = 0f;
                for (int i = 0; i < this.contents.Count; i++)
                {
                    Thing thing = this.contents[i];
                    if (thing != null)
                    {
                        massKg += CargoDisplayUtility.GetThingMass(thing, thing.stackCount);
                    }
                }

                return massKg;
            }
        }

        internal void BindOrRefresh(
            ShuttleModule module,
            ShuttleRefrigeratedCargoModuleDef moduleDef,
            ShuttleRuntimeState runtimeState)
        {
            this.EnsureInitialized();
            if (module != null)
            {
                this.moduleInstanceID = module.ModuleInstanceID;
                this.moduleDefName = module.moduleDefName;
            }
            else if (moduleDef != null)
            {
                this.moduleDefName = moduleDef.defName;
            }

            if (moduleDef == null)
            {
                this.SetCoolingInactive("module-def-missing");
                return;
            }

            this.targetTemperatureC = moduleDef.targetTemperatureC;
            this.coldCargoMassCapacityKg = moduleDef.coldCargoMassCapacityKg;

            if (module == null)
            {
                this.SetCoolingInactive("module-missing");
                return;
            }

            if (!module.IsEnabled)
            {
                this.SetCoolingInactive("module-disabled");
                return;
            }

            if (moduleDef.requirePoweredCooling &&
                (runtimeState == null ||
                    runtimeState.Power == null ||
                    !runtimeState.Power.InternalBusPowered))
            {
                this.SetCoolingInactive("module-unpowered");
                return;
            }

            if (!IsFinite(this.targetTemperatureC))
            {
                this.SetCoolingInactive("temperature-invalid");
                return;
            }

            this.coolingActive = true;
            this.inactiveReason = null;
        }

        internal void SetParentHolder(IThingHolder parent)
        {
            this.parentHolder = parent;
        }

        internal void MarkOrphaned()
        {
            this.SetCoolingInactive("module-orphaned");
        }

        public bool TryGetFixedTemperature(out float temperatureC)
        {
            temperatureC = 0f;
            if (!this.coolingActive || !IsFinite(this.targetTemperatureC))
            {
                return false;
            }

            temperatureC = this.targetTemperatureC;
            return true;
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            this.EnsureInitialized();
            return this.contents;
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            this.EnsureInitialized();
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, this.contents);
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.saveVersion, "saveVersion", 0);
            Scribe_Values.Look(ref this.moduleInstanceID, "moduleInstanceID");
            Scribe_Values.Look(ref this.moduleDefName, "moduleDefName");
            Scribe_Deep.Look(ref this.contents, "contents", new object[] { this });
            Scribe_Values.Look(ref this.targetTemperatureC, "targetTemperatureC", -10f);
            Scribe_Values.Look(ref this.coldCargoMassCapacityKg, "coldCargoMassCapacityKg", 0f);
            Scribe_Values.Look(ref this.inactiveReason, "inactiveReason");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.EnsureInitialized();
                this.SetCoolingInactive("awaiting-reconcile");
                if (this.saveVersion <= 0)
                {
                    this.saveVersion = CurrentSaveVersion;
                }
            }
        }

        internal void EnsureInitialized()
        {
            if (this.contents == null)
            {
                this.contents = new ThingOwner<Thing>(this, false, LookMode.Deep, true);
            }

            if (!IsFinite(this.targetTemperatureC))
            {
                this.targetTemperatureC = -10f;
            }

            if (!IsFinite(this.coldCargoMassCapacityKg) || this.coldCargoMassCapacityKg < 0f)
            {
                this.coldCargoMassCapacityKg = 0f;
            }
        }

        private void SetCoolingInactive(string reason)
        {
            this.coolingActive = false;
            this.inactiveReason = reason;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
