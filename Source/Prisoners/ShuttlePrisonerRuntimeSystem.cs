using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Prisoners
{
    /// <summary>
    /// Shuttle-level Prison Cell runtime. It is intentionally not registered as a
    /// per-module runtime system so multiple Prison Cell modules cannot tick the
    /// same holder contents more than once.
    /// </summary>
    internal sealed class ShuttlePrisonerRuntimeSystem
    {
        private const int NeedsTickInterval = 150;

        private readonly ShuttlePrisonerNeedsService needsService =
            new ShuttlePrisonerNeedsService();
        private readonly ShuttlePrisonerAutoFeedingService autoFeedingService =
            new ShuttlePrisonerAutoFeedingService();
        private int lastNeedsTick = -1;

        internal void Tick(
            ThingWithComps shuttleHost,
            ShuttleProfile profile,
            int ticksGame)
        {
            if (!CanRunPrisonerRuntime(shuttleHost, profile, ticksGame))
            {
                return;
            }

            CompShuttlePrisonCellOccupancy occupancy =
                shuttleHost.TryGetComp<CompShuttlePrisonCellOccupancy>();
            if (occupancy == null || !occupancy.HasPrisoners)
            {
                // Do not make the next admitted prisoner pay hunger for time when the holder was empty.
                this.ResetNeedsTick(ticksGame);
                return;
            }

            CompShuttleHolderLaunchTransferState transferState =
                shuttleHost.TryGetComp<CompShuttleHolderLaunchTransferState>();
            if (transferState != null && transferState.HasAnyActiveOrRecoveryTransfer)
            {
                // Prisoners are staged outside the PrisonCell holder during transfer/recovery.
                // Reset instead of accruing a large catch-up hunger delta after restore.
                this.ResetNeedsTick(ticksGame);
                return;
            }

            this.MaybeTickNeeds(occupancy, ticksGame);
            if (this.autoFeedingService != null)
            {
                this.autoFeedingService.Tick(
                    shuttleHost,
                    occupancy,
                    ticksGame);
            }
        }

        private static bool CanRunPrisonerRuntime(
            ThingWithComps shuttleHost,
            ShuttleProfile profile,
            int ticksGame)
        {
            return ticksGame >= 0 &&
                profile != null &&
                profile.PrisonCell != null &&
                profile.PrisonCell.HasPrisonCell &&
                shuttleHost != null &&
                !shuttleHost.Destroyed;
        }

        private void MaybeTickNeeds(
            CompShuttlePrisonCellOccupancy occupancy,
            int ticksGame)
        {
            if (this.needsService == null)
            {
                return;
            }

            if (this.lastNeedsTick < 0)
            {
                this.lastNeedsTick = ticksGame;
                return;
            }

            int elapsedTicks = ticksGame - this.lastNeedsTick;
            if (elapsedTicks <= 0)
            {
                this.lastNeedsTick = ticksGame;
                return;
            }

            if (elapsedTicks < NeedsTickInterval)
            {
                return;
            }

            this.lastNeedsTick = ticksGame;
            this.needsService.TickFoodNeeds(occupancy, elapsedTicks);
        }

        private void ResetNeedsTick(int ticksGame)
        {
            this.lastNeedsTick = ticksGame >= 0 ? ticksGame : -1;
        }
    }
}
