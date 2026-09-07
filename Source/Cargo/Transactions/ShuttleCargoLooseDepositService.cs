using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo.Transactions
{
    /// <summary>
    /// Atomically deposits caller-created loose Things into planned ordinary transporters.
    /// Destination holders and rollback bookkeeping remain private to Cargo.
    /// </summary>
    internal sealed class ShuttleCargoLooseDepositService
    {
        private const float MassEpsilon = 0.0001f;

        private readonly ShuttleCargoResourceQueryService queryService;
        private readonly ShuttleCargoAccessPolicy accessPolicy;
        private readonly Action invalidateInventorySnapshot;

        internal ShuttleCargoLooseDepositService(
            ShuttleCargoResourceQueryService queryService,
            ShuttleCargoAccessPolicy accessPolicy,
            Action invalidateInventorySnapshot)
        {
            this.queryService = queryService;
            this.accessPolicy = accessPolicy;
            this.invalidateInventorySnapshot = invalidateInventorySnapshot;
        }

        internal bool TryDeposit(
            IReadOnlyList<ShuttleCargoLooseDepositEntry> entries,
            ShuttleCargoAccessRequirement accessRequirement,
            string reason,
            out int depositedCount,
            out string failureReason)
        {
            depositedCount = 0;
            failureReason = null;
            if (this.accessPolicy == null ||
                !this.accessPolicy.CanDeposit(accessRequirement, out failureReason))
            {
                return false;
            }

            if (entries == null || entries.Count == 0 || this.queryService == null)
            {
                failureReason = "Cargo deposit entries or query service are missing.";
                return false;
            }

            List<CompTransporter> transporters = this.queryService.ResolveTransporters();
            List<DepositRecord> depositedThings = new List<DepositRecord>();
            for (int i = 0; i < entries.Count; i++)
            {
                ShuttleCargoLooseDepositEntry entry = entries[i];
                Thing thing = entry != null ? entry.Thing : null;
                if (!this.IsValidLooseEntry(entry, thing, transporters))
                {
                    return this.FailWithRollback(
                        depositedThings,
                        null,
                        "Cargo deposit entry became invalid. reason=" + (reason ?? "null"),
                        out depositedCount,
                        out failureReason);
                }

                CompTransporter transporter = transporters[entry.TransporterIndex];
                ThingOwner destination =
                    ShuttleCargoResourceQueryService.GetContents(transporter);
                float thingMassKg = CargoDisplayUtility.GetThingMass(
                    thing,
                    thing.stackCount);
                bool hasCapacity = transporter != null &&
                    transporter.MassCapacity - transporter.MassUsage + MassEpsilon >=
                        thingMassKg;
                if (destination == null || !hasCapacity ||
                    !destination.TryAdd(thing, false))
                {
                    return this.FailWithRollback(
                        depositedThings,
                        thing,
                        "Cargo deposit destination rejected a loose Thing. reason=" +
                            (reason ?? "null"),
                        out depositedCount,
                        out failureReason);
                }

                if (!destination.Contains(thing))
                {
                    if (thing.holdingOwner == destination)
                    {
                        transporter.Notify_ThingAdded(thing);
                        depositedThings.Add(new DepositRecord(
                            transporter,
                            destination,
                            thing));
                        this.NotifyCargoChanged();
                    }

                    return this.FailWithRollback(
                        depositedThings,
                        thing,
                        "Cargo deposit did not preserve loose Thing identity. reason=" +
                            (reason ?? "null"),
                        out depositedCount,
                        out failureReason);
                }

                transporter.Notify_ThingAdded(thing);
                depositedThings.Add(new DepositRecord(
                    transporter,
                    destination,
                    thing));
                depositedCount += thing.stackCount;
                this.NotifyCargoChanged();
            }

            return true;
        }

        private bool IsValidLooseEntry(
            ShuttleCargoLooseDepositEntry entry,
            Thing thing,
            List<CompTransporter> transporters)
        {
            return entry != null &&
                transporters != null &&
                entry.TransporterIndex >= 0 &&
                entry.TransporterIndex < transporters.Count &&
                ShuttleCargoResourceMatcher.MatchesAvailableThing(thing) &&
                thing.holdingOwner == null &&
                !thing.Spawned;
        }

        private bool FailWithRollback(
            List<DepositRecord> depositedThings,
            Thing currentThing,
            string failure,
            out int depositedCount,
            out string failureReason)
        {
            string rollbackNotice;
            bool rollbackSafe = this.RollBack(depositedThings, out rollbackNotice);
            if (currentThing != null &&
                !currentThing.Destroyed &&
                (currentThing.holdingOwner != null || currentThing.Spawned))
            {
                rollbackSafe = false;
                rollbackNotice = AppendNote(
                    rollbackNotice,
                    "current deposit Thing remains owned after rollback");
            }

            depositedCount = 0;
            failureReason = failure + " rollback=" +
                (rollbackNotice ?? "not-required");
            if (!rollbackSafe)
            {
                failureReason = "Cargo deposit cleanup failed. " + failureReason;
            }

            return false;
        }

        private bool RollBack(
            List<DepositRecord> depositedThings,
            out string rollbackNotice)
        {
            rollbackNotice = null;
            bool success = true;
            for (int i = depositedThings != null ? depositedThings.Count - 1 : -1;
                i >= 0;
                i--)
            {
                DepositRecord record = depositedThings[i];
                if (record == null || record.Thing == null || record.Thing.Destroyed)
                {
                    continue;
                }

                if (record.Thing.holdingOwner != record.DestinationContents)
                {
                    rollbackNotice = AppendNote(
                        rollbackNotice,
                        "deposit Thing left the planned destination");
                    success = false;
                    continue;
                }

                Thing removed = record.DestinationContents.Take(
                    record.Thing,
                    record.Thing.stackCount);
                if (removed == null)
                {
                    rollbackNotice = AppendNote(
                        rollbackNotice,
                        "failed to remove " + Describe(record.Thing));
                    success = false;
                    continue;
                }

                record.DestinationTransporter.Notify_ThingRemoved(removed);
                this.NotifyCargoChanged();
                rollbackNotice = AppendNote(
                    rollbackNotice,
                    "removed " + Describe(removed));
            }

            return success;
        }

        private void NotifyCargoChanged()
        {
            if (this.invalidateInventorySnapshot != null)
            {
                this.invalidateInventorySnapshot();
            }
        }

        private static string Describe(Thing thing)
        {
            return thing == null
                ? "null"
                : (thing.def != null ? thing.def.defName : "unknown") +
                    " x" + thing.stackCount;
        }

        private static string AppendNote(string existing, string note)
        {
            if (string.IsNullOrEmpty(note))
            {
                return existing;
            }

            return string.IsNullOrEmpty(existing)
                ? note
                : existing + "; " + note;
        }

        private sealed class DepositRecord
        {
            internal DepositRecord(
                CompTransporter destinationTransporter,
                ThingOwner destinationContents,
                Thing thing)
            {
                this.DestinationTransporter = destinationTransporter;
                this.DestinationContents = destinationContents;
                this.Thing = thing;
            }

            internal CompTransporter DestinationTransporter { get; private set; }
            internal ThingOwner DestinationContents { get; private set; }
            internal Thing Thing { get; private set; }
        }
    }
}
