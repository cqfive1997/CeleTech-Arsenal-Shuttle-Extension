using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Settings
{
    internal static class V3SettingsReleaseNotesStyle
    {
        internal const float ScrollbarReserve = 16f;
        internal const float CardPadding = 10f;
        internal const float CardGap = 8f;
        internal const float HeaderGap = 6f;
        internal const float SectionHeaderGap = 8f;
        internal const float EntryGap = 6f;
        internal const float NumberGap = 8f;
        internal const float SectionNumberWidth = 32f;
        internal const float SectionNumberHeight = 24f;
        internal const float EntryNumberWidth = 34f;
        internal const float AccentWidth = 3f;

        private static readonly Color[] SectionColors =
        {
            ShuttleUIStyle.BlueStatusColor,
            ShuttleUIStyle.GreenStatusColor,
            ShuttleUIStyle.YellowStatusColor
        };

        internal static Color GetSectionColor(int index)
        {
            int safeIndex = Mathf.Max(0, index);
            return SectionColors[safeIndex % SectionColors.Length];
        }
    }

    internal sealed class V3SettingsReleaseNotesLayoutCache
    {
        private V3SettingsReleaseNotesLayout cachedLayout;

        internal V3SettingsReleaseNotesLayout GetLayout(
            ShuttleReleaseNotesReadModel notes,
            string version,
            float width)
        {
            int widthKey = Mathf.Max(0, Mathf.RoundToInt(width));
            string languageKey = this.ResolveLanguageKey();
            if (this.cachedLayout == null ||
                !object.ReferenceEquals(this.cachedLayout.Source, notes) ||
                this.cachedLayout.WidthKey != widthKey ||
                !string.Equals(this.cachedLayout.Version, version, StringComparison.Ordinal) ||
                !string.Equals(
                    this.cachedLayout.LanguageKey,
                    languageKey,
                    StringComparison.Ordinal))
            {
                this.cachedLayout =
                    this.BuildLayout(notes, version, languageKey, widthKey);
            }

            return this.cachedLayout;
        }

        private V3SettingsReleaseNotesLayout BuildLayout(
            ShuttleReleaseNotesReadModel notes,
            string version,
            string languageKey,
            int widthKey)
        {
            V3SettingsReleaseNotesLayout layout =
                new V3SettingsReleaseNotesLayout();
            layout.Source = notes;
            layout.Version = version;
            layout.LanguageKey = languageKey;
            layout.WidthKey = widthKey;
            layout.Title = ShuttleUIText.Tr(notes.TitleFormatKey, version);
            layout.Intro = ShuttleUIText.Tr(notes.IntroKey);

            float cardWidth = Mathf.Max(40f, widthKey);
            float textWidth = Mathf.Max(
                20f,
                cardWidth - (V3SettingsReleaseNotesStyle.CardPadding * 2f));
            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            bool oldWordWrap = Text.WordWrap;
            try
            {
                Text.Anchor = TextAnchor.UpperLeft;
                Text.WordWrap = true;

                Text.Font = GameFont.Medium;
                layout.TitleHeight = this.MeasureText(layout.Title, textWidth);
                Text.Font = GameFont.Small;
                layout.IntroHeight = this.MeasureText(layout.Intro, textWidth);
                layout.HeaderHeight =
                    V3SettingsReleaseNotesStyle.CardPadding +
                    layout.TitleHeight +
                    V3SettingsReleaseNotesStyle.HeaderGap +
                    layout.IntroHeight +
                    V3SettingsReleaseNotesStyle.CardPadding;

                for (int i = 0; i < notes.Sections.Count; i++)
                {
                    layout.Sections.Add(
                        this.BuildSectionLayout(notes.Sections[i], cardWidth));
                }

                for (int i = 0; i < notes.FooterKeys.Count; i++)
                {
                    V3SettingsReleaseNotesTextLayout footer =
                        new V3SettingsReleaseNotesTextLayout();
                    footer.Text = ShuttleUIText.Tr(notes.FooterKeys[i]);
                    footer.Height = this.MeasureText(footer.Text, textWidth);
                    layout.Footers.Add(footer);
                }
            }
            finally
            {
                Text.Font = oldFont;
                Text.Anchor = oldAnchor;
                Text.WordWrap = oldWordWrap;
            }

            this.CalculateContentHeight(layout);
            return layout;
        }

        private V3SettingsReleaseNotesSectionLayout BuildSectionLayout(
            ShuttleReleaseNotesSectionReadModel source,
            float cardWidth)
        {
            V3SettingsReleaseNotesSectionLayout section =
                new V3SettingsReleaseNotesSectionLayout();
            section.Title = ShuttleUIText.Tr(source != null ? source.TitleKey : null);

            float titleWidth = Mathf.Max(
                20f,
                cardWidth -
                (V3SettingsReleaseNotesStyle.CardPadding * 2f) -
                V3SettingsReleaseNotesStyle.SectionNumberWidth -
                V3SettingsReleaseNotesStyle.NumberGap);
            Text.Font = GameFont.Small;
            section.TitleHeight = this.MeasureText(section.Title, titleWidth);

            float entryWidth = Mathf.Max(
                20f,
                cardWidth -
                (V3SettingsReleaseNotesStyle.CardPadding * 2f) -
                V3SettingsReleaseNotesStyle.EntryNumberWidth -
                V3SettingsReleaseNotesStyle.NumberGap);
            int entryCount = source != null ? source.EntryKeys.Count : 0;
            float entriesHeight = 0f;
            for (int i = 0; i < entryCount; i++)
            {
                V3SettingsReleaseNotesTextLayout entry =
                    new V3SettingsReleaseNotesTextLayout();
                entry.Text = ShuttleUIText.Tr(source.EntryKeys[i]);
                entry.Height = this.MeasureText(entry.Text, entryWidth);
                section.Entries.Add(entry);
                entriesHeight += entry.Height;
                if (i + 1 < entryCount)
                {
                    entriesHeight += V3SettingsReleaseNotesStyle.EntryGap;
                }
            }

            section.Height =
                V3SettingsReleaseNotesStyle.CardPadding +
                Mathf.Max(
                    V3SettingsReleaseNotesStyle.SectionNumberHeight,
                    section.TitleHeight) +
                (entryCount > 0
                    ? V3SettingsReleaseNotesStyle.SectionHeaderGap + entriesHeight
                    : 0f) +
                V3SettingsReleaseNotesStyle.CardPadding;
            return section;
        }

        private void CalculateContentHeight(V3SettingsReleaseNotesLayout layout)
        {
            float contentHeight = layout.HeaderHeight;
            for (int i = 0; i < layout.Sections.Count; i++)
            {
                contentHeight +=
                    V3SettingsReleaseNotesStyle.CardGap +
                    layout.Sections[i].Height;
            }

            if (layout.Footers.Count > 0)
            {
                layout.FooterHeight =
                    V3SettingsReleaseNotesStyle.CardPadding * 2f;
                for (int i = 0; i < layout.Footers.Count; i++)
                {
                    if (i > 0)
                    {
                        layout.FooterHeight +=
                            V3SettingsReleaseNotesStyle.HeaderGap;
                    }

                    layout.FooterHeight += layout.Footers[i].Height;
                }

                contentHeight +=
                    V3SettingsReleaseNotesStyle.CardGap +
                    layout.FooterHeight;
            }

            layout.ContentHeight = contentHeight;
        }

        private float MeasureText(string text, float width)
        {
            return Mathf.Ceil(
                Mathf.Max(
                    Text.LineHeight,
                    Text.CalcHeight(
                        ShuttleUILayout.ResolveSafeDisplayText(text),
                        Mathf.Max(1f, width))));
        }

        private string ResolveLanguageKey()
        {
            try
            {
                return LanguageDatabase.activeLanguage != null
                    ? LanguageDatabase.activeLanguage.folderName ?? string.Empty
                    : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    internal sealed class V3SettingsReleaseNotesLayout
    {
        internal ShuttleReleaseNotesReadModel Source;
        internal string Version;
        internal string LanguageKey;
        internal int WidthKey;
        internal string Title;
        internal string Intro;
        internal float TitleHeight;
        internal float IntroHeight;
        internal float HeaderHeight;
        internal float FooterHeight;
        internal float ContentHeight;
        internal readonly List<V3SettingsReleaseNotesSectionLayout> Sections =
            new List<V3SettingsReleaseNotesSectionLayout>();
        internal readonly List<V3SettingsReleaseNotesTextLayout> Footers =
            new List<V3SettingsReleaseNotesTextLayout>();
    }

    internal sealed class V3SettingsReleaseNotesSectionLayout
    {
        internal string Title;
        internal float TitleHeight;
        internal float Height;
        internal readonly List<V3SettingsReleaseNotesTextLayout> Entries =
            new List<V3SettingsReleaseNotesTextLayout>();
    }

    internal sealed class V3SettingsReleaseNotesTextLayout
    {
        internal string Text;
        internal float Height;
    }
}
