namespace CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections
{
    /// <summary>
    /// Gross launch-mass envelope derived from structure/modules and flight energy.
    /// Cargo backend truth is CargoProfile.CargoMassCapacityKg, not RemainingMassCapacity.
    /// </summary>
    public sealed class MassProfile
    {
        public MassProfile(float segmentMass, float moduleMass, float massCapacity)
        {
            this.SegmentMass = segmentMass;
            this.ModuleMass = moduleMass;
            this.MassCapacity = massCapacity;
            this.TotalMass = segmentMass + moduleMass;
            this.RemainingMassCapacity = massCapacity - this.TotalMass;
            if (this.RemainingMassCapacity < 0f)
            {
                this.RemainingMassCapacity = 0f;
            }
        }

        public float SegmentMass { get; private set; }
        public float ModuleMass { get; private set; }

        // Gross launch mass envelope, including shuttle structure/modules plus loaded cargo.
        public float MassCapacity { get; private set; }

        public float TotalMass { get; private set; }

        // Legacy/UI convenience for "gross envelope minus structure".
        // Do not use this as cargo backend capacity; use CargoProfile.CargoMassCapacityKg.
        public float RemainingMassCapacity { get; private set; }
    }
}
