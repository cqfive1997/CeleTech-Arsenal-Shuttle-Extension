using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo
{
    internal enum ShuttleCargoInventorySourceKind
    {
        RegularCargo,
        RefrigeratedCargo
    }

    internal sealed class CargoStackRef
    {
        internal CargoStackRef(
            int thingIDNumber,
            string defName,
            int count,
            ShuttleCargoInventorySourceKind sourceKind,
            int sourceIndex,
            string moduleInstanceID)
        {
            this.ThingIDNumber = thingIDNumber;
            this.DefName = defName;
            this.Count = count;
            this.SourceKind = sourceKind;
            this.SourceIndex = sourceIndex;
            this.ModuleInstanceID = moduleInstanceID;
        }

        internal int ThingIDNumber { get; private set; }
        internal string DefName { get; private set; }
        internal int Count { get; private set; }
        internal ShuttleCargoInventorySourceKind SourceKind { get; private set; }
        internal int SourceIndex { get; private set; }
        internal string ModuleInstanceID { get; private set; }
    }

    internal sealed class ShuttleCargoInventorySnapshot
    {
        private readonly Dictionary<ThingDef, int> stackCountByThingDef;
        private readonly Dictionary<string, int> stackCountByDefName;
        private readonly Dictionary<string, List<CargoStackRef>> stackRefsByDefName;
        private readonly List<CargoThingDefCount> thingDefCounts;
        private readonly List<CargoStackRef> stackRefs;

        internal ShuttleCargoInventorySnapshot(
            int revision,
            int builtAtTick,
            Dictionary<ThingDef, int> stackCountByThingDef,
            Dictionary<string, int> stackCountByDefName,
            Dictionary<string, List<CargoStackRef>> stackRefsByDefName,
            float totalMassKg,
            bool includesRegularCargo,
            bool includesRefrigeratedCargo,
            int scannedStackCount,
            bool isAvailable,
            string unavailableReason)
        {
            this.Revision = revision;
            this.BuiltAtTick = builtAtTick;
            this.stackCountByThingDef = stackCountByThingDef ?? new Dictionary<ThingDef, int>();
            this.stackCountByDefName = stackCountByDefName ?? new Dictionary<string, int>();
            this.stackRefsByDefName = stackRefsByDefName ?? new Dictionary<string, List<CargoStackRef>>();
            this.TotalMassKg = totalMassKg;
            this.IncludesRegularCargo = includesRegularCargo;
            this.IncludesRefrigeratedCargo = includesRefrigeratedCargo;
            this.ScannedStackCount = scannedStackCount;
            this.IsAvailable = isAvailable;
            this.UnavailableReason = unavailableReason;
            this.thingDefCounts = this.BuildThingDefCounts(this.stackCountByThingDef);
            this.stackRefs = this.BuildStackRefs(this.stackRefsByDefName);
        }

        internal int Revision { get; private set; }
        internal int BuiltAtTick { get; private set; }
        internal float TotalMassKg { get; private set; }
        internal bool IncludesRegularCargo { get; private set; }
        internal bool IncludesRefrigeratedCargo { get; private set; }
        internal int ScannedStackCount { get; private set; }
        internal bool IsAvailable { get; private set; }
        internal string UnavailableReason { get; private set; }

        internal IReadOnlyDictionary<string, int> StackCountByDefName
        {
            get
            {
                return this.stackCountByDefName;
            }
        }

        internal IReadOnlyList<CargoThingDefCount> ThingDefCounts
        {
            get
            {
                return this.thingDefCounts;
            }
        }

        internal IReadOnlyList<CargoStackRef> StackRefs
        {
            get { return this.stackRefs; }
        }

        internal int Count(ThingDef thingDef)
        {
            if (thingDef == null)
            {
                return 0;
            }

            int count;
            return this.stackCountByThingDef.TryGetValue(thingDef, out count) ? count : 0;
        }

        internal int Count(string defName)
        {
            if (string.IsNullOrEmpty(defName))
            {
                return 0;
            }

            int count;
            return this.stackCountByDefName.TryGetValue(defName, out count) ? count : 0;
        }

        internal IReadOnlyList<CargoStackRef> GetStackRefs(string defName)
        {
            List<CargoStackRef> refs;
            return !string.IsNullOrEmpty(defName) &&
                this.stackRefsByDefName.TryGetValue(defName, out refs)
                ? refs
                : new List<CargoStackRef>();
        }

        internal IReadOnlyList<CargoThingDefCount> GetThingDefs(
            ThingFilter ingredientFilter,
            ThingFilter fixedIngredientFilter)
        {
            List<CargoThingDefCount> result = new List<CargoThingDefCount>();
            if (ingredientFilter == null)
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

        private List<CargoThingDefCount> BuildThingDefCounts(Dictionary<ThingDef, int> counts)
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

        private List<CargoStackRef> BuildStackRefs(
            Dictionary<string, List<CargoStackRef>> refsByDefName)
        {
            List<CargoStackRef> result = new List<CargoStackRef>();
            if (refsByDefName == null)
            {
                return result;
            }

            foreach (KeyValuePair<string, List<CargoStackRef>> pair in refsByDefName)
            {
                if (pair.Value != null)
                {
                    result.AddRange(pair.Value);
                }
            }

            result.Sort(delegate(CargoStackRef left, CargoStackRef right)
            {
                if (left == null || right == null)
                {
                    return left == right ? 0 : (left == null ? 1 : -1);
                }

                int sourceKindCompare = left.SourceKind.CompareTo(right.SourceKind);
                if (sourceKindCompare != 0)
                {
                    return sourceKindCompare;
                }

                int sourceIndexCompare = left.SourceIndex.CompareTo(right.SourceIndex);
                return sourceIndexCompare != 0
                    ? sourceIndexCompare
                    : left.ThingIDNumber.CompareTo(right.ThingIDNumber);
            });
            return result;
        }

        private int CompareThingDefCountNames(CargoThingDefCount left, CargoThingDefCount right)
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
