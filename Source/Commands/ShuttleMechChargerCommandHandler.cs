using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Commands
{
    /// <summary>
    /// Handles mech charging bay occupancy commands through the charging occupancy service.
    /// </summary>
    internal sealed class ShuttleMechChargerCommandHandler : IShuttleCommandHandler
    {
        private readonly ShuttleMechChargerOccupancyService occupancyService =
            new ShuttleMechChargerOccupancyService();

        public bool CanHandle(IShuttleCommand command)
        {
            return command is EjectMechChargerOccupantCommand;
        }

        public ShuttleCommandResult Execute(IShuttleCommand command, ShuttleCommandContext context)
        {
            if (context == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ContextUnavailable".Translate().ToString());
            }

            EjectMechChargerOccupantCommand ejectMech =
                command as EjectMechChargerOccupantCommand;
            if (ejectMech != null)
            {
                return this.ExecuteEjectMech(context, ejectMech);
            }

            return ShuttleCommandResult.Failed("CT_Shuttle_Command_Unsupported".Translate(command.CommandID).ToString());
        }

        private ShuttleCommandResult ExecuteEjectMech(
            ShuttleCommandContext context,
            EjectMechChargerOccupantCommand command)
        {
            if (command == null || command.MechThingID <= 0)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_MechCharger_InvalidMech".Translate().ToString());
            }

            string failureReason;
            bool hadMech;
            if (!this.occupancyService.TryEjectChargingMech(
                context.Host,
                command.MechThingID,
                out hadMech,
                out failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            return hadMech
                ? ShuttleCommandResult.Succeeded("CT_Shuttle_MechCharger_EjectMech".Translate().ToString())
                : ShuttleCommandResult.Succeeded("CT_Shuttle_MechCharger_NoChargingMechs".Translate().ToString());
        }
    }
}
