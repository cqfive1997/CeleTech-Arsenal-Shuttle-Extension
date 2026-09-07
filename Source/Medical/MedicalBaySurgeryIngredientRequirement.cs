using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Medical
{
    internal sealed class MedicalBaySurgeryIngredientRequirement
    {
        internal MedicalBaySurgeryIngredientRequirement(
            IReadOnlyList<ThingDef> allowedThingDefs,
            int requiredCount,
            int availableCount,
            string label,
            bool isMedicineRequirement)
        {
            this.AllowedThingDefs = allowedThingDefs ?? new List<ThingDef>();
            this.RequiredCount = requiredCount;
            this.AvailableCount = availableCount;
            this.MissingCount = UnityEngine.Mathf.Max(0, requiredCount - availableCount);
            this.Label = label ?? string.Empty;
            this.IsMedicineRequirement = isMedicineRequirement;
        }

        internal IReadOnlyList<ThingDef> AllowedThingDefs { get; private set; }
        internal int RequiredCount { get; private set; }
        internal int AvailableCount { get; private set; }
        internal int MissingCount { get; private set; }
        internal string Label { get; private set; }
        internal bool IsMedicineRequirement { get; private set; }
    }
}
