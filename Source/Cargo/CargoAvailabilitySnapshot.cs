using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo
{
    /// <summary>
    /// Read-only cargo availability view for one module operation. It stores ThingDef counts only.
    /// </summary>
    internal sealed class CargoAvailabilitySnapshot
    {
        private readonly Dictionary<ThingDef, int> countsByThingDef;
        private readonly List<CargoThingDefCount> thingDefCounts;

        internal CargoAvailabilitySnapshot(
            int createdTick,
            bool isAvailable,
            Dictionary<ThingDef, int> countsByThingDef,
            int scannedStackCount)
        {
            this.CreatedTick = createdTick;
            this.IsAvailable = isAvailable;
            this.countsByThingDef = countsByThingDef ?? new Dictionary<ThingDef, int>();
            this.thingDefCounts = this.BuildThingDefCounts(this.countsByThingDef);
            this.ScannedStackCount = scannedStackCount;
        }

        internal int CreatedTick { get; private set; }

        internal bool IsAvailable { get; private set; }

        internal IReadOnlyDictionary<ThingDef, int> CountsByThingDef
        {
            get
            {
                return this.countsByThingDef;
            }
        }

        internal IReadOnlyList<CargoThingDefCount> ThingDefCounts
        {
            get
            {
                return this.thingDefCounts;
            }
        }

        internal int ScannedStackCount { get; private set; }

        internal int CountAvailable(ThingDef thingDef)
        {
            if (!this.IsAvailable || thingDef == null)
            {
                return 0;
            }

            int count;
            return this.countsByThingDef.TryGetValue(thingDef, out count) ? count : 0;
        }

        internal IReadOnlyList<CargoThingDefCount> GetAvailableThingDefs(
            ThingFilter ingredientFilter,
            ThingFilter fixedIngredientFilter)
        {
            List<CargoThingDefCount> result = new List<CargoThingDefCount>();
            if (!this.IsAvailable || ingredientFilter == null)
            {
                return result;
            }

            for (int i = 0; i < this.thingDefCounts.Count; i++)
            {
                CargoThingDefCount count = this.thingDefCounts[i];
                ThingDef thingDef = count != null ? count.ThingDef : null;
                if (thingDef == null ||
                    !ingredientFilter.Allows(thingDef) ||
                    (fixedIngredientFilter != null && !fixedIngredientFilter.Allows(thingDef)))
                {
                    continue;
                }

                result.Add(count);
            }

            return result;
        }

        private List<CargoThingDefCount> BuildThingDefCounts(
            Dictionary<ThingDef, int> counts)
        {
            List<CargoThingDefCount> result = new List<CargoThingDefCount>();
            if (counts == null)
            {
                return result;
            }

            foreach (KeyValuePair<ThingDef, int> pair in counts)
            {
                if (pair.Key == null || pair.Value <= 0)
                {
                    continue;
                }

                result.Add(new CargoThingDefCount(pair.Key, pair.Value));
            }

            result.Sort(this.CompareThingDefCountNames);
            return result;
        }

        private int CompareThingDefCountNames(
            CargoThingDefCount left,
            CargoThingDefCount right)
        {
            string leftName = left != null && left.ThingDef != null
                ? left.ThingDef.defName
                : string.Empty;
            string rightName = right != null && right.ThingDef != null
                ? right.ThingDef.defName
                : string.Empty;
            return string.CompareOrdinal(leftName, rightName);
        }
    }
}
