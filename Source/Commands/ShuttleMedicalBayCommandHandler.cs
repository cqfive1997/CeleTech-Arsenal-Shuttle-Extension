using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Medical;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Commands
{
    /// <summary>
    /// Handles Medical Bay occupancy commands through the Medical Bay service boundary.
    /// </summary>
    internal sealed class ShuttleMedicalBayCommandHandler : IShuttleCommandHandler
    {
        private readonly ShuttleMedicalBayOccupancyService occupancyService =
            new ShuttleMedicalBayOccupancyService();
        private readonly MedicalBayTreatmentService treatmentService =
            new MedicalBayTreatmentService();

        public bool CanHandle(IShuttleCommand command)
        {
            return command is EjectMedicalBayPatientsCommand ||
                command is AdmitMedicalBayPatientCommand ||
                command is AdmitPatientSelfCommand ||
                command is CarryPatientToMedicalBayCommand ||
                command is TreatMedicalBayPatientCommand ||
                command is CancelMedicalBayProcedureCommand ||
                command is ScheduleMedicalBaySurgeryCommand;
        }

        public ShuttleCommandResult Execute(IShuttleCommand command, ShuttleCommandContext context)
        {
            if (context == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ContextUnavailable".Translate().ToString());
            }

            EjectMedicalBayPatientsCommand ejectPatients = command as EjectMedicalBayPatientsCommand;
            if (ejectPatients != null)
            {
                return this.ExecuteEjectMedicalBayPatients(context, ejectPatients);
            }

            AdmitMedicalBayPatientCommand admitPatient = command as AdmitMedicalBayPatientCommand;
            if (admitPatient != null)
            {
                return this.ExecuteAdmitMedicalBayPatient(context, admitPatient);
            }

            AdmitPatientSelfCommand selfAdmitPatient = command as AdmitPatientSelfCommand;
            if (selfAdmitPatient != null)
            {
                return this.ExecuteAdmitPatientSelf(context, selfAdmitPatient);
            }

            CarryPatientToMedicalBayCommand carryPatient = command as CarryPatientToMedicalBayCommand;
            if (carryPatient != null)
            {
                return this.ExecuteCarryPatientToMedicalBay(context, carryPatient);
            }

            TreatMedicalBayPatientCommand treatPatient = command as TreatMedicalBayPatientCommand;
            if (treatPatient != null)
            {
                return this.ExecuteTreatMedicalBayPatient(context, treatPatient);
            }

            CancelMedicalBayProcedureCommand cancelProcedure = command as CancelMedicalBayProcedureCommand;
            if (cancelProcedure != null)
            {
                return this.ExecuteCancelMedicalBayProcedure(context, cancelProcedure);
            }

            ScheduleMedicalBaySurgeryCommand scheduleSurgery = command as ScheduleMedicalBaySurgeryCommand;
            if (scheduleSurgery != null)
            {
                return this.ExecuteScheduleMedicalBaySurgery(context, scheduleSurgery);
            }

            return ShuttleCommandResult.Failed("CT_Shuttle_Command_Unsupported".Translate(command.CommandID).ToString());
        }

        private ShuttleCommandResult ExecuteEjectMedicalBayPatients(
            ShuttleCommandContext context,
            EjectMedicalBayPatientsCommand command)
        {
            string failureReason;
            bool hadPatients;
            if (!this.occupancyService.TryEjectMedicalBayPatients(
                context.Host,
                command.PatientThingID,
                out hadPatients,
                out failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            if (!hadPatients)
            {
                return ShuttleCommandResult.Succeeded("CT_Shuttle_MedicalBay_NoPatients".Translate().ToString());
            }

            if (command.PatientThingID > 0)
            {
                return ShuttleCommandResult.Succeeded("CT_Shuttle_MedicalBay_EjectPatient".Translate().ToString());
            }

            return hadPatients
                ? ShuttleCommandResult.Succeeded("CT_Shuttle_MedicalBay_EjectAll".Translate().ToString())
                : ShuttleCommandResult.Succeeded("CT_Shuttle_MedicalBay_NoPatients".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteAdmitMedicalBayPatient(
            ShuttleCommandContext context,
            AdmitMedicalBayPatientCommand command)
        {
            if (command == null || command.PatientThingID <= 0)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_MedicalBay_InvalidPatient".Translate().ToString());
            }

            string failureReason;
            if (!this.occupancyService.TryAdmitSelfPatientByThingID(
                context.Host,
                command.PatientThingID,
                out failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            return ShuttleCommandResult.Succeeded("CT_Shuttle_MedicalBay_AdmitSucceeded".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteAdmitPatientSelf(
            ShuttleCommandContext context,
            AdmitPatientSelfCommand command)
        {
            if (command == null || command.PatientThingID <= 0)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_MedicalBay_InvalidPatient".Translate().ToString());
            }

            string failureReason;
            if (!this.occupancyService.TryAssignSelfAdmissionJobByThingID(
                context.Host,
                command.PatientThingID,
                out failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            return ShuttleCommandResult.Succeeded("CT_Shuttle_MedicalBay_Enter".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteCarryPatientToMedicalBay(
            ShuttleCommandContext context,
            CarryPatientToMedicalBayCommand command)
        {
            if (command == null || command.PatientThingID <= 0 || command.CarrierThingID <= 0)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_MedicalBay_InvalidPatient".Translate().ToString());
            }

            string failureReason;
            if (!this.occupancyService.TryAssignCarryAdmissionJobByThingID(
                context.Host,
                command.PatientThingID,
                command.CarrierThingID,
                out failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            return ShuttleCommandResult.Succeeded("CT_Shuttle_MedicalBay_CarryPatient".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteTreatMedicalBayPatient(
            ShuttleCommandContext context,
            TreatMedicalBayPatientCommand command)
        {
            if (command == null || command.PatientThingID <= 0 || command.DoctorThingID <= 0)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_MedicalBay_InvalidPatient".Translate().ToString());
            }

            string failureReason;
            if (!this.treatmentService.TryAssignTendJobByThingID(
                context.Host,
                command.PatientThingID,
                command.DoctorThingID,
                command.UseAvailableMedicine,
                out failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            return ShuttleCommandResult.Succeeded("CT_Shuttle_MedicalBay_TendingInProgress".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteCancelMedicalBayProcedure(
            ShuttleCommandContext context,
            CancelMedicalBayProcedureCommand command)
        {
            if (command == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_MedicalProcedure_CancelFailed".Translate().ToString());
            }

            string message;
            if (!context.TryCancelMedicalBayProcedure(command.ProcedureID, out message))
            {
                return ShuttleCommandResult.Failed(
                    !string.IsNullOrEmpty(message)
                        ? message
                        : "CT_Shuttle_MedicalProcedure_CancelFailed".Translate().ToString());
            }

            return ShuttleCommandResult.Succeeded(
                !string.IsNullOrEmpty(message)
                    ? message
                    : "CT_Shuttle_MedicalProcedure_Cancelled".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteScheduleMedicalBaySurgery(
            ShuttleCommandContext context,
            ScheduleMedicalBaySurgeryCommand command)
        {
            if (command == null ||
                command.PatientThingID <= 0 ||
                command.DoctorThingID <= 0 ||
                string.IsNullOrEmpty(command.RecipeDefName))
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_MedicalSurgery_InvalidRecipe".Translate().ToString());
            }

            Pawn doctor = this.FindSpawnedPawnByThingID(context.Host != null ? context.Host.Map : null, command.DoctorThingID);
            if (doctor == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_MedicalBay_DoctorUnavailable".Translate().ToString());
            }

            string message;
            if (!context.TryStartMedicalBaySurgeryProcedure(
                doctor,
                command.PatientThingID,
                command.RecipeDefName,
                command.BodyPartIndex,
                out message))
            {
                return ShuttleCommandResult.Failed(
                    !string.IsNullOrEmpty(message)
                        ? message
                        : "CT_Shuttle_MedicalSurgery_Failed".Translate().ToString());
            }

            return ShuttleCommandResult.Succeeded(
                !string.IsNullOrEmpty(message)
                    ? message
                    : "CT_Shuttle_MedicalSurgery_Started".Translate().ToString());
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
