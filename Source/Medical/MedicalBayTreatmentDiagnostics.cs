using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Medical
{
    internal sealed class MedicalBayTreatmentDiagnostics
    {
        internal string AppendNotice(string message, string notice)
        {
            if (string.IsNullOrEmpty(notice))
            {
                return message;
            }

            if (string.IsNullOrEmpty(message))
            {
                return notice;
            }

            return message + " " + notice;
        }

        internal string GetMedicineLabel(Medicine medicine)
        {
            return medicine != null && medicine.def != null
                ? medicine.def.LabelCap.ToString()
                : "CT_Shuttle_MedicalBay_DoctorInventoryMedicine".Translate().ToString();
        }

        internal string BuildLoadedCargoMedicineSpikeTrace(
            IShuttleMedicalSupplyWithdrawal withdrawal,
            Medicine medicine,
            Pawn patient,
            CompShuttleMedicalBayOccupancy occupancy)
        {
            bool patientHeldAfter = patient != null &&
                occupancy != null &&
                occupancy.ContainsPatient(patient);
            bool patientSpawnedAfter = patient != null && patient.Spawned;
            return "medicineDef=" +
                (withdrawal != null ? withdrawal.MedicineDefName : (medicine != null && medicine.def != null ? medicine.def.defName : "null")) +
                " source=" +
                (withdrawal != null ? withdrawal.SourceLabel : "null") +
                " sourceThingID=" +
                (withdrawal != null ? withdrawal.SourceThingID.ToString() : "-1") +
                " takenThingID=" +
                (withdrawal != null ? withdrawal.TakenThingID.ToString() : (medicine != null ? medicine.thingIDNumber.ToString() : "-1")) +
                " sourceStackBefore=" +
                (withdrawal != null ? withdrawal.SourceStackCountBefore.ToString() : "-1") +
                " sourceStackAfter=" +
                (withdrawal != null ? withdrawal.SourceStackCountCurrent.ToString() : "-1") +
                " medicineStackAfter=" +
                (medicine != null ? medicine.stackCount.ToString() : "-1") +
                " medicineDestroyed=" +
                (medicine == null || medicine.Destroyed) +
                " patientHeldAfter=" +
                patientHeldAfter +
                " patientSpawnedAfter=" +
                patientSpawnedAfter;
        }
    }

    internal struct MedicineSpikeTrace
    {
        private readonly string medicineDefName;
        private readonly int stackCountBefore;
        private readonly bool destroyedBefore;
        private int stackCountAfter;
        private bool destroyedAfter;
        private bool patientHeldAfter;
        private bool patientSpawnedAfter;

        internal MedicineSpikeTrace(Medicine medicine)
        {
            this.medicineDefName = medicine != null && medicine.def != null
                ? medicine.def.defName
                : "null";
            this.stackCountBefore = medicine != null ? medicine.stackCount : -1;
            this.destroyedBefore = medicine == null || medicine.Destroyed;
            this.stackCountAfter = -1;
            this.destroyedAfter = medicine == null || medicine.Destroyed;
            this.patientHeldAfter = false;
            this.patientSpawnedAfter = false;
        }

        internal void RecordAfter(
            Medicine medicine,
            Pawn patient,
            CompShuttleMedicalBayOccupancy occupancy)
        {
            this.stackCountAfter = medicine != null ? medicine.stackCount : -1;
            this.destroyedAfter = medicine == null || medicine.Destroyed;
            this.patientHeldAfter = patient != null &&
                occupancy != null &&
                occupancy.ContainsPatient(patient);
            this.patientSpawnedAfter = patient != null && patient.Spawned;
        }

        internal string ToLogString()
        {
            return "medicineDef=" +
                this.medicineDefName +
                " stackBefore=" +
                this.stackCountBefore +
                " stackAfter=" +
                this.stackCountAfter +
                " destroyedBefore=" +
                this.destroyedBefore +
                " destroyedAfter=" +
                this.destroyedAfter +
                " patientHeldAfter=" +
                this.patientHeldAfter +
                " patientSpawnedAfter=" +
                this.patientSpawnedAfter;
        }
    }
}
