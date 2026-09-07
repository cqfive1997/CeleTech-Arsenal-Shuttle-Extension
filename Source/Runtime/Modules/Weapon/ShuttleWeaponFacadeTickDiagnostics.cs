using System.Runtime.CompilerServices;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Attributes the thin weapon facade without changing gameplay. Runtime-state identity staggers
    /// samples so fixed multi-weapon dispatch order cannot repeatedly select only one instance.
    /// </summary>
    internal struct ShuttleWeaponFacadeTickDiagnostics
    {
        private const int SampleInterval = 30;

        private readonly bool enabled;
        private readonly string bindingSection;
        private readonly string driverSection;
        private long sectionStart;
        private bool driverStarted;

        private ShuttleWeaponFacadeTickDiagnostics(
            string bindingSection,
            string driverSection)
        {
            this.enabled = true;
            this.bindingSection = bindingSection;
            this.driverSection = driverSection;
            this.sectionStart = 0L;
            this.driverStarted = false;
        }

        internal static ShuttleWeaponFacadeTickDiagnostics Create(
            ShuttleModuleRuntimeContext context)
        {
            if (!ShuttleWeaponRuntimeProfiler.Enabled || !ShouldSample(context))
            {
                return default(ShuttleWeaponFacadeTickDiagnostics);
            }

            string instanceID = context != null &&
                !string.IsNullOrEmpty(context.ModuleInstanceID)
                    ? context.ModuleInstanceID
                    : "unknown";
            return new ShuttleWeaponFacadeTickDiagnostics(
                ShuttleWeaponRuntimeProfiler.SectionFacadeBinding +
                    " [" + instanceID + "]",
                ShuttleWeaponRuntimeProfiler.SectionFacadeBackendDriver +
                    " [" + instanceID + "]");
        }

        internal void Begin()
        {
            this.sectionStart = ShuttleWeaponRuntimeProfiler.StartSection(this.enabled);
        }

        internal void CompleteBindingAndStartDriver()
        {
            if (!this.enabled)
            {
                return;
            }

            ShuttleWeaponRuntimeProfiler.Record(
                true,
                this.bindingSection,
                this.sectionStart);
            this.sectionStart = ShuttleWeaponRuntimeProfiler.StartSection(true);
            this.driverStarted = true;
        }

        internal void Finish()
        {
            if (!this.enabled)
            {
                return;
            }

            ShuttleWeaponRuntimeProfiler.Record(
                true,
                this.driverStarted ? this.driverSection : this.bindingSection,
                this.sectionStart);
        }

        private static bool ShouldSample(ShuttleModuleRuntimeContext context)
        {
            ShuttleWeaponRuntimeState state = context != null
                ? context.State as ShuttleWeaponRuntimeState
                : null;
            if (state == null || context.TicksGame < 0)
            {
                return false;
            }

            int phase = (RuntimeHelpers.GetHashCode(state) & int.MaxValue) %
                SampleInterval;
            int tickPhase = context.TicksGame % SampleInterval;
            if (tickPhase < 0)
            {
                tickPhase += SampleInterval;
            }

            return tickPhase == phase;
        }
    }
}
