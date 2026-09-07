using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Medical;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo
{
    internal sealed class LoadCargoReadModelBuilder
    {
        private readonly VanillaTransporterAdapter transporterAdapter;
        private readonly CargoTransferFilter transferFilter;

        public LoadCargoReadModelBuilder(
            VanillaTransporterAdapter transporterAdapter,
            CargoTransferFilter transferFilter)
        {
            this.transporterAdapter = transporterAdapter;
            this.transferFilter = transferFilter;
        }

        public ShuttleLoadCargoReadModel Build(
            ThingWithComps host,
            ShuttleProfile profile,
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState)
        {
            ShuttleLoadCargoReadModel readModel = new ShuttleLoadCargoReadModel();
            if (host != null && host.Map != null)
            {
                readModel.MapTile = host.Map.Tile;
            }

            List<CompTransporter> transporters = this.transporterAdapter.ResolveTransportersForLaunch(host);
            readModel.HasTransporter = transporters.Count > 0;
            if (!readModel.HasTransporter)
            {
                return readModel;
            }

            ShuttleCargoRegionConfigState cargoRegionConfig = assemblyState != null
                ? assemblyState.CargoRegionConfig
                : null;
            ShuttleCargoLoadAdmissionResolver admissionResolver =
                new ShuttleCargoLoadAdmissionResolver(host, profile, assemblyState, runtimeState);
            readModel.PassengerTransferables = this.transferFilter.BuildPassengerTransferables(host, profile, cargoRegionConfig);
            this.AddHeldPassengerTransferables(readModel, host);
            readModel.CargoTransferables = this.transferFilter.BuildCargoTransferables(
                host,
                profile,
                cargoRegionConfig,
                admissionResolver);
            this.FillCargoAdmissionRows(readModel, admissionResolver);
            this.FillMedevacSummary(readModel, profile);
            readModel.ExistingMassUsageKg = this.transporterAdapter.ExistingMassUsage(transporters);
            readModel.MassCapacityKg = this.transporterAdapter.MassCapacity(transporters);
            readModel.RefrigeratedMassSharesOverallCapacity =
                admissionResolver.RefrigeratedMassSharesOverallCapacity;
            readModel.RefrigeratedAutoTransferCapacityKg = admissionResolver.RefrigeratedAutoTransferCapacityKg;
            readModel.RefrigeratedAutoTransferUsedMassKg = admissionResolver.RefrigeratedAutoTransferUsedMassKg;
            readModel.RefrigeratedAutoTransferAvailableMassKg = admissionResolver.RefrigeratedAutoTransferAvailableMassKg;
            readModel.RegularAvailableMassKg = readModel.MassCapacityKg -
                readModel.ExistingMassUsageKg;
            if (readModel.RefrigeratedMassSharesOverallCapacity)
            {
                readModel.RegularAvailableMassKg -=
                    readModel.RefrigeratedAutoTransferUsedMassKg;
            }

            if (readModel.RegularAvailableMassKg < 0f)
            {
                readModel.RegularAvailableMassKg = 0f;
            }

            if (readModel.RefrigeratedMassSharesOverallCapacity)
            {
                readModel.RefrigeratedAutoTransferAvailableMassKg =
                    readModel.RegularAvailableMassKg;
                readModel.AvailableMassKg = readModel.RegularAvailableMassKg;
            }
            else
            {
                readModel.AvailableMassKg = readModel.RegularAvailableMassKg +
                    readModel.RefrigeratedAutoTransferAvailableMassKg;
            }

            readModel.HasQueuedLoads =
                this.transporterAdapter.HasPendingLoadQueue(host) ||
                (runtimeState != null &&
                    runtimeState.PassengerBoardingIntents != null &&
                    runtimeState.PassengerBoardingIntents.HasAny);
            this.FillLoadCargoSummary(readModel, transporters);
            readModel.RefreshSearchCorpusKey();
            return readModel;
        }

        private void AddHeldPassengerTransferables(
            ShuttleLoadCargoReadModel readModel,
            ThingWithComps host)
        {
            if (readModel == null || readModel.PassengerTransferables == null)
            {
                return;
            }

            HashSet<int> existingThingIDs = new HashSet<int>();
            for (int i = 0; i < readModel.PassengerTransferables.Count; i++)
            {
                TransferableOneWay existing = readModel.PassengerTransferables[i];
                if (existing == null || existing.things == null)
                {
                    continue;
                }

                for (int j = 0; j < existing.things.Count; j++)
                {
                    Thing thing = existing.things[j];
                    if (thing != null)
                    {
                        existingThingIDs.Add(thing.thingIDNumber);
                    }
                }
            }

            ShuttleHeldPassengerSnapshot held = ShuttleHeldPassengerQuery.Build(host);
            IReadOnlyList<Pawn> heldPawns = held.Pawns;
            for (int i = 0; i < heldPawns.Count; i++)
            {
                Pawn pawn = heldPawns[i];
                if (pawn == null)
                {
                    continue;
                }

                readModel.HeldPassengerThingIDs.Add(pawn.thingIDNumber);
                if (!existingThingIDs.Add(pawn.thingIDNumber))
                {
                    continue;
                }

                TransferableOneWay transferable = new TransferableOneWay();
                transferable.things.Add(pawn);
                transferable.AdjustTo(1);
                readModel.PassengerTransferables.Add(transferable);
            }
        }

        private void FillCargoAdmissionRows(
            ShuttleLoadCargoReadModel readModel,
            ShuttleCargoLoadAdmissionResolver admissionResolver)
        {
            if (readModel == null ||
                readModel.CargoTransferables == null ||
                admissionResolver == null ||
                readModel.CargoAdmissionRows == null)
            {
                return;
            }

            for (int i = 0; i < readModel.CargoTransferables.Count; i++)
            {
                TransferableOneWay transferable = readModel.CargoTransferables[i];
                if (transferable == null || transferable.things == null)
                {
                    continue;
                }

                for (int j = 0; j < transferable.things.Count; j++)
                {
                    Thing thing = transferable.things[j];
                    if (thing == null)
                    {
                        continue;
                    }

                    ShuttleCargoLoadAdmissionResult admission = admissionResolver.Resolve(thing);
                    if (admission != null && admission.Accepted)
                    {
                        readModel.CargoAdmissionRows.Add(admission);
                    }
                }
            }
        }

        private void FillMedevacSummary(ShuttleLoadCargoReadModel readModel, ShuttleProfile profile)
        {
            if (readModel == null)
            {
                return;
            }

            readModel.HasMedicalBay = profile != null &&
                profile.MedicalBay != null &&
                profile.MedicalBay.HasMedicalBay;
            readModel.MedicalPatientSlots = profile != null && profile.MedicalBay != null
                ? profile.MedicalBay.MedicalPatientSlots
                : 0;
            readModel.SupportsMedevacPriority = profile != null &&
                profile.MedicalBay != null &&
                profile.MedicalBay.SupportsMedevacPriority;

            if (!readModel.SupportsMedevacPriority ||
                readModel.PassengerTransferables == null ||
                readModel.PassengerTransferables.Count == 0)
            {
                readModel.MedevacSummaryLabel = this.BuildMedevacSummaryLabel(readModel);
                return;
            }

            for (int i = 0; i < readModel.PassengerTransferables.Count; i++)
            {
                TransferableOneWay transferable = readModel.PassengerTransferables[i];
                if (transferable == null || transferable.things == null)
                {
                    continue;
                }

                for (int j = 0; j < transferable.things.Count; j++)
                {
                    Pawn pawn = transferable.things[j] as Pawn;
                    if (!MedicalEvacuationUtility.IsMedevacCandidate(pawn))
                    {
                        continue;
                    }

                    bool isCritical = MedicalEvacuationUtility.IsCriticalMedevacCandidate(pawn);
                    readModel.MedevacCandidateCount++;
                    if (isCritical)
                    {
                        readModel.CriticalMedevacCandidateCount++;
                    }

                    if (transferable.CountToTransfer > 0)
                    {
                        readModel.SelectedMedevacCandidateCount++;
                    }

                    readModel.MedevacCandidates.Add(new ShuttleMedevacCandidateReadModel
                    {
                        PawnThingID = pawn.thingIDNumber,
                        PawnLabel = pawn.LabelShortCap,
                        IsCritical = isCritical,
                        IsDowned = pawn.Downed,
                        Priority = MedicalEvacuationUtility.GetMedevacPriority(pawn),
                        ReasonLabel = MedicalEvacuationUtility.GetMedevacReasonLabel(pawn)
                    });
                }
            }

            readModel.MedevacSummaryLabel = this.BuildMedevacSummaryLabel(readModel);
        }

        private string BuildMedevacSummaryLabel(ShuttleLoadCargoReadModel readModel)
        {
            if (readModel == null)
            {
                return string.Empty;
            }

            return "CT_Shuttle_Medevac_Summary".Translate(
                readModel.MedicalPatientSlots,
                readModel.MedevacCandidateCount,
                readModel.CriticalMedevacCandidateCount).ToString();
        }

        private void FillLoadCargoSummary(ShuttleLoadCargoReadModel readModel, List<CompTransporter> transporters)
        {
            if (readModel == null || transporters == null)
            {
                return;
            }

            for (int i = 0; i < transporters.Count; i++)
            {
                CompTransporter transporter = transporters[i];
                if (transporter == null)
                {
                    continue;
                }

                if (transporter.innerContainer != null)
                {
                    foreach (Thing thing in transporter.innerContainer)
                    {
                        if (thing == null)
                        {
                            continue;
                        }

                        readModel.LoadedStackCount++;
                        readModel.LoadedThingCount += thing.stackCount;
                        this.TryAddLoadPreviewLine(
                            readModel,
                            "CT_Shuttle_Cargo_CurrentLoaded".Translate(
                                CargoDisplayUtility.GetDisplayLabel(thing),
                                CargoDisplayUtility.GetCountSuffix(thing.stackCount)).ToString());
                    }
                }

                if (transporter.leftToLoad != null)
                {
                    for (int j = 0; j < transporter.leftToLoad.Count; j++)
                    {
                        TransferableOneWay transferable = transporter.leftToLoad[j];
                        if (transferable == null || transferable.CountToTransfer <= 0)
                        {
                            continue;
                        }

                        readModel.QueuedStackCount++;
                        readModel.QueuedThingCount += transferable.CountToTransfer;
                        if (transferable.AnyThing != null)
                        {
                            this.TryAddLoadPreviewLine(
                                readModel,
                                "CT_Shuttle_Cargo_CurrentQueued".Translate(
                                    CargoDisplayUtility.GetDisplayLabel(transferable.AnyThing),
                                    CargoDisplayUtility.GetCountSuffix(transferable.CountToTransfer)).ToString());
                        }
                    }
                }
            }
        }

        private void TryAddLoadPreviewLine(ShuttleLoadCargoReadModel readModel, string line)
        {
            if (readModel == null || string.IsNullOrEmpty(line) || readModel.CurrentLoadPreviewLines == null)
            {
                return;
            }

            if (readModel.CurrentLoadPreviewLines.Count < 3)
            {
                readModel.CurrentLoadPreviewLines.Add(line);
            }
        }
    }
}
