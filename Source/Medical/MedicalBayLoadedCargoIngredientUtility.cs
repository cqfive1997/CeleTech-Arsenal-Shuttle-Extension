using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Transactions;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Medical
{
    /// <summary>
    /// Thin Medical Bay adapter that turns surgery requirements into exact Cargo withdrawals.
    /// Selection and rollback mechanics are delegated to focused collaborators.
    /// </summary>
    internal sealed class MedicalBayLoadedCargoIngredientUtility
    {
        private readonly MedicalBayCargoIngredientCandidateSelector candidateSelector =
            new MedicalBayCargoIngredientCandidateSelector();

        internal int CountLoadedCargoThings(
            ThingWithComps shuttleHost,
            ThingDef thingDef)
        {
            IShuttleCargoResourceBroker cargoBroker;
            return this.candidateSelector.TryResolveBroker(
                    shuttleHost,
                    out cargoBroker)
                ? this.candidateSelector.Count(cargoBroker, thingDef)
                : 0;
        }

        internal int CountLoadedCargoThings(
            ThingWithComps shuttleHost,
            IReadOnlyList<ThingDef> allowedThingDefs)
        {
            IShuttleCargoResourceBroker cargoBroker;
            return this.candidateSelector.TryResolveBroker(
                    shuttleHost,
                    out cargoBroker)
                ? this.candidateSelector.Count(cargoBroker, allowedThingDefs)
                : 0;
        }

        internal bool TryTakeLoadedCargoIngredients(
            ThingWithComps shuttleHost,
            IReadOnlyList<MedicalBaySurgeryIngredientRequirement> requirements,
            out List<Thing> ingredients,
            out MedicalBayLoadedCargoIngredientWithdrawal withdrawal,
            out string reason)
        {
            ingredients = new List<Thing>();
            withdrawal = new MedicalBayLoadedCargoIngredientWithdrawal();
            reason = null;

            if (requirements == null || requirements.Count == 0)
            {
                return true;
            }

            string missingSummary = this.BuildMissingMaterialsSummary(requirements);
            if (!string.IsNullOrEmpty(missingSummary))
            {
                reason = "CT_Shuttle_MedicalSurgery_MissingMaterials"
                    .Translate(missingSummary)
                    .ToString();
                return false;
            }

            IShuttleCargoResourceBroker cargoBroker;
            if (!this.candidateSelector.TryResolveBroker(
                    shuttleHost,
                    out cargoBroker))
            {
                reason = "CT_Shuttle_MedicalSurgery_MissingMaterials"
                    .Translate(this.BuildRequiredMaterialsSummary(requirements))
                    .ToString();
                return false;
            }

            for (int requirementIndex = 0;
                requirementIndex < requirements.Count;
                requirementIndex++)
            {
                MedicalBaySurgeryIngredientRequirement requirement =
                    requirements[requirementIndex];
                if (requirement == null || requirement.RequiredCount <= 0)
                {
                    continue;
                }

                int remaining = requirement.RequiredCount;
                while (remaining > 0)
                {
                    MedicalBayCargoIngredientCandidate candidate =
                        this.candidateSelector.FindBest(
                            cargoBroker,
                            requirement.AllowedThingDefs);
                    if (candidate == null || candidate.StackRef == null)
                    {
                        return this.FailTake(
                            withdrawal,
                            ingredients,
                            shuttleHost,
                            requirement,
                            remaining,
                            out reason);
                    }

                    int takeCount = Mathf.Min(remaining, candidate.StackRef.Count);
                    ShuttleCargoWithdrawal cargoWithdrawal;
                    string transactionFailure;
                    if (!cargoBroker.TryBeginExactWithdrawal(
                            candidate.StackRef,
                            takeCount,
                            ShuttleCargoAccessRequirement.None,
                            "Medical Bay surgery ingredients",
                            out cargoWithdrawal,
                            out transactionFailure))
                    {
                        return this.FailTake(
                            withdrawal,
                            ingredients,
                            shuttleHost,
                            requirement,
                            remaining,
                            out reason);
                    }

                    withdrawal.Add(cargoWithdrawal);
                    Thing taken = cargoWithdrawal != null
                        ? cargoWithdrawal.Thing
                        : null;
                    if (taken == null ||
                        taken.Destroyed ||
                        taken.stackCount <= 0 ||
                        taken.holdingOwner != null)
                    {
                        return this.FailTake(
                            withdrawal,
                            ingredients,
                            shuttleHost,
                            requirement,
                            remaining,
                            out reason);
                    }

                    ingredients.Add(taken);
                    remaining -= taken.stackCount;
                }
            }

            return true;
        }

        internal string BuildRequiredMaterialsSummary(
            IReadOnlyList<MedicalBaySurgeryIngredientRequirement> requirements)
        {
            if (requirements == null || requirements.Count == 0)
            {
                return "CT_Shuttle_MedicalSurgery_MaterialsAvailable".Translate().ToString();
            }

            List<string> parts = new List<string>();
            for (int i = 0; i < requirements.Count; i++)
            {
                MedicalBaySurgeryIngredientRequirement requirement = requirements[i];
                if (requirement != null && requirement.RequiredCount > 0)
                {
                    parts.Add(this.FormatRequirement(
                        requirement.Label,
                        requirement.RequiredCount));
                }
            }

            return parts.Count > 0
                ? string.Join(", ", parts.ToArray())
                : "CT_Shuttle_MedicalSurgery_MaterialsAvailable".Translate().ToString();
        }

        internal string BuildMissingMaterialsSummary(
            IReadOnlyList<MedicalBaySurgeryIngredientRequirement> requirements)
        {
            if (requirements == null || requirements.Count == 0)
            {
                return string.Empty;
            }

            List<string> parts = new List<string>();
            for (int i = 0; i < requirements.Count; i++)
            {
                MedicalBaySurgeryIngredientRequirement requirement = requirements[i];
                if (requirement != null && requirement.MissingCount > 0)
                {
                    parts.Add(this.FormatRequirement(
                        requirement.Label,
                        requirement.MissingCount));
                }
            }

            return parts.Count > 0
                ? string.Join(", ", parts.ToArray())
                : string.Empty;
        }

        internal string FormatRequirement(string label, int count)
        {
            return "CT_Shuttle_MedicalSurgery_MaterialCount"
                .Translate(string.IsNullOrEmpty(label) ? "-" : label, Mathf.Max(0, count))
                .ToString();
        }

        private bool FailTake(
            MedicalBayLoadedCargoIngredientWithdrawal withdrawal,
            List<Thing> ingredients,
            ThingWithComps shuttleHost,
            MedicalBaySurgeryIngredientRequirement requirement,
            int remaining,
            out string reason)
        {
            string rollbackNotice;
            if (withdrawal != null)
            {
                withdrawal.RollbackOrDrop(null, shuttleHost, out rollbackNotice);
            }

            if (ingredients != null)
            {
                ingredients.Clear();
            }

            reason = "CT_Shuttle_MedicalSurgery_MissingMaterials"
                .Translate(this.FormatRequirement(
                    requirement != null ? requirement.Label : string.Empty,
                    remaining))
                .ToString();
            return false;
        }
    }
}
