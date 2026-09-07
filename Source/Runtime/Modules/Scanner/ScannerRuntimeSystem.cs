using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Launch;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Scanner
{
    /// <summary>
    /// Built-in no-op scanner runtime smoke system.
    /// It records lifecycle ticks only; real scanning gameplay is intentionally absent.
    /// </summary>
    internal sealed class ScannerRuntimeSystem : ShuttleModuleRuntimeSystemBase
    {
        public static readonly ScannerRuntimeSystem Instance = new ScannerRuntimeSystem();

        private const string ScannerRuntimeSystemKey = ShuttleRuntimeSystemKeyUtility.Scanner;

        private ScannerRuntimeSystem()
        {
        }

        public override string RuntimeSystemKey
        {
            get
            {
                return ScannerRuntimeSystemKey;
            }
        }

        public override int TickInterval
        {
            get
            {
                return 250;
            }
        }

        public override bool AppliesTo(ShuttleModule module)
        {
            return module != null && module.ModuleDef is ShuttleScannerModuleDef;
        }

        public override bool AppliesTo(ShuttleLaunchModuleRecord moduleRecord)
        {
            return moduleRecord != null && moduleRecord.ModuleDef is ShuttleScannerModuleDef;
        }

        public override IShuttleModuleRuntimeState CreateState()
        {
            return new ScannerRuntimeState();
        }

        public override void Reconcile(ShuttleModuleRuntimeContext context)
        {
            ScannerRuntimeState state = this.GetScannerState(context);
            if (state != null)
            {
                state.MarkReconciled(context.TicksGame);
            }
        }

        public override void Tick(ShuttleModuleRuntimeContext context)
        {
            ScannerRuntimeState state = this.GetScannerState(context);
            if (state != null)
            {
                state.MarkTicked(context.TicksGame);
            }
        }

        public override bool PreLaunchValidate(ShuttleModulePreLaunchValidationContext context, out string reason)
        {
            reason = null;
            return true;
        }

        public override void OnInstalled(ShuttleModuleRuntimeContext context)
        {
        }

        public override void OnRemoved(ShuttleModuleRuntimeContext context)
        {
        }

        public override void OnLaunchSucceeded(ShuttleModuleLaunchContext context)
        {
        }

        public override void OnArrived(ShuttleModuleRuntimeContext context)
        {
        }

        private ScannerRuntimeState GetScannerState(ShuttleModuleRuntimeContext context)
        {
            return context != null ? context.State as ScannerRuntimeState : null;
        }
    }
}
