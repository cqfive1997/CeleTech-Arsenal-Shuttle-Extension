using CeleTech.ShuttleExtension.ModularShuttle.Medical;

namespace CeleTech.ShuttleExtension.ModularShuttle.AI.MedicalBay
{
    /// <summary>
    /// AI-facing Medical Bay constants. Shared admission validation lives in
    /// MedicalBayAdmissionValidator so Comps do not depend on the AI/Job namespace.
    /// </summary>
    internal static class MedicalBayUtility
    {
        internal const string EnterMedicalBayJobDefName =
            MedicalBayAdmissionValidator.EnterMedicalBayJobDefName;

        internal const string CarryPatientToMedicalBayJobDefName =
            MedicalBayAdmissionValidator.CarryPatientToMedicalBayJobDefName;

        internal const string TendMedicalBayPatientJobDefName =
            MedicalBayAdmissionValidator.TendMedicalBayPatientJobDefName;

        internal const string TendMedicalBayPatientWithMedicineJobDefName =
            MedicalBayAdmissionValidator.TendMedicalBayPatientWithMedicineJobDefName;
    }
}
