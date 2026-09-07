using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Medical
{
    /// <summary>
    /// Read-only medevac triage helper for shuttle loading UI. It does not change health,
    /// assign jobs, tend wounds, or move pawns.
    /// </summary>
    public static class MedicalEvacuationUtility
    {
        private const int DownedPriority = 10000;
        private const int SevereBleedingPriority = 9000;
        private const int LowConsciousnessPriority = 8000;
        private const int ExtremePainPriority = 7000;
        private const int MajorInjuryPriority = 6000;
        private const int ReducedMovingPriority = 5000;
        private const int BleedingPriority = 4000;
        private const float SevereBleedRateThreshold = 0.30f;

        public static bool IsMedevacCandidate(Pawn pawn)
        {
            return IsEligiblePatient(pawn) && GetMedevacPriority(pawn) > 0;
        }

        public static bool IsCriticalMedevacCandidate(Pawn pawn)
        {
            if (!IsEligiblePatient(pawn))
            {
                return false;
            }

            if (pawn.Downed)
            {
                return true;
            }

            return HasSevereBleeding(pawn) ||
                GetCapacityLevel(pawn, PawnCapacityDefOf.Consciousness) < 0.45f ||
                GetPainTotal(pawn) >= 0.75f;
        }

        public static int GetMedevacPriority(Pawn pawn)
        {
            if (!IsEligiblePatient(pawn))
            {
                return 0;
            }

            if (pawn.Downed)
            {
                return DownedPriority;
            }

            if (HasSevereBleeding(pawn))
            {
                return SevereBleedingPriority;
            }

            if (GetCapacityLevel(pawn, PawnCapacityDefOf.Consciousness) < 0.45f)
            {
                return LowConsciousnessPriority;
            }

            if (GetPainTotal(pawn) >= 0.75f)
            {
                return ExtremePainPriority;
            }

            if (HasMajorInjury(pawn))
            {
                return MajorInjuryPriority;
            }

            if (GetCapacityLevel(pawn, PawnCapacityDefOf.Moving) < 0.60f)
            {
                return ReducedMovingPriority;
            }

            if (HasBleeding(pawn))
            {
                return BleedingPriority;
            }

            return 0;
        }

        public static string GetMedevacReasonLabel(Pawn pawn)
        {
            if (!IsEligiblePatient(pawn))
            {
                return string.Empty;
            }

            if (pawn.Downed)
            {
                return "CT_Shuttle_Medevac_ReasonDowned".Translate().ToString();
            }

            if (HasSevereBleeding(pawn))
            {
                return "CT_Shuttle_Medevac_ReasonSevereBleeding".Translate().ToString();
            }

            if (GetCapacityLevel(pawn, PawnCapacityDefOf.Consciousness) < 0.45f)
            {
                return "CT_Shuttle_Medevac_ReasonLowConsciousness".Translate().ToString();
            }

            if (GetPainTotal(pawn) >= 0.75f)
            {
                return "CT_Shuttle_Medevac_ReasonExtremePain".Translate().ToString();
            }

            if (HasMajorInjury(pawn))
            {
                return "CT_Shuttle_Medevac_ReasonMajorInjury".Translate().ToString();
            }

            if (GetCapacityLevel(pawn, PawnCapacityDefOf.Moving) < 0.60f)
            {
                return "CT_Shuttle_Medevac_ReasonReducedMoving".Translate().ToString();
            }

            if (HasBleeding(pawn))
            {
                return "CT_Shuttle_Medevac_ReasonBleeding".Translate().ToString();
            }

            return string.Empty;
        }

        private static bool IsEligiblePatient(Pawn pawn)
        {
            if (pawn == null || pawn.Destroyed || pawn.Dead)
            {
                return false;
            }

            return pawn.RaceProps != null && pawn.RaceProps.Humanlike && pawn.IsColonist;
        }

        private static bool HasSevereBleeding(Pawn pawn)
        {
            float bleedRate = GetBleedRateTotal(pawn);
            if (bleedRate > 0f)
            {
                return bleedRate >= SevereBleedRateThreshold;
            }

            // TODO: Keep this as a conservative fallback for API/version edge cases where
            // total bleed-rate data is not available. It intentionally only reads hediffs.
            return GetBleedingHediffCount(pawn) >= 2;
        }

        private static bool HasBleeding(Pawn pawn)
        {
            return GetBleedRateTotal(pawn) > 0f || GetBleedingHediffCount(pawn) > 0;
        }

        private static float GetBleedRateTotal(Pawn pawn)
        {
            if (pawn == null || pawn.health == null || pawn.health.hediffSet == null)
            {
                return 0f;
            }

            return pawn.health.hediffSet.BleedRateTotal;
        }

        private static int GetBleedingHediffCount(Pawn pawn)
        {
            if (pawn == null || pawn.health == null || pawn.health.hediffSet == null ||
                pawn.health.hediffSet.hediffs == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < pawn.health.hediffSet.hediffs.Count; i++)
            {
                Hediff hediff = pawn.health.hediffSet.hediffs[i];
                if (hediff != null && hediff.Bleeding)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool HasMajorInjury(Pawn pawn)
        {
            if (pawn == null || pawn.health == null || pawn.health.hediffSet == null ||
                pawn.health.hediffSet.hediffs == null)
            {
                return false;
            }

            for (int i = 0; i < pawn.health.hediffSet.hediffs.Count; i++)
            {
                Hediff_Injury injury = pawn.health.hediffSet.hediffs[i] as Hediff_Injury;
                if (injury != null && injury.Severity >= 8f)
                {
                    return true;
                }
            }

            return false;
        }

        private static float GetPainTotal(Pawn pawn)
        {
            if (pawn == null || pawn.health == null || pawn.health.hediffSet == null)
            {
                return 0f;
            }

            return pawn.health.hediffSet.PainTotal;
        }

        private static float GetCapacityLevel(Pawn pawn, PawnCapacityDef capacityDef)
        {
            if (pawn == null || pawn.health == null || pawn.health.capacities == null || capacityDef == null)
            {
                return 1f;
            }

            return pawn.health.capacities.GetLevel(capacityDef);
        }
    }
}
