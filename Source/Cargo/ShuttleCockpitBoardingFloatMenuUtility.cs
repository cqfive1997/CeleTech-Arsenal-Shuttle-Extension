using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using RimWorld;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo
{
    /// <summary>
    /// Shuttle-owned float-menu shortcut for ordering a selected colonist into the cockpit.
    /// The cargo command remains authoritative for admission and transporter writes. Drafted pawns
    /// receive vanilla's player-ordered boarding job after that command succeeds.
    /// </summary>
    internal static class ShuttleCockpitBoardingFloatMenuUtility
    {
        private static readonly VanillaTransporterAdapter TransporterAdapter =
            new VanillaTransporterAdapter();

        internal static IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selectedPawn, Thing shuttleHost)
        {
            if (!IsPotentialBoardingPawn(selectedPawn))
            {
                yield break;
            }

            ThingWithComps host = shuttleHost as ThingWithComps;
            if (host == null)
            {
                yield break;
            }

            ShuttleController controller;
            string failReason;
            if (!TryResolveController(host, out controller, out failReason))
            {
                yield return DisabledOption(failReason);
                yield break;
            }

            if (!CanQueuePawnForCockpit(controller, host, selectedPawn, out failReason))
            {
                yield return DisabledOption(failReason);
                yield break;
            }

            Pawn pawnForOrder = selectedPawn;
            ThingWithComps hostForOrder = host;
            ShuttleController controllerForOrder = controller;
            yield return new FloatMenuOption(
                "CT_Shuttle_CockpitBoarding_Enter".Translate().ToString(),
                delegate
                {
                    TryStartCockpitBoarding(controllerForOrder, hostForOrder, pawnForOrder);
                });
        }

        internal static IEnumerable<FloatMenuOption> GetMultiSelectFloatMenuOptions(
            IEnumerable<Pawn> selectedPawns,
            Thing shuttleHost)
        {
            if (selectedPawns == null)
            {
                yield break;
            }

            ThingWithComps host = shuttleHost as ThingWithComps;
            if (host == null)
            {
                yield break;
            }

            ShuttleController controller;
            string failReason;
            if (!TryResolveController(host, out controller, out failReason))
            {
                yield return DisabledOption(failReason);
                yield break;
            }

            List<Pawn> queueablePawns = CollectQueueablePawns(
                controller,
                host,
                selectedPawns,
                out failReason);
            if (queueablePawns.Count == 0)
            {
                yield return DisabledOption(failReason);
                yield break;
            }

            List<Pawn> pawnsForOrder = new List<Pawn>(queueablePawns);
            ThingWithComps hostForOrder = host;
            ShuttleController controllerForOrder = controller;
            yield return new FloatMenuOption(
                "CT_Shuttle_CockpitBoarding_EnterMultiple".Translate(
                    pawnsForOrder.Count).ToString(),
                delegate
                {
                    TryStartCockpitBoarding(
                        controllerForOrder,
                        hostForOrder,
                        pawnsForOrder);
                });
        }

        private static bool TryStartCockpitBoarding(
            ShuttleController controller,
            ThingWithComps host,
            Pawn pawn)
        {
            List<Pawn> pawns = new List<Pawn>();
            pawns.Add(pawn);
            return TryStartCockpitBoarding(controller, host, pawns);
        }

        private static bool TryStartCockpitBoarding(
            ShuttleController controller,
            ThingWithComps host,
            IEnumerable<Pawn> pawns)
        {
            string failReason;
            List<Pawn> queueablePawns = CollectQueueablePawns(
                controller,
                host,
                pawns,
                out failReason);
            if (queueablePawns.Count == 0)
            {
                ShowReject(failReason);
                return false;
            }

            List<TransferableOneWay> passengers = new List<TransferableOneWay>();
            for (int i = 0; i < queueablePawns.Count; i++)
            {
                TransferableOneWay passenger = new TransferableOneWay();
                passenger.things.Add(queueablePawns[i]);
                passenger.AdjustTo(1);
                passengers.Add(passenger);
            }

            ShuttleCommandResult result = controller.Execute(new BeginLoadCargoCommand(
                passengers,
                new List<TransferableOneWay>(),
                false));
            if (result != null && result.Success)
            {
                AssignDraftedBoardingJobs(host, queueablePawns);
            }

            return ShowResult(result);
        }

        private static void AssignDraftedBoardingJobs(
            ThingWithComps host,
            List<Pawn> pawns)
        {
            if (host == null || host.Map == null || pawns == null)
            {
                return;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (!IsPotentialBoardingPawn(pawn) ||
                    !pawn.Drafted ||
                    pawn.Map != host.Map ||
                    pawn.jobs == null)
                {
                    continue;
                }

                Job job = JobMaker.MakeJob(JobDefOf.EnterTransporter, host);
                pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc, false);
            }
        }

        private static bool CanQueuePawnForCockpit(
            ShuttleController controller,
            ThingWithComps host,
            Pawn pawn,
            out string failReason)
        {
            failReason = null;
            if (controller == null)
            {
                failReason = "CT_Shuttle_CockpitBoarding_ControllerUnavailable".Translate().ToString();
                return false;
            }

            if (host == null || host.Map == null)
            {
                failReason = "CT_Shuttle_Command_ShuttleMapUnavailable".Translate().ToString();
                return false;
            }

            if (!IsPotentialBoardingPawn(pawn) || pawn.Map != host.Map)
            {
                failReason = "CT_Shuttle_CockpitBoarding_PawnUnavailable".Translate().ToString();
                return false;
            }

            if (!pawn.CanReach(
                    host,
                    PathEndMode.Touch,
                    Danger.Deadly,
                    false,
                    false,
                    TraverseMode.ByPawn))
            {
                failReason = "CT_Shuttle_CockpitBoarding_PawnUnavailable".Translate().ToString();
                return false;
            }

            ShuttleProfile profile = controller.GetProfileForRead();
            if (profile == null || profile.Command == null || !profile.Command.HasCockpit)
            {
                failReason = "CT_Shuttle_CockpitBoarding_NoCockpit".Translate().ToString();
                return false;
            }

            List<CompTransporter> transporters = TransporterAdapter.ResolveTransportersForLaunch(host);
            if (transporters == null || transporters.Count == 0)
            {
                failReason = "CT_Shuttle_CockpitBoarding_TransporterUnavailable".Translate().ToString();
                return false;
            }

            int activeRegionCount = controller.ActiveCargoRegionCount;
            ShuttleCargoRegionConfigState cargoRegionConfig = controller.CargoRegionConfig;
            if (cargoRegionConfig == null ||
                activeRegionCount <= 0 ||
                !cargoRegionConfig.AllowsAnyActiveRegion(pawn, activeRegionCount))
            {
                failReason = "CT_Shuttle_CockpitBoarding_PawnUnavailable".Translate().ToString();
                return false;
            }

            if (!IsSendableByCurrentTransporterGroup(pawn, transporters, host.Map))
            {
                failReason = "CT_Shuttle_CockpitBoarding_PawnUnavailable".Translate().ToString();
                return false;
            }

            return true;
        }

        private static List<Pawn> CollectQueueablePawns(
            ShuttleController controller,
            ThingWithComps host,
            IEnumerable<Pawn> pawns,
            out string failReason)
        {
            List<Pawn> result = new List<Pawn>();
            HashSet<Pawn> seen = new HashSet<Pawn>();
            failReason = null;
            if (pawns == null)
            {
                failReason = "CT_Shuttle_CockpitBoarding_PawnUnavailable".Translate().ToString();
                return result;
            }

            foreach (Pawn pawn in pawns)
            {
                if (pawn == null || !seen.Add(pawn))
                {
                    continue;
                }

                string pawnFailReason;
                if (CanQueuePawnForCockpit(
                        controller,
                        host,
                        pawn,
                        out pawnFailReason))
                {
                    result.Add(pawn);
                }
                else if (string.IsNullOrEmpty(failReason))
                {
                    failReason = pawnFailReason;
                }
            }

            if (result.Count == 0 && string.IsNullOrEmpty(failReason))
            {
                failReason = "CT_Shuttle_CockpitBoarding_PawnUnavailable".Translate().ToString();
            }

            return result;
        }

        private static bool IsPotentialBoardingPawn(Pawn pawn)
        {
            return pawn != null &&
                !pawn.Destroyed &&
                !pawn.Dead &&
                !pawn.Downed &&
                pawn.Spawned &&
                pawn.Map != null &&
                pawn.MentalState == null &&
                pawn.IsColonistPlayerControlled;
        }

        private static bool IsSendableByCurrentTransporterGroup(
            Pawn pawn,
            List<CompTransporter> transporters,
            Map map)
        {
            if (pawn == null || transporters == null || map == null)
            {
                return false;
            }

            foreach (Pawn sendablePawn in TransporterUtility.AllSendablePawns(transporters, map))
            {
                if (sendablePawn == null)
                {
                    continue;
                }

                if (sendablePawn == pawn ||
                    (sendablePawn.thingIDNumber > 0 &&
                        sendablePawn.thingIDNumber == pawn.thingIDNumber))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryResolveController(
            ThingWithComps host,
            out ShuttleController controller,
            out string failReason)
        {
            controller = null;
            failReason = null;
            CompModularShuttleCore core = host != null ? host.TryGetComp<CompModularShuttleCore>() : null;
            if (core == null || core.Controller == null)
            {
                failReason = "CT_Shuttle_CockpitBoarding_ControllerUnavailable".Translate().ToString();
                return false;
            }

            controller = core.Controller;
            return true;
        }

        private static FloatMenuOption DisabledOption(string reason)
        {
            return new FloatMenuOption(
                string.IsNullOrEmpty(reason)
                    ? "CT_Shuttle_CockpitBoarding_PawnUnavailable".Translate().ToString()
                    : reason,
                null);
        }

        private static bool ShowResult(ShuttleCommandResult result)
        {
            if (result == null)
            {
                ShowReject("CT_Shuttle_Command_ContextUnavailable".Translate().ToString());
                return false;
            }

            if (!string.IsNullOrEmpty(result.Message))
            {
                Messages.Message(
                    result.Message,
                    result.Success ? MessageTypeDefOf.PositiveEvent : MessageTypeDefOf.RejectInput,
                    false);
            }

            return result.Success;
        }

        private static void ShowReject(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                message = "CT_Shuttle_CockpitBoarding_PawnUnavailable".Translate().ToString();
            }

            Messages.Message(message, MessageTypeDefOf.RejectInput, false);
        }
    }
}
