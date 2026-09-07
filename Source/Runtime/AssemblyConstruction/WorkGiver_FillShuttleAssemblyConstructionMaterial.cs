using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyConstruction
{
    public sealed class WorkGiver_FillShuttleAssemblyConstructionMaterial : WorkGiver_Scanner
    {
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return ShuttleAssemblyConstructionHaulUtility.AllShuttlesAwaitingMaterials(
                pawn != null ? pawn.Map : null);
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (pawn == null || t == null || !t.Spawned)
            {
                return false;
            }

            if (!ShuttleAssemblyConstructionHaulUtility.IsShuttleAwaitingMaterials(t))
            {
                return false;
            }

            if (!pawn.CanReach(
                t,
                PathEndMode.InteractionCell,
                Danger.Deadly,
                false,
                false,
                TraverseMode.ByPawn))
            {
                return false;
            }

            Thing material;
            int count;
            return ShuttleAssemblyConstructionHaulUtility.TryFindNextMaterialForShuttle(
                pawn,
                t,
                out material,
                out count);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Thing material;
            int count;
            if (!ShuttleAssemblyConstructionHaulUtility.TryFindNextMaterialForShuttle(
                pawn,
                t,
                out material,
                out count))
            {
                return null;
            }

            JobDef jobDef = DefDatabase<JobDef>.GetNamedSilentFail(
                ShuttleAssemblyConstructionHaulUtility.FillMaterialJobDefName);
            if (jobDef == null || jobDef.driverClass == null)
            {
                return null;
            }

            Job job = JobMaker.MakeJob(jobDef, material, t);
            job.count = count;
            return job;
        }
    }
}
