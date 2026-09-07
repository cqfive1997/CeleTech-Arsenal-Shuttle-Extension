using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Medical;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Presentation
{
    /// <summary>
    /// UI-facing Medical Bay snapshot. It exposes static capability and patient status,
    /// but never exposes holders or mutable patient records.
    /// </summary>
    public sealed class ShuttleMedicalBayReadModel
    {
        public bool HasMedicalBay;
        public bool MedicalBayPoweredKnown;
        public bool MedicalBayPowered;
        public int MedicalPatientSlots;
        public int PatientCount;
        public int FreePatientSlots;
        public bool HasPatients;
        public bool SupportsPassiveComfort;
        public float PassiveJoyCapPct = -1f;
        public int LoadedCargoMedicineStackCount;
        public int LoadedCargoMedicineTotalCount;
        public int LoadedCargoMedicineAllowedCount;
        public string LoadedCargoMedicineSummaryLabel;
        public bool HasActiveProcedure;
        public string ActiveProcedureType;
        public string ActiveProcedureStatus;
        public int ActiveProcedureID;
        public int ActiveProcedureDoctorThingID;
        public int ActiveProcedurePatientThingID;
        public string ActiveProcedureDoctorLabel;
        public string ActiveProcedurePatientLabel;
        public Thing ActiveProcedureDoctorDisplayThing;
        public int ActiveProcedureWorkTicksDone;
        public int ActiveProcedureWorkTicksTotal;
        public float ActiveProcedureProgress;
        public int ActiveProcedureTendCyclesCompleted;
        public int ActiveProcedureLastKnownRemainingTendableCount;
        public string ActiveProcedureStatusLabel;
        public string ActiveProcedureTooltip;
        public IReadOnlyList<ShuttleMedicineSupplyReadModel> LoadedCargoMedicine =
            new List<ShuttleMedicineSupplyReadModel>();
        public IReadOnlyList<ShuttleMedicalPatientReadModel> Patients =
            new List<ShuttleMedicalPatientReadModel>();
        public IReadOnlyList<ShuttleMedicalAdmissionCandidateReadModel> AdmissionCandidates =
            new List<ShuttleMedicalAdmissionCandidateReadModel>();
    }

    public sealed class ShuttleMedicalPatientReadModel
    {
        public int PawnThingID;
        public string PawnLabel;
        public Thing DisplayThing;
        public bool IsDowned;
        public bool IsConscious;
        public bool IsConsciousKnown;
        public float ConsciousnessPct = -1f;
        public float PainPct = -1f;
        public bool IsBleeding;
        public string BleedingLabel;
        public bool HasMedicalEmergency;
        public bool NeedsTend;
        public bool HasUntendedInjury;
        public bool HasTendedInjury;
        public int TendableHediffCount;
        public int UntendedHediffCount;
        public int TendedHediffCount;
        public int BleedingHediffCount;
        public float BleedRateTotal;
        public bool HasInfectionLikeHediff;
        public string MedicalSummaryLabel;
        public string TendSummaryLabel;
        public string InfectionSummaryLabel;
        public string MedicineNeedLabel;
        public bool IsActiveProcedurePatient;
        public string ActiveProcedureStatusLabel;
        public string ActiveProcedureTooltip;
        public int LoadedCargoMedicineAllowedCount;
        public string LoadedCargoMedicineSummaryLabel;
        public IReadOnlyList<ShuttleMedicineSupplyReadModel> LoadedCargoMedicine =
            new List<ShuttleMedicineSupplyReadModel>();
        public IReadOnlyList<ShuttleMedicalSurgeryOptionReadModel> SurgeryOptions =
            new List<ShuttleMedicalSurgeryOptionReadModel>();
        public List<ShuttleMedicalHediffReadModel> Hediffs =
            new List<ShuttleMedicalHediffReadModel>();
        public float FoodPct = -1f;
        public float RestPct = -1f;
        public float JoyPct = -1f;
        public float MoodPct = -1f;
        public bool HasMechDurability;
        public float DurabilityPct = -1f;
        public float DamagePct = -1f;
        public bool HasMechEnergy;
        public float EnergyPct = -1f;
        public string AdmissionMode;
        public int AdmissionTick = -1;
        public bool SupportsPassiveComfort;
        public bool PassiveComfortActive;
        public string PassiveComfortInactiveReason;
        public bool PassiveJoyActive;
        public string PassiveJoyInactiveReason;
        public bool PassiveMoodComfortActive;
        public string PassiveMoodComfortInactiveReason;
        public float PassiveJoyCapPct = -1f;
    }

    public sealed class ShuttleMedicalHediffReadModel
    {
        public string Label;
        public string PartLabel;
        public bool Bleeding;
        public bool Tendable;
        public bool Tended;
        public float Severity;
        public float BleedRate;
        public string SummaryLabel;
    }

    public sealed class ShuttleMedicalSurgeryOptionReadModel
    {
        public string RecipeDefName;
        public string Label;
        public string Description;
        public int BodyPartIndex;
        public string BodyPartLabel;
        public bool CanSchedule;
        public string DisabledReason;
        public int WorkTicks;
        public string RequiredMaterialsSummary;
        public string MissingMaterialsSummary;
        public bool HasMissingMaterials;
    }

    public sealed class ShuttleMedicalAdmissionCandidateReadModel
    {
        public int PawnThingID;
        public string Id = string.Empty;
        public string Label = string.Empty;
        public Thing DisplayThing;
        public string KindKey = string.Empty;
        public string KindLabel = string.Empty;
        public string TriageKey = string.Empty;
        public string TriageLabel = string.Empty;
        public string AdmissionModeKey = "Pending";
        public string AdmissionModeLabel = string.Empty;
        public string ReasonText = string.Empty;
        public bool CanAdmit;
        public bool NeedsCarry;
        public bool IsAlreadyInMedicalBay;
        public string Tooltip = string.Empty;
        public IReadOnlyList<ShuttleMedicalAdmissionCarrierReadModel> CarryCandidates =
            new List<ShuttleMedicalAdmissionCarrierReadModel>();
    }

    public sealed class ShuttleMedicalAdmissionCarrierReadModel
    {
        public int PawnThingID;
        public string Id = string.Empty;
        public string Label = string.Empty;
        public Thing DisplayThing;
        public bool CanCarry;
        public string StatusText = string.Empty;
        public string ReasonText = string.Empty;
        public string Tooltip = string.Empty;
    }
}
