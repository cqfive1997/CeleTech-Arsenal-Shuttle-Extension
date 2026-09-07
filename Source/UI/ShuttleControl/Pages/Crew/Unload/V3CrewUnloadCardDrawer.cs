using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Crew
{
    internal sealed class V3CrewUnloadCardDrawer
    {
        private readonly V3CrewText text = new V3CrewText();

        internal bool Draw(Rect rect, V3CrewUnloadOption option, bool selected)
        {
            if (option == null || option.Card == null)
            {
                return false;
            }

            bool oldWordWrap = Text.WordWrap;
            TextAnchor oldAnchor = Text.Anchor;
            GameFont oldFont = Text.Font;

            try
            {
                this.DrawCardBackground(rect, selected, !option.CanUnload, option.Card.CardColor);
                this.DrawCheckbox(new Rect(rect.x + 10f, rect.y + 24f, 24f, 24f), selected, option.CanUnload);
                this.text.DrawThingIcon(
                    new Rect(rect.x + 44f, rect.y + 12f, 46f, 46f),
                    option.Card.DisplayThing,
                    option.Card.FallbackIconText,
                    option.Card.AppearanceTooltip);
                this.DrawText(new Rect(rect.x + 100f, rect.y + 8f, rect.width - 112f, rect.height - 16f), option);
            }
            finally
            {
                Text.WordWrap = oldWordWrap;
                Text.Anchor = oldAnchor;
                Text.Font = oldFont;
                GUI.color = Color.white;
            }

            this.text.AddTooltip(rect, this.BuildTooltip(option));
            return option.CanUnload && Widgets.ButtonInvisible(rect);
        }

        private void DrawCardBackground(Rect rect, bool selected, bool disabled, Color cardColor)
        {
            Color color = cardColor.a > 0f ? cardColor : V3CrewText.CardColor;
            if (selected)
            {
                color = V3CrewText.StrongCardColor;
            }

            if (disabled)
            {
                color = new Color(color.r, color.g, color.b, color.a * 0.56f);
            }

            Widgets.DrawBoxSolid(rect, color);
            if (selected)
            {
                Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, 4f, rect.height), V3CrewText.AccentColor);
            }

            Widgets.DrawBox(rect, 1);
        }

        private void DrawCheckbox(Rect rect, bool selected, bool enabled)
        {
            Color color = enabled ? V3CrewText.BlueColor : ShuttleUIStyle.MutedTextColor;
            Widgets.DrawBoxSolid(rect, new Color(0f, 0f, 0f, 0.24f));
            Widgets.DrawBox(rect, 1);
            if (!selected)
            {
                return;
            }

            Widgets.DrawBoxSolid(
                new Rect(rect.x + 5f, rect.y + 5f, rect.width - 10f, rect.height - 10f),
                color);
        }

        private void DrawText(Rect rect, V3CrewUnloadOption option)
        {
            V3CrewCardModel card = option.Card;
            Color primaryColor = option.CanUnload
                ? Color.white
                : ShuttleUIStyle.MutedTextColor;
            Text.WordWrap = false;
            this.DrawFittedLine(
                new Rect(rect.x, rect.y, rect.width, 22f),
                string.IsNullOrEmpty(card.Label) ? "-" : card.Label,
                GameFont.Small,
                primaryColor,
                TextAnchor.MiddleLeft);
            this.DrawFittedLine(
                new Rect(rect.x, rect.y + 24f, rect.width, 18f),
                this.BuildSourceLine(card),
                GameFont.Tiny,
                card.CategoryTextColor.a > 0f ? card.CategoryTextColor : V3CrewText.AccentColor,
                TextAnchor.MiddleLeft);
            this.DrawFittedLine(
                new Rect(rect.x, rect.y + 44f, rect.width, 18f),
                this.BuildNoteLine(option),
                GameFont.Tiny,
                option.CanUnload ? this.GetActivityTextColor(card) : V3CrewText.RedColor,
                TextAnchor.MiddleLeft);
        }

        private void DrawFittedLine(
            Rect rect,
            string value,
            GameFont font,
            Color color,
            TextAnchor anchor)
        {
            Text.Font = font;
            Text.Anchor = anchor;
            GUI.color = color;
            ShuttleUILayout.SafeLabel(rect, this.text.FitLabelText(value, rect.width));
            GUI.color = Color.white;
        }

        private string BuildSourceLine(V3CrewCardModel card)
        {
            if (card == null)
            {
                return "-";
            }

            string source = !string.IsNullOrEmpty(card.CompartmentLabel)
                ? card.CompartmentLabel
                : "-";
            string identity = !string.IsNullOrEmpty(card.IdentityLabel)
                ? card.IdentityLabel
                : null;
            return string.IsNullOrEmpty(identity) ? source : source + " - " + identity;
        }

        private string BuildNoteLine(V3CrewUnloadOption option)
        {
            if (option == null || option.Card == null)
            {
                return "-";
            }

            if (!option.CanUnload)
            {
                return string.IsNullOrEmpty(option.DisabledReason)
                    ? this.text.Tr("CT_Shuttle_UI_ActionUnavailableYet")
                    : option.DisabledReason;
            }

            if (!string.IsNullOrEmpty(option.Card.CurrentActivityLabel))
            {
                return option.Card.CurrentActivityLabel;
            }

            return !string.IsNullOrEmpty(option.Card.Note) ? option.Card.Note : "-";
        }

        private string BuildTooltip(V3CrewUnloadOption option)
        {
            if (option == null || option.Card == null)
            {
                return null;
            }

            if (!option.CanUnload)
            {
                return option.DisabledReason;
            }

            return option.Card.IssueTooltip;
        }

        private Color GetActivityTextColor(V3CrewCardModel card)
        {
            return card != null && card.ActivityTextColor.a > 0f
                ? card.ActivityTextColor
                : ShuttleUIStyle.MutedTextColor;
        }
    }
}
