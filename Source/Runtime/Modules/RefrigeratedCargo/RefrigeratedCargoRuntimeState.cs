using System;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.RefrigeratedCargo
{
    /// <summary>
    /// Durable retry/throttle telemetry for refrigerated cargo auto-transfer.
    /// It intentionally stores no Thing references; the transfer service rebuilds candidates
    /// from current cargo holders each pass.
    /// </summary>
    internal sealed class RefrigeratedCargoRuntimeState : IShuttleModuleRuntimeState
    {
        private const int CurrentSaveVersion = 1;

        private int saveVersion = CurrentSaveVersion;
        private int nextAutoTransferTick = -1;
        private int lastAutoTransferTick = -1;
        private int lastMovedStackCount;
        private int lastMovedThingCount;
        private int lastFailureTick = -1;
        private int consecutiveFailures;
        private string lastFailureReason;
        private string lastStatus;

        internal int NextAutoTransferTick
        {
            get
            {
                return this.nextAutoTransferTick;
            }
        }

        internal string LastFailureReason
        {
            get
            {
                return this.lastFailureReason;
            }
        }

        public void EnsureInitialized()
        {
            if (this.nextAutoTransferTick < -1)
            {
                this.nextAutoTransferTick = -1;
            }

            if (this.lastAutoTransferTick < -1)
            {
                this.lastAutoTransferTick = -1;
            }

            if (this.lastFailureTick < -1)
            {
                this.lastFailureTick = -1;
            }

            if (this.lastMovedStackCount < 0)
            {
                this.lastMovedStackCount = 0;
            }

            if (this.lastMovedThingCount < 0)
            {
                this.lastMovedThingCount = 0;
            }

            if (this.consecutiveFailures < 0)
            {
                this.consecutiveFailures = 0;
            }
        }

        internal bool ShouldRun(int ticksGame)
        {
            return this.nextAutoTransferTick < 0 || ticksGame >= this.nextAutoTransferTick;
        }

        internal void ScheduleNext(int ticksGame, int intervalTicks)
        {
            this.nextAutoTransferTick = intervalTicks > 0
                ? ticksGame + intervalTicks
                : -1;
        }

        internal void DisableAutoTransferSchedule(string status)
        {
            this.lastStatus = status;
            this.lastFailureReason = null;
            this.consecutiveFailures = 0;
            this.nextAutoTransferTick = -1;
        }

        internal void RecordSkipped(int ticksGame, int intervalTicks, string status)
        {
            this.lastStatus = status;
            this.lastFailureReason = null;
            this.consecutiveFailures = 0;
            this.ScheduleNext(ticksGame, intervalTicks);
        }

        internal void RecordSuccess(
            int ticksGame,
            int movedStackCount,
            int movedThingCount,
            int intervalTicks,
            string status)
        {
            this.lastAutoTransferTick = ticksGame;
            this.lastMovedStackCount = Math.Max(0, movedStackCount);
            this.lastMovedThingCount = Math.Max(0, movedThingCount);
            this.lastStatus = status;
            this.lastFailureReason = null;
            this.consecutiveFailures = 0;
            this.ScheduleNext(ticksGame, intervalTicks);
        }

        internal void RecordFailure(int ticksGame, string failureReason, int intervalTicks)
        {
            this.lastFailureTick = ticksGame;
            this.lastFailureReason = failureReason;
            this.lastStatus = "failed";
            if (this.consecutiveFailures < 1000)
            {
                this.consecutiveFailures++;
            }

            this.nextAutoTransferTick = ticksGame + this.GetFailureBackoffTicks(intervalTicks);
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.saveVersion, "saveVersion", 0);
            Scribe_Values.Look(ref this.nextAutoTransferTick, "nextAutoTransferTick", -1);
            Scribe_Values.Look(ref this.lastAutoTransferTick, "lastAutoTransferTick", -1);
            Scribe_Values.Look(ref this.lastMovedStackCount, "lastMovedStackCount", 0);
            Scribe_Values.Look(ref this.lastMovedThingCount, "lastMovedThingCount", 0);
            Scribe_Values.Look(ref this.lastFailureTick, "lastFailureTick", -1);
            Scribe_Values.Look(ref this.consecutiveFailures, "consecutiveFailures", 0);
            Scribe_Values.Look(ref this.lastFailureReason, "lastFailureReason", null);
            Scribe_Values.Look(ref this.lastStatus, "lastStatus", null);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                int loadedVersion = this.saveVersion;
                this.EnsureInitialized();
                this.MigratePostLoad(loadedVersion);
                this.saveVersion = CurrentSaveVersion;
            }
        }

        private int GetFailureBackoffTicks(int intervalTicks)
        {
            int baseInterval = Math.Max(intervalTicks > 0 ? intervalTicks : 600, 600);
            int multiplier = Math.Min(this.consecutiveFailures, 4);
            return Math.Min(baseInterval * multiplier, 5000);
        }

        private void MigratePostLoad(int loadedVersion)
        {
            if (loadedVersion > CurrentSaveVersion)
            {
                Log.Warning("[CeleTech Shuttle] Loaded RefrigeratedCargoRuntimeState save version " +
                    loadedVersion + " newer than supported version " + CurrentSaveVersion +
                    ". Keeping loaded automatic transfer throttle state intact.");
            }
        }
    }
}
