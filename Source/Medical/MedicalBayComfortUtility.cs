using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Medical
{
    /// <summary>
    /// Passive Medical Bay comfort only supports needs/thoughts for contained,
    /// conscious patients. It never treats hediffs or changes medical state.
    /// </summary>
    internal static class MedicalBayComfortUtility
    {
        internal const int ComfortTickIntervalTicks = 250;
        private const float ConsciousnessThreshold = 0.05f;
        private const float PassiveJoyPer2500Ticks = 0.03f;
        private const string MedicalBayComfortThoughtDefName = "CT_Shuttle_MedicalBayComfort";

        internal static bool CanReceivePassiveComfort(Pawn patient, out string inactiveReason)
        {
            inactiveReason = null;
            if (patient == null || patient.Destroyed || patient.Dead)
            {
                inactiveReason = "CT_Shuttle_MedicalBay_InvalidPatient".Translate().ToString();
                return false;
            }

            if (!HasUsableConsciousness(patient))
            {
                inactiveReason = "CT_Shuttle_MedicalBay_ComfortInactiveUnconscious".Translate().ToString();
                return false;
            }

            if (patient.MentalState != null)
            {
                inactiveReason = "CT_Shuttle_MedicalBay_ComfortInactiveMentalState".Translate().ToString();
                return false;
            }

            return true;
        }

        internal static bool TryApplyPassiveComfortTick(
            Pawn patient,
            MedicalBayProfile profile,
            out string inactiveReason)
        {
            return TryApplyPassiveComfortTick(patient, profile, true, out inactiveReason);
        }

        internal static bool TryApplyPassiveComfortTick(
            Pawn patient,
            MedicalBayProfile profile,
            bool medicalBayPowered,
            out string inactiveReason)
        {
            inactiveReason = GetBasePassiveComfortInactiveReason(patient, profile, medicalBayPowered);
            if (!string.IsNullOrEmpty(inactiveReason))
            {
                return false;
            }

            bool joyActive = string.IsNullOrEmpty(
                GetPassiveJoyInactiveReason(patient, profile, medicalBayPowered));
            bool moodActive = string.IsNullOrEmpty(
                GetPassiveMoodComfortInactiveReason(patient, profile, medicalBayPowered));

            if (joyActive)
            {
                TryGainPassiveJoy(patient, profile);
            }

            if (moodActive)
            {
                TryApplyComfortThought(patient, profile);
            }

            if (joyActive || moodActive)
            {
                inactiveReason = string.Empty;
                return true;
            }

            inactiveReason = GetPassiveComfortInactiveReason(patient, profile, medicalBayPowered);
            return false;
        }

        internal static string GetPassiveComfortInactiveReason(
            Pawn patient,
            MedicalBayProfile profile)
        {
            return GetPassiveComfortInactiveReason(patient, profile, true);
        }

        internal static string GetPassiveComfortInactiveReason(
            Pawn patient,
            MedicalBayProfile profile,
            bool medicalBayPowered)
        {
            string baseReason = GetBasePassiveComfortInactiveReason(patient, profile, medicalBayPowered);
            if (!string.IsNullOrEmpty(baseReason))
            {
                return baseReason;
            }

            string joyReason = GetPassiveJoyInactiveReason(patient, profile, medicalBayPowered);
            string moodReason = GetPassiveMoodComfortInactiveReason(patient, profile, medicalBayPowered);
            if (string.IsNullOrEmpty(joyReason) || string.IsNullOrEmpty(moodReason))
            {
                return string.Empty;
            }

            return joyReason;
        }

        internal static string GetPassiveJoyInactiveReason(
            Pawn patient,
            MedicalBayProfile profile,
            bool medicalBayPowered)
        {
            string baseReason = GetBasePassiveComfortInactiveReason(patient, profile, medicalBayPowered);
            if (!string.IsNullOrEmpty(baseReason))
            {
                return baseReason;
            }

            if (patient.needs == null || patient.needs.joy == null)
            {
                return "CT_Shuttle_MedicalBay_ComfortInactiveNoJoyNeed".Translate().ToString();
            }

            if (patient.needs.joy.CurLevelPercentage >= Clamp01(profile.PassiveJoyCapPct) - 0.0001f)
            {
                return "CT_Shuttle_MedicalBay_ComfortInactiveAtCap".Translate().ToString();
            }

            return string.Empty;
        }

        internal static string GetPassiveMoodComfortInactiveReason(
            Pawn patient,
            MedicalBayProfile profile,
            bool medicalBayPowered)
        {
            string baseReason = GetBasePassiveComfortInactiveReason(patient, profile, medicalBayPowered);
            if (!string.IsNullOrEmpty(baseReason))
            {
                return baseReason;
            }

            if (patient.needs == null ||
                patient.needs.mood == null ||
                patient.needs.mood.thoughts == null ||
                patient.needs.mood.thoughts.memories == null)
            {
                return "CT_Shuttle_MedicalBay_ComfortInactiveNoMoodMemory".Translate().ToString();
            }

            ThoughtDef thoughtDef = GetComfortThoughtDef();
            if (thoughtDef == null || thoughtDef.stages == null || thoughtDef.stages.Count == 0)
            {
                return "CT_Shuttle_MedicalBay_ComfortInactiveThoughtUnavailable".Translate().ToString();
            }

            return string.Empty;
        }

        private static string GetBasePassiveComfortInactiveReason(
            Pawn patient,
            MedicalBayProfile profile,
            bool medicalBayPowered)
        {
            if (profile == null ||
                !profile.HasMedicalBay ||
                !profile.SupportsPassiveComfort ||
                profile.MedicalPatientSlots <= 0)
            {
                return "CT_Shuttle_MedicalBay_ComfortInactiveUnsupported".Translate().ToString();
            }

            if (!medicalBayPowered)
            {
                return "CT_Shuttle_MedicalBay_ComfortInactiveUnpowered".Translate().ToString();
            }

            string pawnReason;
            if (!CanReceivePassiveComfort(patient, out pawnReason))
            {
                return pawnReason;
            }

            return string.Empty;
        }

        private static void TryGainPassiveJoy(Pawn patient, MedicalBayProfile profile)
        {
            if (patient == null ||
                profile == null ||
                patient.needs == null ||
                patient.needs.joy == null)
            {
                return;
            }

            JoyKindDef joyKind = JoyKindDefOf.Meditative;
            if (joyKind == null)
            {
                return;
            }

            float capPct = Clamp01(profile.PassiveJoyCapPct);
            float currentPct = patient.needs.joy.CurLevelPercentage;
            if (currentPct >= capPct)
            {
                return;
            }

            float rawGain = profile.PassiveJoyGainFactor *
                PassiveJoyPer2500Ticks *
                ComfortTickIntervalTicks /
                2500f;
            float maxGain = (capPct - currentPct) * patient.needs.joy.MaxLevel;
            float gain = rawGain < maxGain ? rawGain : maxGain;
            if (gain > 0f)
            {
                patient.needs.joy.GainJoy(gain, joyKind);
            }
        }

        private static void TryApplyComfortThought(Pawn patient, MedicalBayProfile profile)
        {
            if (patient == null ||
                profile == null ||
                patient.needs == null ||
                patient.needs.mood == null ||
                patient.needs.mood.thoughts == null ||
                patient.needs.mood.thoughts.memories == null)
            {
                return;
            }

            ThoughtDef thoughtDef = GetComfortThoughtDef();
            if (thoughtDef == null || thoughtDef.stages == null || thoughtDef.stages.Count == 0)
            {
                return;
            }

            if (patient.needs.mood.thoughts.memories.GetFirstMemoryOfDef(thoughtDef) != null)
            {
                return;
            }

            int stageIndex = profile.ComfortThoughtStageIndex;
            if (stageIndex < 0)
            {
                stageIndex = 0;
            }
            else if (stageIndex >= thoughtDef.stages.Count)
            {
                stageIndex = thoughtDef.stages.Count - 1;
            }

            patient.needs.mood.thoughts.memories.TryGainMemory(
                ThoughtMaker.MakeThought(thoughtDef, stageIndex),
                null);
        }

        private static ThoughtDef GetComfortThoughtDef()
        {
            return DefDatabase<ThoughtDef>.GetNamedSilentFail(MedicalBayComfortThoughtDefName);
        }

        private static bool HasUsableConsciousness(Pawn patient)
        {
            return patient != null &&
                patient.health != null &&
                patient.health.capacities != null &&
                patient.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness) > ConsciousnessThreshold;
        }

        private static float Clamp01(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
            {
                return 0f;
            }

            return value > 1f ? 1f : value;
        }
    }
}
