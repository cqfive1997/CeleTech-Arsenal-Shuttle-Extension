using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Medical;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using RimWorld;
using Verse;
using Verse.AI;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    public sealed class CompProperties_ShuttleMedicalBayOccupancy : CompProperties
    {
        public CompProperties_ShuttleMedicalBayOccupancy()
        {
            this.compClass = typeof(CompShuttleMedicalBayOccupancy);
        }
    }

    /// <summary>
    /// Dedicated holder for pawns admitted into shuttle Medical Bay modules.
    /// This comp owns containment, records, save/load reconciliation, and safe ejection.
    /// Passive comfort is limited to the explicit Medical Bay M4 utility; doctor-assisted tending
    /// reservations are tracked here, but hediff mutation remains centralized in treatment services.
    /// </summary>
    public sealed partial class CompShuttleMedicalBayOccupancy : ThingComp, IThingHolder
    {
        private const int ReservationCleanupIntervalTicks = 120;
        private const int PatientRecordReconcileIntervalTicks = 250;
        private const int InvalidPatientEjectIntervalTicks = 250;
        private const int PatientAutoDischargeIntervalTicks = 250;
        private const float SafeConsciousnessDischargeThreshold = 0.45f;
        private const float SafeMovingDischargeThreshold = 0.60f;
        private const float SeverePainDischargeThreshold = 0.75f;
        private const float MajorInjuryDischargeSeverity = 8f;

        private static readonly MedicalBayPatientRecordService SharedPatientRecordService =
            new MedicalBayPatientRecordService();

        private ThingOwner<Thing> medicalHeldThings;
        private List<ShuttleMedicalPatientRecord> patientRecords =
            new List<ShuttleMedicalPatientRecord>();
        private List<ShuttleMedicalTreatmentReservation> treatmentReservations =
            new List<ShuttleMedicalTreatmentReservation>();
        private List<ShuttleMedicalAdmissionReservation> admissionReservations =
            new List<ShuttleMedicalAdmissionReservation>();
        private int nextReservationCleanupTick = -1;
        private int nextPatientRecordReconcileTick = -1;
        private int nextInvalidPatientEjectTick = -1;
        private int nextPatientAutoDischargeTick = -1;

        public CompShuttleMedicalBayOccupancy()
        {
            this.medicalHeldThings = new ThingOwner<Thing>(this, false, LookMode.Deep, true);
        }

        internal bool HasPatients
        {
            get
            {
                return this.PatientCount > 0;
            }
        }

        internal int PatientCount
        {
            get
            {
                this.EnsureInitialized();
                return this.CountValidPatientRecords();
            }
        }

        internal int FreePatientSlots
        {
            get
            {
                int slots = this.GetMedicalPatientSlots();
                this.ClearStaleAdmissionReservations();
                int free = slots - this.PatientCount - this.CountAdmissionReservationsExcluding(-1);
                return free > 0 ? free : 0;
            }
        }

        internal List<Pawn> HeldPatients
        {
            get
            {
                return this.GetHeldPatientsForReading();
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Deep.Look(ref this.medicalHeldThings, "medicalHeldThings", new object[] { this });
            Scribe_Collections.Look(
                ref this.patientRecords,
                "medicalPatientRecords",
                LookMode.Deep,
                System.Array.Empty<object>());
            Scribe_Collections.Look(
                ref this.treatmentReservations,
                "medicalTreatmentReservations",
                LookMode.Deep,
                System.Array.Empty<object>());
            Scribe_Collections.Look(
                ref this.admissionReservations,
                "medicalAdmissionReservations",
                LookMode.Deep,
                System.Array.Empty<object>());

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.EnsureInitialized();
                this.ResetTickIntervals();
                this.ReconcilePatientRecordsToHeldPawns();
                this.ClearStaleAdmissionReservations();
                this.ClearStaleTreatmentReservations();
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            this.EnsureInitialized();
            this.MaybeReconcilePatientRecords();
            this.MaybeCleanupReservations();
            this.TickHeldPatients();
            this.TickPassiveComfortPatients();
            this.MaybeAutoDischargeRecoveredPatients();
            this.MaybeEjectInvalidPatients();
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);

            if (previousMap != null)
            {
                if (!this.TryEjectAllPatients(previousMap, out string failureReason) && this.HasPatients)
                {
                    Log.Error("[CeleTech Shuttle] Failed to eject all Medical Bay patients while shuttle was destroyed on-map. " + failureReason);
                }
                return;
            }

            this.PreserveDestroyedHolderContents(mode);
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            this.EnsureInitialized();
            return this.medicalHeldThings;
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, this.GetDirectlyHeldThings());
        }

        private void EnsureInitialized()
        {
            if (this.medicalHeldThings == null)
            {
                this.medicalHeldThings = new ThingOwner<Thing>(this, false, LookMode.Deep, true);
            }

            if (this.patientRecords == null)
            {
                this.patientRecords = new List<ShuttleMedicalPatientRecord>();
            }

            if (this.treatmentReservations == null)
            {
                this.treatmentReservations = new List<ShuttleMedicalTreatmentReservation>();
            }

            if (this.admissionReservations == null)
            {
                this.admissionReservations = new List<ShuttleMedicalAdmissionReservation>();
            }
        }
    }

    internal sealed class ShuttleMedicalTreatmentReservation : IExposable
    {
        internal int PatientThingID;
        internal int DoctorThingID;
        internal int ReservationTick;

        public ShuttleMedicalTreatmentReservation()
        {
        }

        internal ShuttleMedicalTreatmentReservation(
            int patientThingID,
            int doctorThingID,
            int reservationTick)
        {
            this.PatientThingID = patientThingID;
            this.DoctorThingID = doctorThingID;
            this.ReservationTick = reservationTick;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.PatientThingID, "patientThingID", -1);
            Scribe_Values.Look(ref this.DoctorThingID, "doctorThingID", -1);
            Scribe_Values.Look(ref this.ReservationTick, "reservationTick", -1);
        }
    }

    internal sealed class ShuttleMedicalAdmissionReservation : IExposable
    {
        internal int PatientThingID;
        internal int ActorThingID;
        internal int ReservationTick;

        public ShuttleMedicalAdmissionReservation()
        {
        }

        internal ShuttleMedicalAdmissionReservation(
            int patientThingID,
            int actorThingID,
            int reservationTick)
        {
            this.PatientThingID = patientThingID;
            this.ActorThingID = actorThingID;
            this.ReservationTick = reservationTick;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.PatientThingID, "patientThingID", -1);
            Scribe_Values.Look(ref this.ActorThingID, "actorThingID", -1);
            Scribe_Values.Look(ref this.ReservationTick, "reservationTick", -1);
        }
    }

    internal sealed class ShuttleMedicalPatientSnapshot
    {
        internal ShuttleMedicalPatientSnapshot(ShuttleMedicalPatientRecord record, Pawn pawn)
        {
            this.Pawn = pawn;
            this.PawnThingID = record != null ? record.PawnThingID : (pawn != null ? pawn.thingIDNumber : -1);
            this.PawnLabel = record != null && !string.IsNullOrEmpty(record.PawnLabel)
                ? record.PawnLabel
                : (pawn != null ? pawn.LabelShort : string.Empty);
            this.WasDownedOnAdmission = record != null && record.WasDownedOnAdmission;
            this.AdmissionTick = record != null ? record.AdmissionTick : -1;
            this.AdmissionMode = record != null ? record.AdmissionMode : string.Empty;
            this.LastKnownReasonLabel = record != null ? record.LastKnownReasonLabel : string.Empty;
            this.LastKnownSeverityLabel = record != null ? record.LastKnownSeverityLabel : string.Empty;
        }

        internal Pawn Pawn { get; private set; }
        internal int PawnThingID { get; private set; }
        internal string PawnLabel { get; private set; }
        internal bool WasDownedOnAdmission { get; private set; }
        internal int AdmissionTick { get; private set; }
        internal string AdmissionMode { get; private set; }
        internal string LastKnownReasonLabel { get; private set; }
        internal string LastKnownSeverityLabel { get; private set; }
    }
}
