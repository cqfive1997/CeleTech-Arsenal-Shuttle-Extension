using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    /// <summary>
    /// Lightweight record for a pawn currently contained by the shuttle Medical Bay holder.
    /// It deliberately avoids hediff lists, treatment progress, doctor jobs, and comfort runtime.
    /// </summary>
    internal sealed class ShuttleMedicalPatientRecord : IExposable
    {
        private Pawn pawn;
        private int pawnThingID = -1;
        private string pawnLabel;
        private bool wasDownedOnAdmission;
        private int admissionTick = -1;
        private string admissionMode;
        private string lastKnownReasonLabel;
        private string lastKnownSeverityLabel;

        public ShuttleMedicalPatientRecord()
        {
        }

        internal ShuttleMedicalPatientRecord(
            Pawn pawn,
            string admissionMode,
            int admissionTick,
            string reasonLabel,
            string severityLabel)
        {
            this.pawn = pawn;
            this.pawnThingID = pawn != null ? pawn.thingIDNumber : -1;
            this.pawnLabel = pawn != null ? pawn.LabelShort : string.Empty;
            this.wasDownedOnAdmission = pawn != null && pawn.Downed;
            this.admissionTick = admissionTick;
            this.admissionMode = admissionMode;
            this.lastKnownReasonLabel = reasonLabel;
            this.lastKnownSeverityLabel = severityLabel;
            this.Sanitize();
        }

        internal ShuttleMedicalPatientRecord(
            Pawn pawn,
            string admissionMode,
            int admissionTick,
            bool wasDownedOnAdmission,
            string reasonLabel,
            string severityLabel)
        {
            this.pawn = pawn;
            this.pawnThingID = pawn != null ? pawn.thingIDNumber : -1;
            this.pawnLabel = pawn != null ? pawn.LabelShort : string.Empty;
            this.wasDownedOnAdmission = wasDownedOnAdmission;
            this.admissionTick = admissionTick;
            this.admissionMode = admissionMode;
            this.lastKnownReasonLabel = reasonLabel;
            this.lastKnownSeverityLabel = severityLabel;
            this.Sanitize();
        }

        internal Pawn Pawn
        {
            get
            {
                return this.pawn;
            }
        }

        internal int PawnThingID
        {
            get
            {
                return this.pawnThingID;
            }
        }

        internal string PawnLabel
        {
            get
            {
                return this.pawnLabel;
            }
        }

        internal bool WasDownedOnAdmission
        {
            get
            {
                return this.wasDownedOnAdmission;
            }
        }

        internal int AdmissionTick
        {
            get
            {
                return this.admissionTick;
            }
        }

        internal string AdmissionMode
        {
            get
            {
                return this.admissionMode;
            }
        }

        internal string LastKnownReasonLabel
        {
            get
            {
                return this.lastKnownReasonLabel;
            }
        }

        internal string LastKnownSeverityLabel
        {
            get
            {
                return this.lastKnownSeverityLabel;
            }
        }

        internal void StopAdmission()
        {
            this.admissionMode = string.Empty;
        }

        internal void Sanitize()
        {
            if (this.pawnThingID < -1)
            {
                this.pawnThingID = -1;
            }

            if (this.pawn != null)
            {
                this.pawnThingID = this.pawn.thingIDNumber;
                if (string.IsNullOrEmpty(this.pawnLabel))
                {
                    this.pawnLabel = this.pawn.LabelShort;
                }
            }

            if (this.pawnLabel == null)
            {
                this.pawnLabel = string.Empty;
            }

            if (this.admissionTick < -1)
            {
                this.admissionTick = -1;
            }

            if (this.admissionMode == null)
            {
                this.admissionMode = string.Empty;
            }

            if (this.lastKnownReasonLabel == null)
            {
                this.lastKnownReasonLabel = string.Empty;
            }

            if (this.lastKnownSeverityLabel == null)
            {
                this.lastKnownSeverityLabel = string.Empty;
            }
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref this.pawn, "pawn");
            Scribe_Values.Look(ref this.pawnThingID, "pawnThingID", -1);
            Scribe_Values.Look(ref this.pawnLabel, "pawnLabel", string.Empty);
            Scribe_Values.Look(ref this.wasDownedOnAdmission, "wasDownedOnAdmission", false);
            Scribe_Values.Look(ref this.admissionTick, "admissionTick", -1);
            Scribe_Values.Look(ref this.admissionMode, "admissionMode", string.Empty);
            Scribe_Values.Look(ref this.lastKnownReasonLabel, "lastKnownReasonLabel", string.Empty);
            Scribe_Values.Look(ref this.lastKnownSeverityLabel, "lastKnownSeverityLabel", string.Empty);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.Sanitize();
            }
        }
    }
}
