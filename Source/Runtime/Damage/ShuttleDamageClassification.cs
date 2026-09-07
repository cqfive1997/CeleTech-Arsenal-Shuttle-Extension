namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Damage
{
    public readonly struct ShuttleDamageClassification
    {
        public readonly ShuttleDamageCategory HullCategory;
        public readonly ShuttleDamageCategory SurfaceCategory;

        public readonly bool IsProjectile;
        public readonly bool IsExplosive;
        public readonly bool IsEMP;
        public readonly bool IsHeat;
        public readonly bool IsDirect;

        public readonly bool HasDamageAmount;

        public ShuttleDamageClassification(
            ShuttleDamageCategory hullCategory,
            ShuttleDamageCategory surfaceCategory,
            bool isProjectile,
            bool isExplosive,
            bool isEMP,
            bool isHeat,
            bool isDirect,
            bool hasDamageAmount)
        {
            this.HullCategory = hullCategory;
            this.SurfaceCategory = surfaceCategory;
            this.IsProjectile = isProjectile;
            this.IsExplosive = isExplosive;
            this.IsEMP = isEMP;
            this.IsHeat = isHeat;
            this.IsDirect = isDirect;
            this.HasDamageAmount = hasDamageAmount;
        }
    }
}
