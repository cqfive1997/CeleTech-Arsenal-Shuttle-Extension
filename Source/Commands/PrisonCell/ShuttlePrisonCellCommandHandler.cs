using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Prisoners;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Commands
{
    internal sealed class ShuttlePrisonCellCommandHandler : IShuttleCommandHandler
    {
        private readonly ShuttlePrisonerOperationService operationService =
            new ShuttlePrisonerOperationService();
        private readonly ShuttlePrisonerFeedingService feedingService =
            new ShuttlePrisonerFeedingService();
        private readonly ShuttlePrisonerTreatmentService treatmentService =
            new ShuttlePrisonerTreatmentService();

        public bool CanHandle(IShuttleCommand command)
        {
            return command is CarryPrisonerToShuttleCellCommand ||
                command is EjectShuttlePrisonerCommand ||
                command is FeedShuttlePrisonerCommand ||
                command is TendShuttlePrisonerCommand ||
                command is SetShuttlePrisonCellSupplyPolicyCommand;
        }

        public ShuttleCommandResult Execute(IShuttleCommand command, ShuttleCommandContext context)
        {
            if (context == null || context.Host == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ContextUnavailable".Translate().ToString());
            }

            CarryPrisonerToShuttleCellCommand carry = command as CarryPrisonerToShuttleCellCommand;
            if (carry != null)
            {
                return this.ExecuteCarryPrisoner(context, carry);
            }

            EjectShuttlePrisonerCommand eject = command as EjectShuttlePrisonerCommand;
            if (eject != null)
            {
                return this.ExecuteEjectPrisoner(context, eject);
            }

            FeedShuttlePrisonerCommand feed = command as FeedShuttlePrisonerCommand;
            if (feed != null)
            {
                return this.ExecuteFeedPrisoner(context, feed);
            }

            TendShuttlePrisonerCommand tend = command as TendShuttlePrisonerCommand;
            if (tend != null)
            {
                return this.ExecuteTendPrisoner(context, tend);
            }

            SetShuttlePrisonCellSupplyPolicyCommand setSupplyPolicy =
                command as SetShuttlePrisonCellSupplyPolicyCommand;
            if (setSupplyPolicy != null)
            {
                return this.ExecuteSetSupplyPolicy(context, setSupplyPolicy);
            }

            return ShuttleCommandResult.Failed("CT_Shuttle_Command_Unsupported".Translate(command.CommandID).ToString());
        }

        private ShuttleCommandResult ExecuteCarryPrisoner(
            ShuttleCommandContext context,
            CarryPrisonerToShuttleCellCommand command)
        {
            if (command == null || command.PrisonerThingID <= 0 || command.CarrierThingID <= 0)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_PrisonCell_InvalidPrisoner".Translate().ToString());
            }

            Map map = context.Host.Map;
            Pawn carrier = this.FindSpawnedPawnByThingID(map, command.CarrierThingID);
            Pawn prisoner = this.FindSpawnedPawnByThingID(map, command.PrisonerThingID);
            string failReason;
            if (!this.operationService.TryAssignCarryPrisonerJob(
                context.Host,
                carrier,
                prisoner,
                out failReason))
            {
                return ShuttleCommandResult.Failed(failReason);
            }

            return ShuttleCommandResult.Succeeded("CT_Shuttle_PrisonCell_CarryJobAssigned".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteEjectPrisoner(
            ShuttleCommandContext context,
            EjectShuttlePrisonerCommand command)
        {
            bool hadPrisoner;
            string failReason;
            if (!this.operationService.TryEjectPrisonerByThingID(
                context.Host,
                command != null ? command.PrisonerThingID : -1,
                out hadPrisoner,
                out failReason))
            {
                return ShuttleCommandResult.Failed(failReason);
            }

            return ShuttleCommandResult.Succeeded(
                hadPrisoner
                    ? "CT_Shuttle_PrisonCell_EjectSucceeded".Translate().ToString()
                    : "CT_Shuttle_PrisonCell_NoPrisoners".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteFeedPrisoner(
            ShuttleCommandContext context,
            FeedShuttlePrisonerCommand command)
        {
            if (command == null || command.PrisonerThingID <= 0 || command.FeederThingID <= 0)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_PrisonCell_InvalidPrisoner".Translate().ToString());
            }

            Map map = context.Host.Map;
            Pawn feeder = this.FindSpawnedPawnByThingID(map, command.FeederThingID);
            string failReason;
            if (!this.feedingService.TryAssignFeedPrisonerJob(
                context.Host,
                feeder,
                command.PrisonerThingID,
                out failReason))
            {
                return ShuttleCommandResult.Failed(failReason);
            }

            return ShuttleCommandResult.Succeeded("CT_Shuttle_PrisonCell_FeedJobAssigned".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteTendPrisoner(
            ShuttleCommandContext context,
            TendShuttlePrisonerCommand command)
        {
            if (command == null || command.PrisonerThingID <= 0 || command.DoctorThingID <= 0)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_PrisonCell_InvalidPrisoner".Translate().ToString());
            }

            Map map = context.Host.Map;
            Pawn doctor = this.FindSpawnedPawnByThingID(map, command.DoctorThingID);
            string failReason;
            if (!this.treatmentService.TryAssignTendPrisonerJob(
                context.Host,
                doctor,
                command.PrisonerThingID,
                out failReason))
            {
                return ShuttleCommandResult.Failed(failReason);
            }

            return ShuttleCommandResult.Succeeded("CT_Shuttle_PrisonCell_TendJobAssigned".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteSetSupplyPolicy(
            ShuttleCommandContext context,
            SetShuttlePrisonCellSupplyPolicyCommand command)
        {
            if (context == null || context.AssemblyState == null || command == null)
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_Command_AssemblyUnavailable".Translate().ToString());
            }

            if (!ShuttlePrisonCellSupplyConfigState.IsSupportedMaximumFoodPreferability(
                command.MaximumFoodPreferability))
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_PrisonSupply_InvalidFoodTier".Translate().ToString());
            }

            ShuttlePrisonCellSupplyConfigState config =
                context.AssemblyState.PrisonCellSupplyConfig;
            if (config == null)
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_Command_AssemblyUnavailable".Translate().ToString());
            }

            config.Set(
                command.CargoFoodSupplyEnabled,
                command.RefrigeratedFoodSupplyEnabled,
                command.MaximumFoodPreferability);
            context.MarkProfileDirty(ProfileDirtyReason.AssemblyChanged);
            return ShuttleCommandResult.Succeeded(
                "CT_Shuttle_PrisonSupply_Applied".Translate().ToString(),
                true);
        }

        private Pawn FindSpawnedPawnByThingID(Map map, int thingID)
        {
            if (map == null || map.mapPawns == null || thingID <= 0)
            {
                return null;
            }

            System.Collections.Generic.IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            if (pawns == null)
            {
                return null;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn != null && pawn.thingIDNumber == thingID)
                {
                    return pawn;
                }
            }

            return null;
        }
    }
}
