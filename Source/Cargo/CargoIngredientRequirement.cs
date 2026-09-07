using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo
{
    /// <summary>
    /// Exact cargo ingredient requirement resolved before an atomic broker consume call.
    /// Runtime systems resolve recipe filters to concrete ThingDefs before constructing this.
    /// </summary>
    internal sealed class CargoIngredientRequirement
    {
        public CargoIngredientRequirement(ThingDef thingDef, int count)
        {
            this.ThingDef = thingDef;
            this.Count = count;
        }

        public ThingDef ThingDef { get; private set; }
        public int Count { get; private set; }
    }
}
