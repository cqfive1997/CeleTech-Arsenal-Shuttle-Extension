using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo
{
    /// <summary>
    /// Aggregated cargo availability by ThingDef.
    /// </summary>
    internal sealed class CargoThingDefCount
    {
        public CargoThingDefCount(ThingDef thingDef, int count)
        {
            this.ThingDef = thingDef;
            this.Count = count;
        }

        public ThingDef ThingDef { get; private set; }
        public int Count { get; private set; }
    }
}
