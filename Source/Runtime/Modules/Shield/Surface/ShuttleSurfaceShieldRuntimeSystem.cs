using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Launch;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Shield.Surface
{
    /// <summary>
    /// Owns the custom surface shield live HP and recharge lifecycle. Damage absorption and
    /// presentation pulses are intentionally left for later phases.
    /// </summary>
    internal sealed class ShuttleSurfaceShieldRuntimeSystem : ShuttleModuleRuntimeSystemBase
    {
        public static readonly ShuttleSurfaceShieldRuntimeSystem Instance =
            new ShuttleSurfaceShieldRuntimeSystem();

        internal const string SurfaceShieldRuntimeSystemKey = ShuttleRuntimeSystemKeyUtility.SurfaceShield;

        private ShuttleSurfaceShieldRuntimeSystem()
        {
        }

        public override string RuntimeSystemKey
        {
            get
            {
                return SurfaceShieldRuntimeSystemKey;
            }
        }

        public override int TickInterval
        {
            get
            {
                return 15;
            }
        }

        public override bool AppliesTo(ShuttleModule module)
        {
            return module != null && module.ModuleDef is ShuttleSurfaceShieldModuleDef;
        }

        public override bool AppliesTo(ShuttleLaunchModuleRecord moduleRecord)
        {
            return moduleRecord != null && moduleRecord.ModuleDef is ShuttleSurfaceShieldModuleDef;
        }

        public override IShuttleModuleRuntimeState CreateState()
        {
            return new ShuttleSurfaceShieldRuntimeState();
        }

        public override void Reconcile(ShuttleModuleRuntimeContext context)
        {
            this.ReconcileState(context);
        }

        public override void OnInstalled(ShuttleModuleRuntimeContext context)
        {
            this.ReconcileState(context);
        }

        public override void Tick(ShuttleModuleRuntimeContext context)
        {
            ShuttleSurfaceShieldRuntimeState state = this.GetState(context);
            ShuttleSurfaceShieldModuleDef moduleDef = this.GetModuleDef(context);
            if (context == null || state == null || moduleDef == null)
            {
                return;
            }

            if (!context.IsEnabled)
            {
                state.MarkRechargeStalledForRuntime(false);
                return;
            }

            if (!context.InternalBusPowered)
            {
                state.MarkRechargeStalledForRuntime(false);
                return;
            }

            state.EnsureInitialized();

            int maxHitPoints = this.GetMaxHitPoints(moduleDef);
            if (maxHitPoints <= 0)
            {
                state.ClampHitPoints(maxHitPoints);
                state.MarkRechargeStalledForRuntime(false);
                return;
            }

            state.ClampHitPoints(maxHitPoints);
            if (state.CurrentHitPoints >= maxHitPoints)
            {
                state.MarkRechargeStalledForRuntime(false);
                return;
            }

            int rechargeAmount = this.GetEffectiveRechargeHitPointsPerInterval(moduleDef, state);
            int rechargeIntervalTicks = this.GetRechargeIntervalTicks(moduleDef);
            if (rechargeAmount <= 0 || rechargeIntervalTicks <= 0)
            {
                state.MarkRechargeStalledForRuntime(false);
                return;
            }

            if (state.LastRechargeTick >= 0 &&
                context.TicksGame - state.LastRechargeTick < rechargeIntervalTicks)
            {
                state.MarkRechargeStalledForRuntime(false);
                return;
            }

            if (rechargeAmount > maxHitPoints - state.CurrentHitPoints)
            {
                rechargeAmount = maxHitPoints - state.CurrentHitPoints;
            }

            float rechargeCostWd = this.GetRechargeCostWd(
                rechargeAmount,
                this.GetRechargeEnergyPerHitPointWd(moduleDef));
            if (!context.TryConsumeStoredEnergyWd(rechargeCostWd))
            {
                state.MarkRechargeStalledForRuntime(true);
                return;
            }

            if (state.TryRechargeForRuntime(rechargeAmount, maxHitPoints, context.TicksGame))
            {
                state.MarkRechargeStalledForRuntime(false);
                return;
            }

            state.MarkRechargeStalledForRuntime(false);
        }

        private void ReconcileState(ShuttleModuleRuntimeContext context)
        {
            ShuttleSurfaceShieldRuntimeState state = this.GetState(context);
            ShuttleSurfaceShieldModuleDef moduleDef = this.GetModuleDef(context);
            if (state == null || moduleDef == null)
            {
                return;
            }

            state.EnsureInitialized();
            int maxHitPoints = this.GetMaxHitPoints(moduleDef);
            state.InitializeHitPointsIfNeeded(maxHitPoints);
            state.ClampHitPoints(maxHitPoints);
        }

        private ShuttleSurfaceShieldRuntimeState GetState(ShuttleModuleRuntimeContext context)
        {
            return context != null ? context.State as ShuttleSurfaceShieldRuntimeState : null;
        }

        private ShuttleSurfaceShieldModuleDef GetModuleDef(ShuttleModuleRuntimeContext context)
        {
            return context != null ? context.ModuleDef as ShuttleSurfaceShieldModuleDef : null;
        }

        private int GetMaxHitPoints(ShuttleSurfaceShieldModuleDef moduleDef)
        {
            int baseHitPoints = moduleDef != null && moduleDef.maxHitPoints > 0
                ? moduleDef.maxHitPoints
                : 0;
            return CeleTechShuttleMod.EffectiveCombatTuning.ApplyIntMultiplier(
                baseHitPoints,
                CeleTechShuttleMod.EffectiveCombatTuning.ShieldHitPointsMultiplier,
                baseHitPoints > 0 ? 1 : 0,
                int.MaxValue);
        }

        private int GetRechargeHitPointsPerInterval(ShuttleSurfaceShieldModuleDef moduleDef)
        {
            return moduleDef != null && moduleDef.rechargeHitPointsPerInterval > 0
                ? moduleDef.rechargeHitPointsPerInterval
                : 0;
        }

        private int GetEffectiveRechargeHitPointsPerInterval(
            ShuttleSurfaceShieldModuleDef moduleDef,
            ShuttleSurfaceShieldRuntimeState state)
        {
            int baseRecharge = this.GetRechargeHitPointsPerInterval(moduleDef);
            if (baseRecharge <= 0 || state == null)
            {
                return 0;
            }

            float multiplier = ShuttleSurfaceShieldRuntimeState.SanitizeRechargeSpeedMultiplier(
                state.RechargeSpeedMultiplier);
            double effective = System.Math.Ceiling(baseRecharge * (double)multiplier);
            if (effective <= 0d)
            {
                return 0;
            }

            int recharge = effective > int.MaxValue ? int.MaxValue : (int)effective;
            return CeleTechShuttleMod.EffectiveCombatTuning.ApplyIntMultiplier(
                recharge,
                CeleTechShuttleMod.EffectiveCombatTuning.ShieldRechargeRateMultiplier,
                recharge > 0 ? 1 : 0,
                int.MaxValue);
        }

        private int GetRechargeIntervalTicks(ShuttleSurfaceShieldModuleDef moduleDef)
        {
            int baseTicks = moduleDef != null && moduleDef.rechargeIntervalTicks > 0
                ? moduleDef.rechargeIntervalTicks
                : 0;
            return CeleTechShuttleMod.EffectiveCombatTuning.ApplyTicksMultiplier(
                baseTicks,
                CeleTechShuttleMod.EffectiveCombatTuning.ShieldRechargeIntervalMultiplier,
                baseTicks > 0 ? 1 : 0,
                int.MaxValue);
        }

        private float GetRechargeEnergyPerHitPointWd(ShuttleSurfaceShieldModuleDef moduleDef)
        {
            float baseCost = moduleDef != null && moduleDef.rechargeEnergyPerHitPointWd > 0f
                ? moduleDef.rechargeEnergyPerHitPointWd
                : 0f;
            return CeleTechShuttleMod.EffectiveCombatTuning.ApplyFloatMultiplier(
                baseCost,
                CeleTechShuttleMod.EffectiveCombatTuning.ShieldRechargeEnergyCostMultiplier,
                0f,
                float.MaxValue);
        }

        private float GetRechargeCostWd(int rechargeAmount, float rechargeEnergyPerHitPointWd)
        {
            if (rechargeAmount <= 0 ||
                !this.IsFiniteFloat(rechargeEnergyPerHitPointWd) ||
                rechargeEnergyPerHitPointWd <= 0f)
            {
                return 0f;
            }

            float cost = rechargeAmount * rechargeEnergyPerHitPointWd;
            return this.IsFiniteFloat(cost) && cost > 0f ? cost : 0f;
        }

        private bool IsFiniteFloat(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
