using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo
{
    internal sealed class VanillaTransporterAdapter
    {
        public void ApplyProfile(ThingWithComps host, ShuttleProfile profile)
        {
            if (host == null || profile == null || profile.Cargo == null)
            {
                return;
            }

            CompTransporter transporter = host.TryGetComp<CompTransporter>();
            if (transporter == null)
            {
                return;
            }

            float cargoMassCapacity = profile.Cargo.CargoMassCapacityKg;
            if (cargoMassCapacity < 0f)
            {
                cargoMassCapacity = 0f;
            }

            transporter.massCapacityOverride = cargoMassCapacity;
        }

        public float GetAvailableMass(ThingWithComps host)
        {
            List<CompTransporter> transporters = this.ResolveTransportersForLaunch(host);
            float availableMass = this.MassCapacity(transporters) - this.ExistingMassUsage(transporters);
            return availableMass > 0f ? availableMass : 0f;
        }

        public bool BeginLoading(
            ThingWithComps host,
            List<TransferableOneWay> passengerTransferables,
            List<TransferableOneWay> cargoTransferables,
            bool cleanExistingQueue,
            CargoTransferFilter transferFilter,
            ShuttleCargoLoadAdmissionResolver admissionResolver,
            float reservedExternalMassKg,
            out string failureReason)
        {
            failureReason = null;

            CompTransporter transporter = this.ResolvePrimaryTransporter(host, out failureReason);
            if (transporter == null)
            {
                return false;
            }

            List<CompTransporter> transporters = this.ResolveTransporters(host, transporter);
            if (transporters == null || transporters.Count == 0)
            {
                failureReason = "CT_Shuttle_Cargo_TransporterGroupUnavailable".Translate().ToString();
                return false;
            }

            if (transferFilter == null)
            {
                failureReason = "CT_Shuttle_Cargo_FilterUnavailable".Translate().ToString();
                return false;
            }

            int selectedCount = transferFilter.SelectedCount(passengerTransferables) +
                transferFilter.SelectedCount(cargoTransferables);
            if (selectedCount <= 0)
            {
                failureReason = "CT_Shuttle_Cargo_NoCargoSelected".Translate().ToString();
                return false;
            }

            if (!this.TryValidateLoadSelectionCapacity(
                    passengerTransferables,
                    cargoTransferables,
                    transporters,
                    transferFilter,
                    admissionResolver,
                    reservedExternalMassKg,
                    out failureReason))
            {
                return false;
            }

            if (cleanExistingQueue)
            {
                this.ClearPendingLoadQueueOnly(host, transporters);
            }

            CompTransporter targetTransporter = transporters[0];
            List<Pawn> selectedPawns = transferFilter.GetSelectedPawns(passengerTransferables);

            // Remove the Lord bound to the previous transporter group before
            // InitiateLoading assigns a new group ID. Once the ID changes, vanilla can no
            // longer find that Lord and previously selected pawns may keep re-entering.
            ShuttleTransporterLordLifecycle.ResetBeforeNewManifest(host, transporters);

            // This is the only write path from the shuttle Load Cargo dialog into vanilla
            // leftToLoad. Dialogs pass intent; the backend performs the vanilla side effects.
            transferFilter.AddSelectionsToLoadList(targetTransporter, passengerTransferables);
            transferFilter.AddSelectionsToLoadList(targetTransporter, cargoTransferables);

            TransporterUtility.InitiateLoading(transporters);
            TransporterUtility.MakeLordsAsAppropriate(selectedPawns, transporters, host.Map);
            return true;
        }

        private bool TryValidateLoadSelectionCapacity(
            List<TransferableOneWay> passengerTransferables,
            List<TransferableOneWay> cargoTransferables,
            List<CompTransporter> transporters,
            CargoTransferFilter transferFilter,
            ShuttleCargoLoadAdmissionResolver admissionResolver,
            float reservedExternalMassKg,
            out string failureReason)
        {
            failureReason = null;
            float regularAvailable = this.MassCapacity(transporters) - this.ExistingMassUsage(transporters);
            if (reservedExternalMassKg > 0f &&
                !float.IsNaN(reservedExternalMassKg) &&
                !float.IsInfinity(reservedExternalMassKg))
            {
                regularAvailable -= reservedExternalMassKg;
            }

            if (regularAvailable < 0f)
            {
                regularAvailable = 0f;
            }

            float refrigeratedAvailable = admissionResolver != null
                ? admissionResolver.RefrigeratedAutoTransferAvailableMassKg
                : 0f;
            bool refrigeratedMassSharesOverallCapacity = admissionResolver != null &&
                admissionResolver.RefrigeratedMassSharesOverallCapacity;
            if (refrigeratedMassSharesOverallCapacity)
            {
                regularAvailable -= admissionResolver.RefrigeratedAutoTransferUsedMassKg;
                if (regularAvailable < 0f)
                {
                    regularAvailable = 0f;
                }

                refrigeratedAvailable = regularAvailable;
            }

            float selectedRegularMass = transferFilter != null
                ? transferFilter.SelectedMass(passengerTransferables)
                : 0f;
            float selectedColdMass = 0f;

            if (selectedRegularMass > regularAvailable + 0.001f)
            {
                failureReason = "CT_Shuttle_Cargo_SelectedExceedsCapacity".Translate().ToString();
                return false;
            }

            if (cargoTransferables == null)
            {
                return true;
            }

            for (int i = 0; i < cargoTransferables.Count; i++)
            {
                TransferableOneWay transferable = cargoTransferables[i];
                if (transferable == null || transferable.CountToTransfer <= 0 || transferable.things == null)
                {
                    continue;
                }

                int remaining = transferable.CountToTransfer;
                for (int j = 0; j < transferable.things.Count && remaining > 0; j++)
                {
                    Thing thing = transferable.things[j];
                    if (thing == null)
                    {
                        continue;
                    }

                    int count = remaining < thing.stackCount ? remaining : thing.stackCount;
                    if (count <= 0)
                    {
                        continue;
                    }

                    float mass = thing.GetStatValue(StatDefOf.Mass, true, -1) * count;
                    ShuttleCargoLoadAdmissionResult admission = admissionResolver != null
                        ? admissionResolver.ResolveOrDefault(thing)
                        : null;

                    bool assigned = false;
                    if (refrigeratedMassSharesOverallCapacity)
                    {
                        bool refrigeratedAccepted = admission != null &&
                            admission.RefrigeratedAccepted;
                        bool regularAccepted = admission == null || admission.RegularAccepted;
                        if ((refrigeratedAccepted || regularAccepted) &&
                            selectedRegularMass + selectedColdMass + mass <=
                                regularAvailable + 0.001f)
                        {
                            if (refrigeratedAccepted)
                            {
                                selectedColdMass += mass;
                            }
                            else
                            {
                                selectedRegularMass += mass;
                            }

                            assigned = true;
                        }
                    }
                    else if (admission != null &&
                        admission.RefrigeratedAccepted &&
                        mass <= admission.RefrigeratedAvailableMassKg + 0.001f &&
                        selectedColdMass + mass <= refrigeratedAvailable + 0.001f)
                    {
                        selectedColdMass += mass;
                        assigned = true;
                    }
                    else if ((admission == null || admission.RegularAccepted) &&
                        selectedRegularMass + mass <= regularAvailable + 0.001f)
                    {
                        selectedRegularMass += mass;
                        assigned = true;
                    }

                    if (!assigned)
                    {
                        failureReason = admission != null && !string.IsNullOrEmpty(admission.Reason)
                            ? admission.Reason
                            : "CT_Shuttle_Cargo_SelectedExceedsCapacity".Translate().ToString();
                        return false;
                    }

                    remaining -= count;
                }
            }

            return true;
        }

        public bool ClearQueuedLoad(ThingWithComps host, out string failureReason)
        {
            failureReason = null;

            CompTransporter transporter = this.ResolvePrimaryTransporter(host, out failureReason);
            if (transporter == null)
            {
                return false;
            }

            List<CompTransporter> transporters = this.ResolveTransporters(host, transporter);
            this.ClearPendingLoadQueueOnly(host, transporters);

            return true;
        }

        public bool TryCancelPendingPassenger(
            ThingWithComps host,
            Pawn pawn,
            out bool canceled,
            out string failureReason)
        {
            canceled = false;
            failureReason = null;
            if (pawn == null || pawn.Destroyed || pawn.Dead)
            {
                failureReason =
                    "CT_Shuttle_Cargo_PassengerUnavailable".Translate().ToString();
                return false;
            }

            CompTransporter transporter = this.ResolvePrimaryTransporter(host, out failureReason);
            if (transporter == null)
            {
                return false;
            }

            List<CompTransporter> transporters = this.ResolveTransporters(host, transporter);
            for (int i = 0; i < transporters.Count; i++)
            {
                CompTransporter candidateTransporter = transporters[i];
                if (candidateTransporter == null || candidateTransporter.leftToLoad == null)
                {
                    continue;
                }

                for (int j = candidateTransporter.leftToLoad.Count - 1; j >= 0; j--)
                {
                    TransferableOneWay transferable = candidateTransporter.leftToLoad[j];
                    if (!this.TryRemoveSelectedPawnFromTransferable(transferable, pawn))
                    {
                        continue;
                    }

                    canceled = true;
                    if (transferable.CountToTransfer <= 0 ||
                        transferable.things == null ||
                        transferable.things.Count == 0)
                    {
                        candidateTransporter.leftToLoad.RemoveAt(j);
                    }
                }
            }

            if (canceled)
            {
                if (pawn.jobs != null &&
                    pawn.CurJobDef == JobDefOf.EnterTransporter)
                {
                    this.TryStopPawnTransporterEntryJob(host, transporters, pawn);
                }

                this.TryReleasePawnFromRestrictiveLord(pawn);
                this.ReleaseLoadingLordIfQueueEmpty(host, transporters);
            }

            return true;
        }

        public bool CancelQueuedLoadEntry(
            ThingWithComps host,
            int transporterIndex,
            int queueIndex,
            out string failureReason)
        {
            TransferableOneWay canceledTransferable;
            int canceledCount;
            return this.CancelQueuedLoadEntry(
                host,
                transporterIndex,
                queueIndex,
                int.MaxValue,
                out canceledTransferable,
                out canceledCount,
                out failureReason);
        }

        public bool CancelQueuedLoadEntry(
            ThingWithComps host,
            int transporterIndex,
            int queueIndex,
            int count,
            out TransferableOneWay canceledTransferable,
            out int canceledCount,
            out string failureReason)
        {
            failureReason = null;
            canceledTransferable = null;
            canceledCount = 0;

            if (count <= 0)
            {
                failureReason = "CT_Shuttle_Cargo_Quantity_Invalid".Translate().ToString();
                return false;
            }

            CompTransporter transporter = this.ResolvePrimaryTransporter(host, out failureReason);
            if (transporter == null)
            {
                return false;
            }

            List<CompTransporter> transporters = this.ResolveTransporters(host, transporter);
            if (transporterIndex < 0 || transporterIndex >= transporters.Count)
            {
                failureReason = "CT_Shuttle_Cargo_QueuedIndexInvalid".Translate().ToString();
                return false;
            }

            CompTransporter target = transporters[transporterIndex];
            if (target == null || target.leftToLoad == null || queueIndex < 0 || queueIndex >= target.leftToLoad.Count)
            {
                failureReason = "CT_Shuttle_Cargo_QueuedEntryUnavailable".Translate().ToString();
                return false;
            }

            canceledTransferable = target.leftToLoad[queueIndex];
            if (canceledTransferable == null || canceledTransferable.CountToTransfer <= 0)
            {
                failureReason = "CT_Shuttle_Cargo_QueuedEntryUnavailable".Translate().ToString();
                return false;
            }

            int queuedCount = canceledTransferable.CountToTransfer;
            bool isPawn = canceledTransferable.AnyThing is Pawn;
            if (isPawn && count < queuedCount)
            {
                failureReason = "CT_Shuttle_Cargo_Quantity_Invalid".Translate().ToString();
                return false;
            }

            canceledCount = count < queuedCount ? count : queuedCount;
            if (canceledCount < queuedCount)
            {
                canceledTransferable.AdjustTo(queuedCount - canceledCount);
                return true;
            }

            List<Pawn> canceledPawns = this.CollectPawnsFromTransferable(canceledTransferable);
            target.leftToLoad.RemoveAt(queueIndex);
            this.ReleaseCanceledPendingLoadPawns(host, transporters, canceledPawns);
            this.ReleaseLoadingLordIfQueueEmpty(host, transporters);
            return true;
        }

        public bool UnloadLoadedCargoEntry(
            ThingWithComps host,
            int transporterIndex,
            int loadedIndex,
            int thingIDNumber,
            string expectedDefName,
            int count,
            out string failureReason)
        {
            int unloadedCount;
            return this.UnloadLoadedCargoEntry(
                host,
                transporterIndex,
                loadedIndex,
                thingIDNumber,
                expectedDefName,
                count,
                out unloadedCount,
                out failureReason);
        }

        public bool UnloadLoadedCargoEntry(
            ThingWithComps host,
            int transporterIndex,
            int loadedIndex,
            int thingIDNumber,
            string expectedDefName,
            int count,
            out int unloadedCount,
            out string failureReason)
        {
            unloadedCount = 0;
            failureReason = null;

            if (count <= 0)
            {
                failureReason = "CT_Shuttle_Cargo_Quantity_Invalid".Translate().ToString();
                return false;
            }

            CompTransporter transporter = this.ResolvePrimaryTransporter(host, out failureReason);
            if (transporter == null)
            {
                return false;
            }

            List<CompTransporter> transporters = this.ResolveTransporters(host, transporter);
            if (transporterIndex < 0 || transporterIndex >= transporters.Count)
            {
                failureReason = "CT_Shuttle_Cargo_LoadedTransporterIndexInvalid".Translate().ToString();
                return false;
            }

            CompTransporter target = transporters[transporterIndex];
            if (target == null || target.innerContainer == null)
            {
                failureReason = "CT_Shuttle_Cargo_LoadedContainerUnavailable".Translate().ToString();
                return false;
            }

            Thing thing = this.FindLoadedThing(target, loadedIndex, thingIDNumber, expectedDefName);
            if (thing == null)
            {
                failureReason = "CT_Shuttle_Cargo_LoadedEntryUnavailable".Translate().ToString();
                return false;
            }

            if (thing.stackCount <= 0)
            {
                failureReason = "CT_Shuttle_Cargo_LoadedEntryNoStack".Translate().ToString();
                return false;
            }

            bool isPawn = thing is Pawn;
            if (isPawn && count < thing.stackCount)
            {
                failureReason = "CT_Shuttle_Cargo_Quantity_Invalid".Translate().ToString();
                return false;
            }

            int dropCount = count < thing.stackCount ? count : thing.stackCount;
            Thing resultingThing;
            if (!target.innerContainer.TryDrop(
                thing,
                target.parent.Position,
                host.Map,
                ThingPlaceMode.Near,
                dropCount,
                out resultingThing,
                null,
                null))
            {
                failureReason = "CT_Shuttle_Cargo_UnloadFailed".Translate().ToString();
                return false;
            }

            // ThingOwner removal does not call CompTransporter.Notify_ThingRemoved for us.
            target.Notify_ThingRemoved(resultingThing ?? thing);
            Pawn resultingPawn = resultingThing as Pawn ?? thing as Pawn;
            if (resultingPawn != null)
            {
                this.RemovePawnFromPendingLoadQueues(transporters, resultingPawn);
                this.TryStopPawnTransporterEntryJob(host, transporters, resultingPawn);
                this.TryReleasePawnFromRestrictiveLord(resultingPawn);
                this.TryReleaseCompletedLoadingLordAfterUnload(host, transporters, resultingPawn);
            }

            unloadedCount = dropCount;

            return true;
        }

        public bool UnloadLoadedCargoBay(
            ThingWithComps host,
            IReadOnlyList<ShuttleLoadedCargoUnloadTarget> targets,
            out int unloadedStackCount,
            out int unloadedThingCount,
            out string failureReason)
        {
            unloadedStackCount = 0;
            unloadedThingCount = 0;
            failureReason = null;

            if (targets == null || targets.Count == 0)
            {
                failureReason = "CT_Shuttle_Cargo_BayUnloadNoContents".Translate().ToString();
                return false;
            }

            CompTransporter transporter = this.ResolvePrimaryTransporter(host, out failureReason);
            if (transporter == null)
            {
                return false;
            }

            List<CompTransporter> transporters = this.ResolveTransporters(host, transporter);
            List<ResolvedLoadedCargoUnloadTarget> resolvedTargets =
                new List<ResolvedLoadedCargoUnloadTarget>();
            HashSet<int> seenThingIDs = new HashSet<int>();
            for (int i = 0; i < targets.Count; i++)
            {
                ShuttleLoadedCargoUnloadTarget target = targets[i];
                if (target == null)
                {
                    failureReason = "CT_Shuttle_Cargo_LoadedEntryUnavailable".Translate().ToString();
                    return false;
                }

                if (target.ThingIDNumber <= 0 || string.IsNullOrEmpty(target.ExpectedDefName))
                {
                    failureReason = "CT_Shuttle_Cargo_LoadedEntryUnavailable".Translate().ToString();
                    return false;
                }

                if (seenThingIDs.Contains(target.ThingIDNumber))
                {
                    continue;
                }

                seenThingIDs.Add(target.ThingIDNumber);

                ResolvedLoadedCargoUnloadTarget resolvedTarget;
                if (!this.TryResolveLoadedCargoUnloadTarget(
                        transporters,
                        target,
                        out resolvedTarget,
                        out failureReason))
                {
                    return false;
                }

                resolvedTargets.Add(resolvedTarget);
            }

            if (resolvedTargets.Count == 0)
            {
                failureReason = "CT_Shuttle_Cargo_BayUnloadNoContents".Translate().ToString();
                return false;
            }

            for (int i = 0; i < resolvedTargets.Count; i++)
            {
                ResolvedLoadedCargoUnloadTarget target = resolvedTargets[i];
                int dropCount = target.Count;
                if (dropCount <= 0)
                {
                    failureReason = "CT_Shuttle_Cargo_LoadedEntryNoStack".Translate().ToString();
                    return false;
                }

                Thing resultingThing;
                if (!target.Transporter.innerContainer.TryDrop(
                    target.Thing,
                    target.Transporter.parent.Position,
                    host.Map,
                    ThingPlaceMode.Near,
                    dropCount,
                    out resultingThing,
                    null,
                    null))
                {
                    failureReason = "CT_Shuttle_Cargo_UnloadFailed".Translate().ToString();
                    return false;
                }

                target.Transporter.Notify_ThingRemoved(resultingThing ?? target.Thing);
                unloadedStackCount++;
                unloadedThingCount += dropCount;
            }

            return true;
        }

        private bool TryResolveLoadedCargoUnloadTarget(
            List<CompTransporter> transporters,
            ShuttleLoadedCargoUnloadTarget target,
            out ResolvedLoadedCargoUnloadTarget resolvedTarget,
            out string failureReason)
        {
            resolvedTarget = null;
            failureReason = null;

            if (transporters == null ||
                target == null ||
                target.TransporterIndex < 0 ||
                target.TransporterIndex >= transporters.Count)
            {
                failureReason = "CT_Shuttle_Cargo_LoadedTransporterIndexInvalid".Translate().ToString();
                return false;
            }

            CompTransporter targetTransporter = transporters[target.TransporterIndex];
            if (targetTransporter == null || targetTransporter.innerContainer == null)
            {
                failureReason = "CT_Shuttle_Cargo_LoadedContainerUnavailable".Translate().ToString();
                return false;
            }

            Thing thing = this.FindLoadedThing(
                targetTransporter,
                target.LoadedIndex,
                target.ThingIDNumber,
                target.ExpectedDefName);
            if (thing == null)
            {
                failureReason = "CT_Shuttle_Cargo_LoadedEntryUnavailable".Translate().ToString();
                return false;
            }

            if (thing is Pawn)
            {
                failureReason = "CT_Shuttle_Cargo_BayUnloadPassengersUnsupported".Translate().ToString();
                return false;
            }

            if (thing.stackCount <= 0)
            {
                failureReason = "CT_Shuttle_Cargo_LoadedEntryNoStack".Translate().ToString();
                return false;
            }

            if (target.Count <= 0 || target.Count > thing.stackCount)
            {
                failureReason = "CT_Shuttle_Cargo_Quantity_Invalid".Translate().ToString();
                return false;
            }

            resolvedTarget = new ResolvedLoadedCargoUnloadTarget(
                targetTransporter,
                thing,
                target.Count);
            return true;
        }

        private void TryReleaseCompletedLoadingLordAfterUnload(
            ThingWithComps host,
            List<CompTransporter> transporters,
            Pawn unloadedPawn)
        {
            if (host == null || host.Map == null || transporters == null || unloadedPawn == null)
            {
                return;
            }

            if (this.HasPendingLoadQueue(transporters))
            {
                return;
            }

            // Manual unload can leave spawned pawns in the completed vanilla loading lord,
            // which blocks later shuttle jobs; only release it after the load queue is empty.
            bool hasLoadedContents = this.HasLoadedContents(transporters);
            for (int i = 0; i < transporters.Count; i++)
            {
                CompTransporter transporter = transporters[i];
                if (transporter != null)
                {
                    transporter.TryRemoveLord(host.Map);
                    if (!hasLoadedContents)
                    {
                        transporter.groupID = -1;
                    }
                }
            }
        }

        private void RemovePawnFromPendingLoadQueues(List<CompTransporter> transporters, Pawn unloadedPawn)
        {
            if (transporters == null || unloadedPawn == null)
            {
                return;
            }

            for (int i = 0; i < transporters.Count; i++)
            {
                CompTransporter transporter = transporters[i];
                if (transporter == null || transporter.leftToLoad == null)
                {
                    continue;
                }

                for (int j = transporter.leftToLoad.Count - 1; j >= 0; j--)
                {
                    TransferableOneWay transferable = transporter.leftToLoad[j];
                    int removedCount = this.RemovePawnFromTransferable(transferable, unloadedPawn);
                    if (transferable == null || transferable.CountToTransfer <= 0 || removedCount <= 0)
                    {
                        if (transferable == null || transferable.CountToTransfer <= 0)
                        {
                            transporter.leftToLoad.RemoveAt(j);
                        }

                        continue;
                    }

                    int adjustedCount = transferable.CountToTransfer - removedCount;
                    if (adjustedCount <= 0 || transferable.things == null || transferable.things.Count == 0)
                    {
                        transporter.leftToLoad.RemoveAt(j);
                    }
                    else
                    {
                        transferable.AdjustTo(adjustedCount);
                    }
                }
            }
        }

        private int RemovePawnFromTransferable(TransferableOneWay transferable, Pawn pawn)
        {
            if (transferable == null || transferable.things == null || pawn == null)
            {
                return 0;
            }

            int removedCount = 0;
            for (int i = transferable.things.Count - 1; i >= 0; i--)
            {
                Pawn candidate = transferable.things[i] as Pawn;
                if (candidate == null)
                {
                    continue;
                }

                if (candidate == pawn ||
                    (candidate.thingIDNumber > 0 &&
                        candidate.thingIDNumber == pawn.thingIDNumber))
                {
                    transferable.things.RemoveAt(i);
                    removedCount++;
                }
            }

            return removedCount;
        }

        private bool TryRemoveSelectedPawnFromTransferable(
            TransferableOneWay transferable,
            Pawn pawn)
        {
            if (transferable == null ||
                transferable.things == null ||
                transferable.CountToTransfer <= 0 ||
                pawn == null)
            {
                return false;
            }

            int selectedCount = transferable.CountToTransfer;
            int remainingSelectedCount = selectedCount;
            for (int i = 0;
                i < transferable.things.Count && remainingSelectedCount > 0;
                i++)
            {
                Pawn candidate = transferable.things[i] as Pawn;
                if (candidate == null)
                {
                    continue;
                }

                if (candidate == pawn ||
                    (candidate.thingIDNumber > 0 &&
                        candidate.thingIDNumber == pawn.thingIDNumber))
                {
                    transferable.things.RemoveAt(i);
                    transferable.AdjustTo(selectedCount - 1);
                    return true;
                }

                remainingSelectedCount--;
            }

            return false;
        }

        private void TryStopPawnTransporterEntryJob(
            ThingWithComps host,
            List<CompTransporter> transporters,
            Pawn pawn)
        {
            if (pawn == null || pawn.jobs == null || pawn.CurJob == null)
            {
                return;
            }

            Job job = pawn.CurJob;
            if (!this.JobTargetsTransporterGroup(job, host, transporters))
            {
                return;
            }

            try
            {
                pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, false, true);
            }
            catch (System.Exception exception)
            {
                Log.Warning("[CeleTech Shuttle] Could not stop transporter entry job after shuttle cargo unload: " + exception);
            }
        }

        private void TryReleasePawnFromRestrictiveLord(Pawn pawn)
        {
            if (pawn == null || pawn.Destroyed)
            {
                return;
            }

            Lord lord = pawn.GetLord();
            if (lord == null ||
                lord.CurLordToil == null ||
                lord.CurLordToil.AllowSatisfyLongNeeds)
            {
                return;
            }

            try
            {
                lord.Notify_PawnLost(pawn, PawnLostCondition.ForcedToJoinOtherLord);
            }
            catch (System.Exception exception)
            {
                Log.Warning("[CeleTech Shuttle] Could not release unloaded pawn from restrictive transporter lord: " + exception);
            }
        }

        private bool JobTargetsTransporterGroup(
            Job job,
            ThingWithComps host,
            List<CompTransporter> transporters)
        {
            if (job == null)
            {
                return false;
            }

            return this.TargetReferencesTransporterGroup(job.GetTarget(TargetIndex.A), host, transporters) ||
                this.TargetReferencesTransporterGroup(job.GetTarget(TargetIndex.B), host, transporters) ||
                this.TargetReferencesTransporterGroup(job.GetTarget(TargetIndex.C), host, transporters);
        }

        private bool TargetReferencesTransporterGroup(
            LocalTargetInfo target,
            ThingWithComps host,
            List<CompTransporter> transporters)
        {
            if (!target.IsValid || !target.HasThing)
            {
                return false;
            }

            Thing targetThing = target.Thing;
            if (targetThing == null)
            {
                return false;
            }

            if (host != null && targetThing == host)
            {
                return true;
            }

            if (transporters == null)
            {
                return false;
            }

            for (int i = 0; i < transporters.Count; i++)
            {
                CompTransporter transporter = transporters[i];
                if (transporter != null && transporter.parent == targetThing)
                {
                    return true;
                }
            }

            return false;
        }

        public List<CompTransporter> ResolveTransportersForLaunch(ThingWithComps host)
        {
            List<CompTransporter> transporters = new List<CompTransporter>();
            if (host == null)
            {
                return transporters;
            }

            CompTransporter primary = host.TryGetComp<CompTransporter>();
            if (primary == null)
            {
                return transporters;
            }

            return this.ResolveTransporters(host, primary);
        }

        public List<IThingHolder> ResolveTransporterHoldersForLaunch(ThingWithComps host)
        {
            List<IThingHolder> holders = new List<IThingHolder>();
            List<CompTransporter> transporters = this.ResolveTransportersForLaunch(host);
            for (int i = 0; i < transporters.Count; i++)
            {
                CompTransporter transporter = transporters[i];
                if (transporter != null)
                {
                    holders.Add(transporter);
                }
            }

            return holders;
        }

        public bool HasPendingLoadQueue(ThingWithComps host)
        {
            return this.HasPendingLoadQueue(this.ResolveTransportersForLaunch(host));
        }

        private bool HasPendingLoadQueue(List<CompTransporter> transporters)
        {
            for (int i = 0; i < transporters.Count; i++)
            {
                CompTransporter transporter = transporters[i];
                if (transporter == null || transporter.leftToLoad == null)
                {
                    continue;
                }

                for (int j = 0; j < transporter.leftToLoad.Count; j++)
                {
                    TransferableOneWay transferable = transporter.leftToLoad[j];
                    if (transferable != null && transferable.CountToTransfer > 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void ClearPendingLoadQueueOnly(ThingWithComps host, List<CompTransporter> transporters)
        {
            if (host == null || host.Map == null || transporters == null)
            {
                return;
            }

            // CleanUpLoadingVars also drops innerContainer contents; queue cancel must not unload cargo.
            List<Pawn> canceledPawns = this.CollectPendingLoadPawns(transporters);
            this.CollectLegacyBlockedLoadingPawns(host, transporters, canceledPawns);
            bool hasLoadedContents = this.HasLoadedContents(transporters);
            for (int i = 0; i < transporters.Count; i++)
            {
                CompTransporter transporter = transporters[i];
                if (transporter != null)
                {
                    transporter.TryRemoveLord(host.Map);
                }
            }

            for (int i = 0; i < transporters.Count; i++)
            {
                CompTransporter transporter = transporters[i];
                if (transporter == null)
                {
                    continue;
                }

                if (transporter.leftToLoad != null)
                {
                    transporter.leftToLoad.Clear();
                }

                if (!hasLoadedContents)
                {
                    transporter.groupID = -1;
                }
            }

            this.ReleaseCanceledPendingLoadPawns(host, transporters, canceledPawns);
        }

        private List<Pawn> CollectPendingLoadPawns(List<CompTransporter> transporters)
        {
            List<Pawn> pawns = new List<Pawn>();
            if (transporters == null)
            {
                return pawns;
            }

            for (int i = 0; i < transporters.Count; i++)
            {
                CompTransporter transporter = transporters[i];
                if (transporter == null || transporter.leftToLoad == null)
                {
                    continue;
                }

                for (int j = 0; j < transporter.leftToLoad.Count; j++)
                {
                    this.CollectPawnsFromTransferable(transporter.leftToLoad[j], pawns);
                }
            }

            return pawns;
        }

        private void CollectLegacyBlockedLoadingPawns(
            ThingWithComps host,
            List<CompTransporter> transporters,
            List<Pawn> pawns)
        {
            if (host == null || host.Map == null || host.Map.mapPawns == null || pawns == null)
            {
                return;
            }

            List<Pawn> colonists = host.Map.mapPawns.FreeColonistsSpawned;
            if (colonists == null)
            {
                return;
            }

            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn pawn = colonists[i];
                if (this.IsLegacyBlockedLoadingPawn(pawn, host, transporters))
                {
                    this.AddUniquePawn(pawns, pawn);
                }
            }
        }

        private bool IsLegacyBlockedLoadingPawn(
            Pawn pawn,
            ThingWithComps host,
            List<CompTransporter> transporters)
        {
            if (pawn == null ||
                pawn.Destroyed ||
                pawn.Dead ||
                !pawn.Spawned ||
                pawn.Map != host.Map)
            {
                return false;
            }

            if (pawn.CurJob != null && this.JobTargetsTransporterGroup(pawn.CurJob, host, transporters))
            {
                return true;
            }

            Lord lord = pawn.GetLord();
            if (lord == null ||
                lord.CurLordToil == null ||
                lord.CurLordToil.AllowSatisfyLongNeeds ||
                lord.LordJob == null)
            {
                return false;
            }

            string lordJobName = lord.LordJob.GetType().Name;
            return !string.IsNullOrEmpty(lordJobName) &&
                lordJobName.IndexOf("Transporter", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private List<Pawn> CollectPawnsFromTransferable(TransferableOneWay transferable)
        {
            List<Pawn> pawns = new List<Pawn>();
            this.CollectPawnsFromTransferable(transferable, pawns);
            return pawns;
        }

        private void CollectPawnsFromTransferable(TransferableOneWay transferable, List<Pawn> pawns)
        {
            if (transferable == null || transferable.things == null || pawns == null)
            {
                return;
            }

            for (int i = 0; i < transferable.things.Count; i++)
            {
                Pawn pawn = transferable.things[i] as Pawn;
                if (pawn != null)
                {
                    this.AddUniquePawn(pawns, pawn);
                }
            }
        }

        private void AddUniquePawn(List<Pawn> pawns, Pawn pawn)
        {
            if (pawns == null || pawn == null)
            {
                return;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn existing = pawns[i];
                if (existing == pawn ||
                    (existing != null &&
                        existing.thingIDNumber > 0 &&
                        existing.thingIDNumber == pawn.thingIDNumber))
                {
                    return;
                }
            }

            pawns.Add(pawn);
        }

        private void ReleaseCanceledPendingLoadPawns(
            ThingWithComps host,
            List<CompTransporter> transporters,
            List<Pawn> canceledPawns)
        {
            if (canceledPawns == null)
            {
                return;
            }

            for (int i = 0; i < canceledPawns.Count; i++)
            {
                Pawn pawn = canceledPawns[i];
                if (pawn == null)
                {
                    continue;
                }

                this.TryStopPawnTransporterEntryJob(host, transporters, pawn);
                this.TryReleasePawnFromRestrictiveLord(pawn);
            }
        }

        private void ReleaseLoadingLordIfQueueEmpty(ThingWithComps host, List<CompTransporter> transporters)
        {
            if (host == null || host.Map == null || transporters == null || this.HasPendingLoadQueue(transporters))
            {
                return;
            }

            bool hasLoadedContents = this.HasLoadedContents(transporters);
            for (int i = 0; i < transporters.Count; i++)
            {
                CompTransporter transporter = transporters[i];
                if (transporter != null)
                {
                    transporter.TryRemoveLord(host.Map);
                    if (!hasLoadedContents)
                    {
                        transporter.groupID = -1;
                    }
                }
            }
        }

        private bool HasLoadedContents(List<CompTransporter> transporters)
        {
            if (transporters == null)
            {
                return false;
            }

            for (int i = 0; i < transporters.Count; i++)
            {
                CompTransporter transporter = transporters[i];
                if (transporter != null &&
                    transporter.innerContainer != null &&
                    transporter.innerContainer.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        public bool AnyLaunchTransporterPositionRoofed(ThingWithComps host, Map map)
        {
            if (map == null)
            {
                return false;
            }

            List<CompTransporter> transporters = this.ResolveTransportersForLaunch(host);
            for (int i = 0; i < transporters.Count; i++)
            {
                CompTransporter transporter = transporters[i];
                if (transporter != null && transporter.parent != null && transporter.parent.Position.Roofed(map))
                {
                    return true;
                }
            }

            return false;
        }

        public int GetLaunchGroupID(ThingWithComps host)
        {
            List<CompTransporter> transporters = this.ResolveTransportersForLaunch(host);
            if (transporters == null || transporters.Count == 0 || transporters[0] == null)
            {
                return -1;
            }

            return transporters[0].groupID;
        }

        public bool TryEnsureLaunchGroupID(
            List<CompTransporter> transporters,
            out int groupID,
            out string failureReason)
        {
            groupID = -1;
            failureReason = null;
            if (transporters == null || transporters.Count == 0)
            {
                failureReason = "CT_Shuttle_Launch_Failed_TransporterBackendUnavailable".Translate().ToString();
                return false;
            }

            for (int i = 0; i < transporters.Count; i++)
            {
                CompTransporter transporter = transporters[i];
                if (transporter != null && transporter.groupID >= 0)
                {
                    groupID = transporter.groupID;
                    break;
                }
            }

            if (groupID < 0)
            {
                if (Find.UniqueIDsManager == null)
                {
                    failureReason = "CT_Shuttle_Launch_Failed_InvalidTransporterGroupID".Translate().ToString();
                    return false;
                }

                groupID = Find.UniqueIDsManager.GetNextTransporterGroupID();
            }

            if (groupID < 0)
            {
                failureReason = "CT_Shuttle_Launch_Failed_InvalidTransporterGroupID".Translate().ToString();
                return false;
            }

            for (int i = 0; i < transporters.Count; i++)
            {
                CompTransporter transporter = transporters[i];
                if (transporter != null)
                {
                    transporter.groupID = groupID;
                }
            }

            return true;
        }

        public void TryRemoveLaunchLords(ThingWithComps host, Map map)
        {
            List<CompTransporter> transporters = this.ResolveTransportersForLaunch(host);
            for (int i = 0; i < transporters.Count; i++)
            {
                CompTransporter transporter = transporters[i];
                if (transporter != null)
                {
                    transporter.TryRemoveLord(map);
                }
            }
        }

        public float ExistingMassUsage(List<CompTransporter> transporters)
        {
            float mass = 0f;
            if (transporters == null)
            {
                return mass;
            }

            for (int i = 0; i < transporters.Count; i++)
            {
                CompTransporter transporter = transporters[i];
                if (transporter != null)
                {
                    mass += transporter.MassUsage;
                }
            }

            return mass;
        }

        public float MassCapacity(List<CompTransporter> transporters)
        {
            float capacity = 0f;
            if (transporters == null)
            {
                return capacity;
            }

            for (int i = 0; i < transporters.Count; i++)
            {
                CompTransporter transporter = transporters[i];
                if (transporter != null)
                {
                    capacity += transporter.MassCapacity;
                }
            }

            return capacity;
        }

        private CompTransporter ResolvePrimaryTransporter(ThingWithComps host, out string failureReason)
        {
            failureReason = null;

            if (host == null)
            {
                failureReason = "CT_Shuttle_Command_ShuttleHostUnavailable".Translate().ToString();
                return null;
            }

            CompTransporter transporter = host.TryGetComp<CompTransporter>();
            if (transporter == null)
            {
                failureReason = "CT_Shuttle_UI_TransporterCompUnavailable".Translate().ToString();
                return null;
            }

            if (host.Map == null)
            {
                failureReason = "CT_Shuttle_Command_ShuttleMapUnavailable".Translate().ToString();
                return null;
            }

            return transporter;
        }

        private List<CompTransporter> ResolveTransporters(ThingWithComps host, CompTransporter primaryTransporter)
        {
            List<CompTransporter> transporters = null;
            if (host != null && host.Map != null && primaryTransporter != null)
            {
                transporters = primaryTransporter.TransportersInGroup(host.Map);
            }

            if (transporters == null || transporters.Count == 0)
            {
                transporters = new List<CompTransporter>();
                if (primaryTransporter != null)
                {
                    transporters.Add(primaryTransporter);
                }
            }

            return transporters;
        }

        private Thing FindLoadedThing(
            CompTransporter transporter,
            int loadedIndex,
            int thingIDNumber,
            string expectedDefName)
        {
            if (transporter == null || transporter.innerContainer == null)
            {
                return null;
            }

            if (loadedIndex >= 0 && loadedIndex < transporter.innerContainer.Count)
            {
                Thing indexedThing = transporter.innerContainer[loadedIndex];
                if (this.MatchesLoadedThing(indexedThing, thingIDNumber, expectedDefName))
                {
                    return indexedThing;
                }
            }

            if (thingIDNumber <= 0)
            {
                return null;
            }

            for (int i = 0; i < transporter.innerContainer.Count; i++)
            {
                Thing thing = transporter.innerContainer[i];
                if (this.MatchesLoadedThing(thing, thingIDNumber, expectedDefName))
                {
                    return thing;
                }
            }

            return null;
        }

        private bool MatchesLoadedThing(Thing thing, int thingIDNumber, string expectedDefName)
        {
            if (thing == null)
            {
                return false;
            }

            if (thingIDNumber > 0 && thing.thingIDNumber != thingIDNumber)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(expectedDefName))
            {
                if (thing.def == null || thing.def.defName != expectedDefName)
                {
                    return false;
                }
            }

            return true;
        }

        private sealed class ResolvedLoadedCargoUnloadTarget
        {
            internal ResolvedLoadedCargoUnloadTarget(
                CompTransporter transporter,
                Thing thing,
                int count)
            {
                this.Transporter = transporter;
                this.Thing = thing;
                this.Count = count;
            }

            internal readonly CompTransporter Transporter;
            internal readonly Thing Thing;
            internal readonly int Count;
        }
    }
}
