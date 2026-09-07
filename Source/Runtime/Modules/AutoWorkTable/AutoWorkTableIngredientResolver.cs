using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.AutoWorkTable
{
    internal sealed class AutoWorkTableIngredientResolver
    {
        internal bool TryResolveRequirements(
            RecipeDef recipeDef,
            IShuttleCargoResourceBroker cargoBroker,
            out List<CargoIngredientRequirement> requirements,
            out string failureReason)
        {
            CargoAvailabilitySnapshot snapshot = cargoBroker != null
                ? cargoBroker.GetAvailabilitySnapshot()
                : null;
            return this.TryResolveRequirements(
                recipeDef,
                snapshot,
                null,
                out requirements,
                out failureReason);
        }

        internal bool TryResolveRequirements(
            RecipeDef recipeDef,
            CargoAvailabilitySnapshot availabilitySnapshot,
            out List<CargoIngredientRequirement> requirements,
            out string failureReason)
        {
            return this.TryResolveRequirements(
                recipeDef,
                availabilitySnapshot,
                null,
                out requirements,
                out failureReason);
        }

        internal bool TryResolveRequirements(
            RecipeDef recipeDef,
            CargoAvailabilitySnapshot availabilitySnapshot,
            ThingFilter orderIngredientFilter,
            out List<CargoIngredientRequirement> requirements,
            out string failureReason)
        {
            requirements = new List<CargoIngredientRequirement>();
            failureReason = null;

            if (recipeDef == null)
            {
                failureReason = "Recipe is missing.";
                return false;
            }

            if (availabilitySnapshot == null || !availabilitySnapshot.IsAvailable)
            {
                failureReason = "Cargo availability snapshot is not available.";
                return false;
            }

            if (AutoWorkTableMechanoidDisassemblyUtility.IsSupportedRecipe(recipeDef))
            {
                return this.TryResolveMechanoidDisassemblyRequirement(
                    recipeDef,
                    availabilitySnapshot,
                    orderIngredientFilter,
                    out requirements,
                    out failureReason);
            }

            if (AutoWorkTableAnimalButcheryUtility.IsSupportedRecipe(recipeDef))
            {
                return this.TryResolveAnimalButcheryRequirement(
                    recipeDef,
                    availabilitySnapshot,
                    orderIngredientFilter,
                    out requirements,
                    out failureReason);
            }

            if (recipeDef.ingredients == null || recipeDef.ingredients.Count == 0)
            {
                return true;
            }

            Bill calculationBill = null;
            Dictionary<ThingDef, int> reservedCounts = new Dictionary<ThingDef, int>();
            for (int pass = 0; pass < 2; pass++)
            {
                bool resolveFixedIngredients = pass == 0;
                for (int i = 0; i < recipeDef.ingredients.Count; i++)
                {
                    IngredientCount ingredient = recipeDef.ingredients[i];
                    if (ingredient == null || ingredient.filter == null)
                    {
                        failureReason = "Recipe " + recipeDef.defName + " has an invalid ingredient.";
                        return false;
                    }

                    if (ingredient.IsFixedIngredient != resolveFixedIngredients)
                    {
                        continue;
                    }

                    ThingDef resolvedThingDef;
                    int requiredCount;
                    if (!this.TryResolveIngredient(
                        recipeDef,
                        ingredient,
                        calculationBill,
                        availabilitySnapshot,
                        orderIngredientFilter,
                        reservedCounts,
                        out resolvedThingDef,
                        out requiredCount,
                        out failureReason))
                    {
                        return false;
                    }

                    if (requiredCount > 0)
                    {
                        this.AddRequirement(requirements, resolvedThingDef, requiredCount);
                        this.AddReservedCount(reservedCounts, resolvedThingDef, requiredCount);
                    }
                }
            }

            requirements.Sort(this.CompareRequirements);
            return true;
        }

        internal bool TryResolveAvailability(
            RecipeDef recipeDef,
            ShuttleCargoInventorySnapshot inventorySnapshot,
            out AutoWorkTableIngredientAvailability availability)
        {
            return this.TryResolveAvailability(
                recipeDef,
                inventorySnapshot,
                null,
                out availability);
        }

        internal bool TryResolveAvailability(
            RecipeDef recipeDef,
            ShuttleCargoInventorySnapshot inventorySnapshot,
            ThingFilter orderIngredientFilter,
            out AutoWorkTableIngredientAvailability availability)
        {
            availability = null;
            if (recipeDef == null)
            {
                availability = this.BuildAvailability(
                    false,
                    new List<CargoIngredientRequirement>(),
                    new List<AutoWorkTableIngredientNeedStatus>(),
                    null,
                    "Recipe is missing.",
                    "Recipe is missing.");
                return false;
            }

            if (inventorySnapshot == null || !inventorySnapshot.IsAvailable)
            {
                string reason = inventorySnapshot != null && !string.IsNullOrEmpty(inventorySnapshot.UnavailableReason)
                    ? inventorySnapshot.UnavailableReason
                    : "Shuttle inventory scan is unavailable.";
                availability = this.BuildAvailability(
                    false,
                    new List<CargoIngredientRequirement>(),
                    new List<AutoWorkTableIngredientNeedStatus>(),
                    null,
                    reason,
                    reason);
                return false;
            }

            if (AutoWorkTableMechanoidDisassemblyUtility.IsSupportedRecipe(recipeDef))
            {
                return this.TryResolveMechanoidDisassemblyAvailability(
                    recipeDef,
                    inventorySnapshot,
                    orderIngredientFilter,
                    out availability);
            }

            if (AutoWorkTableAnimalButcheryUtility.IsSupportedRecipe(recipeDef))
            {
                return this.TryResolveAnimalButcheryAvailability(
                    recipeDef,
                    inventorySnapshot,
                    orderIngredientFilter,
                    out availability);
            }

            if (recipeDef.ingredients == null || recipeDef.ingredients.Count == 0)
            {
                availability = this.BuildAvailability(
                    true,
                    new List<CargoIngredientRequirement>(),
                    new List<AutoWorkTableIngredientNeedStatus>(),
                    "No ingredients required.",
                    null,
                    null);
                return true;
            }

            List<CargoIngredientRequirement> requirements = new List<CargoIngredientRequirement>();
            List<AutoWorkTableIngredientNeedStatus> needs =
                new List<AutoWorkTableIngredientNeedStatus>();
            Dictionary<ThingDef, int> reservedCounts = new Dictionary<ThingDef, int>();
            List<string> missingParts = new List<string>();

            for (int pass = 0; pass < 2; pass++)
            {
                bool resolveFixedIngredients = pass == 0;
                for (int i = 0; i < recipeDef.ingredients.Count; i++)
                {
                    IngredientCount ingredient = recipeDef.ingredients[i];
                    if (ingredient == null || ingredient.filter == null)
                    {
                        string reason = "Recipe " + recipeDef.defName + " has an invalid ingredient.";
                        availability = this.BuildAvailability(
                            false,
                            requirements,
                            needs,
                            null,
                            reason,
                            reason);
                        return false;
                    }

                    if (ingredient.IsFixedIngredient != resolveFixedIngredients)
                    {
                        continue;
                    }

                    ThingDef resolvedThingDef;
                    int requiredCount;
                    int availableCount;
                    string label;
                    string failureReason;
                    bool satisfied = this.TryResolveIngredientFromInventory(
                        recipeDef,
                        ingredient,
                        inventorySnapshot,
                        orderIngredientFilter,
                        reservedCounts,
                        out resolvedThingDef,
                        out requiredCount,
                        out availableCount,
                        out label,
                        out failureReason);

                    int missingCount = requiredCount > availableCount
                        ? requiredCount - availableCount
                        : 0;
                    needs.Add(new AutoWorkTableIngredientNeedStatus(
                        label,
                        resolvedThingDef != null ? resolvedThingDef.defName : null,
                        requiredCount,
                        availableCount,
                        missingCount,
                        this.CountSource(inventorySnapshot, resolvedThingDef, ShuttleCargoInventorySourceKind.RegularCargo),
                        this.CountSource(inventorySnapshot, resolvedThingDef, ShuttleCargoInventorySourceKind.RefrigeratedCargo)));

                    if (!satisfied)
                    {
                        missingParts.Add(label + " x" + missingCount.ToString());
                        continue;
                    }

                    if (requiredCount > 0)
                    {
                        this.AddRequirement(requirements, resolvedThingDef, requiredCount);
                        this.AddReservedCount(reservedCounts, resolvedThingDef, requiredCount);
                    }
                }
            }

            requirements.Sort(this.CompareRequirements);
            bool available = missingParts.Count == 0;
            string summary = available
                ? this.BuildAvailableSummary(needs)
                : "Missing: " + string.Join(", ", missingParts.ToArray());
            availability = this.BuildAvailability(
                available,
                requirements,
                needs,
                summary,
                available ? null : summary,
                available ? null : summary);
            return available;
        }

        private bool TryResolveMechanoidDisassemblyRequirement(
            RecipeDef recipeDef,
            CargoAvailabilitySnapshot availabilitySnapshot,
            ThingFilter orderIngredientFilter,
            out List<CargoIngredientRequirement> requirements,
            out string failureReason)
        {
            requirements = new List<CargoIngredientRequirement>();
            failureReason = null;

            IReadOnlyList<CargoThingDefCount> candidates =
                this.GetMechanoidCorpseCandidates(
                    recipeDef,
                    availabilitySnapshot,
                    orderIngredientFilter);
            if (candidates == null || candidates.Count == 0)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_NoMechanoidCorpses".Translate().ToString();
                return false;
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                CargoThingDefCount candidate = candidates[i];
                if (candidate != null && candidate.ThingDef != null && candidate.Count > 0)
                {
                    requirements.Add(new CargoIngredientRequirement(candidate.ThingDef, 1));
                    return true;
                }
            }

            failureReason = "CT_Shuttle_AutoWorkTable_NoMechanoidCorpses".Translate().ToString();
            return false;
        }

        private bool TryResolveAnimalButcheryRequirement(
            RecipeDef recipeDef,
            CargoAvailabilitySnapshot availabilitySnapshot,
            ThingFilter orderIngredientFilter,
            out List<CargoIngredientRequirement> requirements,
            out string failureReason)
        {
            requirements = new List<CargoIngredientRequirement>();
            failureReason = null;

            IReadOnlyList<CargoThingDefCount> candidates =
                this.GetAnimalCorpseCandidates(
                    recipeDef,
                    availabilitySnapshot,
                    orderIngredientFilter);
            if (candidates == null || candidates.Count == 0)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_NoAnimalCorpses".Translate().ToString();
                return false;
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                CargoThingDefCount candidate = candidates[i];
                if (candidate != null && candidate.ThingDef != null && candidate.Count > 0)
                {
                    requirements.Add(new CargoIngredientRequirement(candidate.ThingDef, 1));
                    return true;
                }
            }

            failureReason = "CT_Shuttle_AutoWorkTable_NoAnimalCorpses".Translate().ToString();
            return false;
        }

        private bool TryResolveMechanoidDisassemblyAvailability(
            RecipeDef recipeDef,
            ShuttleCargoInventorySnapshot inventorySnapshot,
            ThingFilter orderIngredientFilter,
            out AutoWorkTableIngredientAvailability availability)
        {
            List<CargoIngredientRequirement> requirements = new List<CargoIngredientRequirement>();
            List<AutoWorkTableIngredientNeedStatus> needs =
                new List<AutoWorkTableIngredientNeedStatus>();
            string label = AutoWorkTableMechanoidDisassemblyUtility.MechanoidCorpseLabel();
            IReadOnlyList<CargoThingDefCount> candidates =
                this.GetMechanoidCorpseCandidates(
                    recipeDef,
                    inventorySnapshot,
                    orderIngredientFilter);

            int availableCount = 0;
            int regularCargoCount = 0;
            int refrigeratedCargoCount = 0;
            ThingDef selectedThingDef = null;
            if (candidates != null)
            {
                for (int i = 0; i < candidates.Count; i++)
                {
                    CargoThingDefCount candidate = candidates[i];
                    ThingDef candidateDef = candidate != null ? candidate.ThingDef : null;
                    if (candidateDef == null || candidate.Count <= 0)
                    {
                        continue;
                    }

                    if (selectedThingDef == null)
                    {
                        selectedThingDef = candidateDef;
                    }

                    availableCount += candidate.Count;
                    regularCargoCount += this.CountSource(
                        inventorySnapshot,
                        candidateDef,
                        ShuttleCargoInventorySourceKind.RegularCargo);
                    refrigeratedCargoCount += this.CountSource(
                        inventorySnapshot,
                        candidateDef,
                        ShuttleCargoInventorySourceKind.RefrigeratedCargo);
                }
            }

            bool available = availableCount > 0;
            if (available && selectedThingDef != null)
            {
                requirements.Add(new CargoIngredientRequirement(selectedThingDef, 1));
            }

            needs.Add(new AutoWorkTableIngredientNeedStatus(
                label,
                selectedThingDef != null ? selectedThingDef.defName : null,
                1,
                availableCount,
                available ? 0 : 1,
                regularCargoCount,
                refrigeratedCargoCount));

            string summary = label + " " + availableCount.ToString() + "/1";
            string missingSummary = available
                ? null
                : "CT_Shuttle_AutoWorkTable_NoMechanoidCorpses".Translate().ToString();
            availability = this.BuildAvailability(
                available,
                requirements,
                needs,
                available ? summary : missingSummary,
                missingSummary,
                missingSummary);
            return available;
        }

        private bool TryResolveAnimalButcheryAvailability(
            RecipeDef recipeDef,
            ShuttleCargoInventorySnapshot inventorySnapshot,
            ThingFilter orderIngredientFilter,
            out AutoWorkTableIngredientAvailability availability)
        {
            List<CargoIngredientRequirement> requirements = new List<CargoIngredientRequirement>();
            List<AutoWorkTableIngredientNeedStatus> needs =
                new List<AutoWorkTableIngredientNeedStatus>();
            string label = AutoWorkTableAnimalButcheryUtility.AnimalCorpseLabel();
            IReadOnlyList<CargoThingDefCount> candidates =
                this.GetAnimalCorpseCandidates(
                    recipeDef,
                    inventorySnapshot,
                    orderIngredientFilter);

            int availableCount = 0;
            int regularCargoCount = 0;
            int refrigeratedCargoCount = 0;
            ThingDef selectedThingDef = null;
            if (candidates != null)
            {
                for (int i = 0; i < candidates.Count; i++)
                {
                    CargoThingDefCount candidate = candidates[i];
                    ThingDef candidateDef = candidate != null ? candidate.ThingDef : null;
                    if (candidateDef == null || candidate.Count <= 0)
                    {
                        continue;
                    }

                    if (selectedThingDef == null)
                    {
                        selectedThingDef = candidateDef;
                    }

                    availableCount += candidate.Count;
                    regularCargoCount += this.CountSource(
                        inventorySnapshot,
                        candidateDef,
                        ShuttleCargoInventorySourceKind.RegularCargo);
                    refrigeratedCargoCount += this.CountSource(
                        inventorySnapshot,
                        candidateDef,
                        ShuttleCargoInventorySourceKind.RefrigeratedCargo);
                }
            }

            bool available = availableCount > 0;
            if (available && selectedThingDef != null)
            {
                requirements.Add(new CargoIngredientRequirement(selectedThingDef, 1));
            }

            needs.Add(new AutoWorkTableIngredientNeedStatus(
                label,
                selectedThingDef != null ? selectedThingDef.defName : null,
                1,
                availableCount,
                available ? 0 : 1,
                regularCargoCount,
                refrigeratedCargoCount));

            string summary = label + " " + availableCount.ToString() + "/1";
            string missingSummary = available
                ? null
                : "CT_Shuttle_AutoWorkTable_NoAnimalCorpses".Translate().ToString();
            availability = this.BuildAvailability(
                available,
                requirements,
                needs,
                available ? summary : missingSummary,
                missingSummary,
                missingSummary);
            return available;
        }

        private IReadOnlyList<CargoThingDefCount> GetMechanoidCorpseCandidates(
            RecipeDef recipeDef,
            CargoAvailabilitySnapshot availabilitySnapshot,
            ThingFilter orderIngredientFilter)
        {
            if (recipeDef == null || availabilitySnapshot == null)
            {
                return new List<CargoThingDefCount>();
            }

            IngredientCount ingredient = this.GetFirstIngredient(recipeDef);
            IReadOnlyList<CargoThingDefCount> candidates = ingredient != null && ingredient.filter != null
                ? availabilitySnapshot.GetAvailableThingDefs(
                    ingredient.filter,
                    recipeDef.fixedIngredientFilter)
                : new List<CargoThingDefCount>();
            return this.FilterOrderCandidates(candidates, orderIngredientFilter);
        }

        private IReadOnlyList<CargoThingDefCount> GetAnimalCorpseCandidates(
            RecipeDef recipeDef,
            CargoAvailabilitySnapshot availabilitySnapshot,
            ThingFilter orderIngredientFilter)
        {
            if (recipeDef == null || availabilitySnapshot == null)
            {
                return new List<CargoThingDefCount>();
            }

            IngredientCount ingredient = this.GetFirstIngredient(recipeDef);
            IReadOnlyList<CargoThingDefCount> candidates = ingredient != null && ingredient.filter != null
                ? availabilitySnapshot.GetAvailableThingDefs(
                    ingredient.filter,
                    recipeDef.fixedIngredientFilter)
                : new List<CargoThingDefCount>();
            return this.FilterAnimalCorpseCandidates(
                this.FilterOrderCandidates(candidates, orderIngredientFilter));
        }

        private IReadOnlyList<CargoThingDefCount> GetMechanoidCorpseCandidates(
            RecipeDef recipeDef,
            ShuttleCargoInventorySnapshot inventorySnapshot,
            ThingFilter orderIngredientFilter)
        {
            if (recipeDef == null || inventorySnapshot == null)
            {
                return new List<CargoThingDefCount>();
            }

            IngredientCount ingredient = this.GetFirstIngredient(recipeDef);
            IReadOnlyList<CargoThingDefCount> candidates = ingredient != null && ingredient.filter != null
                ? inventorySnapshot.GetThingDefs(
                    ingredient.filter,
                    recipeDef.fixedIngredientFilter)
                : new List<CargoThingDefCount>();
            return this.FilterOrderCandidates(candidates, orderIngredientFilter);
        }

        private IReadOnlyList<CargoThingDefCount> GetAnimalCorpseCandidates(
            RecipeDef recipeDef,
            ShuttleCargoInventorySnapshot inventorySnapshot,
            ThingFilter orderIngredientFilter)
        {
            if (recipeDef == null || inventorySnapshot == null)
            {
                return new List<CargoThingDefCount>();
            }

            IngredientCount ingredient = this.GetFirstIngredient(recipeDef);
            IReadOnlyList<CargoThingDefCount> candidates = ingredient != null && ingredient.filter != null
                ? inventorySnapshot.GetThingDefs(
                    ingredient.filter,
                    recipeDef.fixedIngredientFilter)
                : new List<CargoThingDefCount>();
            return this.FilterAnimalCorpseCandidates(
                this.FilterOrderCandidates(candidates, orderIngredientFilter));
        }

        private IReadOnlyList<CargoThingDefCount> FilterOrderCandidates(
            IReadOnlyList<CargoThingDefCount> candidates,
            ThingFilter orderIngredientFilter)
        {
            if (orderIngredientFilter == null)
            {
                return candidates ?? new List<CargoThingDefCount>();
            }

            List<CargoThingDefCount> result = new List<CargoThingDefCount>();
            if (candidates == null)
            {
                return result;
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                CargoThingDefCount candidate = candidates[i];
                if (candidate != null &&
                    candidate.ThingDef != null &&
                    orderIngredientFilter.Allows(candidate.ThingDef))
                {
                    result.Add(candidate);
                }
            }

            return result;
        }

        private IReadOnlyList<CargoThingDefCount> FilterAnimalCorpseCandidates(
            IReadOnlyList<CargoThingDefCount> candidates)
        {
            List<CargoThingDefCount> result = new List<CargoThingDefCount>();
            if (candidates == null)
            {
                return result;
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                CargoThingDefCount candidate = candidates[i];
                ThingDef corpseDef = candidate != null ? candidate.ThingDef : null;
                if (candidate == null ||
                    corpseDef == null ||
                    candidate.Count <= 0 ||
                    AutoWorkTableAnimalButcheryUtility.TryGetAnimalPawnDefFromCorpseDef(corpseDef) == null)
                {
                    continue;
                }

                result.Add(candidate);
            }

            return result;
        }

        private IngredientCount GetFirstIngredient(RecipeDef recipeDef)
        {
            return recipeDef != null &&
                recipeDef.ingredients != null &&
                recipeDef.ingredients.Count > 0
                    ? recipeDef.ingredients[0]
                    : null;
        }

        private bool TryResolveIngredient(
            RecipeDef recipeDef,
            IngredientCount ingredient,
            Bill calculationBill,
            CargoAvailabilitySnapshot availabilitySnapshot,
            ThingFilter orderIngredientFilter,
            Dictionary<ThingDef, int> reservedCounts,
            out ThingDef resolvedThingDef,
            out int requiredCount,
            out string failureReason)
        {
            resolvedThingDef = null;
            requiredCount = 0;
            failureReason = null;

            if (ingredient.IsFixedIngredient)
            {
                // Match Bill.IsFixedOrAllowedIngredient: a recipe-mandated fixed ingredient
                // bypasses the player bill filter and cannot be accidentally disabled.
                resolvedThingDef = ingredient.FixedIngredient;
                if (resolvedThingDef == null)
                {
                    failureReason = "Recipe " + recipeDef.defName + " has a null fixed ingredient.";
                    return false;
                }

                requiredCount = ingredient.CountRequiredOfFor(resolvedThingDef, recipeDef, calculationBill);
                int availableCount = availabilitySnapshot.CountAvailable(resolvedThingDef) -
                    this.GetReservedCount(reservedCounts, resolvedThingDef);
                if (requiredCount <= 0 || availableCount >= requiredCount)
                {
                    return true;
                }

                failureReason = "Cargo lacks required fixed ingredient " +
                    resolvedThingDef.defName + " for recipe " + recipeDef.defName + ".";
                return false;
            }

            IReadOnlyList<CargoThingDefCount> availableDefs =
                availabilitySnapshot.GetAvailableThingDefs(
                    ingredient.filter,
                    recipeDef.fixedIngredientFilter);
            if (availableDefs == null || availableDefs.Count == 0)
            {
                failureReason = "No cargo ingredient ThingDef matches recipe " + recipeDef.defName + ".";
                return false;
            }

            List<CargoThingDefCount> sortedDefs = new List<CargoThingDefCount>(availableDefs);
            sortedDefs.Sort(this.CompareThingDefCountNames);

            for (int i = 0; i < sortedDefs.Count; i++)
            {
                CargoThingDefCount availableDef = sortedDefs[i];
                ThingDef candidate = availableDef != null ? availableDef.ThingDef : null;
                if (candidate == null ||
                    !ingredient.filter.Allows(candidate) ||
                    (orderIngredientFilter != null && !orderIngredientFilter.Allows(candidate)) ||
                    (recipeDef.fixedIngredientFilter != null &&
                     !recipeDef.fixedIngredientFilter.Allows(candidate)))
                {
                    continue;
                }

                int candidateRequiredCount = ingredient.CountRequiredOfFor(candidate, recipeDef, calculationBill);
                if (candidateRequiredCount <= 0)
                {
                    continue;
                }

                int availableCount = availableDef.Count - this.GetReservedCount(reservedCounts, candidate);
                if (availableCount >= candidateRequiredCount)
                {
                    resolvedThingDef = candidate;
                    requiredCount = candidateRequiredCount;
                    return true;
                }
            }

            failureReason = "Cargo lacks enough matching ingredients for recipe " + recipeDef.defName + ".";
            return false;
        }

        private bool TryResolveIngredientFromInventory(
            RecipeDef recipeDef,
            IngredientCount ingredient,
            ShuttleCargoInventorySnapshot inventorySnapshot,
            ThingFilter orderIngredientFilter,
            Dictionary<ThingDef, int> reservedCounts,
            out ThingDef resolvedThingDef,
            out int requiredCount,
            out int availableCount,
            out string label,
            out string failureReason)
        {
            resolvedThingDef = null;
            requiredCount = 0;
            availableCount = 0;
            label = "ingredient";
            failureReason = null;

            if (ingredient.IsFixedIngredient)
            {
                // Fixed recipe inputs are mandatory in vanilla bills and ignore bill filters.
                resolvedThingDef = ingredient.FixedIngredient;
                label = this.LabelForThingDef(resolvedThingDef);
                if (resolvedThingDef == null)
                {
                    failureReason = "Recipe " + recipeDef.defName + " has a null fixed ingredient.";
                    return false;
                }

                requiredCount = ingredient.CountRequiredOfFor(resolvedThingDef, recipeDef, null);
                availableCount = inventorySnapshot.Count(resolvedThingDef) -
                    this.GetReservedCount(reservedCounts, resolvedThingDef);
                if (availableCount < 0)
                {
                    availableCount = 0;
                }

                if (requiredCount <= 0 || availableCount >= requiredCount)
                {
                    return true;
                }

                failureReason = "Missing " + label + " x" + (requiredCount - availableCount).ToString() +
                    " for recipe " + recipeDef.defName + ".";
                return false;
            }

            IReadOnlyList<CargoThingDefCount> availableDefs =
                inventorySnapshot.GetThingDefs(
                    ingredient.filter,
                    recipeDef.fixedIngredientFilter);
            if (availableDefs == null || availableDefs.Count == 0)
            {
                label = "matching ingredient";
                requiredCount = 1;
                failureReason = "No shuttle cargo ingredient ThingDef matches recipe " + recipeDef.defName + ".";
                return false;
            }

            List<CargoThingDefCount> sortedDefs = new List<CargoThingDefCount>(availableDefs);
            sortedDefs.Sort(this.CompareThingDefCountNames);
            ThingDef bestCandidate = null;
            int bestRequiredCount = 0;
            int bestAvailableCount = 0;

            for (int i = 0; i < sortedDefs.Count; i++)
            {
                CargoThingDefCount availableDef = sortedDefs[i];
                ThingDef candidate = availableDef != null ? availableDef.ThingDef : null;
                if (candidate == null ||
                    !ingredient.filter.Allows(candidate) ||
                    (orderIngredientFilter != null && !orderIngredientFilter.Allows(candidate)) ||
                    (recipeDef.fixedIngredientFilter != null &&
                     !recipeDef.fixedIngredientFilter.Allows(candidate)))
                {
                    continue;
                }

                int candidateRequiredCount = ingredient.CountRequiredOfFor(candidate, recipeDef, null);
                if (candidateRequiredCount <= 0)
                {
                    continue;
                }

                int candidateAvailableCount = availableDef.Count -
                    this.GetReservedCount(reservedCounts, candidate);
                if (candidateAvailableCount < 0)
                {
                    candidateAvailableCount = 0;
                }

                if (bestCandidate == null ||
                    candidateAvailableCount > bestAvailableCount)
                {
                    bestCandidate = candidate;
                    bestRequiredCount = candidateRequiredCount;
                    bestAvailableCount = candidateAvailableCount;
                }

                if (candidateAvailableCount >= candidateRequiredCount)
                {
                    resolvedThingDef = candidate;
                    requiredCount = candidateRequiredCount;
                    availableCount = candidateAvailableCount;
                    label = this.LabelForThingDef(candidate);
                    return true;
                }
            }

            resolvedThingDef = bestCandidate;
            requiredCount = bestRequiredCount > 0 ? bestRequiredCount : 1;
            availableCount = bestAvailableCount;
            label = this.LabelForThingDef(bestCandidate);
            failureReason = "Shuttle cargo lacks enough matching ingredients for recipe " + recipeDef.defName + ".";
            return false;
        }

        private AutoWorkTableIngredientAvailability BuildAvailability(
            bool available,
            List<CargoIngredientRequirement> requirements,
            List<AutoWorkTableIngredientNeedStatus> needs,
            string summary,
            string missingSummary,
            string failureReason)
        {
            return new AutoWorkTableIngredientAvailability(
                available,
                requirements,
                needs,
                !string.IsNullOrEmpty(summary) ? summary : missingSummary,
                missingSummary,
                failureReason);
        }

        private string BuildAvailableSummary(List<AutoWorkTableIngredientNeedStatus> needs)
        {
            if (needs == null || needs.Count == 0)
            {
                return "Ingredients ready.";
            }

            List<string> parts = new List<string>();
            for (int i = 0; i < needs.Count; i++)
            {
                AutoWorkTableIngredientNeedStatus need = needs[i];
                if (need == null)
                {
                    continue;
                }

                parts.Add(need.Label + " " + need.AvailableCount.ToString() + "/" +
                    need.RequiredCount.ToString());
            }

            return parts.Count > 0 ? string.Join(", ", parts.ToArray()) : "Ingredients ready.";
        }

        private string LabelForThingDef(ThingDef thingDef)
        {
            if (thingDef == null)
            {
                return "matching ingredient";
            }

            return !string.IsNullOrEmpty(thingDef.label)
                ? thingDef.LabelCap.ToString()
                : thingDef.defName;
        }

        private int CountSource(
            ShuttleCargoInventorySnapshot inventorySnapshot,
            ThingDef thingDef,
            ShuttleCargoInventorySourceKind sourceKind)
        {
            if (inventorySnapshot == null || thingDef == null || string.IsNullOrEmpty(thingDef.defName))
            {
                return 0;
            }

            IReadOnlyList<CargoStackRef> refs = inventorySnapshot.GetStackRefs(thingDef.defName);
            int count = 0;
            for (int i = 0; i < refs.Count; i++)
            {
                CargoStackRef stackRef = refs[i];
                if (stackRef != null && stackRef.SourceKind == sourceKind)
                {
                    count += stackRef.Count;
                }
            }

            return count;
        }

        private void AddRequirement(
            List<CargoIngredientRequirement> requirements,
            ThingDef thingDef,
            int count)
        {
            if (requirements == null || thingDef == null || count <= 0)
            {
                return;
            }

            for (int i = 0; i < requirements.Count; i++)
            {
                CargoIngredientRequirement requirement = requirements[i];
                if (requirement != null && requirement.ThingDef == thingDef)
                {
                    requirements[i] = new CargoIngredientRequirement(
                        requirement.ThingDef,
                        requirement.Count + count);
                    return;
                }
            }

            requirements.Add(new CargoIngredientRequirement(thingDef, count));
        }

        private void AddReservedCount(
            Dictionary<ThingDef, int> reservedCounts,
            ThingDef thingDef,
            int count)
        {
            if (reservedCounts == null || thingDef == null || count <= 0)
            {
                return;
            }

            int existingCount;
            reservedCounts.TryGetValue(thingDef, out existingCount);
            reservedCounts[thingDef] = existingCount + count;
        }

        private int GetReservedCount(
            Dictionary<ThingDef, int> reservedCounts,
            ThingDef thingDef)
        {
            if (reservedCounts == null || thingDef == null)
            {
                return 0;
            }

            int count;
            return reservedCounts.TryGetValue(thingDef, out count) ? count : 0;
        }

        private int CompareRequirements(
            CargoIngredientRequirement left,
            CargoIngredientRequirement right)
        {
            return string.CompareOrdinal(
                left != null && left.ThingDef != null ? left.ThingDef.defName : string.Empty,
                right != null && right.ThingDef != null ? right.ThingDef.defName : string.Empty);
        }

        private int CompareThingDefNames(ThingDef left, ThingDef right)
        {
            return string.CompareOrdinal(
                left != null ? left.defName : string.Empty,
                right != null ? right.defName : string.Empty);
        }

        private int CompareThingDefCountNames(
            CargoThingDefCount left,
            CargoThingDefCount right)
        {
            return string.CompareOrdinal(
                left != null && left.ThingDef != null ? left.ThingDef.defName : string.Empty,
                right != null && right.ThingDef != null ? right.ThingDef.defName : string.Empty);
        }
    }
}
