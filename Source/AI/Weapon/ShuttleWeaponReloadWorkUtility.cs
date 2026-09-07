using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using RimWorld;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.AI.Weapon
{
    internal static class ShuttleWeaponReloadWorkUtility
    {
        internal const string ReloadJobDefName = "CT_Shuttle_ReloadShuttleWeapon";
        internal const int MaxReloadersPerShuttle = 1;

        internal static IEnumerable<Thing> AllShuttlesNeedingWeaponReload(Map map)
        {
            if (map == null)
            {
                yield break;
            }

            List<Thing> shuttleHosts = ShuttleHostCandidateUtility.GetModularShuttleHosts(map);
            for (int i = 0; i < shuttleHosts.Count; i++)
            {
                ThingWithComps thing = shuttleHosts[i] as ThingWithComps;
                if (thing == null || thing.Destroyed || !thing.Spawned)
                {
                    continue;
                }

                CompModularShuttleCore core = thing.TryGetComp<CompModularShuttleCore>();
                if (core != null && core.Controller != null)
                {
                    yield return thing;
                }
            }
        }

        internal static bool TryGetController(Thing shuttleHost, out ShuttleController controller)
        {
            controller = null;
            ThingWithComps withComps = shuttleHost as ThingWithComps;
            CompModularShuttleCore core = withComps != null ? withComps.TryGetComp<CompModularShuttleCore>() : null;
            controller = core != null ? core.Controller : null;
            return controller != null;
        }

        internal static bool CanPawnReloadShuttleWeapon(Pawn pawn, Thing shuttleHost, bool forced)
        {
            ShuttleWeaponManualReloadPlan plan;
            Thing ammoThing;
            return TryGetReloadJobInputs(
                pawn,
                shuttleHost,
                forced,
                out plan,
                out ammoThing);
        }

        internal static Job TryMakeReloadJob(Pawn pawn, Thing shuttleHost, bool forced)
        {
            ShuttleWeaponManualReloadPlan plan;
            Thing ammoThing;
            if (!TryGetReloadJobInputs(
                pawn,
                shuttleHost,
                forced,
                out plan,
                out ammoThing))
            {
                return null;
            }

            JobDef jobDef = DefDatabase<JobDef>.GetNamedSilentFail(ReloadJobDefName);
            if (jobDef == null || jobDef.driverClass == null)
            {
                return null;
            }

            int count = plan.RequestedAmmoCount < ammoThing.stackCount
                ? plan.RequestedAmmoCount
                : ammoThing.stackCount;
            if (count <= 0)
            {
                return null;
            }

            Job job = JobMaker.MakeJob(jobDef, shuttleHost, ammoThing);
            job.count = count;
            return job;
        }

        private static bool TryGetReloadJobInputs(
            Pawn pawn,
            Thing shuttleHost,
            bool forced,
            out ShuttleWeaponManualReloadPlan plan,
            out Thing ammoThing)
        {
            plan = null;
            ammoThing = null;
            if (pawn == null ||
                pawn.Map == null ||
                shuttleHost == null ||
                !shuttleHost.Spawned ||
                shuttleHost.Map != pawn.Map ||
                pawn.Downed ||
                pawn.Drafted)
            {
                return false;
            }

            ShuttleController controller;
            string reason;
            if (!TryGetController(shuttleHost, out controller) ||
                !controller.TryGetManualWeaponReloadPlan(pawn, out plan, out reason) ||
                plan == null ||
                plan.AmmoThingDef == null ||
                plan.RequestedAmmoCount <= 0)
            {
                return false;
            }

            if (!pawn.CanReserveAndReach(
                shuttleHost,
                PathEndMode.InteractionCell,
                Danger.Deadly,
                MaxReloadersPerShuttle,
                1,
                null,
                forced))
            {
                return false;
            }

            ammoThing = FindClosestReachableAmmo(pawn, plan.AmmoThingDef);
            return ammoThing != null;
        }

        private static Thing FindClosestReachableAmmo(Pawn pawn, ThingDef ammoThingDef)
        {
            if (pawn == null || pawn.Map == null || ammoThingDef == null)
            {
                return null;
            }

            return GenClosest.ClosestThingReachable(
                pawn.Position,
                pawn.Map,
                ThingRequest.ForDef(ammoThingDef),
                PathEndMode.Touch,
                TraverseParms.For(pawn),
                9999f,
                delegate(Thing thing)
                {
                    return thing != null &&
                        !thing.Destroyed &&
                        thing.Spawned &&
                        thing.def == ammoThingDef &&
                        thing.def.category == ThingCategory.Item &&
                        thing.stackCount > 0 &&
                        !thing.IsForbidden(pawn) &&
                        pawn.CanReserveAndReach(
                            thing,
                            PathEndMode.Touch,
                            Danger.Deadly);
                });
        }
    }
}
