using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.MechCharging;
using CeleTech.ShuttleExtension.ModularShuttle.Medical;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    internal sealed class MedicalBayPatientSnapshotBuilder
    {
        private const int MaxHediffReadModels = 10;
        private const float SevereBleedRateThreshold = 0.30f;

        private readonly MedicalBayResourceReadModelBuilder resourceBuilder;
        private readonly VirtualMedicalBaySurgeryService surgeryService =
            new VirtualMedicalBaySurgeryService();

        internal MedicalBayPatientSnapshotBuilder(MedicalBayResourceReadModelBuilder resourceBuilder)
        {
            this.resourceBuilder = resourceBuilder;
        }

        internal ShuttleMedicalPatientReadModel BuildPatientModel(
            ShuttleMedicalPatientSnapshot snapshot,
            MedicalBayProfile medicalBay,
            bool medicalBayPowered,
            ThingWithComps shuttleHost,
            bool includeSurgeryOptions = true)
        {
            Pawn pawn = snapshot.Pawn;
            ShuttleMedicalPatientReadModel model = new ShuttleMedicalPatientReadModel();
            model.PawnThingID = snapshot.PawnThingID;
            model.PawnLabel = !string.IsNullOrEmpty(snapshot.PawnLabel)
                ? snapshot.PawnLabel
                : (pawn != null ? pawn.LabelShort : string.Empty);
            model.DisplayThing = pawn;
            model.IsDowned = pawn != null && pawn.Downed;
            model.ConsciousnessPct = this.GetCapacityPct(pawn, PawnCapacityDefOf.Consciousness);
            model.IsConsciousKnown = model.ConsciousnessPct >= 0f;
            // Unknown consciousness stays display-safe as conscious=true so old/missing
            // pawn capacity data does not present a false coma state in the read model.
            model.IsConscious = !model.IsConsciousKnown || model.ConsciousnessPct > 0.05f;
            model.PainPct = this.GetPainPct(pawn);
            this.FillHealthReadModel(pawn, model);
            model.BleedingLabel = model.IsBleeding
                ? "CT_Shuttle_Medevac_ReasonBleeding".Translate().ToString()
                : string.Empty;
            model.FoodPct = this.GetFoodPct(pawn);
            model.RestPct = this.GetRestPct(pawn);
            model.JoyPct = this.GetJoyPct(pawn);
            model.MoodPct = this.GetMoodPct(pawn);
            this.FillMechVitalsReadModel(pawn, model);
            model.AdmissionMode = snapshot.AdmissionMode;
            model.AdmissionTick = snapshot.AdmissionTick;
            model.SupportsPassiveComfort = medicalBay != null && medicalBay.SupportsPassiveComfort;
            model.PassiveJoyCapPct = model.SupportsPassiveComfort ? medicalBay.PassiveJoyCapPct : -1f;
            model.PassiveJoyInactiveReason =
                MedicalBayComfortUtility.GetPassiveJoyInactiveReason(pawn, medicalBay, medicalBayPowered);
            model.PassiveJoyActive = string.IsNullOrEmpty(model.PassiveJoyInactiveReason);
            model.PassiveMoodComfortInactiveReason =
                MedicalBayComfortUtility.GetPassiveMoodComfortInactiveReason(pawn, medicalBay, medicalBayPowered);
            model.PassiveMoodComfortActive = string.IsNullOrEmpty(model.PassiveMoodComfortInactiveReason);
            model.PassiveComfortActive = model.PassiveJoyActive || model.PassiveMoodComfortActive;
            model.PassiveComfortInactiveReason = model.PassiveComfortActive
                ? string.Empty
                : MedicalBayComfortUtility.GetPassiveComfortInactiveReason(pawn, medicalBay, medicalBayPowered);
            this.resourceBuilder.FillPatientCargoMedicineReadModel(pawn, model, shuttleHost);
            model.SurgeryOptions = includeSurgeryOptions
                ? this.BuildSurgeryOptions(pawn, shuttleHost, medicalBayPowered)
                : new List<ShuttleMedicalSurgeryOptionReadModel>();
            return model;
        }

        private IReadOnlyList<ShuttleMedicalSurgeryOptionReadModel> BuildSurgeryOptions(
            Pawn pawn,
            ThingWithComps shuttleHost,
            bool medicalBayPowered)
        {
            List<ShuttleMedicalSurgeryOptionReadModel> options =
                new List<ShuttleMedicalSurgeryOptionReadModel>();
            if (!this.IsHumanSurgeryPatient(pawn) ||
                pawn.def == null ||
                pawn.def.AllRecipes == null ||
                this.surgeryService == null)
            {
                return options;
            }

            List<RecipeDef> recipes = pawn.def.AllRecipes;
            for (int i = 0; i < recipes.Count; i++)
            {
                RecipeDef recipe = recipes[i];
                if (!this.surgeryService.IsSupportedSurgeryRecipe(recipe))
                {
                    continue;
                }

                if (recipe.targetsBodyPart)
                {
                    IEnumerable<BodyPartRecord> parts = recipe.Worker != null
                        ? recipe.Worker.GetPartsToApplyOn(pawn, recipe)
                        : null;
                    if (parts == null)
                    {
                        continue;
                    }

                    foreach (BodyPartRecord part in parts)
                    {
                        ShuttleMedicalSurgeryOptionReadModel option =
                            this.BuildSurgeryOption(pawn, shuttleHost, medicalBayPowered, recipe, part);
                        if (option != null)
                        {
                            options.Add(option);
                        }
                    }

                    continue;
                }

                if (pawn.health != null &&
                    pawn.health.hediffSet != null &&
                    recipe.addsHediff != null &&
                    pawn.health.hediffSet.HasHediff(recipe.addsHediff, false))
                {
                    continue;
                }

                ShuttleMedicalSurgeryOptionReadModel noPartOption =
                    this.BuildSurgeryOption(pawn, shuttleHost, medicalBayPowered, recipe, null);
                if (noPartOption != null)
                {
                    options.Add(noPartOption);
                }
            }

            return options;
        }

        private ShuttleMedicalSurgeryOptionReadModel BuildSurgeryOption(
            Pawn pawn,
            ThingWithComps shuttleHost,
            bool medicalBayPowered,
            RecipeDef recipe,
            BodyPartRecord part)
        {
            if (pawn == null || recipe == null || recipe.Worker == null)
            {
                return null;
            }

            AcceptanceReport report = recipe.Worker.AvailableReport(pawn, part);
            bool availableNow = recipe.AvailableOnNow(pawn, part);
            if (!availableNow && report.Reason.NullOrEmpty())
            {
                return null;
            }

            ShuttleMedicalSurgeryOptionReadModel option =
                new ShuttleMedicalSurgeryOptionReadModel();
            option.RecipeDefName = recipe.defName;
            option.BodyPartIndex = this.GetBodyPartIndex(pawn, part);
            option.BodyPartLabel = part != null && !recipe.hideBodyPartNames
                ? part.Label
                : string.Empty;
            option.Label = this.GetSurgeryOptionLabel(pawn, recipe, part);
            option.Description = recipe.description ?? string.Empty;
            option.WorkTicks = this.surgeryService.GetSurgeryWorkTicks(recipe, pawn);
            option.RequiredMaterialsSummary = this.surgeryService.BuildRequiredMaterialsSummary(
                recipe,
                shuttleHost,
                pawn);
            option.MissingMaterialsSummary = this.surgeryService.BuildMissingMaterialsSummary(
                recipe,
                shuttleHost,
                pawn);
            option.HasMissingMaterials = !string.IsNullOrEmpty(option.MissingMaterialsSummary);
            option.CanSchedule = true;
            option.DisabledReason = string.Empty;

            if (!medicalBayPowered)
            {
                option.CanSchedule = false;
                option.DisabledReason = "CT_Shuttle_MedicalBay_Unpowered".Translate().ToString();
                return option;
            }

            if (!availableNow || !report.Accepted)
            {
                option.CanSchedule = false;
                option.DisabledReason = !report.Reason.NullOrEmpty()
                    ? report.Reason
                    : "CT_Shuttle_MedicalSurgery_InvalidRecipe".Translate().ToString();
                return option;
            }

            if (this.surgeryService.HasUnsupportedExtraIngredients(recipe))
            {
                option.CanSchedule = false;
                option.DisabledReason =
                    "CT_Shuttle_MedicalSurgery_IngredientUnsupported".Translate().ToString();
                return option;
            }

            if (option.HasMissingMaterials)
            {
                option.CanSchedule = false;
                option.DisabledReason =
                    "CT_Shuttle_MedicalSurgery_MissingMaterials"
                        .Translate(option.MissingMaterialsSummary)
                        .ToString();
                return option;
            }

            return option;
        }

        private bool IsHumanSurgeryPatient(Pawn pawn)
        {
            return pawn != null &&
                !pawn.Destroyed &&
                !pawn.Dead &&
                pawn.RaceProps != null &&
                pawn.RaceProps.Humanlike;
        }

        private string GetSurgeryOptionLabel(
            Pawn pawn,
            RecipeDef recipe,
            BodyPartRecord part)
        {
            string label = recipe != null && recipe.Worker != null
                ? recipe.Worker.GetLabelWhenUsedOn(pawn, part).CapitalizeFirst()
                : string.Empty;
            if (string.IsNullOrEmpty(label) && recipe != null)
            {
                label = recipe.LabelCap.ToString();
            }

            return label ?? string.Empty;
        }

        private int GetBodyPartIndex(Pawn pawn, BodyPartRecord part)
        {
            if (pawn == null || part == null || pawn.RaceProps == null || pawn.RaceProps.body == null)
            {
                return -1;
            }

            List<BodyPartRecord> parts = pawn.RaceProps.body.AllParts;
            return parts != null ? parts.IndexOf(part) : -1;
        }

        private void FillMechVitalsReadModel(
            Pawn pawn,
            ShuttleMedicalPatientReadModel model)
        {
            if (pawn == null ||
                pawn.RaceProps == null ||
                !pawn.RaceProps.IsMechanoid ||
                model == null)
            {
                return;
            }

            float durabilityPct = this.GetSummaryHealthPct(pawn);
            if (durabilityPct >= 0f)
            {
                model.HasMechDurability = true;
                model.DurabilityPct = durabilityPct;
                model.DamagePct = this.Clamp01(1f - durabilityPct);
            }

            float energyPct = this.GetMechEnergyPct(pawn);
            if (energyPct >= 0f)
            {
                model.HasMechEnergy = true;
                model.EnergyPct = energyPct;
            }
        }

        private void FillHealthReadModel(Pawn pawn, ShuttleMedicalPatientReadModel model)
        {
            if (model == null)
            {
                return;
            }

            model.BleedRateTotal = this.GetBleedRateTotal(pawn);

            List<HediffReadEntry> hediffEntries = new List<HediffReadEntry>();
            int infectionLikeCount = 0;
            if (pawn != null &&
                pawn.health != null &&
                pawn.health.hediffSet != null &&
                pawn.health.hediffSet.hediffs != null)
            {
                for (int i = 0; i < pawn.health.hediffSet.hediffs.Count; i++)
                {
                    Hediff hediff = pawn.health.hediffSet.hediffs[i];
                    if (hediff == null)
                    {
                        continue;
                    }

                    HediffReadEntry entry = this.BuildHediffReadEntry(hediff);
                    if (entry == null || entry.Model == null)
                    {
                        continue;
                    }

                    if (entry.Model.Tendable)
                    {
                        model.TendableHediffCount++;
                    }

                    if (entry.Model.Tended)
                    {
                        model.TendedHediffCount++;
                    }
                    else if (entry.Model.Tendable)
                    {
                        model.UntendedHediffCount++;
                    }

                    if (entry.Model.Bleeding)
                    {
                        model.BleedingHediffCount++;
                    }

                    if (entry.InfectionLike)
                    {
                        infectionLikeCount++;
                    }

                    if (hediff is Hediff_Injury)
                    {
                        if (entry.Model.Tended)
                        {
                            model.HasTendedInjury = true;
                        }
                        else if (entry.Model.Tendable)
                        {
                            model.HasUntendedInjury = true;
                        }
                    }

                    hediffEntries.Add(entry);
                }
            }

            hediffEntries.Sort(this.CompareHediffReadEntries);
            model.Hediffs.Clear();
            int limit = hediffEntries.Count < MaxHediffReadModels
                ? hediffEntries.Count
                : MaxHediffReadModels;
            for (int i = 0; i < limit; i++)
            {
                model.Hediffs.Add(hediffEntries[i].Model);
            }

            model.IsBleeding = model.BleedRateTotal > 0f || model.BleedingHediffCount > 0;
            model.NeedsTend = model.UntendedHediffCount > 0;
            model.HasInfectionLikeHediff = infectionLikeCount > 0;
            model.HasMedicalEmergency =
                model.IsDowned ||
                model.BleedRateTotal >= SevereBleedRateThreshold ||
                model.NeedsTend ||
                model.HasInfectionLikeHediff ||
                (model.ConsciousnessPct >= 0f && model.ConsciousnessPct < 0.45f) ||
                model.PainPct >= 0.75f;
            model.MedicalSummaryLabel = this.BuildMedicalSummaryLabel(model, infectionLikeCount);
            model.TendSummaryLabel = "CT_Shuttle_MedicalBay_TendSummary".Translate(
                model.TendableHediffCount,
                model.UntendedHediffCount,
                model.TendedHediffCount).ToString();
            model.InfectionSummaryLabel = infectionLikeCount > 0
                ? "CT_Shuttle_MedicalBay_HealthInfection".Translate(infectionLikeCount).ToString()
                : string.Empty;
            model.MedicineNeedLabel = this.BuildMedicineNeedLabel(model);
        }

        private HediffReadEntry BuildHediffReadEntry(Hediff hediff)
        {
            if (hediff == null)
            {
                return null;
            }

            ShuttleMedicalHediffReadModel model = new ShuttleMedicalHediffReadModel();
            model.Label = !string.IsNullOrEmpty(hediff.LabelCap)
                ? hediff.LabelCap
                : (!string.IsNullOrEmpty(hediff.Label) ? hediff.Label : string.Empty);
            model.PartLabel = hediff.Part != null
                ? (!string.IsNullOrEmpty(hediff.Part.LabelCap) ? hediff.Part.LabelCap : hediff.Part.Label)
                : string.Empty;
            model.Bleeding = hediff.Bleeding;
            model.BleedRate = hediff.BleedRate;
            model.Severity = hediff.Severity;
            model.Tended = this.IsHediffTended(hediff);
            model.Tendable = this.IsHediffTendable(hediff);
            model.SummaryLabel = this.BuildHediffSummaryLabel(model);

            return new HediffReadEntry
            {
                Model = model,
                InfectionLike = this.IsInfectionLikeHediff(hediff),
                SortLabel = model.Label
            };
        }

        private bool IsHediffTendable(Hediff hediff)
        {
            if (hediff == null)
            {
                return false;
            }

            return hediff.TendableNow(false);
        }

        private bool IsHediffTended(Hediff hediff)
        {
            HediffComp_TendDuration tendComp = this.GetTendDurationComp(hediff);
            return tendComp != null && tendComp.IsTended;
        }

        private HediffComp_TendDuration GetTendDurationComp(Hediff hediff)
        {
            HediffWithComps withComps = hediff as HediffWithComps;
            return withComps != null ? withComps.GetComp<HediffComp_TendDuration>() : null;
        }

        private bool IsInfectionLikeHediff(Hediff hediff)
        {
            HediffWithComps withComps = hediff as HediffWithComps;
            return withComps != null && withComps.GetComp<HediffComp_Immunizable>() != null;
        }

        private string BuildMedicalSummaryLabel(
            ShuttleMedicalPatientReadModel model,
            int infectionLikeCount)
        {
            if (model == null)
            {
                return string.Empty;
            }

            if (model.IsBleeding)
            {
                return "CT_Shuttle_MedicalBay_HealthBleeding".Translate(
                    model.BleedingHediffCount,
                    this.FormatHealthNumber(model.BleedRateTotal)).ToString();
            }

            if (model.UntendedHediffCount > 0)
            {
                return "CT_Shuttle_MedicalBay_HealthNeedsTend".Translate(
                    model.UntendedHediffCount).ToString();
            }

            if (infectionLikeCount > 0)
            {
                return "CT_Shuttle_MedicalBay_HealthInfection".Translate(
                    infectionLikeCount).ToString();
            }

            return "CT_Shuttle_MedicalBay_HealthStable".Translate().ToString();
        }

        private string BuildMedicineNeedLabel(ShuttleMedicalPatientReadModel model)
        {
            if (model == null)
            {
                return string.Empty;
            }

            if (model.BleedRateTotal >= SevereBleedRateThreshold ||
                model.UntendedHediffCount >= 2 ||
                (model.IsDowned && model.NeedsTend))
            {
                return "CT_Shuttle_MedicalBay_MedicineRequiredSoon".Translate().ToString();
            }

            if (model.NeedsTend || model.IsBleeding || model.HasInfectionLikeHediff)
            {
                return "CT_Shuttle_MedicalBay_MedicineRecommended".Translate().ToString();
            }

            return "CT_Shuttle_MedicalBay_MedicineNotNeeded".Translate().ToString();
        }

        private string BuildHediffSummaryLabel(ShuttleMedicalHediffReadModel model)
        {
            if (model == null)
            {
                return string.Empty;
            }

            List<string> tags = new List<string>();
            if (model.Bleeding)
            {
                tags.Add("CT_Shuttle_MedicalBay_HediffBleeding".Translate().ToString());
            }

            if (model.Tended)
            {
                tags.Add("CT_Shuttle_MedicalBay_HediffTended".Translate().ToString());
            }
            else if (model.Tendable)
            {
                tags.Add("CT_Shuttle_MedicalBay_HediffUntended".Translate().ToString());
            }

            if (!string.IsNullOrEmpty(model.PartLabel))
            {
                tags.Add(model.PartLabel);
            }

            return tags.Count > 0
                ? model.Label + " (" + string.Join(", ", tags.ToArray()) + ")"
                : model.Label;
        }

        private int CompareHediffReadEntries(HediffReadEntry left, HediffReadEntry right)
        {
            if (left == null || right == null)
            {
                return left == right ? 0 : (left == null ? 1 : -1);
            }

            int result = this.CompareBoolDescending(left.Model.Bleeding, right.Model.Bleeding);
            if (result != 0)
            {
                return result;
            }

            bool leftUntended = left.Model.Tendable && !left.Model.Tended;
            bool rightUntended = right.Model.Tendable && !right.Model.Tended;
            result = this.CompareBoolDescending(leftUntended, rightUntended);
            if (result != 0)
            {
                return result;
            }

            result = this.CompareBoolDescending(left.InfectionLike, right.InfectionLike);
            if (result != 0)
            {
                return result;
            }

            result = right.Model.Severity.CompareTo(left.Model.Severity);
            if (result != 0)
            {
                return result;
            }

            return string.Compare(
                left.SortLabel,
                right.SortLabel,
                StringComparison.OrdinalIgnoreCase);
        }

        private int CompareBoolDescending(bool left, bool right)
        {
            if (left == right)
            {
                return 0;
            }

            return left ? -1 : 1;
        }

        private string FormatHealthNumber(float value)
        {
            return value.ToString("0.##");
        }

        private float GetCapacityPct(Pawn pawn, PawnCapacityDef capacityDef)
        {
            if (pawn == null || pawn.health == null || pawn.health.capacities == null || capacityDef == null)
            {
                return -1f;
            }

            return pawn.health.capacities.GetLevel(capacityDef);
        }

        private float GetPainPct(Pawn pawn)
        {
            if (pawn == null || pawn.health == null || pawn.health.hediffSet == null)
            {
                return -1f;
            }

            return pawn.health.hediffSet.PainTotal;
        }

        private float GetBleedRateTotal(Pawn pawn)
        {
            if (pawn == null || pawn.health == null || pawn.health.hediffSet == null)
            {
                return 0f;
            }

            return pawn.health.hediffSet.BleedRateTotal;
        }

        private float GetFoodPct(Pawn pawn)
        {
            return pawn != null && pawn.needs != null && pawn.needs.food != null
                ? pawn.needs.food.CurLevelPercentage
                : -1f;
        }

        private float GetRestPct(Pawn pawn)
        {
            return pawn != null && pawn.needs != null && pawn.needs.rest != null
                ? pawn.needs.rest.CurLevelPercentage
                : -1f;
        }

        private float GetJoyPct(Pawn pawn)
        {
            return pawn != null && pawn.needs != null && pawn.needs.joy != null
                ? pawn.needs.joy.CurLevelPercentage
                : -1f;
        }

        private float GetMoodPct(Pawn pawn)
        {
            return pawn != null && pawn.needs != null && pawn.needs.mood != null
                ? pawn.needs.mood.CurLevelPercentage
                : -1f;
        }

        private float GetSummaryHealthPct(Pawn pawn)
        {
            if (pawn == null ||
                pawn.health == null ||
                pawn.health.summaryHealth == null)
            {
                return -1f;
            }

            return this.Clamp01(pawn.health.summaryHealth.SummaryHealthPercent);
        }

        private float GetMechEnergyPct(Pawn pawn)
        {
            return ShuttleMechChargeNeedUtility.GetChargeNeedPct(pawn);
        }

        private float Clamp01(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return -1f;
            }

            if (value < 0f)
            {
                return 0f;
            }

            if (value > 1f)
            {
                return 1f;
            }

            return value;
        }

        private sealed class HediffReadEntry
        {
            internal ShuttleMedicalHediffReadModel Model;
            internal bool InfectionLike;
            internal string SortLabel;
        }
    }
}
