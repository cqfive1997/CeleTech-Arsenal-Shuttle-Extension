using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyConstruction
{
    public sealed class JobDriver_ConstructShuttleAssemblyOrder : JobDriver
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
            if (!ShuttleAssemblyConstructionWorkUtility.IsShuttleReadyForConstruction(this.ShuttleHost))
            {
                return false;
            }

            return ShuttleAssemblyConstructionWorkUtility.TryReserveForConstructionJob(
                this.pawn,
                this.job,
                this.ShuttleHost,
                errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => !ShuttleAssemblyConstructionWorkUtility.IsShuttleReadyForConstruction(this.ShuttleHost));

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell)
                .FailOnDespawnedNullOrForbidden(TargetIndex.A)
                .FailOn(() => !ShuttleAssemblyConstructionWorkUtility.IsShuttleReadyForConstruction(this.ShuttleHost));

            yield return this.MakeConstructToil();
        }

        private Toil MakeConstructToil()
        {
            Toil toil = ToilMaker.MakeToil("ConstructShuttleAssemblyOrder");
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            toil.tickAction = delegate
            {
                if (!ShuttleAssemblyConstructionWorkUtility.IsShuttleReadyForConstruction(this.ShuttleHost))
                {
                    this.EndJobWith(JobCondition.Incompletable);
                    return;
                }

                ShuttleController controller;
                if (!ShuttleAssemblyConstructionWorkUtility.TryGetController(this.ShuttleHost, out controller))
                {
                    this.EndJobWith(JobCondition.Incompletable);
                    return;
                }

                ShuttleAssemblyConstructionOrder activeOrder =
                    ShuttleAssemblyConstructionWorkUtility.GetActiveOrder(this.ShuttleHost);
                if (activeOrder == null)
                {
                    this.EndJobWith(JobCondition.Incompletable);
                    return;
                }

                this.accumulatedWork +=
                    ShuttleAssemblyConstructionSkillUtility.GetConstructionWorkPerTick(
                        this.pawn,
                        activeOrder);
                ShuttleAssemblyConstructionSkillUtility.LearnConstructionWork(this.pawn, 1);
                int wholeWork = Mathf.FloorToInt(this.accumulatedWork);
                if (wholeWork <= 0)
                {
                    return;
                }

                this.accumulatedWork -= wholeWork;
                ShuttleCommandResult result = controller.AddAssemblyConstructionWorkFromPawn(this.pawn, wholeWork);
                if (result == null || !result.Success)
                {
                    this.EndJobWith(JobCondition.Incompletable);
                    return;
                }

                if (!ShuttleAssemblyConstructionWorkUtility.IsShuttleReadyForConstruction(this.ShuttleHost))
                {
                    this.EndJobWith(JobCondition.Succeeded);
                }
            };

            return toil;
        }
    }
}
