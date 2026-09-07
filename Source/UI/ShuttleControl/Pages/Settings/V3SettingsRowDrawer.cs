using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Settings
{
    internal sealed class V3SettingsRowDrawer
    {
        private static readonly Color CardColor = ShuttleUIStyle.SettingsInfoRowColor;
        private static readonly Color AccentColor = ShuttleUIStyle.BlueStatusColor;

        internal void DrawPanelTitle(Rect rect, string title)
        {
            ShuttleUIPanelChrome.DrawPanelTitle(rect, title);
        }

        internal void DrawRows(
            Rect rect,
            IList<ShuttleSettingsRow> rows,
            ref Vector2 scroll,
            float rowHeight)
        {
            float gap = 6f;
            int count = rows != null ? rows.Count : 0;
            Rect viewRect = new Rect(
                0f,
                0f,
                Mathf.Max(0f, rect.width - 16f),
                Mathf.Max(rect.height, count * (rowHeight + gap)));

            Widgets.BeginScrollView(rect, ref scroll, viewRect);
            try
            {
                float y = 0f;
                for (int i = 0; i < count; i++)
                {
                    this.DrawSettingRow(
                        new Rect(0f, y, viewRect.width, rowHeight),
                        rows[i]);
                    y += rowHeight + gap;
                }
            }
            finally
            {
                Widgets.EndScrollView();
            }
        }

        internal void DrawInfoCards(
            Rect rect,
            IList<ShuttleInfoCardReadModel> cards,
            ref Vector2 scroll)
        {
            float cardHeight = 92f;
            float gap = 8f;
            int count = cards != null ? cards.Count : 0;
            Rect viewRect = new Rect(
                0f,
                0f,
                Mathf.Max(0f, rect.width - 16f),
                Mathf.Max(rect.height, count * (cardHeight + gap)));

            Widgets.BeginScrollView(rect, ref scroll, viewRect);
            try
            {
                float y = 0f;
                for (int i = 0; i < count; i++)
                {
                    this.DrawInfoCard(
                        new Rect(0f, y, viewRect.width, cardHeight),
                        cards[i]);
                    y += cardHeight + gap;
                }
            }
            finally
            {
                Widgets.EndScrollView();
            }
        }

        internal bool DrawActionButton(
            Rect rect,
            string label,
            bool danger,
            string tooltip)
        {
            return danger
                ? ShuttleUIActionButtonDrawer.DrawDangerButton(rect, label, true, tooltip)
                : ShuttleUIActionButtonDrawer.DrawPrimaryButton(rect, label, true, tooltip);
        }

        internal bool DrawCheckbox(
            Rect rect,
            string label,
            ref bool value,
            string tooltip)
        {
            bool oldValue = value;
            bool hovered = Mouse.IsOver(rect);
            ShuttleUILayout.DrawCardBackground(rect, false, false, CardColor);
            ShuttleUILayout.DrawRectBorder(
                rect,
                hovered ? AccentColor : ShuttleUIStyle.ButtonBorderColor,
                hovered ? ShuttleUIStyle.ThickBorder : ShuttleUIStyle.ThinBorder);
            Text.Font = rect.height <= 30f ? GameFont.Tiny : GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = ShuttleUIStyle.ButtonTextColor;
            Widgets.CheckboxLabeled(
                new Rect(rect.x + 8f, rect.y + 2f, rect.width - 16f, rect.height - 4f),
                ShuttleUILayout.FitSingleLineLabelText(label, rect.width - 48f),
                ref value);
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            ShuttleUITooltip.Tip(rect, tooltip);
            return oldValue != value;
        }

        private void DrawSettingRow(Rect rect, ShuttleSettingsRow row)
        {
            ShuttleUILayout.DrawCardBackground(rect, false, false, CardColor);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, 4f, rect.height), AccentColor);
            Text.Font = GameFont.Tiny;
            GUI.color = Color.white;
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x + 12f, rect.y + 7f, rect.width - 24f, 18f),
                row != null ? row.Label : "-",
                GameFont.Tiny,
                GameFont.Tiny,
                Color.white,
                row != null ? row.Tooltip : null,
                TextAnchor.MiddleLeft);
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.SafeLabel(
                new Rect(rect.x + 12f, rect.y + 28f, rect.width - 24f, rect.height - 32f),
                row != null ? row.Summary : "-");
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            ShuttleUITooltip.Tip(rect, row != null ? row.Tooltip : null);
        }

        private void DrawInfoCard(Rect rect, ShuttleInfoCardReadModel card)
        {
            bool hovered = Mouse.IsOver(rect);
            Color bgColor = hovered
                ? ShuttleUIStyle.WithAlpha(ShuttleUIStyle.BlueStatusColor, 0.16f)
                : CardColor;
            ShuttleUILayout.DrawCardBackground(rect, false, false, bgColor);
            ShuttleUILayout.DrawRectBorder(
                rect,
                hovered ? ShuttleUIStyle.BlueStatusColor : ShuttleUIStyle.SubtleBorderColor,
                hovered ? ShuttleUIStyle.ThickBorder : ShuttleUIStyle.ThinBorder);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, 4f, rect.height), AccentColor);
            Text.Font = GameFont.Tiny;
            GUI.color = Color.white;
            string title = card != null ? ShuttleUIText.Tr(card.TitleKey) : "-";
            string summary = card != null ? ShuttleUIText.Tr(card.SummaryKey) : "-";
            string body = card != null ? ShuttleUIText.Tr(card.BodyKey) : "-";
            ShuttleUILayout.DrawFittedSingleLineLabel(
                new Rect(rect.x + 12f, rect.y + 8f, rect.width - 24f, 18f),
                title,
                GameFont.Tiny,
                GameFont.Tiny,
                Color.white,
                title,
                TextAnchor.MiddleLeft);
            GUI.color = ShuttleUIStyle.MutedTextColor;
            bool oldWrap = Text.WordWrap;
            Text.WordWrap = true;
            ShuttleUILayout.SafeLabel(
                new Rect(rect.x + 12f, rect.y + 30f, rect.width - 24f, 22f),
                summary);
            ShuttleUILayout.SafeLabel(
                new Rect(rect.x + 12f, rect.y + 55f, rect.width - 24f, rect.height - 60f),
                body);
            Text.WordWrap = oldWrap;
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            ShuttleUITooltip.Tip(
                rect,
                title + "\n" + summary + "\n\n" + body);
        }

    }
}
