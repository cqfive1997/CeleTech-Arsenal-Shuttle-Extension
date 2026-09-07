using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyConstruction;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyRemoval;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.AI
{
    public sealed class JobDriver_DeconstructShuttleModule : JobDriver
    {
        private float accumulatedWork;

        private Thing ShuttleHost
        {
            get
            {
                return this.job != null ? this.job.GetTarget(TargetIndex.A).Thing : null;
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!ShuttleModuleRemovalWorkUtility.IsShuttleReadyForRemovalWork(this.ShuttleHost))
            {
                return false;
            }

            return ShuttleModuleRemovalWorkUtility.TryReserveForRemovalJob(
                this.pawn,
                this.job,
                this.ShuttleHost,
                errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => !ShuttleModuleRemovalWorkUtility.IsShuttleReadyForRemovalWork(this.ShuttleHost));

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell)
                .FailOnDespawnedNullOrForbidden(TargetIndex.A)
                .FailOn(() => !ShuttleModuleRemovalWorkUtility.IsShuttleReadyForRemovalWork(this.ShuttleHost));

            yield return this.MakeDeconstructToil();
        }

        private Toil MakeDeconstructToil()
        {
            Toil toil = ToilMaker.MakeToil("DeconstructShuttleModule");
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            toil.tickAction = delegate
            {
                if (!ShuttleModuleRemovalWorkUtility.IsShuttleReadyForRemovalWork(this.ShuttleHost))
                {
                    this.EndJobWith(JobCondition.Succeeded);
                    return;
                }

                ShuttleController controller;
                if (!ShuttleModuleRemovalWorkUtility.TryGetController(this.ShuttleHost, out controller))
                {
                    this.EndJobWith(JobCondition.Incompletable);
                    return;
                }

                this.accumulatedWork +=
                    ShuttleAssemblyConstructionSkillUtility.GetConstructionWorkPerTick(
                        this.pawn,
                        null);
                ShuttleAssemblyConstructionSkillUtility.LearnConstructionWork(this.pawn, 1);
                int wholeWork = Mathf.FloorToInt(this.accumulatedWork);
                if (wholeWork <= 0)
                {
                    return;
                }

                this.accumulatedWork -= wholeWork;
                ShuttleCommandResult result = controller.AddActiveModuleRemovalWorkFromPawn(this.pawn, wholeWork);
                if (result == null || !result.Success)
                {
                    this.EndJobWith(JobCondition.Incompletable);
                    return;
                }

                if (!ShuttleModuleRemovalWorkUtility.IsShuttleReadyForRemovalWork(this.ShuttleHost))
                {
                    this.EndJobWith(JobCondition.Succeeded);
                }
            };

            return toil;
        }
    }
}
