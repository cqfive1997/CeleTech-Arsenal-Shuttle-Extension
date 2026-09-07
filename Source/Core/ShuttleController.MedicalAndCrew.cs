using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Medical;
using CeleTech.ShuttleExtension.ModularShuttle.Profile;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Hull;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Medical;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Shield;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Shield.Surface;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using RimWorld;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    public sealed partial class ShuttleController
    {
        internal bool TryConsumeShieldRechargeEnergy(float amountWd)
        {
            this.EnsureRuntimeState();
            return this.powerSystem.TryConsumeStoredEnergyWd(this.runtimeState, amountWd);
        }

        internal bool TryGetActiveSurfaceShieldStatus(
            out ShuttleSurfaceShieldStatusSnapshot snapshot)
        {
            this.EnsureRuntimeState();
            int ticksGame = ShuttleTickUtility.TicksGameOrZero();
            return SharedSurfaceShieldRuntimeService.TryGetActiveSurfaceShieldStatus(
                this.shuttleHost,
                this.assemblyState,
                this.runtimeState,
                ticksGame,
                out snapshot);
        }

        internal bool TryGetActiveSurfaceShieldVisualConfig(
            out ShuttleSurfaceShieldVisualConfigSnapshot snapshot)
        {
            this.EnsureRuntimeState();
            int ticksGame = ShuttleTickUtility.TicksGameOrZero();
            return SharedSurfaceShieldRuntimeService.TryGetActiveSurfaceShieldVisualConfig(
                this.shuttleHost,
                this.assemblyState,
                this.runtimeState,
                ticksGame,
                out snapshot);
        }

        internal bool TrySetActiveSurfaceShieldRechargeSpeed(float multiplier)
        {
            this.EnsureRuntimeState();
            return SharedSurfaceShieldRuntimeService.TrySetActiveSurfaceShieldRechargeSpeed(
                this.shuttleHost,
                this.assemblyState,
                this.runtimeState,
                multiplier);
        }

        internal bool TryAbsorbIncomingSurfaceShieldDamage(
            ref DamageInfo dinfo,
            out ShuttleSurfaceShieldAbsorbResult result)
        {
            this.EnsureRuntimeState();
            int ticksGame = ShuttleTickUtility.TicksGameOrZero();
            return SharedSurfaceShieldRuntimeService.TryAbsorbIncomingDamage(
                this.shuttleHost,
                this.assemblyState,
                this.runtimeState,
                ref dinfo,
                ticksGame,
                out result);
        }

        internal bool TryApplyIncomingHullDamage(
            ref DamageInfo dinfo,
            out ShuttleHullDamageResult result)
        {
            ShuttleProfile currentProfile = this.EnsureProfile();
            this.EnsureRuntimeState();
            int ticksGame = ShuttleTickUtility.TicksGameOrZero();
            return SharedHullIntegrityService.TryApplyIncomingDamage(
                currentProfile,
                this.runtimeState,
                ref dinfo,
                ticksGame,
                out result);
        }

        internal bool HasActiveMedicalProcedure()
        {
            this.EnsureRuntimeState();
            if (this.runtimeState.MedicalProcedures.HasActiveProcedure)
            {
                return true;
            }

            return SharedMedicalBayProcedureService.HasActiveProcedure(this.shuttleHost);
        }

        internal bool HasActiveMedicalProcedureForPatient(int patientThingID)
        {
            if (patientThingID <= 0)
            {
                return false;
            }

            this.EnsureRuntimeState();
            return this.runtimeState.MedicalProcedures != null &&
                this.runtimeState.MedicalProcedures.HasActiveProcedureForPatient(patientThingID);
        }

        internal bool TryDevStartMedicalProcedure(
            Pawn doctor,
            int patientThingID,
            out string message)
        {
            message = null;
            if (!Prefs.DevMode)
            {
                message = "[CeleTech Shuttle] Medical procedure test entry is DevMode-only.";
                return false;
            }

            MedicalBayProcedureRecord record;
            string reason;
            if (!SharedMedicalBayProcedureService.TryStartTendProcedure(
                this.shuttleHost,
                doctor,
                patientThingID,
                false,
                out record,
                out reason))
            {
                message = reason ?? "CT_Shuttle_MedicalProcedure_DoctorEnterFailed".Translate().ToString();
                return false;
            }

            message = "[CeleTech Shuttle] Medical procedure test started. procedureID=" +
                (record != null ? record.ProcedureID.ToString() : "unknown");
            return true;
        }

        internal bool TryStartMedicalBayTendProcedureFromPawn(
            Pawn doctor,
            int patientThingID,
            bool useAvailableMedicine,
            out string message)
        {
            message = null;
            MedicalBayProcedureRecord record;
            string reason;
            if (!SharedMedicalBayProcedureService.TryStartTendProcedure(
                this.shuttleHost,
                doctor,
                patientThingID,
                useAvailableMedicine,
                out record,
                out reason))
            {
                message = reason ?? "CT_Shuttle_MedicalProcedure_DoctorEnterFailed".Translate().ToString();
                return false;
            }

            message = "CT_Shuttle_MedicalProcedure_TendStarted".Translate().ToString();
            return true;
        }

        internal bool TryStartMedicalBaySurgeryProcedureFromPawn(
            Pawn doctor,
            int patientThingID,
            string recipeDefName,
            int bodyPartIndex,
            out string message)
        {
            message = null;
            MedicalBayProcedureRecord record;
            string reason;
            if (!SharedMedicalBayProcedureService.TryStartSurgeryProcedure(
                this.shuttleHost,
                doctor,
                patientThingID,
                recipeDefName,
                bodyPartIndex,
                out record,
                out reason))
            {
                message = reason ?? "CT_Shuttle_MedicalSurgery_Failed".Translate().ToString();
                return false;
            }

            message = "CT_Shuttle_MedicalSurgery_Started".Translate().ToString();
            return true;
        }

        internal bool TryDevExitAllMedicalProcedureDoctors(out string message)
        {
            message = null;
            if (!Prefs.DevMode)
            {
                message = "[CeleTech Shuttle] Medical procedure doctor exit is DevMode-only.";
                return false;
            }

            string reason;
            if (!SharedMedicalBayProcedureService.TryCancelAllProceduresAndExitDoctors(
                this.shuttleHost,
                out reason))
            {
                message = reason ?? "CT_Shuttle_MedicalProcedure_DoctorExitFailed".Translate().ToString();
                return false;
            }

            message = "[CeleTech Shuttle] Medical procedure doctors returned from the medical bay and active procedure state was cancelled.";
            return true;
        }

        internal bool TryCancelMedicalBayProcedure(
            int procedureID,
            out string message)
        {
            message = null;
            this.EnsureRuntimeState();

            string reason;
            bool success = procedureID > 0
                ? SharedMedicalBayProcedureService.TryCancelProcedure(
                    this.shuttleHost,
                    procedureID,
                    out reason)
                : SharedMedicalBayProcedureService.TryCancelAllProceduresAndExitDoctors(
                    this.shuttleHost,
                    out reason);
            if (!success)
            {
                message = reason ?? "CT_Shuttle_MedicalProcedure_CancelFailed".Translate().ToString();
                return false;
            }

            message = "CT_Shuttle_MedicalProcedure_Cancelled".Translate().ToString();
            return true;
        }

        internal bool CanPawnRepairHull(Pawn pawn, out string reason)
        {
            ShuttleHullRepairPlan plan;
            return this.CanPawnRepairHull(pawn, true, out plan, out reason);
        }

        internal bool CanPawnContinueHullRepair(Pawn pawn, out string reason)
        {
            ShuttleHullRepairPlan plan;
            return this.CanPawnRepairHull(pawn, false, out plan, out reason);
        }

        private bool CanPawnRepairHull(
            Pawn pawn,
            bool requireMaterials,
            out ShuttleHullRepairPlan plan,
            out string reason)
        {
            plan = null;
            reason = null;
            if (this.shuttleHost == null || this.shuttleHost.Destroyed)
            {
                reason = "CT_Shuttle_HullRepair_NoController".Translate().ToString();
                return false;
            }

            if (pawn == null || pawn.Destroyed || pawn.Dead || pawn.Downed || !pawn.IsColonistPlayerControlled)
            {
                reason = "CT_Shuttle_HullRepair_Failed".Translate().ToString();
                return false;
            }

            ShuttleProfile currentProfile = this.ReconcileProfileToHost();
            this.EnsureRuntimeState();
            if (!SharedHullRepairService.NeedsRepair(currentProfile, this.runtimeState))
            {
                reason = "CT_Shuttle_HullRepair_NotNeeded".Translate().ToString();
                return false;
            }

            if (!SharedHullRepairService.TryBuildRepairPlan(
                currentProfile,
                this.runtimeState,
                this.assemblyState,
                out plan))
            {
                reason = "CT_Shuttle_HullRepair_NotNeeded".Translate().ToString();
                return false;
            }

            if (pawn.Map == null ||
                this.shuttleHost.Map != pawn.Map ||
                !pawn.CanReserveAndReach(this.shuttleHost, PathEndMode.Touch, Danger.Deadly, 1, 1, null, false))
            {
                reason = "CT_Shuttle_HullRepair_Unreachable".Translate().ToString();
                return false;
            }

            if (requireMaterials &&
                !SharedHullRepairService.HasRepairMaterialsOnMap(
                    pawn,
                    this.shuttleHost,
                    plan.CostList,
                    out reason))
            {
                return false;
            }

            return true;
        }

        internal bool TryCompleteHullRepairFromPawn(
            Pawn pawn,
            float plannedRepairHitPoints,
            ShuttleHullRepairMaterialLedger stagedMaterials,
            out string message)
        {
            string reason;
            ShuttleProfile currentProfile;
            float repairHitPoints;
            List<ThingDefCountClass> costList;
            if (!this.TryResolveActiveHullRepairPlan(
                pawn,
                plannedRepairHitPoints,
                out currentProfile,
                out repairHitPoints,
                out costList,
                out reason))
            {
                message = reason;
                return false;
            }

            string costSummary = SharedHullRepairService.BuildCostSummary(costList);
            float actualRepair;
            if (!SharedHullRepairService.TryApplyRepairWithTrackedMaterials(
                currentProfile,
                this.runtimeState,
                this.shuttleHost,
                repairHitPoints,
                costList,
                stagedMaterials,
                out actualRepair,
                out reason))
            {
                message = reason;
                return false;
            }

            int maxHitPoints = currentProfile != null && currentProfile.Hull != null
                ? currentProfile.Hull.MaxHitPoints
                : 0;
            message = "CT_Shuttle_HullRepair_CompletedWithMaterials".Translate(
                actualRepair.ToString("0.#"),
                this.runtimeState.Hull.CurrentHitPoints.ToString("0.#"),
                maxHitPoints.ToString(),
                costSummary).ToString();
            return true;
        }

        internal bool TryFindNextHullRepairMaterialForPawn(
            Pawn pawn,
            float plannedRepairHitPoints,
            ShuttleHullRepairMaterialLedger stagedMaterials,
            out Thing material,
            out int count,
            out bool materialsLoaded,
            out string reason)
        {
            material = null;
            count = 0;
            materialsLoaded = false;
            ShuttleProfile currentProfile;
            float repairHitPoints;
            List<ThingDefCountClass> costList;
            if (!this.TryResolveActiveHullRepairPlan(
                pawn,
                plannedRepairHitPoints,
                out currentProfile,
                out repairHitPoints,
                out costList,
                out reason))
            {
                return false;
            }

            return SharedHullRepairService.TryFindNextRepairMaterialForHaul(
                pawn,
                this.shuttleHost,
                costList,
                stagedMaterials,
                out material,
                out count,
                out materialsLoaded,
                out reason);
        }

        internal bool TryStageCarriedHullRepairMaterialFromPawn(
            Pawn pawn,
            float plannedRepairHitPoints,
            ShuttleHullRepairMaterialLedger stagedMaterials,
            out string reason)
        {
            ShuttleProfile currentProfile;
            float repairHitPoints;
            List<ThingDefCountClass> costList;
            if (!this.TryResolveActiveHullRepairPlan(
                pawn,
                plannedRepairHitPoints,
                out currentProfile,
                out repairHitPoints,
                out costList,
                out reason))
            {
                return false;
            }

            return SharedHullRepairService.TryStageCarriedRepairMaterial(
                pawn,
                this.shuttleHost,
                costList,
                stagedMaterials,
                out reason);
        }

        internal bool TryGetHullRepairPlanForPawn(
            Pawn pawn,
            out float repairHitPoints,
            out int workTicks,
            out string reason)
        {
            string costSummary;
            return this.TryGetHullRepairPlanForPawn(
                pawn,
                out repairHitPoints,
                out workTicks,
                out costSummary,
                out reason);
        }

        internal bool TryGetHullRepairPlanForPawn(
            Pawn pawn,
            out float repairHitPoints,
            out int workTicks,
            out string costSummary,
            out string reason)
        {
            repairHitPoints = 0f;
            workTicks = 0;
            costSummary = null;
            ShuttleHullRepairPlan plan;
            if (!this.CanPawnRepairHull(pawn, true, out plan, out reason))
            {
                return false;
            }

            repairHitPoints = plan != null ? plan.RepairHitPoints : 0f;
            workTicks = plan != null ? plan.WorkTicks : 0;
            costSummary = plan != null ? plan.CostSummary : null;
            if (repairHitPoints <= 0f || workTicks <= 0)
            {
                reason = "CT_Shuttle_HullRepair_NotNeeded".Translate().ToString();
                repairHitPoints = 0f;
                workTicks = 0;
                costSummary = null;
                return false;
            }

            reason = null;
            return true;
        }

        private bool TryResolveActiveHullRepairPlan(
            Pawn pawn,
            float plannedRepairHitPoints,
            out ShuttleProfile currentProfile,
            out float repairHitPoints,
            out List<ThingDefCountClass> costList,
            out string reason)
        {
            currentProfile = null;
            repairHitPoints = 0f;
            costList = null;
            ShuttleHullRepairPlan currentPlan;
            if (!this.CanPawnContinueHullRepair(pawn, out reason))
            {
                return false;
            }

            currentProfile = this.ReconcileProfileToHost();
            this.EnsureRuntimeState();
            if (!SharedHullRepairService.TryBuildRepairPlan(
                currentProfile,
                this.runtimeState,
                this.assemblyState,
                out currentPlan))
            {
                reason = "CT_Shuttle_HullRepair_NotNeeded".Translate().ToString();
                return false;
            }

            repairHitPoints = plannedRepairHitPoints > 0f
                ? System.Math.Min(plannedRepairHitPoints, currentPlan.RepairHitPoints)
                : currentPlan.RepairHitPoints;
            if (repairHitPoints <= 0f)
            {
                reason = "CT_Shuttle_HullRepair_NotNeeded".Translate().ToString();
                return false;
            }

            costList = SharedHullRepairService.BuildRepairCost(
                this.assemblyState,
                repairHitPoints);
            reason = null;
            return true;
        }
    }
}
