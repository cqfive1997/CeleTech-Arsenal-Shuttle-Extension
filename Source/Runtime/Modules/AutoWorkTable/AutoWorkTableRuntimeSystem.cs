using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Launch;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.AutoWorkTable
{
    /// <summary>
    /// Shuttle-owned selected-recipe production runtime. It borrows recipe definitions from
    /// configured source bench AllRecipes and gates repeat behavior through its own policy state.
    /// </summary>
    internal sealed class AutoWorkTableRuntimeSystem : ShuttleModuleRuntimeSystemBase
    {
        public static readonly AutoWorkTableRuntimeSystem Instance = new AutoWorkTableRuntimeSystem();
        internal const string AutoWorkTableRuntimeSystemKey = ShuttleRuntimeSystemKeyUtility.AutoWorkTable;

        private readonly AutoWorkTableRecipeCatalog catalog = new AutoWorkTableRecipeCatalog();
        private readonly AutoWorkTableIngredientResolver ingredientResolver =
            new AutoWorkTableIngredientResolver();
        private readonly AutoWorkTableProductFactory productFactory =
            new AutoWorkTableProductFactory();
        private readonly AutoWorkTableProductionPolicyEvaluator productionPolicyEvaluator =
            new AutoWorkTableProductionPolicyEvaluator();
        private readonly AutoWorkTableIngredientStager ingredientStager;
        private readonly AutoWorkTableProductDepositor productDepositor =
            new AutoWorkTableProductDepositor();
        private readonly AutoWorkTableOrderScheduler orderScheduler;
        private readonly AutoWorkTableCycleRunner cycleRunner;

        private AutoWorkTableRuntimeSystem()
        {
            this.ingredientStager = new AutoWorkTableIngredientStager(this.ingredientResolver);
            this.orderScheduler = new AutoWorkTableOrderScheduler(
                this.catalog,
                this.ingredientResolver,
                this.productionPolicyEvaluator);
            this.cycleRunner = new AutoWorkTableCycleRunner(
                this.catalog,
                this.ingredientResolver,
                this.productFactory,
                this.ingredientStager,
                this.productDepositor,
                this.orderScheduler);
        }

        public override string RuntimeSystemKey
        {
            get
            {
                return AutoWorkTableRuntimeSystemKey;
            }
        }

        public override int TickInterval
        {
            get
            {
                return 60;
            }
        }

        public override bool ParticipatesInPowerDemand
        {
            get
            {
                return true;
            }
        }

        public override bool AppliesTo(ShuttleModule module)
        {
            return module != null && module.ModuleDef is ShuttleAutoWorkTableModuleDef;
        }

        public override bool AppliesTo(ShuttleLaunchModuleRecord moduleRecord)
        {
            return moduleRecord != null && moduleRecord.ModuleDef is ShuttleAutoWorkTableModuleDef;
        }

        public override IShuttleModuleRuntimeState CreateState()
        {
            return new AutoWorkTableRuntimeState();
        }

        public override void Reconcile(ShuttleModuleRuntimeContext context)
        {
            ShuttleAutoWorkTableModuleDef moduleDef = this.GetModuleDef(context);
            AutoWorkTableRuntimeState state = this.GetState(context);
            if (moduleDef == null || state == null)
            {
                return;
            }

            state.EnsureInitialized();
            state.SetDefaultSelectionIfEmpty(
                moduleDef.defaultSelectedSourceBenchDef != null
                    ? moduleDef.defaultSelectedSourceBenchDef.defName
                    : null,
                moduleDef.defaultSelectedRecipeDef != null
                    ? moduleDef.defaultSelectedRecipeDef.defName
                    : null);
        }

        public override void CollectPowerDemand(ShuttleModuleRuntimeContext context)
        {
            ShuttleAutoWorkTableModuleDef moduleDef = this.GetModuleDef(context);
            AutoWorkTableRuntimeState state = this.GetState(context);
            if (moduleDef == null ||
                state == null ||
                moduleDef.activePowerDrawWatts <= 0f ||
                state.IsPaused ||
                !state.HasActiveProduction)
            {
                return;
            }

            context.AddInternalPowerDemandWatts(moduleDef.activePowerDrawWatts);
        }

        public override void Tick(ShuttleModuleRuntimeContext context)
        {
            ShuttleAutoWorkTableModuleDef moduleDef = this.GetModuleDef(context);
            AutoWorkTableRuntimeState state = this.GetState(context);
            if (context == null || moduleDef == null || state == null)
            {
                return;
            }

            state.EnsureInitialized();

            this.cycleRunner.Tick(context, moduleDef, state);
        }

        public override bool CanRemove(ShuttleModuleRuntimeContext context, out string reason)
        {
            reason = null;
            AutoWorkTableRuntimeState state = this.GetState(context);
            if (state == null)
            {
                return true;
            }

            state.EnsureInitialized();
            if (state.HasActiveProduction)
            {
                reason = "AutoWorkTable has active production.";
                return false;
            }

            if (state.HasPendingProducts)
            {
                reason = "AutoWorkTable has pending products.";
                return false;
            }

            if (state.HasStagedIngredients)
            {
                reason = "AutoWorkTable has staged ingredients that must be recovered.";
                return false;
            }

            return true;
        }

        public override bool PreLaunchValidate(
            ShuttleModulePreLaunchValidationContext context,
            out string reason)
        {
            reason = null;
            if (context == null || context.StateForRead == null)
            {
                return true;
            }

            IAutoWorkTableRuntimeStateReadOnly stateView;
            if (!context.StateForRead.TryGetStateView(out stateView) || stateView == null)
            {
                reason = "AutoWorkTable runtime state is unavailable.";
                return false;
            }

            if (stateView.CompletionCommitBlocked)
            {
                reason = "AutoWorkTable completion commit is blocked.";
                return false;
            }

            if ((stateView.HasActiveProduction ||
                    stateView.HasPendingProducts ||
                    stateView.HasStagedIngredients) &&
                !stateView.IsPaused)
            {
                reason = "CT_Shuttle_AutoWorkTable_LaunchRequiresPausedProcessing".Translate().ToString();
                return false;
            }

            return true;
        }

        public override void OnInstalled(ShuttleModuleRuntimeContext context)
        {
            this.Reconcile(context);
        }

        public override void OnArrived(ShuttleModuleRuntimeContext context)
        {
            this.Reconcile(context);
        }

        internal bool TrySetSelectedRecipe(
            AutoWorkTableRuntimeState state,
            ShuttleAutoWorkTableModuleDef moduleDef,
            string sourceBenchDefName,
            string recipeDefName,
            out string failureReason)
        {
            failureReason = null;
            if (state == null)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_RuntimeStateUnavailable".Translate().ToString();
                return false;
            }

            AutoWorkTableRecipeCatalogEntry entry;
            if (!this.catalog.TryResolveEntry(
                moduleDef,
                sourceBenchDefName,
                recipeDefName,
                out entry,
                out failureReason))
            {
                return false;
            }

            return state.TrySetSelection(sourceBenchDefName, recipeDefName, out failureReason);
        }

        internal bool TryClearSelectedRecipe(
            AutoWorkTableRuntimeState state,
            out string failureReason)
        {
            if (state == null)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_RuntimeStateUnavailable".Translate().ToString();
                return false;
            }

            return state.TrySetSelection(null, null, out failureReason);
        }

        internal bool TrySetProductionPolicy(
            AutoWorkTableRuntimeState state,
            ShuttleAutoWorkTableModuleDef moduleDef,
            AutoWorkTableProductionMode mode,
            int repeatCount,
            int targetCount,
            out string failureReason)
        {
            failureReason = null;
            if (state == null)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_RuntimeStateUnavailable".Translate().ToString();
                return false;
            }

            if (string.IsNullOrEmpty(state.SelectedSourceBenchDefName) ||
                string.IsNullOrEmpty(state.SelectedRecipeDefName))
            {
                failureReason = "CT_Shuttle_AutoWorkTable_NoSelectedRecipe".Translate().ToString();
                return false;
            }

            if (mode == AutoWorkTableProductionMode.RepeatCount && repeatCount < 1)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_RepeatCountInvalid".Translate().ToString();
                return false;
            }

            if (mode == AutoWorkTableProductionMode.DoUntilStock && targetCount < 1)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_TargetCountInvalid".Translate().ToString();
                return false;
            }

            if (mode == AutoWorkTableProductionMode.DoUntilStock &&
                !this.productionPolicyEvaluator.SelectedRecipeSupportsDoUntilStock(
                    this.catalog,
                    moduleDef,
                    state,
                    out failureReason))
            {
                return false;
            }

            state.SetProductionPolicy(mode, repeatCount, targetCount);
            if (!state.HasActiveProduction && !state.HasPendingProducts && !state.HasStagedIngredients)
            {
                state.SetStatus(state.IsPaused ? AutoWorkTableStatus.Paused : AutoWorkTableStatus.Idle);
            }

            state.ScheduleRecipeCheck(Find.TickManager != null ? Find.TickManager.TicksGame : -1, 1);
            return true;
        }

        internal bool TrySetPaused(
            AutoWorkTableRuntimeState state,
            bool paused,
            out string failureReason)
        {
            failureReason = null;
            if (state == null)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_RuntimeStateUnavailable".Translate().ToString();
                return false;
            }

            if (string.IsNullOrEmpty(state.SelectedRecipeDefName) &&
                string.IsNullOrEmpty(state.ActiveRecipeDefName))
            {
                failureReason = "CT_Shuttle_AutoWorkTable_NoSelectedRecipe".Translate().ToString();
                return false;
            }

            state.SetPaused(paused, Find.TickManager != null ? Find.TickManager.TicksGame : -1);
            return true;
        }

        internal bool TryAddProductionOrder(
            AutoWorkTableRuntimeState state,
            ShuttleAutoWorkTableModuleDef moduleDef,
            string sourceBenchDefName,
            string recipeDefName,
            out string failureReason)
        {
            failureReason = null;
            AutoWorkTableRecipeCatalogEntry entry;
            if (state == null || !this.catalog.TryResolveEntry(
                moduleDef,
                sourceBenchDefName,
                recipeDefName,
                out entry,
                out failureReason))
            {
                return false;
            }

            AutoWorkTableProductionOrderState order;
            return state.TryAddOrder(
                sourceBenchDefName,
                recipeDefName,
                out order,
                out failureReason);
        }

        internal bool TryRemoveProductionOrder(
            AutoWorkTableRuntimeState state,
            int orderId,
            out string failureReason)
        {
            if (state == null)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_RuntimeStateUnavailable".Translate().ToString();
                return false;
            }

            return state.TryRemoveOrder(orderId, out failureReason);
        }

        internal bool TryMoveProductionOrder(
            AutoWorkTableRuntimeState state,
            int orderId,
            int direction,
            out string failureReason)
        {
            if (state == null)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_RuntimeStateUnavailable".Translate().ToString();
                return false;
            }

            return state.TryMoveOrder(orderId, direction, out failureReason);
        }

        internal bool TrySetProductionOrderSuspended(
            AutoWorkTableRuntimeState state,
            int orderId,
            bool suspended,
            out string failureReason)
        {
            if (state == null)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_RuntimeStateUnavailable".Translate().ToString();
                return false;
            }

            return state.TrySetOrderSuspended(orderId, suspended, out failureReason);
        }

        internal bool TrySetProductionOrderPolicy(
            AutoWorkTableRuntimeState state,
            ShuttleAutoWorkTableModuleDef moduleDef,
            int orderId,
            AutoWorkTableProductionMode mode,
            int repeatCount,
            int targetCount,
            out string failureReason)
        {
            failureReason = null;
            AutoWorkTableProductionOrderState order = state != null ? state.GetOrder(orderId) : null;
            if (order == null)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_OrderMissing".Translate().ToString();
                return false;
            }

            if (mode == AutoWorkTableProductionMode.RepeatCount && repeatCount < 1)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_RepeatCountInvalid".Translate().ToString();
                return false;
            }

            if (mode == AutoWorkTableProductionMode.DoUntilStock && targetCount < 1)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_TargetCountInvalid".Translate().ToString();
                return false;
            }

            AutoWorkTableRecipeCatalogEntry entry;
            if (!this.catalog.TryResolveEntry(
                moduleDef,
                order.SourceBenchDefName,
                order.RecipeDefName,
                out entry,
                out failureReason))
            {
                return false;
            }

            if (mode == AutoWorkTableProductionMode.DoUntilStock &&
                this.productionPolicyEvaluator.RecipeRejectsDoUntilStock(entry.RecipeDef))
            {
                failureReason = this.productionPolicyEvaluator.GetDoUntilStockUnsupportedReason(
                    entry.RecipeDef);
                return false;
            }

            bool result = state.TrySetOrderProductionPolicy(
                orderId,
                mode,
                repeatCount,
                targetCount,
                out failureReason);
            if (result)
            {
                state.ScheduleRecipeCheck(Find.TickManager != null ? Find.TickManager.TicksGame : -1, 1);
            }

            return result;
        }

        internal bool TrySetProductionOrderIngredientFilter(
            AutoWorkTableRuntimeState state,
            ShuttleAutoWorkTableModuleDef moduleDef,
            int orderId,
            ThingFilter ingredientFilter,
            bool useRecipeDefault,
            out string failureReason)
        {
            failureReason = null;
            AutoWorkTableProductionOrderState order = state != null ? state.GetOrder(orderId) : null;
            AutoWorkTableRecipeCatalogEntry entry;
            if (order == null)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_OrderMissing".Translate().ToString();
                return false;
            }

            if (!this.catalog.TryResolveEntry(
                moduleDef,
                order.SourceBenchDefName,
                order.RecipeDefName,
                out entry,
                out failureReason))
            {
                return false;
            }

            return state.TrySetOrderIngredientFilter(
                orderId,
                ingredientFilter,
                useRecipeDefault,
                out failureReason);
        }

        private ShuttleAutoWorkTableModuleDef GetModuleDef(ShuttleModuleRuntimeContext context)
        {
            return context != null ? context.ModuleDef as ShuttleAutoWorkTableModuleDef : null;
        }

        private AutoWorkTableRuntimeState GetState(ShuttleModuleRuntimeContext context)
        {
            return context != null ? context.State as AutoWorkTableRuntimeState : null;
        }
    }
}
