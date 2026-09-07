using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.AutoWorkTable
{
    /// <summary>
    /// Durable private payload for one installed AutoWorkTable module.
    /// It owns progress and pending products; cargo truth remains broker-owned.
    /// </summary>
    internal sealed class AutoWorkTableRuntimeState :
        IShuttleModuleRuntimeState,
        IThingHolder,
        IAutoWorkTableRuntimeStateReadOnly
    {
        private const int CurrentSaveVersion = 6;
        internal const int MaxProductionOrders = 15;
        private const float Epsilon = 0.0001f;

        private int saveVersion = CurrentSaveVersion;
        private string selectedSourceBenchDefName;
        private string selectedRecipeDefName;
        private string activeSourceBenchDefName;
        private string activeRecipeDefName;
        private float workLeft;
        private int lastWorkTick = -1;
        private int nextRecipeCheckTick = -1;
        private int nextDepositRetryTick = -1;
        private AutoWorkTableStatus status = AutoWorkTableStatus.Idle;
        private string lastFailureReason;
        private AutoWorkTableProductionMode productionMode = AutoWorkTableProductionMode.OneShot;
        private int requestedCount = 1;
        private int remainingCount = 1;
        private int targetCount = 1;
        private int completedCount;
        private int targetStockCount;
        private bool isPaused;
        private bool defaultSelectionResolved;
        private string ingredientStatusSummary;
        private string missingIngredientSummary;
        private readonly AutoWorkTableProductionQueueState productionQueue =
            new AutoWorkTableProductionQueueState();
        private int activeOrderId = -1;
        // Top-level ThingOwners must be both Scribe_Deep saved and exposed through holder roots
        // so RimWorld can traverse pending products and staged ingredients after load/recovery.
        private ThingOwner<Thing> pendingProducts;
        private ThingOwner<Thing> stagedIngredients;
        private ThingOwnerHolderRoot pendingProductsRoot;
        private ThingOwnerHolderRoot stagedIngredientsRoot;

        public AutoWorkTableRuntimeState()
        {
            this.pendingProducts = new ThingOwner<Thing>(this, false, LookMode.Deep, true);
            this.stagedIngredients = new ThingOwner<Thing>(this, false, LookMode.Deep, true);
            this.EnsureHolderRoots();
        }

        public IThingHolder ParentHolder
        {
            get
            {
                return null;
            }
        }

        public bool HasActiveProduction
        {
            get
            {
                return this.status == AutoWorkTableStatus.Working ||
                    this.workLeft > Epsilon ||
                    this.activeOrderId > 0 ||
                    !string.IsNullOrEmpty(this.activeSourceBenchDefName) ||
                    !string.IsNullOrEmpty(this.activeRecipeDefName);
            }
        }

        public bool HasPendingProducts
        {
            get
            {
                return this.pendingProducts != null && this.pendingProducts.Count > 0;
            }
        }

        public bool HasStagedIngredients
        {
            get
            {
                return this.stagedIngredients != null && this.stagedIngredients.Count > 0;
            }
        }

        public bool CompletionCommitBlocked
        {
            get
            {
                return this.HasActiveProduction &&
                    this.HasPendingProducts &&
                    this.HasStagedIngredients;
            }
        }

        public bool IsPaused
        {
            get
            {
                return this.isPaused;
            }
        }

        public AutoWorkTableStatus Status
        {
            get
            {
                return this.status;
            }
        }

        public string ActiveSourceBenchDefName
        {
            get
            {
                return this.activeSourceBenchDefName;
            }
        }

        public string ActiveRecipeDefName
        {
            get
            {
                return this.activeRecipeDefName;
            }
        }

        public string SelectedSourceBenchDefName
        {
            get
            {
                AutoWorkTableProductionOrderState order = this.GetPrimaryOrderForRead();
                return order != null
                    ? order.SourceBenchDefName
                    : this.selectedSourceBenchDefName;
            }
        }

        public string SelectedRecipeDefName
        {
            get
            {
                AutoWorkTableProductionOrderState order = this.GetPrimaryOrderForRead();
                return order != null
                    ? order.RecipeDefName
                    : this.selectedRecipeDefName;
            }
        }

        public AutoWorkTableProductionMode ProductionMode
        {
            get
            {
                AutoWorkTableProductionOrderState order = this.GetPrimaryOrderForRead();
                return order != null ? order.ProductionMode : this.productionMode;
            }
        }

        public int RequestedCount
        {
            get
            {
                AutoWorkTableProductionOrderState order = this.GetPrimaryOrderForRead();
                return order != null ? order.RequestedCount : this.requestedCount;
            }
        }

        public int RemainingCount
        {
            get
            {
                AutoWorkTableProductionOrderState order = this.GetPrimaryOrderForRead();
                return order != null ? order.RemainingCount : this.remainingCount;
            }
        }

        public int TargetCount
        {
            get
            {
                AutoWorkTableProductionOrderState order = this.GetPrimaryOrderForRead();
                return order != null ? order.TargetCount : this.targetCount;
            }
        }

        public int CompletedCount
        {
            get
            {
                AutoWorkTableProductionOrderState order = this.GetPrimaryOrderForRead();
                return order != null ? order.CompletedCount : this.completedCount;
            }
        }

        public int TargetStockCount
        {
            get
            {
                AutoWorkTableProductionOrderState order = this.GetPrimaryOrderForRead();
                return order != null ? order.TargetStockCount : this.targetStockCount;
            }
        }

        internal IReadOnlyList<AutoWorkTableProductionOrderState> ProductionOrders
        {
            get
            {
                this.EnsureProductionOrders();
                return this.productionQueue.Orders;
            }
        }

        internal int ProductionOrderCount
        {
            get
            {
                return this.productionQueue.Count;
            }
        }

        internal int ActiveOrderId { get { return this.activeOrderId; } }
        internal int QueueRevision { get { return this.productionQueue.Revision; } }

        internal float WorkLeft
        {
            get
            {
                return this.workLeft;
            }
        }

        internal int LastWorkTick
        {
            get
            {
                return this.lastWorkTick;
            }
        }

        internal int NextRecipeCheckTick
        {
            get
            {
                return this.nextRecipeCheckTick;
            }
        }

        internal int NextDepositRetryTick
        {
            get
            {
                return this.nextDepositRetryTick;
            }
        }

        internal string LastFailureReason
        {
            get
            {
                return this.lastFailureReason;
            }
        }

        internal string IngredientStatusSummary
        {
            get
            {
                AutoWorkTableProductionOrderState order = this.GetPrimaryOrderForRead();
                return order != null && !string.IsNullOrEmpty(order.IngredientStatusSummary)
                    ? order.IngredientStatusSummary
                    : this.ingredientStatusSummary;
            }
        }

        internal string MissingIngredientSummary
        {
            get
            {
                AutoWorkTableProductionOrderState order = this.GetPrimaryOrderForRead();
                return order != null && !string.IsNullOrEmpty(order.MissingIngredientSummary)
                    ? order.MissingIngredientSummary
                    : this.missingIngredientSummary;
            }
        }

        internal int PendingProductCount
        {
            get
            {
                return this.pendingProducts != null ? this.pendingProducts.Count : 0;
            }
        }

        internal ThingOwner<Thing> PendingProducts
        {
            get
            {
                this.EnsureInitialized();
                return this.pendingProducts;
            }
        }

        internal ThingOwner<Thing> StagedIngredients
        {
            get
            {
                this.EnsureInitialized();
                return this.stagedIngredients;
            }
        }

        public void EnsureInitialized()
        {
            if (this.pendingProducts == null)
            {
                this.pendingProducts = new ThingOwner<Thing>(this, false, LookMode.Deep, true);
            }

            if (this.stagedIngredients == null)
            {
                this.stagedIngredients = new ThingOwner<Thing>(this, false, LookMode.Deep, true);
            }

            this.EnsureHolderRoots();
            this.EnsureProductionOrders();
            this.RebindActiveOrderIfNeeded();

            if (this.CompletionCommitBlocked)
            {
                this.status = AutoWorkTableStatus.CompletionCommitBlocked;
            }
            else if (this.pendingProducts.Count > 0)
            {
                this.status = AutoWorkTableStatus.OutputBlocked;
            }
            else if (this.status == AutoWorkTableStatus.OutputBlocked)
            {
                this.status = this.HasActiveProduction
                    ? AutoWorkTableStatus.Working
                    : AutoWorkTableStatus.Idle;
            }

            if (this.workLeft < 0f)
            {
                this.workLeft = 0f;
            }

            this.SanitizeProductionPolicy();

            if (this.HasStagedIngredients &&
                !this.HasActiveProduction &&
                this.status != AutoWorkTableStatus.OutputBlocked)
            {
                this.status = AutoWorkTableStatus.RecoveryBlocked;
            }
        }

        internal void SetDefaultSelectionIfEmpty(string sourceBenchDefName, string recipeDefName)
        {
            if (this.defaultSelectionResolved)
            {
                return;
            }

            this.EnsureProductionOrders();
            if (this.productionQueue.Count > 0 ||
                string.IsNullOrEmpty(sourceBenchDefName) ||
                string.IsNullOrEmpty(recipeDefName))
            {
                this.defaultSelectionResolved = true;
                return;
            }

            this.AddOrderUnchecked(sourceBenchDefName, recipeDefName);
            this.defaultSelectionResolved = true;
            this.AdvanceQueueRevision();
        }

        internal bool TrySetSelection(
            string sourceBenchDefName,
            string recipeDefName,
            out string failureReason)
        {
            failureReason = null;
            if (this.HasActiveProduction)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_CannotChangeRecipeActiveProduction"
                    .Translate()
                    .ToString();
                return false;
            }

            if (this.HasPendingProducts)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_CannotChangeRecipePendingProducts"
                    .Translate()
                    .ToString();
                return false;
            }

            if (this.HasStagedIngredients)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_CannotChangeRecipeStagedIngredients"
                    .Translate()
                    .ToString();
                return false;
            }

            this.EnsureProductionOrders();
            this.productionQueue.Clear();
            if (!string.IsNullOrEmpty(sourceBenchDefName) &&
                !string.IsNullOrEmpty(recipeDefName))
            {
                this.AddOrderUnchecked(sourceBenchDefName, recipeDefName);
            }

            this.selectedSourceBenchDefName = null;
            this.selectedRecipeDefName = null;
            this.defaultSelectionResolved = true;
            this.isPaused = false;
            this.ResetLegacyProductionPolicy();

            this.status = string.IsNullOrEmpty(sourceBenchDefName) || string.IsNullOrEmpty(recipeDefName)
                ? AutoWorkTableStatus.WaitingForSelection
                : AutoWorkTableStatus.Idle;
            this.lastFailureReason = null;
            this.nextRecipeCheckTick = -1;
            this.ClearIngredientAvailability();
            this.AdvanceQueueRevision();
            return true;
        }

        internal void SetPaused(bool paused, int ticksGame)
        {
            this.isPaused = paused;
            this.lastFailureReason = null;
            if (!this.isPaused)
            {
                this.nextRecipeCheckTick = -1;
                if (this.status == AutoWorkTableStatus.Paused)
                {
                    this.status = this.HasActiveProduction
                        ? AutoWorkTableStatus.Working
                        : AutoWorkTableStatus.Idle;
                }

                return;
            }

            if (ticksGame >= 0)
            {
                this.lastWorkTick = ticksGame;
            }

            this.status = AutoWorkTableStatus.Paused;
        }

        internal void SetProductionPolicy(
            AutoWorkTableProductionMode mode,
            int repeatCount,
            int targetCount)
        {
            AutoWorkTableProductionOrderState order = this.GetPrimaryOrderForRead();
            if (order != null)
            {
                order.SetProductionPolicy(mode, repeatCount, targetCount);
                this.AdvanceQueueRevision();
                return;
            }

            this.SetLegacyProductionPolicy(mode, repeatCount, targetCount);
        }

        internal void NotifyProductionCompleted()
        {
            AutoWorkTableProductionOrderState order = this.GetActiveOrder();
            if (order == null)
            {
                order = this.GetPrimaryOrderForRead();
            }

            if (order != null)
            {
                order.NotifyProductionCompleted();
                return;
            }

            this.completedCount++;
            if ((this.productionMode == AutoWorkTableProductionMode.OneShot ||
                    this.productionMode == AutoWorkTableProductionMode.RepeatCount) &&
                this.remainingCount > 0)
            {
                this.remainingCount--;
            }
        }

        internal void SetTargetStockCount(int value)
        {
            AutoWorkTableProductionOrderState order = this.GetPrimaryOrderForRead();
            if (order != null)
            {
                order.SetTargetStockCount(value);
                return;
            }

            this.targetStockCount = Max(0, value);
        }

        internal void SetIngredientAvailability(AutoWorkTableIngredientAvailability availability)
        {
            this.ingredientStatusSummary = availability != null ? availability.Summary : null;
            this.missingIngredientSummary = availability != null ? availability.MissingSummary : null;
            AutoWorkTableProductionOrderState order = this.GetActiveOrder();
            if (order == null)
            {
                order = this.GetPrimaryOrderForRead();
            }

            if (order != null)
            {
                order.SetIngredientAvailability(availability);
            }
        }

        internal void ClearIngredientAvailability()
        {
            this.ingredientStatusSummary = null;
            this.missingIngredientSummary = null;
        }

        internal void BeginWork(
            string sourceBenchDefName,
            string recipeDefName,
            float workAmount,
            int ticksGame)
        {
            AutoWorkTableProductionOrderState order = this.GetPrimaryOrderForRead();
            this.BeginWork(
                order != null ? order.OrderId : -1,
                sourceBenchDefName,
                recipeDefName,
                workAmount,
                ticksGame);
        }

        internal void BeginWork(
            int orderId,
            string sourceBenchDefName,
            string recipeDefName,
            float workAmount,
            int ticksGame)
        {
            this.activeOrderId = orderId;
            this.activeSourceBenchDefName = sourceBenchDefName;
            this.activeRecipeDefName = recipeDefName;
            this.workLeft = workAmount > 0f ? workAmount : 0f;
            this.lastWorkTick = ticksGame;
            this.status = AutoWorkTableStatus.Working;
            this.lastFailureReason = null;
            this.ClearIngredientAvailability();
        }

        internal void SetLastWorkTick(int ticksGame)
        {
            this.lastWorkTick = ticksGame;
        }

        internal void AdvanceWork(float workAmount)
        {
            if (workAmount <= 0f)
            {
                return;
            }

            this.workLeft -= workAmount;
            if (this.workLeft < 0f)
            {
                this.workLeft = 0f;
            }
        }

        internal void ClearActiveProduction()
        {
            this.activeOrderId = -1;
            this.activeSourceBenchDefName = null;
            this.activeRecipeDefName = null;
            this.workLeft = 0f;
            this.lastWorkTick = -1;
        }

        internal bool TryDestroyStagedIngredients(out string failureReason)
        {
            failureReason = null;
            this.EnsureInitialized();
            while (this.stagedIngredients.Count > 0)
            {
                Thing thing = this.stagedIngredients[0];
                if (thing == null)
                {
                    failureReason = "Staged ingredient owner contains a null entry.";
                    return false;
                }

                Thing taken = null;
                try
                {
                    // Destructive consumption is allowed only at completion commit, after products
                    // have been created in pendingProducts. On any exception, try to put the taken
                    // ingredient back into stagedIngredients and leave the state blocked.
                    taken = this.stagedIngredients.Take(thing, thing.stackCount);
                    if (taken == null)
                    {
                        failureReason = "Failed to take staged ingredient for final consumption.";
                        return false;
                    }

                    if (!taken.Destroyed)
                    {
                        taken.Destroy(DestroyMode.Vanish);
                    }
                }
                catch (System.Exception exception)
                {
                    if (taken != null && !taken.Destroyed && !this.stagedIngredients.Contains(taken))
                    {
                        if (!this.stagedIngredients.TryAddOrTransfer(taken, false))
                        {
                            failureReason = "Destroying staged ingredient threw and rollback to staged owner failed. ingredient=" +
                                taken +
                                " exception=" +
                                exception.GetType().Name;
                            return false;
                        }
                    }

                    failureReason = "Destroying staged ingredient threw. ingredient=" +
                        (taken ?? thing) +
                        " exception=" +
                        exception.GetType().Name;
                    return false;
                }
            }

            if (this.stagedIngredients.Count > 0)
            {
                failureReason = "Staged ingredient owner was not fully cleared.";
                return false;
            }

            return true;
        }

        internal void SetStatus(AutoWorkTableStatus value, string failureReason = null)
        {
            this.status = value;
            this.lastFailureReason = failureReason;
            AutoWorkTableProductionOrderState order = this.GetActiveOrder();
            if (order == null)
            {
                order = this.GetPrimaryOrderForRead();
            }

            if (order != null)
            {
                order.SetTransientStatus(value, failureReason);
            }
        }

        internal void SetAggregateStatus(
            AutoWorkTableStatus value,
            string failureReason = null)
        {
            this.status = value;
            this.lastFailureReason = failureReason;
        }

        internal void ScheduleRecipeCheck(int ticksGame, int intervalTicks)
        {
            this.nextRecipeCheckTick = ticksGame + Max(1, intervalTicks);
        }

        internal void SuspendRecipeChecks()
        {
            this.nextRecipeCheckTick = int.MaxValue;
        }

        internal void ScheduleDepositRetry(int ticksGame, int intervalTicks)
        {
            this.nextDepositRetryTick = ticksGame + Max(1, intervalTicks);
        }

        internal AutoWorkTableProductionOrderState GetOrder(int orderId)
        {
            return this.productionQueue.GetOrder(orderId);
        }

        internal AutoWorkTableProductionOrderState GetActiveOrder()
        {
            return this.activeOrderId > 0 ? this.GetOrder(this.activeOrderId) : null;
        }

        internal bool TryAddOrder(
            string sourceBenchDefName,
            string recipeDefName,
            out AutoWorkTableProductionOrderState order,
            out string failureReason)
        {
            order = null;
            failureReason = null;
            this.EnsureProductionOrders();
            if (this.productionQueue.Count >= MaxProductionOrders)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_QueueFull".Translate(
                    MaxProductionOrders).ToString();
                return false;
            }

            if (string.IsNullOrEmpty(sourceBenchDefName) || string.IsNullOrEmpty(recipeDefName))
            {
                failureReason = "CT_Shuttle_AutoWorkTable_RecipeInfoMissing".Translate().ToString();
                return false;
            }

            order = this.AddOrderUnchecked(sourceBenchDefName, recipeDefName);
            this.defaultSelectionResolved = true;
            this.isPaused = false;
            this.status = AutoWorkTableStatus.Idle;
            this.lastFailureReason = null;
            this.nextRecipeCheckTick = -1;
            this.AdvanceQueueRevision();
            return true;
        }

        internal bool TryRemoveOrder(int orderId, out string failureReason)
        {
            failureReason = null;
            this.EnsureProductionOrders();
            AutoWorkTableProductionOrderState order = this.GetOrder(orderId);
            if (order == null)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_OrderMissing".Translate().ToString();
                return false;
            }

            bool removingActiveOrder = this.activeOrderId == orderId;
            if (removingActiveOrder && this.HasPendingProducts)
            {
                // Pending products mean completion has already crossed its durable create step.
                // Keep the active identity until the normal commit/recovery path settles it.
                failureReason = "CT_Shuttle_AutoWorkTable_CannotChangeActiveOrder".Translate().ToString();
                return false;
            }

            if (removingActiveOrder)
            {
                // Cancelling work ends the active identity immediately, but staged Things remain
                // module-owned recovery truth until the runtime cargo broker returns them.
                this.ClearActiveProduction();
            }

            if (!this.productionQueue.TryRemove(orderId, -1, out failureReason))
            {
                return false;
            }

            this.nextRecipeCheckTick = -1;
            if (this.HasStagedIngredients)
            {
                this.status = AutoWorkTableStatus.RecoveryBlocked;
                this.lastFailureReason = null;
            }
            else if (this.productionQueue.Count == 0)
            {
                this.status = this.isPaused
                    ? AutoWorkTableStatus.Paused
                    : AutoWorkTableStatus.WaitingForSelection;
                this.lastFailureReason = null;
            }
            else if (!this.HasActiveProduction)
            {
                this.status = this.isPaused
                    ? AutoWorkTableStatus.Paused
                    : AutoWorkTableStatus.Idle;
                this.lastFailureReason = null;
            }

            return true;
        }

        internal bool AbortActiveProductionAndSuspendOrder(string failureReason)
        {
            AutoWorkTableProductionOrderState order = this.GetActiveOrder();
            if (order == null || this.HasPendingProducts)
            {
                return false;
            }

            order.SetSuspended(true);
            order.SetTransientStatus(AutoWorkTableStatus.Paused, failureReason);
            this.ClearActiveProduction();
            this.status = this.HasStagedIngredients
                ? AutoWorkTableStatus.RecoveryBlocked
                : AutoWorkTableStatus.Paused;
            this.lastFailureReason = failureReason;
            this.nextRecipeCheckTick = -1;
            this.AdvanceQueueRevision();
            return true;
        }

        internal bool TryMoveOrder(int orderId, int direction, out string failureReason)
        {
            failureReason = null;
            this.EnsureProductionOrders();
            if (!this.productionQueue.TryMove(orderId, direction, out failureReason))
            {
                return false;
            }
            this.nextRecipeCheckTick = -1;
            return true;
        }

        internal bool TrySetOrderSuspended(
            int orderId,
            bool suspended,
            out string failureReason)
        {
            failureReason = null;
            AutoWorkTableProductionOrderState order = this.GetOrder(orderId);
            if (order == null)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_OrderMissing".Translate().ToString();
                return false;
            }

            order.SetSuspended(suspended);
            this.nextRecipeCheckTick = -1;
            this.AdvanceQueueRevision();
            return true;
        }

        internal bool TrySetOrderProductionPolicy(
            int orderId,
            AutoWorkTableProductionMode mode,
            int repeatCount,
            int targetCount,
            out string failureReason)
        {
            failureReason = null;
            AutoWorkTableProductionOrderState order = this.GetOrder(orderId);
            if (order == null)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_OrderMissing".Translate().ToString();
                return false;
            }

            if (this.activeOrderId == orderId)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_CannotChangeActiveOrder".Translate().ToString();
                return false;
            }

            order.SetProductionPolicy(mode, repeatCount, targetCount);
            this.nextRecipeCheckTick = -1;
            this.AdvanceQueueRevision();
            return true;
        }

        internal bool TrySetOrderIngredientFilter(
            int orderId,
            ThingFilter filter,
            bool useRecipeDefault,
            out string failureReason)
        {
            failureReason = null;
            AutoWorkTableProductionOrderState order = this.GetOrder(orderId);
            if (order == null)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_OrderMissing".Translate().ToString();
                return false;
            }

            if (this.activeOrderId == orderId)
            {
                failureReason = "CT_Shuttle_AutoWorkTable_CannotChangeActiveOrder".Translate().ToString();
                return false;
            }

            if (useRecipeDefault)
            {
                order.ClearCustomIngredientFilter();
            }
            else
            {
                order.SetCustomIngredientFilter(filter);
            }

            this.nextRecipeCheckTick = -1;
            this.AdvanceQueueRevision();
            return true;
        }

        internal bool ObserveCargoRevision(int cargoRevision)
        {
            if (!this.productionQueue.ObserveCargoRevision(cargoRevision))
            {
                return false;
            }
            if (!this.HasActiveProduction && !this.HasPendingProducts && !this.HasStagedIngredients)
            {
                this.nextRecipeCheckTick = -1;
            }

            return true;
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return null;
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            // Expose each top-level owner as its own holder root; appending only child holders
            // would hide the owner itself from RimWorld holder traversal.
            this.EnsureHolderRoots();
            outChildren.Add(this.pendingProductsRoot);
            outChildren.Add(this.stagedIngredientsRoot);
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.saveVersion, "saveVersion", 0);
            Scribe_Values.Look(ref this.selectedSourceBenchDefName, "selectedSourceBenchDefName");
            Scribe_Values.Look(ref this.selectedRecipeDefName, "selectedRecipeDefName");
            Scribe_Values.Look(ref this.activeSourceBenchDefName, "activeSourceBenchDefName");
            Scribe_Values.Look(ref this.activeRecipeDefName, "activeRecipeDefName");
            Scribe_Values.Look(ref this.workLeft, "workLeft", 0f);
            Scribe_Values.Look(ref this.lastWorkTick, "lastWorkTick", -1);
            Scribe_Values.Look(ref this.nextRecipeCheckTick, "nextRecipeCheckTick", -1);
            Scribe_Values.Look(ref this.nextDepositRetryTick, "nextDepositRetryTick", -1);
            Scribe_Values.Look(ref this.status, "status", AutoWorkTableStatus.Idle);
            Scribe_Values.Look(ref this.lastFailureReason, "lastFailureReason");
            Scribe_Values.Look(ref this.productionMode, "productionMode", AutoWorkTableProductionMode.OneShot);
            Scribe_Values.Look(ref this.requestedCount, "requestedCount", 1);
            Scribe_Values.Look(ref this.remainingCount, "remainingCount", 1);
            Scribe_Values.Look(ref this.targetCount, "targetCount", 1);
            Scribe_Values.Look(ref this.completedCount, "completedCount", 0);
            Scribe_Values.Look(ref this.targetStockCount, "targetStockCount", 0);
            Scribe_Values.Look(ref this.isPaused, "isPaused", false);
            Scribe_Values.Look(ref this.defaultSelectionResolved, "defaultSelectionResolved", false);
            this.productionQueue.ExposeFlatData();
            Scribe_Values.Look(ref this.activeOrderId, "activeOrderId", -1);
            Scribe_Deep.Look(ref this.pendingProducts, "pendingProducts", new object[] { this });
            Scribe_Deep.Look(ref this.stagedIngredients, "stagedIngredients", new object[] { this });

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                int loadedVersion = this.saveVersion;

                if (loadedVersion < 6)
                {
                    this.MigrateLegacySelectionToQueue();
                }

                this.EnsureInitialized();
                this.WarnIfLoadedVersionIsNewer(loadedVersion);
                if (loadedVersion < 5)
                {
                    this.defaultSelectionResolved = true;
                }

                this.SanitizeProductionPolicy();
                this.productionQueue.ResetTransientRevision();
                this.saveVersion = CurrentSaveVersion;
            }
        }

        private void WarnIfLoadedVersionIsNewer(int loadedVersion)
        {
            if (loadedVersion > CurrentSaveVersion)
            {
                Log.Warning("[CeleTech Shuttle] Loaded AutoWorkTableRuntimeState save version " +
                    loadedVersion + " newer than supported version " + CurrentSaveVersion +
                    ". Keeping loaded auto worktable state intact.");
            }
        }

        private void EnsureHolderRoots()
        {
            if (this.pendingProductsRoot == null)
            {
                this.pendingProductsRoot = new ThingOwnerHolderRoot(
                    this,
                    () => this.PendingProducts,
                    "AutoWorkTableRuntimeState.pendingProducts");
            }

            if (this.stagedIngredientsRoot == null)
            {
                this.stagedIngredientsRoot = new ThingOwnerHolderRoot(
                    this,
                    () => this.StagedIngredients,
                    "AutoWorkTableRuntimeState.stagedIngredients");
            }
        }

        private static int Max(int a, int b)
        {
            return a > b ? a : b;
        }

        private void ResetLegacyProductionPolicy()
        {
            this.productionMode = AutoWorkTableProductionMode.OneShot;
            this.requestedCount = 1;
            this.remainingCount = 1;
            this.targetCount = 1;
            this.completedCount = 0;
            this.targetStockCount = 0;
            this.isPaused = false;
            this.ClearIngredientAvailability();
        }

        private void SetLegacyProductionPolicy(
            AutoWorkTableProductionMode mode,
            int repeatCount,
            int targetCount)
        {
            this.productionMode = mode;
            this.targetStockCount = 0;
            this.targetCount = Max(1, targetCount);
            this.completedCount = 0;
            if (mode == AutoWorkTableProductionMode.RepeatCount)
            {
                this.requestedCount = Max(1, repeatCount);
                this.remainingCount = this.requestedCount;
            }
            else if (mode == AutoWorkTableProductionMode.RepeatForever ||
                mode == AutoWorkTableProductionMode.DoUntilStock)
            {
                this.requestedCount = 0;
                this.remainingCount = -1;
            }
            else
            {
                this.productionMode = AutoWorkTableProductionMode.OneShot;
                this.requestedCount = 1;
                this.remainingCount = 1;
            }

            this.ClearIngredientAvailability();
        }

        private AutoWorkTableProductionOrderState AddOrderUnchecked(
            string sourceBenchDefName,
            string recipeDefName)
        {
            return this.productionQueue.AddUnchecked(
                sourceBenchDefName,
                recipeDefName);
        }

        private void EnsureProductionOrders()
        {
            this.productionQueue.EnsureInitialized();
        }

        private void RebindActiveOrderIfNeeded()
        {
            bool hasActiveRecipe = !string.IsNullOrEmpty(this.activeSourceBenchDefName) &&
                !string.IsNullOrEmpty(this.activeRecipeDefName);
            if (!hasActiveRecipe)
            {
                if (this.workLeft <= Epsilon)
                {
                    this.activeOrderId = -1;
                }

                return;
            }

            AutoWorkTableProductionOrderState activeOrder = this.GetOrder(this.activeOrderId);
            if (activeOrder != null &&
                activeOrder.SourceBenchDefName == this.activeSourceBenchDefName &&
                activeOrder.RecipeDefName == this.activeRecipeDefName)
            {
                return;
            }

            IReadOnlyList<AutoWorkTableProductionOrderState> orders =
                this.productionQueue.Orders;
            for (int i = 0; i < orders.Count; i++)
            {
                AutoWorkTableProductionOrderState order = orders[i];
                if (order != null &&
                    order.SourceBenchDefName == this.activeSourceBenchDefName &&
                    order.RecipeDefName == this.activeRecipeDefName)
                {
                    this.activeOrderId = order.OrderId;
                    return;
                }
            }

            if (this.productionQueue.Count < MaxProductionOrders)
            {
                AutoWorkTableProductionOrderState recoveredOrder = this.AddOrderUnchecked(
                    this.activeSourceBenchDefName,
                    this.activeRecipeDefName);
                recoveredOrder.RestoreLegacyPolicy(
                    this.productionMode,
                    this.requestedCount,
                    this.remainingCount,
                    this.targetCount,
                    this.completedCount,
                    this.targetStockCount,
                    false);
                this.activeOrderId = recoveredOrder.OrderId;
            }
        }

        private AutoWorkTableProductionOrderState GetPrimaryOrderForRead()
        {
            this.EnsureProductionOrders();
            AutoWorkTableProductionOrderState activeOrder = this.GetOrder(this.activeOrderId);
            return activeOrder ?? this.productionQueue.GetFirstOrder();
        }

        private void AdvanceQueueRevision()
        {
            this.productionQueue.AdvanceRevision();
        }

        private void MigrateLegacySelectionToQueue()
        {
            this.EnsureProductionOrders();
            if (this.productionQueue.Count > 0)
            {
                return;
            }

            string sourceBenchDefName = !string.IsNullOrEmpty(this.activeSourceBenchDefName)
                ? this.activeSourceBenchDefName
                : this.selectedSourceBenchDefName;
            string recipeDefName = !string.IsNullOrEmpty(this.activeRecipeDefName)
                ? this.activeRecipeDefName
                : this.selectedRecipeDefName;
            if (string.IsNullOrEmpty(sourceBenchDefName) || string.IsNullOrEmpty(recipeDefName))
            {
                return;
            }

            AutoWorkTableProductionOrderState order = this.AddOrderUnchecked(
                sourceBenchDefName,
                recipeDefName);
            bool legacyPaused = this.isPaused;
            order.RestoreLegacyPolicy(
                this.productionMode,
                this.requestedCount,
                this.remainingCount,
                this.targetCount,
                this.completedCount,
                this.targetStockCount,
                legacyPaused);
            this.isPaused = false;
            if (!string.IsNullOrEmpty(this.activeRecipeDefName) || this.workLeft > Epsilon)
            {
                this.activeOrderId = order.OrderId;
            }

            this.selectedSourceBenchDefName = null;
            this.selectedRecipeDefName = null;
            this.defaultSelectionResolved = true;
        }

        private void SanitizeProductionPolicy()
        {
            if (this.productionMode != AutoWorkTableProductionMode.OneShot &&
                this.productionMode != AutoWorkTableProductionMode.RepeatCount &&
                this.productionMode != AutoWorkTableProductionMode.RepeatForever &&
                this.productionMode != AutoWorkTableProductionMode.DoUntilStock)
            {
                this.productionMode = AutoWorkTableProductionMode.OneShot;
            }

            if (this.productionMode == AutoWorkTableProductionMode.RepeatCount)
            {
                this.requestedCount = Max(1, this.requestedCount);
                if (this.remainingCount < 0)
                {
                    this.remainingCount = this.requestedCount;
                }
            }
            else if (this.productionMode == AutoWorkTableProductionMode.RepeatForever ||
                this.productionMode == AutoWorkTableProductionMode.DoUntilStock)
            {
                this.requestedCount = Max(0, this.requestedCount);
                this.remainingCount = -1;
            }
            else
            {
                this.productionMode = AutoWorkTableProductionMode.OneShot;
                this.requestedCount = 1;
                if (this.remainingCount < 0)
                {
                    this.remainingCount = 1;
                }
            }

            this.targetCount = Max(1, this.targetCount);
            this.completedCount = Max(0, this.completedCount);
            this.targetStockCount = Max(0, this.targetStockCount);
        }
    }
}
