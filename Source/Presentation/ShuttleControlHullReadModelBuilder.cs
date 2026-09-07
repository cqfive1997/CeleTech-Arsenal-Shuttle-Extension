using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Profile;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Hull;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Presentation
{
    internal sealed class ShuttleControlHullReadModelBuilder
    {
        private readonly ShuttleHullRepairService hullRepairService = new ShuttleHullRepairService();

        internal void ApplyHull(
            ShuttleControlReadModel model,
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            ShuttleAssemblyState assemblyState)
        {
            if (model == null)
            {
                return;
            }

            model.Hull = new ShuttleHullReadModel();
            if (profile == null || profile.Hull == null || profile.Hull.MaxHitPoints <= 0)
            {
                model.Hull.HasHull = false;
                model.Hull.StatusKey = "Missing";
                model.Hull.StatusLabel = "CT_Shuttle_Hull_Status_Missing".Translate().ToString();
                return;
            }

            int maxHitPoints = Mathf.Max(0, profile.Hull.MaxHitPoints);
            float currentHitPoints = 0f;
            if (runtimeState != null)
            {
                runtimeState.EnsureInitialized();
                currentHitPoints = runtimeState.Hull != null
                    ? runtimeState.Hull.CurrentHitPoints
                    : 0f;
            }

            currentHitPoints = Mathf.Clamp(currentHitPoints, 0f, maxHitPoints);
            float integrityPct = maxHitPoints > 0
                ? Mathf.Clamp01(currentHitPoints / maxHitPoints)
                : 0f;
            float armorProtection01 = this.GetHullArmorProtection01(profile.Hull);
            string statusKey = this.GetHullStatusKey(
                currentHitPoints,
                integrityPct,
                profile.Hull.MinLaunchIntegrityPct);

            model.Hull.HasHull = true;
            model.Hull.CurrentHitPoints = currentHitPoints;
            model.Hull.MaxHitPoints = maxHitPoints;
            model.Hull.IntegrityPct = integrityPct;
            model.Hull.IntegrityPctRounded = Mathf.RoundToInt(integrityPct * 100f);
            model.Hull.ArmorModuleCount = profile.Hull.ArmorModuleCount;
            model.Hull.ArmorProtection01 = armorProtection01;
            model.Hull.ArmorProtectionPctRounded = Mathf.RoundToInt(armorProtection01 * 100f);
            model.Hull.SharpDamageMultiplier = profile.Hull.SharpDamageMultiplier;
            model.Hull.BluntDamageMultiplier = profile.Hull.BluntDamageMultiplier;
            model.Hull.HeatDamageMultiplier = profile.Hull.HeatDamageMultiplier;
            model.Hull.ExplosionDamageMultiplier = profile.Hull.ExplosionDamageMultiplier;
            model.Hull.EmpDamageMultiplier = profile.Hull.EmpDamageMultiplier;
            model.Hull.FlatDamageReduction = profile.Hull.FlatDamageReduction;
            model.Hull.StatusKey = statusKey;
            model.Hull.StatusLabel = ("CT_Shuttle_Hull_Status_" + statusKey).Translate().ToString();
            if (statusKey == "Critical" || statusKey == "Breached")
            {
                model.Hull.WarningLabel =
                    "CT_Shuttle_Hull_EmergencyLaunchAllowedWarning".Translate().ToString();
            }

            ShuttleHullRepairPlan repairPlan;
            if (this.hullRepairService.TryBuildRepairPlan(
                profile,
                runtimeState,
                assemblyState,
                out repairPlan))
            {
                model.Hull.NeedsRepair = true;
                model.Hull.MissingHitPoints = repairPlan.RepairHitPoints;
                model.Hull.RepairCostSummary = repairPlan.CostSummary;
                model.Hull.RepairWorkTicks = repairPlan.WorkTicks;
            }
            else
            {
                model.Hull.NeedsRepair = false;
                model.Hull.MissingHitPoints = 0f;
                model.Hull.RepairCostSummary =
                    "CT_Shuttle_HullRepair_CostSummaryEmpty".Translate().ToString();
                model.Hull.RepairWorkTicks = 0;
            }
        }

        private string GetHullStatusKey(
            float currentHitPoints,
            float integrityPct,
            float warningThreshold)
        {
            float threshold = warningThreshold > 0f ? Mathf.Clamp01(warningThreshold) : 0.25f;
            if (currentHitPoints <= 0f)
            {
                return "Breached";
            }

            if (integrityPct < threshold)
            {
                return "Critical";
            }

            if (integrityPct < 0.75f)
            {
                return "Damaged";
            }

            return "Nominal";
        }

        private float GetHullArmorProtection01(HullProfile hull)
        {
            if (hull == null)
            {
                return 0f;
            }

            float bestMultiplier = 1f;
            bestMultiplier = Mathf.Min(bestMultiplier, this.SanitizeHullDamageMultiplier(hull.SharpDamageMultiplier));
            bestMultiplier = Mathf.Min(bestMultiplier, this.SanitizeHullDamageMultiplier(hull.BluntDamageMultiplier));
            bestMultiplier = Mathf.Min(bestMultiplier, this.SanitizeHullDamageMultiplier(hull.HeatDamageMultiplier));
            bestMultiplier = Mathf.Min(bestMultiplier, this.SanitizeHullDamageMultiplier(hull.ExplosionDamageMultiplier));
            bestMultiplier = Mathf.Min(bestMultiplier, this.SanitizeHullDamageMultiplier(hull.EmpDamageMultiplier));
            return Mathf.Clamp01(1f - bestMultiplier);
        }

        private float SanitizeHullDamageMultiplier(float multiplier)
        {
            if (float.IsNaN(multiplier) || float.IsInfinity(multiplier) || multiplier <= 0f)
            {
                return 1f;
            }

            return multiplier;
        }
    }
}
