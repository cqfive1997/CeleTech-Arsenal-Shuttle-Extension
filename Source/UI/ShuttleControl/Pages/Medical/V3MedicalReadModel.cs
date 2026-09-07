using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Medical
{
    internal enum V3MedicalPatientKind
    {
        Human,
        Animal,
        Mechanoid,
        Other
    }

    internal enum V3MedicalOccupantKind
    {
        Patient,
        Doctor
    }

    internal sealed class V3MedicalPageReadModel
    {
        internal bool HasMedicalBay;
        internal bool MedicalBayInstalled;
        internal bool MedicalBayEnabled;
        internal bool MedicalBayPowered;
        internal bool MedicalBayOffline;
        internal bool MedicalBayFull;
        internal bool MedicalBayMedicineLow;
        internal bool MedicalBayHasPendingTreatment;
        internal bool MedicalBayPoweredKnown;
        internal bool MedicalBayEnabledKnown;
        internal string MedicalBayStateKey;
        internal int PatientCount;
        internal int OccupiedBeds;
        internal int TotalBeds;
        internal int FreeBeds;
        internal int CriticalPatientCount;
        internal int PendingTreatmentCount;
        internal int AvailableMedicineCount;
        internal int EstimatedMedicineNeed;
        internal string MedicineStateText;
        internal string MedicalBayStateText;
        internal bool CanOpenAdmissionDialog;
        internal string AdmissionBlockedReason;
        internal bool ModuleStatusPlaceholder;
        internal bool PowerStatusPlaceholder = true;
        internal bool HasActiveProcedure;
        internal string ActiveProcedureType;
        internal string ActiveProcedureStatus;
        internal int ActiveProcedureID;
        internal int ActiveProcedureDoctorThingID;
        internal int ActiveProcedurePatientThingID;
        internal string ActiveProcedureDoctorLabel;
        internal string ActiveProcedurePatientLabel;
        internal int ActiveProcedureWorkTicksDone;
        internal int ActiveProcedureWorkTicksTotal;
        internal float ActiveProcedureProgress;
        internal int ActiveProcedureTendCyclesCompleted;
        internal int ActiveProcedureLastKnownRemainingTendableCount;
        internal string ActiveProcedureStatusLabel;
        internal string ActiveProcedureTooltip;
        internal readonly List<V3MedicalSummaryMetricModel> SummaryMetrics =
            new List<V3MedicalSummaryMetricModel>();
        internal readonly List<V3MedicalPatientCardModel> Patients =
            new List<V3MedicalPatientCardModel>();
        internal readonly List<V3MedicalOccupantCardModel> Occupants =
            new List<V3MedicalOccupantCardModel>();
        internal readonly List<V3MedicalAdmissionCandidateModel> AdmissionCandidates =
            new List<V3MedicalAdmissionCandidateModel>();
    }

    internal sealed class V3MedicalSummaryMetricModel
    {
        internal string Label;
        internal string ShortLabel;
        internal string Value;
        internal string Tooltip;
        internal string SeverityKey;
    }

    internal sealed class V3MedicalPatientCardModel
    {
        internal int PatientThingID;
        internal string PatientKey;
        internal string Label;
        internal Thing DisplayThing;
        internal V3MedicalPatientKind Kind;
        internal string KindKey;
        internal string KindFallbackLabel;
        internal string StateKey;
        internal string StateFallbackLabel;
        internal string TriageKey;
        internal string TriageFallbackLabel;
        internal string LocationText;
        internal string MedicineNeedText;
        internal bool IsActiveProcedurePatient;
        internal bool TreatmentBlockedByActiveProcedure;
        internal string ActiveProcedureStatusLabel;
        internal string ActiveProcedureTooltip;
        internal bool IsDowned;
        internal bool NeedsTreatment;
        internal int PendingTreatmentCount;
        internal float PainPct = -1f;
        internal float ConsciousnessPct = -1f;
        internal float BleedSeverityPct = -1f;
        internal float MovingPct = -1f;
        internal float FoodPct = -1f;
        internal float DurabilityPct = -1f;
        internal float DamagePct = -1f;
        internal float EnergyPct = -1f;
        internal bool HasMechDurability;
        internal bool HasMechEnergy;
        internal readonly List<V3MedicalVitalLineModel> Vitals =
            new List<V3MedicalVitalLineModel>();
        internal readonly List<V3MedicalDetailLineModel> Details =
            new List<V3MedicalDetailLineModel>();
        internal readonly List<V3MedicalSurgeryOptionModel> SurgeryOptions =
            new List<V3MedicalSurgeryOptionModel>();
    }

    internal sealed class V3MedicalOccupantCardModel
    {
        internal V3MedicalOccupantKind Kind;
        internal int PawnThingID;
        internal string PawnKey;
        internal string Label;
        internal Thing DisplayThing;
        internal string RoleLabel;
        internal string CurrentStatusLabel;
        internal string ActivityLabel;
        internal bool CanSelectAsPatient;
        internal bool CanEjectFromMedicalBay;
        internal bool IsActiveDoctor;
        internal int TreatingPatientThingID;
        internal string TreatingPatientLabel;
        internal V3MedicalPatientCardModel Patient;
        internal readonly List<V3MedicalVitalLineModel> Vitals =
            new List<V3MedicalVitalLineModel>();
    }

    internal sealed class V3MedicalVitalLineModel
    {
        internal string Label;
        internal float Value01 = -1f;
        internal string ValueText;
        internal string SeverityKey;
    }

    internal sealed class V3MedicalDetailLineModel
    {
        internal string Label;
        internal string Summary;
        internal bool PendingTreatment;
        internal bool IsPlaceholder;
        internal string SeverityKey;
    }

    internal sealed class V3MedicalSurgeryOptionModel
    {
        internal string RecipeDefName;
        internal string Label;
        internal string Description;
        internal int BodyPartIndex;
        internal string BodyPartLabel;
        internal bool CanSchedule;
        internal string DisabledReason;
        internal int WorkTicks;
        internal string RequiredMaterialsSummary;
        internal string MissingMaterialsSummary;
        internal bool HasMissingMaterials;
    }

    internal sealed class V3MedicalAdmissionCandidateModel
    {
        internal int PawnThingID;
        internal string Id = string.Empty;
        internal string Label = string.Empty;
        internal Thing DisplayThing;
        internal string KindKey = string.Empty;
        internal string KindLabel = string.Empty;
        internal string TriageKey = string.Empty;
        internal string TriageLabel = string.Empty;
        internal string AdmissionModeKey = "Pending";
        internal string AdmissionModeLabel = string.Empty;
        internal string ReasonText = string.Empty;
        internal bool CanAdmit;
        internal bool NeedsCarry;
        internal bool IsAlreadyInMedicalBay;
        internal string Tooltip = string.Empty;
        internal readonly List<V3MedicalAdmissionCarrierModel> CarryCandidates =
            new List<V3MedicalAdmissionCarrierModel>();
    }

    internal sealed class V3MedicalAdmissionCarrierModel
    {
        internal int PawnThingID;
        internal string Id = string.Empty;
        internal string Label = string.Empty;
        internal Thing DisplayThing;
        internal bool CanCarry;
        internal string StatusText = string.Empty;
        internal string ReasonText = string.Empty;
        internal string Tooltip = string.Empty;
    }
}
