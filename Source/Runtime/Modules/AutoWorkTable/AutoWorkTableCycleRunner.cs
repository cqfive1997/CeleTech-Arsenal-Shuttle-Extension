using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.AutoWorkTable
{
    internal sealed class AutoWorkTableCycleRunner
    {
        private readonly AutoWorkTableRecipeCatalog catalog;
        private readonly AutoWorkTableIngredientResolver ingredientResolver;
        private readonly AutoWorkTableProductFactory productFactory;
        private readonly AutoWorkTableIngredientStager ingredientStager;
        private readonly AutoWorkTableProductDepositor productDepositor;
        private readonly AutoWorkTableOrderScheduler orderScheduler;

        internal AutoWorkTableCycleRunner(
            AutoWorkTableRecipeCatalog catalog,
            AutoWorkTableIngredientResolver ingredientResolver,
            AutoWorkTableProductFactory productFactory,
            AutoWorkTableIngredientStager ingredientStager,
            AutoWorkTableProductDepositor productDepositor,
            AutoWorkTableOrderScheduler orderScheduler)
        {
            this.catalog = catalog;
            this.ingredientResolver = ingredientResolver;
            this.productFactory = productFactory;
            this.ingredientStager = ingredientStager;
            this.productDepositor = productDepositor;
            this.orderScheduler = orderScheduler;
        }

        internal void Tick(
            ShuttleModuleRuntimeContext context,
            ShuttleAutoWorkTableModuleDef moduleDef,
            AutoWorkTableRuntimeState state)
        {
            bool profilingEnabled = AutoWorkTableRuntimeProfiler.Enabled;
            long startedAt = AutoWorkTableRuntimeProfiler.Start(profilingEnabled);
            try
            {
                if (context.CargoResourceBroker != null &&
                    state.ObserveCargoRevision(context.CargoResourceBroker.InventoryRevision))
                {
                    AutoWorkTableRuntimeProfiler.Count(
                        profilingEnabled,
                        AutoWorkTableProfileCounter.CargoRevisionWakes);
                }

                this.TickCore(context, moduleDef, state, profilingEnabled);
            }
            finally
            {
                AutoWorkTableRuntimeProfiler.Record(
                    profilingEnabled,
                    AutoWorkTableProfileSection.RuntimeTick,
                    startedAt);
                AutoWorkTableRuntimeProfiler.MaybeLog(context.TicksGame);
            }
        }

        private void TickCore(
            ShuttleModuleRuntimeContext context,
            ShuttleAutoWorkTableModuleDef moduleDef,
            AutoWorkTableRuntimeState state,
            bool profilingEnabled)
        {
            if (state.CompletionCommitBlocked)
            {
                // Completion commit is a fail-closed state: products and ingredients both exist
                // while the active job is still durable, so depositing products would duplicate output.
                state.SetStatus(
                    AutoWorkTableStatus.CompletionCommitBlocked,
                    state.LastFailureReason ??
                        "AutoWorkTable completion commit is blocked: products and staged ingredients both remain while active production is still open.");
                return;
            }

            if (this.TryAbortInvalidAnimalButcheryCycle(state))
            {
                if (state.HasStagedIngredients)
                {
                    this.TryRecoverStagedIngredientsToCargo(context, moduleDef, state);
                }

                return;
            }

            if (state.HasPendingProducts)
            {
                // Pending products are already durable module-owned Things. Deposit retries are safe,
                // but no new work may start until this owner is empty.
                state.SetStatus(AutoWorkTableStatus.OutputBlocked, state.LastFailureReason);
                this.TryDepositPendingProducts(context, moduleDef, state);
                return;
            }

            if (state.HasStagedIngredients && !state.HasActiveProduction)
            {
                // Staged ingredients without an active job are recovery truth, not consumable cargo.
                // Try to put them back through the broker before normal ticking resumes.
                this.TryRecoverStagedIngredientsToCargo(context, moduleDef, state);
                return;
            }

            if (!context.IsEnabled)
            {
                state.SetStatus(AutoWorkTableStatus.Idle, "AutoWorkTable module is disabled.");
                return;
            }

            if (state.IsPaused)
            {
                state.SetPaused(true, context.TicksGame);
                return;
            }

            if (state.HasActiveProduction)
            {
                AutoWorkTableProductionOrderState activeOrder = state.GetActiveOrder();
                if (activeOrder != null && activeOrder.Suspended)
                {
                    state.SetLastWorkTick(context.TicksGame);
                    state.SetAggregateStatus(AutoWorkTableStatus.Paused);
                    return;
                }

                state.SetAggregateStatus(AutoWorkTableStatus.Working);
                this.TickActiveProduction(context, moduleDef, state);
                return;
            }

            if (!this.ShouldCheckRecipe(state, context.TicksGame))
            {
                return;
            }

            this.TryStartNextProductionCycle(context, moduleDef, state, profilingEnabled);
        }

        private bool TryAbortInvalidAnimalButcheryCycle(AutoWorkTableRuntimeState state)
        {
            if (state == null ||
                !state.HasActiveProduction ||
                state.ActiveRecipeDefName != AutoWorkTableAnimalButcheryUtility.RecipeDefName)
            {
                return false;
            }

            if (AutoWorkTableAnimalButcheryUtility.FindAnimalCorpse(state.StagedIngredients) != null)
            {
                return false;
            }

            return state.AbortActiveProductionAndSuspendOrder(
                "CT_Shuttle_AutoWorkTable_AnimalButcheryCorpseInvalidated".Translate().ToString());
        }

        private void TryStartNextProductionCycle(
            ShuttleModuleRuntimeContext context,
            ShuttleAutoWorkTableModuleDef moduleDef,
            AutoWorkTableRuntimeState state,
            bool profilingEnabled)
        {
            if (state.ProductionOrderCount == 0 &&
                moduleDef.allowAutoPickFirstCraftableRecipe)
            {
                CargoAvailabilitySnapshot availabilitySnapshot =
                    this.GetCargoAvailabilitySnapshot(context);
                this.TryAutoPickFirstCraftableRecipe(
                    context,
                    moduleDef,
                    state,
                    availabilitySnapshot);
            }

            if (state.ProductionOrderCount == 0)
            {
                state.ClearIngredientAvailability();
                state.SetStatus(AutoWorkTableStatus.WaitingForSelection);
                state.ScheduleRecipeCheck(context.TicksGame, moduleDef.recipeCheckIntervalTicks);
                return;
            }

            string failureReason;
            if (!this.TryValidateCargoAccess(
                context,
                moduleDef,
                out failureReason))
            {
                state.ClearIngredientAvailability();
                state.SetStatus(this.StatusForValidationFailure(failureReason), failureReason);
                state.ScheduleRecipeCheck(context.TicksGame, moduleDef.recipeCheckIntervalTicks);
                return;
            }

            long schedulingStarted = AutoWorkTableRuntimeProfiler.Start(profilingEnabled);
            AutoWorkTableProductionOrderState order;
            AutoWorkTableRecipeCatalogEntry entry;
            bool selected = this.orderScheduler.TrySelectOrder(
                context,
                moduleDef,
                state,
                profilingEnabled,
                out order,
                out entry,
                out failureReason);
            AutoWorkTableRuntimeProfiler.Record(
                profilingEnabled,
                AutoWorkTableProfileSection.QueueScheduling,
                schedulingStarted);
            if (!selected)
            {
                state.ScheduleRecipeCheck(context.TicksGame, moduleDef.recipeCheckIntervalTicks);
                return;
            }

            long stagingStarted = AutoWorkTableRuntimeProfiler.Start(profilingEnabled);
            AutoWorkTableRuntimeProfiler.Count(
                profilingEnabled,
                AutoWorkTableProfileCounter.StageAttempts);
            if (!this.ingredientStager.TryStageIngredients(
                context,
                entry,
                order,
                state,
                out failureReason))
            {
                AutoWorkTableRuntimeProfiler.Record(
                    profilingEnabled,
                    AutoWorkTableProfileSection.IngredientStaging,
                    stagingStarted);
                AutoWorkTableStatus status = this.StatusForValidationFailure(failureReason);
                order.SetTransientStatus(status, failureReason);
                state.SetAggregateStatus(status, failureReason);
                state.ScheduleRecipeCheck(context.TicksGame, moduleDef.recipeCheckIntervalTicks);
                return;
            }
            AutoWorkTableRuntimeProfiler.Record(
                profilingEnabled,
                AutoWorkTableProfileSection.IngredientStaging,
                stagingStarted);

            float workAmount = Math.Max(
                0.001f,
                entry.RecipeDef.WorkAmountForStuff(null) * moduleDef.workAmountFactor);
            try
            {
                // Only after staged ingredients are durable do we mark the production job active.
                // If this persistence step fails, the staged owner remains recoverable.
                state.BeginWork(
                    order.OrderId,
                    entry.SourceBenchDefName,
                    entry.RecipeDefName,
                    workAmount,
                    context.TicksGame);
            }
            catch (Exception exception)
            {
                context.CargoResourceBroker.InvalidateInventorySnapshot();
                state.SetStatus(
                    AutoWorkTableStatus.RecoveryBlocked,
                    "Failed to persist AutoWorkTable active production after staging ingredients: " +
                        exception.GetType().Name + ".");
                this.TryRecoverStagedIngredientsToCargo(context, moduleDef, state);
            }
        }

        private void TickActiveProduction(
            ShuttleModuleRuntimeContext context,
            ShuttleAutoWorkTableModuleDef moduleDef,
            AutoWorkTableRuntimeState state)
        {
            bool profilingEnabled = AutoWorkTableRuntimeProfiler.Enabled;
            long startedAt = AutoWorkTableRuntimeProfiler.Start(profilingEnabled);
            try
            {
            if (moduleDef.requirePoweredInternalBus && !context.InternalBusPowered)
            {
                state.SetLastWorkTick(context.TicksGame);
                state.SetStatus(AutoWorkTableStatus.Working, "Internal power bus is offline.");
                return;
            }

            if (state.LastWorkTick < 0 || context.TicksGame < state.LastWorkTick)
            {
                state.SetLastWorkTick(context.TicksGame);
                return;
            }

            int elapsedTicks = context.TicksGame - state.LastWorkTick;
            if (elapsedTicks < moduleDef.workTickIntervalTicks)
            {
                return;
            }

            int cappedElapsedTicks = elapsedTicks < moduleDef.maxCatchUpTicks
                ? elapsedTicks
                : moduleDef.maxCatchUpTicks;
            state.SetLastWorkTick(context.TicksGame);
            state.AdvanceWork(cappedElapsedTicks * moduleDef.workSpeedFactor);

            if (state.WorkLeft > 0f)
            {
                return;
            }

            this.CompleteActiveProduction(context, moduleDef, state);
            }
            finally
            {
                AutoWorkTableRuntimeProfiler.Record(
                    profilingEnabled,
                    AutoWorkTableProfileSection.ActiveWork,
                    startedAt);
            }
        }

        private void CompleteActiveProduction(
            ShuttleModuleRuntimeContext context,
            ShuttleAutoWorkTableModuleDef moduleDef,
            AutoWorkTableRuntimeState state)
        {
            bool profilingEnabled = AutoWorkTableRuntimeProfiler.Enabled;
            long startedAt = AutoWorkTableRuntimeProfiler.Start(profilingEnabled);
            AutoWorkTableRuntimeProfiler.Count(
                profilingEnabled,
                AutoWorkTableProfileCounter.CompletionAttempts);
            try
            {
            AutoWorkTableRecipeCatalogEntry entry;
            string failureReason;
            if (!this.catalog.TryResolveEntry(
                moduleDef,
                state.ActiveSourceBenchDefName,
                state.ActiveRecipeDefName,
                out entry,
                out failureReason))
            {
                state.SetStatus(AutoWorkTableStatus.RecoveryBlocked, failureReason);
                state.SuspendRecipeChecks();
                return;
            }

            if (!this.productFactory.TryCreateProducts(
                entry.RecipeDef,
                state.StagedIngredients,
                state.PendingProducts,
                out failureReason))
            {
                state.SetStatus(AutoWorkTableStatus.RecoveryBlocked, failureReason);
                state.SuspendRecipeChecks();
                return;
            }

            if (!state.TryDestroyStagedIngredients(out failureReason))
            {
                // Products are already durable at this point. Keep both owners and the active job
                // so the next tick blocks instead of depositing products and completing twice.
                state.SetStatus(
                    AutoWorkTableStatus.CompletionCommitBlocked,
                    "Finished production created pending products, but staged ingredients could not be consumed: " +
                        (failureReason ?? "null"));
                state.SuspendRecipeChecks();
                return;
            }

            state.NotifyProductionCompleted();
            state.ClearActiveProduction();
            if (state.HasPendingProducts)
            {
                state.SetStatus(AutoWorkTableStatus.OutputBlocked);
                this.TryDepositPendingProducts(context, moduleDef, state);
                return;
            }

            this.SetPostOutputStatus(context, moduleDef, state);
            }
            finally
            {
                AutoWorkTableRuntimeProfiler.Record(
                    profilingEnabled,
                    AutoWorkTableProfileSection.Completion,
                    startedAt);
            }
        }

        private void TryRecoverStagedIngredientsToCargo(
            ShuttleModuleRuntimeContext context,
            ShuttleAutoWorkTableModuleDef moduleDef,
            AutoWorkTableRuntimeState state)
        {
            bool profilingEnabled = AutoWorkTableRuntimeProfiler.Enabled;
            long startedAt = AutoWorkTableRuntimeProfiler.Start(profilingEnabled);
            try
            {
            if (state == null || !state.HasStagedIngredients)
            {
                return;
            }

            string failureReason;
            if (!this.ingredientStager.TryRecoverStagedIngredientsToCargo(
                context,
                moduleDef,
                state,
                out failureReason))
            {
                state.SetStatus(AutoWorkTableStatus.RecoveryBlocked, failureReason);
                return;
            }

            this.SetPostOutputStatus(context, moduleDef, state);
            }
            finally
            {
                AutoWorkTableRuntimeProfiler.Record(
                    profilingEnabled,
                    AutoWorkTableProfileSection.Recovery,
                    startedAt);
            }
        }

        private void TryDepositPendingProducts(
            ShuttleModuleRuntimeContext context,
            ShuttleAutoWorkTableModuleDef moduleDef,
            AutoWorkTableRuntimeState state)
        {
            bool profilingEnabled = AutoWorkTableRuntimeProfiler.Enabled;
            long startedAt = AutoWorkTableRuntimeProfiler.Start(profilingEnabled);
            AutoWorkTableRuntimeProfiler.Count(
                profilingEnabled,
                AutoWorkTableProfileCounter.DepositAttempts);
            try
            {
            bool shouldSetPostOutputStatus;
            this.productDepositor.TryDepositPendingProducts(
                context,
                moduleDef,
                state,
                out shouldSetPostOutputStatus);
            if (shouldSetPostOutputStatus)
            {
                this.SetPostOutputStatus(context, moduleDef, state);
            }
            }
            finally
            {
                AutoWorkTableRuntimeProfiler.Record(
                    profilingEnabled,
                    AutoWorkTableProfileSection.ProductDeposit,
                    startedAt);
            }
        }

        private void SetPostOutputStatus(
            ShuttleModuleRuntimeContext context,
            ShuttleAutoWorkTableModuleDef moduleDef,
            AutoWorkTableRuntimeState state)
        {
            if (state == null)
            {
                return;
            }

            state.ClearIngredientAvailability();
            if (state.IsPaused)
            {
                state.SetStatus(AutoWorkTableStatus.Paused);
                state.ScheduleRecipeCheck(context.TicksGame, moduleDef.recipeCheckIntervalTicks);
                return;
            }

            state.SetStatus(AutoWorkTableStatus.Idle);
            state.ScheduleRecipeCheck(context.TicksGame, 1);
        }

        private bool TryValidateCycleStart(
            ShuttleModuleRuntimeContext context,
            ShuttleAutoWorkTableModuleDef moduleDef,
            string sourceBenchDefName,
            string recipeDefName,
            CargoAvailabilitySnapshot availabilitySnapshot,
            out AutoWorkTableRecipeCatalogEntry entry,
            out string failureReason)
        {
            entry = null;
            failureReason = null;

            if (!this.TryValidateRecipeEntry(
                moduleDef,
                sourceBenchDefName,
                recipeDefName,
                out entry,
                out failureReason))
            {
                return false;
            }

            return this.TryValidateCargoAccess(
                context,
                moduleDef,
                availabilitySnapshot,
                out failureReason);
        }

        private bool TryValidateRecipeEntry(
            ShuttleAutoWorkTableModuleDef moduleDef,
            string sourceBenchDefName,
            string recipeDefName,
            out AutoWorkTableRecipeCatalogEntry entry,
            out string failureReason)
        {
            entry = null;
            failureReason = null;

            if (!this.catalog.TryResolveEntry(
                moduleDef,
                sourceBenchDefName,
                recipeDefName,
                out entry,
                out failureReason))
            {
                return false;
            }

            if (!entry.RecipeDef.AvailableNow)
            {
                failureReason = "Recipe " + entry.RecipeDefName + " is not currently available.";
                return false;
            }

            if (!this.VirtualSkillRequirementsPass(moduleDef, entry.RecipeDef, out failureReason))
            {
                return false;
            }

            return true;
        }

        private bool TryValidateCargoAccess(
            ShuttleModuleRuntimeContext context,
            ShuttleAutoWorkTableModuleDef moduleDef,
            out string failureReason)
        {
            return this.TryValidateCargoAccess(
                context,
                moduleDef,
                null,
                out failureReason);
        }

        private bool TryValidateCargoAccess(
            ShuttleModuleRuntimeContext context,
            ShuttleAutoWorkTableModuleDef moduleDef,
            CargoAvailabilitySnapshot availabilitySnapshot,
            out string failureReason)
        {
            failureReason = null;
            if (context == null)
            {
                failureReason = "AutoWorkTable runtime context is unavailable.";
                return false;
            }

            if (moduleDef.requirePoweredInternalBus && !context.InternalBusPowered)
            {
                failureReason = "Internal power bus is offline.";
                return false;
            }

            bool cargoAvailable = availabilitySnapshot != null
                ? availabilitySnapshot.IsAvailable
                : context.CargoResourceBroker != null && context.CargoResourceBroker.IsAvailable;
            if (context.CargoResourceBroker == null || !cargoAvailable)
            {
                failureReason = "Cargo broker is not available.";
                return false;
            }

            if (!moduleDef.requiresCargoLogistics)
            {
                return true;
            }

            if (context.Profile == null || context.Profile.CargoLogistics == null)
            {
                failureReason = "Cargo logistics profile is unavailable.";
                return false;
            }

            if (!context.Profile.CargoLogistics.HasCargoLogistics)
            {
                failureReason = "Cargo logistics module is missing.";
                return false;
            }

            if (!context.Profile.CargoLogistics.SupportsItemConsumption)
            {
                failureReason = "Cargo logistics does not support item consumption.";
                return false;
            }

            if (!context.Profile.CargoLogistics.SupportsItemDeposit)
            {
                failureReason = "Cargo logistics does not support item deposit.";
                return false;
            }

            return true;
        }

        private CargoAvailabilitySnapshot GetCargoAvailabilitySnapshot(
            ShuttleModuleRuntimeContext context)
        {
            return context != null && context.CargoResourceBroker != null
                ? context.CargoResourceBroker.GetAvailabilitySnapshot()
                : null;
        }

        private bool TryAutoPickFirstCraftableRecipe(
            ShuttleModuleRuntimeContext context,
            ShuttleAutoWorkTableModuleDef moduleDef,
            AutoWorkTableRuntimeState state,
            CargoAvailabilitySnapshot availabilitySnapshot)
        {
            if (availabilitySnapshot == null)
            {
                availabilitySnapshot = this.GetCargoAvailabilitySnapshot(context);
            }

            IReadOnlyList<AutoWorkTableRecipeCatalogEntry> entries = this.catalog.GetAvailableEntries(moduleDef);
            for (int i = 0; i < entries.Count; i++)
            {
                AutoWorkTableRecipeCatalogEntry entry = entries[i];
                if (entry == null || entry.RecipeDef == null || entry.SourceBenchDef == null)
                {
                    continue;
                }

                string failureReason;
                AutoWorkTableRecipeCatalogEntry validatedEntry;
                if (!this.TryValidateCycleStart(
                    context,
                    moduleDef,
                    entry.SourceBenchDefName,
                    entry.RecipeDefName,
                    availabilitySnapshot,
                    out validatedEntry,
                    out failureReason))
                {
                    continue;
                }

                List<CargoIngredientRequirement> requirements;
                if (!this.ingredientResolver.TryResolveRequirements(
                    entry.RecipeDef,
                    availabilitySnapshot,
                    out requirements,
                    out failureReason))
                {
                    continue;
                }

                AutoWorkTableProductionOrderState addedOrder;
                return state.TryAddOrder(
                    entry.SourceBenchDefName,
                    entry.RecipeDefName,
                    out addedOrder,
                    out failureReason);
            }

            return false;
        }

        private bool VirtualSkillRequirementsPass(
            ShuttleAutoWorkTableModuleDef moduleDef,
            RecipeDef recipeDef,
            out string failureReason)
        {
            failureReason = null;
            if (recipeDef == null || recipeDef.skillRequirements == null)
            {
                return true;
            }

            for (int i = 0; i < recipeDef.skillRequirements.Count; i++)
            {
                SkillRequirement skillRequirement = recipeDef.skillRequirements[i];
                if (skillRequirement == null || skillRequirement.skill == null)
                {
                    continue;
                }

                if (moduleDef.GetVirtualSkillLevel(skillRequirement.skill) < skillRequirement.minLevel)
                {
                    failureReason = "Virtual skill " + skillRequirement.skill.defName +
                        " does not satisfy recipe " + recipeDef.defName + ".";
                    return false;
                }
            }

            return true;
        }

        private bool ShouldCheckRecipe(AutoWorkTableRuntimeState state, int ticksGame)
        {
            return state.NextRecipeCheckTick < 0 || ticksGame >= state.NextRecipeCheckTick;
        }

        private AutoWorkTableStatus StatusForValidationFailure(string failureReason)
        {
            if (string.IsNullOrEmpty(failureReason))
            {
                return AutoWorkTableStatus.Error;
            }

            if (failureReason.IndexOf("ingredient", StringComparison.OrdinalIgnoreCase) >= 0 ||
                failureReason.IndexOf("missing", StringComparison.OrdinalIgnoreCase) >= 0 ||
                failureReason.IndexOf("lacks required", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return AutoWorkTableStatus.WaitingForIngredients;
            }

            if (failureReason.IndexOf("cargo", StringComparison.OrdinalIgnoreCase) >= 0 ||
                failureReason.IndexOf("inventory", StringComparison.OrdinalIgnoreCase) >= 0 ||
                failureReason.IndexOf("logistics", StringComparison.OrdinalIgnoreCase) >= 0 ||
                failureReason.IndexOf("power", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return AutoWorkTableStatus.WaitingForCargo;
            }

            return AutoWorkTableStatus.Error;
        }
    }
}
