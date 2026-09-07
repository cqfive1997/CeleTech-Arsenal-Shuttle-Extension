namespace CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections
{
    /// <summary>
    /// Derived static mech charger capability. Held mechs and charge progress stay in
    /// the occupancy comp so the profile remains rebuildable read-side data.
    /// </summary>
    public sealed class MechChargerProfile
    {
        private static readonly MechChargerProfile empty = new MechChargerProfile(false, 0, 1f);

        public MechChargerProfile(
            bool hasMechCharger,
            int mechChargeSlots,
            float maxChargeRateFactor)
        {
            this.MechChargeSlots = Max(0, mechChargeSlots);
            this.HasMechCharger = hasMechCharger && this.MechChargeSlots > 0;
            this.MaxChargeRateFactor = IsFinitePositive(maxChargeRateFactor)
                ? maxChargeRateFactor
                : 1f;
        }

        public static MechChargerProfile Empty
        {
            get
            {
                return empty;
            }
        }

        public bool HasMechCharger { get; private set; }
        public int MechChargeSlots { get; private set; }
        public float MaxChargeRateFactor { get; private set; }

        private static bool IsFinitePositive(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;
        }

        private static int Max(int a, int b)
        {
            return a > b ? a : b;
        }
    }
}
