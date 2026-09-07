using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyConstruction;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Hull;
using RimWorld;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.AI.Hull
{
    public sealed class JobDriver_RepairShuttleHull : JobDriver
    {
        private float plannedRepairHitPoints;
        private int plannedWorkTicks;
        private bool materialsLoaded;
        private ShuttleHullRepairMaterialLedger stagedMaterials =
            new ShuttleHullRepairMaterialLedger();

        private Thing ShuttleHost
        {
            get
            {
                return this.job != null ? this.job.GetTarget(TargetIndex.A).Thing : null;
            }
        }

        public override void Notify_Starting()
        {
            base.Notify_Starting();
            this.AddFinishAction(this.ReleaseRepairMaterialsOnExit);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Thing shuttleHost = this.ShuttleHost;
            ShuttleController controller;
            string reason;
            if (!this.TryGetController(out controller) ||
                !controller.CanPawnRepairHull(this.pawn, out reason))
            {
                return false;
            }

            return this.pawn.Reserve(
                shuttleHost,
                this.job,
                1,
                1,
                null,
                errorOnFailed,
                false);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOn(() => !this.CanStillRepairHull());

            yield return this.MakeInitializeRepairPlanToil();

            Toil findMaterial = this.MakeFindNextMaterialToil();
            Toil gotoRepair = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch)
                .FailOnDestroyedOrNull(TargetIndex.A)
                .FailOn(() => !this.CanStillRepairHull());

            yield return findMaterial;
            yield return Toils_Jump.JumpIf(gotoRepair, () => this.materialsLoaded);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch)
                .FailOnDespawnedNullOrForbidden(TargetIndex.B)
                .FailOn(() => !this.CanStillRepairHull());
            yield return Toils_Haul.StartCarryThing(TargetIndex.B);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell)
                .FailOnDestroyedOrNull(TargetIndex.A)
                .FailOn(() => !this.CanStillRepairHull());
            yield return this.MakeStageMaterialToil();
            yield return Toils_Jump.Jump(findMaterial);
            yield return gotoRepair;
            yield return this.MakeRepairWorkToil();
            yield return this.MakeApplyRepairToil();
        }

        private Toil MakeInitializeRepairPlanToil()
        {
            Toil toil = ToilMaker.MakeToil("InitializeShuttleHullRepairPlan");
            toil.initAction = delegate
            {
                float repairHitPoints;
                int workTicks;
                string reason;
                if (!this.TryGetRepairPlan(out repairHitPoints, out workTicks, out reason))
                {
                    this.EndJobWith(JobCondition.Incompletable);
                    return;
                }

                this.plannedRepairHitPoints = repairHitPoints;
                this.plannedWorkTicks = workTicks;
                this.materialsLoaded = false;
                this.EnsureMaterialLedger();
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }

        private Toil MakeFindNextMaterialToil()
        {
            Toil toil = ToilMaker.MakeToil("FindNextShuttleHullRepairMaterial");
            toil.initAction = delegate
            {
                ShuttleController controller;
                if (!this.TryGetController(out controller))
                {
                    this.EndJobWith(JobCondition.Incompletable);
                    return;
                }

                Thing material;
                int count;
                bool loaded;
                string reason;
                if (!controller.TryFindNextHullRepairMaterialForPawn(
                    this.pawn,
                    this.plannedRepairHitPoints,
                    this.EnsureMaterialLedger(),
                    out material,
                    out count,
                    out loaded,
                    out reason))
                {
                    Messages.Message(
                        reason ?? "CT_Shuttle_HullRepair_MissingMaterials".Translate("-").ToString(),
                        MessageTypeDefOf.RejectInput,
                        false);
                    this.EndJobWith(JobCondition.Incompletable);
                    return;
                }

                this.materialsLoaded = loaded;
                if (loaded)
                {
                    return;
                }

                if (material == null || count <= 0)
                {
                    this.EndJobWith(JobCondition.Incompletable);
                    return;
                }

                this.job.SetTarget(TargetIndex.B, material);
                this.job.count = count;
                if (!this.pawn.Reserve(material, this.job, 1, count, null, false, false))
                {
                    this.EndJobWith(JobCondition.Incompletable);
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }

        private Toil MakeStageMaterialToil()
        {
            Toil toil = ToilMaker.MakeToil("StageShuttleHullRepairMaterial");
            toil.initAction = delegate
            {
                ShuttleController controller;
                if (!this.TryGetController(out controller))
                {
                    this.EndJobWith(JobCondition.Incompletable);
                    return;
                }

                string reason;
                if (!controller.TryStageCarriedHullRepairMaterialFromPawn(
                    this.pawn,
                    this.plannedRepairHitPoints,
                    this.EnsureMaterialLedger(),
                    out reason))
                {
                    Messages.Message(
                        reason ?? "CT_Shuttle_HullRepair_Failed".Translate().ToString(),
                        MessageTypeDefOf.RejectInput,
                        false);
                    this.EndJobWith(JobCondition.Incompletable);
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }

        private Toil MakeRepairWorkToil()
        {
            Toil toil = ToilMaker.MakeToil("RepairShuttleHull");
            toil.debugName = "RepairShuttleHull";
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = ShuttleHullRepairService.DefaultRepairWorkTicks;
            toil.initAction = delegate
            {
                int workTicks = this.plannedWorkTicks > 0
                    ? this.plannedWorkTicks
                    : ShuttleHullRepairService.DefaultRepairWorkTicks;
                toil.defaultDuration = workTicks;
                this.ticksLeftThisToil = workTicks;
            };
            toil.activeSkill = () => SkillDefOf.Construction;
            toil.handlingFacing = true;
            toil.tickIntervalAction = delegate(int delta)
            {
                if (this.pawn != null &&
                    this.pawn.rotationTracker != null &&
                    this.ShuttleHost != null)
                {
                    this.pawn.rotationTracker.FaceTarget(this.ShuttleHost);
                }

                ShuttleAssemblyWorkEffectUtility.TryPlayWorkTickEffect(
                    this.ShuttleHost as ThingWithComps,
                    this.pawn,
                    ShuttleAssemblyWorkEffectKind.Repair,
                    Find.TickManager != null ? Find.TickManager.TicksGame : -1,
                    60);
            };
            return toil.WithProgressBarToilDelay(TargetIndex.A)
                .FailOnDestroyedOrNull(TargetIndex.A)
                .FailOn(() => !this.CanStillRepairHull());
        }

        private Toil MakeFailToil()
        {
            Toil toil = ToilMaker.MakeToil("FailShuttleHullRepair");
            toil.initAction = delegate
            {
                this.EndJobWith(JobCondition.Incompletable);
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }

        private Toil MakeApplyRepairToil()
        {
            Toil toil = ToilMaker.MakeToil("ApplyShuttleHullRepair");
            toil.initAction = delegate
            {
                ShuttleController controller;
                if (!this.TryGetController(out controller))
                {
                    this.EndJobWith(JobCondition.Incompletable);
                    return;
                }

                string message;
                bool success = controller.TryCompleteHullRepairFromPawn(
                    this.pawn,
                    this.plannedRepairHitPoints,
                    this.EnsureMaterialLedger(),
                    out message);
                Messages.Message(
                    message ?? "CT_Shuttle_HullRepair_Failed".Translate().ToString(),
                    success ? MessageTypeDefOf.PositiveEvent : MessageTypeDefOf.RejectInput,
                    false);
                if (!success)
                {
                    this.EndJobWith(JobCondition.Incompletable);
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }

        private bool CanStillRepairHull()
        {
            ShuttleController controller;
            string reason;
            return this.TryGetController(out controller) &&
                controller.CanPawnContinueHullRepair(this.pawn, out reason);
        }

        private bool TryGetRepairPlan(out float repairHitPoints, out int workTicks, out string reason)
        {
            repairHitPoints = 0f;
            workTicks = 0;
            ShuttleController controller;
            if (!this.TryGetController(out controller))
            {
                reason = "CT_Shuttle_HullRepair_NoController".Translate().ToString();
                return false;
            }

            return controller.TryGetHullRepairPlanForPawn(
                this.pawn,
                out repairHitPoints,
                out workTicks,
                out reason);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(
                ref this.plannedRepairHitPoints,
                "plannedRepairHitPoints",
                0f);
            Scribe_Values.Look(ref this.plannedWorkTicks, "plannedWorkTicks", 0);
            Scribe_Values.Look(ref this.materialsLoaded, "materialsLoaded", false);
            Scribe_Deep.Look(ref this.stagedMaterials, "stagedMaterialLedger");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.EnsureMaterialLedger();
            }
        }

        private ShuttleHullRepairMaterialLedger EnsureMaterialLedger()
        {
            if (this.stagedMaterials == null)
            {
                this.stagedMaterials = new ShuttleHullRepairMaterialLedger();
            }

            this.stagedMaterials.EnsureInitialized();
            return this.stagedMaterials;
        }

        private void ReleaseRepairMaterialsOnExit(JobCondition condition)
        {
            if (condition != JobCondition.Succeeded &&
                this.pawn != null &&
                this.pawn.carryTracker != null &&
                this.pawn.carryTracker.CarriedThing != null)
            {
                Thing dropped;
                this.pawn.carryTracker.TryDropCarriedThing(
                    this.pawn.Position,
                    ThingPlaceMode.Near,
                    out dropped);
            }

            this.EnsureMaterialLedger().ReleaseForNormalHauling();
        }

        private bool TryGetController(out ShuttleController controller)
        {
            controller = null;
            ThingWithComps shuttleWithComps = this.ShuttleHost as ThingWithComps;
            CompModularShuttleCore core = shuttleWithComps != null
                ? shuttleWithComps.TryGetComp<CompModularShuttleCore>()
                : null;
            controller = core != null ? core.Controller : null;
            return controller != null;
        }
    }
}
