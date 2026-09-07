using CeleTech.ShuttleExtension.ModularShuttle.MechCharging;
using RimWorld;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.AI.MechCharging
{
    public sealed class JobGiver_GetEnergy_ShuttleMechCharger : JobGiver_GetEnergy
    {
        private const float ShuttleFallbackPriority = 9.49f;

        public override float GetPriority(Pawn pawn)
        {
            Thing shuttleHost;
            return this.TryFindShuttleMechCharger(pawn, out shuttleHost)
                ? ShuttleFallbackPriority
                : 0f;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            Thing shuttleHost;
            if (!this.TryFindShuttleMechCharger(pawn, out shuttleHost))
            {
                return null;
            }

            JobDef jobDef = DefDatabase<JobDef>.GetNamedSilentFail(
                MechChargerAdmissionValidator.ShuttleMechChargeJobDefName);
            if (jobDef == null || jobDef.driverClass == null)
            {
                return null;
            }

            Job job = JobMaker.MakeJob(jobDef, shuttleHost);
            job.targetA = shuttleHost;
            return job;
        }

        private bool TryFindShuttleMechCharger(Pawn pawn, out Thing shuttleHost)
        {
            shuttleHost = null;
            if (!IsEligibleColonyMech(pawn) ||
                !this.ShouldUseShuttleAutoRecharge(pawn) ||
                ShuttleMechChargerUtility.HasUsableVanillaMechCharger(pawn))
            {
                return false;
            }

            shuttleHost = ShuttleMechChargerUtility.FindBestAvailableShuttleMechCharger(pawn);
            if (shuttleHost == null || shuttleHost.Destroyed)
            {
                shuttleHost = null;
                return false;
            }

            return true;
        }

        private static bool IsEligibleColonyMech(Pawn pawn)
        {
            return ModsConfig.BiotechActive &&
                pawn != null &&
                !pawn.Destroyed &&
                !pawn.Dead &&
                pawn.Spawned &&
                pawn.Map != null &&
                pawn.RaceProps != null &&
                pawn.RaceProps.IsMechanoid &&
                pawn.IsColonyMech &&
                ShuttleMechChargeNeedUtility.HasChargeNeed(pawn);
        }

        private bool ShouldUseShuttleAutoRecharge(Pawn pawn)
        {
            if (pawn != null && pawn.needs != null && pawn.needs.energy != null)
            {
                return this.ShouldAutoRecharge(pawn);
            }

            return ShuttleMechChargeNeedUtility.ShouldAutoRecharge(pawn, 0.35f);
        }
    }
}
