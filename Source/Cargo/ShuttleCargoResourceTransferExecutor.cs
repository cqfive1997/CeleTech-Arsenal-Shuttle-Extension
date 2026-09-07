using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo
{
    internal sealed class ShuttleCargoResourceTransferExecutor
    {
        private readonly ThingWithComps host;
        private readonly ShuttleCargoResourceQueryService queryService;
        private readonly Action invalidateInventorySnapshot;

        internal ShuttleCargoResourceTransferExecutor(
            ThingWithComps host,
            ShuttleCargoResourceQueryService queryService,
            Action invalidateInventorySnapshot)
        {
            this.host = host;
            this.queryService = queryService;
            this.invalidateInventorySnapshot = invalidateInventorySnapshot;
        }

        internal bool TryTakeMatchingThings(
            ThingDef thingDef,
            int count,
            List<CargoTakeRecord> takenThings,
            out int remaining)
        {
            remaining = count;
            List<CompTransporter> transporters = this.ResolveTransporters();
            for (int i = 0; i < transporters.Count && remaining > 0; i++)
            {
                CompTransporter transporter = transporters[i];
                ThingOwner contents = ShuttleCargoResourceQueryService.GetContents(transporter);
                if (contents == null)
                {
                    continue;
                }

                for (int j = contents.Count - 1; j >= 0 && remaining > 0; j--)
                {
                    Thing thing = contents[j];
                    if (!ShuttleCargoResourceMatcher.Matches(thing, thingDef))
                    {
                        continue;
                    }

                    int takeCount = thing.stackCount < remaining ? thing.stackCount : remaining;
                    Thing taken = contents.Take(thing, takeCount);
                    if (taken == null)
                    {
                        return false;
                    }

                    this.invalidateInventorySnapshot();
                    transporter.Notify_ThingRemoved(taken);
                    takenThings.Add(new CargoTakeRecord(transporter, contents, taken));
                    remaining -= taken.stackCount;
                }
            }

            if (remaining <= 0)
            {
                return true;
            }

            CompShuttleRefrigeratedCargoRegistry registry = this.host != null
                ? this.host.TryGetComp<CompShuttleRefrigeratedCargoRegistry>()
                : null;
            IReadOnlyList<RefrigeratedCargoRecord> records = registry != null ? registry.Records : null;
            if (records == null)
            {
                return true;
            }

            for (int i = 0; i < records.Count && remaining > 0; i++)
            {
                RefrigeratedCargoRecord record = records[i];
                if (record == null || !record.CoolingActive)
                {
                    continue;
                }

                ThingOwner contents = record.GetDirectlyHeldThings();
                if (contents == null)
                {
                    continue;
                }

                for (int j = contents.Count - 1; j >= 0 && remaining > 0; j--)
                {
                    Thing thing = contents[j];
                    if (!ShuttleCargoResourceMatcher.Matches(thing, thingDef))
                    {
                        continue;
                    }

                    int takeCount = thing.stackCount < remaining ? thing.stackCount : remaining;
                    Thing taken = contents.Take(thing, takeCount);
                    if (taken == null)
                    {
                        return false;
                    }

                    this.invalidateInventorySnapshot();
                    takenThings.Add(new CargoTakeRecord(null, contents, taken));
                    remaining -= taken.stackCount;
                }
            }

            return true;
        }

        internal bool TryTakeFirstMatchingThing(
            Predicate<Thing> matcher,
            out CargoTakeRecord takeRecord)
        {
            takeRecord = null;
            if (matcher == null)
            {
                return false;
            }

            List<CompTransporter> transporters = this.ResolveTransporters();
            for (int i = 0; i < transporters.Count; i++)
            {
                CompTransporter transporter = transporters[i];
                ThingOwner contents = ShuttleCargoResourceQueryService.GetContents(transporter);
                if (contents == null)
                {
                    continue;
                }

                for (int j = contents.Count - 1; j >= 0; j--)
                {
                    Thing thing = contents[j];
                    if (!ShuttleCargoResourceMatcher.MatchesAvailableThing(thing) || !matcher(thing))
                    {
                        continue;
                    }

                    Thing taken = contents.Take(thing, thing.stackCount);
                    if (taken == null)
                    {
                        return false;
                    }

                    this.invalidateInventorySnapshot();
                    transporter.Notify_ThingRemoved(taken);
                    takeRecord = new CargoTakeRecord(transporter, contents, taken);
                    return true;
                }
            }

            CompShuttleRefrigeratedCargoRegistry registry = this.host != null
                ? this.host.TryGetComp<CompShuttleRefrigeratedCargoRegistry>()
                : null;
            IReadOnlyList<RefrigeratedCargoRecord> records = registry != null ? registry.Records : null;
            if (records == null)
            {
                return true;
            }

            for (int i = 0; i < records.Count; i++)
            {
                RefrigeratedCargoRecord record = records[i];
                if (record == null || !record.CoolingActive)
                {
                    continue;
                }

                ThingOwner contents = record.GetDirectlyHeldThings();
                if (contents == null)
                {
                    continue;
                }

                for (int j = contents.Count - 1; j >= 0; j--)
                {
                    Thing thing = contents[j];
                    if (!ShuttleCargoResourceMatcher.MatchesAvailableThing(thing) || !matcher(thing))
                    {
                        continue;
                    }

                    Thing taken = contents.Take(thing, thing.stackCount);
                    if (taken == null)
                    {
                        return false;
                    }

                    this.invalidateInventorySnapshot();
                    takeRecord = new CargoTakeRecord(null, contents, taken);
                    return true;
                }
            }

            return true;
        }

        internal bool TryTakeExactThing(
            CargoStackRef stackRef,
            int count,
            out CargoTakeRecord takeRecord,
            out string failureReason)
        {
            takeRecord = null;
            failureReason = null;
            if (stackRef == null || stackRef.ThingIDNumber <= 0 || count <= 0)
            {
                failureReason = "Cargo stack identity or requested count is invalid.";
                return false;
            }

            if (stackRef.SourceKind == ShuttleCargoInventorySourceKind.RegularCargo)
            {
                List<CompTransporter> transporters = this.ResolveTransporters();
                if (stackRef.SourceIndex < 0 || stackRef.SourceIndex >= transporters.Count)
                {
                    failureReason = "Regular cargo source index is no longer available.";
                    return false;
                }

                CompTransporter transporter = transporters[stackRef.SourceIndex];
                ThingOwner contents = ShuttleCargoResourceQueryService.GetContents(transporter);
                return this.TryTakeExactFromOwner(
                    stackRef,
                    count,
                    transporter,
                    contents,
                    out takeRecord,
                    out failureReason);
            }

            CompShuttleRefrigeratedCargoRegistry registry = this.host != null
                ? this.host.TryGetComp<CompShuttleRefrigeratedCargoRegistry>()
                : null;
            IReadOnlyList<RefrigeratedCargoRecord> records = registry != null
                ? registry.Records
                : null;
            if (records == null || stackRef.SourceIndex < 0 || stackRef.SourceIndex >= records.Count)
            {
                failureReason = "Refrigerated cargo source index is no longer available.";
                return false;
            }

            RefrigeratedCargoRecord record = records[stackRef.SourceIndex];
            if (record == null || !record.CoolingActive ||
                (!string.IsNullOrEmpty(stackRef.ModuleInstanceID) &&
                    !string.Equals(
                        record.ModuleInstanceID,
                        stackRef.ModuleInstanceID,
                        StringComparison.Ordinal)))
            {
                failureReason = "Refrigerated cargo source is inactive or no longer matches.";
                return false;
            }

            return this.TryTakeExactFromOwner(
                stackRef,
                count,
                null,
                record.GetDirectlyHeldThings(),
                out takeRecord,
                out failureReason);
        }

        private bool TryTakeExactFromOwner(
            CargoStackRef stackRef,
            int count,
            CompTransporter transporter,
            ThingOwner contents,
            out CargoTakeRecord takeRecord,
            out string failureReason)
        {
            takeRecord = null;
            failureReason = null;
            if (contents == null)
            {
                failureReason = "Cargo source holder is unavailable.";
                return false;
            }

            Thing source = null;
            for (int i = 0; i < contents.Count; i++)
            {
                Thing thing = contents[i];
                if (thing != null &&
                    thing.thingIDNumber == stackRef.ThingIDNumber &&
                    thing.def != null &&
                    string.Equals(thing.def.defName, stackRef.DefName, StringComparison.Ordinal))
                {
                    source = thing;
                    break;
                }
            }

            if (!ShuttleCargoResourceMatcher.MatchesAvailableThing(source) ||
                source.stackCount < count)
            {
                failureReason = "Cargo stack is unavailable or no longer has the requested count.";
                return false;
            }

            Thing taken = contents.Take(source, count);
            if (taken == null || taken.stackCount != count)
            {
                failureReason = "Cargo holder failed to take the exact requested count.";
                return false;
            }

            this.invalidateInventorySnapshot();
            if (transporter != null)
            {
                transporter.Notify_ThingRemoved(taken);
            }

            takeRecord = new CargoTakeRecord(transporter, contents, taken);
            return true;
        }

        private List<CompTransporter> ResolveTransporters()
        {
            return this.queryService != null
                ? this.queryService.ResolveTransporters()
                : new List<CompTransporter>();
        }
    }
}
