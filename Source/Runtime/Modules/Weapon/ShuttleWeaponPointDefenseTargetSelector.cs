using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Enumerates only projectile-domain candidates during an existing scheduled weapon scan.
    /// Ranking prefers fewer current assignments, then shorter ETA, then greater damage.
    /// </summary>
    internal sealed class ShuttleWeaponPointDefenseTargetSelector
    {
        private readonly ShuttleWeaponPointDefenseEvaluator evaluator;
        private readonly ShuttlePointDefenseAssignmentService assignments;

        internal ShuttleWeaponPointDefenseTargetSelector(
            ShuttleWeaponPointDefenseEvaluator evaluator,
            ShuttlePointDefenseAssignmentService assignments)
        {
            this.evaluator = evaluator;
            this.assignments = assignments;
        }

        internal LocalTargetInfo FindNewTarget(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state,
            Verb attackVerb)
        {
            ThingWithComps host = context != null ? context.Host : null;
            Map map = host != null ? host.Map : null;
            if (map == null || map.listerThings == null || this.evaluator == null ||
                this.assignments == null || weaponDef == null ||
                weaponDef.pointDefenseMaxAssignmentsPerThreat <= 0)
            {
                return LocalTargetInfo.Invalid;
            }

            IReadOnlyList<Thing> projectiles =
                map.listerThings.ThingsInGroup(ThingRequestGroup.Projectile);
            Thing best = null;
            int bestAssignments = int.MaxValue;
            int bestEta = int.MaxValue;
            float bestDamage = float.MinValue;
            for (int i = 0; projectiles != null && i < projectiles.Count; i++)
            {
                Thing candidate = projectiles[i];
                LocalTargetInfo target = candidate != null
                    ? new LocalTargetInfo(candidate)
                    : LocalTargetInfo.Invalid;
                ShuttleProjectileThreat threat;
                ShuttleWeaponEngagementResult result;
                if (!this.evaluator.TryEvaluate(
                        context,
                        weaponDef,
                        state,
                        attackVerb,
                        target,
                        out threat,
                        out result))
                {
                    continue;
                }

                int assignmentCount = this.assignments.CountAssignments(context, candidate);
                if (assignmentCount >= weaponDef.pointDefenseMaxAssignmentsPerThreat ||
                    !IsBetter(
                        assignmentCount,
                        threat.TicksToImpact,
                        threat.DamageAmount,
                        bestAssignments,
                        bestEta,
                        bestDamage))
                {
                    continue;
                }

                best = candidate;
                bestAssignments = assignmentCount;
                bestEta = threat.TicksToImpact;
                bestDamage = threat.DamageAmount;
            }

            if (best == null ||
                !this.assignments.TryClaim(context, weaponDef, best))
            {
                return LocalTargetInfo.Invalid;
            }

            return new LocalTargetInfo(best);
        }

        private static bool IsBetter(
            int assignments,
            int eta,
            float damage,
            int bestAssignments,
            int bestEta,
            float bestDamage)
        {
            if (assignments != bestAssignments)
            {
                return assignments < bestAssignments;
            }

            if (eta != bestEta)
            {
                return eta < bestEta;
            }

            return damage > bestDamage;
        }
    }
}
