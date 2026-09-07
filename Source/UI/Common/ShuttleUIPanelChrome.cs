using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common
{
    internal static class ShuttleUIPanelChrome
    {
        internal static void DrawPanelTitle(Rect rect, string title)
        {
            ShuttleUILayout.DrawPanelBackground(rect);
            ShuttleUILayout.DrawSectionHeader(new Rect(rect.x, rect.y, rect.width, 32f), title);
        }

        internal static void DrawCompactPanelTitle(Rect rect, string title)
        {
            GameFont oldFont = Text.Font;
            Color oldColor = GUI.color;

            try
            {
                ShuttleUILayout.DrawPanelBackground(rect);
                Text.Font = GameFont.Small;
                GUI.color = Color.white;
                ShuttleUILayout.SafeLabel(
                    new Rect(
                        rect.x + ShuttleUIStyle.LargeGap,
                        rect.y + 5f,
                        Mathf.Max(0f, rect.width - (ShuttleUIStyle.LargeGap * 2f)),
                        24f),
                    title);
            }
            finally
            {
                GUI.color = oldColor;
                Text.Font = oldFont;
            }
        }

        internal static Rect GetPanelInnerRect(Rect rect, float topOffset)
        {
            return new Rect(
                rect.x + ShuttleUIStyle.StandardPadding,
                rect.y + topOffset,
                Mathf.Max(0f, rect.width - (ShuttleUIStyle.StandardPadding * 2f)),
                Mathf.Max(0f, rect.height - topOffset - ShuttleUIStyle.StandardPadding));
        }

        internal static void DrawEmptyPanelMessage(Rect rect, string label)
        {
            Rect cardRect = new Rect(rect.x, rect.y, rect.width, Mathf.Min(48f, rect.height));
            ShuttleUILayout.DrawCardBackground(cardRect, false, true);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(
                    cardRect.x + ShuttleUIStyle.LargeGap,
                    cardRect.y + 14f,
                    Mathf.Max(0f, cardRect.width - (ShuttleUIStyle.LargeGap * 2f)),
                    20f),
                label,
                GameFont.Small,
                GameFont.Tiny,
                ShuttleUIStyle.MutedTextColor,
                label,
                TextAnchor.MiddleLeft);
        }

        internal static void DrawCenteredEmptyPanelMessage(Rect rect, string label)
        {
            ShuttleUILayout.DrawFittedSingleLineLabel(
                rect,
                label,
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.MutedTextColor,
                label,
                TextAnchor.MiddleCenter);
        }
    }
}
