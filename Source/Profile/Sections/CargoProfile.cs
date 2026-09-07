namespace CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections
{
    public sealed class CargoProfile
    {
        public CargoProfile(uint cargoRegionCount, float cargoMassCapacityKg)
        {
            this.CargoRegionCount = cargoRegionCount;
            this.CargoMassCapacityKg = cargoMassCapacityKg;
        }

        public uint CargoRegionCount { get; private set; }
        public float CargoMassCapacityKg { get; private set; }
    }
}
