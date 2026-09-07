namespace CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections
{
    public sealed class FireControlProfile
    {
        public static readonly FireControlProfile Empty = new FireControlProfile(
            false,
            0,
            false,
            false,
            0f,
            1f,
            0f,
            0f,
            1f);

        public FireControlProfile(
            bool hasFireControlRadar,
            int fireControlModuleCount,
            bool supportsAutoDefense,
            bool supportsPointDefense,
            float maxPointDefenseRadius,
            float directFireAccuracyMultiplier,
            float directFireAccuracyBonus,
            float directFireAccuracyFloor,
            float forcedMissRadiusMultiplier)
        {
            this.HasFireControlRadar = hasFireControlRadar;
            this.FireControlModuleCount = fireControlModuleCount;
            this.SupportsAutoDefense = supportsAutoDefense;
            this.SupportsPointDefense = supportsPointDefense;
            this.MaxPointDefenseRadius = maxPointDefenseRadius;
            this.DirectFireAccuracyMultiplier = directFireAccuracyMultiplier;
            this.DirectFireAccuracyBonus = directFireAccuracyBonus;
            this.DirectFireAccuracyFloor = directFireAccuracyFloor;
            this.ForcedMissRadiusMultiplier = forcedMissRadiusMultiplier;
        }

        public bool HasFireControlRadar { get; private set; }

        public int FireControlModuleCount { get; private set; }

        public bool SupportsAutoDefense { get; private set; }

        public bool SupportsPointDefense { get; private set; }

        public float MaxPointDefenseRadius { get; private set; }

        public float DirectFireAccuracyMultiplier { get; private set; }

        public float DirectFireAccuracyBonus { get; private set; }

        public float DirectFireAccuracyFloor { get; private set; }

        public float ForcedMissRadiusMultiplier { get; private set; }
    }
}
