using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Crew
{
    internal sealed class V3CrewDefinitionStyleResolver
    {
        internal void ApplyCrewKindDisplay(
            V3CrewCardModel card,
            string kindKey,
            string fallbackLabel,
            Color fallbackCardColor,
            Color fallbackTextColor)
        {
            if (card == null)
            {
                return;
            }

            card.KindKey = kindKey;
            card.CategoryLabel = fallbackLabel;
            card.CardColor = fallbackCardColor;
            card.CategoryTextColor = fallbackTextColor;
            card.HasCrewKindDisplay = true;

            ShuttleCrewKindDef def = this.GetCrewKindDef(kindKey);
            if (def == null)
            {
                return;
            }

            card.CategoryLabel = GetLabelOrFallback(def, fallbackLabel);
            card.CardColor = GetColorOrFallback(def.cardColor, fallbackCardColor);
            card.CategoryTextColor = GetColorOrFallback(def.textColor, fallbackTextColor);
        }

        internal void ApplyCrewActivityDisplay(
            V3CrewCardModel card,
            string activityKey,
            string fallbackLabel,
            Color fallbackColor)
        {
            if (card == null)
            {
                return;
            }

            card.ActivityKey = activityKey;
            card.ActivityLabel = fallbackLabel;
            card.ActivityTextColor = fallbackColor;
            card.HasCrewActivityDisplay = true;

            ShuttleCrewActivityDef def = this.GetCrewActivityDef(activityKey);
            if (def == null)
            {
                return;
            }

            card.ActivityLabel = GetLabelOrFallback(def, fallbackLabel);
            card.ActivityTextColor = GetColorOrFallback(def.color, fallbackColor);
        }

        private ShuttleCrewKindDef GetCrewKindDef(string kindKey)
        {
            try
            {
                return !string.IsNullOrEmpty(kindKey)
                    ? DefDatabase<ShuttleCrewKindDef>.GetNamedSilentFail(kindKey)
                    : null;
            }
            catch
            {
                return null;
            }
        }

        private ShuttleCrewActivityDef GetCrewActivityDef(string activityKey)
        {
            try
            {
                return !string.IsNullOrEmpty(activityKey)
                    ? DefDatabase<ShuttleCrewActivityDef>.GetNamedSilentFail(activityKey)
                    : null;
            }
            catch
            {
                return null;
            }
        }

        private static string GetLabelOrFallback(Def def, string fallback)
        {
            return ShuttleAssemblyDisplayTextResolver.ResolveDefDisplayName(
                def,
                fallback);
        }

        private static Color GetColorOrFallback(Color configured, Color fallback)
        {
            if (!IsFiniteColor(configured))
            {
                return fallback;
            }

            return configured.a > 0f ? configured : fallback;
        }

        private static bool IsFiniteColor(Color value)
        {
            return IsFiniteFloat(value.r) &&
                IsFiniteFloat(value.g) &&
                IsFiniteFloat(value.b) &&
                IsFiniteFloat(value.a);
        }

        private static bool IsFiniteFloat(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
