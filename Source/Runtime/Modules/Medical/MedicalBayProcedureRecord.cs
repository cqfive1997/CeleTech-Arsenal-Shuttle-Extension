using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Medical
{
    internal sealed class MedicalBayProcedureRecord : IExposable
    {
        internal const string TypeTend = "Tend";
        internal const string TypeSurgery = "Surgery";

        internal const string StatusPreparing = "Preparing";
        internal const string StatusInProgress = "InProgress";
        internal const string StatusCompleted = "Completed";
        internal const string StatusFailed = "Failed";
        internal const string StatusCancelled = "Cancelled";
        internal const string StatusRecoveryRequired = "RecoveryRequired";

        private int procedureID;
        private string procedureType;
        private int doctorThingID;
        private int patientThingID;
        private bool useAvailableMedicine;
        private string recipeDefName;
        private int bodyPartIndex;
        private int workTicksTotal;
        private int workTicksDone;
        private int tendCyclesCompleted;
        private int lastKnownRemainingTendableCount;
        private string status;

        public MedicalBayProcedureRecord()
        {
        }

        internal MedicalBayProcedureRecord(
            int procedureID,
            string procedureType,
            int doctorThingID,
            int patientThingID,
            bool useAvailableMedicine,
            string recipeDefName,
            int bodyPartIndex,
            int workTicksTotal)
        {
            this.procedureID = procedureID;
            this.procedureType = SanitizeProcedureType(procedureType);
            this.doctorThingID = doctorThingID;
            this.patientThingID = patientThingID;
            this.useAvailableMedicine = useAvailableMedicine;
            this.recipeDefName = recipeDefName ?? string.Empty;
            this.bodyPartIndex = bodyPartIndex;
            this.workTicksTotal = Max(0, workTicksTotal);
            this.workTicksDone = 0;
            this.status = StatusPreparing;
        }

        internal int ProcedureID
        {
            get { return this.procedureID; }
        }

        internal string ProcedureType
        {
            get { return this.procedureType; }
        }

        internal int DoctorThingID
        {
            get { return this.doctorThingID; }
        }

        internal int PatientThingID
        {
            get { return this.patientThingID; }
        }

        internal bool UseAvailableMedicine
        {
            get { return this.useAvailableMedicine; }
        }

        internal string RecipeDefName
        {
            get { return this.recipeDefName; }
        }

        internal int BodyPartIndex
        {
            get { return this.bodyPartIndex; }
        }

        internal int WorkTicksTotal
        {
            get { return this.workTicksTotal; }
        }

        internal int WorkTicksDone
        {
            get { return this.workTicksDone; }
        }

        internal int TendCyclesCompleted
        {
            get { return this.tendCyclesCompleted; }
        }

        internal int LastKnownRemainingTendableCount
        {
            get { return this.lastKnownRemainingTendableCount; }
        }

        internal string Status
        {
            get { return this.status; }
        }

        internal bool IsActive
        {
            get
            {
                return this.status == StatusPreparing ||
                    this.status == StatusInProgress ||
                    this.status == StatusRecoveryRequired;
            }
        }

        internal void MarkInProgress()
        {
            this.status = StatusInProgress;
        }

        internal void MarkCompleted()
        {
            this.status = StatusCompleted;
            this.workTicksDone = this.workTicksTotal;
        }

        internal void MarkFailed()
        {
            this.status = StatusFailed;
        }

        internal void MarkCancelled()
        {
            this.status = StatusCancelled;
        }

        internal void MarkRecoveryRequired()
        {
            this.status = StatusRecoveryRequired;
        }

        internal void ResetWorkForNextCycle(int workTicksTotal)
        {
            this.workTicksDone = 0;
            this.workTicksTotal = Max(1, workTicksTotal);
            this.status = StatusInProgress;
        }

        internal void MarkTendCycleCompleted(int remainingTendableCount)
        {
            this.tendCyclesCompleted++;
            this.lastKnownRemainingTendableCount = Max(0, remainingTendableCount);
        }

        internal void AddWorkTicks(int ticks)
        {
            if (ticks <= 0)
            {
                return;
            }

            this.workTicksDone = Clamp(this.workTicksDone + ticks, 0, this.workTicksTotal);
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.procedureID, "procedureID", 0);
            Scribe_Values.Look(ref this.procedureType, "procedureType", TypeTend);
            Scribe_Values.Look(ref this.doctorThingID, "doctorThingID", -1);
            Scribe_Values.Look(ref this.patientThingID, "patientThingID", -1);
            Scribe_Values.Look(ref this.useAvailableMedicine, "useAvailableMedicine", false);
            Scribe_Values.Look(ref this.recipeDefName, "recipeDefName");
            Scribe_Values.Look(ref this.bodyPartIndex, "bodyPartIndex", -1);
            Scribe_Values.Look(ref this.workTicksTotal, "workTicksTotal", 0);
            Scribe_Values.Look(ref this.workTicksDone, "workTicksDone", 0);
            Scribe_Values.Look(ref this.tendCyclesCompleted, "tendCyclesCompleted", 0);
            Scribe_Values.Look(ref this.lastKnownRemainingTendableCount, "lastKnownRemainingTendableCount", 0);
            Scribe_Values.Look(ref this.status, "status", StatusPreparing);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.SanitizePostLoad();
            }
        }

        internal void SanitizePostLoad()
        {
            this.procedureType = SanitizeProcedureType(this.procedureType);
            this.recipeDefName = this.recipeDefName ?? string.Empty;
            this.workTicksTotal = Max(0, this.workTicksTotal);
            this.workTicksDone = Clamp(this.workTicksDone, 0, this.workTicksTotal);
            this.tendCyclesCompleted = Max(0, this.tendCyclesCompleted);
            this.lastKnownRemainingTendableCount = Max(0, this.lastKnownRemainingTendableCount);
            this.status = SanitizeStatus(this.status);
        }

        private static string SanitizeProcedureType(string value)
        {
            if (value == TypeSurgery)
            {
                return TypeSurgery;
            }

            return TypeTend;
        }

        private static string SanitizeStatus(string value)
        {
            if (value == StatusPreparing ||
                value == StatusInProgress ||
                value == StatusCompleted ||
                value == StatusFailed ||
                value == StatusCancelled ||
                value == StatusRecoveryRequired)
            {
                return value;
            }

            return StatusPreparing;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }

        private static int Max(int left, int right)
        {
            return left > right ? left : right;
        }
    }
}
