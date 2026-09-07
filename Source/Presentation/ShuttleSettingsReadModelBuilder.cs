using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Onboarding;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Presentation
{
    internal sealed class ShuttleSettingsReadModelBuilder
    {
        private readonly ShuttleReleaseNotesReadModel releaseNotes;

        internal ShuttleSettingsReadModelBuilder()
        {
            this.releaseNotes = this.BuildReleaseNotes();
        }

        internal ShuttleSettingsReadModel Build()
        {
            ShuttleSettingsReadModel model = new ShuttleSettingsReadModel();
            model.ModVersion = ShuttleModVersionUtility.ResolveCurrentModVersionOrUnknown();
            model.ReleaseNotes = this.releaseNotes;
            model.AuthorsSummary = this.Tr("CT_Shuttle_Settings_AuthorsValue");
            model.CreditsSummary = this.Tr("CT_Shuttle_Settings_CreditsValue");
            model.CreditsTooltip = this.Tr("CT_Shuttle_Settings_CreditsTooltip");

            this.AddEffectiveSummary(model);
            this.AddSettingRows(model);
            this.AddHelpRows(model);
            this.AddChangelogRows(model);
            this.AddGuideRows(model);
            this.AddInfoCards(model);
            return model;
        }

        private void AddEffectiveSummary(ShuttleSettingsReadModel model)
        {
            ShuttleEffectiveSettings effective = CeleTechShuttleMod.EffectiveSettings;
            model.EffectiveVisualQuality = CeleTechShuttleMod.GetVisualQualityLabel(effective.VisualQuality);
            model.EffectiveHeaderMeterStyle = CeleTechShuttleMod.GetHeaderMeterStyleLabel(effective.HeaderMeterStyle);
            model.EffectiveRefreshPreset = CeleTechShuttleMod.GetRefreshPresetLabel(effective.UIRefreshPreset);
            model.EffectiveRefreshSummary = "CT_Shuttle_Settings_CurrentEffectiveRefresh"
                .Translate(effective.ControlRefreshTicks, effective.HeavyRefreshTicks)
                .ToString();

            this.AddRawRow(
                model.EffectiveSettingRows,
                this.Tr("CT_Shuttle_Settings_Summary_Visual"),
                model.EffectiveVisualQuality + " / " + model.EffectiveHeaderMeterStyle,
                null,
                0);
            this.AddRawRow(
                model.EffectiveSettingRows,
                this.Tr("CT_Shuttle_Settings_Summary_Refresh"),
                model.EffectiveRefreshPreset + " / " + effective.ControlRefreshTicks + "/" + effective.HeavyRefreshTicks,
                null,
                0);
            this.AddRawRow(
                model.EffectiveSettingRows,
                this.Tr("CT_Shuttle_Settings_Summary_Advanced"),
                this.Tr(effective.EnableUIProfiler ? "CT_Shuttle_Common_Yes" : "CT_Shuttle_Common_No"),
                null,
                0);
            this.AddRawRow(
                model.EffectiveSettingRows,
                this.Tr("CT_Shuttle_CombatTuning_Title"),
                this.Tr(CeleTechShuttleMod.EffectiveCombatTuning.AdvancedCombatTuningEnabled
                    ? "CT_Shuttle_Common_Yes"
                    : "CT_Shuttle_CombatTuning_DefaultBalance"),
                null,
                0);
        }

        private void AddSettingRows(ShuttleSettingsReadModel model)
        {
            this.AddRawRow(
                model.SettingRows,
                this.Tr("CT_Shuttle_Settings_Category_Appearance"),
                model.EffectiveVisualQuality,
                this.Tr("CT_Shuttle_Settings_PerformanceHint"),
                0);
            this.AddRawRow(
                model.SettingRows,
                this.Tr("CT_Shuttle_Settings_Category_Responsiveness"),
                model.EffectiveRefreshSummary,
                this.Tr("CT_Shuttle_Settings_PerformanceHint"),
                0);
            this.AddRawRow(
                model.SettingRows,
                this.Tr("CT_Shuttle_Settings_Category_Advanced"),
                this.Tr("CT_Shuttle_Settings_ApplyScope_Immediate"),
                this.Tr("CT_Shuttle_Settings_DevModeOnly"),
                0);
            this.AddRawRow(
                model.SettingRows,
                this.Tr("CT_Shuttle_CombatTuning_Title"),
                this.Tr(CeleTechShuttleMod.EffectiveCombatTuning.AdvancedCombatTuningEnabled
                    ? "CT_Shuttle_CombatTuning_AppliesImmediately"
                    : "CT_Shuttle_CombatTuning_DefaultBalance"),
                this.Tr("CT_Shuttle_CombatTuning_SandboxWarning"),
                0);
            ShuttleOtherSettings other = CeleTechShuttleMod.Settings.Other ??
                new ShuttleOtherSettings();
            this.AddRawRow(
                model.SettingRows,
                this.Tr("CT_Shuttle_OtherSettings_GameplaySection"),
                "CT_Shuttle_OtherSettings_SummaryValue".Translate(
                    other.PayloadCapacityMultiplier.ToString("0.0"),
                    other.ConstructionWorkMultiplier.ToString("0.0"),
                    this.Tr(other.AllowColonistOnboardDeviceUseAtPlayerHome
                        ? "CT_Shuttle_Common_Yes"
                        : "CT_Shuttle_Common_No"),
                    this.Tr(other.AllowMechOnboardDeviceUseAtPlayerHome
                        ? "CT_Shuttle_Common_Yes"
                        : "CT_Shuttle_Common_No"),
                    this.ResolveStarterPresetModeLabel(),
                    other.ShareRefrigeratedCargoMassWithOverallCapacity
                        ? this.Tr("CT_Shuttle_OtherSettings_RefrigeratedSummaryShared")
                        : "CT_Shuttle_OtherSettings_RefrigeratedSummaryIndependent"
                            .Translate(
                                other.IndependentRefrigeratedCargoCapacityRatio
                                    .ToString("0.00"))
                            .ToString()).ToString(),
                this.Tr("CT_Shuttle_OtherSettings_Subtitle"),
                0);
        }

        private void AddHelpRows(ShuttleSettingsReadModel model)
        {
            this.AddRow(model.HelpRows, "CT_Shuttle_Settings_PerformanceHint", null, null, 1);
            this.AddRow(model.HelpRows, "CT_Shuttle_Settings_ApplyScope_Immediate", null, null, 2);
            this.AddRow(model.HelpRows, "CT_Shuttle_Settings_DevModeOnly", null, null, 3);
        }

        private void AddChangelogRows(ShuttleSettingsReadModel model)
        {
            this.AddRow(model.ChangelogRows, "CT_Shuttle_Settings_Changelog_Deconstruction", null, null, 0);
            this.AddRow(model.ChangelogRows, "CT_Shuttle_Settings_Changelog_KitchenButchery", null, null, 0);
            this.AddRow(model.ChangelogRows, "CT_Shuttle_Settings_Changelog_PrisonCell", null, null, 0);
            this.AddRow(model.ChangelogRows, "CT_Shuttle_Settings_Changelog_Weapons", null, null, 0);
        }

        private void AddGuideRows(ShuttleSettingsReadModel model)
        {
            this.AddRow(model.GuideRows, "CT_Shuttle_Settings_Guide_Assembly", null, null, 1);
            this.AddRow(model.GuideRows, "CT_Shuttle_Settings_Guide_CargoLogistics", null, null, 2);
            this.AddRow(model.GuideRows, "CT_Shuttle_Settings_Guide_Holders", null, null, 3);
            this.AddRow(model.GuideRows, "CT_Shuttle_Settings_Guide_Processing", null, null, 4);
            this.AddRow(model.GuideRows, "CT_Shuttle_Settings_Guide_Deconstruction", null, null, 5);
        }

        private void AddInfoCards(ShuttleSettingsReadModel model)
        {
            this.AddInfoCard(
                model.GameplayGuideCards,
                "CT_Shuttle_Guide_AboutShuttle_Title",
                "CT_Shuttle_Guide_AboutShuttle_Summary",
                "CT_Shuttle_Guide_AboutShuttle_Body",
                1);
            this.AddInfoCard(
                model.GameplayGuideCards,
                "CT_Shuttle_Guide_Segments_Title",
                "CT_Shuttle_Guide_Segments_Summary",
                "CT_Shuttle_Guide_Segments_Body",
                2);
            this.AddInfoCard(
                model.GameplayGuideCards,
                "CT_Shuttle_Guide_Modules_Title",
                "CT_Shuttle_Guide_Modules_Summary",
                "CT_Shuttle_Guide_Modules_Body",
                3);
            this.AddInfoCard(
                model.GameplayGuideCards,
                "CT_Shuttle_Guide_Cockpit_Title",
                "CT_Shuttle_Guide_Cockpit_Summary",
                "CT_Shuttle_Guide_Cockpit_Body",
                4);
            this.AddInfoCard(
                model.GameplayGuideCards,
                "CT_Shuttle_Guide_Cargo_Title",
                "CT_Shuttle_Guide_Cargo_Summary",
                "CT_Shuttle_Guide_Cargo_Body",
                5);
            this.AddInfoCard(
                model.GameplayGuideCards,
                "CT_Shuttle_Guide_Operations_Title",
                "CT_Shuttle_Guide_Operations_Summary",
                "CT_Shuttle_Guide_Operations_Body",
                6);
            this.AddInfoCard(
                model.GameplayGuideCards,
                "CT_Shuttle_Guide_Weapons_Title",
                "CT_Shuttle_Guide_Weapons_Summary",
                "CT_Shuttle_Guide_Weapons_Body",
                7);
            this.AddInfoCard(
                model.GameplayGuideCards,
                "CT_Shuttle_Guide_Shields_Title",
                "CT_Shuttle_Guide_Shields_Summary",
                "CT_Shuttle_Guide_Shields_Body",
                8);
            this.AddInfoCard(
                model.GameplayGuideCards,
                "CT_Shuttle_Guide_Processing_Title",
                "CT_Shuttle_Guide_Processing_Summary",
                "CT_Shuttle_Guide_Processing_Body",
                9);
            this.AddInfoCard(
                model.GameplayGuideCards,
                "CT_Shuttle_Guide_Medical_Title",
                "CT_Shuttle_Guide_Medical_Summary",
                "CT_Shuttle_Guide_Medical_Body",
                10);
            this.AddInfoCard(
                model.GameplayGuideCards,
                "CT_Shuttle_Guide_ThirdParty_Title",
                "CT_Shuttle_Guide_ThirdParty_Summary",
                "CT_Shuttle_Guide_ThirdParty_Body",
                11);
        }

        private ShuttleReleaseNotesReadModel BuildReleaseNotes()
        {
            ShuttleReleaseNotesReadModel notes = new ShuttleReleaseNotesReadModel();
            notes.TitleFormatKey = "CT_Shuttle_ReleaseNotes_TitleFormat";
            notes.IntroKey = "CT_Shuttle_ReleaseNotes_Intro";

            this.AddReleaseNotesSection(
                notes,
                "CT_Shuttle_ReleaseNotes_Section_Experience_Title",
                "CT_Shuttle_ReleaseNotes_Experience_01",
                "CT_Shuttle_ReleaseNotes_Experience_02",
                "CT_Shuttle_ReleaseNotes_Experience_03");
            this.AddReleaseNotesSection(
                notes,
                "CT_Shuttle_ReleaseNotes_Section_Performance_Title",
                "CT_Shuttle_ReleaseNotes_Performance_01",
                "CT_Shuttle_ReleaseNotes_Performance_02");
            this.AddReleaseNotesSection(
                notes,
                "CT_Shuttle_ReleaseNotes_Section_Weapons_Title",
                "CT_Shuttle_ReleaseNotes_Weapons_01",
                "CT_Shuttle_ReleaseNotes_Weapons_02");
            this.AddReleaseNotesSection(
                notes,
                "CT_Shuttle_ReleaseNotes_Section_Stability_Title",
                "CT_Shuttle_ReleaseNotes_Stability_01",
                "CT_Shuttle_ReleaseNotes_Stability_02");

            notes.FooterKeys.Add("CT_Shuttle_ReleaseNotes_Feedback");
            return notes;
        }

        private void AddReleaseNotesSection(
            ShuttleReleaseNotesReadModel notes,
            string titleKey,
            params string[] entryKeys)
        {
            if (notes == null)
            {
                return;
            }

            ShuttleReleaseNotesSectionReadModel section =
                new ShuttleReleaseNotesSectionReadModel();
            section.TitleKey = titleKey;
            if (entryKeys != null)
            {
                for (int i = 0; i < entryKeys.Length; i++)
                {
                    if (!string.IsNullOrEmpty(entryKeys[i]))
                    {
                        section.EntryKeys.Add(entryKeys[i]);
                    }
                }
            }

            notes.Sections.Add(section);
        }

        private void AddRow(
            List<ShuttleSettingsRow> rows,
            string labelKey,
            string summaryKey,
            string tooltip,
            int number)
        {
            if (rows == null)
            {
                return;
            }

            ShuttleSettingsRow row = new ShuttleSettingsRow();
            row.Label = this.Tr(labelKey);
            row.Summary = !string.IsNullOrEmpty(summaryKey) ? this.Tr(summaryKey) : null;
            row.Tooltip = tooltip;
            row.Number = number;
            rows.Add(row);
        }

        private void AddRawRow(
            List<ShuttleSettingsRow> rows,
            string label,
            string summary,
            string tooltip,
            int number)
        {
            if (rows == null)
            {
                return;
            }

            ShuttleSettingsRow row = new ShuttleSettingsRow();
            row.Label = label;
            row.Summary = summary;
            row.Tooltip = tooltip;
            row.Number = number;
            rows.Add(row);
        }

        private void AddInfoCard(
            List<ShuttleInfoCardReadModel> cards,
            string titleKey,
            string summaryKey,
            string bodyKey,
            int number)
        {
            if (cards == null)
            {
                return;
            }

            ShuttleInfoCardReadModel card = new ShuttleInfoCardReadModel();
            card.TitleKey = titleKey;
            card.SummaryKey = summaryKey;
            card.BodyKey = bodyKey;
            card.Number = number;
            cards.Add(card);
        }

        private string Tr(string key)
        {
            return string.IsNullOrEmpty(key) ? "-" : key.Translate().ToString();
        }

        private string ResolveStarterPresetModeLabel()
        {
            ShuttleStarterPresetGameComponent component =
                ShuttleStarterPresetGameComponent.CurrentComponent;
            if (component == null || !component.SelectionResolved)
            {
                return "-";
            }

            return this.Tr(
                component.SelectedMode == ShuttleStarterPresetMode.BasicFlightAndCargo
                    ? "CT_Shuttle_StarterPreset_Basic_Title"
                    : "CT_Shuttle_StarterPreset_FromScratch_Title");
        }

    }
}
