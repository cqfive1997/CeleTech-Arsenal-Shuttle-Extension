using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Shield
{
    /// <summary>
    /// Detached read projection for common shield settings. Volatile backend internals are
    /// intentionally excluded because those belong to the active shield backend.
    /// </summary>
    internal sealed class ShuttleShieldSettingsSnapshot
    {
        public string ModuleInstanceID;
        public bool SupportsRadiusControl;
        public float SelectedRadius;
        public float MinRadius;
        public float MaxRadius;
        public float DefaultRadius;
    }

    internal interface IShuttleShieldReadPort
    {
        bool TryGetShieldSettings(string moduleInstanceID, out ShuttleShieldSettingsSnapshot snapshot);
    }

    /// <summary>
    /// Read-only static/runtime inputs needed by the vanilla projectile-interceptor backend.
    /// Current hit points and cooldown are intentionally not included.
    /// </summary>
    internal sealed class ShuttleProjectileInterceptorShieldSnapshot
    {
        public string ModuleInstanceID;
        public float SelectedRadius;
        public int ShieldHitPoints;
        public int HitPointsPerRechargeInterval;
        public int RechargeIntervalTicks;
        public float RechargeEnergyPerHitPointWd;
        public bool InterceptGroundProjectiles;
        public bool InterceptAirProjectiles;
        public bool InterceptNonHostileProjectiles;
        public int CooldownTicks;
        public EffecterDef InterceptEffect;
        public Color Color;
        public SoundDef ActiveSound;
        public EffecterDef ReactivateEffect;
    }

    internal sealed class ShuttleProjectileInterceptorBackendStatusSnapshot
    {
        // Read-only telemetry borrowed from the vanilla interceptor backend for UI/inspect display.
        // This is not runtime-state truth and must not be saved by shuttle systems.
        public string ModuleInstanceID;
        public bool HasActiveShield;
        public bool Active;
        public bool Charging;
        public bool OnCooldown;
        public int CurrentHitPoints;
        public int MaxHitPoints;
        public int ChargingTicksLeft;
        public int CooldownTicksLeft;
    }

    internal interface IShuttleProjectileInterceptorShieldPort
    {
        bool TryGetActiveProjectileInterceptorShield(
            out ShuttleProjectileInterceptorShieldSnapshot snapshot);

        bool TryConsumeShieldRechargeEnergy(float amountWd);
    }
}
