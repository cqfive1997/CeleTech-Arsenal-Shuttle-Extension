using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Launch;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Habitat
{
    /// <summary>
    /// Runtime hook for installed Habitat/Recreation modules. Pawn occupancy is owned by
    /// CompShuttleHabitatOccupancy; this runtime bucket stays module-scoped and contains
    /// only durable per-module timestamps.
    /// </summary>
    internal sealed class ShuttleHabitatRuntimeSystem : ShuttleModuleRuntimeSystemBase
    {
        public static readonly ShuttleHabitatRuntimeSystem Instance = new ShuttleHabitatRuntimeSystem();

        internal const string HabitatRuntimeSystemKey = ShuttleRuntimeSystemKeyUtility.Habitat;

        private ShuttleHabitatRuntimeSystem()
        {
        }

        public override string RuntimeSystemKey
        {
            get
            {
                return HabitatRuntimeSystemKey;
            }
        }

        public override int TickInterval
        {
            get
            {
                return 0;
            }
        }

        public override bool AppliesTo(ShuttleModule module)
        {
            return module != null && module.ModuleDef is ShuttleHabitatModuleDef;
        }

        public override bool AppliesTo(ShuttleLaunchModuleRecord moduleRecord)
        {
            return moduleRecord != null && moduleRecord.ModuleDef is ShuttleHabitatModuleDef;
        }

        public override IShuttleModuleRuntimeState CreateState()
        {
            return new ShuttleHabitatRuntimeState();
        }

        public override void Reconcile(ShuttleModuleRuntimeContext context)
        {
            this.SanitizeState(context);
        }

        public override void OnInstalled(ShuttleModuleRuntimeContext context)
        {
            this.SanitizeState(context);
        }

        public override void OnArrived(ShuttleModuleRuntimeContext context)
        {
            this.SanitizeState(context);
        }

        private void SanitizeState(ShuttleModuleRuntimeContext context)
        {
            ShuttleHabitatRuntimeState state = context != null
                ? context.State as ShuttleHabitatRuntimeState
                : null;
            if (state != null)
            {
                state.SanitizeForRuntimeOnly();
            }
        }
    }
}
