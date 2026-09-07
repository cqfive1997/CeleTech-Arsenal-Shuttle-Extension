using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Launch
{
    /// <summary>
    /// Normalizes flight ownership for unequipped passenger weapons. Equipped Things are not
    /// stored in Pawn_InventoryTracker and are therefore outside this transaction.
    /// </summary>
    internal static class ShuttlePassengerWeaponCargoTransfer
    {
        internal static bool TryMoveFromTransporter(
            ActiveTransporterInfo transporter,
            out int movedWeaponCount,
            out string failureReason)
        {
            movedWeaponCount = 0;
            failureReason = null;
            if (transporter == null || transporter.innerContainer == null)
            {
                failureReason = "Passenger weapon cargo transfer has no flight container.";
                return false;
            }

            List<Pawn> pawns = new List<Pawn>();
            for (int i = 0; i < transporter.innerContainer.Count; i++)
            {
                Pawn pawn = transporter.innerContainer[i] as Pawn;
                if (pawn != null)
                {
                    pawns.Add(pawn);
                }
            }

            TransferReceipt receipt;
            return TryMoveFromPawns(
                pawns,
                transporter.innerContainer,
                out receipt,
                out movedWeaponCount,
                out failureReason);
        }

        internal static bool TryMoveFromPawns(
            IEnumerable<Pawn> pawns,
            ThingOwner destination,
            out TransferReceipt receipt,
            out int movedWeaponCount,
            out string failureReason)
        {
            receipt = null;
            movedWeaponCount = 0;
            failureReason = null;
            if (destination == null)
            {
                failureReason = "Passenger weapon cargo transfer destination is unavailable.";
                return false;
            }

            List<TransferCandidate> candidates = CollectCandidates(pawns);
            if (candidates.Count == 0)
            {
                return true;
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                TransferCandidate candidate = candidates[i];
                if (!IsValidCandidate(candidate) || !candidate.Source.Contains(candidate.Thing))
                {
                    failureReason = "Passenger inventory weapon is not transferable during preflight.";
                    return false;
                }
            }

            List<TransferCandidate> moved = new List<TransferCandidate>();
            try
            {
                for (int i = 0; i < candidates.Count; i++)
                {
                    TransferCandidate candidate = candidates[i];
                    Thing thing = candidate.Thing;
                    ThingOwner source = candidate.Source;
                    if (!IsValidCandidate(candidate) || !source.Contains(thing))
                    {
                        failureReason = "Passenger inventory weapon changed before flight cargo transfer.";
                        AppendRollbackFailure(destination, moved, ref failureReason);
                        return false;
                    }

                    int expectedCount = thing.stackCount;
                    int transferredCount = source.TryTransferToContainer(
                        thing,
                        destination,
                        expectedCount,
                        false);
                    if (transferredCount > 0)
                    {
                        moved.Add(candidate);
                    }

                    if (transferredCount != expectedCount ||
                        source.Contains(thing) ||
                        !destination.Contains(thing))
                    {
                        failureReason = "Passenger inventory weapon transfer did not satisfy ownership postconditions. thing=" +
                            Describe(thing) +
                            " expected=" + expectedCount +
                            " moved=" + transferredCount + ".";
                        AppendRollbackFailure(destination, moved, ref failureReason);
                        return false;
                    }

                    movedWeaponCount++;
                }
            }
            catch (Exception exception)
            {
                failureReason = "Passenger inventory weapon transfer threw " +
                    exception.GetType().Name + ".";
                AppendRollbackFailure(destination, moved, ref failureReason);
                return false;
            }

            receipt = new TransferReceipt(destination, moved);
            return true;
        }

        private static List<TransferCandidate> CollectCandidates(IEnumerable<Pawn> pawns)
        {
            List<TransferCandidate> candidates = new List<TransferCandidate>();
            HashSet<int> seenThingIds = new HashSet<int>();
            if (pawns == null)
            {
                return candidates;
            }

            foreach (Pawn pawn in pawns)
            {
                ThingOwner inventory = pawn != null && pawn.inventory != null
                    ? pawn.inventory.innerContainer
                    : null;
                if (inventory == null)
                {
                    continue;
                }

                for (int i = 0; i < inventory.Count; i++)
                {
                    Thing thing = inventory[i];
                    if (!IsUnequippedWeapon(thing) || !seenThingIds.Add(thing.thingIDNumber))
                    {
                        continue;
                    }

                    candidates.Add(new TransferCandidate(pawn, inventory, thing));
                }
            }

            return candidates;
        }

        private static bool IsValidCandidate(TransferCandidate candidate)
        {
            return candidate != null &&
                candidate.Pawn != null &&
                !candidate.Pawn.Destroyed &&
                candidate.Source != null &&
                IsUnequippedWeapon(candidate.Thing);
        }

        private static bool IsUnequippedWeapon(Thing thing)
        {
            return thing != null &&
                !thing.Destroyed &&
                thing.def != null &&
                thing.def.IsWeapon;
        }

        private static void AppendRollbackFailure(
            ThingOwner destination,
            List<TransferCandidate> moved,
            ref string failureReason)
        {
            string rollbackFailure;
            if (!TryRollback(destination, moved, out rollbackFailure))
            {
                failureReason = (failureReason ?? "Passenger weapon cargo transfer failed.") +
                    " Rollback fatal: " + rollbackFailure;
            }
        }

        private static bool TryRollback(
            ThingOwner destination,
            List<TransferCandidate> moved,
            out string failureReason)
        {
            failureReason = null;
            List<string> rollbackFailures = new List<string>();
            for (int i = moved.Count - 1; i >= 0; i--)
            {
                TransferCandidate candidate = moved[i];
                Thing thing = candidate != null ? candidate.Thing : null;
                ThingOwner source = candidate != null ? candidate.Source : null;
                if (thing == null || thing.Destroyed || source == null)
                {
                    rollbackFailures.Add("missing rollback candidate");
                    continue;
                }

                if (source.Contains(thing))
                {
                    continue;
                }

                if (destination == null || !destination.Contains(thing))
                {
                    rollbackFailures.Add("weapon has no rollback owner: " + Describe(thing));
                    continue;
                }

                try
                {
                    int expectedCount = thing.stackCount;
                    int restoredCount = destination.TryTransferToContainer(
                        thing,
                        source,
                        expectedCount,
                        false);
                    if (restoredCount != expectedCount ||
                        destination.Contains(thing) ||
                        !source.Contains(thing))
                    {
                        rollbackFailures.Add("rollback postcondition failed: " + Describe(thing));
                    }
                }
                catch (Exception exception)
                {
                    rollbackFailures.Add(
                        "rollback threw " + exception.GetType().Name + ": " + Describe(thing));
                }
            }

            if (rollbackFailures.Count > 0)
            {
                failureReason = string.Join(" | ", rollbackFailures.ToArray());
                return false;
            }

            return true;
        }

        private static string Describe(Thing thing)
        {
            return thing == null
                ? "null"
                : (thing.def != null ? thing.def.defName : "unknown") + "#" + thing.thingIDNumber;
        }

        internal sealed class TransferCandidate
        {
            internal TransferCandidate(Pawn pawn, ThingOwner source, Thing thing)
            {
                this.Pawn = pawn;
                this.Source = source;
                this.Thing = thing;
            }

            internal Pawn Pawn { get; private set; }
            internal ThingOwner Source { get; private set; }
            internal Thing Thing { get; private set; }
        }

        internal sealed class TransferReceipt
        {
            private ThingOwner destination;
            private List<TransferCandidate> moved;

            internal TransferReceipt(ThingOwner destination, List<TransferCandidate> moved)
            {
                this.destination = destination;
                this.moved = moved != null
                    ? new List<TransferCandidate>(moved)
                    : new List<TransferCandidate>();
            }

            internal bool TryRollback(out string failureReason)
            {
                if (this.moved == null)
                {
                    failureReason = null;
                    return true;
                }

                bool success = ShuttlePassengerWeaponCargoTransfer.TryRollback(
                    this.destination,
                    this.moved,
                    out failureReason);
                if (success)
                {
                    this.destination = null;
                    this.moved = null;
                }

                return success;
            }
        }
    }
}
