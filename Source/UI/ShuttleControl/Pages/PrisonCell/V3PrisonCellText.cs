using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.PrisonCell
{
    internal sealed class V3PrisonCellText
    {
        internal static readonly Color CardColor =
            ShuttleUIStyle.CardColor;
        internal static readonly Color StrongCardColor =
            ShuttleUIStyle.SelectedColor;
        internal static readonly Color MutedCardColor =
            ShuttleUIStyle.IconFallbackBackgroundColor;
        internal static readonly Color AccentColor =
            ShuttleUIStyle.BlueStatusColor;
        internal static readonly Color GreenColor =
            ShuttleUIStyle.GreenStatusColor;
        internal static readonly Color YellowColor =
            ShuttleUIStyle.YellowStatusColor;
        internal static readonly Color RedColor =
            ShuttleUIStyle.RedStatusColor;
        internal static readonly Color BlueColor =
            ShuttleUIStyle.BlueStatusColor;

        internal string Tr(string key)
        {
            return ShuttleUIText.Tr(key);
        }

        internal string ValueOrDash(string value)
        {
            return ShuttleAssemblyDisplayTextResolver.ResolveSafeDisplayText(value);
        }

        internal string ValueOrFallback(string value, string fallback)
        {
            return !string.IsNullOrEmpty(value)
                ? value
                : ShuttleAssemblyDisplayTextResolver.ResolveSafeDisplayText(fallback);
        }

        internal string FitLabelText(string text, float width)
        {
            return ShuttleUILayout.FitSingleLineLabelText(text, width);
        }

        internal void AddTooltip(Rect rect, string tooltip)
        {
            ShuttleUITooltip.Tip(rect, tooltip);
        }

        internal void DrawThingIcon(Rect rect, Thing displayThing)
        {
            this.DrawThingIcon(rect, displayThing, "P");
        }

        internal void DrawThingIcon(Rect rect, Thing displayThing, string fallback)
        {
            Widgets.DrawBoxSolid(rect, MutedCardColor);
            Widgets.DrawBox(rect, 1);
            Rect iconRect = rect.ContractedBy(3f);
            if (ShuttleThingIconDrawer.Draw(iconRect, displayThing))
            {
                return;
            }

            ShuttleUILayout.DrawIconOrFallback(iconRect, null, fallback, 1f);
        }
    }
}
