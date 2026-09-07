using System;
using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.AssemblyState
{
    /// <summary>
    /// Classifies the durable intent behind a cargo-region ThingFilter. ThingFilter persists a
    /// concrete Def set, while some compatibility mods make initially hidden items storable later.
    /// </summary>
    internal static class ShuttleCargoThingFilterIntentPolicy
    {
        private const string CombatExtendedAmmoDefTypeName = "CombatExtended.AmmoDef";
        private const string CombatExtendedAmmoTradeTag = "CE_Ammo";
        private const string CombatExtendedAmmoInjectorTradeTag = "CE_AmmoInjector";

        internal static bool IsCurrentDefaultAllowAll(ThingFilter filter)
        {
            return MatchesCurrentDefault(filter, false);
        }

        internal static bool IsLegacyDefaultAllowAll(ThingFilter filter)
        {
            if (filter == null)
            {
                return true;
            }

            return MatchesCurrentDefault(filter, true);
        }

        private static bool MatchesCurrentDefault(
            ThingFilter filter,
            bool tolerateMissingLateEnabledCeAmmo)
        {
            if (filter == null)
            {
                return false;
            }

            ThingFilter currentDefault = ThingFilter.CreateOnlyEverStorableThingFilter();
            if (currentDefault == null || !RangesMatch(filter, currentDefault))
            {
                return false;
            }

            List<ThingDef> thingDefs = DefDatabase<ThingDef>.AllDefsListForReading;
            for (int i = 0; i < thingDefs.Count; i++)
            {
                ThingDef thingDef = thingDefs[i];
                if (thingDef == null)
                {
                    continue;
                }

                bool expected = currentDefault.Allows(thingDef);
                bool actual = filter.Allows(thingDef);
                if (expected == actual)
                {
                    continue;
                }

                if (tolerateMissingLateEnabledCeAmmo &&
                    expected &&
                    !actual &&
                    IsLateEnabledCombatExtendedAmmo(thingDef))
                {
                    continue;
                }

                return false;
            }

            List<SpecialThingFilterDef> specialFilters =
                DefDatabase<SpecialThingFilterDef>.AllDefsListForReading;
            for (int i = 0; i < specialFilters.Count; i++)
            {
                SpecialThingFilterDef specialFilter = specialFilters[i];
                if (specialFilter != null &&
                    filter.Allows(specialFilter) != currentDefault.Allows(specialFilter))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool RangesMatch(ThingFilter left, ThingFilter right)
        {
            return left.AllowedHitPointsPercents == right.AllowedHitPointsPercents &&
                left.AllowedMentalBreakChance == right.AllowedMentalBreakChance &&
                left.AllowedQualityLevels == right.AllowedQualityLevels;
        }

        private static bool IsLateEnabledCombatExtendedAmmo(ThingDef thingDef)
        {
            Type defType = thingDef != null ? thingDef.GetType() : null;
            if (defType != null &&
                string.Equals(
                    defType.FullName,
                    CombatExtendedAmmoDefTypeName,
                    StringComparison.Ordinal))
            {
                return true;
            }

            List<string> tradeTags = thingDef != null ? thingDef.tradeTags : null;
            if (tradeTags == null)
            {
                return false;
            }

            for (int i = 0; i < tradeTags.Count; i++)
            {
                string tradeTag = tradeTags[i];
                if (string.Equals(
                        tradeTag,
                        CombatExtendedAmmoTradeTag,
                        StringComparison.Ordinal) ||
                    string.Equals(
                        tradeTag,
                        CombatExtendedAmmoInjectorTradeTag,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
