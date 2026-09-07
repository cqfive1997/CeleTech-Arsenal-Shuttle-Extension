namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Shield.Surface
{
    /// <summary>
    /// Detached read-only projection of the active surface shield backend. It must not keep
    /// references to mutable runtime payloads.
    /// </summary>
    internal sealed class ShuttleSurfaceShieldStatusSnapshot
    {
        public string ModuleInstanceID;
        public string ModuleLabel;
        public string ModuleDefName;
        public bool HasActiveShield;
        public bool IsEnabled;
        public bool Online;
        public bool InternalBusPowered;
        public bool Broken;
        public bool RechargingBlocked;
        public bool RechargeStalledForNoEnergy;
        public int CurrentHitPoints;
        public int MaxHitPoints;
        public float HitPointsPercent;
        public int LastHitTick;
        public int BrokenUntilTick;
        public int BrokenTicksLeft;
        public int RechargeBlockedUntilTick;
        public int RechargeBlockedTicksLeft;
        public int LastRechargeTick;
        public int RechargeHitPointsPerInterval;
        public int RechargeIntervalTicks;
        public float RechargeEnergyPerHitPointWd;
        public float RechargeSpeedMultiplier;
        public int EffectiveRechargeHitPointsPerInterval;
        public float EffectiveRechargeEnergyPerIntervalWd;
        public float MinRechargeSpeedMultiplier;
        public float MaxRechargeSpeedMultiplier;
        public float SelectedRadius;
        public float MinRadius;
        public float MaxRadius;
        public string StatusKey;
    }
}
