using RimWorld;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.AI.Habitat
{
    public sealed class JoyGiver_ShuttleRecreation : JoyGiver
    {
        private const float AvailableHabitatJoyChance = 6f;

        public override bool CanBeGivenTo(Pawn pawn)
        {
            return base.CanBeGivenTo(pawn) &&
                this.def != null &&
                this.def.joyKind != null &&
                HabitatUtility.FindBestHabitatForJoy(pawn, this.def.joyKind) != null;
        }

        public override float GetChance(Pawn pawn)
        {
            return this.def != null && this.def.baseChance > AvailableHabitatJoyChance
                ? this.def.baseChance
                : AvailableHabitatJoyChance;
        }

        public override Job TryGiveJob(Pawn pawn)
        {
            if (this.def == null || this.def.jobDef == null || this.def.joyKind == null)
            {
                return null;
            }

            Thing shuttleHost = HabitatUtility.FindBestHabitatForJoy(pawn, this.def.joyKind);
            if (shuttleHost == null)
            {
                return null;
            }

            Job job = JobMaker.MakeJob(this.def.jobDef, shuttleHost);
            job.targetA = shuttleHost;
            return job;
        }
    }
}
