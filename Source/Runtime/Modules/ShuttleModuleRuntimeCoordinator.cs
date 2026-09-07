using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Launch;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.AutoWorkTable;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules
{
    /// <summary>
    /// Facade for built-in module runtime orchestration.
    /// Concrete lifecycle, power, launch, and weapon-command work lives in focused services.
    /// </summary>
    internal sealed class ShuttleModuleRuntimeCoordinator
    {
        private readonly BuiltInShuttleModuleRuntimeRegistry registry;
        private readonly ShuttleRuntimeTickDispatcher tickDispatcher;
        private readonly ShuttleRuntimePowerDemandService powerDemandService;
        private readonly ShuttleRuntimeMassContributionService massContributionService;
        private readonly ShuttleModuleRemovalRefundService removalRefundService;
        private readonly ShuttleRuntimeLaunchCallbackDispatcher launchCallbackDispatcher;
        private readonly AutoWorkTableRuntimeCommandService autoWorkTableCommandService;
        private readonly ShuttleRuntimeWeaponCommandService weaponCommandService;

        public ShuttleModuleRuntimeCoordinator(BuiltInShuttleModuleRuntimeRegistry registry)
        {
            this.registry = registry ?? BuiltInShuttleModuleRuntimeRegistry.CreateDefault();
            ShuttleRuntimeDispatchSupport support = new ShuttleRuntimeDispatchSupport();
            this.tickDispatcher = new ShuttleRuntimeTickDispatcher(this.registry, support);
            this.powerDemandService = new ShuttleRuntimePowerDemandService(this.registry, support);
            this.massContributionService = new ShuttleRuntimeMassContributionService(this.registry, support);
            this.removalRefundService = new ShuttleModuleRemovalRefundService(this.registry, support);
            this.launchCallbackDispatcher = new ShuttleRuntimeLaunchCallbackDispatcher(this.registry, support);
            this.autoWorkTableCommandService = new AutoWorkTableRuntimeCommandService();
            this.weaponCommandService = new ShuttleRuntimeWeaponCommandService();
        }

        internal int DispatchFingerprint
        {
            get
            {
                return this.registry != null ? this.registry.DispatchFingerprint : 0;
            }
        }

        public void Reconcile(
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            IShuttleStoredEnergySink storedEnergySink,
            int ticksGame,
            IShuttleCargoResourceBroker cargoResourceBroker = null)
        {
            this.tickDispatcher.Reconcile(
                host,
                assemblyState,
                profile,
                runtimeState,
                storedEnergySink,
                ticksGame,
                cargoResourceBroker);
        }

        public void Tick(
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            IShuttleStoredEnergySink storedEnergySink,
            int ticksGame,
            IShuttleCargoResourceBroker cargoResourceBroker = null,
            IShuttleCargoColdTransferService refrigeratedCargoTransferService = null,
            Func<string, ShuttleRefrigeratedCargoAutoTransferConfig> refrigeratedAutoTransferConfigResolver = null,
            IShuttleCargoPostDepositRouter postDepositCargoRouter = null,
            IShuttleRuntimeSystemTickProfileSink runtimeSystemTickProfileSink = null)
        {
            this.tickDispatcher.Tick(
                host,
                assemblyState,
                profile,
                runtimeState,
                storedEnergySink,
                ticksGame,
                cargoResourceBroker,
                refrigeratedCargoTransferService,
                refrigeratedAutoTransferConfigResolver,
                postDepositCargoRouter,
                runtimeSystemTickProfileSink);
        }

        public void CollectPowerDemand(
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            IShuttlePowerDemandSink powerDemandSink,
            int ticksGame,
            IShuttlePowerDemandProfileSink powerDemandProfileSink = null)
        {
            this.powerDemandService.CollectPowerDemand(
                host,
                assemblyState,
                profile,
                runtimeState,
                powerDemandSink,
                ticksGame,
                powerDemandProfileSink);
        }

        public ShuttleRuntimeMassContributionSnapshot CollectMassContributions(
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            int ticksGame)
        {
            return this.massContributionService.CollectMassContributions(
                host,
                assemblyState,
                profile,
                runtimeState,
                ticksGame);
        }

        public bool TrySetWeaponForcedTarget(
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            LocalTargetInfo target,
            out string failReason)
        {
            return this.weaponCommandService.TrySetWeaponForcedTarget(
                host,
                assemblyState,
                profile,
                runtimeState,
                moduleInstanceID,
                target,
                out failReason);
        }

        public bool TryClearWeaponForcedTarget(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            out string failReason)
        {
            return this.weaponCommandService.TryClearWeaponForcedTarget(
                assemblyState,
                runtimeState,
                moduleInstanceID,
                out failReason);
        }

        public bool TrySetWeaponHoldFire(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            bool holdFire,
            out string failReason)
        {
            return this.weaponCommandService.TrySetWeaponHoldFire(
                assemblyState,
                runtimeState,
                moduleInstanceID,
                holdFire,
                out failReason);
        }

        public bool TrySetWeaponFireControlLinked(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            bool linked,
            out string failReason)
        {
            return this.weaponCommandService.TrySetFireControlLinked(
                assemblyState,
                runtimeState,
                moduleInstanceID,
                linked,
                out failReason);
        }

        public bool TrySetWeaponFireControlMode(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            ShuttleWeaponFireControlMode mode,
            out string failReason)
        {
            return this.weaponCommandService.TrySetFireControlMode(
                assemblyState,
                runtimeState,
                moduleInstanceID,
                mode,
                out failReason);
        }

        public bool TrySetWeaponTargetPriority(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            ShuttleWeaponTargetPriority priority,
            out string failReason)
        {
            return this.weaponCommandService.TrySetTargetPriority(
                assemblyState,
                runtimeState,
                moduleInstanceID,
                priority,
                out failReason);
        }

        public bool TrySetWeaponAutoFireEnabled(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            bool enabled,
            out string failReason)
        {
            return this.weaponCommandService.TrySetAutoFireEnabled(
                assemblyState,
                runtimeState,
                moduleInstanceID,
                enabled,
                out failReason);
        }

        public bool TrySetWeaponAmmo(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            string ammoDefName,
            out string failReason)
        {
            return this.weaponCommandService.TrySetWeaponAmmo(
                assemblyState,
                runtimeState,
                moduleInstanceID,
                ammoDefName,
                out failReason);
        }

        public bool TryStartWeaponReload(
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            IShuttleCargoBackend cargoBackend,
            IShuttleStoredEnergySink storedEnergySink,
            string moduleInstanceID,
            out string failReason)
        {
            return this.weaponCommandService.TryStartWeaponReload(
                host,
                assemblyState,
                profile,
                runtimeState,
                cargoBackend,
                storedEnergySink,
                moduleInstanceID,
                out failReason);
        }

        public bool TryCancelWeaponReload(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            out string failReason)
        {
            return this.weaponCommandService.TryCancelWeaponReload(
                assemblyState,
                runtimeState,
                moduleInstanceID,
                out failReason);
        }

        public bool TrySetWeaponAutoReload(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            bool enabled,
            out string failReason)
        {
            return this.weaponCommandService.TrySetWeaponAutoReload(
                assemblyState,
                runtimeState,
                moduleInstanceID,
                enabled,
                out failReason);
        }

        public bool TrySetWeaponManualReloadAllowed(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            bool enabled,
            out string failReason)
        {
            return this.weaponCommandService.TrySetWeaponManualReloadAllowed(
                assemblyState,
                runtimeState,
                moduleInstanceID,
                enabled,
                out failReason);
        }

        public bool TrySetWeaponLogisticsAutoFeed(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            bool enabled,
            out string failReason)
        {
            return this.weaponCommandService.TrySetWeaponLogisticsAutoFeed(
                assemblyState,
                runtimeState,
                moduleInstanceID,
                enabled,
                out failReason);
        }

        public bool CanManualWeaponReloadJob(
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            IShuttleCargoBackend cargoBackend,
            IShuttleStoredEnergySink storedEnergySink,
            out ShuttleWeaponManualReloadPlan plan,
            out string failReason)
        {
            return this.weaponCommandService.CanManualReloadJob(
                host,
                assemblyState,
                profile,
                runtimeState,
                cargoBackend,
                storedEnergySink,
                out plan,
                out failReason);
        }

        public bool TryClaimManualWeaponReloadJob(
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            IShuttleCargoBackend cargoBackend,
            IShuttleStoredEnergySink storedEnergySink,
            Pawn pawn,
            int jobLoadID,
            ThingDef carriedAmmoThingDef,
            out ShuttleWeaponManualReloadPlan plan,
            out string failReason)
        {
            return this.weaponCommandService.TryClaimManualReloadJob(
                host,
                assemblyState,
                profile,
                runtimeState,
                cargoBackend,
                storedEnergySink,
                pawn,
                jobLoadID,
                carriedAmmoThingDef,
                out plan,
                out failReason);
        }

        public bool TryCompleteManualWeaponReloadJob(
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            IShuttleCargoBackend cargoBackend,
            IShuttleStoredEnergySink storedEnergySink,
            string moduleInstanceID,
            Pawn pawn,
            int jobLoadID,
            out string failReason)
        {
            return this.weaponCommandService.TryCompleteManualReloadJob(
                host,
                assemblyState,
                profile,
                runtimeState,
                cargoBackend,
                storedEnergySink,
                moduleInstanceID,
                pawn,
                jobLoadID,
                out failReason);
        }

        public bool TryReleaseManualWeaponReloadJob(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            Pawn pawn,
            int jobLoadID)
        {
            return this.weaponCommandService.TryReleaseManualReloadJob(
                assemblyState,
                runtimeState,
                moduleInstanceID,
                pawn,
                jobLoadID);
        }

        public bool TrySetAutoWorkTableSelectedRecipe(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            string sourceBenchDefName,
            string recipeDefName,
            out string failReason)
        {
            return this.autoWorkTableCommandService.TrySetSelectedRecipe(
                assemblyState,
                runtimeState,
                moduleInstanceID,
                sourceBenchDefName,
                recipeDefName,
                out failReason);
        }

        public bool TryClearAutoWorkTableSelectedRecipe(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            out string failReason)
        {
            return this.autoWorkTableCommandService.TryClearSelectedRecipe(
                assemblyState,
                runtimeState,
                moduleInstanceID,
                out failReason);
        }

        public bool TrySetAutoWorkTableProductionPolicy(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            AutoWorkTableProductionMode mode,
            int repeatCount,
            int targetCount,
            out string failReason)
        {
            return this.autoWorkTableCommandService.TrySetProductionPolicy(
                assemblyState,
                runtimeState,
                moduleInstanceID,
                mode,
                repeatCount,
                targetCount,
                out failReason);
        }

        public bool TrySetAutoWorkTablePaused(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            bool paused,
            out string failReason)
        {
            return this.autoWorkTableCommandService.TrySetPaused(
                assemblyState,
                runtimeState,
                moduleInstanceID,
                paused,
                out failReason);
        }

        public bool TryPauseAutoWorkTablesForLaunch(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            out string failReason)
        {
            return this.autoWorkTableCommandService.TryPauseForLaunch(
                assemblyState,
                runtimeState,
                out failReason);
        }

        public bool TryAddAutoWorkTableProductionOrder(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            string sourceBenchDefName,
            string recipeDefName,
            out string failReason)
        {
            return this.autoWorkTableCommandService.TryAddProductionOrder(
                assemblyState, runtimeState, moduleInstanceID,
                sourceBenchDefName, recipeDefName, out failReason);
        }

        public bool TryRemoveAutoWorkTableProductionOrder(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            int orderId,
            out string failReason)
        {
            return this.autoWorkTableCommandService.TryRemoveProductionOrder(
                assemblyState, runtimeState, moduleInstanceID, orderId, out failReason);
        }

        public bool TryMoveAutoWorkTableProductionOrder(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            int orderId,
            int direction,
            out string failReason)
        {
            return this.autoWorkTableCommandService.TryMoveProductionOrder(
                assemblyState, runtimeState, moduleInstanceID, orderId, direction, out failReason);
        }

        public bool TrySetAutoWorkTableProductionOrderSuspended(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            int orderId,
            bool suspended,
            out string failReason)
        {
            return this.autoWorkTableCommandService.TrySetProductionOrderSuspended(
                assemblyState, runtimeState, moduleInstanceID, orderId, suspended, out failReason);
        }

        public bool TrySetAutoWorkTableProductionOrderPolicy(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            int orderId,
            AutoWorkTableProductionMode mode,
            int repeatCount,
            int targetCount,
            out string failReason)
        {
            return this.autoWorkTableCommandService.TrySetProductionOrderPolicy(
                assemblyState, runtimeState, moduleInstanceID, orderId,
                mode, repeatCount, targetCount, out failReason);
        }

        public bool TrySetAutoWorkTableProductionOrderIngredientFilter(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            string moduleInstanceID,
            int orderId,
            ThingFilter ingredientFilter,
            bool useRecipeDefault,
            out string failReason)
        {
            return this.autoWorkTableCommandService.TrySetProductionOrderIngredientFilter(
                assemblyState, runtimeState, moduleInstanceID, orderId,
                ingredientFilter, useRecipeDefault, out failReason);
        }

        public bool CanRemoveModule(
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            ShuttleModule module,
            IShuttleStoredEnergySink storedEnergySink,
            int ticksGame,
            out string reason)
        {
            return this.tickDispatcher.CanRemoveModule(
                host,
                assemblyState,
                profile,
                runtimeState,
                module,
                storedEnergySink,
                ticksGame,
                out reason);
        }

        public bool TryCollectModuleRemovalRefunds(
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            ShuttleModule module,
            IShuttleStoredEnergySink storedEnergySink,
            int ticksGame,
            out List<ThingDefCountClass> refunds,
            out string failureReason)
        {
            return this.removalRefundService.TryCollect(
                host,
                assemblyState,
                profile,
                runtimeState,
                module,
                storedEnergySink,
                ticksGame,
                out refunds,
                out failureReason);
        }

        public void NotifyModuleInstalled(
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            ShuttleModule module,
            IShuttleStoredEnergySink storedEnergySink,
            int ticksGame)
        {
            this.tickDispatcher.NotifyModuleInstalled(
                host,
                assemblyState,
                profile,
                runtimeState,
                module,
                storedEnergySink,
                ticksGame);
        }

        public void NotifyModuleRemoved(
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            ShuttleModule module,
            IShuttleStoredEnergySink storedEnergySink,
            int ticksGame)
        {
            this.tickDispatcher.NotifyModuleRemoved(
                host,
                assemblyState,
                profile,
                runtimeState,
                module,
                storedEnergySink,
                ticksGame);
        }

        public bool PreLaunchValidate(
            ShuttleLaunchAssemblySnapshot assemblySnapshot,
            ShuttleModuleRuntimeStateBucket moduleStates,
            out string reason)
        {
            return this.launchCallbackDispatcher.PreLaunchValidate(
                assemblySnapshot,
                moduleStates,
                out reason);
        }

        public bool PreLaunchValidateWithoutExternalRuntimeSystems(
            ShuttleLaunchAssemblySnapshot assemblySnapshot,
            ShuttleModuleRuntimeStateBucket moduleStates,
            out string reason)
        {
            return this.launchCallbackDispatcher.PreLaunchValidate(
                assemblySnapshot,
                moduleStates,
                false,
                out reason);
        }

        public void NotifyLaunchSucceeded(
            ShuttleLaunchAssemblySnapshot assemblySnapshot,
            ShuttleModuleRuntimeStateBucket moduleStates)
        {
            this.launchCallbackDispatcher.NotifyLaunchSucceeded(
                assemblySnapshot,
                moduleStates);
        }

        public void NotifyArrived(
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            IShuttleStoredEnergySink storedEnergySink,
            int ticksGame,
            IShuttleCargoResourceBroker cargoResourceBroker = null)
        {
            this.tickDispatcher.NotifyArrived(
                host,
                assemblyState,
                profile,
                runtimeState,
                storedEnergySink,
                ticksGame,
                cargoResourceBroker);
        }
    }
}
