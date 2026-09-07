using System;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.MechCharging
{
    internal static class ShuttleMechChargeNeedUtility
    {
        private const float GenericRechargeLimitPct = 1f;
        private const float CompletionEpsilon = 0.0001f;

        internal static bool HasChargeNeed(Pawn mech)
        {
            Need need;
            bool usesVanillaLimit;
            return TryGetChargeNeed(mech, out need, out usesVanillaLimit);
        }

        internal static float GetChargeNeedPct(Pawn mech)
        {
            Need need;
            bool usesVanillaLimit;
            if (!TryGetChargeNeed(mech, out need, out usesVanillaLimit))
            {
                return -1f;
            }

            return Clamp01(need.CurLevelPercentage);
        }

        internal static bool IsAtRechargeLimit(Pawn mech)
        {
            Need need;
            bool usesVanillaLimit;
            if (!TryGetChargeNeed(mech, out need, out usesVanillaLimit))
            {
                return false;
            }

            if (usesVanillaLimit)
            {
                return need.CurLevel >= GetVanillaRechargeLimit(mech) - CompletionEpsilon;
            }

            return Clamp01(need.CurLevelPercentage) >= GenericRechargeLimitPct - CompletionEpsilon;
        }

        internal static bool TryCharge(Pawn mech, float chargePctPerTick, out bool reachedLimit)
        {
            reachedLimit = false;
            Need need;
            bool usesVanillaLimit;
            if (!TryGetChargeNeed(mech, out need, out usesVanillaLimit))
            {
                return false;
            }

            float charge = SanitizeCharge(chargePctPerTick);
            if (usesVanillaLimit)
            {
                float maxLimit = GetVanillaRechargeLimit(mech);
                if (need.CurLevel >= maxLimit)
                {
                    reachedLimit = true;
                    return true;
                }

                float nextLevel = need.CurLevel + charge;
                need.CurLevel = nextLevel > maxLimit ? maxLimit : nextLevel;
                reachedLimit = need.CurLevel >= maxLimit - CompletionEpsilon;
                return true;
            }

            float maxLevel = need.MaxLevel;
            if (float.IsNaN(maxLevel) || float.IsInfinity(maxLevel) || maxLevel <= 0f)
            {
                return false;
            }

            float currentPct = Clamp01(need.CurLevelPercentage);
            if (currentPct < 0f)
            {
                return false;
            }

            if (currentPct >= GenericRechargeLimitPct)
            {
                reachedLimit = true;
                return true;
            }

            float nextPct = currentPct + charge;
            if (nextPct > GenericRechargeLimitPct)
            {
                nextPct = GenericRechargeLimitPct;
            }

            need.CurLevel = nextPct * maxLevel;
            reachedLimit = nextPct >= GenericRechargeLimitPct - CompletionEpsilon;
            return true;
        }

        internal static bool ShouldAutoRecharge(Pawn mech, float thresholdPct)
        {
            float pct = GetChargeNeedPct(mech);
            return pct >= 0f && pct < thresholdPct;
        }

        private static bool TryGetChargeNeed(
            Pawn mech,
            out Need need,
            out bool usesVanillaLimit)
        {
            need = null;
            usesVanillaLimit = false;
            if (mech == null || mech.needs == null)
            {
                return false;
            }

            if (IsUsableNeed(mech.needs.energy))
            {
                need = mech.needs.energy;
                usesVanillaLimit = true;
                return true;
            }

            if (mech.needs.AllNeeds == null)
            {
                return false;
            }

            for (int i = 0; i < mech.needs.AllNeeds.Count; i++)
            {
                Need candidate = mech.needs.AllNeeds[i];
                if (IsChargeNeedCandidate(candidate))
                {
                    need = candidate;
                    return true;
                }
            }

            return false;
        }

        private static bool IsUsableNeed(Need need)
        {
            if (need == null)
            {
                return false;
            }

            float maxLevel = need.MaxLevel;
            return !float.IsNaN(maxLevel) &&
                !float.IsInfinity(maxLevel) &&
                maxLevel > 0f;
        }

        private static bool IsChargeNeedCandidate(Need need)
        {
            if (!IsUsableNeed(need))
            {
                return false;
            }

            Type type = need.GetType();
            if (MatchesChargeNeedIdentifier(type != null ? type.Name : null))
            {
                return true;
            }

            NeedDef def = need.def;
            if (def == null)
            {
                return false;
            }

            return MatchesChargeNeedIdentifier(def.defName) ||
                MatchesChargeNeedIdentifier(def.label);
        }

        private static bool MatchesChargeNeedIdentifier(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            string normalized = value.Replace("_", string.Empty)
                .Replace("-", string.Empty)
                .Replace(" ", string.Empty)
                .ToLowerInvariant();

            return normalized.Contains("mechenergy") ||
                normalized.Contains("mechcharge") ||
                normalized.Contains("mechpower") ||
                normalized.Contains("mechanitorenergy") ||
                normalized.Contains("energy") ||
                normalized.Contains("battery") ||
                normalized.Contains("charge");
        }

        private static float GetVanillaRechargeLimit(Pawn mech)
        {
            float maxLimit = JobGiver_GetEnergy.GetMaxRechargeLimit(mech);
            if (float.IsNaN(maxLimit) || float.IsInfinity(maxLimit) || maxLimit <= 0f)
            {
                return 1f;
            }

            return maxLimit;
        }

        private static float SanitizeCharge(float charge)
        {
            return !float.IsNaN(charge) && !float.IsInfinity(charge) && charge > 0f
                ? charge
                : 0f;
        }

        private static float Clamp01(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return -1f;
            }

            if (value < 0f)
            {
                return 0f;
            }

            if (value > 1f)
            {
                return 1f;
            }

            return value;
        }
    }
}
