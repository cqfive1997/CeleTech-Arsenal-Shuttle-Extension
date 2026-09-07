using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Presentation
{
    public sealed class ShuttleAutoWorkTableReadModel
    {
        public List<ShuttleAutoWorkTableModuleReadModel> Modules =
            new List<ShuttleAutoWorkTableModuleReadModel>();
    }

    public sealed class ShuttleAutoWorkTableModuleReadModel
    {
        public string ModuleInstanceID;
        public string ModuleDefName;
        public string ModuleLabel;
        public bool IsEnabled;
        public bool HasRuntimeState;
        public bool IsPaused;
        public string Status;
        public bool HasActiveProduction;
        public bool HasPendingProducts;
        public int PendingProductCount;
        public string SelectedSourceBenchDefName;
        public string SelectedRecipeDefName;
        public string ActiveSourceBenchDefName;
        public string ActiveRecipeDefName;
        public float WorkLeft;
        public string LastFailureReason;
        public string IngredientStatusSummary;
        public string MissingIngredientSummary;
        public string ProductionModeKey;
        public int RequestedCount;
        public int RemainingCount;
        public int TargetCount;
        public int CompletedCount;
        public int TargetStockCount;
        public string TargetProductDefName;
        public string TargetProductLabel;
        public bool SelectedRecipeSupportsDoUntilStock = true;
        public string SelectedRecipeDoUntilStockUnsupportedReason;
        public int QueueRevision;
        public List<ShuttleAutoWorkTableOrderReadModel> Orders =
            new List<ShuttleAutoWorkTableOrderReadModel>();
        public List<ShuttleAutoWorkTableRecipeReadModel> Recipes =
            new List<ShuttleAutoWorkTableRecipeReadModel>();
    }

    public sealed class ShuttleAutoWorkTableOrderReadModel
    {
        public int OrderId;
        public int QueueIndex;
        public bool IsActive;
        public bool IsSuspended;
        public bool CanMoveUp;
        public bool CanMoveDown;
        public string SourceBenchDefName;
        public string SourceBenchLabel;
        public string RecipeDefName;
        public string RecipeLabel;
        public string Status;
        public string FailureReason;
        public string IngredientStatusSummary;
        public string MissingIngredientSummary;
        public string ProductionModeKey;
        public int RequestedCount;
        public int RemainingCount;
        public int TargetCount;
        public int CompletedCount;
        public int TargetStockCount;
        public bool HasCustomIngredientFilter;
        public int IngredientFilterRevision;
        public ThingFilter IngredientFilter;
        public bool SupportsDoUntilStock = true;
        public string DoUntilStockUnsupportedReason;
    }

    public sealed class ShuttleAutoWorkTableRecipeReadModel
    {
        public string SourceBenchDefName;
        public string SourceBenchLabel;
        public string RecipeDefName;
        public string RecipeLabel;
        public string ProductCategoryKey;
        public string IngredientSummary;
        public string MissingIngredientSummary;
        public bool IngredientsAvailable;
        public int MechanoidDisassemblyCorpseCount;
        public string MechanoidDisassemblyCandidateLabel;
        public string MechanoidDisassemblyProductPreview;
        public bool MechanoidDisassemblyProductsVary;
        public bool HasMechanoidDisassemblyProductPreview;
        public int AnimalButcheryCorpseCount;
        public string AnimalButcheryCandidateLabel;
        public string AnimalButcheryProductPreview;
        public bool AnimalButcheryProductsVary;
        public bool HasAnimalButcheryProductPreview;
        public bool SupportsDoUntilStock = true;
        public string DoUntilStockUnsupportedReason;
        public List<ShuttleAutoWorkTableIngredientLineReadModel> IngredientLines =
            new List<ShuttleAutoWorkTableIngredientLineReadModel>();
    }

    public sealed class ShuttleAutoWorkTableIngredientLineReadModel
    {
        public string Label;
        public string DefName;
        public int RequiredCount;
        public int AvailableCount;
        public int MissingCount;
        public bool Available;
        public int RegularCargoCount;
        public int RefrigeratedCargoCount;
        public string Tooltip;
    }
}
