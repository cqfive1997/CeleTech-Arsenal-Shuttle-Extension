using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Medical
{
    /// <summary>
    /// Executes Medical Bay surgery without vanilla bills or vanilla surgery jobs.
    /// The patient remains held by the shuttle Medical Bay; callers must return the
    /// doctor to the map before invoking TryApplySurgery.
    /// </summary>
    internal sealed class VirtualMedicalBaySurgeryService
    {
        internal const int FallbackSurgeryWorkTicks = 2500;
        private const int MaxSurgeryWorkTicks = 30000;

        private readonly MedicalBayLoadedCargoIngredientUtility loadedCargoIngredientUtility =
            new MedicalBayLoadedCargoIngredientUtility();

        internal bool CanApplySurgery(
            ThingWithComps shuttle,
            Pawn doctor,
            Pawn patient,
            RecipeDef recipeDef,
            int bodyPartIndex,
            out string reason)
        {
            reason = null;
            BodyPartRecord part;
            if (!this.TryValidateSurgeryRequest(
                shuttle,
                doctor,
                patient,
                recipeDef,
                bodyPartIndex,
                out part,
                out reason))
            {
                return false;
            }

            List<MedicalBaySurgeryIngredientRequirement> requirements;
            if (!this.TryBuildSurgeryIngredientRequirements(
                recipeDef,
                shuttle,
                patient,
                out requirements,
                out reason))
            {
                return false;
            }

            string missingSummary = this.loadedCargoIngredientUtility != null
                ? this.loadedCargoIngredientUtility.BuildMissingMaterialsSummary(requirements)
                : string.Empty;
            if (!string.IsNullOrEmpty(missingSummary))
            {
                reason = "CT_Shuttle_MedicalSurgery_MissingMaterials".Translate(missingSummary).ToString();
                return false;
            }

            return true;
        }

        internal bool CanApplySurgeryByDefName(
            ThingWithComps shuttle,
            Pawn doctor,
            int patientThingID,
            string recipeDefName,
            int bodyPartIndex,
            out string reason)
        {
            Pawn patient = this.FindPatientByThingID(shuttle, patientThingID);
            RecipeDef recipeDef = this.ResolveRecipeDef(recipeDefName);
            return this.CanApplySurgery(
                shuttle,
                doctor,
                patient,
                recipeDef,
                bodyPartIndex,
                out reason);
        }

        internal bool TryApplySurgery(
            ThingWithComps shuttle,
            Pawn doctor,
            Pawn patient,
            RecipeDef recipeDef,
            int bodyPartIndex,
            out string notice,
            out string reason)
        {
            notice = null;
            BodyPartRecord part;
            if (!this.CanApplySurgery(shuttle, doctor, patient, recipeDef, bodyPartIndex, out reason) ||
                !this.TryResolveBodyPart(patient, recipeDef, bodyPartIndex, out part, out reason))
            {
                return false;
            }

            if (doctor == null || !doctor.Spawned)
            {
                reason = "CT_Shuttle_MedicalProcedure_DoctorExitFailed".Translate().ToString();
                return false;
            }

            List<MedicalBaySurgeryIngredientRequirement> requirements;
            if (!this.TryBuildSurgeryIngredientRequirements(
                recipeDef,
                shuttle,
                patient,
                out requirements,
                out reason))
            {
                return false;
            }

            List<Thing> ingredients;
            MedicalBayLoadedCargoIngredientWithdrawal withdrawal;
            if (this.loadedCargoIngredientUtility == null ||
                !this.loadedCargoIngredientUtility.TryTakeLoadedCargoIngredients(
                    shuttle,
                    requirements,
                    out ingredients,
                    out withdrawal,
                    out reason))
            {
                return false;
            }

            try
            {
                recipeDef.Worker.ApplyOnPawn(patient, part, doctor, ingredients, null);
                this.ApplySurgeryRecords(patient, doctor);
                if (withdrawal != null)
                {
                    withdrawal.CommitConsumed(recipeDef, doctor.Map);
                }
            }
            catch (Exception exception)
            {
                string rollbackNotice;
                if (withdrawal != null)
                {
                    withdrawal.RollbackOrDrop(doctor, shuttle, out rollbackNotice);
                }

                reason = "CT_Shuttle_MedicalSurgery_Failed".Translate().ToString() +
                    ": " + exception.GetType().Name + ": " + exception.Message;
                if (Prefs.DevMode)
                {
                    Log.Warning("[CeleTech Shuttle] " + reason);
                }

                return false;
            }

            notice = "CT_Shuttle_MedicalSurgery_Completed".Translate().ToString();
            reason = null;
            return true;
        }

        internal int GetSurgeryWorkTicks(RecipeDef recipeDef, Pawn patient)
        {
            if (recipeDef == null)
            {
                return FallbackSurgeryWorkTicks;
            }

            float workAmount = recipeDef.WorkAmountTotal(patient);
            if (float.IsNaN(workAmount) || float.IsInfinity(workAmount) || workAmount <= 0f)
            {
                return FallbackSurgeryWorkTicks;
            }

            int ticks = UnityEngine.Mathf.CeilToInt(workAmount);
            if (ticks < 1)
            {
                ticks = FallbackSurgeryWorkTicks;
            }
            else if (ticks > MaxSurgeryWorkTicks)
            {
                ticks = MaxSurgeryWorkTicks;
            }

            return ticks;
        }

        internal RecipeDef ResolveRecipeDef(string recipeDefName)
        {
            return string.IsNullOrEmpty(recipeDefName)
                ? null
                : DefDatabase<RecipeDef>.GetNamedSilentFail(recipeDefName);
        }

        internal bool TryResolveBodyPart(
            Pawn patient,
            RecipeDef recipeDef,
            int bodyPartIndex,
            out BodyPartRecord part,
            out string reason)
        {
            part = null;
            reason = null;
            if (recipeDef == null)
            {
                reason = "CT_Shuttle_MedicalSurgery_InvalidRecipe".Translate().ToString();
                return false;
            }

            if (!recipeDef.targetsBodyPart)
            {
                return true;
            }

            List<BodyPartRecord> parts = patient != null &&
                patient.RaceProps != null &&
                patient.RaceProps.body != null
                    ? patient.RaceProps.body.AllParts
                    : null;
            if (parts == null ||
                bodyPartIndex < 0 ||
                bodyPartIndex >= parts.Count)
            {
                reason = "CT_Shuttle_MedicalSurgery_InvalidBodyPart".Translate().ToString();
                return false;
            }

            part = parts[bodyPartIndex];
            if (part == null || !recipeDef.AvailableOnNow(patient, part))
            {
                reason = "CT_Shuttle_MedicalSurgery_InvalidBodyPart".Translate().ToString();
                return false;
            }

            return true;
        }

        internal bool IsSupportedSurgeryRecipe(RecipeDef recipeDef)
        {
            return recipeDef != null &&
                recipeDef.AvailableNow &&
                recipeDef.Worker is Recipe_Surgery;
        }

        internal bool HasUnsupportedExtraIngredients(RecipeDef recipeDef)
        {
            List<MedicalBaySurgeryIngredientRequirement> requirements;
            string reason;
            return !this.TryBuildSurgeryIngredientRequirements(
                recipeDef,
                null,
                null,
                out requirements,
                out reason);
        }

        internal string BuildRequiredMaterialsSummary(RecipeDef recipeDef)
        {
            return this.BuildRequiredMaterialsSummary(recipeDef, null, null);
        }

        internal string BuildRequiredMaterialsSummary(
            RecipeDef recipeDef,
            ThingWithComps shuttle,
            Pawn patient)
        {
            List<MedicalBaySurgeryIngredientRequirement> requirements;
            string reason;
            if (!this.TryBuildSurgeryIngredientRequirements(
                recipeDef,
                shuttle,
                patient,
                out requirements,
                out reason))
            {
                return reason;
            }

            return this.loadedCargoIngredientUtility != null
                ? this.loadedCargoIngredientUtility.BuildRequiredMaterialsSummary(requirements)
                : string.Empty;
        }

        internal string BuildMissingMaterialsSummary(
            RecipeDef recipeDef,
            ThingWithComps shuttle,
            Pawn patient)
        {
            List<MedicalBaySurgeryIngredientRequirement> requirements;
            string reason;
            if (!this.TryBuildSurgeryIngredientRequirements(
                recipeDef,
                shuttle,
                patient,
                out requirements,
                out reason))
            {
                return reason;
            }

            return this.loadedCargoIngredientUtility != null
                ? this.loadedCargoIngredientUtility.BuildMissingMaterialsSummary(requirements)
                : string.Empty;
        }

        internal bool TryBuildSurgeryIngredientRequirements(
            RecipeDef recipeDef,
            ThingWithComps shuttle,
            Pawn patient,
            out List<MedicalBaySurgeryIngredientRequirement> requirements,
            out string reason)
        {
            requirements = new List<MedicalBaySurgeryIngredientRequirement>();
            reason = null;
            if (recipeDef == null)
            {
                reason = "CT_Shuttle_MedicalSurgery_InvalidRecipe".Translate().ToString();
                return false;
            }

            if (recipeDef.ingredients != null)
            {
                for (int i = 0; i < recipeDef.ingredients.Count; i++)
                {
                    IngredientCount ingredient = recipeDef.ingredients[i];
                    MedicalBaySurgeryIngredientRequirement requirement;
                    if (!this.TryBuildIngredientRequirement(
                        ingredient,
                        recipeDef,
                        shuttle,
                        patient,
                        out requirement,
                        out reason))
                    {
                        return false;
                    }

                    if (requirement != null)
                    {
                        requirements.Add(requirement);
                    }
                }
            }

            if (!this.HasMedicineRequirement(requirements))
            {
                MedicalBaySurgeryIngredientRequirement medicineRequirement =
                    this.BuildMedicineRequirement(shuttle, patient, 1);
                if (medicineRequirement == null)
                {
                    reason = "CT_Shuttle_MedicalSurgery_IngredientUnsupported".Translate().ToString();
                    return false;
                }

                requirements.Insert(0, medicineRequirement);
            }

            return true;
        }

        private bool TryBuildIngredientRequirement(
            IngredientCount ingredient,
            RecipeDef recipeDef,
            ThingWithComps shuttle,
            Pawn patient,
            out MedicalBaySurgeryIngredientRequirement requirement,
            out string reason)
        {
            requirement = null;
            reason = null;
            if (ingredient == null || ingredient.filter == null)
            {
                reason = "CT_Shuttle_MedicalSurgery_IngredientUnsupported".Translate().ToString();
                return false;
            }

            List<ThingDef> allowedDefs = this.GetAllowedIngredientDefs(ingredient, recipeDef, patient);
            if (allowedDefs.Count == 0)
            {
                reason = "CT_Shuttle_MedicalSurgery_IngredientUnsupported".Translate().ToString();
                return false;
            }

            int requiredCount = UnityEngine.Mathf.CeilToInt(ingredient.GetBaseCount());
            if (requiredCount <= 0)
            {
                return true;
            }

            bool isMedicineRequirement = this.IsMedicineRequirement(allowedDefs);
            int availableCount = this.loadedCargoIngredientUtility != null
                ? this.loadedCargoIngredientUtility.CountLoadedCargoThings(shuttle, allowedDefs)
                : 0;
            requirement = new MedicalBaySurgeryIngredientRequirement(
                allowedDefs,
                requiredCount,
                availableCount,
                this.BuildIngredientLabel(allowedDefs, isMedicineRequirement),
                isMedicineRequirement);
            return true;
        }

        private MedicalBaySurgeryIngredientRequirement BuildMedicineRequirement(
            ThingWithComps shuttle,
            Pawn patient,
            int requiredCount)
        {
            List<ThingDef> allowedDefs = this.GetAllowedMedicineDefs(patient);
            if (allowedDefs.Count == 0)
            {
                return null;
            }

            int availableCount = this.loadedCargoIngredientUtility != null
                ? this.loadedCargoIngredientUtility.CountLoadedCargoThings(shuttle, allowedDefs)
                : 0;
            return new MedicalBaySurgeryIngredientRequirement(
                allowedDefs,
                UnityEngine.Mathf.Max(1, requiredCount),
                availableCount,
                this.GetMedicineRequirementLabel(),
                true);
        }

        private List<ThingDef> GetAllowedIngredientDefs(
            IngredientCount ingredient,
            RecipeDef recipeDef,
            Pawn patient)
        {
            List<ThingDef> allowedDefs = new List<ThingDef>();
            if (ingredient == null || ingredient.filter == null)
            {
                return allowedDefs;
            }

            foreach (ThingDef allowedDef in ingredient.filter.AllowedThingDefs)
            {
                if (allowedDef == null ||
                    !this.RecipeAllowsIngredientDef(recipeDef, allowedDef) ||
                    (this.IsMedicineThingDef(allowedDef) && !this.PatientAllowsMedicine(patient, allowedDef)))
                {
                    continue;
                }

                if (!allowedDefs.Contains(allowedDef))
                {
                    allowedDefs.Add(allowedDef);
                }
            }

            allowedDefs.Sort(this.CompareThingDefs);
            return allowedDefs;
        }

        private List<ThingDef> GetAllowedMedicineDefs(Pawn patient)
        {
            List<ThingDef> allowedDefs = new List<ThingDef>();
            List<ThingDef> allDefs = DefDatabase<ThingDef>.AllDefsListForReading;
            for (int i = 0; i < allDefs.Count; i++)
            {
                ThingDef thingDef = allDefs[i];
                if (thingDef != null &&
                    this.IsMedicineThingDef(thingDef) &&
                    this.PatientAllowsMedicine(patient, thingDef))
                {
                    allowedDefs.Add(thingDef);
                }
            }

            allowedDefs.Sort(this.CompareThingDefs);
            return allowedDefs;
        }

        private bool HasMedicineRequirement(
            IReadOnlyList<MedicalBaySurgeryIngredientRequirement> requirements)
        {
            if (requirements == null)
            {
                return false;
            }

            for (int i = 0; i < requirements.Count; i++)
            {
                if (requirements[i] != null && requirements[i].IsMedicineRequirement)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsMedicineRequirement(IReadOnlyList<ThingDef> thingDefs)
        {
            if (thingDefs == null || thingDefs.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < thingDefs.Count; i++)
            {
                if (!this.IsMedicineThingDef(thingDefs[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsMedicineThingDef(ThingDef thingDef)
        {
            if (thingDef == null)
            {
                return false;
            }

            if (thingDef.IsMedicine)
            {
                return true;
            }

            if (thingDef.thingCategories == null)
            {
                return false;
            }

            for (int i = 0; i < thingDef.thingCategories.Count; i++)
            {
                ThingCategoryDef category = thingDef.thingCategories[i];
                if (category != null && category.defName == "Medicine")
                {
                    return true;
                }
            }

            return false;
        }

        private bool RecipeAllowsIngredientDef(RecipeDef recipeDef, ThingDef thingDef)
        {
            return thingDef != null &&
                (recipeDef == null ||
                    recipeDef.fixedIngredientFilter == null ||
                    recipeDef.fixedIngredientFilter.Allows(thingDef));
        }

        private bool PatientAllowsMedicine(Pawn patient, ThingDef medicineDef)
        {
            if (medicineDef == null || !this.IsMedicineThingDef(medicineDef))
            {
                return false;
            }

            return patient == null ||
                patient.playerSettings == null ||
                MedicalCareUtility.AllowsMedicine(patient.playerSettings.medCare, medicineDef);
        }

        private string BuildIngredientLabel(
            IReadOnlyList<ThingDef> allowedDefs,
            bool isMedicineRequirement)
        {
            if (isMedicineRequirement)
            {
                return this.GetMedicineRequirementLabel();
            }

            if (allowedDefs == null || allowedDefs.Count == 0)
            {
                return "-";
            }

            if (allowedDefs.Count == 1)
            {
                return this.GetThingDefLabel(allowedDefs[0]);
            }

            List<string> labels = new List<string>();
            int labelCount = allowedDefs.Count < 3 ? allowedDefs.Count : 3;
            for (int i = 0; i < labelCount; i++)
            {
                labels.Add(this.GetThingDefLabel(allowedDefs[i]));
            }

            if (allowedDefs.Count > labelCount)
            {
                labels.Add("...");
            }

            return string.Join("/", labels.ToArray());
        }

        private string GetMedicineRequirementLabel()
        {
            return ThingDefOf.MedicineIndustrial != null
                ? ThingDefOf.MedicineIndustrial.LabelCap.ToString()
                : "CT_Shuttle_MedicalSurgery_RequiresMedicine".Translate().ToString();
        }

        private string GetThingDefLabel(ThingDef thingDef)
        {
            return thingDef != null ? thingDef.LabelCap.ToString() : "-";
        }

        private int CompareThingDefs(ThingDef left, ThingDef right)
        {
            if (left == null || right == null)
            {
                return left == right ? 0 : (left == null ? 1 : -1);
            }

            string leftLabel = this.GetThingDefLabel(left);
            string rightLabel = this.GetThingDefLabel(right);
            int labelCompare = string.Compare(leftLabel, rightLabel, StringComparison.OrdinalIgnoreCase);
            if (labelCompare != 0)
            {
                return labelCompare;
            }

            return string.Compare(left.defName, right.defName, StringComparison.OrdinalIgnoreCase);
        }

        private bool TryValidateSurgeryRequest(
            ThingWithComps shuttle,
            Pawn doctor,
            Pawn patient,
            RecipeDef recipeDef,
            int bodyPartIndex,
            out BodyPartRecord part,
            out string reason)
        {
            part = null;
            reason = null;
            if (shuttle == null || shuttle.Destroyed)
            {
                reason = "CT_Shuttle_MedicalBay_NoMedicalBay".Translate().ToString();
                return false;
            }

            if (doctor == null || doctor.Destroyed || doctor.Dead || doctor.Downed)
            {
                reason = "CT_Shuttle_MedicalBay_DoctorUnavailable".Translate().ToString();
                return false;
            }

            if (patient == null || patient.Destroyed || patient.Dead)
            {
                reason = "CT_Shuttle_MedicalProcedure_NoPatient".Translate().ToString();
                return false;
            }

            CompShuttleMedicalBayOccupancy occupancy = shuttle.TryGetComp<CompShuttleMedicalBayOccupancy>();
            if (occupancy == null || !occupancy.ContainsPatient(patient))
            {
                reason = "CT_Shuttle_MedicalBay_PatientNotHeld".Translate().ToString();
                return false;
            }

            if (!MedicalBayAdmissionValidator.IsMedicalBayPowered(shuttle))
            {
                reason = "CT_Shuttle_MedicalBay_Unpowered".Translate().ToString();
                return false;
            }

            if (!this.IsSupportedSurgeryRecipe(recipeDef))
            {
                reason = "CT_Shuttle_MedicalSurgery_RecipeWorkerUnsupported".Translate().ToString();
                return false;
            }

            if (!recipeDef.AvailableOnNow(patient, null))
            {
                reason = "CT_Shuttle_MedicalSurgery_InvalidRecipe".Translate().ToString();
                return false;
            }

            return this.TryResolveBodyPart(patient, recipeDef, bodyPartIndex, out part, out reason);
        }

        private Pawn FindPatientByThingID(ThingWithComps shuttle, int patientThingID)
        {
            if (shuttle == null || patientThingID <= 0)
            {
                return null;
            }

            CompShuttleMedicalBayOccupancy occupancy =
                shuttle.TryGetComp<CompShuttleMedicalBayOccupancy>();
            if (occupancy == null)
            {
                return null;
            }

            List<Pawn> patients = occupancy.GetHeldPatientsForReading();
            for (int i = 0; i < patients.Count; i++)
            {
                Pawn patient = patients[i];
                if (patient != null && patient.thingIDNumber == patientThingID)
                {
                    return patient;
                }
            }

            return null;
        }

        private void ApplySurgeryRecords(Pawn patient, Pawn doctor)
        {
            if (patient != null &&
                patient.RaceProps != null &&
                patient.RaceProps.IsFlesh &&
                patient.records != null)
            {
                patient.records.Increment(RecordDefOf.OperationsReceived);
            }

            if (doctor != null && doctor.records != null)
            {
                doctor.records.Increment(RecordDefOf.OperationsPerformed);
            }
        }
    }
}
