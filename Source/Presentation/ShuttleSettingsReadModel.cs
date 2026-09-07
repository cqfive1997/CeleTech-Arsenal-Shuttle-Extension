using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.Presentation
{
    internal sealed class ShuttleSettingsReadModel
    {
        internal string ModVersion;
        internal string AuthorsSummary;
        internal string CreditsSummary;
        internal string CreditsTooltip;
        internal string EffectiveVisualQuality;
        internal string EffectiveHeaderMeterStyle;
        internal string EffectiveRefreshPreset;
        internal string EffectiveRefreshSummary;
        internal readonly List<ShuttleSettingsRow> SettingRows = new List<ShuttleSettingsRow>();
        internal readonly List<ShuttleSettingsRow> EffectiveSettingRows = new List<ShuttleSettingsRow>();
        internal readonly List<ShuttleSettingsRow> HelpRows = new List<ShuttleSettingsRow>();
        internal readonly List<ShuttleSettingsRow> ChangelogRows = new List<ShuttleSettingsRow>();
        internal readonly List<ShuttleSettingsRow> GuideRows = new List<ShuttleSettingsRow>();
        internal ShuttleReleaseNotesReadModel ReleaseNotes;
        internal readonly List<ShuttleInfoCardReadModel> GameplayGuideCards = new List<ShuttleInfoCardReadModel>();
    }

    internal sealed class ShuttleSettingsRow
    {
        internal string Label;
        internal string Summary;
        internal string Tooltip;
        internal int Number;
    }

    internal sealed class ShuttleInfoCardReadModel
    {
        internal string TitleKey;
        internal string SummaryKey;
        internal string BodyKey;
        internal int Number;
    }

    internal sealed class ShuttleReleaseNotesReadModel
    {
        internal string TitleFormatKey;
        internal string IntroKey;
        internal readonly List<ShuttleReleaseNotesSectionReadModel> Sections =
            new List<ShuttleReleaseNotesSectionReadModel>();
        internal readonly List<string> FooterKeys = new List<string>();
    }

    internal sealed class ShuttleReleaseNotesSectionReadModel
    {
        internal string TitleKey;
        internal readonly List<string> EntryKeys = new List<string>();
    }
}
