using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.AutoWorkTable
{
    /// <summary>
    /// Durable player intent for one AutoWorkTable production order. Ingredient availability
    /// and display status are deliberately transient and rebuilt from current cargo.
    /// </summary>
    internal sealed class AutoWorkTableProductionOrderState : IExposable
    {
        private const int CurrentSaveVersion = 1;

        private int saveVersion = CurrentSaveVersion;
        private int orderId;
        private string sourceBenchDefName;
        private string recipeDefName;
        private AutoWorkTableProductionMode productionMode = AutoWorkTableProductionMode.OneShot;
        private int requestedCount = 1;
        private int remainingCount = 1;
        private int targetCount = 1;
        private int completedCount;
        private int targetStockCount;
        private bool suspended;
        private bool hasCustomIngredientFilter;
        private ThingFilter customIngredientFilter;

        // These fields are runtime projections. Saving them would let stale cargo status survive
        // loading, so the scheduler rebuilds them after the next queue check.
        private AutoWorkTableStatus transientStatus = AutoWorkTableStatus.Idle;
        private string transientFailureReason;
        private string ingredientStatusSummary;
        private string missingIngredientSummary;
        private int filterRevision = 1;

        public AutoWorkTableProductionOrderState()
        {
        }

        internal AutoWorkTableProductionOrderState(
            int orderId,
            string sourceBenchDefName,
            string recipeDefName)
        {
            this.orderId = orderId;
            this.sourceBenchDefName = sourceBenchDefName;
            this.recipeDefName = recipeDefName;
            this.SetProductionPolicy(AutoWorkTableProductionMode.OneShot, 1, 1);
        }

        internal int OrderId { get { return this.orderId; } }
        internal string SourceBenchDefName { get { return this.sourceBenchDefName; } }
        internal string RecipeDefName { get { return this.recipeDefName; } }
        internal AutoWorkTableProductionMode ProductionMode { get { return this.productionMode; } }
        internal int RequestedCount { get { return this.requestedCount; } }
        internal int RemainingCount { get { return this.remainingCount; } }
        internal int TargetCount { get { return this.targetCount; } }
        internal int CompletedCount { get { return this.completedCount; } }
        internal int TargetStockCount { get { return this.targetStockCount; } }
        internal bool Suspended { get { return this.suspended; } }
        internal bool HasCustomIngredientFilter { get { return this.hasCustomIngredientFilter; } }
        internal ThingFilter CustomIngredientFilterForRead { get { return this.customIngredientFilter; } }
        internal AutoWorkTableStatus TransientStatus { get { return this.transientStatus; } }
        internal string TransientFailureReason { get { return this.transientFailureReason; } }
        internal string IngredientStatusSummary { get { return this.ingredientStatusSummary; } }
        internal string MissingIngredientSummary { get { return this.missingIngredientSummary; } }
        internal int FilterRevision { get { return this.filterRevision; } }

        internal void RepairOrderId(int value)
        {
            this.orderId = value > 0 ? value : 1;
        }

        internal bool IsFiniteProductionComplete
        {
            get
            {
                if (this.productionMode == AutoWorkTableProductionMode.OneShot)
                {
                    return this.remainingCount <= 0 || this.completedCount >= 1;
                }

                return this.productionMode == AutoWorkTableProductionMode.RepeatCount &&
                    this.remainingCount <= 0;
            }
        }

        internal void SetSuspended(bool value)
        {
            this.suspended = value;
            this.transientFailureReason = null;
            this.transientStatus = value
                ? AutoWorkTableStatus.Paused
                : AutoWorkTableStatus.Idle;
        }

        internal void SetProductionPolicy(
            AutoWorkTableProductionMode mode,
            int repeatCount,
            int targetCount)
        {
            this.productionMode = mode;
            this.targetStockCount = 0;
            this.completedCount = 0;
            this.targetCount = Max(1, targetCount);

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
            this.SetTransientStatus(AutoWorkTableStatus.Idle);
        }

        internal void RestoreLegacyPolicy(
            AutoWorkTableProductionMode mode,
            int requestedCount,
            int remainingCount,
            int targetCount,
            int completedCount,
            int targetStockCount,
            bool suspended)
        {
            this.productionMode = mode;
            this.requestedCount = requestedCount;
            this.remainingCount = remainingCount;
            this.targetCount = targetCount;
            this.completedCount = completedCount;
            this.targetStockCount = targetStockCount;
            this.suspended = suspended;
            this.Sanitize();
        }

        internal void NotifyProductionCompleted()
        {
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
            this.targetStockCount = Max(0, value);
        }

        internal void SetCustomIngredientFilter(ThingFilter filter)
        {
            this.hasCustomIngredientFilter = true;
            this.customIngredientFilter =
                AutoWorkTableIngredientFilterUtility.CopyOrNull(filter) ?? new ThingFilter();
            this.AdvanceFilterRevision();
            this.ClearIngredientAvailability();
        }

        internal void ClearCustomIngredientFilter()
        {
            this.hasCustomIngredientFilter = false;
            this.customIngredientFilter = null;
            this.AdvanceFilterRevision();
            this.ClearIngredientAvailability();
        }

        internal bool AllowsIngredient(ThingDef thingDef)
        {
            return !this.hasCustomIngredientFilter ||
                AutoWorkTableIngredientFilterUtility.Allows(
                    this.customIngredientFilter,
                    thingDef);
        }

        internal void SetIngredientAvailability(AutoWorkTableIngredientAvailability availability)
        {
            this.ingredientStatusSummary = availability != null ? availability.Summary : null;
            this.missingIngredientSummary = availability != null ? availability.MissingSummary : null;
        }

        internal void ClearIngredientAvailability()
        {
            this.ingredientStatusSummary = null;
            this.missingIngredientSummary = null;
        }

        internal void SetTransientStatus(
            AutoWorkTableStatus value,
            string failureReason = null)
        {
            this.transientStatus = value;
            this.transientFailureReason = failureReason;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.saveVersion, "saveVersion", 0);
            Scribe_Values.Look(ref this.orderId, "orderId", 0);
            Scribe_Values.Look(ref this.sourceBenchDefName, "sourceBenchDefName");
            Scribe_Values.Look(ref this.recipeDefName, "recipeDefName");
            Scribe_Values.Look(ref this.productionMode, "productionMode", AutoWorkTableProductionMode.OneShot);
            Scribe_Values.Look(ref this.requestedCount, "requestedCount", 1);
            Scribe_Values.Look(ref this.remainingCount, "remainingCount", 1);
            Scribe_Values.Look(ref this.targetCount, "targetCount", 1);
            Scribe_Values.Look(ref this.completedCount, "completedCount", 0);
            Scribe_Values.Look(ref this.targetStockCount, "targetStockCount", 0);
            Scribe_Values.Look(ref this.suspended, "suspended", false);
            Scribe_Values.Look(ref this.hasCustomIngredientFilter, "hasCustomIngredientFilter", false);
            Scribe_Deep.Look(ref this.customIngredientFilter, "customIngredientFilter");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.Sanitize();
                this.filterRevision = 1;
                this.transientStatus = this.suspended
                    ? AutoWorkTableStatus.Paused
                    : AutoWorkTableStatus.Idle;
                this.transientFailureReason = null;
                this.ClearIngredientAvailability();
                this.saveVersion = CurrentSaveVersion;
            }
        }

        private void Sanitize()
        {
            if (this.productionMode != AutoWorkTableProductionMode.OneShot &&
                this.productionMode != AutoWorkTableProductionMode.RepeatCount &&
                this.productionMode != AutoWorkTableProductionMode.RepeatForever &&
                this.productionMode != AutoWorkTableProductionMode.DoUntilStock)
            {
                this.productionMode = AutoWorkTableProductionMode.OneShot;
            }

            this.targetCount = Max(1, this.targetCount);
            this.completedCount = Max(0, this.completedCount);
            this.targetStockCount = Max(0, this.targetStockCount);

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

            if (this.hasCustomIngredientFilter && this.customIngredientFilter == null)
            {
                // An explicitly empty custom filter must stay empty instead of silently
                // reverting to recipe defaults after a missing-mod load.
                this.customIngredientFilter = new ThingFilter();
            }
            else if (!this.hasCustomIngredientFilter)
            {
                this.customIngredientFilter = null;
            }
        }

        private void AdvanceFilterRevision()
        {
            this.filterRevision = this.filterRevision == int.MaxValue
                ? 1
                : this.filterRevision + 1;
        }

        private static int Max(int left, int right)
        {
            return left > right ? left : right;
        }
    }
}
