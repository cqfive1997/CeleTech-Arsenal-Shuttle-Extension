using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyRemoval
{
    public sealed class WorkGiver_DeconstructShuttleModule : WorkGiver_Scanner
    {
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return ShuttleModuleRemovalWorkUtility.AllShuttlesReadyForRemovalWork(
                pawn != null ? pawn.Map : null);
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return ShuttleModuleRemovalWorkUtility.CanPawnTakeRemovalWork(
                pawn,
                t,
                forced);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            JobDef jobDef = DefDatabase<JobDef>.GetNamedSilentFail(
                ShuttleModuleRemovalWorkUtility.DeconstructModuleJobDefName);
            if (jobDef == null || jobDef.driverClass == null)
            {
                return null;
            }

            return JobMaker.MakeJob(jobDef, t);
        }
    }
}
