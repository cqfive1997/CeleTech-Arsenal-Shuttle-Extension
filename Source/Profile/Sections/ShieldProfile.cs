namespace CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections
{
    public sealed class ShieldProfile
    {
        // Legacy static-capacity compatibility constructor. The old profile shape only
        // exposed one capacity value, so treat any non-zero value as one static surface
        // shield capability, never as a module count.
        public ShieldProfile(uint shieldCapacity)
            : this(
                shieldCapacity > 0u,
                false,
                shieldCapacity > 0u,
                0,
                shieldCapacity > 0u ? 1 : 0,
                0,
                0,
                shieldCapacity > int.MaxValue ? int.MaxValue : (int)shieldCapacity,
                shieldCapacity > int.MaxValue ? int.MaxValue : (int)shieldCapacity,
                0f)
        {
        }

        public ShieldProfile(
            bool hasAnyShield,
            bool hasVanillaInterceptorShield,
            bool hasSurfaceShield,
            int vanillaInterceptorShieldModuleCount,
            int surfaceShieldModuleCount,
            int totalVanillaInterceptorShieldMaxHitPoints,
            int bestVanillaInterceptorShieldMaxHitPoints,
            int totalSurfaceShieldMaxHitPoints,
            int bestSurfaceShieldMaxHitPoints,
            float bestSurfaceShieldRechargeEnergyPerHitPointWd)
        {
            this.HasAnyShield = hasAnyShield;
            this.HasVanillaInterceptorShield = hasVanillaInterceptorShield;
            this.HasSurfaceShield = hasSurfaceShield;
            this.VanillaInterceptorShieldModuleCount = vanillaInterceptorShieldModuleCount;
            this.SurfaceShieldModuleCount = surfaceShieldModuleCount;
            this.TotalVanillaInterceptorShieldMaxHitPoints = totalVanillaInterceptorShieldMaxHitPoints;
            this.BestVanillaInterceptorShieldMaxHitPoints = bestVanillaInterceptorShieldMaxHitPoints;
            this.TotalSurfaceShieldMaxHitPoints = totalSurfaceShieldMaxHitPoints;
            this.BestSurfaceShieldMaxHitPoints = bestSurfaceShieldMaxHitPoints;
            this.BestSurfaceShieldRechargeEnergyPerHitPointWd = bestSurfaceShieldRechargeEnergyPerHitPointWd;
            this.ShieldCapacity = this.ToShieldCapacity(
                totalSurfaceShieldMaxHitPoints,
                totalVanillaInterceptorShieldMaxHitPoints);
        }

        public uint ShieldCapacity { get; private set; }

        public bool HasAnyShield { get; private set; }

        public bool HasVanillaInterceptorShield { get; private set; }

        public bool HasSurfaceShield { get; private set; }

        public int VanillaInterceptorShieldModuleCount { get; private set; }

        public int SurfaceShieldModuleCount { get; private set; }

        public int TotalVanillaInterceptorShieldMaxHitPoints { get; private set; }

        public int BestVanillaInterceptorShieldMaxHitPoints { get; private set; }

        public int TotalSurfaceShieldMaxHitPoints { get; private set; }

        public int BestSurfaceShieldMaxHitPoints { get; private set; }

        public float BestSurfaceShieldRechargeEnergyPerHitPointWd { get; private set; }

        private uint ToShieldCapacity(
            int totalSurfaceShieldMaxHitPoints,
            int totalVanillaInterceptorShieldMaxHitPoints)
        {
            ulong total = this.ToNonNegativeUInt64(totalSurfaceShieldMaxHitPoints) +
                this.ToNonNegativeUInt64(totalVanillaInterceptorShieldMaxHitPoints);
            return total > uint.MaxValue ? uint.MaxValue : (uint)total;
        }

        private ulong ToNonNegativeUInt64(int value)
        {
            return value > 0 ? (ulong)value : 0ul;
        }
    }
}
