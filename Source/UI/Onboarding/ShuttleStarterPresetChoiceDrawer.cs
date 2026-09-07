using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared.Dialogs;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Onboarding
{
    internal static class ShuttleStarterPresetChoiceDrawer
    {
        internal static bool Draw(
            Rect rect,
            string title,
            string body,
            string consequence,
            bool primary)
        {
            bool hovered = Mouse.IsOver(rect);
            Color background = primary
                ? new Color(0.075f, 0.175f, 0.205f, 0.96f)
                : new Color(0.075f, 0.095f, 0.120f, 0.94f);
            if (hovered)
            {
                background = primary
                    ? new Color(0.105f, 0.245f, 0.280f, 0.98f)
                    : new Color(0.105f, 0.145f, 0.180f, 0.98f);
            }

            Widgets.DrawBoxSolid(rect, background);
            Widgets.DrawBoxSolid(
                new Rect(rect.x, rect.y, 5f, rect.height),
                primary
                    ? ShuttleV3DialogStyle.PrimaryButtonBorderColor
                    : ShuttleV3DialogStyle.ButtonBorderColor);
            ShuttleV3DialogLayout.DrawRectBorder(
                rect,
                hovered
                    ? ShuttleV3DialogStyle.HeaderTitleTextColor
                    : ShuttleV3DialogStyle.SubtleBorderColor,
                hovered ? 2f : 1f);

            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            bool oldWordWrap = Text.WordWrap;
            Color oldColor = GUI.color;
            try
            {
                Text.Anchor = TextAnchor.UpperLeft;
                Text.WordWrap = true;
                Text.Font = GameFont.Medium;
                GUI.color = primary
                    ? ShuttleV3DialogStyle.PrimaryButtonTextColor
                    : ShuttleV3DialogStyle.HeaderTitleTextColor;
                Widgets.Label(
                    new Rect(rect.x + 20f, rect.y + 12f, rect.width - 40f, 30f),
                    title);

                Text.Font = GameFont.Small;
                GUI.color = Color.white;
                Widgets.Label(
                    new Rect(rect.x + 20f, rect.y + 48f, rect.width - 40f, 56f),
                    body);

                Text.Font = GameFont.Tiny;
                GUI.color = primary
                    ? ShuttleV3DialogStyle.GreenStatusColor
                    : ShuttleV3DialogStyle.MutedTextColor;
                Widgets.Label(
                    new Rect(rect.x + 20f, rect.yMax - 31f, rect.width - 40f, 22f),
                    consequence);
            }
            finally
            {
                Text.Font = oldFont;
                Text.Anchor = oldAnchor;
                Text.WordWrap = oldWordWrap;
                GUI.color = oldColor;
            }

            return Widgets.ButtonInvisible(rect);
        }
    }
}
