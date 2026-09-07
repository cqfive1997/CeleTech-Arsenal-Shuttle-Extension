using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal static class ShuttleHolderLaunchManifestConstants
    {
        internal const int CurrentManifestVersion = 1;
        internal const string DevTestHolderKind = "DevTest";
        internal const string DevTestSourceHolder = "devTestHeldThings";
        internal const string HabitatSourceHolder = "habitatHeldThings";
        internal const string MedicalBayPatientHolderKind = "MedicalBayPatient";
        internal const string MedicalBaySourceHolder = "medicalHeldThings";
        internal const string MechChargerHolderKind = "MechCharger";
        internal const string MechChargerSourceHolder = "mechChargingHeldThings";
        internal const string PrisonCellPrisonerHolderKind = "PrisonCellPrisoner";
        internal const string PrisonCellSourceHolder = "prisonCellHeldThings";
        // Habitat living entries: sleep pawns and dining pawn/food pairs.
        internal const string HabitatSleepHolderKind = "HabitatSleep";
        internal const string HabitatDiningPawnHolderKind = "HabitatDiningPawn";
        internal const string HabitatDiningFoodHolderKind = "HabitatDiningFood";
        // Recreation Room entries. Joy uses the same Habitat holder but a distinct record type.
        internal const string HabitatJoyHolderKind = "HabitatJoy";
        internal const string ActivityKindSleep = "Sleep";
        internal const string ActivityKindDining = "Dining";
        internal const string ActivityKindJoy = "Joy";
        internal const string RecordKindDevTest = "DevTestThing";
        internal const string RecordKindHabitatSleepPawn = "HabitatSleepPawn";
        internal const string RecordKindHabitatDiningPawn = "HabitatDiningPawn";
        internal const string RecordKindHabitatDiningFood = "HabitatDiningFood";
        internal const string RecordKindHabitatJoyPawn = "HabitatJoyPawn";
        internal const string RecordKindMedicalBayPatient = "MedicalBayPatient";
        internal const string RecordKindMechChargerPawn = "MechChargerPawn";
        internal const string RecordKindPrisonCellPrisoner = "PrisonCellPrisoner";
        internal const string PhaseNone = "None";
        internal const string PhaseExported = "Exported";
        internal const string PhaseInFlight = "InFlight";
        internal const string PhaseRestored = "Restored";
        internal const string PhaseRolledBack = "RolledBack";
        internal const string PhaseEjected = "Ejected";
        internal const string PhaseQuarantined = "Quarantined";
        internal const string PhaseFailed = "Failed";
    }

    /// <summary>
    /// Persisted manifest for shuttle-owned holder contents during launch transfer.
    /// It records identity and activity state only; the actual Thing remains in the
    /// launch/incoming containers until restore moves it back to the owning holder.
    /// </summary>
    internal sealed class ShuttleHolderLaunchManifest : IExposable
    {
        internal int ManifestVersion = ShuttleHolderLaunchManifestConstants.CurrentManifestVersion;
        internal List<ShuttleHolderLaunchManifestEntry> Entries =
            new List<ShuttleHolderLaunchManifestEntry>();

        internal bool HasEntries
        {
            get
            {
                this.EnsureInitialized();
                return this.Entries.Count > 0;
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.ManifestVersion, "manifestVersion", ShuttleHolderLaunchManifestConstants.CurrentManifestVersion);
            Scribe_Collections.Look(
                ref this.Entries,
                "entries",
                LookMode.Deep,
                System.Array.Empty<object>());

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.EnsureInitialized();
            }
        }

        internal void EnsureInitialized()
        {
            if (this.Entries == null)
            {
                this.Entries = new List<ShuttleHolderLaunchManifestEntry>();
            }

            if (this.ManifestVersion <= 0)
            {
                this.ManifestVersion = ShuttleHolderLaunchManifestConstants.CurrentManifestVersion;
            }

            for (int i = 0; i < this.Entries.Count; i++)
            {
                ShuttleHolderLaunchManifestEntry entry = this.Entries[i];
                if (entry != null)
                {
                    entry.EnsureInitialized();
                }
            }
        }

        internal void Clear()
        {
            this.EnsureInitialized();
            this.Entries.Clear();
            this.ManifestVersion = ShuttleHolderLaunchManifestConstants.CurrentManifestVersion;
        }

        internal ShuttleHolderLaunchManifestEntry AddDevTestEntry(Thing thing, int exportTick, string phase, string debugNotes)
        {
            this.EnsureInitialized();
            ShuttleHolderLaunchManifestEntry entry = new ShuttleHolderLaunchManifestEntry
            {
                ManifestVersion = this.ManifestVersion,
                HolderKind = ShuttleHolderLaunchManifestConstants.DevTestHolderKind,
                ThingID = thing != null ? thing.thingIDNumber : -1,
                DefName = thing != null && thing.def != null ? thing.def.defName : null,
                Label = thing != null ? thing.LabelCapNoCount : null,
                SourceHolder = ShuttleHolderLaunchManifestConstants.DevTestSourceHolder,
                RecordKind = ShuttleHolderLaunchManifestConstants.RecordKindDevTest,
                ExportTick = exportTick,
                TransferPhase = phase,
                DebugNotes = debugNotes
            };
            entry.EnsureInitialized();
            this.Entries.Add(entry);
            return entry;
        }

        internal ShuttleHolderLaunchManifestEntry AddHabitatSleepEntry(
            int pawnThingID,
            string pawnDefName,
            string pawnLabel,
            string activityID,
            int originalRecordIndex,
            int restStartTick,
            int restedTicks,
            bool isSleeping,
            int exportTick,
            string phase,
            string debugNotes)
        {
            this.EnsureInitialized();
            ShuttleHolderLaunchManifestEntry entry = new ShuttleHolderLaunchManifestEntry
            {
                ManifestVersion = this.ManifestVersion,
                HolderKind = ShuttleHolderLaunchManifestConstants.HabitatSleepHolderKind,
                ActivityKind = ShuttleHolderLaunchManifestConstants.ActivityKindSleep,
                ActivityID = activityID,
                RecordKind = ShuttleHolderLaunchManifestConstants.RecordKindHabitatSleepPawn,
                OriginalRecordIndex = originalRecordIndex,
                ThingID = pawnThingID,
                DefName = pawnDefName,
                Label = pawnLabel,
                SourceHolder = ShuttleHolderLaunchManifestConstants.HabitatSourceHolder,
                RestStartTick = restStartTick,
                RestedTicks = restedTicks,
                IsSleeping = isSleeping,
                ExportTick = exportTick,
                TransferPhase = phase,
                DebugNotes = debugNotes
            };
            entry.EnsureInitialized();
            this.Entries.Add(entry);
            return entry;
        }

        internal ShuttleHolderLaunchManifestEntry AddHabitatDiningPawnEntry(
            int pawnThingID,
            string pawnDefName,
            string pawnLabel,
            string activityID,
            int originalRecordIndex,
            int foodThingID,
            string foodDefName,
            string foodLabel,
            int foodStackCount,
            int diningStartTick,
            int chewTicksLeft,
            int chewTicksTotal,
            bool isDining,
            bool finalized,
            int exportTick,
            string phase,
            string debugNotes)
        {
            this.EnsureInitialized();
            ShuttleHolderLaunchManifestEntry entry = new ShuttleHolderLaunchManifestEntry
            {
                ManifestVersion = this.ManifestVersion,
                HolderKind = ShuttleHolderLaunchManifestConstants.HabitatDiningPawnHolderKind,
                ActivityKind = ShuttleHolderLaunchManifestConstants.ActivityKindDining,
                ActivityID = activityID,
                RecordKind = ShuttleHolderLaunchManifestConstants.RecordKindHabitatDiningPawn,
                OriginalRecordIndex = originalRecordIndex,
                ThingID = pawnThingID,
                DefName = pawnDefName,
                Label = pawnLabel,
                SourceHolder = ShuttleHolderLaunchManifestConstants.HabitatSourceHolder,
                FoodThingID = foodThingID,
                FoodDefName = foodDefName,
                FoodLabel = foodLabel,
                FoodStackCount = foodStackCount,
                DiningStartTick = diningStartTick,
                ChewTicksLeft = chewTicksLeft,
                ChewTicksTotal = chewTicksTotal,
                IsDining = isDining,
                Finalized = finalized,
                ExportTick = exportTick,
                TransferPhase = phase,
                DebugNotes = debugNotes
            };
            entry.EnsureInitialized();
            this.Entries.Add(entry);
            return entry;
        }

        internal ShuttleHolderLaunchManifestEntry AddHabitatDiningFoodEntry(
            int foodThingID,
            string foodDefName,
            string foodLabel,
            int foodStackCount,
            string activityID,
            int originalRecordIndex,
            int diningStartTick,
            int chewTicksLeft,
            int chewTicksTotal,
            bool isDining,
            bool finalized,
            int exportTick,
            string phase,
            string debugNotes)
        {
            this.EnsureInitialized();
            ShuttleHolderLaunchManifestEntry entry = new ShuttleHolderLaunchManifestEntry
            {
                ManifestVersion = this.ManifestVersion,
                HolderKind = ShuttleHolderLaunchManifestConstants.HabitatDiningFoodHolderKind,
                ActivityKind = ShuttleHolderLaunchManifestConstants.ActivityKindDining,
                ActivityID = activityID,
                RecordKind = ShuttleHolderLaunchManifestConstants.RecordKindHabitatDiningFood,
                OriginalRecordIndex = originalRecordIndex,
                ThingID = foodThingID,
                DefName = foodDefName,
                Label = foodLabel,
                SourceHolder = ShuttleHolderLaunchManifestConstants.HabitatSourceHolder,
                FoodThingID = foodThingID,
                FoodDefName = foodDefName,
                FoodLabel = foodLabel,
                FoodStackCount = foodStackCount,
                DiningStartTick = diningStartTick,
                ChewTicksLeft = chewTicksLeft,
                ChewTicksTotal = chewTicksTotal,
                IsDining = isDining,
                Finalized = finalized,
                ExportTick = exportTick,
                TransferPhase = phase,
                DebugNotes = debugNotes
            };
            entry.EnsureInitialized();
            this.Entries.Add(entry);
            return entry;
        }

        internal ShuttleHolderLaunchManifestEntry AddHabitatJoyEntry(
            int pawnThingID,
            string pawnDefName,
            string pawnLabel,
            string activityID,
            int originalRecordIndex,
            string joyKindDefName,
            string joyKindLabel,
            int joyStartTick,
            int joyTicks,
            float joyGainRate,
            int maxJoyTicks,
            bool isJoying,
            int exportTick,
            string phase,
            string debugNotes)
        {
            this.EnsureInitialized();
            ShuttleHolderLaunchManifestEntry entry = new ShuttleHolderLaunchManifestEntry
            {
                ManifestVersion = this.ManifestVersion,
                HolderKind = ShuttleHolderLaunchManifestConstants.HabitatJoyHolderKind,
                ActivityKind = ShuttleHolderLaunchManifestConstants.ActivityKindJoy,
                ActivityID = activityID,
                RecordKind = ShuttleHolderLaunchManifestConstants.RecordKindHabitatJoyPawn,
                OriginalRecordIndex = originalRecordIndex,
                ThingID = pawnThingID,
                DefName = pawnDefName,
                Label = pawnLabel,
                SourceHolder = ShuttleHolderLaunchManifestConstants.HabitatSourceHolder,
                JoyKindDefName = joyKindDefName,
                JoyKindLabel = joyKindLabel,
                JoyStartTick = joyStartTick,
                JoyTicks = joyTicks,
                JoyGainRate = joyGainRate,
                MaxJoyTicks = maxJoyTicks,
                IsJoying = isJoying,
                ExportTick = exportTick,
                TransferPhase = phase,
                DebugNotes = debugNotes
            };
            entry.EnsureInitialized();
            this.Entries.Add(entry);
            return entry;
        }

        internal ShuttleHolderLaunchManifestEntry AddMedicalBayPatientEntry(
            int pawnThingID,
            string pawnDefName,
            string pawnLabel,
            int originalRecordIndex,
            string admissionMode,
            int admissionTick,
            bool wasDownedOnAdmission,
            string lastKnownReasonLabel,
            string lastKnownSeverityLabel,
            int exportTick,
            string phase,
            string debugNotes)
        {
            this.EnsureInitialized();
            ShuttleHolderLaunchManifestEntry entry = new ShuttleHolderLaunchManifestEntry
            {
                ManifestVersion = this.ManifestVersion,
                HolderKind = ShuttleHolderLaunchManifestConstants.MedicalBayPatientHolderKind,
                RecordKind = ShuttleHolderLaunchManifestConstants.RecordKindMedicalBayPatient,
                OriginalRecordIndex = originalRecordIndex,
                ThingID = pawnThingID,
                DefName = pawnDefName,
                Label = pawnLabel,
                SourceHolder = ShuttleHolderLaunchManifestConstants.MedicalBaySourceHolder,
                AdmissionMode = admissionMode,
                AdmissionTick = admissionTick,
                WasDownedOnAdmission = wasDownedOnAdmission,
                LastKnownReasonLabel = lastKnownReasonLabel,
                LastKnownSeverityLabel = lastKnownSeverityLabel,
                ExportTick = exportTick,
                TransferPhase = phase,
                DebugNotes = debugNotes
            };
            entry.EnsureInitialized();
            this.Entries.Add(entry);
            return entry;
        }

        internal ShuttleHolderLaunchManifestEntry AddMechChargerEntry(
            int pawnThingID,
            string pawnDefName,
            string pawnLabel,
            int originalRecordIndex,
            string moduleInstanceID,
            float chargeRateFactor,
            int entryTick,
            int lastChargeTick,
            int exportTick,
            string phase,
            string debugNotes)
        {
            this.EnsureInitialized();
            ShuttleHolderLaunchManifestEntry entry = new ShuttleHolderLaunchManifestEntry
            {
                ManifestVersion = this.ManifestVersion,
                HolderKind = ShuttleHolderLaunchManifestConstants.MechChargerHolderKind,
                RecordKind = ShuttleHolderLaunchManifestConstants.RecordKindMechChargerPawn,
                OriginalRecordIndex = originalRecordIndex,
                ThingID = pawnThingID,
                DefName = pawnDefName,
                Label = pawnLabel,
                SourceHolder = ShuttleHolderLaunchManifestConstants.MechChargerSourceHolder,
                ModuleInstanceID = moduleInstanceID,
                ChargeRateFactor = chargeRateFactor,
                HolderEntryTick = entryTick,
                LastChargeTick = lastChargeTick,
                ExportTick = exportTick,
                TransferPhase = phase,
                DebugNotes = debugNotes
            };
            entry.EnsureInitialized();
            this.Entries.Add(entry);
            return entry;
        }

        internal ShuttleHolderLaunchManifestEntry AddPrisonCellPrisonerEntry(
            int pawnThingID,
            string pawnDefName,
            string pawnLabel,
            int originalRecordIndex,
            int admissionTick,
            Faction originalFaction,
            Faction hostFaction,
            PrisonerInteractionModeDef interactionMode,
            bool wasPrisonerOnAdmission,
            bool released,
            bool pendingRelease,
            int lastFedTick,
            float lastFedNutrition,
            string lastFedFoodLabel,
            int lastTendedTick,
            string lastTendMedicineLabel,
            int lastAutoFeedAttemptTick,
            int lastAutoFeedSuccessTick,
            int lastAutoFeedFailureTick,
            string lastAutoFeedFailureReason,
            int exportTick,
            string phase,
            string debugNotes)
        {
            this.EnsureInitialized();
            ShuttleHolderLaunchManifestEntry entry = new ShuttleHolderLaunchManifestEntry
            {
                ManifestVersion = this.ManifestVersion,
                HolderKind = ShuttleHolderLaunchManifestConstants.PrisonCellPrisonerHolderKind,
                RecordKind = ShuttleHolderLaunchManifestConstants.RecordKindPrisonCellPrisoner,
                OriginalRecordIndex = originalRecordIndex,
                ThingID = pawnThingID,
                DefName = pawnDefName,
                Label = pawnLabel,
                SourceHolder = ShuttleHolderLaunchManifestConstants.PrisonCellSourceHolder,
                AdmissionTick = admissionTick,
                OriginalFaction = originalFaction,
                HostFaction = hostFaction,
                InteractionMode = interactionMode,
                WasPrisonerOnAdmission = wasPrisonerOnAdmission,
                Released = released,
                PendingRelease = pendingRelease,
                LastFedTick = lastFedTick,
                LastFedNutrition = lastFedNutrition,
                LastFedFoodLabel = lastFedFoodLabel,
                LastTendedTick = lastTendedTick,
                LastTendMedicineLabel = lastTendMedicineLabel,
                LastAutoFeedAttemptTick = lastAutoFeedAttemptTick,
                LastAutoFeedSuccessTick = lastAutoFeedSuccessTick,
                LastAutoFeedFailureTick = lastAutoFeedFailureTick,
                LastAutoFeedFailureReason = lastAutoFeedFailureReason,
                ExportTick = exportTick,
                TransferPhase = phase,
                DebugNotes = debugNotes
            };
            entry.EnsureInitialized();
            this.Entries.Add(entry);
            return entry;
        }

        internal ShuttleHolderLaunchManifestEntry FindEntryByThingID(int thingID)
        {
            this.EnsureInitialized();
            if (thingID <= 0)
            {
                return null;
            }

            for (int i = 0; i < this.Entries.Count; i++)
            {
                ShuttleHolderLaunchManifestEntry entry = this.Entries[i];
                if (entry != null && entry.ThingID == thingID)
                {
                    return entry;
                }
            }

            return null;
        }

        internal List<ShuttleHolderLaunchManifestEntry> FindEntriesByActivityID(string activityID)
        {
            this.EnsureInitialized();
            List<ShuttleHolderLaunchManifestEntry> matches = new List<ShuttleHolderLaunchManifestEntry>();
            if (string.IsNullOrEmpty(activityID))
            {
                return matches;
            }

            for (int i = 0; i < this.Entries.Count; i++)
            {
                ShuttleHolderLaunchManifestEntry entry = this.Entries[i];
                if (entry != null && entry.ActivityID == activityID)
                {
                    matches.Add(entry);
                }
            }

            return matches;
        }

        internal List<ShuttleHolderLaunchManifestEntry> FindEntriesByHolderKind(string holderKind)
        {
            this.EnsureInitialized();
            List<ShuttleHolderLaunchManifestEntry> matches = new List<ShuttleHolderLaunchManifestEntry>();
            if (string.IsNullOrEmpty(holderKind))
            {
                return matches;
            }

            for (int i = 0; i < this.Entries.Count; i++)
            {
                ShuttleHolderLaunchManifestEntry entry = this.Entries[i];
                if (entry != null && entry.HolderKind == holderKind)
                {
                    matches.Add(entry);
                }
            }

            return matches;
        }

        internal void RemoveHabitatLivingEntries()
        {
            this.EnsureInitialized();
            for (int i = this.Entries.Count - 1; i >= 0; i--)
            {
                ShuttleHolderLaunchManifestEntry entry = this.Entries[i];
                if (entry == null ||
                    entry.HolderKind == ShuttleHolderLaunchManifestConstants.HabitatSleepHolderKind ||
                    entry.HolderKind == ShuttleHolderLaunchManifestConstants.HabitatDiningPawnHolderKind ||
                    entry.HolderKind == ShuttleHolderLaunchManifestConstants.HabitatDiningFoodHolderKind)
                {
                    this.Entries.RemoveAt(i);
                }
            }
        }

        internal void RemoveHabitatJoyEntries()
        {
            this.EnsureInitialized();
            for (int i = this.Entries.Count - 1; i >= 0; i--)
            {
                ShuttleHolderLaunchManifestEntry entry = this.Entries[i];
                if (entry == null ||
                    entry.HolderKind == ShuttleHolderLaunchManifestConstants.HabitatJoyHolderKind)
                {
                    this.Entries.RemoveAt(i);
                }
            }
        }

        internal void RemoveMedicalBayPatientEntries()
        {
            this.EnsureInitialized();
            for (int i = this.Entries.Count - 1; i >= 0; i--)
            {
                ShuttleHolderLaunchManifestEntry entry = this.Entries[i];
                if (entry == null ||
                    entry.HolderKind == ShuttleHolderLaunchManifestConstants.MedicalBayPatientHolderKind)
                {
                    this.Entries.RemoveAt(i);
                }
            }
        }

        internal void RemoveMechChargerEntries()
        {
            this.EnsureInitialized();
            for (int i = this.Entries.Count - 1; i >= 0; i--)
            {
                ShuttleHolderLaunchManifestEntry entry = this.Entries[i];
                if (entry == null ||
                    entry.HolderKind == ShuttleHolderLaunchManifestConstants.MechChargerHolderKind)
                {
                    this.Entries.RemoveAt(i);
                }
            }
        }

        internal void RemovePrisonCellPrisonerEntries()
        {
            this.EnsureInitialized();
            for (int i = this.Entries.Count - 1; i >= 0; i--)
            {
                ShuttleHolderLaunchManifestEntry entry = this.Entries[i];
                if (entry == null ||
                    entry.HolderKind == ShuttleHolderLaunchManifestConstants.PrisonCellPrisonerHolderKind)
                {
                    this.Entries.RemoveAt(i);
                }
            }
        }

        internal string DumpForDebug()
        {
            this.EnsureInitialized();
            StringBuilder builder = new StringBuilder();
            builder.Append("manifestVersion=");
            builder.Append(this.ManifestVersion);
            builder.Append(" entries=");
            builder.Append(this.Entries.Count);

            for (int i = 0; i < this.Entries.Count; i++)
            {
                ShuttleHolderLaunchManifestEntry entry = this.Entries[i];
                builder.AppendLine();
                builder.Append("  [");
                builder.Append(i);
                builder.Append("] ");
                builder.Append(entry != null ? entry.DumpForDebug() : "null");
            }

            return builder.ToString();
        }
    }

    /// <summary>
    /// One restore/export record for a holder Thing. Dining uses paired entries
    /// linked by ActivityID so pawn and food can restore as one transaction.
    /// </summary>
    internal sealed class ShuttleHolderLaunchManifestEntry : IExposable
    {
        internal int ManifestVersion = ShuttleHolderLaunchManifestConstants.CurrentManifestVersion;
        internal string HolderKind = ShuttleHolderLaunchManifestConstants.DevTestHolderKind;
        internal int ThingID = -1;
        internal string DefName;
        internal string Label;
        internal string SourceHolder = ShuttleHolderLaunchManifestConstants.DevTestSourceHolder;
        internal string ActivityKind;
        internal string ActivityID;
        internal string RecordKind;
        internal int OriginalRecordIndex = -1;
        internal int RestStartTick = -1;
        internal int RestedTicks;
        internal bool IsSleeping;
        internal int FoodThingID = -1;
        internal string FoodDefName;
        internal string FoodLabel;
        internal int FoodStackCount;
        internal int DiningStartTick = -1;
        internal int ChewTicksLeft;
        internal int ChewTicksTotal;
        internal bool IsDining;
        internal bool Finalized;
        internal string JoyKindDefName;
        internal string JoyKindLabel;
        internal int JoyStartTick = -1;
        internal int JoyTicks;
        internal float JoyGainRate = 1f;
        internal int MaxJoyTicks = 4000;
        internal bool IsJoying;
        internal string AdmissionMode;
        internal int AdmissionTick = -1;
        internal bool WasDownedOnAdmission;
        internal string LastKnownReasonLabel;
        internal string LastKnownSeverityLabel;
        internal Faction OriginalFaction;
        internal Faction HostFaction;
        internal PrisonerInteractionModeDef InteractionMode;
        internal bool WasPrisonerOnAdmission;
        internal bool Released;
        internal bool PendingRelease;
        internal int LastFedTick = -1;
        internal float LastFedNutrition;
        internal string LastFedFoodLabel;
        internal int LastTendedTick = -1;
        internal string LastTendMedicineLabel;
        internal int LastAutoFeedAttemptTick = -1;
        internal int LastAutoFeedSuccessTick = -1;
        internal int LastAutoFeedFailureTick = -1;
        internal string LastAutoFeedFailureReason;
        internal string ModuleInstanceID;
        internal float ChargeRateFactor = 1f;
        internal int HolderEntryTick = -1;
        internal int LastChargeTick = -1;
        internal int ExportTick = -1;
        internal string TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseNone;
        internal string DebugNotes;

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.ManifestVersion, "manifestVersion", ShuttleHolderLaunchManifestConstants.CurrentManifestVersion);
            Scribe_Values.Look(ref this.HolderKind, "holderKind", ShuttleHolderLaunchManifestConstants.DevTestHolderKind);
            Scribe_Values.Look(ref this.ThingID, "thingID", -1);
            Scribe_Values.Look(ref this.DefName, "defName", null);
            Scribe_Values.Look(ref this.Label, "label", null);
            Scribe_Values.Look(ref this.SourceHolder, "sourceHolder", ShuttleHolderLaunchManifestConstants.DevTestSourceHolder);
            Scribe_Values.Look(ref this.ActivityKind, "activityKind", null);
            Scribe_Values.Look(ref this.ActivityID, "activityID", null);
            Scribe_Values.Look(ref this.RecordKind, "recordKind", null);
            Scribe_Values.Look(ref this.OriginalRecordIndex, "originalRecordIndex", -1);
            Scribe_Values.Look(ref this.RestStartTick, "restStartTick", -1);
            Scribe_Values.Look(ref this.RestedTicks, "restedTicks", 0);
            Scribe_Values.Look(ref this.IsSleeping, "isSleeping", false);
            Scribe_Values.Look(ref this.FoodThingID, "foodThingID", -1);
            Scribe_Values.Look(ref this.FoodDefName, "foodDefName", null);
            Scribe_Values.Look(ref this.FoodLabel, "foodLabel", null);
            Scribe_Values.Look(ref this.FoodStackCount, "foodStackCount", 0);
            Scribe_Values.Look(ref this.DiningStartTick, "diningStartTick", -1);
            Scribe_Values.Look(ref this.ChewTicksLeft, "chewTicksLeft", 0);
            Scribe_Values.Look(ref this.ChewTicksTotal, "chewTicksTotal", 0);
            Scribe_Values.Look(ref this.IsDining, "isDining", false);
            Scribe_Values.Look(ref this.Finalized, "finalized", false);
            Scribe_Values.Look(ref this.JoyKindDefName, "joyKindDefName", null);
            Scribe_Values.Look(ref this.JoyKindLabel, "joyKindLabel", null);
            Scribe_Values.Look(ref this.JoyStartTick, "joyStartTick", -1);
            Scribe_Values.Look(ref this.JoyTicks, "joyTicks", 0);
            Scribe_Values.Look(ref this.JoyGainRate, "joyGainRate", 1f);
            Scribe_Values.Look(ref this.MaxJoyTicks, "maxJoyTicks", 4000);
            Scribe_Values.Look(ref this.IsJoying, "isJoying", false);
            Scribe_Values.Look(ref this.AdmissionMode, "admissionMode", null);
            Scribe_Values.Look(ref this.AdmissionTick, "admissionTick", -1);
            Scribe_Values.Look(ref this.WasDownedOnAdmission, "wasDownedOnAdmission", false);
            Scribe_Values.Look(ref this.LastKnownReasonLabel, "lastKnownReasonLabel", null);
            Scribe_Values.Look(ref this.LastKnownSeverityLabel, "lastKnownSeverityLabel", null);
            Scribe_References.Look(ref this.OriginalFaction, "originalFaction");
            Scribe_References.Look(ref this.HostFaction, "hostFaction");
            Scribe_Defs.Look(ref this.InteractionMode, "interactionMode");
            Scribe_Values.Look(ref this.WasPrisonerOnAdmission, "wasPrisonerOnAdmission", false);
            Scribe_Values.Look(ref this.Released, "released", false);
            Scribe_Values.Look(ref this.PendingRelease, "pendingRelease", false);
            Scribe_Values.Look(ref this.LastFedTick, "lastFedTick", -1);
            Scribe_Values.Look(ref this.LastFedNutrition, "lastFedNutrition", 0f);
            Scribe_Values.Look(ref this.LastFedFoodLabel, "lastFedFoodLabel", null);
            Scribe_Values.Look(ref this.LastTendedTick, "lastTendedTick", -1);
            Scribe_Values.Look(ref this.LastTendMedicineLabel, "lastTendMedicineLabel", null);
            Scribe_Values.Look(ref this.LastAutoFeedAttemptTick, "lastAutoFeedAttemptTick", -1);
            Scribe_Values.Look(ref this.LastAutoFeedSuccessTick, "lastAutoFeedSuccessTick", -1);
            Scribe_Values.Look(ref this.LastAutoFeedFailureTick, "lastAutoFeedFailureTick", -1);
            Scribe_Values.Look(ref this.LastAutoFeedFailureReason, "lastAutoFeedFailureReason", null);
            Scribe_Values.Look(ref this.ModuleInstanceID, "moduleInstanceID", null);
            Scribe_Values.Look(ref this.ChargeRateFactor, "chargeRateFactor", 1f);
            Scribe_Values.Look(ref this.HolderEntryTick, "holderEntryTick", -1);
            Scribe_Values.Look(ref this.LastChargeTick, "lastChargeTick", -1);
            Scribe_Values.Look(ref this.ExportTick, "exportTick", -1);
            Scribe_Values.Look(ref this.TransferPhase, "transferPhase", ShuttleHolderLaunchManifestConstants.PhaseNone);
            Scribe_Values.Look(ref this.DebugNotes, "debugNotes", null);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.EnsureInitialized();
            }
        }

        internal void EnsureInitialized()
        {
            if (this.ManifestVersion <= 0)
            {
                this.ManifestVersion = ShuttleHolderLaunchManifestConstants.CurrentManifestVersion;
            }

            if (string.IsNullOrEmpty(this.HolderKind))
            {
                this.HolderKind = ShuttleHolderLaunchManifestConstants.DevTestHolderKind;
            }

            if (string.IsNullOrEmpty(this.SourceHolder))
            {
                if (this.HolderKind == ShuttleHolderLaunchManifestConstants.DevTestHolderKind)
                {
                    this.SourceHolder = ShuttleHolderLaunchManifestConstants.DevTestSourceHolder;
                }
                else if (this.HolderKind == ShuttleHolderLaunchManifestConstants.MedicalBayPatientHolderKind)
                {
                    this.SourceHolder = ShuttleHolderLaunchManifestConstants.MedicalBaySourceHolder;
                }
                else if (this.HolderKind == ShuttleHolderLaunchManifestConstants.MechChargerHolderKind)
                {
                    this.SourceHolder = ShuttleHolderLaunchManifestConstants.MechChargerSourceHolder;
                }
                else if (this.HolderKind == ShuttleHolderLaunchManifestConstants.PrisonCellPrisonerHolderKind)
                {
                    this.SourceHolder = ShuttleHolderLaunchManifestConstants.PrisonCellSourceHolder;
                }
                else
                {
                    this.SourceHolder = ShuttleHolderLaunchManifestConstants.HabitatSourceHolder;
                }
            }

            if (string.IsNullOrEmpty(this.TransferPhase))
            {
                this.TransferPhase = ShuttleHolderLaunchManifestConstants.PhaseNone;
            }

            if (string.IsNullOrEmpty(this.ActivityKind))
            {
                if (this.HolderKind == ShuttleHolderLaunchManifestConstants.HabitatSleepHolderKind)
                {
                    this.ActivityKind = ShuttleHolderLaunchManifestConstants.ActivityKindSleep;
                }
                else if (this.HolderKind == ShuttleHolderLaunchManifestConstants.HabitatDiningPawnHolderKind ||
                    this.HolderKind == ShuttleHolderLaunchManifestConstants.HabitatDiningFoodHolderKind)
                {
                    this.ActivityKind = ShuttleHolderLaunchManifestConstants.ActivityKindDining;
                }
                else if (this.HolderKind == ShuttleHolderLaunchManifestConstants.HabitatJoyHolderKind)
                {
                    this.ActivityKind = ShuttleHolderLaunchManifestConstants.ActivityKindJoy;
                }
            }

            if (string.IsNullOrEmpty(this.RecordKind))
            {
                if (this.HolderKind == ShuttleHolderLaunchManifestConstants.HabitatSleepHolderKind)
                {
                    this.RecordKind = ShuttleHolderLaunchManifestConstants.RecordKindHabitatSleepPawn;
                }
                else if (this.HolderKind == ShuttleHolderLaunchManifestConstants.HabitatDiningPawnHolderKind)
                {
                    this.RecordKind = ShuttleHolderLaunchManifestConstants.RecordKindHabitatDiningPawn;
                }
                else if (this.HolderKind == ShuttleHolderLaunchManifestConstants.HabitatDiningFoodHolderKind)
                {
                    this.RecordKind = ShuttleHolderLaunchManifestConstants.RecordKindHabitatDiningFood;
                }
                else if (this.HolderKind == ShuttleHolderLaunchManifestConstants.HabitatJoyHolderKind)
                {
                    this.RecordKind = ShuttleHolderLaunchManifestConstants.RecordKindHabitatJoyPawn;
                }
                else if (this.HolderKind == ShuttleHolderLaunchManifestConstants.MedicalBayPatientHolderKind)
                {
                    this.RecordKind = ShuttleHolderLaunchManifestConstants.RecordKindMedicalBayPatient;
                }
                else if (this.HolderKind == ShuttleHolderLaunchManifestConstants.MechChargerHolderKind)
                {
                    this.RecordKind = ShuttleHolderLaunchManifestConstants.RecordKindMechChargerPawn;
                }
                else if (this.HolderKind == ShuttleHolderLaunchManifestConstants.PrisonCellPrisonerHolderKind)
                {
                    this.RecordKind = ShuttleHolderLaunchManifestConstants.RecordKindPrisonCellPrisoner;
                }
                else if (this.HolderKind == ShuttleHolderLaunchManifestConstants.DevTestHolderKind)
                {
                    this.RecordKind = ShuttleHolderLaunchManifestConstants.RecordKindDevTest;
                }
            }

            if (this.OriginalRecordIndex < -1)
            {
                this.OriginalRecordIndex = -1;
            }

            if (this.RestStartTick < -1)
            {
                this.RestStartTick = -1;
            }

            if (this.RestedTicks < 0)
            {
                this.RestedTicks = 0;
            }

            if (this.FoodThingID < -1)
            {
                this.FoodThingID = -1;
            }

            if (this.FoodStackCount < 0)
            {
                this.FoodStackCount = 0;
            }

            if (this.DiningStartTick < -1)
            {
                this.DiningStartTick = -1;
            }

            if (this.ChewTicksLeft < 0)
            {
                this.ChewTicksLeft = 0;
            }

            if (this.ChewTicksTotal < 0)
            {
                this.ChewTicksTotal = 0;
            }

            if (this.ChewTicksTotal > 0 && this.ChewTicksLeft > this.ChewTicksTotal)
            {
                this.ChewTicksLeft = this.ChewTicksTotal;
            }

            if (this.JoyStartTick < -1)
            {
                this.JoyStartTick = -1;
            }

            if (this.JoyTicks < 0)
            {
                this.JoyTicks = 0;
            }

            if (this.JoyGainRate <= 0f ||
                float.IsNaN(this.JoyGainRate) ||
                float.IsInfinity(this.JoyGainRate))
            {
                this.JoyGainRate = 1f;
            }

            if (this.MaxJoyTicks < 1)
            {
                this.MaxJoyTicks = 4000;
            }

            if (this.AdmissionMode == null)
            {
                this.AdmissionMode = string.Empty;
            }

            if (this.AdmissionTick < -1)
            {
                this.AdmissionTick = -1;
            }

            if (this.LastKnownReasonLabel == null)
            {
                this.LastKnownReasonLabel = string.Empty;
            }

            if (this.LastKnownSeverityLabel == null)
            {
                this.LastKnownSeverityLabel = string.Empty;
            }

            if (this.HolderKind == ShuttleHolderLaunchManifestConstants.PrisonCellPrisonerHolderKind)
            {
                if (this.PendingRelease && this.Released)
                {
                    this.PendingRelease = false;
                }

                if (this.LastFedTick < -1)
                {
                    this.LastFedTick = -1;
                }

                if (this.LastFedNutrition < 0f ||
                    float.IsNaN(this.LastFedNutrition) ||
                    float.IsInfinity(this.LastFedNutrition))
                {
                    this.LastFedNutrition = 0f;
                }

                if (this.LastFedFoodLabel == null)
                {
                    this.LastFedFoodLabel = string.Empty;
                }

                if (this.LastTendedTick < -1)
                {
                    this.LastTendedTick = -1;
                }

                if (this.LastTendMedicineLabel == null)
                {
                    this.LastTendMedicineLabel = string.Empty;
                }

                this.LastAutoFeedAttemptTick = SanitizeTick(this.LastAutoFeedAttemptTick);
                this.LastAutoFeedSuccessTick = SanitizeTick(this.LastAutoFeedSuccessTick);
                this.LastAutoFeedFailureTick = SanitizeTick(this.LastAutoFeedFailureTick);
                if (this.LastAutoFeedFailureReason == null)
                {
                    this.LastAutoFeedFailureReason = string.Empty;
                }
            }

            if (this.ModuleInstanceID == null)
            {
                this.ModuleInstanceID = string.Empty;
            }

            if (float.IsNaN(this.ChargeRateFactor) ||
                float.IsInfinity(this.ChargeRateFactor) ||
                this.ChargeRateFactor <= 0f)
            {
                this.ChargeRateFactor = 1f;
            }

            if (this.HolderEntryTick < -1)
            {
                this.HolderEntryTick = -1;
            }

            if (this.LastChargeTick < -1)
            {
                this.LastChargeTick = -1;
            }
        }

        private static int SanitizeTick(int tick)
        {
            return tick < -1 ? -1 : tick;
        }

        internal string DumpForDebug()
        {
            this.EnsureInitialized();
            StringBuilder builder = new StringBuilder();
            builder.Append("thingID=");
            builder.Append(this.ThingID);
            builder.Append(" defName=");
            builder.Append(this.DefName ?? "null");
            builder.Append(" label=");
            builder.Append(this.Label ?? "null");
            builder.Append(" holderKind=");
            builder.Append(this.HolderKind ?? "null");
            builder.Append(" sourceHolder=");
            builder.Append(this.SourceHolder ?? "null");
            if (!string.IsNullOrEmpty(this.ActivityKind))
            {
                builder.Append(" activityKind=");
                builder.Append(this.ActivityKind);
            }

            if (!string.IsNullOrEmpty(this.ActivityID))
            {
                builder.Append(" activityID=");
                builder.Append(this.ActivityID);
            }

            if (!string.IsNullOrEmpty(this.RecordKind))
            {
                builder.Append(" recordKind=");
                builder.Append(this.RecordKind);
            }

            if (this.OriginalRecordIndex >= 0)
            {
                builder.Append(" originalRecordIndex=");
                builder.Append(this.OriginalRecordIndex);
            }

            if (this.HolderKind == ShuttleHolderLaunchManifestConstants.HabitatSleepHolderKind)
            {
                builder.Append(" restStartTick=");
                builder.Append(this.RestStartTick);
                builder.Append(" restedTicks=");
                builder.Append(this.RestedTicks);
                builder.Append(" isSleeping=");
                builder.Append(this.IsSleeping);
            }

            if (this.ActivityKind == ShuttleHolderLaunchManifestConstants.ActivityKindDining ||
                this.HolderKind == ShuttleHolderLaunchManifestConstants.HabitatDiningPawnHolderKind ||
                this.HolderKind == ShuttleHolderLaunchManifestConstants.HabitatDiningFoodHolderKind)
            {
                builder.Append(" foodThingID=");
                builder.Append(this.FoodThingID);
                builder.Append(" foodDefName=");
                builder.Append(this.FoodDefName ?? "null");
                builder.Append(" foodLabel=");
                builder.Append(this.FoodLabel ?? "null");
                builder.Append(" foodStackCount=");
                builder.Append(this.FoodStackCount);
                builder.Append(" diningStartTick=");
                builder.Append(this.DiningStartTick);
                builder.Append(" chewTicksLeft=");
                builder.Append(this.ChewTicksLeft);
                builder.Append(" chewTicksTotal=");
                builder.Append(this.ChewTicksTotal);
                builder.Append(" isDining=");
                builder.Append(this.IsDining);
                builder.Append(" finalized=");
                builder.Append(this.Finalized);
            }

            if (this.ActivityKind == ShuttleHolderLaunchManifestConstants.ActivityKindJoy ||
                this.HolderKind == ShuttleHolderLaunchManifestConstants.HabitatJoyHolderKind)
            {
                builder.Append(" joyKindDefName=");
                builder.Append(this.JoyKindDefName ?? "null");
                builder.Append(" joyKindLabel=");
                builder.Append(this.JoyKindLabel ?? "null");
                builder.Append(" joyStartTick=");
                builder.Append(this.JoyStartTick);
                builder.Append(" joyTicks=");
                builder.Append(this.JoyTicks);
                builder.Append(" joyGainRate=");
                builder.Append(this.JoyGainRate);
                builder.Append(" maxJoyTicks=");
                builder.Append(this.MaxJoyTicks);
                builder.Append(" isJoying=");
                builder.Append(this.IsJoying);
            }

            if (this.HolderKind == ShuttleHolderLaunchManifestConstants.MedicalBayPatientHolderKind)
            {
                builder.Append(" admissionMode=");
                builder.Append(this.AdmissionMode ?? "null");
                builder.Append(" admissionTick=");
                builder.Append(this.AdmissionTick);
                builder.Append(" wasDownedOnAdmission=");
                builder.Append(this.WasDownedOnAdmission);
                builder.Append(" lastKnownReasonLabel=");
                builder.Append(this.LastKnownReasonLabel ?? "null");
                builder.Append(" lastKnownSeverityLabel=");
                builder.Append(this.LastKnownSeverityLabel ?? "null");
            }

            if (this.HolderKind == ShuttleHolderLaunchManifestConstants.MechChargerHolderKind)
            {
                builder.Append(" moduleInstanceID=");
                builder.Append(this.ModuleInstanceID ?? "null");
                builder.Append(" chargeRateFactor=");
                builder.Append(this.ChargeRateFactor);
                builder.Append(" holderEntryTick=");
                builder.Append(this.HolderEntryTick);
                builder.Append(" lastChargeTick=");
                builder.Append(this.LastChargeTick);
            }

            if (this.HolderKind == ShuttleHolderLaunchManifestConstants.PrisonCellPrisonerHolderKind)
            {
                builder.Append(" admissionTick=");
                builder.Append(this.AdmissionTick);
                builder.Append(" hostFaction=");
                builder.Append(this.HostFaction != null ? this.HostFaction.Name : "null");
                builder.Append(" interactionMode=");
                builder.Append(this.InteractionMode != null ? this.InteractionMode.defName : "null");
                builder.Append(" wasPrisonerOnAdmission=");
                builder.Append(this.WasPrisonerOnAdmission);
                builder.Append(" lastFedTick=");
                builder.Append(this.LastFedTick);
                builder.Append(" lastTendedTick=");
                builder.Append(this.LastTendedTick);
                builder.Append(" lastAutoFeedAttemptTick=");
                builder.Append(this.LastAutoFeedAttemptTick);
            }

            builder.Append(" exportTick=");
            builder.Append(this.ExportTick);
            builder.Append(" phase=");
            builder.Append(this.TransferPhase ?? "null");
            if (!string.IsNullOrEmpty(this.DebugNotes))
            {
                builder.Append(" notes=");
                builder.Append(this.DebugNotes);
            }

            return builder.ToString();
        }
    }
}
