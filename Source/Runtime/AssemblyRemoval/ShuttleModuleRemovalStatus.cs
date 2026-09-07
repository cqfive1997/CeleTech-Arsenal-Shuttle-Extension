namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyRemoval
{
    internal enum ShuttleModuleRemovalStatus
    {
        None = 0,
        Working = 1,
        Blocked = 2,
        RefundPending = 3,
        WaitingForWorker = 4
    }
}
