using System.Collections.Generic;
using System.Globalization;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Settings
{
    internal sealed class V3SettingsReleaseNotesDrawer
    {
        private readonly V3SettingsReleaseNotesLayoutCache layoutCache =
            new V3SettingsReleaseNotesLayoutCache();

        internal void Draw(
            Rect rect,
            ShuttleReleaseNotesReadModel notes,
            string version,
            ref Vector2 scroll)
        {
            if (notes == null || rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            float viewWidth = Mathf.Max(
                0f,
                rect.width - V3SettingsReleaseNotesStyle.ScrollbarReserve);
            V3SettingsReleaseNotesLayout layout =
                this.layoutCache.GetLayout(notes, version, viewWidth);
            Rect viewRect = new Rect(
                0f,
                0f,
                viewWidth,
                Mathf.Max(rect.height, layout.ContentHeight));

            Widgets.BeginScrollView(rect, ref scroll, viewRect);
            try
            {
                this.DrawLayout(viewRect.width, layout);
            }
            finally
            {
                Widgets.EndScrollView();
            }
        }

        private void DrawLayout(
            float width,
            V3SettingsReleaseNotesLayout layout)
        {
            Color oldColor = GUI.color;
            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            bool oldWordWrap = Text.WordWrap;
            try
            {
                Text.Anchor = TextAnchor.UpperLeft;
                Text.WordWrap = true;

                float y = 0f;
                this.DrawHeader(
                    new Rect(0f, y, width, layout.HeaderHeight),
                    layout);
                y += layout.HeaderHeight;

                for (int i = 0; i < layout.Sections.Count; i++)
                {
                    V3SettingsReleaseNotesSectionLayout section =
                        layout.Sections[i];
                    y += V3SettingsReleaseNotesStyle.CardGap;
                    this.DrawSection(
                        new Rect(0f, y, width, section.Height),
                        section,
                        i);
                    y += section.Height;
                }

                if (layout.Footers.Count > 0)
                {
                    y += V3SettingsReleaseNotesStyle.CardGap;
                    this.DrawFooters(
                        new Rect(0f, y, width, layout.FooterHeight),
                        layout.Footers);
                }
            }
            finally
            {
                GUI.color = oldColor;
                Text.Font = oldFont;
                Text.Anchor = oldAnchor;
                Text.WordWrap = oldWordWrap;
            }
        }

        private void DrawHeader(
            Rect rect,
            V3SettingsReleaseNotesLayout layout)
        {
            this.DrawCard(rect, ShuttleUIStyle.BlueStatusColor, true);

            Text.Font = GameFont.Medium;
            GUI.color = ShuttleUIStyle.BlueStatusColor;
            ShuttleUILayout.SafeLabel(
                new Rect(
                    rect.x + V3SettingsReleaseNotesStyle.CardPadding,
                    rect.y + V3SettingsReleaseNotesStyle.CardPadding,
                    rect.width -
                        (V3SettingsReleaseNotesStyle.CardPadding * 2f),
                    layout.TitleHeight),
                layout.Title);

            Text.Font = GameFont.Small;
            GUI.color = ShuttleUIStyle.HeaderTitleTextColor;
            ShuttleUILayout.SafeLabel(
                new Rect(
                    rect.x + V3SettingsReleaseNotesStyle.CardPadding,
                    rect.y +
                        V3SettingsReleaseNotesStyle.CardPadding +
                        layout.TitleHeight +
                        V3SettingsReleaseNotesStyle.HeaderGap,
                    rect.width -
                        (V3SettingsReleaseNotesStyle.CardPadding * 2f),
                    layout.IntroHeight),
                layout.Intro);
        }

        private void DrawSection(
            Rect rect,
            V3SettingsReleaseNotesSectionLayout section,
            int sectionIndex)
        {
            Color accent =
                V3SettingsReleaseNotesStyle.GetSectionColor(sectionIndex);
            this.DrawCard(rect, accent, false);

            Rect numberRect = new Rect(
                rect.x + V3SettingsReleaseNotesStyle.CardPadding,
                rect.y + V3SettingsReleaseNotesStyle.CardPadding,
                V3SettingsReleaseNotesStyle.SectionNumberWidth,
                V3SettingsReleaseNotesStyle.SectionNumberHeight);
            Widgets.DrawBoxSolid(
                numberRect,
                ShuttleUIStyle.WithAlpha(accent, 0.18f));
            ShuttleUILayout.DrawRectBorder(
                numberRect,
                ShuttleUIStyle.WithAlpha(accent, 0.72f),
                ShuttleUIStyle.ThinBorder);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.WordWrap = false;
            GUI.color = accent;
            ShuttleUILayout.SafeLabel(
                numberRect,
                (sectionIndex + 1).ToString(
                    "00",
                    CultureInfo.InvariantCulture));

            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;
            GUI.color = accent;
            ShuttleUILayout.SafeLabel(
                new Rect(
                    numberRect.xMax + V3SettingsReleaseNotesStyle.NumberGap,
                    rect.y + V3SettingsReleaseNotesStyle.CardPadding,
                    Mathf.Max(
                        0f,
                        rect.xMax -
                            numberRect.xMax -
                            V3SettingsReleaseNotesStyle.NumberGap -
                            V3SettingsReleaseNotesStyle.CardPadding),
                    section.TitleHeight),
                section.Title);

            float y =
                rect.y +
                V3SettingsReleaseNotesStyle.CardPadding +
                Mathf.Max(
                    V3SettingsReleaseNotesStyle.SectionNumberHeight,
                    section.TitleHeight) +
                V3SettingsReleaseNotesStyle.SectionHeaderGap;
            for (int i = 0; i < section.Entries.Count; i++)
            {
                this.DrawEntry(
                    rect,
                    section.Entries[i],
                    sectionIndex,
                    i,
                    y,
                    accent);
                y +=
                    section.Entries[i].Height +
                    V3SettingsReleaseNotesStyle.EntryGap;
            }
        }

        private void DrawEntry(
            Rect sectionRect,
            V3SettingsReleaseNotesTextLayout entry,
            int sectionIndex,
            int entryIndex,
            float y,
            Color accent)
        {
            string number = string.Concat(
                (sectionIndex + 1).ToString(CultureInfo.InvariantCulture),
                ".",
                (entryIndex + 1).ToString(CultureInfo.InvariantCulture));

            Text.WordWrap = false;
            GUI.color = ShuttleUIStyle.WithAlpha(accent, 0.92f);
            ShuttleUILayout.SafeLabel(
                new Rect(
                    sectionRect.x + V3SettingsReleaseNotesStyle.CardPadding,
                    y,
                    V3SettingsReleaseNotesStyle.EntryNumberWidth,
                    entry.Height),
                number);

            Text.WordWrap = true;
            GUI.color = ShuttleUIStyle.HeaderTitleTextColor;
            ShuttleUILayout.SafeLabel(
                new Rect(
                    sectionRect.x +
                        V3SettingsReleaseNotesStyle.CardPadding +
                        V3SettingsReleaseNotesStyle.EntryNumberWidth +
                        V3SettingsReleaseNotesStyle.NumberGap,
                    y,
                    Mathf.Max(
                        0f,
                        sectionRect.width -
                            (V3SettingsReleaseNotesStyle.CardPadding * 2f) -
                            V3SettingsReleaseNotesStyle.EntryNumberWidth -
                            V3SettingsReleaseNotesStyle.NumberGap),
                    entry.Height),
                entry.Text);
        }

        private void DrawFooters(
            Rect rect,
            IList<V3SettingsReleaseNotesTextLayout> footers)
        {
            this.DrawCard(rect, ShuttleUIStyle.YellowStatusColor, false);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;
            float y = rect.y + V3SettingsReleaseNotesStyle.CardPadding;
            for (int i = 0; i < footers.Count; i++)
            {
                V3SettingsReleaseNotesTextLayout footer = footers[i];
                GUI.color = i == 0
                    ? ShuttleUIStyle.HeaderTitleTextColor
                    : ShuttleUIStyle.MutedTextColor;
                ShuttleUILayout.SafeLabel(
                    new Rect(
                        rect.x + V3SettingsReleaseNotesStyle.CardPadding,
                        y,
                        rect.width -
                            (V3SettingsReleaseNotesStyle.CardPadding * 2f),
                        footer.Height),
                    footer.Text);
                y +=
                    footer.Height +
                    V3SettingsReleaseNotesStyle.HeaderGap;
            }
        }

        private void DrawCard(Rect rect, Color accent, bool horizontalAccent)
        {
            ShuttleUILayout.DrawCardBackground(
                rect,
                false,
                false,
                ShuttleUIStyle.SettingsInfoRowColor);
            Widgets.DrawBoxSolid(
                horizontalAccent
                    ? new Rect(
                        rect.x,
                        rect.y,
                        rect.width,
                        V3SettingsReleaseNotesStyle.AccentWidth)
                    : new Rect(
                        rect.x,
                        rect.y,
                        V3SettingsReleaseNotesStyle.AccentWidth,
                        rect.height),
                accent);
        }
    }
}
