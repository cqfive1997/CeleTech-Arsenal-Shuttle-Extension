using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo
{
    internal sealed class CargoTransferFilter
    {
        private readonly VanillaTransporterAdapter transporterAdapter;
        private readonly ShuttleLoadCandidateProvider candidateProvider;

        public CargoTransferFilter(VanillaTransporterAdapter transporterAdapter)
        {
            this.transporterAdapter = transporterAdapter;
            this.candidateProvider = new ShuttleLoadCandidateProvider();
        }

        public List<TransferableOneWay> BuildPassengerTransferables(
            ThingWithComps host,
            ShuttleProfile profile,
            ShuttleCargoRegionConfigState cargoRegionConfig)
        {
            List<TransferableOneWay> transferables = new List<TransferableOneWay>();
            Map map = host != null ? host.Map : null;
            List<CompTransporter> transporters = this.transporterAdapter.ResolveTransportersForLaunch(host);
            int activeRegionCount = this.GetActiveCargoRegionCount(profile);
            if (map == null || transporters.Count == 0 || cargoRegionConfig == null || activeRegionCount <= 0)
            {
                return transferables;
            }

            List<Pawn> passengerCandidates = this.candidateProvider.GetPassengerCandidates(
                transporters,
                map);
            for (int i = 0; i < passengerCandidates.Count; i++)
            {
                Pawn pawn = passengerCandidates[i];
                this.TryAddTransferable(pawn, transferables, cargoRegionConfig, activeRegionCount, null);
            }

            return transferables;
        }

        public List<TransferableOneWay> BuildCargoTransferables(
            ThingWithComps host,
            ShuttleProfile profile,
            ShuttleCargoRegionConfigState cargoRegionConfig,
            ShuttleCargoLoadAdmissionResolver admissionResolver)
        {
            List<TransferableOneWay> transferables = new List<TransferableOneWay>();
            Map map = host != null ? host.Map : null;
            List<CompTransporter> transporters = this.transporterAdapter.ResolveTransportersForLaunch(host);
            int activeRegionCount = this.GetActiveCargoRegionCount(profile);
            bool canUseRegularFilter = cargoRegionConfig != null && activeRegionCount > 0;
            bool canUseColdAdmission = admissionResolver != null &&
                admissionResolver.HasAnyRefrigeratedAutoTransferDestination;
            if (map == null || transporters.Count == 0 || (!canUseRegularFilter && !canUseColdAdmission))
            {
                return transferables;
            }

            List<Thing> cargoCandidates = this.candidateProvider.GetCargoCandidates(
                transporters,
                map);
            for (int i = 0; i < cargoCandidates.Count; i++)
            {
                Thing thing = cargoCandidates[i];
                this.TryAddTransferable(
                    thing,
                    transferables,
                    cargoRegionConfig,
                    activeRegionCount,
                    admissionResolver);
            }

            return transferables;
        }

        public int SelectedCount(List<TransferableOneWay> transferables)
        {
            int count = 0;
            if (transferables == null)
            {
                return count;
            }

            for (int i = 0; i < transferables.Count; i++)
            {
                TransferableOneWay transferable = transferables[i];
                if (transferable != null)
                {
                    count += transferable.CountToTransfer;
                }
            }

            return count;
        }

        public float SelectedMass(List<TransferableOneWay> transferables)
        {
            float mass = 0f;
            if (transferables == null)
            {
                return mass;
            }

            for (int i = 0; i < transferables.Count; i++)
            {
                TransferableOneWay transferable = transferables[i];
                if (transferable == null || transferable.CountToTransfer <= 0 || transferable.AnyThing == null)
                {
                    continue;
                }

                mass += transferable.AnyThing.GetStatValue(StatDefOf.Mass, true, -1) * transferable.CountToTransfer;
            }

            return mass;
        }

        public List<Pawn> GetSelectedPawns(List<TransferableOneWay> transferables)
        {
            List<Pawn> pawns = new List<Pawn>();
            if (transferables == null)
            {
                return pawns;
            }

            for (int i = 0; i < transferables.Count; i++)
            {
                TransferableOneWay transferable = transferables[i];
                if (transferable == null || transferable.CountToTransfer <= 0 || transferable.things == null)
                {
                    continue;
                }

                int remaining = transferable.CountToTransfer;
                for (int j = 0; j < transferable.things.Count && remaining > 0; j++)
                {
                    Pawn pawn = transferable.things[j] as Pawn;
                    if (pawn == null)
                    {
                        continue;
                    }

                    pawns.Add(pawn);
                    remaining--;
                }
            }

            return pawns;
        }

        public void AddSelectionsToLoadList(CompTransporter targetTransporter, List<TransferableOneWay> transferables)
        {
            if (targetTransporter == null || transferables == null)
            {
                return;
            }

            for (int i = 0; i < transferables.Count; i++)
            {
                TransferableOneWay transferable = transferables[i];
                if (transferable != null && transferable.CountToTransfer > 0)
                {
                    TransferableOneWay loadQueueTransferable = CopyTransferableForLoadQueue(transferable);
                    if (loadQueueTransferable != null && loadQueueTransferable.CountToTransfer > 0)
                    {
                        targetTransporter.AddToTheToLoadList(
                            loadQueueTransferable,
                            loadQueueTransferable.CountToTransfer);
                    }
                }
            }
        }

        private static TransferableOneWay CopyTransferableForLoadQueue(TransferableOneWay source)
        {
            if (source == null || source.CountToTransfer <= 0)
            {
                return null;
            }

            TransferableOneWay copy = new TransferableOneWay();
            if (source.things != null)
            {
                copy.things.AddRange(source.things);
            }

            copy.AdjustTo(source.CountToTransfer);
            return copy;
        }

        private void TryAddTransferable(
            Thing thing,
            List<TransferableOneWay> targetList,
            ShuttleCargoRegionConfigState cargoRegionConfig,
            int activeRegionCount,
            ShuttleCargoLoadAdmissionResolver admissionResolver)
        {
            if (thing == null || targetList == null)
            {
                return;
            }

            bool accepted;
            if (admissionResolver != null)
            {
                ShuttleCargoLoadAdmissionResult admission = admissionResolver.Resolve(thing);
                accepted = admission != null && admission.Accepted;
            }
            else
            {
                accepted = cargoRegionConfig != null &&
                    cargoRegionConfig.AllowsAnyActiveRegion(thing, activeRegionCount);
            }

            if (!accepted)
            {
                return;
            }

            TransferableOneWay transferable = FindExistingTransferableGroup(
                thing,
                targetList);

            if (transferable == null)
            {
                transferable = new TransferableOneWay();
                targetList.Add(transferable);
            }

            transferable.things.Add(thing);
        }

        private static TransferableOneWay FindExistingTransferableGroup(
            Thing thing,
            List<TransferableOneWay> targetList)
        {
            if (thing == null || targetList == null)
            {
                return null;
            }

            // Desperate matching falls back to same-def grouping after strict matching
            // fails. Load planning must retain strict quality, stuff, gene, hit-point,
            // and component stack semantics instead of hiding incompatible item instances.
            // Match vanilla transporter behavior: stackLimit does not define row identity.
            return TransferableUtility.TransferableMatching<TransferableOneWay>(
                thing,
                targetList,
                TransferAsOneMode.PodsOrCaravanPacking);
        }

        private int GetActiveCargoRegionCount(ShuttleProfile profile)
        {
            if (profile == null || profile.Cargo == null)
            {
                return 0;
            }

            return (int)profile.Cargo.CargoRegionCount;
        }
    }
}
