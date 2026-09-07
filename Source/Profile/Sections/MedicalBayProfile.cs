namespace CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections
{
    /// <summary>
    /// Derived static Medical Bay capability. Live patients, treatment reservations,
    /// medicine transactions, and comfort ticks stay out of the rebuildable profile.
    /// </summary>
    public sealed class MedicalBayProfile
    {
        private static readonly MedicalBayProfile empty = new MedicalBayProfile(
            false,
            0,
            false,
            false,
            false,
            1f,
            0.5f,
            0);

        public MedicalBayProfile(
            bool hasMedicalBay,
            int medicalPatientSlots,
            bool supportsMedevacPriority,
            bool supportsStabilization,
            bool supportsPassiveComfort,
            float passiveJoyGainFactor,
            float passiveJoyCapPct,
            int comfortThoughtStageIndex)
        {
            this.MedicalPatientSlots = Max(0, medicalPatientSlots);
            this.HasMedicalBay = hasMedicalBay;
            this.SupportsMedevacPriority =
                this.HasMedicalBay && supportsMedevacPriority && this.MedicalPatientSlots > 0;
            this.SupportsStabilization =
                this.HasMedicalBay && supportsStabilization && this.MedicalPatientSlots > 0;
            this.SupportsPassiveComfort =
                this.HasMedicalBay && supportsPassiveComfort && this.MedicalPatientSlots > 0;
            this.PassiveJoyGainFactor = IsFinitePositive(passiveJoyGainFactor)
                ? passiveJoyGainFactor
                : 1f;
            this.PassiveJoyCapPct = ClampPositivePct(passiveJoyCapPct);
            this.ComfortThoughtStageIndex = Max(0, comfortThoughtStageIndex);
        }

        public static MedicalBayProfile Empty
        {
            get
            {
                return empty;
            }
        }

        public bool HasMedicalBay { get; private set; }
        public int MedicalPatientSlots { get; private set; }
        public bool SupportsMedevacPriority { get; private set; }
        public bool SupportsStabilization { get; private set; }
        public bool SupportsPassiveComfort { get; private set; }
        public float PassiveJoyGainFactor { get; private set; }
        public float PassiveJoyCapPct { get; private set; }
        public int ComfortThoughtStageIndex { get; private set; }

        private static float ClampPositivePct(float value)
        {
            if (!IsFinitePositive(value))
            {
                return 0.5f;
            }

            return value > 1f ? 1f : value;
        }

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
