namespace CeleTech.ShuttleExtension.ModularShuttle.Commands
{
    /// <summary>
    /// Requests safe ejection of all Medical Bay patients, or one patient when a thing ID is supplied.
    /// </summary>
    public sealed class EjectMedicalBayPatientsCommand : IShuttleCommand
    {
        public const string ID = "eject-medical-bay-patients";

        public EjectMedicalBayPatientsCommand()
            : this(-1)
        {
        }

        public EjectMedicalBayPatientsCommand(int patientThingID)
        {
            this.PatientThingID = patientThingID;
        }

        public int PatientThingID { get; private set; }

        public string CommandID
        {
            get
            {
                return ID;
            }
        }
    }

    /// <summary>
    /// Requests self-admission of one spawned, mobile patient into the shuttle Medical Bay.
    /// </summary>
    public sealed class AdmitMedicalBayPatientCommand : IShuttleCommand
    {
        public const string ID = "admit-medical-bay-patient";

        public AdmitMedicalBayPatientCommand(int patientThingID)
        {
            this.PatientThingID = patientThingID;
        }

        public int PatientThingID { get; private set; }

        public string CommandID
        {
            get
            {
                return ID;
            }
        }
    }

    /// <summary>
    /// Requests that one spawned, mobile patient be assigned the Medical Bay self-entry job.
    /// The command/service must revalidate the pawn and bay state before ordering the job.
    /// </summary>
    public sealed class AdmitPatientSelfCommand : IShuttleCommand
    {
        public const string ID = "admit-patient-self";

        public AdmitPatientSelfCommand(int patientThingID)
        {
            this.PatientThingID = patientThingID;
        }

        public int PatientThingID { get; private set; }

        public string CommandID
        {
            get
            {
                return ID;
            }
        }
    }

    /// <summary>
    /// Requests that one carrier pawn be assigned the Medical Bay carry-admission job for
    /// one patient. The command/service must revalidate both pawns and the bay state.
    /// </summary>
    public sealed class CarryPatientToMedicalBayCommand : IShuttleCommand
    {
        public const string ID = "carry-patient-to-medical-bay";

        public CarryPatientToMedicalBayCommand(int patientThingID, int carrierThingID)
        {
            this.PatientThingID = patientThingID;
            this.CarrierThingID = carrierThingID;
        }

        public int PatientThingID { get; private set; }

        public int CarrierThingID { get; private set; }

        public string CommandID
        {
            get
            {
                return ID;
            }
        }
    }

    /// <summary>
    /// Requests that one doctor pawn be assigned the Medical Bay tend job for one contained
    /// patient. The service must revalidate the patient, doctor, bay, medicine, and reachability.
    /// </summary>
    public sealed class TreatMedicalBayPatientCommand : IShuttleCommand
    {
        public const string ID = "treat-medical-bay-patient";

        public TreatMedicalBayPatientCommand(
            int patientThingID,
            int doctorThingID,
            bool useAvailableMedicine)
        {
            this.PatientThingID = patientThingID;
            this.DoctorThingID = doctorThingID;
            this.UseAvailableMedicine = useAvailableMedicine;
        }

        public int PatientThingID { get; private set; }

        public int DoctorThingID { get; private set; }

        public bool UseAvailableMedicine { get; private set; }

        public string CommandID
        {
            get
            {
                return ID;
            }
        }
    }

    /// <summary>
    /// Requests cancellation or recovery of an active Medical Bay procedure.
    /// A non-positive procedure ID asks the controller to recover/cancel all active procedure state.
    /// </summary>
    public sealed class CancelMedicalBayProcedureCommand : IShuttleCommand
    {
        public const string ID = "cancel-medical-bay-procedure";

        public CancelMedicalBayProcedureCommand(int procedureID)
        {
            this.ProcedureID = procedureID;
        }

        public int ProcedureID { get; private set; }

        public string CommandID
        {
            get
            {
                return ID;
            }
        }
    }

    /// <summary>
    /// Requests a virtual Medical Bay surgery procedure for one contained patient.
    /// The command starts procedure runtime work; it does not create vanilla bills or jobs.
    /// </summary>
    public sealed class ScheduleMedicalBaySurgeryCommand : IShuttleCommand
    {
        public const string ID = "schedule-medical-bay-surgery";

        public ScheduleMedicalBaySurgeryCommand(
            int patientThingID,
            int doctorThingID,
            string recipeDefName,
            int bodyPartIndex)
        {
            this.PatientThingID = patientThingID;
            this.DoctorThingID = doctorThingID;
            this.RecipeDefName = recipeDefName ?? string.Empty;
            this.BodyPartIndex = bodyPartIndex;
        }

        public int PatientThingID { get; private set; }

        public int DoctorThingID { get; private set; }

        public string RecipeDefName { get; private set; }

        public int BodyPartIndex { get; private set; }

        public string CommandID
        {
            get
            {
                return ID;
            }
        }
    }
}
