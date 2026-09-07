using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Bridges;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo
{
    internal sealed class CargoSnapshotBuilder
    {
        private readonly VanillaTransporterAdapter transporterAdapter;

        public CargoSnapshotBuilder(VanillaTransporterAdapter transporterAdapter)
        {
            this.transporterAdapter = transporterAdapter;
        }

        public ShuttleCargoSnapshot Build(
            ThingWithComps host,
            ShuttleProfile profile,
            ShuttleCargoRegionConfigState cargoRegionConfig)
        {
            ShuttleCargoSnapshot snapshot = new ShuttleCargoSnapshot();
            if (profile != null && profile.Cargo != null)
            {
                snapshot.CargoRegionCount = (int)profile.Cargo.CargoRegionCount;
                snapshot.MassCapacity = profile.Cargo.CargoMassCapacityKg > 0f
                    ? profile.Cargo.CargoMassCapacityKg
                    : 0f;
            }

            bool hasGrossMassProfile = profile != null && profile.Mass != null;
            if (hasGrossMassProfile)
            {
                snapshot.StructuralMass = profile.Mass.TotalMass;
            }

            bool hasCargoProfile = profile != null && profile.Cargo != null;
            List<CompTransporter> transporters = this.transporterAdapter.ResolveTransportersForLaunch(host);
            snapshot.HasTransporter = transporters.Count > 0;
            if (!snapshot.HasTransporter)
            {
                return snapshot;
            }

            for (int transporterIndex = 0; transporterIndex < transporters.Count; transporterIndex++)
            {
                CompTransporter currentTransporter = transporters[transporterIndex];
                if (currentTransporter == null)
                {
                    continue;
                }

                if (!hasCargoProfile)
                {
                    snapshot.MassCapacity += currentTransporter.MassCapacity;
                }

                if (currentTransporter.innerContainer != null)
                {
                    for (int loadedIndex = 0; loadedIndex < currentTransporter.innerContainer.Count; loadedIndex++)
                    {
                        Thing thing = currentTransporter.innerContainer[loadedIndex];
                        this.AddThingToSnapshot(
                            snapshot,
                            cargoRegionConfig,
                            thing,
                            thing != null ? thing.stackCount : 0,
                            true,
                            false,
                            transporterIndex,
                            loadedIndex,
                            -1);
                    }
                }

                if (currentTransporter.leftToLoad != null)
                {
                    for (int i = 0; i < currentTransporter.leftToLoad.Count; i++)
                    {
                        TransferableOneWay transferable = currentTransporter.leftToLoad[i];
                        if (transferable == null || transferable.CountToTransfer <= 0 || transferable.AnyThing == null)
                        {
                            continue;
                        }

                        this.AddThingToSnapshot(
                            snapshot,
                            cargoRegionConfig,
                            transferable.AnyThing,
                            transferable.CountToTransfer,
                            false,
                            true,
                            transporterIndex,
                            -1,
                            i);
                    }
                }
            }

            snapshot.TotalPlannedMassKg = snapshot.LoadedMassKg + snapshot.QueuedMassKg;
            snapshot.CargoMassUsage = snapshot.TotalPlannedMassKg;
            snapshot.MassUsage = snapshot.StructuralMass + snapshot.CargoMassUsage;
            return snapshot;
        }

        private void AddThingToSnapshot(
            ShuttleCargoSnapshot snapshot,
            ShuttleCargoRegionConfigState cargoRegionConfig,
            Thing thing,
            int stackCount,
            bool isLoaded,
            bool isAssignedToLoad,
            int transporterIndex,
            int loadedIndex,
            int queueIndex)
        {
            if (snapshot == null || thing == null)
            {
                return;
            }

            if (stackCount <= 0)
            {
                stackCount = thing.stackCount;
            }

            ShuttleCargoItemSnapshot item = new ShuttleCargoItemSnapshot();
            item.Label = CargoDisplayUtility.GetDisplayLabel(thing);
            item.DefName = thing.def != null ? thing.def.defName : null;
            item.Category = CargoDisplayUtility.GetDisplayCategory(thing);
            // Display assignment is deterministic across currently allowed cargo bays.
            item.CargoRegionIndex = cargoRegionConfig != null
                ? cargoRegionConfig.FindStableAllowedRegion(thing, snapshot.CargoRegionCount)
                : -1;
            item.TransporterIndex = transporterIndex;
            item.LoadedIndex = loadedIndex;
            item.QueueIndex = queueIndex;
            item.ThingIDNumber = thing.thingIDNumber;
            item.StackCount = stackCount;
            item.Mass = CargoDisplayUtility.GetThingMass(thing, stackCount);
            item.IsLoaded = isLoaded;
            item.IsAssignedToLoad = isAssignedToLoad;
            item.IsPawn = thing is Pawn;
            item.DisplayThing = thing;

            if (item.CargoRegionIndex < 0)
            {
                snapshot.BlockedStackCount++;
            }

            snapshot.Items.Add(item);
            snapshot.StackCount++;
            snapshot.ThingCount += stackCount;

            if (isLoaded)
            {
                snapshot.LoadedStackCount++;
                snapshot.LoadedThingCount += stackCount;
                snapshot.LoadedMassKg += item.Mass;
            }

            if (isAssignedToLoad)
            {
                snapshot.AssignedStackCount++;
                snapshot.AssignedThingCount += stackCount;
                snapshot.QueuedMassKg += item.Mass;
            }
        }
    }
}
