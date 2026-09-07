using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    /// <summary>
    /// Metadata for a pawn contained by the shuttle Prison Cell holder. This record
    /// intentionally does not mutate pawn.guest, prisoner status, or interaction state.
    /// </summary>
    internal sealed class ShuttlePrisonerRecord : IExposable
    {
        private Pawn prisoner;
        private int pawnThingIDNumber = -1;
        private string pawnLabel;
        private int admissionTick = -1;
        private Faction originalFaction;
        private Faction hostFaction;
        private PrisonerInteractionModeDef interactionMode;
        private bool wasPrisonerOnAdmission;
        private bool released;
        private bool pendingRelease;
        private int lastFedTick = -1;
        private float lastFedNutrition;
        private string lastFedFoodLabel;
        private int lastTendedTick = -1;
        private string lastTendMedicineLabel;
        // Auto-feed success is scheduler success: a feed job was assigned.
        // Actual food consumption is recorded separately by MarkFed after the job runs.
        private int lastAutoFeedAttemptTick = -1;
        private int lastAutoFeedSuccessTick = -1;
        private int lastAutoFeedFailureTick = -1;
        private string lastAutoFeedFailureReason;

        public ShuttlePrisonerRecord()
        {
        }

        private ShuttlePrisonerRecord(
            Pawn prisoner,
            Faction hostFaction,
            PrisonerInteractionModeDef interactionMode,
            bool wasPrisonerOnAdmission,
            int admissionTick)
        {
            this.prisoner = prisoner;
            this.pawnThingIDNumber = prisoner != null ? prisoner.thingIDNumber : -1;
            this.pawnLabel = prisoner != null ? prisoner.LabelShort : string.Empty;
            this.admissionTick = admissionTick;
            this.originalFaction = prisoner != null ? prisoner.Faction : null;
            this.hostFaction = hostFaction;
            this.interactionMode = interactionMode;
            this.wasPrisonerOnAdmission = wasPrisonerOnAdmission;
            this.released = false;
            this.pendingRelease = false;
            this.Sanitize();
        }

        internal Pawn Prisoner
        {
            get { return this.prisoner; }
        }

        internal int PawnThingIDNumber
        {
            get { return this.pawnThingIDNumber; }
        }

        internal string PawnLabel
        {
            get { return this.pawnLabel; }
        }

        internal int AdmissionTick
        {
            get { return this.admissionTick; }
        }

        internal Faction OriginalFaction
        {
            get { return this.originalFaction; }
        }

        internal Faction HostFaction
        {
            get { return this.hostFaction; }
        }

        internal PrisonerInteractionModeDef InteractionMode
        {
            get { return this.interactionMode; }
        }

        internal bool WasPrisonerOnAdmission
        {
            get { return this.wasPrisonerOnAdmission; }
        }

        internal bool Released
        {
            get { return this.released; }
        }

        internal bool PendingRelease
        {
            get { return this.pendingRelease; }
        }

        internal int LastFedTick
        {
            get { return this.lastFedTick; }
        }

        internal float LastFedNutrition
        {
            get { return this.lastFedNutrition; }
        }

        internal string LastFedFoodLabel
        {
            get { return this.lastFedFoodLabel; }
        }

        internal int LastTendedTick
        {
            get { return this.lastTendedTick; }
        }

        internal string LastTendMedicineLabel
        {
            get { return this.lastTendMedicineLabel; }
        }

        internal int LastAutoFeedAttemptTick
        {
            get { return this.lastAutoFeedAttemptTick; }
        }

        internal int LastAutoFeedSuccessTick
        {
            get { return this.lastAutoFeedSuccessTick; }
        }

        internal int LastAutoFeedFailureTick
        {
            get { return this.lastAutoFeedFailureTick; }
        }

        internal string LastAutoFeedFailureReason
        {
            get { return this.lastAutoFeedFailureReason; }
        }

        internal static ShuttlePrisonerRecord FromPawn(
            Pawn pawn,
            Faction hostFaction,
            PrisonerInteractionModeDef interactionMode,
            bool wasPrisonerOnAdmission,
            int admissionTick)
        {
            return new ShuttlePrisonerRecord(
                pawn,
                hostFaction,
                interactionMode,
                wasPrisonerOnAdmission,
                admissionTick);
        }

        internal static ShuttlePrisonerRecord FromLaunchManifestEntry(
            Pawn pawn,
            ShuttleHolderLaunchManifestEntry entry)
        {
            ShuttlePrisonerRecord record = new ShuttlePrisonerRecord();
            record.prisoner = pawn;
            record.pawnThingIDNumber = pawn != null ? pawn.thingIDNumber : (entry != null ? entry.ThingID : -1);
            record.pawnLabel = pawn != null ? pawn.LabelShort : (entry != null ? entry.Label : string.Empty);
            record.admissionTick = entry != null ? entry.AdmissionTick : -1;
            record.originalFaction = entry != null ? entry.OriginalFaction : (pawn != null ? pawn.Faction : null);
            record.hostFaction = entry != null ? entry.HostFaction : null;
            record.interactionMode = entry != null ? entry.InteractionMode : null;
            record.wasPrisonerOnAdmission = entry != null && entry.WasPrisonerOnAdmission;
            record.released = entry != null && entry.Released;
            record.pendingRelease = entry != null && entry.PendingRelease;
            record.lastFedTick = entry != null ? entry.LastFedTick : -1;
            record.lastFedNutrition = entry != null ? entry.LastFedNutrition : 0f;
            record.lastFedFoodLabel = entry != null ? entry.LastFedFoodLabel : string.Empty;
            record.lastTendedTick = entry != null ? entry.LastTendedTick : -1;
            record.lastTendMedicineLabel = entry != null ? entry.LastTendMedicineLabel : string.Empty;
            record.lastAutoFeedAttemptTick = entry != null ? entry.LastAutoFeedAttemptTick : -1;
            record.lastAutoFeedSuccessTick = entry != null ? entry.LastAutoFeedSuccessTick : -1;
            record.lastAutoFeedFailureTick = entry != null ? entry.LastAutoFeedFailureTick : -1;
            record.lastAutoFeedFailureReason = entry != null ? entry.LastAutoFeedFailureReason : string.Empty;
            record.Sanitize();
            return record;
        }

        internal void RefreshFromPawn(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            this.prisoner = pawn;
            this.pawnThingIDNumber = pawn.thingIDNumber;
            this.pawnLabel = pawn.LabelShort;
            if (this.originalFaction == null)
            {
                this.originalFaction = pawn.Faction;
            }

            this.Sanitize();
        }

        internal bool Matches(Pawn pawn)
        {
            return pawn != null && this.MatchesThingID(pawn.thingIDNumber);
        }

        internal bool MatchesThingID(int thingIDNumber)
        {
            return thingIDNumber > 0 && this.pawnThingIDNumber == thingIDNumber;
        }

        internal void MarkPendingRelease()
        {
            this.pendingRelease = true;
        }

        internal void MarkReleased()
        {
            this.released = true;
            this.pendingRelease = false;
        }

        internal void MarkFed(int tick, float nutrition, string foodLabel)
        {
            this.lastFedTick = tick;
            this.lastFedNutrition = nutrition > 0f ? nutrition : 0f;
            this.lastFedFoodLabel = foodLabel ?? string.Empty;
        }

        internal void MarkTended(int tick, string medicineLabel)
        {
            this.lastTendedTick = tick;
            this.lastTendMedicineLabel = medicineLabel ?? string.Empty;
        }

        internal void MarkAutoFeedAttempt(int tick)
        {
            this.lastAutoFeedAttemptTick = SanitizeTick(tick);
        }

        internal void MarkAutoFeedSuccess(int tick)
        {
            // This does not mean the prisoner has eaten yet; the feed job still
            // has to reach the shuttle and call the feeding service.
            tick = SanitizeTick(tick);
            this.lastAutoFeedAttemptTick = tick;
            this.lastAutoFeedSuccessTick = tick;
            this.lastAutoFeedFailureReason = string.Empty;
        }

        internal void MarkAutoFeedFailure(int tick, string reason)
        {
            tick = SanitizeTick(tick);
            this.lastAutoFeedAttemptTick = tick;
            this.lastAutoFeedFailureTick = tick;
            this.lastAutoFeedFailureReason = reason ?? string.Empty;
        }

        internal void Sanitize()
        {
            if (this.prisoner != null)
            {
                this.pawnThingIDNumber = this.prisoner.thingIDNumber;
                if (string.IsNullOrEmpty(this.pawnLabel))
                {
                    this.pawnLabel = this.prisoner.LabelShort;
                }
            }

            if (this.pawnThingIDNumber < -1)
            {
                this.pawnThingIDNumber = -1;
            }

            if (this.pawnLabel == null)
            {
                this.pawnLabel = string.Empty;
            }

            if (this.admissionTick < -1)
            {
                this.admissionTick = -1;
            }

            if (this.released)
            {
                this.pendingRelease = false;
            }

            if (this.lastFedTick < -1)
            {
                this.lastFedTick = -1;
            }

            if (this.lastFedNutrition < 0f || float.IsNaN(this.lastFedNutrition) || float.IsInfinity(this.lastFedNutrition))
            {
                this.lastFedNutrition = 0f;
            }

            if (this.lastFedFoodLabel == null)
            {
                this.lastFedFoodLabel = string.Empty;
            }

            if (this.lastTendedTick < -1)
            {
                this.lastTendedTick = -1;
            }

            if (this.lastTendMedicineLabel == null)
            {
                this.lastTendMedicineLabel = string.Empty;
            }

            this.lastAutoFeedAttemptTick = SanitizeTick(this.lastAutoFeedAttemptTick);
            this.lastAutoFeedSuccessTick = SanitizeTick(this.lastAutoFeedSuccessTick);
            this.lastAutoFeedFailureTick = SanitizeTick(this.lastAutoFeedFailureTick);
            if (this.lastAutoFeedFailureReason == null)
            {
                this.lastAutoFeedFailureReason = string.Empty;
            }
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref this.prisoner, "prisoner");
            Scribe_Values.Look(ref this.pawnThingIDNumber, "pawnThingIDNumber", -1);
            Scribe_Values.Look(ref this.pawnLabel, "pawnLabel", string.Empty);
            Scribe_Values.Look(ref this.admissionTick, "admissionTick", -1);
            Scribe_References.Look(ref this.originalFaction, "originalFaction");
            Scribe_References.Look(ref this.hostFaction, "hostFaction");
            Scribe_Defs.Look(ref this.interactionMode, "interactionMode");
            Scribe_Values.Look(ref this.wasPrisonerOnAdmission, "wasPrisonerOnAdmission", false);
            Scribe_Values.Look(ref this.released, "released", false);
            Scribe_Values.Look(ref this.pendingRelease, "pendingRelease", false);
            Scribe_Values.Look(ref this.lastFedTick, "lastFedTick", -1);
            Scribe_Values.Look(ref this.lastFedNutrition, "lastFedNutrition", 0f);
            Scribe_Values.Look(ref this.lastFedFoodLabel, "lastFedFoodLabel", string.Empty);
            Scribe_Values.Look(ref this.lastTendedTick, "lastTendedTick", -1);
            Scribe_Values.Look(ref this.lastTendMedicineLabel, "lastTendMedicineLabel", string.Empty);
            Scribe_Values.Look(ref this.lastAutoFeedAttemptTick, "lastAutoFeedAttemptTick", -1);
            Scribe_Values.Look(ref this.lastAutoFeedSuccessTick, "lastAutoFeedSuccessTick", -1);
            Scribe_Values.Look(ref this.lastAutoFeedFailureTick, "lastAutoFeedFailureTick", -1);
            Scribe_Values.Look(ref this.lastAutoFeedFailureReason, "lastAutoFeedFailureReason", string.Empty);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.Sanitize();
            }
        }

        private static int SanitizeTick(int tick)
        {
            return tick < -1 ? -1 : tick;
        }
    }
}
