using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Shield;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    public sealed class CompProperties_ModularShuttleProjectileInterceptor :
        CompProperties_ProjectileInterceptor
    {
        public CompProperties_ModularShuttleProjectileInterceptor()
        {
            // The host comp is always present, but starts inert until an installed module enables it.
            this.compClass = typeof(CompModularShuttleProjectileInterceptor);
            this.hitPoints = 0;
            this.startWithMaxHitPoints = false;
        }
    }

    /// <summary>
    /// Vanilla projectile-interceptor backend for installed shuttle shield modules.
    /// It adapts module/runtime settings into an instance-local CompProjectileInterceptor props clone.
    /// </summary>
    public sealed class CompModularShuttleProjectileInterceptor : CompProjectileInterceptor
    {
        private CompProperties_ModularShuttleProjectileInterceptor instanceProps;
        private IShuttleProjectileInterceptorShieldPort shieldPort;
        private ShuttleProjectileInterceptorShieldSnapshot activeShield;
        private bool hasActiveShield;
        private bool warnedUnsafeProps;
        private int nextDebugLogTick;
        private int lastObservedCooldownTicksLeft;

        protected override int NumInactiveDots
        {
            get
            {
                return this.hasActiveShield ? base.NumInactiveDots : 0;
            }
        }

        protected override int HitPointsPerInterval
        {
            get
            {
                if (!this.hasActiveShield ||
                    this.activeShield == null ||
                    this.currentHitPoints >= this.HitPointsMax)
                {
                    return 0;
                }

                int hitPoints = this.activeShield.HitPointsPerRechargeInterval;
                if (hitPoints <= 0)
                {
                    return 0;
                }

                float energyCostWd = hitPoints * this.activeShield.RechargeEnergyPerHitPointWd;
                if (energyCostWd <= 0f)
                {
                    return hitPoints;
                }

                // Recharge spends stored shuttle energy only. It never pulls from PowerNet or CompPowerTrader.
                IShuttleProjectileInterceptorShieldPort port = this.ResolveShieldPort();
                if (port == null || !port.TryConsumeShieldRechargeEnergy(energyCostWd))
                {
                    this.LogDebugThrottled(
                        "Recharge skipped for " + this.GetDebugModuleID() +
                        ": unavailable stored energy for " + energyCostWd.ToString("0.###") + " Wd.");
                    return 0;
                }

                this.LogDebugThrottled(
                    "Recharge consumed " + energyCostWd.ToString("0.###") + " Wd for " +
                    hitPoints + " shield HP on " + this.GetDebugModuleID() + ".");
                return hitPoints;
            }
        }

        public override void Initialize(CompProperties props)
        {
            // Vanilla interception reads Props.radius directly, so runtime changes must target
            // this comp's private clone rather than the shared ThingDef comp properties.
            this.instanceProps = this.CloneProps(props as CompProperties_ProjectileInterceptor);
            base.Initialize(this.instanceProps);
            this.WarnIfPropsUnsafe("Initialize");
            this.ApplyInactiveShield();
        }

        public override void PostPostMake()
        {
            base.PostPostMake();
            this.ApplyInactiveShield();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.RefreshFromPort();
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            this.RefreshFromPort();
        }

        public override void CompTick()
        {
            this.RefreshFromPort();
            base.CompTick();
            this.ClampCurrentHitPointsToMax();
            this.LogPossibleInterception();
        }

        public override string CompInspectStringExtra()
        {
            string baseInspect = base.CompInspectStringExtra();
            if (!this.hasActiveShield || this.activeShield == null)
            {
                return baseInspect;
            }

            string shieldLine = "CT_Shuttle_ShieldInspectLine".Translate(
                this.GetLocalizedStatus(),
                this.Max(0, this.currentHitPoints),
                this.Max(0, this.HitPointsMax),
                this.InstanceProps.radius.ToString("0.#")).ToString();

            if (string.IsNullOrEmpty(baseInspect))
            {
                return shieldLine;
            }

            return baseInspect + "\n" + shieldLine;
        }

        internal ShuttleProjectileInterceptorBackendStatusSnapshot BuildStatusSnapshot()
        {
            ShuttleProjectileInterceptorBackendStatusSnapshot snapshot =
                new ShuttleProjectileInterceptorBackendStatusSnapshot();
            snapshot.ModuleInstanceID = this.activeShield != null
                ? this.activeShield.ModuleInstanceID
                : null;
            snapshot.HasActiveShield = this.hasActiveShield;
            snapshot.CurrentHitPoints = this.Max(0, this.currentHitPoints);
            snapshot.MaxHitPoints = this.Max(0, this.HitPointsMax);
            snapshot.Active = this.hasActiveShield && this.Active;
            snapshot.Charging = this.hasActiveShield && this.Charging;
            snapshot.OnCooldown = this.hasActiveShield && this.OnCooldown;
            snapshot.ChargingTicksLeft = this.hasActiveShield ? this.ChargingTicksLeft : 0;
            snapshot.CooldownTicksLeft = this.hasActiveShield ? this.CooldownTicksLeft : 0;
            return snapshot;
        }

        private void RefreshFromPort()
        {
            IShuttleProjectileInterceptorShieldPort port = this.ResolveShieldPort();
            ShuttleProjectileInterceptorShieldSnapshot snapshot;
            if (port == null ||
                !port.TryGetActiveProjectileInterceptorShield(out snapshot) ||
                snapshot == null ||
                snapshot.ShieldHitPoints <= 0)
            {
                if (this.hasActiveShield)
                {
                    this.LogDebugThrottled("Shield sync disabled the interceptor for " + this.GetDebugModuleID() + ".");
                }

                // Missing, removed, disabled, or unsupported shield modules make the static host comp inert.
                this.ApplyInactiveShield();
                return;
            }

            this.ApplyShield(snapshot);
        }

        private IShuttleProjectileInterceptorShieldPort ResolveShieldPort()
        {
            if (this.shieldPort != null)
            {
                return this.shieldPort;
            }

            CompModularShuttleCore core = this.parent != null
                ? this.parent.TryGetComp<CompModularShuttleCore>()
                : null;
            // The comp depends only on the shield port; the core comp owns controller access internally.
            this.shieldPort = core as IShuttleProjectileInterceptorShieldPort;
            return this.shieldPort;
        }

        private void ApplyShield(ShuttleProjectileInterceptorShieldSnapshot snapshot)
        {
            CompProperties_ModularShuttleProjectileInterceptor props = this.InstanceProps;
            this.WarnIfPropsUnsafe("ApplyShield");
            int previousMax = this.maxHitPointsOverride.HasValue
                ? this.maxHitPointsOverride.Value
                : 0;
            int nextMax = this.Max(0, snapshot.ShieldHitPoints);
            bool changedShieldInput = !this.hasActiveShield ||
                this.activeShield == null ||
                this.activeShield.ModuleInstanceID != snapshot.ModuleInstanceID ||
                previousMax != nextMax ||
                Mathf.Abs(props.radius - snapshot.SelectedRadius) > 0.001f;

            this.hasActiveShield = true;
            this.activeShield = snapshot;
            this.maxHitPointsOverride = nextMax;

            if (previousMax <= 0 || this.currentHitPoints < 0)
            {
                // Installing or re-enabling a shield starts empty. Recharge is the only refill path.
                this.currentHitPoints = 0;
            }

            this.ClampCurrentHitPointsToMax();

            // All values below mutate the instance-local props clone, never the XML/shared Def props.
            props.hitPoints = nextMax;
            props.radius = this.SanitizeNonNegativeFinite(snapshot.SelectedRadius);
            props.interceptGroundProjectiles = snapshot.InterceptGroundProjectiles;
            props.interceptAirProjectiles = snapshot.InterceptAirProjectiles;
            props.interceptNonHostileProjectiles = snapshot.InterceptNonHostileProjectiles;
            props.cooldownTicks = this.Max(0, snapshot.CooldownTicks);
            props.rechargeHitPointsIntervalTicks = this.Max(1, snapshot.RechargeIntervalTicks);
            props.interceptEffect = snapshot.InterceptEffect;
            props.color = this.SanitizeColor(snapshot.Color);
            props.activeSound = snapshot.ActiveSound;
            props.reactivateEffect = snapshot.ReactivateEffect;

            if (changedShieldInput)
            {
                this.LogDebugThrottled(
                    "Shield sync applied " + this.GetDebugModuleID() +
                    " radius " + props.radius.ToString("0.#") +
                    ", max HP " + nextMax + ".");
            }
        }

        private void ApplyInactiveShield()
        {
            CompProperties_ModularShuttleProjectileInterceptor props = this.InstanceProps;
            this.WarnIfPropsUnsafe("ApplyInactiveShield");

            // Keep the vanilla comp attached but incapable of intercepting while no eligible module exists.
            this.hasActiveShield = false;
            this.activeShield = null;
            this.maxHitPointsOverride = 0;
            this.currentHitPoints = 0;
            this.lastObservedCooldownTicksLeft = 0;

            props.hitPoints = 0;
            props.radius = 0f;
            props.interceptGroundProjectiles = false;
            props.interceptAirProjectiles = false;
            props.interceptNonHostileProjectiles = false;
            props.cooldownTicks = 0;
            props.rechargeHitPointsIntervalTicks = 1;
            props.interceptEffect = null;
            props.reactivateEffect = null;
        }

        private void ClampCurrentHitPointsToMax()
        {
            int maxHitPoints = this.HitPointsMax;
            if (maxHitPoints <= 0)
            {
                this.currentHitPoints = 0;
                return;
            }

            if (this.currentHitPoints < 0)
            {
                this.currentHitPoints = 0;
            }
            else if (this.currentHitPoints > maxHitPoints)
            {
                this.currentHitPoints = maxHitPoints;
            }
        }

        private CompProperties_ModularShuttleProjectileInterceptor InstanceProps
        {
            get
            {
                if (this.instanceProps == null)
                {
                    this.instanceProps = this.CloneProps(null);
                    this.props = this.instanceProps;
                    this.WarnIfPropsUnsafe("InstanceProps");
                }

                return this.instanceProps;
            }
        }

        private CompProperties_ModularShuttleProjectileInterceptor CloneProps(
            CompProperties_ProjectileInterceptor source)
        {
            CompProperties_ModularShuttleProjectileInterceptor clone =
                new CompProperties_ModularShuttleProjectileInterceptor();

            if (source == null)
            {
                return clone;
            }

            clone.radius = source.radius;
            clone.cooldownTicks = source.cooldownTicks;
            clone.disarmedByEmpForTicks = source.disarmedByEmpForTicks;
            clone.interceptGroundProjectiles = source.interceptGroundProjectiles;
            clone.interceptAirProjectiles = source.interceptAirProjectiles;
            clone.interceptNonHostileProjectiles = source.interceptNonHostileProjectiles;
            clone.interceptOutgoingProjectiles = source.interceptOutgoingProjectiles;
            clone.drawWithNoSelection = source.drawWithNoSelection;
            clone.chargeIntervalTicks = source.chargeIntervalTicks;
            clone.chargeDurationTicks = source.chargeDurationTicks;
            clone.minAlpha = source.minAlpha;
            clone.idlePulseSpeed = source.idlePulseSpeed;
            clone.minIdleAlpha = source.minIdleAlpha;
            clone.hitPoints = source.hitPoints;
            clone.rechargeHitPointsIntervalTicks = source.rechargeHitPointsIntervalTicks;
            clone.gizmoTipKey = source.gizmoTipKey;
            clone.hitPointsRestoreInstantlyAfterCharge = source.hitPointsRestoreInstantlyAfterCharge;
            clone.startWithMaxHitPoints = source.startWithMaxHitPoints;
            clone.alwaysShowHitpointsGizmo = source.alwaysShowHitpointsGizmo;
            clone.activated = source.activated;
            clone.activeDuration = source.activeDuration;
            clone.color = source.color;
            clone.reactivateEffect = source.reactivateEffect;
            clone.interceptEffect = source.interceptEffect;
            clone.activeSound = source.activeSound;
            return clone;
        }

        private float SanitizeNonNegativeFinite(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
            {
                return 0f;
            }

            return value;
        }

        private Color SanitizeColor(Color value)
        {
            if (float.IsNaN(value.r) ||
                float.IsInfinity(value.r) ||
                float.IsNaN(value.g) ||
                float.IsInfinity(value.g) ||
                float.IsNaN(value.b) ||
                float.IsInfinity(value.b) ||
                float.IsNaN(value.a) ||
                float.IsInfinity(value.a))
            {
                return Color.white;
            }

            return value;
        }

        private int Max(int a, int b)
        {
            return a > b ? a : b;
        }

        private void WarnIfPropsUnsafe(string context)
        {
            if (this.warnedUnsafeProps)
            {
                return;
            }

            if (this.instanceProps == null || this.props == null)
            {
                this.warnedUnsafeProps = true;
                ShuttleLog.Warn(
                    "Shield",
                    context + " found missing cloned projectile-interceptor props.");
                return;
            }

            if (!object.ReferenceEquals(this.props, this.instanceProps) || this.IsSharedDefProps(this.props))
            {
                this.warnedUnsafeProps = true;
                ShuttleLog.Warn(
                    "Shield",
                    context + " found projectile-interceptor props are not instance-local. Shared Def props must not be mutated.");
            }
        }

        private bool IsSharedDefProps(CompProperties candidate)
        {
            if (candidate == null || this.parent == null || this.parent.def == null || this.parent.def.comps == null)
            {
                return false;
            }

            for (int i = 0; i < this.parent.def.comps.Count; i++)
            {
                if (object.ReferenceEquals(candidate, this.parent.def.comps[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private void LogPossibleInterception()
        {
            if (!this.hasActiveShield)
            {
                this.lastObservedCooldownTicksLeft = 0;
                return;
            }

            int cooldownTicksLeft = this.CooldownTicksLeft;
            if (cooldownTicksLeft > 0 && cooldownTicksLeft > this.lastObservedCooldownTicksLeft)
            {
                this.LogDebugThrottled(
                    "Shield interception/cooldown observed for " + this.GetDebugModuleID() +
                    ": " + cooldownTicksLeft + " tick(s) remaining.");
            }

            this.lastObservedCooldownTicksLeft = cooldownTicksLeft;
        }

        private void LogDebugThrottled(string message)
        {
            if (!ShuttleLog.IsVerboseLogging)
            {
                return;
            }

            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (ticksGame < this.nextDebugLogTick)
            {
                return;
            }

            this.nextDebugLogTick = ticksGame + 250;
            ShuttleLog.Debug("Shield", message);
        }

        private string GetDebugModuleID()
        {
            return this.activeShield != null && !string.IsNullOrEmpty(this.activeShield.ModuleInstanceID)
                ? this.activeShield.ModuleInstanceID
                : "no-shield";
        }

        private string GetLocalizedStatus()
        {
            if (this.Charging)
            {
                return "CT_Shuttle_ShieldStatusCharging".Translate().ToString();
            }

            if (this.OnCooldown)
            {
                return "CT_Shuttle_ShieldStatusCooldown".Translate().ToString();
            }

            return this.Active
                ? "CT_Shuttle_ShieldStatusActive".Translate().ToString()
                : "CT_Shuttle_ShieldStatusOffline".Translate().ToString();
        }
    }
}
