using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    /// <summary>
    /// Projects firing demand only. Reload demand remains owned by the later CE reload facet.
    /// </summary>
    internal sealed class CeWeaponFiringPowerProjection
    {
        private readonly ShuttleWeaponCyclePolicy cyclePolicy;

        internal CeWeaponFiringPowerProjection(ShuttleWeaponCyclePolicy cyclePolicy)
        {
            this.cyclePolicy = cyclePolicy;
        }

        internal void Collect(
            ShuttleModuleRuntimeContext context,
            CeWeaponMuzzleVerb verb)
        {
            if (context == null || !context.IsEnabled)
            {
                return;
            }

            ShuttleWeaponModuleDef weaponDef =
                context.ModuleDef as ShuttleWeaponModuleDef;
            ShuttleWeaponRuntimeState state =
                context.State as ShuttleWeaponRuntimeState;
            if (weaponDef == null || state == null ||
                !this.cyclePolicy.ShouldReportFiringPowerDemand(state, verb))
            {
                return;
            }

            float watts = this.cyclePolicy.GetFiringPowerDrawWatts(weaponDef);
            if (watts > 0f)
            {
                context.AddInternalPowerDemandWatts(watts);
            }
        }
    }
}
