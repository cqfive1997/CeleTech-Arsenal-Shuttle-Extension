using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo
{
    /// <summary>
    /// Narrow adapter for idempotently projecting one spawned passenger intent into the
    /// vanilla transporter loading flow.
    /// </summary>
    internal sealed class VanillaPassengerBoardingQueueAdapter
    {
        private readonly VanillaTransporterAdapter transporterAdapter;

        internal VanillaPassengerBoardingQueueAdapter(
            VanillaTransporterAdapter transporterAdapter)
        {
            this.transporterAdapter = transporterAdapter;
        }

        internal bool ContainsPassenger(ThingWithComps host, Pawn pawn)
        {
            if (pawn == null || this.transporterAdapter == null)
            {
                return false;
            }

            List<CompTransporter> transporters =
                this.transporterAdapter.ResolveTransportersForLaunch(host);
            for (int i = 0; i < transporters.Count; i++)
            {
                CompTransporter transporter = transporters[i];
                if (transporter == null)
                {
                    continue;
                }

                if (transporter.innerContainer != null)
                {
                    for (int j = 0; j < transporter.innerContainer.Count; j++)
                    {
                        Pawn loadedPawn = transporter.innerContainer[j] as Pawn;
                        if (SamePawn(loadedPawn, pawn))
                        {
                            return true;
                        }
                    }
                }

                if (transporter.leftToLoad == null)
                {
                    continue;
                }

                for (int j = 0; j < transporter.leftToLoad.Count; j++)
                {
                    if (TransferableContainsSelectedPawn(
                        transporter.leftToLoad[j],
                        pawn))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal bool EnsurePassengerQueued(
            ThingWithComps host,
            Pawn pawn,
            out bool queuedNow,
            out string failureReason)
        {
            queuedNow = false;
            failureReason = null;
            if (pawn == null || pawn.Destroyed || pawn.Dead)
            {
                failureReason =
                    "CT_Shuttle_Cargo_PassengerUnavailable".Translate().ToString();
                return false;
            }

            if (this.ContainsPassenger(host, pawn))
            {
                return true;
            }

            if (host == null ||
                host.Map == null ||
                !pawn.Spawned ||
                pawn.Map != host.Map ||
                this.transporterAdapter == null)
            {
                failureReason =
                    "CT_Shuttle_Cargo_PassengerUnavailable".Translate().ToString();
                return false;
            }

            List<CompTransporter> transporters =
                this.transporterAdapter.ResolveTransportersForLaunch(host);
            if (transporters == null || transporters.Count == 0)
            {
                failureReason =
                    "CT_Shuttle_Cargo_TransporterGroupUnavailable".Translate().ToString();
                return false;
            }

            float pawnMass = pawn.GetStatValue(StatDefOf.Mass, true, -1);
            if (pawnMass < 0f ||
                float.IsNaN(pawnMass) ||
                float.IsInfinity(pawnMass))
            {
                pawnMass = 0f;
            }

            if (pawnMass > this.transporterAdapter.MassCapacity(transporters) -
                    this.transporterAdapter.ExistingMassUsage(transporters) + 0.001f)
            {
                failureReason =
                    "CT_Shuttle_Cargo_SelectedExceedsCapacity".Translate().ToString();
                return false;
            }

            bool loadingGroupActive = false;
            for (int i = 0; i < transporters.Count; i++)
            {
                CompTransporter transporter = transporters[i];
                if (transporter != null &&
                    transporter.LoadingInProgressOrReadyToLaunch)
                {
                    loadingGroupActive = true;
                    break;
                }
            }

            TransferableOneWay transferable = new TransferableOneWay();
            transferable.things.Add(pawn);
            transferable.AdjustTo(1);
            transporters[0].AddToTheToLoadList(transferable, 1);

            if (!loadingGroupActive)
            {
                ShuttleTransporterLordLifecycle.ResetBeforeNewManifest(
                    host,
                    transporters);
                TransporterUtility.InitiateLoading(transporters);
            }

            TransporterUtility.MakeLordsAsAppropriate(
                new List<Pawn> { pawn },
                transporters,
                host.Map);
            queuedNow = true;
            return true;
        }

        internal bool TryCancelPendingPassenger(
            ThingWithComps host,
            Pawn pawn,
            out bool canceled,
            out string failureReason)
        {
            canceled = false;
            failureReason = null;
            if (this.transporterAdapter == null)
            {
                failureReason =
                    "CT_Shuttle_Command_TransporterBackendUnavailable".Translate().ToString();
                return false;
            }

            return this.transporterAdapter.TryCancelPendingPassenger(
                host,
                pawn,
                out canceled,
                out failureReason);
        }

        private static bool TransferableContainsSelectedPawn(
            TransferableOneWay transferable,
            Pawn pawn)
        {
            if (transferable == null ||
                pawn == null ||
                transferable.CountToTransfer <= 0 ||
                transferable.things == null)
            {
                return false;
            }

            int remaining = transferable.CountToTransfer;
            for (int i = 0; i < transferable.things.Count && remaining > 0; i++)
            {
                Pawn candidate = transferable.things[i] as Pawn;
                if (candidate == null)
                {
                    continue;
                }

                if (SamePawn(candidate, pawn))
                {
                    return true;
                }

                remaining--;
            }

            return false;
        }

        private static bool SamePawn(Pawn left, Pawn right)
        {
            return left != null &&
                right != null &&
                (object.ReferenceEquals(left, right) ||
                    left.thingIDNumber == right.thingIDNumber);
        }
    }
}
