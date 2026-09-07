using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated
{
    internal sealed class RefrigeratedLaunchStagingHolder : IExposable, IThingHolder, IShuttleFixedTemperatureHolder
    {
        private const int CurrentSaveVersion = 1;

        private int saveVersion = CurrentSaveVersion;
        private string moduleInstanceID;
        private string sourceModuleDefName;
        private float sourceTemperatureC = -10f;
        private float targetTemperatureC = -10f;
        private bool coolingActive;
        private ThingOwner<Thing> contents;
        private IThingHolder parentHolder;

        public RefrigeratedLaunchStagingHolder()
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

        internal string SourceModuleDefName
        {
            get
            {
                return this.sourceModuleDefName;
            }
        }

        internal float SourceTemperatureC
        {
            get
            {
                return this.sourceTemperatureC;
            }
        }

        internal float TargetTemperatureC
        {
            get
            {
                return this.targetTemperatureC;
            }
        }

        internal bool CoolingActive
        {
            get
            {
                return this.coolingActive;
            }
        }

        internal ThingOwner<Thing> Contents
        {
            get
            {
                this.EnsureInitialized();
                return this.contents;
            }
        }

        internal bool HasContents
        {
            get
            {
                return this.contents != null && this.contents.Count > 0;
            }
        }

        public IThingHolder ParentHolder
        {
            get
            {
                return this.parentHolder;
            }
        }

        internal int StackCount
        {
            get
            {
                return this.contents != null ? this.contents.Count : 0;
            }
        }

        internal void BindOrRefresh(
            string sourceModuleInstanceID,
            string moduleDefName,
            float sourceTemperature,
            float targetTemperature,
            bool active)
        {
            this.EnsureInitialized();
            this.moduleInstanceID = sourceModuleInstanceID;
            this.sourceModuleDefName = moduleDefName;
            this.sourceTemperatureC = IsFinite(sourceTemperature) ? sourceTemperature : targetTemperature;
            this.targetTemperatureC = IsFinite(targetTemperature) ? targetTemperature : this.sourceTemperatureC;
            this.coolingActive = active && IsFinite(this.targetTemperatureC);
        }

        internal void SetParentHolder(IThingHolder parent)
        {
            this.parentHolder = parent;
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
            Scribe_Values.Look(ref this.moduleInstanceID, "moduleInstanceID", null);
            Scribe_Values.Look(ref this.sourceModuleDefName, "sourceModuleDefName", null);
            Scribe_Values.Look(ref this.sourceTemperatureC, "sourceTemperatureC", -10f);
            Scribe_Values.Look(ref this.targetTemperatureC, "targetTemperatureC", -10f);
            Scribe_Values.Look(ref this.coolingActive, "coolingActive", false);
            Scribe_Deep.Look(ref this.contents, "contents", new object[] { this });

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.EnsureInitialized();
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

            if (!IsFinite(this.sourceTemperatureC))
            {
                this.sourceTemperatureC = -10f;
            }

            if (!IsFinite(this.targetTemperatureC))
            {
                this.targetTemperatureC = this.sourceTemperatureC;
            }

            if (!IsFinite(this.targetTemperatureC))
            {
                this.coolingActive = false;
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
