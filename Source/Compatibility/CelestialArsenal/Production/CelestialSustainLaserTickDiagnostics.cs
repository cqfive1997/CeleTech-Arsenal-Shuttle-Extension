using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;

namespace CeleTech.ShuttleExtension.Compatibility.CelestialArsenal
{
    /// <summary>
    /// Samples one Particle Lance call in thirty and forwards timings to the shared weapon profiler.
    /// The disabled path is the default value type and performs no timestamp or dictionary work.
    /// </summary>
    internal struct CelestialSustainLaserTickDiagnostics
    {
        private const int SampleInterval = 30;
        private static int sampleCounter;

        private readonly bool enabled;
        private readonly int ticksGame;
        private long sectionStart;

        private CelestialSustainLaserTickDiagnostics(bool enabled, int ticksGame)
        {
            this.enabled = enabled;
            this.ticksGame = ticksGame;
            this.sectionStart = 0L;
        }

        internal static CelestialSustainLaserTickDiagnostics Create(
            ShuttleModuleRuntimeContext context)
        {
            if (!ShuttleWeaponRuntimeProfiler.Enabled || !ShouldSample())
            {
                return default(CelestialSustainLaserTickDiagnostics);
            }

            return new CelestialSustainLaserTickDiagnostics(
                true,
                context != null ? context.TicksGame : -1);
        }

        internal void Begin()
        {
            this.sectionStart = ShuttleWeaponRuntimeProfiler.StartSection(this.enabled);
        }

        internal void CompleteAndStart(string sectionName)
        {
            if (!this.enabled)
            {
                return;
            }

            ShuttleWeaponRuntimeProfiler.Record(true, sectionName, this.sectionStart);
            this.sectionStart = ShuttleWeaponRuntimeProfiler.StartSection(true);
        }

        internal void Finish(string sectionName)
        {
            if (!this.enabled)
            {
                return;
            }

            ShuttleWeaponRuntimeProfiler.Record(true, sectionName, this.sectionStart);
            ShuttleWeaponRuntimeProfiler.MaybeLog(this.ticksGame);
        }

        private static bool ShouldSample()
        {
            sampleCounter++;
            if (sampleCounter < SampleInterval)
            {
                return false;
            }

            sampleCounter = 0;
            return true;
        }
    }
}
