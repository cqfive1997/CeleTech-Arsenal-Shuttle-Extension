using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo
{
    internal sealed class ShuttleCargoResourceRequirementPlanner
    {
        internal List<CargoIngredientRequirement> NormalizeRequirements(
            IReadOnlyList<CargoIngredientRequirement> requirements,
            out int requiredTotal,
            out string failureReason)
        {
            requiredTotal = 0;
            failureReason = null;

            if (requirements == null || requirements.Count == 0)
            {
                failureReason = "No cargo ingredient requirements were provided.";
                return null;
            }

            List<CargoIngredientRequirement> normalized = new List<CargoIngredientRequirement>();
            for (int i = 0; i < requirements.Count; i++)
            {
                CargoIngredientRequirement requirement = requirements[i];
                if (requirement == null || requirement.ThingDef == null || requirement.Count <= 0)
                {
                    failureReason = "Invalid cargo ingredient requirement.";
                    return null;
                }

                int existingIndex = this.IndexOfRequirement(normalized, requirement.ThingDef);
                if (existingIndex >= 0)
                {
                    CargoIngredientRequirement existing = normalized[existingIndex];
                    normalized[existingIndex] = new CargoIngredientRequirement(
                        existing.ThingDef,
                        existing.Count + requirement.Count);
                }
                else
                {
                    normalized.Add(new CargoIngredientRequirement(
                        requirement.ThingDef,
                        requirement.Count));
                }

                requiredTotal += requirement.Count;
            }

            normalized.Sort(this.CompareRequirementNames);
            return normalized;
        }

        private int IndexOfRequirement(
            List<CargoIngredientRequirement> requirements,
            ThingDef thingDef)
        {
            if (requirements == null || thingDef == null)
            {
                return -1;
            }

            for (int i = 0; i < requirements.Count; i++)
            {
                CargoIngredientRequirement requirement = requirements[i];
                if (requirement != null && requirement.ThingDef == thingDef)
                {
                    return i;
                }
            }

            return -1;
        }

        private int CompareRequirementNames(
            CargoIngredientRequirement left,
            CargoIngredientRequirement right)
        {
            string leftName = left != null
                ? ShuttleCargoResourceMatcher.GetThingDefName(left.ThingDef)
                : string.Empty;
            string rightName = right != null
                ? ShuttleCargoResourceMatcher.GetThingDefName(right.ThingDef)
                : string.Empty;
            return string.CompareOrdinal(leftName, rightName);
        }
    }
}
