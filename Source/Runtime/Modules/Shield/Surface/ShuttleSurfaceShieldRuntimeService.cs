using System;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Damage;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Shield.Surface
{
    /// <summary>
    /// Narrow service for the custom surface shield backend. It reads detached snapshots and
    /// mutates only the selected module's surface-shield runtime state during damage requests.
    /// It does not touch vanilla projectile-interceptor HP/cooldown or presentation effects.
    /// </summary>
    internal sealed class ShuttleSurfaceShieldRuntimeService
    {
        internal bool TryGetActiveSurfaceShieldStatus(
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            int ticksGame,
            out ShuttleSurfaceShieldStatusSnapshot snapshot)
        {
            snapshot = null;

            ShuttleModule module;
            ShuttleSurfaceShieldModuleDef moduleDef;
            if (!this.TrySelectSurfaceShieldModule(
                assemblyState,
                true,
                out module,
                out moduleDef))
            {
                return false;
            }

            ShuttleSurfaceShieldRuntimeState state = this.TryGetSurfaceState(
                runtimeState,
                module.ModuleInstanceID);
            snapshot = this.BuildStatusSnapshot(
                module,
                moduleDef,
                state,
                runtimeState,
                ticksGame);
            return snapshot != null;
        }

        internal bool TryGetActiveSurfaceShieldVisualConfig(
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            int ticksGame,
            out ShuttleSurfaceShieldVisualConfigSnapshot snapshot)
        {
            snapshot = null;

            ShuttleModule module;
            ShuttleSurfaceShieldModuleDef moduleDef;
            if (!this.TrySelectSurfaceShieldModule(
                assemblyState,
                true,
                out module,
                out moduleDef))
            {
                return false;
            }

            ShuttleSurfaceShieldRuntimeState state = this.TryGetSurfaceState(
                runtimeState,
                module.ModuleInstanceID);
            ShuttleSurfaceShieldStatusSnapshot status = this.BuildStatusSnapshot(
                module,
                moduleDef,
                state,
                runtimeState,
                ticksGame);
            if (status == null)
            {
                return false;
            }

            snapshot = new ShuttleSurfaceShieldVisualConfigSnapshot();
            snapshot.HasConfig =
                !string.IsNullOrEmpty(moduleDef.surfaceShaderBundlePath) &&
                !string.IsNullOrEmpty(moduleDef.surfaceMaterialAssetName);
            snapshot.ModuleInstanceID = module.ModuleInstanceID;
            snapshot.SurfaceShaderBundlePath = moduleDef.surfaceShaderBundlePath;
            snapshot.SurfaceMaterialAssetName = moduleDef.surfaceMaterialAssetName;
            // Static visual asset paths only. Presentation code may load them and
            // must fall back without changing shield runtime behavior.
            snapshot.SurfaceMaskTexturePath = moduleDef.surfaceMaskTexturePath;
            snapshot.SurfaceNoiseTexturePath = moduleDef.surfaceNoiseTexturePath;
            snapshot.SelectedRadius = status.SelectedRadius;
            snapshot.MinRadius = status.MinRadius;
            snapshot.MaxRadius = status.MaxRadius;
            snapshot.HitPointsPercent = status.HitPointsPercent;
            snapshot.StatusKey = status.StatusKey;
            snapshot.Online = status.Online;
            return true;
        }

        internal bool TrySetActiveSurfaceShieldRechargeSpeed(
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            float multiplier)
        {
            if (assemblyState == null || runtimeState == null)
            {
                return false;
            }

            ShuttleModule module;
            ShuttleSurfaceShieldModuleDef moduleDef;
            if (!this.TrySelectSurfaceShieldModule(
                assemblyState,
                true,
                out module,
                out moduleDef))
            {
                return false;
            }

            ShuttleSurfaceShieldRuntimeState state = this.TryGetSurfaceState(
                runtimeState,
                module.ModuleInstanceID);
            if (state == null)
            {
                return false;
            }

            // This is a player configuration value, not a live HP operation.
            // Disabled modules may still store the choice so it is ready when
            // re-enabled; missing runtime state is not created here.
            state.SetRechargeSpeedMultiplierForRuntime(multiplier);
            return true;
        }

        internal bool TryFillActiveSurfaceShieldForDev(
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            int ticksGame,
            out int maxHitPoints)
        {
            maxHitPoints = 0;
            if (assemblyState == null || runtimeState == null)
            {
                return false;
            }

            ShuttleModule module;
            ShuttleSurfaceShieldModuleDef moduleDef;
            if (!this.TrySelectSurfaceShieldModule(
                assemblyState,
                false,
                out module,
                out moduleDef))
            {
                return false;
            }

            ShuttleSurfaceShieldRuntimeState state = this.TryGetSurfaceState(
                runtimeState,
                module.ModuleInstanceID);
            if (state == null)
            {
                return false;
            }

            // Dev-only maintenance: fill the currently active surface shield without
            // creating missing runtime buckets or touching damage absorption rules.
            maxHitPoints = this.GetMaxHitPoints(moduleDef);
            state.FillToMaxForDev(maxHitPoints, ticksGame);
            return maxHitPoints > 0;
        }

        internal bool TryAbsorbIncomingDamage(
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            ref DamageInfo dinfo,
            int ticksGame,
            out ShuttleSurfaceShieldAbsorbResult result)
        {
            result = null;
            if (host == null || assemblyState == null || runtimeState == null || dinfo.Def == null)
            {
                return false;
            }

            ShuttleModule module;
            ShuttleSurfaceShieldModuleDef moduleDef;
            if (!this.TrySelectSurfaceShieldModule(
                assemblyState,
                false,
                out module,
                out moduleDef))
            {
                return false;
            }

            ShuttleSurfaceShieldRuntimeState state = this.TryGetSurfaceState(
                runtimeState,
                module.ModuleInstanceID);
            if (state == null || !module.IsEnabled || !runtimeState.Power.InternalBusPowered)
            {
                return false;
            }

            state.EnsureInitialized();
            int maxHitPoints = this.GetMaxHitPoints(moduleDef);
            state.ClampHitPoints(maxHitPoints);
            if (maxHitPoints <= 0 ||
                state.CurrentHitPoints <= 0)
            {
                return false;
            }

            float incomingDamageAmount = this.SanitizeNonNegativeFinite(dinfo.Amount);
            if (incomingDamageAmount <= 0f)
            {
                return false;
            }

            ShuttleDamageClassification classification =
                DamageClassificationUtility.Classify(dinfo);
            if (!this.CanAbsorbDamageCategory(moduleDef, classification))
            {
                return false;
            }

            float multiplier = this.GetShieldDamageMultiplier(moduleDef, classification);
            int shieldDamage = this.CalculateShieldDamage(incomingDamageAmount, multiplier);
            int availableHitPoints = state.CurrentHitPoints;
            bool brokeShield;
            string damageCategory = classification.SurfaceCategory.ToString();

            if (shieldDamage <= availableHitPoints)
            {
                // A zero shield-damage result is intentional when XML gives this category a
                // zero multiplier: the online shield fully absorbs the hit without spending HP.
                state.TryApplyShieldDamageForRuntime(
                    shieldDamage,
                    ticksGame,
                    maxHitPoints,
                    out brokeShield);

                result = this.BuildAbsorbResult(
                    true,
                    false,
                    brokeShield,
                    shieldDamage,
                    incomingDamageAmount,
                    0f,
                    host,
                    dinfo,
                    module,
                    damageCategory,
                    "FullyAbsorbed");
                return true;
            }

            state.TryApplyShieldDamageForRuntime(
                shieldDamage,
                ticksGame,
                maxHitPoints,
                out brokeShield);

            float remainingDamageAmount = this.CalculateRemainingDamage(
                incomingDamageAmount,
                shieldDamage,
                availableHitPoints);
            dinfo.SetAmount(remainingDamageAmount);
            result = this.BuildAbsorbResult(
                false,
                true,
                brokeShield,
                availableHitPoints,
                incomingDamageAmount,
                remainingDamageAmount,
                host,
                dinfo,
                module,
                damageCategory,
                "PartiallyAbsorbed");
            return true;
        }

        private bool TrySelectSurfaceShieldModule(
            ShuttleAssemblyState assemblyState,
            bool allowDisabledFallback,
            out ShuttleModule selectedModule,
            out ShuttleSurfaceShieldModuleDef selectedDef)
        {
            selectedModule = null;
            selectedDef = null;
            if (assemblyState == null)
            {
                return false;
            }

            assemblyState.EnsureInitialized();

            ShuttleModule disabledFallbackModule = null;
            ShuttleSurfaceShieldModuleDef disabledFallbackDef = null;
            int bestEnabledMaxHitPoints = -1;
            int bestDisabledMaxHitPoints = -1;
            for (int i = 0; i < assemblyState.Modules.Count; i++)
            {
                ShuttleModule module = assemblyState.Modules[i];
                ShuttleSurfaceShieldModuleDef moduleDef = module != null
                    ? module.ModuleDef as ShuttleSurfaceShieldModuleDef
                    : null;
                if (module == null || moduleDef == null)
                {
                    continue;
                }

                int maxHitPoints = this.GetMaxHitPoints(moduleDef);
                if (module.IsEnabled)
                {
                    if (selectedModule == null || maxHitPoints > bestEnabledMaxHitPoints)
                    {
                        selectedModule = module;
                        selectedDef = moduleDef;
                        bestEnabledMaxHitPoints = maxHitPoints;
                    }
                }
                else if (allowDisabledFallback &&
                    (disabledFallbackModule == null || maxHitPoints > bestDisabledMaxHitPoints))
                {
                    disabledFallbackModule = module;
                    disabledFallbackDef = moduleDef;
                    bestDisabledMaxHitPoints = maxHitPoints;
                }
            }

            // MVP selects one active surface shield by static max HP. Ties use assembly order.
            // Multiple surface-shield merging/stacking is a future extension. Vanilla
            // interceptor shields are deliberately not part of this selection.
            if (selectedModule != null)
            {
                return true;
            }

            if (allowDisabledFallback && disabledFallbackModule != null)
            {
                selectedModule = disabledFallbackModule;
                selectedDef = disabledFallbackDef;
                return true;
            }

            return false;
        }

        private ShuttleSurfaceShieldStatusSnapshot BuildStatusSnapshot(
            ShuttleModule module,
            ShuttleSurfaceShieldModuleDef moduleDef,
            ShuttleSurfaceShieldRuntimeState state,
            ShuttleRuntimeState runtimeState,
            int ticksGame)
        {
            if (module == null || moduleDef == null)
            {
                return null;
            }

            bool internalBusPowered =
                runtimeState != null && runtimeState.Power.InternalBusPowered;
            int maxHitPoints = this.GetMaxHitPoints(moduleDef);
            int currentHitPoints = state != null
                ? this.ClampInt(state.CurrentHitPoints, 0, maxHitPoints)
                : 0;
            bool broken = state != null && maxHitPoints > 0 && currentHitPoints <= 0;
            bool rechargeBlocked = false;
            ShuttleShieldRuntimeState radiusState = this.TryGetRadiusState(
                runtimeState,
                module.ModuleInstanceID);
            float selectedRadius = ShuttleShieldRuntimeUtility.ClampRadiusOrDefault(
                radiusState != null ? radiusState.SelectedRadius : ShuttleShieldRuntimeState.MissingSelectedRadius,
                moduleDef);
            selectedRadius = this.GetEffectiveShieldRadius(selectedRadius);

            ShuttleSurfaceShieldStatusSnapshot snapshot = new ShuttleSurfaceShieldStatusSnapshot();
            snapshot.ModuleInstanceID = module.ModuleInstanceID;
            snapshot.ModuleLabel = this.GetModuleLabel(module);
            snapshot.ModuleDefName = module.moduleDefName;
            snapshot.IsEnabled = module.IsEnabled;
            snapshot.InternalBusPowered = internalBusPowered;
            snapshot.Broken = broken;
            snapshot.RechargingBlocked = rechargeBlocked;
            snapshot.RechargeStalledForNoEnergy =
                state != null && state.LastRechargeStalledForNoEnergy;
            snapshot.CurrentHitPoints = currentHitPoints;
            snapshot.MaxHitPoints = maxHitPoints;
            snapshot.HitPointsPercent = this.GetHitPointsPercent(currentHitPoints, maxHitPoints);
            snapshot.LastHitTick = state != null ? state.LastHitTick : -1;
            snapshot.BrokenUntilTick = 0;
            snapshot.BrokenTicksLeft = 0;
            snapshot.RechargeBlockedUntilTick = 0;
            snapshot.RechargeBlockedTicksLeft = 0;
            snapshot.LastRechargeTick = state != null ? state.LastRechargeTick : -1;
            snapshot.RechargeHitPointsPerInterval =
                this.GetRechargeHitPointsPerInterval(moduleDef);
            snapshot.RechargeIntervalTicks = this.GetRechargeIntervalTicks(moduleDef);
            snapshot.RechargeEnergyPerHitPointWd =
                this.GetRechargeEnergyPerHitPointWd(moduleDef);
            snapshot.RechargeSpeedMultiplier = state != null
                ? state.RechargeSpeedMultiplier
                : ShuttleSurfaceShieldRuntimeState.DefaultRechargeSpeedMultiplier;
            snapshot.MinRechargeSpeedMultiplier = ShuttleSurfaceShieldRuntimeState.MinRechargeSpeedMultiplier;
            snapshot.MaxRechargeSpeedMultiplier = ShuttleSurfaceShieldRuntimeState.MaxRechargeSpeedMultiplier;
            snapshot.EffectiveRechargeHitPointsPerInterval =
                this.GetEffectiveRechargeHitPointsPerInterval(
                    snapshot.RechargeHitPointsPerInterval,
                    snapshot.RechargeSpeedMultiplier);
            snapshot.EffectiveRechargeEnergyPerIntervalWd =
                snapshot.EffectiveRechargeHitPointsPerInterval * snapshot.RechargeEnergyPerHitPointWd;
            snapshot.SelectedRadius = selectedRadius;
            snapshot.MinRadius = this.GetEffectiveShieldRadius(moduleDef.minRadius);
            snapshot.MaxRadius = this.GetEffectiveShieldRadius(moduleDef.maxRadius);
            if (snapshot.MaxRadius < snapshot.MinRadius)
            {
                snapshot.MaxRadius = snapshot.MinRadius;
            }

            snapshot.StatusKey = this.GetStatusKey(
                module,
                state,
                internalBusPowered,
                broken,
                rechargeBlocked,
                currentHitPoints,
                maxHitPoints);
            snapshot.Online = state != null &&
                module.IsEnabled &&
                internalBusPowered &&
                currentHitPoints > 0 &&
                !broken;
            snapshot.HasActiveShield = snapshot.Online && maxHitPoints > 0;
            return snapshot;
        }

        private string GetStatusKey(
            ShuttleModule module,
            ShuttleSurfaceShieldRuntimeState state,
            bool internalBusPowered,
            bool broken,
            bool rechargeBlocked,
            int currentHitPoints,
            int maxHitPoints)
        {
            if (state == null)
            {
                return "Missing";
            }

            if (module == null || !module.IsEnabled)
            {
                return "Disabled";
            }

            if (!internalBusPowered)
            {
                return "Offline";
            }

            if (broken)
            {
                return "Broken";
            }

            if (rechargeBlocked)
            {
                return "RechargeBlocked";
            }

            if (state.LastRechargeStalledForNoEnergy)
            {
                return "NoEnergy";
            }

            if (maxHitPoints > 0 && currentHitPoints >= maxHitPoints)
            {
                return "Full";
            }

            if (currentHitPoints > 0 && currentHitPoints < maxHitPoints)
            {
                return "Recharging";
            }

            return currentHitPoints > 0 ? "Online" : "Offline";
        }

        private ShuttleSurfaceShieldRuntimeState TryGetSurfaceState(
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID)
        {
            IShuttleModuleRuntimeState payload;
            if (runtimeState == null ||
                string.IsNullOrEmpty(moduleInstanceID) ||
                !runtimeState.Modules.TryGetState(
                    moduleInstanceID,
                    ShuttleSurfaceShieldRuntimeSystem.SurfaceShieldRuntimeSystemKey,
                    out payload))
            {
                return null;
            }

            return payload as ShuttleSurfaceShieldRuntimeState;
        }

        private ShuttleShieldRuntimeState TryGetRadiusState(
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID)
        {
            IShuttleModuleRuntimeState payload;
            if (runtimeState == null ||
                string.IsNullOrEmpty(moduleInstanceID) ||
                !runtimeState.Modules.TryGetState(
                    moduleInstanceID,
                    ShuttleShieldRuntimeSystem.ShieldRuntimeSystemKey,
                    out payload))
            {
                return null;
            }

            return payload as ShuttleShieldRuntimeState;
        }

        private bool CanAbsorbDamageCategory(
            ShuttleSurfaceShieldModuleDef moduleDef,
            ShuttleDamageClassification classification)
        {
            if (moduleDef == null)
            {
                return false;
            }

            if (classification.SurfaceCategory == ShuttleDamageCategory.EMP)
            {
                return moduleDef.absorbsEMP;
            }

            if (classification.SurfaceCategory == ShuttleDamageCategory.Heat)
            {
                return moduleDef.absorbsFireDamage;
            }

            if (classification.SurfaceCategory == ShuttleDamageCategory.Explosion)
            {
                return moduleDef.absorbsExplosionDamage;
            }

            if (classification.SurfaceCategory == ShuttleDamageCategory.Projectile)
            {
                return moduleDef.absorbsProjectileDamage;
            }

            if (classification.SurfaceCategory == ShuttleDamageCategory.Direct)
            {
                return moduleDef.absorbsMeleeDamage;
            }

            return false;
        }

        private float GetShieldDamageMultiplier(
            ShuttleSurfaceShieldModuleDef moduleDef,
            ShuttleDamageClassification classification)
        {
            bool isEmp = classification.SurfaceCategory == ShuttleDamageCategory.EMP;
            float multiplier = isEmp
                ? moduleDef.empShieldDamageMultiplier
                : moduleDef.damageToShieldMultiplier;
            float tuningMultiplier = isEmp
                ? CeleTechShuttleMod.EffectiveCombatTuning.ShieldEmpDamageTakenMultiplier
                : CeleTechShuttleMod.EffectiveCombatTuning.ShieldDamageTakenMultiplier;
            return CeleTechShuttleMod.EffectiveCombatTuning.ApplyFloatMultiplier(
                this.SanitizeNonNegativeFinite(multiplier),
                tuningMultiplier,
                0f,
                float.MaxValue);
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
            int baseHitPoints = moduleDef != null && moduleDef.rechargeHitPointsPerInterval > 0
                ? moduleDef.rechargeHitPointsPerInterval
                : 0;
            return CeleTechShuttleMod.EffectiveCombatTuning.ApplyIntMultiplier(
                baseHitPoints,
                CeleTechShuttleMod.EffectiveCombatTuning.ShieldRechargeRateMultiplier,
                baseHitPoints > 0 ? 1 : 0,
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

        private float GetEffectiveShieldRadius(float authoredRadius)
        {
            authoredRadius = this.SanitizeNonNegativeFinite(authoredRadius);
            return CeleTechShuttleMod.EffectiveCombatTuning.ApplyFloatMultiplier(
                authoredRadius,
                CeleTechShuttleMod.EffectiveCombatTuning.ShieldRadiusMultiplier,
                0f,
                float.MaxValue);
        }

        private int CalculateShieldDamage(float incomingDamageAmount, float multiplier)
        {
            incomingDamageAmount = this.SanitizeNonNegativeFinite(incomingDamageAmount);
            multiplier = this.SanitizeNonNegativeFinite(multiplier);
            if (incomingDamageAmount <= 0f || multiplier <= 0f)
            {
                // A zero multiplier means this shield can absorb the category without HP loss.
                // Phase 4's PreApplyDamage hook must still treat the resulting FullyAbsorbed
                // result as absorbed hull damage; this is an XML balance affordance.
                return 0;
            }

            double rawShieldDamage = Math.Ceiling((double)incomingDamageAmount * multiplier);
            if (rawShieldDamage <= 0d)
            {
                return 0;
            }

            return rawShieldDamage > int.MaxValue ? int.MaxValue : (int)rawShieldDamage;
        }

        private int GetEffectiveRechargeHitPointsPerInterval(
            int baseRechargeHitPoints,
            float multiplier)
        {
            baseRechargeHitPoints = this.SanitizeNonNegative(baseRechargeHitPoints);
            if (baseRechargeHitPoints <= 0)
            {
                return 0;
            }

            float sanitizedMultiplier = ShuttleSurfaceShieldRuntimeState.SanitizeRechargeSpeedMultiplier(multiplier);
            double effective = Math.Ceiling(baseRechargeHitPoints * (double)sanitizedMultiplier);
            if (effective <= 0d)
            {
                return 0;
            }

            return effective > int.MaxValue ? int.MaxValue : (int)effective;
        }

        private float CalculateRemainingDamage(
            float incomingDamageAmount,
            int shieldDamage,
            int availableHitPoints)
        {
            incomingDamageAmount = this.SanitizeNonNegativeFinite(incomingDamageAmount);
            if (incomingDamageAmount <= 0f || shieldDamage <= 0)
            {
                return 0f;
            }

            int overflowShieldDamage = shieldDamage - this.SanitizeNonNegative(availableHitPoints);
            if (overflowShieldDamage <= 0)
            {
                return 0f;
            }

            float remaining = incomingDamageAmount * ((float)overflowShieldDamage / shieldDamage);
            if (!this.IsFiniteFloat(remaining) || remaining < 0f)
            {
                return 0f;
            }

            return remaining > incomingDamageAmount ? incomingDamageAmount : remaining;
        }

        private ShuttleSurfaceShieldAbsorbResult BuildAbsorbResult(
            bool fullyAbsorbed,
            bool partiallyAbsorbed,
            bool brokeShield,
            int shieldDamageApplied,
            float incomingDamageAmount,
            float remainingDamageAmount,
            ThingWithComps host,
            DamageInfo dinfo,
            ShuttleModule module,
            string damageCategory,
            string statusKey)
        {
            ShuttleSurfaceShieldAbsorbResult result = new ShuttleSurfaceShieldAbsorbResult();
            result.Handled = true;
            result.FullyAbsorbed = fullyAbsorbed;
            result.PartiallyAbsorbed = partiallyAbsorbed;
            result.BrokeShield = brokeShield;
            result.ShieldDamageApplied = this.SanitizeNonNegative(shieldDamageApplied);
            result.IncomingDamageAmount = this.SanitizeNonNegativeFinite(incomingDamageAmount);
            result.RemainingDamageAmount = this.SanitizeNonNegativeFinite(remainingDamageAmount);
            result.HitWorldPosition = this.ResolveHitWorldPosition(host, dinfo);
            result.ModuleInstanceID = module != null ? module.ModuleInstanceID : null;
            result.ModuleDefName = module != null ? module.moduleDefName : null;
            result.DamageCategory = damageCategory;
            result.StatusKey = statusKey;
            return result;
        }

        private Vector3 ResolveHitWorldPosition(ThingWithComps host, DamageInfo dinfo)
        {
            if (host == null)
            {
                return Vector3.zero;
            }

            Thing instigator = dinfo.Instigator;
            if (instigator != null && instigator.Spawned && instigator.Position.IsValid)
            {
                return this.ResolveSurfaceImpactPointFromDirection(host, instigator.Position.ToVector3Shifted());
            }

            return host.DrawPos;
        }

        private Vector3 ResolveSurfaceImpactPointFromDirection(ThingWithComps host, Vector3 sourceWorldPosition)
        {
            Vector3 hostCenter = host.DrawPos;
            Vector3 fromHostToSource = sourceWorldPosition - hostCenter;
            fromHostToSource.y = 0f;

            if (fromHostToSource.sqrMagnitude < 0.0001f)
            {
                return hostCenter;
            }

            Vector3 direction = fromHostToSource.normalized;
            Vector2 visibleHullSize = this.ResolveVisibleHullWorldSize(host);
            float halfWidth = Mathf.Max(0.5f, visibleHullSize.x * 0.5f);
            float halfDepth = Mathf.Max(0.5f, visibleHullSize.y * 0.5f);

            // Project the incoming direction onto the shuttle's top-down footprint.
            // This is a sprite-space approximation: the instigator gives direction,
            // but the final visual point is clamped back to the host surface.
            float edgeDistanceX = Mathf.Abs(direction.x) > 0.0001f
                ? halfWidth / Mathf.Abs(direction.x)
                : float.PositiveInfinity;
            float edgeDistanceZ = Mathf.Abs(direction.z) > 0.0001f
                ? halfDepth / Mathf.Abs(direction.z)
                : float.PositiveInfinity;
            float edgeDistance = Mathf.Min(edgeDistanceX, edgeDistanceZ);
            if (float.IsInfinity(edgeDistance) || edgeDistance <= 0f)
            {
                edgeDistance = Mathf.Max(halfWidth, halfDepth);
            }

            const float SurfacePadding = 0.25f;
            Vector3 impactPoint = hostCenter + direction * (edgeDistance + SurfacePadding);
            impactPoint.y = hostCenter.y;
            return impactPoint;
        }

        private Vector2 ResolveVisibleHullWorldSize(ThingWithComps host)
        {
            if (host == null)
            {
                return Vector2.one;
            }

            Graphic graphic = host.Graphic;
            Vector2 drawSize = graphic != null
                ? graphic.drawSize
                : Vector2.zero;
            if (drawSize == Vector2.zero &&
                host.def != null &&
                host.def.graphicData != null)
            {
                drawSize = host.def.graphicData.drawSize;
            }

            if (drawSize == Vector2.zero && host.def != null)
            {
                drawSize = new Vector2(
                    Mathf.Max(1f, host.def.size.x),
                    Mathf.Max(1f, host.def.size.z));
            }

            if (host.Rotation.IsHorizontal &&
                (graphic == null || !graphic.ShouldDrawRotated))
            {
                drawSize = drawSize.Rotated();
            }

            return new Vector2(
                Mathf.Max(1f, drawSize.x),
                Mathf.Max(1f, drawSize.y));
        }

        private string GetModuleLabel(ShuttleModule module)
        {
            if (module == null)
            {
                return null;
            }

            if (module.ModuleDef != null && !string.IsNullOrEmpty(module.ModuleDef.label))
            {
                return module.ModuleDef.LabelCap.ToString();
            }

            return module.moduleDefName;
        }

        private int GetTicksLeft(int ticksGame, int untilTick)
        {
            if (untilTick <= ticksGame)
            {
                return 0;
            }

            return untilTick - ticksGame;
        }

        private float GetHitPointsPercent(int currentHitPoints, int maxHitPoints)
        {
            if (maxHitPoints <= 0)
            {
                return 0f;
            }

            return this.Clamp01((float)currentHitPoints / maxHitPoints);
        }

        private int SanitizeNonNegative(int value)
        {
            return value > 0 ? value : 0;
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
            if (!this.IsFiniteFloat(value) || value <= 0f)
            {
                return 0f;
            }

            return value > 1f ? 1f : value;
        }

        private float SanitizeNonNegativeFinite(float value)
        {
            if (!this.IsFiniteFloat(value) || value < 0f)
            {
                return 0f;
            }

            return value;
        }

        private bool IsFiniteFloat(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
