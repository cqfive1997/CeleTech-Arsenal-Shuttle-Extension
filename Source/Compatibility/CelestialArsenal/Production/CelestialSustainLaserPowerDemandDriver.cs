using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CelestialArsenal
{
    internal sealed class CelestialSustainLaserPowerDemandDriver :
        IShuttleWeaponPowerDemandDriver
    {
        private readonly CelestialSustainLaserHost host;
        private readonly ShuttleWeaponCyclePolicy cyclePolicy;

        internal CelestialSustainLaserPowerDemandDriver(
            CelestialSustainLaserHost host,
            ShuttleWeaponCyclePolicy cyclePolicy)
        {
            this.host = host;
            this.cyclePolicy = cyclePolicy;
        }

        public void CollectPowerDemand(ShuttleModuleRuntimeContext context)
        {
            ShuttleWeaponModuleDef weaponDef = context != null
                ? context.ModuleDef as ShuttleWeaponModuleDef
                : null;
            ShuttleWeaponRuntimeState state = context != null
                ? context.State as ShuttleWeaponRuntimeState
                : null;
            Verb verb = this.host != null ? this.host.GetVerb(context) : null;
            if (context == null || !context.IsEnabled || weaponDef == null ||
                state == null || this.cyclePolicy == null ||
                weaponDef.firingPowerDrawWatts <= 0f ||
                !this.cyclePolicy.ShouldReportFiringPowerDemand(state, verb))
            {
                return;
            }

            context.AddInternalPowerDemandWatts(
                this.cyclePolicy.GetFiringPowerDrawWatts(weaponDef));
        }
    }
}
