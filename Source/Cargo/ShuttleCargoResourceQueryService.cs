using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Transactions;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo
{
    internal sealed class ShuttleCargoResourceQueryService
    {
        private readonly ThingWithComps host;
        private readonly IShuttleCargoBackend cargoBackend;

        internal ShuttleCargoResourceQueryService(
            ThingWithComps host,
            IShuttleCargoBackend cargoBackend)
        {
            this.host = host;
            this.cargoBackend = cargoBackend;
        }

        internal ShuttleCargoInventorySnapshot BuildInventorySnapshot(int inventoryRevision)
        {
            int createdTick = Find.TickManager != null ? Find.TickManager.TicksGame : -1;
            Dictionary<ThingDef, int> countsByThingDef = new Dictionary<ThingDef, int>();
            Dictionary<string, int> countsByDefName = new Dictionary<string, int>();
            Dictionary<string, List<CargoStackRef>> refsByDefName =
                new Dictionary<string, List<CargoStackRef>>();
            float totalMassKg = 0f;
            int scannedStackCount = 0;
            bool includesRegularCargo = false;
            bool includesRefrigeratedCargo = false;

            List<CompTransporter> transporters = this.ResolveTransporters();
            for (int i = 0; i < transporters.Count; i++)
            {
                CompTransporter transporter = transporters[i];
                ThingOwner contents = GetContents(transporter);
                if (contents == null)
                {
                    continue;
                }

                includesRegularCargo = true;
                for (int j = 0; j < contents.Count; j++)
                {
                    Thing thing = contents[j];
                    scannedStackCount++;
                    this.AddInventoryThing(
                        countsByThingDef,
                        countsByDefName,
                        refsByDefName,
                        thing,
                        ShuttleCargoInventorySourceKind.RegularCargo,
                        i,
                        null,
                        ref totalMassKg);
                }
            }

            CompShuttleRefrigeratedCargoRegistry registry = this.host != null
                ? this.host.TryGetComp<CompShuttleRefrigeratedCargoRegistry>()
                : null;
            IReadOnlyList<RefrigeratedCargoRecord> records = registry != null ? registry.Records : null;
            if (records != null)
            {
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

                    includesRefrigeratedCargo = true;
                    for (int j = 0; j < contents.Count; j++)
                    {
                        Thing thing = contents[j];
                        scannedStackCount++;
                        this.AddInventoryThing(
                            countsByThingDef,
                            countsByDefName,
                            refsByDefName,
                            thing,
                            ShuttleCargoInventorySourceKind.RefrigeratedCargo,
                            i,
                            record.ModuleInstanceID,
                            ref totalMassKg);
                    }
                }
            }

            bool isAvailable = this.host != null && (includesRegularCargo || includesRefrigeratedCargo);
            string unavailableReason = isAvailable
                ? null
                : "No shuttle cargo holders are available for inventory scanning.";
            return new ShuttleCargoInventorySnapshot(
                inventoryRevision,
                createdTick,
                countsByThingDef,
                countsByDefName,
                refsByDefName,
                totalMassKg,
                includesRegularCargo,
                includesRefrigeratedCargo,
                scannedStackCount,
                isAvailable,
                unavailableReason);
        }

        internal List<CompTransporter> ResolveTransporters()
        {
            if (this.cargoBackend == null)
            {
                return new List<CompTransporter>();
            }

            List<CompTransporter> transporters = this.cargoBackend.ResolveTransportersForLaunch(this.host);
            return transporters ?? new List<CompTransporter>();
        }

        internal IReadOnlyList<ShuttleCargoOrdinaryDepositTargetSnapshot>
            BuildOrdinaryDepositTargets()
        {
            List<ShuttleCargoOrdinaryDepositTargetSnapshot> targets =
                new List<ShuttleCargoOrdinaryDepositTargetSnapshot>();
            List<CompTransporter> transporters = this.ResolveTransporters();
            for (int i = 0; i < transporters.Count; i++)
            {
                CompTransporter transporter = transporters[i];
                if (transporter == null || GetContents(transporter) == null)
                {
                    continue;
                }

                targets.Add(new ShuttleCargoOrdinaryDepositTargetSnapshot(
                    i,
                    transporter.MassCapacity - transporter.MassUsage));
            }

            return targets;
        }

        internal static ThingOwner GetContents(CompTransporter transporter)
        {
            if (transporter == null)
            {
                return null;
            }

            return transporter.GetDirectlyHeldThings();
        }

        private void AddInventoryThing(
            Dictionary<ThingDef, int> countsByThingDef,
            Dictionary<string, int> countsByDefName,
            Dictionary<string, List<CargoStackRef>> refsByDefName,
            Thing thing,
            ShuttleCargoInventorySourceKind sourceKind,
            int sourceIndex,
            string moduleInstanceID,
            ref float totalMassKg)
        {
            if (!ShuttleCargoResourceMatcher.MatchesAvailableThing(thing))
            {
                return;
            }

            string defName = ShuttleCargoResourceMatcher.GetThingDefName(thing.def);
            if (string.IsNullOrEmpty(defName))
            {
                return;
            }

            int existingThingDefCount;
            countsByThingDef.TryGetValue(thing.def, out existingThingDefCount);
            countsByThingDef[thing.def] = existingThingDefCount + thing.stackCount;

            int existingDefNameCount;
            countsByDefName.TryGetValue(defName, out existingDefNameCount);
            countsByDefName[defName] = existingDefNameCount + thing.stackCount;

            List<CargoStackRef> refs;
            if (!refsByDefName.TryGetValue(defName, out refs))
            {
                refs = new List<CargoStackRef>();
                refsByDefName[defName] = refs;
            }

            refs.Add(new CargoStackRef(
                thing.thingIDNumber,
                defName,
                thing.stackCount,
                sourceKind,
                sourceIndex,
                moduleInstanceID));
            totalMassKg += CargoDisplayUtility.GetThingMass(thing, thing.stackCount);
        }
    }
}
