using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Launch;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules
{
    /// <summary>
    /// Context for launch-success notifications. Prelaunch validation uses
    /// ShuttleModulePreLaunchValidationContext so it cannot mutate runtime state.
    /// </summary>
    internal sealed class ShuttleModuleLaunchContext
    {
        public ShuttleModuleLaunchContext(
            ShuttleLaunchModuleRecord moduleRecord,
            ShuttleLaunchSegmentRecord parentSegmentRecord,
            IShuttleModuleRuntimeState state)
        {
            this.ModuleRecord = moduleRecord;
            this.ParentSegmentRecord = parentSegmentRecord;
            this.State = state;
        }

        public ShuttleLaunchModuleRecord ModuleRecord { get; private set; }

        /// <summary>
        /// Parent segment identity copied at launch confirmation. It may be null if topology is
        /// inconsistent or a def is missing.
        /// </summary>
        public ShuttleLaunchSegmentRecord ParentSegmentRecord { get; private set; }

        public string ModuleInstanceID
        {
            get
            {
                return this.ModuleRecord != null ? this.ModuleRecord.ModuleInstanceID : null;
            }
        }

        public string ModuleDefName
        {
            get
            {
                return this.ModuleRecord != null ? this.ModuleRecord.ModuleDefName : null;
            }
        }

        public ShuttleModuleBaseDef ModuleDef
        {
            get
            {
                return this.ModuleRecord != null ? this.ModuleRecord.ModuleDef : null;
            }
        }

        public string ParentSegmentInstanceID
        {
            get
            {
                return this.ModuleRecord != null ? this.ModuleRecord.ParentSegmentInstanceID : null;
            }
        }

        public string ParentSegmentDefName
        {
            get
            {
                return this.ParentSegmentRecord != null
                    ? this.ParentSegmentRecord.SegmentDefName
                    : this.ModuleRecord != null ? this.ModuleRecord.ParentSegmentDefName : null;
            }
        }

        public string ParentSlotID
        {
            get
            {
                return this.ModuleRecord != null ? this.ModuleRecord.ParentSlotID : null;
            }
        }

        public bool IsEnabled
        {
            get
            {
                return this.ModuleRecord != null && this.ModuleRecord.IsEnabled;
            }
        }

        /// <summary>
         /// Existing runtime payload. Launch prevalidation must treat it as read-only.
        /// </summary>
        public IShuttleModuleRuntimeState State { get; private set; }
    }
}
