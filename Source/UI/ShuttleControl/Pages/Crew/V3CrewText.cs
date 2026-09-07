using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Crew
{
    internal sealed class V3CrewText
    {
        internal static readonly Color CardColor =
            new Color(0.040f, 0.052f, 0.066f, 0.86f);
        internal static readonly Color StrongCardColor =
            new Color(0.058f, 0.074f, 0.094f, 0.90f);
        internal static readonly Color DisabledCardColor =
            new Color(0.030f, 0.034f, 0.040f, 0.70f);
        internal static readonly Color AccentColor =
            new Color(0.22f, 0.58f, 0.80f, 0.82f);
        internal static readonly Color BlueColor =
            new Color(0.33f, 0.63f, 0.92f, 1f);
        internal static readonly Color GreenColor =
            new Color(0.33f, 0.76f, 0.47f, 1f);
        internal static readonly Color YellowColor =
            new Color(0.95f, 0.72f, 0.28f, 1f);
        internal static readonly Color RedColor =
            new Color(0.91f, 0.30f, 0.24f, 1f);
        internal static readonly Color PurpleColor =
            new Color(0.72f, 0.64f, 0.90f, 1f);

        internal string Tr(string key)
        {
            return ShuttleUIText.Tr(key);
        }

        internal string ValueOrDash(string value)
        {
            return ShuttleAssemblyDisplayTextResolver.ResolveSafeDisplayText(value);
        }

        internal string FormatPercent(float value01)
        {
            return value01 >= 0f
                ? ShuttleUIMetricFormatter.FormatPercent(value01)
                : this.Tr("CT_Shuttle_Crew_NoData");
        }

        internal string FitLabelText(string text, float width)
        {
            return ShuttleUILayout.FitSingleLineLabelText(text, width);
        }

        internal void AddTooltip(Rect rect, string tooltip)
        {
            ShuttleUITooltip.Tip(rect, tooltip);
        }

        internal void DrawThingIcon(Rect rect, Thing displayThing, string fallbackText, string tooltip)
        {
            if (ShuttleThingIconDrawer.Draw(rect, displayThing))
            {
                return;
            }

            Widgets.DrawBoxSolid(rect, new Color(0f, 0f, 0f, 0.24f));
            ShuttleUILayout.DrawRectBorder(
                rect,
                ShuttleUIStyle.SubtleBorderColor,
                ShuttleUIStyle.ThinBorder);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            ShuttleUILayout.SafeLabel(rect, this.ValueOrDash(fallbackText));
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            this.AddTooltip(
                rect,
                !string.IsNullOrEmpty(tooltip)
                    ? tooltip
                    : this.Tr("CT_ShuttleCrew_AppearanceUnavailable"));
        }

        internal bool IsEnglishLanguage()
        {
            return ShuttleUIText.IsEnglishLanguage();
        }
    }
}
