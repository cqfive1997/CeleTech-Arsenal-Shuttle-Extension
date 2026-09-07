using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using RimWorld;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.AI.Habitat
{
    public sealed class JobDriver_HabitatJoy : JobDriver
    {
        private Thing ShuttleHost
        {
            get
            {
                return this.job != null ? this.job.GetTarget(TargetIndex.A).Thing : null;
            }
        }

        private JoyKindDef JoyKind
        {
            get
            {
                return this.job != null && this.job.def != null ? this.job.def.joyKind : null;
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Thing shuttleHost = this.ShuttleHost;
            JoyKindDef joyKind = this.JoyKind;
            string failReason;
            if (!HabitatUtility.CanUseHabitatForJoy(this.pawn, shuttleHost, joyKind, out failReason))
            {
                return false;
            }

            ShuttleProfile profile;
            HabitatProfile habitat;
            if (!HabitatUtility.TryGetPoweredHabitatProfile(shuttleHost, out profile, out habitat))
            {
                return false;
            }

            return this.pawn.Reserve(shuttleHost, this.job, habitat.JoySlots, 0, null, errorOnFailed, false);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => !this.CanStillUseHabitatForJoy());

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell)
                .FailOnDespawnedNullOrForbidden(TargetIndex.A)
                .FailOn(() => !this.CanStillUseHabitatForJoy())
                .FailOn(() => !this.HasAvailableJoySlotForCurrentJob());

            yield return this.MakeEnterHabitatJoyToil();
        }

        private Toil MakeEnterHabitatJoyToil()
        {
            Toil toil = ToilMaker.MakeToil("EnterHabitatJoy");
            toil.initAction = delegate
            {
                CompShuttleHabitatOccupancy occupancy = this.GetHabitatOccupancy();
                if (occupancy == null)
                {
                    this.EndJobWith(JobCondition.Incompletable);
                    return;
                }

                string failReason;
                JobDef jobDef = this.job != null ? this.job.def : null;
                int maxJoyTicks = jobDef != null && jobDef.joyDuration > 0 ? jobDef.joyDuration : 4000;
                float joyGainRate = jobDef != null && jobDef.joyGainRate > 0f ? jobDef.joyGainRate : 1f;
                if (!occupancy.TryEnterForJoy(
                    this.pawn,
                    this.JoyKind,
                    joyGainRate,
                    maxJoyTicks,
                    out failReason))
                {
                    this.EndJobWith(JobCondition.Incompletable);
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }

        private bool CanStillUseHabitatForJoy()
        {
            string failReason;
            return HabitatUtility.CanUseHabitatForJoy(this.pawn, this.ShuttleHost, this.JoyKind, out failReason);
        }

        private bool HasAvailableJoySlotForCurrentJob()
        {
            HabitatProfile habitat;
            if (!this.TryGetCurrentHabitat(out habitat))
            {
                return false;
            }

            int currentUsers = HabitatUtility.CountCurrentHabitatJoyUsers(this.ShuttleHost, this.pawn);
            return currentUsers < habitat.JoySlots;
        }

        private bool TryGetCurrentHabitat(out HabitatProfile habitat)
        {
            habitat = null;
            ShuttleProfile profile;
            return HabitatUtility.TryGetPoweredHabitatProfile(
                this.ShuttleHost,
                out profile,
                out habitat);
        }

        private CompShuttleHabitatOccupancy GetHabitatOccupancy()
        {
            ThingWithComps shuttleWithComps = this.ShuttleHost as ThingWithComps;
            return shuttleWithComps != null
                ? shuttleWithComps.TryGetComp<CompShuttleHabitatOccupancy>()
                : null;
        }
    }
}
