using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CelestialArsenal
{
    internal sealed class CelestialSustainLaserLifecycleDriver :
        IShuttleWeaponLifecycleDriver,
        IShuttleWeaponCycleHost
    {
        private readonly CelestialSustainLaserHost host;
        private readonly CelestialSustainLaserFireDriver fireDriver;

        internal CelestialSustainLaserLifecycleDriver(
            CelestialSustainLaserHost host,
            CelestialSustainLaserFireDriver fireDriver)
        {
            this.host = host;
            this.fireDriver = fireDriver;
        }

        public bool EnsureReady(ShuttleModuleRuntimeContext context)
        {
            Verb_ShuttleCelestialSustainLaser verb;
            return this.TryGetReadyVerb(context, out verb);
        }

        internal bool TryGetReadyVerb(
            ShuttleModuleRuntimeContext context,
            out Verb_ShuttleCelestialSustainLaser verb)
        {
            verb = null;
            return this.host != null &&
                this.host.TryEnsureReady(context, this.fireDriver, out verb);
        }

        public Verb GetAttackVerb(ShuttleModuleRuntimeContext context)
        {
            return this.host != null ? this.host.GetVerb(context) : null;
        }

        public void TickVerbs(ShuttleModuleRuntimeContext context)
        {
            if (this.host != null)
            {
                this.host.TickVerbs(context);
            }
        }
    }
}
