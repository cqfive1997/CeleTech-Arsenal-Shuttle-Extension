using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Narrow access boundary for the shuttle host's transient point-defense assignment comp.
    /// </summary>
    internal sealed class ShuttlePointDefenseAssignmentService
    {
        internal int CountAssignments(
            ShuttleModuleRuntimeContext context,
            Thing projectile)
        {
            CompShuttlePointDefenseAssignments comp = this.TryGetComp(context);
            return comp != null
                ? comp.CountAssignments(projectile, context.TicksGame)
                : 0;
        }

        internal bool TryClaim(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            Thing projectile)
        {
            CompShuttlePointDefenseAssignments comp = this.TryGetComp(context);
            if (comp == null)
            {
                return true;
            }

            return comp.TryClaim(
                projectile,
                context.ModuleInstanceID,
                weaponDef.pointDefenseMaxAssignmentsPerThreat,
                context.TicksGame,
                weaponDef.pointDefenseAssignmentLeaseTicks);
        }

        internal bool TryRefreshIfAssigned(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state,
            LocalTargetInfo target)
        {
            if (context == null || weaponDef == null || state == null ||
                state.FireControlMode != ShuttleWeaponFireControlMode.PointDefense ||
                !target.HasThing)
            {
                return true;
            }

            CompShuttlePointDefenseAssignments comp = this.TryGetComp(context);
            if (comp == null)
            {
                return true;
            }

            return comp.TryRefresh(
                target.Thing,
                context.ModuleInstanceID,
                context.TicksGame,
                weaponDef.pointDefenseAssignmentLeaseTicks);
        }

        private CompShuttlePointDefenseAssignments TryGetComp(
            ShuttleModuleRuntimeContext context)
        {
            ThingWithComps host = context != null ? context.Host : null;
            return host != null
                ? host.TryGetComp<CompShuttlePointDefenseAssignments>()
                : null;
        }
    }
}
