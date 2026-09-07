namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.AutoWorkTable
{
    internal enum AutoWorkTableStatus
    {
        Idle = 0,
        WaitingForSelection = 1,
        WaitingForCargo = 2,
        WaitingForIngredients = 3,
        Working = 4,
        OutputBlocked = 5,
        Error = 6,
        RecoveryBlocked = 7,
        CompletionCommitBlocked = 8,
        ProductionCompleted = 9,
        TargetSatisfied = 10,
        Paused = 11
    }
}
