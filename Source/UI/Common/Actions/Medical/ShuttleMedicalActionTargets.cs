using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Medical
{
    internal enum ShuttleMedicalActionPatientKind
    {
        Human,
        Animal,
        Mechanoid,
        Other
    }

    internal sealed class ShuttleMedicalPatientActionTarget
    {
        internal int PatientThingID;
        internal string PatientKey;
        internal string Label;
        internal Thing DisplayThing;
        internal ShuttleMedicalActionPatientKind Kind;
        internal string KindKey;
        internal string KindFallbackLabel;
        internal string StateKey;
        internal string StateFallbackLabel;
        internal string TriageKey;
        internal string TriageFallbackLabel;
        internal bool IsActiveProcedurePatient;
        internal bool TreatmentBlockedByActiveProcedure;
        internal int ActiveProcedureID;
        internal string ActiveProcedureStatus;
        internal string ActiveProcedureStatusLabel;
        internal string ActiveProcedureTooltip;
        internal bool NeedsTreatment;
        internal int PendingTreatmentCount;
        internal bool CanEjectFromMedicalBay = true;
        internal readonly List<ShuttleMedicalSurgeryOptionActionTarget> SurgeryOptions =
            new List<ShuttleMedicalSurgeryOptionActionTarget>();
    }

    internal sealed class ShuttleMedicalPageActionContext
    {
        internal bool HasMedicalBay;
        internal bool MedicalBayInstalled;
        internal bool MedicalBayEnabled;
        internal bool MedicalBayPowered;
        internal bool MedicalBayFull;
        internal bool CanOpenAdmissionDialog;
        internal string AdmissionBlockedReason;
        internal int PatientCount;
        internal int OccupiedBeds;
        internal int TotalBeds;
        internal int FreeBeds;
        internal bool HasActiveProcedure;
        internal string ActiveProcedureType;
        internal string ActiveProcedureStatus;
        internal int ActiveProcedureID;
        internal int ActiveProcedureDoctorThingID;
        internal int ActiveProcedurePatientThingID;
        internal string ActiveProcedureDoctorLabel;
        internal string ActiveProcedurePatientLabel;
        internal string ActiveProcedureStatusLabel;
        internal string ActiveProcedureTooltip;
        internal readonly List<ShuttleMedicalPatientActionTarget> Patients =
            new List<ShuttleMedicalPatientActionTarget>();
        internal readonly List<ShuttleMedicalAdmissionCandidateActionTarget> AdmissionCandidates =
            new List<ShuttleMedicalAdmissionCandidateActionTarget>();
    }

    internal sealed class ShuttleMedicalProcedureActionTarget
    {
        internal bool HasActiveProcedure;
        internal string ActiveProcedureType;
        internal string ActiveProcedureStatus;
        internal int ActiveProcedureID;
        internal int PatientThingID;
        internal int DoctorThingID;
        internal string PatientLabel;
        internal string DoctorLabel;
        internal string StatusLabel;
        internal string Tooltip;
    }

    internal sealed class ShuttleMedicalSurgeryOptionActionTarget
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

    internal sealed class ShuttleMedicalAdmissionCandidateActionTarget
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
        internal readonly List<ShuttleMedicalAdmissionCarrierActionTarget> CarryCandidates =
            new List<ShuttleMedicalAdmissionCarrierActionTarget>();
    }

    internal sealed class ShuttleMedicalAdmissionCarrierActionTarget
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
