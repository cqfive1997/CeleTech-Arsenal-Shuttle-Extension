using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Shield.Surface
{
    /// <summary>
    /// Durable live state for the custom surface shield backend. This state belongs to one
    /// installed module instance and intentionally does not store Def tuning or visual pulses.
    /// </summary>
    internal sealed class ShuttleSurfaceShieldRuntimeState : IShuttleModuleRuntimeState
    {
        internal const float DefaultRechargeSpeedMultiplier = 1f;
        internal const float MinRechargeSpeedMultiplier = 0.25f;
        internal const float MaxRechargeSpeedMultiplier = 2f;

        private const int CurrentSaveVersion = 3;
        private const int MissingTick = -1;

        private int saveVersion = CurrentSaveVersion;
        private int currentHitPoints;
        private bool hasInitializedHitPoints;
        private int lastHitTick = MissingTick;
        private int rechargeBlockedUntilTick;
        private int brokenUntilTick;
        private int lastRechargeTick = MissingTick;
        private bool lastRechargeStalledForNoEnergy;
        private float rechargeSpeedMultiplier = DefaultRechargeSpeedMultiplier;

        public int CurrentHitPoints
        {
            get
            {
                return this.currentHitPoints;
            }
        }

        public bool HasInitializedHitPoints
        {
            get
            {
                return this.hasInitializedHitPoints;
            }
        }

        public int LastHitTick
        {
            get
            {
                return this.lastHitTick;
            }
        }

        public int RechargeBlockedUntilTick
        {
            get
            {
                return this.rechargeBlockedUntilTick;
            }
        }

        public int BrokenUntilTick
        {
            get
            {
                return this.brokenUntilTick;
            }
        }

        public int LastRechargeTick
        {
            get
            {
                return this.lastRechargeTick;
            }
        }

        public bool LastRechargeStalledForNoEnergy
        {
            get
            {
                return this.lastRechargeStalledForNoEnergy;
            }
        }

        public float RechargeSpeedMultiplier
        {
            get
            {
                return SanitizeRechargeSpeedMultiplier(this.rechargeSpeedMultiplier);
            }
        }

        public void EnsureInitialized()
        {
            this.SanitizeTicks();
            // Save versions before 3 may contain post-hit and broken downtime gates. The powered
            // regeneration policy no longer uses either timer, so reconcile them to inert values.
            this.rechargeBlockedUntilTick = 0;
            this.brokenUntilTick = 0;
            this.currentHitPoints = this.ClampInt(this.currentHitPoints, 0, int.MaxValue);
            this.rechargeSpeedMultiplier = SanitizeRechargeSpeedMultiplier(this.rechargeSpeedMultiplier);
        }

        internal void InitializeHitPointsIfNeeded(int maxHp)
        {
            maxHp = this.SanitizeMaxHitPoints(maxHp);
            if (maxHp <= 0)
            {
                this.currentHitPoints = 0;
                this.hasInitializedHitPoints = true;
                return;
            }

            if (this.hasInitializedHitPoints)
            {
                this.ClampHitPoints(maxHp);
                return;
            }

            // First installation grants full surface-shield HP without spending stored energy.
            // Normal recharge after real damage still pays through the runtime tick path.
            this.currentHitPoints = maxHp;
            this.hasInitializedHitPoints = true;
        }

        internal void ClampHitPoints(int maxHp)
        {
            maxHp = this.SanitizeMaxHitPoints(maxHp);
            this.currentHitPoints = maxHp <= 0
                ? 0
                : this.ClampInt(this.currentHitPoints, 0, maxHp);
        }

        internal void SetCurrentHitPointsForRuntime(int value, int maxHp)
        {
            maxHp = this.SanitizeMaxHitPoints(maxHp);
            this.currentHitPoints = maxHp <= 0
                ? 0
                : this.ClampInt(value, 0, maxHp);
            this.hasInitializedHitPoints = true;
        }

        internal void FillToMaxForDev(int maxHp, int ticksGame)
        {
            maxHp = this.SanitizeMaxHitPoints(maxHp);
            this.SetCurrentHitPointsForRuntime(maxHp, maxHp);
            this.rechargeBlockedUntilTick = 0;
            this.brokenUntilTick = 0;
            this.lastRechargeTick = this.SanitizeTick(ticksGame);
            this.lastRechargeStalledForNoEnergy = false;
        }

        internal void MarkHitForRuntime(int ticksGame)
        {
            this.lastHitTick = this.SanitizeTick(ticksGame);
            this.rechargeBlockedUntilTick = 0;
            this.brokenUntilTick = 0;
        }

        internal bool TryApplyShieldDamageForRuntime(
            int shieldDamage,
            int ticksGame,
            int maxHp,
            out bool brokeShield)
        {
            brokeShield = false;
            maxHp = this.SanitizeMaxHitPoints(maxHp);
            this.ClampHitPoints(maxHp);
            if (maxHp <= 0)
            {
                return false;
            }

            // Keep the last-contact diagnostic, but powered regeneration is never delayed by it.
            this.MarkHitForRuntime(ticksGame);
            if (shieldDamage <= 0)
            {
                return true;
            }

            if (this.currentHitPoints <= 0)
            {
                return false;
            }

            int damageToApply = shieldDamage > this.currentHitPoints
                ? this.currentHitPoints
                : shieldDamage;
            this.currentHitPoints -= damageToApply;
            if (this.currentHitPoints <= 0)
            {
                this.currentHitPoints = 0;
                brokeShield = true;
            }

            return damageToApply > 0;
        }

        internal bool TryRechargeForRuntime(int amount, int maxHp, int ticksGame)
        {
            amount = this.SanitizeNonNegative(amount);
            maxHp = this.SanitizeMaxHitPoints(maxHp);
            this.ClampHitPoints(maxHp);
            if (amount <= 0 || maxHp <= 0 || this.currentHitPoints >= maxHp)
            {
                return false;
            }

            this.currentHitPoints = this.ClampInt(
                this.SaturatingAdd(this.currentHitPoints, amount),
                0,
                maxHp);
            this.lastRechargeTick = this.SanitizeTick(ticksGame);
            return true;
        }

        internal void MarkRechargeStalledForRuntime(bool stalled)
        {
            this.lastRechargeStalledForNoEnergy = stalled;
        }

        internal void SetRechargeSpeedMultiplierForRuntime(float value)
        {
            this.rechargeSpeedMultiplier = SanitizeRechargeSpeedMultiplier(value);
        }

        internal static float SanitizeRechargeSpeedMultiplier(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return DefaultRechargeSpeedMultiplier;
            }

            if (value <= 0f)
            {
                return MinRechargeSpeedMultiplier;
            }

            if (value < MinRechargeSpeedMultiplier)
            {
                return MinRechargeSpeedMultiplier;
            }

            if (value > MaxRechargeSpeedMultiplier)
            {
                return MaxRechargeSpeedMultiplier;
            }

            return value;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.saveVersion, "saveVersion", 0);
            Scribe_Values.Look(ref this.currentHitPoints, "currentHitPoints", 0);
            Scribe_Values.Look(ref this.hasInitializedHitPoints, "hasInitializedHitPoints", false);
            Scribe_Values.Look(ref this.lastHitTick, "lastHitTick", MissingTick);
            Scribe_Values.Look(ref this.rechargeBlockedUntilTick, "rechargeBlockedUntilTick", 0);
            Scribe_Values.Look(ref this.brokenUntilTick, "brokenUntilTick", 0);
            Scribe_Values.Look(ref this.lastRechargeTick, "lastRechargeTick", MissingTick);
            Scribe_Values.Look(
                ref this.lastRechargeStalledForNoEnergy,
                "lastRechargeStalledForNoEnergy",
                false);
            Scribe_Values.Look(
                ref this.rechargeSpeedMultiplier,
                "rechargeSpeedMultiplier",
                DefaultRechargeSpeedMultiplier);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                int loadedVersion = this.saveVersion;
                this.EnsureInitialized();
                this.MigratePostLoad(loadedVersion);
                this.saveVersion = CurrentSaveVersion;
            }
        }

        private void MigratePostLoad(int loadedVersion)
        {
            if (loadedVersion > CurrentSaveVersion)
            {
                Log.Warning("[CeleTech Shuttle] Loaded ShuttleSurfaceShieldRuntimeState save version " +
                    loadedVersion + " newer than supported version " + CurrentSaveVersion +
                    ". Keeping loaded runtime state intact.");
            }
        }

        private void SanitizeTicks()
        {
            this.lastHitTick = this.SanitizeOptionalTick(this.lastHitTick);
            this.lastRechargeTick = this.SanitizeOptionalTick(this.lastRechargeTick);
            this.rechargeBlockedUntilTick = this.SanitizeNonNegative(this.rechargeBlockedUntilTick);
            this.brokenUntilTick = this.SanitizeNonNegative(this.brokenUntilTick);
        }

        private int SanitizeOptionalTick(int value)
        {
            return value < 0 ? MissingTick : value;
        }

        private int SanitizeTick(int value)
        {
            return value < 0 ? 0 : value;
        }

        private int SanitizeMaxHitPoints(int value)
        {
            return value > 0 ? value : 0;
        }

        private int SanitizeNonNegative(int value)
        {
            return value > 0 ? value : 0;
        }

        private int SaturatingAdd(int left, int right)
        {
            if (right <= 0)
            {
                return left < 0 ? 0 : left;
            }

            if (left < 0)
            {
                left = 0;
            }

            if (int.MaxValue - left < right)
            {
                return int.MaxValue;
            }

            return left + right;
        }

        private int ClampInt(int value, int min, int max)
        {
            if (max < min)
            {
                max = min;
            }

            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }

        private float Clamp01(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }

            if (value > 1f)
            {
                return 1f;
            }

            return value;
        }

        private float SanitizeFinite(float value, float fallback)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value)
                ? value
                : fallback;
        }
    }
}
