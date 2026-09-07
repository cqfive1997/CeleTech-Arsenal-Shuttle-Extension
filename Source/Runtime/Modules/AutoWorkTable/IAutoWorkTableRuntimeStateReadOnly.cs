namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.AutoWorkTable
{
    internal interface IAutoWorkTableRuntimeStateReadOnly
    {
        bool HasActiveProduction { get; }
        bool HasPendingProducts { get; }
        bool HasStagedIngredients { get; }
        bool CompletionCommitBlocked { get; }
        bool IsPaused { get; }
        AutoWorkTableStatus Status { get; }
        string ActiveSourceBenchDefName { get; }
        string ActiveRecipeDefName { get; }
        string SelectedSourceBenchDefName { get; }
        string SelectedRecipeDefName { get; }
        AutoWorkTableProductionMode ProductionMode { get; }
        int RequestedCount { get; }
        int RemainingCount { get; }
        int TargetCount { get; }
        int CompletedCount { get; }
        int TargetStockCount { get; }
    }
}
