using System;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CelestialArsenal
{
    internal enum CelestialSustainLaserShotResult
    {
        Failed,
        Applied,
        Transitioning
    }

    /// <summary>
    /// Executes one authored beam pulse after Verse schedules it; owns no Verb or runtime state.
    /// </summary>
    internal sealed class CelestialSustainLaserShotExecutor
    {
        private readonly CelestialSustainLaserDamageApplier damageApplier =
            new CelestialSustainLaserDamageApplier();

        internal CelestialSustainLaserShotResult Execute(
            Thing caster,
            ThingWithComps equipmentSource,
            VerbProperties verbProps,
            LocalTargetInfo target,
            IntVec3 sourceCell,
            CompShuttleSustainLaserData data,
            CelestialSustainLaserRetargetController retargetController,
            Func<LocalTargetInfo, bool> hasLineOfSight)
        {
            if (caster == null || caster.Map == null || equipmentSource == null ||
                !sourceCell.IsValid)
            {
                return CelestialSustainLaserShotResult.Failed;
            }

            Thing targetThing = target.Thing;
            if (targetThing != null && targetThing.Map != caster.Map)
            {
                return CelestialSustainLaserShotResult.Failed;
            }

            if (verbProps != null && verbProps.requireLineOfSight &&
                verbProps.stopBurstWithoutLos &&
                (hasLineOfSight == null || !hasLineOfSight(target)))
            {
                return CelestialSustainLaserShotResult.Failed;
            }

            CompChangeableProjectile changeable =
                equipmentSource.GetComp<CompChangeableProjectile>();
            if (changeable != null)
            {
                changeable.Notify_ProjectileLaunched();
            }

            if (retargetController != null &&
                retargetController.AdvanceTransitionShot())
            {
                return CelestialSustainLaserShotResult.Transitioning;
            }

            bool applied = this.damageApplier.Apply(
                caster,
                equipmentSource,
                target,
                targetThing,
                sourceCell,
                data);
            return applied
                ? CelestialSustainLaserShotResult.Applied
                : CelestialSustainLaserShotResult.Failed;
        }
    }
}
