namespace CeleTech.ShuttleExtension.ModularShuttle.Snapshots
{
    public sealed class ShuttleCargoUnloadProgressSnapshot
    {
        public bool Active;
        public int PendingStackCount;
        public int PendingThingCount;
        public int TotalStackCount;
        public int TotalThingCount;
        public int CompletedStackCount;
        public int CompletedThingCount;
        public int SkippedStackCount;
        public int SkippedThingCount;
        public int StartedTick = -1;
        public int LastCompletedTick = -1;
        public string LastFailureReason;
        public int Revision;
    }
}
