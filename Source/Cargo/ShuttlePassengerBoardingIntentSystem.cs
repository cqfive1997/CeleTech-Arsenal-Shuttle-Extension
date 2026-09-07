using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo
{
    /// <summary>
    /// Converts durable held-passenger intent into ordinary vanilla boarding only after a pawn
    /// naturally leaves its module holder.
    /// </summary>
    internal static class ShuttlePassengerBoardingIntentSystem
    {
        private const int ReconcileIntervalTicks = 60;

        internal static bool Tick(
            ThingWithComps host,
            ShuttleRuntimeState runtimeState,
            IShuttleCargoBackend cargoBackend,
            int ticksGame)
        {
            ShuttlePassengerBoardingIntentState state =
                runtimeState != null ? runtimeState.PassengerBoardingIntents : null;
            IShuttlePassengerBoardingBackend boardingBackend =
                cargoBackend as IShuttlePassengerBoardingBackend;
            if (host == null ||
                boardingBackend == null ||
                state == null ||
                !state.ShouldReconcile(ticksGame, ReconcileIntervalTicks))
            {
                return false;
            }

            state.RemoveInvalidRecords();
            if (!state.HasAny)
            {
                return false;
            }

            bool changed = false;
            ShuttleHeldPassengerSnapshot held = ShuttleHeldPassengerQuery.Build(host);
            IReadOnlyList<ShuttlePassengerBoardingIntentRecord> records =
                state.RecordsForReading;
            for (int i = 0; i < records.Count; i++)
            {
                ShuttlePassengerBoardingIntentRecord record = records[i];
                Pawn pawn = record != null ? record.Pawn : null;
                if (pawn == null || held.Contains(pawn))
                {
                    continue;
                }

                bool queuedNow;
                string failureReason;
                if (boardingBackend.EnsurePassengerQueued(
                    host,
                    pawn,
                    out queuedNow,
                    out failureReason))
                {
                    changed |= queuedNow;
                }
            }

            return changed;
        }

        internal static bool TryGetLaunchBlocker(
            ThingWithComps host,
            ShuttleRuntimeState runtimeState,
            IShuttleCargoBackend cargoBackend,
            out ShuttlePassengerBoardingIntentRecord blocker)
        {
            blocker = null;
            ShuttlePassengerBoardingIntentState state =
                runtimeState != null ? runtimeState.PassengerBoardingIntents : null;
            if (state == null || !state.HasAny)
            {
                return false;
            }

            ShuttleHeldPassengerSnapshot held = ShuttleHeldPassengerQuery.Build(host);
            IShuttlePassengerBoardingBackend boardingBackend =
                cargoBackend as IShuttlePassengerBoardingBackend;
            IReadOnlyList<ShuttlePassengerBoardingIntentRecord> records =
                state.RecordsForReading;
            for (int i = 0; i < records.Count; i++)
            {
                ShuttlePassengerBoardingIntentRecord record = records[i];
                Pawn pawn = record != null ? record.Pawn : null;
                if (record == null)
                {
                    continue;
                }

                if ((pawn != null && held.Contains(pawn)) ||
                    (pawn != null &&
                        boardingBackend != null &&
                        boardingBackend.ContainsPassenger(host, pawn)))
                {
                    continue;
                }

                blocker = record;
                return true;
            }

            return false;
        }
    }
}
