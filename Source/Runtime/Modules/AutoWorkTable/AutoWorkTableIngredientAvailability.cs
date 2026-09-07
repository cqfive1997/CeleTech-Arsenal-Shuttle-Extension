using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.AutoWorkTable
{
    internal sealed class AutoWorkTableIngredientAvailability
    {
        internal AutoWorkTableIngredientAvailability(
            bool available,
            List<CargoIngredientRequirement> requirements,
            List<AutoWorkTableIngredientNeedStatus> needs,
            string summary,
            string missingSummary,
            string failureReason)
        {
            this.Available = available;
            this.Requirements = requirements ?? new List<CargoIngredientRequirement>();
            this.Needs = needs ?? new List<AutoWorkTableIngredientNeedStatus>();
            this.Summary = summary;
            this.MissingSummary = missingSummary;
            this.FailureReason = failureReason;
        }

        internal bool Available { get; private set; }
        internal List<CargoIngredientRequirement> Requirements { get; private set; }
        internal List<AutoWorkTableIngredientNeedStatus> Needs { get; private set; }
        internal string Summary { get; private set; }
        internal string MissingSummary { get; private set; }
        internal string FailureReason { get; private set; }
    }

    internal sealed class AutoWorkTableIngredientNeedStatus
    {
        internal AutoWorkTableIngredientNeedStatus(
            string label,
            string defName,
            int requiredCount,
            int availableCount,
            int missingCount,
            int regularCargoCount,
            int refrigeratedCargoCount)
        {
            this.Label = label;
            this.DefName = defName;
            this.RequiredCount = requiredCount;
            this.AvailableCount = availableCount;
            this.MissingCount = missingCount;
            this.RegularCargoCount = regularCargoCount;
            this.RefrigeratedCargoCount = refrigeratedCargoCount;
        }

        internal string Label { get; private set; }
        internal string DefName { get; private set; }
        internal int RequiredCount { get; private set; }
        internal int AvailableCount { get; private set; }
        internal int MissingCount { get; private set; }
        internal int RegularCargoCount { get; private set; }
        internal int RefrigeratedCargoCount { get; private set; }
    }
}
