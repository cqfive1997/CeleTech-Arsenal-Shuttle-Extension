namespace CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections
{
    public sealed class HullProfile
    {
        public static readonly HullProfile Empty = new HullProfile(
            false,
            0,
            0,
            1f,
            1f,
            1f,
            1f,
            1f,
            0f,
            0f,
            3f);

        public HullProfile(
            bool hasHull,
            int maxHitPoints,
            int armorModuleCount,
            float sharpDamageMultiplier,
            float bluntDamageMultiplier,
            float heatDamageMultiplier,
            float explosionDamageMultiplier,
            float empDamageMultiplier,
            float flatDamageReduction,
            float minLaunchIntegrityPct,
            float repairMaterialStagingRadius)
        {
            this.HasHull = hasHull;
            this.MaxHitPoints = maxHitPoints;
            this.ArmorModuleCount = armorModuleCount;
            this.SharpDamageMultiplier = sharpDamageMultiplier;
            this.BluntDamageMultiplier = bluntDamageMultiplier;
            this.HeatDamageMultiplier = heatDamageMultiplier;
            this.ExplosionDamageMultiplier = explosionDamageMultiplier;
            this.EmpDamageMultiplier = empDamageMultiplier;
            this.FlatDamageReduction = flatDamageReduction;
            this.MinLaunchIntegrityPct = minLaunchIntegrityPct;
            this.RepairMaterialStagingRadius = repairMaterialStagingRadius;
        }

        public bool HasHull { get; private set; }

        public int MaxHitPoints { get; private set; }

        public int ArmorModuleCount { get; private set; }

        public float SharpDamageMultiplier { get; private set; }

        public float BluntDamageMultiplier { get; private set; }

        public float HeatDamageMultiplier { get; private set; }

        public float ExplosionDamageMultiplier { get; private set; }

        public float EmpDamageMultiplier { get; private set; }

        public float FlatDamageReduction { get; private set; }

        public float MinLaunchIntegrityPct { get; private set; }

        public float RepairMaterialStagingRadius { get; private set; }
    }
}
