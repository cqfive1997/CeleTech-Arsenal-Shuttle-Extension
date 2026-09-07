using System;
using CeleTech.ShuttleExtension.ModularShuttle;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared.Dialogs;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Settings.Dialogs
{
    internal enum ShuttlePerformancePresetKind
    {
        PowerSaving,
        Standard,
        Responsive,
        Fancy,
        Custom
    }

    internal enum ShuttleSettingsDialogCategory
    {
        Appearance,
        Responsiveness,
        Advanced
    }

    internal sealed class Dialog_ShuttlePerformanceSettingsV3 : Window, IShuttleSettingsHubSection
    {
        private const float HeaderHeight = 64f;
        private const float PresetHeight = 46f;
        private const float BottomHeight = 44f;
        private const float LeftWidth = 170f;
        private const float RightWidth = 260f;

        private readonly Action onSettingsChanged;
        private ShuttleSettingsDialogCategory selectedCategory = ShuttleSettingsDialogCategory.Appearance;
        private Vector2 mainScroll = Vector2.zero;
        private Vector2 helpScroll = Vector2.zero;
        private bool manualCustomMode;

        internal Dialog_ShuttlePerformanceSettingsV3(Action onSettingsChanged)
        {
            this.onSettingsChanged = onSettingsChanged;
            this.doCloseX = true;
            this.absorbInputAroundWindow = true;
            this.forcePause = false;
            this.draggable = true;
            this.resizeable = false;
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(900f, 620f);
            }
        }

        public ShuttleSettingsHubCommitMode CommitMode
        {
            get { return ShuttleSettingsHubCommitMode.Immediate; }
        }

        public bool CanApply
        {
            get { return false; }
        }

        public bool HasPendingChanges
        {
            get { return false; }
        }

        public string ApplyDisabledReason
        {
            get { return null; }
        }

        public override void DoWindowContents(Rect inRect)
        {
            ShuttleV3DialogLayout.DrawPanelBackground(inRect);
            Rect headerRect = new Rect(inRect.x, inRect.y, inRect.width, HeaderHeight);
            Rect presetRect = new Rect(inRect.x, headerRect.yMax + ShuttleV3DialogStyle.Gap, inRect.width, PresetHeight);
            Rect bottomRect = new Rect(inRect.x, inRect.yMax - BottomHeight, inRect.width, BottomHeight);
            Rect bodyRect = new Rect(
                inRect.x,
                presetRect.yMax + ShuttleV3DialogStyle.Gap,
                inRect.width,
                bottomRect.y - presetRect.yMax - (ShuttleV3DialogStyle.Gap * 2f));

            this.DrawHeader(headerRect);
            this.DrawPresetStrip(presetRect);
            this.DrawBody(bodyRect);
            this.DrawBottomBar(bottomRect);
        }

        public void Draw(Rect rect)
        {
            Rect presetRect = new Rect(rect.x, rect.y, rect.width, PresetHeight);
            Rect tabsRect = new Rect(
                rect.x,
                presetRect.yMax + ShuttleV3DialogStyle.Gap,
                rect.width,
                40f);
            Rect panelsRect = new Rect(
                rect.x,
                tabsRect.yMax + ShuttleV3DialogStyle.Gap,
                rect.width,
                Mathf.Max(0f, rect.yMax - tabsRect.yMax - ShuttleV3DialogStyle.Gap));

            this.DrawPresetStrip(presetRect);
            this.DrawCategoryTabs(tabsRect);
            float helpWidth = Mathf.Min(RightWidth, Mathf.Max(220f, panelsRect.width * 0.31f));
            Rect helpRect = new Rect(
                panelsRect.xMax - helpWidth,
                panelsRect.y,
                helpWidth,
                panelsRect.height);
            Rect settingsRect = new Rect(
                panelsRect.x,
                panelsRect.y,
                Mathf.Max(0f, helpRect.x - panelsRect.x - ShuttleV3DialogStyle.Gap),
                panelsRect.height);
            this.DrawSettingsPanel(settingsRect);
            this.DrawHelpPanel(helpRect);
        }

        public void Reset()
        {
            CeleTechShuttleMod.Settings.ResetPerformanceToDefaults();
            this.manualCustomMode = false;
            this.ApplySettingsChanged();
        }

        public bool Apply()
        {
            return true;
        }

        public void OnHubClosed()
        {
        }

        private void DrawHeader(Rect rect)
        {
            Text.Font = GameFont.Medium;
            GUI.color = ShuttleV3DialogStyle.HeaderTitleTextColor;
            ShuttleV3DialogLayout.SafeLabel(
                new Rect(rect.x + 12f, rect.y + 6f, rect.width - 24f, 30f),
                "CT_Shuttle_PerformanceSettings_Title".Translate().ToString());
            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleV3DialogStyle.MutedTextColor;
            ShuttleV3DialogLayout.SafeLabel(
                new Rect(rect.x + 12f, rect.y + 36f, rect.width - 24f, 22f),
                "CT_Shuttle_PerformanceSettings_Subtitle".Translate().ToString());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private void DrawPresetStrip(Rect rect)
        {
            ShuttleV3DialogLayout.DrawCardBackground(rect, false, false, ShuttleV3DialogStyle.CardColor);
            CeleTechShuttleModSettings settings = CeleTechShuttleMod.Settings;
            ShuttlePerformancePresetKind activePreset = this.GetActivePreset(settings);
            float gap = ShuttleV3DialogStyle.SmallGap;
            float buttonWidth = (rect.width - 24f - (gap * 4f)) / 5f;
            float x = rect.x + 12f;
            this.DrawPresetButton(new Rect(x, rect.y + 8f, buttonWidth, rect.height - 16f), "CT_Shuttle_PerformanceSettings_Preset_PowerSaving".Translate().ToString(), activePreset == ShuttlePerformancePresetKind.PowerSaving, ShuttlePerformancePresetKind.PowerSaving, null);
            x += buttonWidth + gap;
            this.DrawPresetButton(new Rect(x, rect.y + 8f, buttonWidth, rect.height - 16f), "CT_Shuttle_PerformanceSettings_Preset_Standard".Translate().ToString(), activePreset == ShuttlePerformancePresetKind.Standard, ShuttlePerformancePresetKind.Standard, null);
            x += buttonWidth + gap;
            this.DrawPresetButton(new Rect(x, rect.y + 8f, buttonWidth, rect.height - 16f), "CT_Shuttle_PerformanceSettings_Preset_Responsive".Translate().ToString(), activePreset == ShuttlePerformancePresetKind.Responsive, ShuttlePerformancePresetKind.Responsive, null);
            x += buttonWidth + gap;
            this.DrawPresetButton(new Rect(x, rect.y + 8f, buttonWidth, rect.height - 16f), "CT_Shuttle_PerformanceSettings_Preset_Fancy".Translate().ToString(), activePreset == ShuttlePerformancePresetKind.Fancy, ShuttlePerformancePresetKind.Fancy, null);
            x += buttonWidth + gap;
            this.DrawPresetButton(new Rect(x, rect.y + 8f, buttonWidth, rect.height - 16f), "CT_Shuttle_PerformanceSettings_Preset_Custom".Translate().ToString(), activePreset == ShuttlePerformancePresetKind.Custom, ShuttlePerformancePresetKind.Custom, "CT_Shuttle_PerformanceSettings_CustomTooltip".Translate().ToString());
        }

        private void DrawPresetButton(Rect rect, string label, bool selected, ShuttlePerformancePresetKind preset, string tooltip)
        {
            if (!this.DrawSettingsButton(rect, label, selected, false, tooltip))
            {
                return;
            }

            CeleTechShuttleModSettings settings = CeleTechShuttleMod.Settings;
            if (preset == ShuttlePerformancePresetKind.Custom)
            {
                settings.RestoreLastCustomOrKeepCurrent();
                this.manualCustomMode = true;
                this.ApplySettingsChanged();
                return;
            }

            if (this.GetActivePreset(settings) == ShuttlePerformancePresetKind.Custom)
            {
                settings.CaptureCurrentAsLastCustom();
            }

            this.ApplyPreset(preset, settings);
            this.manualCustomMode = false;
            this.ApplySettingsChanged();
        }

        private void DrawBody(Rect rect)
        {
            float gap = ShuttleV3DialogStyle.Gap;
            Rect leftRect = new Rect(rect.x, rect.y, LeftWidth, rect.height);
            Rect rightRect = new Rect(rect.xMax - RightWidth, rect.y, RightWidth, rect.height);
            Rect centerRect = new Rect(leftRect.xMax + gap, rect.y, rect.width - LeftWidth - RightWidth - (gap * 2f), rect.height);
            this.DrawCategoryPanel(leftRect);
            this.DrawSettingsPanel(centerRect);
            this.DrawHelpPanel(rightRect);
        }

        private void DrawCategoryPanel(Rect rect)
        {
            ShuttleV3DialogLayout.DrawPanelBackground(rect);
            ShuttleV3DialogLayout.DrawSectionHeader(rect, "CT_Shuttle_Settings_Settings".Translate().ToString());
            Rect inner = new Rect(rect.x + 10f, rect.y + 44f, rect.width - 20f, rect.height - 54f);
            float y = inner.y;
            this.DrawCategoryButton(new Rect(inner.x, y, inner.width, 42f), ShuttleSettingsDialogCategory.Appearance, "CT_Shuttle_Settings_Category_Appearance".Translate().ToString());
            y += 50f;
            this.DrawCategoryButton(new Rect(inner.x, y, inner.width, 42f), ShuttleSettingsDialogCategory.Responsiveness, "CT_Shuttle_Settings_Category_Responsiveness".Translate().ToString());
            y += 50f;
            this.DrawCategoryButton(new Rect(inner.x, y, inner.width, 42f), ShuttleSettingsDialogCategory.Advanced, "CT_Shuttle_Settings_Category_Advanced".Translate().ToString());
        }

        private void DrawCategoryTabs(Rect rect)
        {
            ShuttleV3DialogLayout.DrawPanelBackground(rect);
            float gap = ShuttleV3DialogStyle.SmallGap;
            float buttonWidth = (rect.width - 24f - (gap * 2f)) / 3f;
            float x = rect.x + 12f;
            this.DrawCategoryButton(
                new Rect(x, rect.y + 5f, buttonWidth, rect.height - 10f),
                ShuttleSettingsDialogCategory.Appearance,
                "CT_Shuttle_Settings_Category_Appearance".Translate().ToString());
            x += buttonWidth + gap;
            this.DrawCategoryButton(
                new Rect(x, rect.y + 5f, buttonWidth, rect.height - 10f),
                ShuttleSettingsDialogCategory.Responsiveness,
                "CT_Shuttle_Settings_Category_Responsiveness".Translate().ToString());
            x += buttonWidth + gap;
            this.DrawCategoryButton(
                new Rect(x, rect.y + 5f, buttonWidth, rect.height - 10f),
                ShuttleSettingsDialogCategory.Advanced,
                "CT_Shuttle_Settings_Category_Advanced".Translate().ToString());
        }

        private void DrawCategoryButton(Rect rect, ShuttleSettingsDialogCategory category, string label)
        {
            if (this.DrawSettingsButton(rect, label, this.selectedCategory == category, false, null))
            {
                this.selectedCategory = category;
                this.mainScroll = Vector2.zero;
            }
        }

        private void DrawSettingsPanel(Rect rect)
        {
            ShuttleV3DialogLayout.DrawPanelBackground(rect);
            ShuttleV3DialogLayout.DrawSectionHeader(rect, this.GetSelectedCategoryLabel());
            Rect inner = new Rect(rect.x + 10f, rect.y + 44f, rect.width - 20f, rect.height - 54f);
            Rect viewRect = new Rect(0f, 0f, Mathf.Max(0f, inner.width - 16f), Mathf.Max(inner.height, this.GetMainViewHeight()));
            Widgets.BeginScrollView(inner, ref this.mainScroll, viewRect);
            float y = 0f;
            CeleTechShuttleModSettings settings = CeleTechShuttleMod.Settings;
            ShuttleSettingsSanitizer.Sanitize(settings);
            if (this.selectedCategory == ShuttleSettingsDialogCategory.Responsiveness)
            {
                this.DrawResponsivenessSettings(viewRect, settings, ref y);
            }
            else if (this.selectedCategory == ShuttleSettingsDialogCategory.Advanced)
            {
                this.DrawAdvancedSettings(viewRect, settings, ref y);
            }
            else
            {
                this.DrawAppearanceSettings(viewRect, settings, ref y);
            }

            Widgets.EndScrollView();
        }

        private void DrawHelpPanel(Rect rect)
        {
            ShuttleV3DialogLayout.DrawPanelBackground(rect);
            ShuttleV3DialogLayout.DrawSectionHeader(rect, "CT_Shuttle_Settings_CurrentEffectiveSettings".Translate().ToString());
            Rect inner = new Rect(rect.x + 10f, rect.y + 44f, rect.width - 20f, rect.height - 54f);
            Rect viewRect = new Rect(0f, 0f, Mathf.Max(0f, inner.width - 16f), Mathf.Max(inner.height, 320f));
            Widgets.BeginScrollView(inner, ref this.helpScroll, viewRect);
            float y = 0f;
            this.DrawHelpText(new Rect(0f, y, viewRect.width, 98f), this.GetSelectedHelpText());
            y += 106f;
            this.DrawEffectiveSettingsSummary(new Rect(0f, y, viewRect.width, 238f));
            Widgets.EndScrollView();
        }

        private void DrawBottomBar(Rect rect)
        {
            Rect resetRect = new Rect(rect.x + 8f, rect.y + 6f, 180f, 32f);
            Rect closeRect = new Rect(rect.xMax - 128f, rect.y + 6f, 120f, 32f);
            if (this.DrawSettingsButton(resetRect, "CT_Shuttle_Settings_ResetUIDefaults".Translate().ToString(), false, true, "CT_Shuttle_Settings_ResetConfirm".Translate().ToString()))
            {
                this.Reset();
            }

            if (this.DrawSettingsButton(closeRect, "CT_Shuttle_Settings_Close".Translate().ToString(), false, false, null))
            {
                this.Close(false);
            }
        }

        private void DrawAppearanceSettings(Rect viewRect, CeleTechShuttleModSettings settings, ref float y)
        {
            this.DrawEnumCycler(new Rect(0f, y, viewRect.width, 48f), "CT_Shuttle_Settings_VisualQuality".Translate().ToString(), CeleTechShuttleMod.GetVisualQualityLabel(settings.VisualQuality), delegate
            {
                settings.VisualQuality = CeleTechShuttleMod.NextVisualQuality(settings.VisualQuality);
            });
            y += 56f;
            this.DrawEnumCycler(new Rect(0f, y, viewRect.width, 48f), "CT_Shuttle_Settings_HeaderMeterStyle".Translate().ToString(), CeleTechShuttleMod.GetHeaderMeterStyleLabel(settings.HeaderMeterStyle), delegate
            {
                settings.HeaderMeterStyle = CeleTechShuttleMod.NextEnum(settings.HeaderMeterStyle);
            });
            y += 56f;
            this.DrawCheckboxRow(new Rect(0f, y, viewRect.width, 38f), "CT_Shuttle_Settings_TransparentButtons".Translate().ToString(), ref settings.TransparentButtons, null);
            y += 44f;
            this.DrawCheckboxRow(new Rect(0f, y, viewRect.width, 38f), "CT_Shuttle_Settings_HoverGlow".Translate().ToString(), ref settings.HoverGlow, null);
            y += 44f;
            this.DrawCheckboxRow(new Rect(0f, y, viewRect.width, 38f), "CT_Shuttle_Settings_WindowAnimations".Translate().ToString(), ref settings.WindowAnimations, null);
            y += 44f;
            this.DrawCheckboxRow(new Rect(0f, y, viewRect.width, 38f), "CT_Shuttle_Settings_SystemUpdateAnimations".Translate().ToString(), ref settings.SystemUpdateAnimations, null);
            y += 44f;
            this.DrawCheckboxRow(new Rect(0f, y, viewRect.width, 38f), "CT_Shuttle_Settings_CargoIcons".Translate().ToString(), ref settings.CargoIcons, null);
        }

        private void DrawResponsivenessSettings(Rect viewRect, CeleTechShuttleModSettings settings, ref float y)
        {
            ShuttleEffectiveSettings effective = CeleTechShuttleMod.EffectiveSettings;
            this.DrawEnumCycler(new Rect(0f, y, viewRect.width, 48f), "CT_Shuttle_Settings_UIRefreshPreset".Translate().ToString(), CeleTechShuttleMod.GetRefreshPresetLabel(settings.UIRefreshPreset), delegate
            {
                settings.UIRefreshPreset = CeleTechShuttleMod.NextEnum(settings.UIRefreshPreset);
            });
            y += 56f;
            this.DrawDescription(new Rect(0f, y, viewRect.width, 46f), "CT_Shuttle_Settings_CurrentEffectiveRefresh".Translate(effective.ControlRefreshTicks, effective.HeavyRefreshTicks).ToString());
            y += 54f;
            this.DrawCheckboxRow(new Rect(0f, y, viewRect.width, 38f), "CT_Shuttle_Settings_AdvancedOverrides".Translate().ToString(), ref settings.AdvancedOverridesEnabled, null);
            y += 46f;
            if (settings.AdvancedOverridesEnabled)
            {
                this.DrawIntStepperRow(new Rect(0f, y, viewRect.width, 42f), "CT_Shuttle_Settings_ControlRefreshTicks".Translate().ToString(), ref settings.ControlRefreshTicksOverride, 1, 120, 1);
                y += 50f;
                this.DrawIntStepperRow(new Rect(0f, y, viewRect.width, 42f), "CT_Shuttle_Settings_HeavyRefreshTicks".Translate().ToString(), ref settings.HeavyRefreshTicksOverride, 5, 300, 5);
            }
        }

        private void DrawAdvancedSettings(Rect viewRect, CeleTechShuttleModSettings settings, ref float y)
        {
            this.DrawCheckboxRow(new Rect(0f, y, viewRect.width, 38f), "CT_Shuttle_Settings_ForceCompactCargoList".Translate().ToString(), ref settings.ForceCompactCargoList, null);
            y += 44f;
            this.DrawCheckboxRow(new Rect(0f, y, viewRect.width, 38f), "CT_Shuttle_Settings_EnableUIProfiler".Translate().ToString(), ref settings.EnableUIProfiler, "CT_Shuttle_Settings_EnableUIProfilerTooltip".Translate().ToString());
            y += 44f;
            if (Prefs.DevMode)
            {
                this.DrawCheckboxRow(
                    new Rect(0f, y, viewRect.width, 38f),
                    this.GetDevTimeCostLoggingLabel(settings.DevTimeCostLoggingEnabled),
                    ref settings.DevTimeCostLoggingEnabled,
                    "CT_Shuttle_Settings_DevTimeCostLoggingTooltip".Translate().ToString());
                y += 52f;
                this.DrawCheckboxRow(
                    new Rect(0f, y, viewRect.width, 38f),
                    "CT_Shuttle_Settings_PerformanceSummaryLogs".Translate().ToString(),
                    ref settings.EnablePerformanceSummaryLogs,
                    "CT_Shuttle_Settings_PerformanceSummaryLogsTooltip".Translate().ToString());
                y += 52f;
                if (settings.EnablePerformanceSummaryLogs)
                {
                    this.DrawCheckboxRow(
                        new Rect(0f, y, viewRect.width, 38f),
                        "CT_Shuttle_Settings_DetailedPerformanceBreakdownLogs".Translate().ToString(),
                        ref settings.EnableDetailedPerformanceBreakdownLogs,
                        "CT_Shuttle_Settings_DetailedPerformanceBreakdownLogsTooltip".Translate().ToString());
                    y += 52f;
                }
                else
                {
                    settings.EnableDetailedPerformanceBreakdownLogs = false;
                }
            }
            else
            {
                y += 8f;
            }

            this.DrawEffectiveSettingsSummary(new Rect(0f, y, viewRect.width, 238f));
            y += 250f;
            if (this.DrawSettingsButton(new Rect(0f, y, 190f, 34f), "CT_Shuttle_Settings_ResetDefaults".Translate().ToString(), false, true, "CT_Shuttle_Settings_ResetConfirm".Translate().ToString()))
            {
                settings.ResetPerformanceToDefaults();
                this.ApplySettingsChanged();
            }
        }

        private void DrawEnumCycler(Rect rect, string label, string value, Action onCycle)
        {
            ShuttleV3DialogLayout.DrawCardBackground(rect, false, false, ShuttleV3DialogStyle.CardColor);
            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleV3DialogStyle.MutedTextColor;
            ShuttleV3DialogLayout.SafeLabel(new Rect(rect.x + 10f, rect.y + 6f, rect.width * 0.48f, 18f), label);
            GUI.color = Color.white;
            if (this.DrawSettingsButton(new Rect(rect.x + rect.width * 0.52f, rect.y + 8f, rect.width * 0.44f, 30f), value, false, false, null))
            {
                if (onCycle != null)
                {
                    onCycle();
                }

                this.MarkManualSettingsChanged();
                this.ApplySettingsChanged();
            }

            Text.Font = GameFont.Small;
            GUI.color = Color.white;
        }

        private void DrawCheckboxRow(Rect rect, string label, ref bool value, string tooltip)
        {
            bool oldValue = value;
            ShuttleV3DialogLayout.DrawCardBackground(rect, false, false, ShuttleV3DialogStyle.SettingsInfoRowColor);
            Rect checkboxRect = new Rect(rect.x + 10f, rect.y + 10f, 18f, 18f);
            Widgets.Checkbox(checkboxRect.position, ref value);
            Text.Font = GameFont.Tiny;
            GUI.color = Color.white;
            ShuttleV3DialogLayout.SafeLabel(new Rect(rect.x + 36f, rect.y + 8f, rect.width - 46f, 20f), label);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            this.AddTooltip(rect, tooltip);
            if (oldValue != value)
            {
                this.MarkManualSettingsChanged();
                this.ApplySettingsChanged();
            }
        }

        private void DrawIntStepperRow(Rect rect, string label, ref int value, int min, int max, int step)
        {
            ShuttleV3DialogLayout.DrawCardBackground(rect, false, false, ShuttleV3DialogStyle.SettingsInfoRowColor);
            Text.Font = GameFont.Tiny;
            GUI.color = Color.white;
            ShuttleV3DialogLayout.SafeLabel(new Rect(rect.x + 10f, rect.y + 11f, rect.width - 150f, 18f), label);
            int oldValue = value;
            Rect minusRect = new Rect(rect.xMax - 132f, rect.y + 7f, 34f, 28f);
            Rect valueRect = new Rect(rect.xMax - 94f, rect.y + 7f, 48f, 28f);
            Rect plusRect = new Rect(rect.xMax - 42f, rect.y + 7f, 34f, 28f);
            TextAnchor oldAnchor = Text.Anchor;
            try
            {
                if (this.DrawSettingsButton(minusRect, "-", false, false, null))
                {
                    value -= Mathf.Max(1, step);
                }

                ShuttleV3DialogLayout.DrawCardBackground(valueRect, false, false, ShuttleV3DialogStyle.CardColor);
                Text.Anchor = TextAnchor.MiddleCenter;
                ShuttleV3DialogLayout.SafeLabel(valueRect, Mathf.Clamp(value, min, max).ToString());
                if (this.DrawSettingsButton(plusRect, "+", false, false, null))
                {
                    value += Mathf.Max(1, step);
                }
            }
            finally
            {
                Text.Anchor = oldAnchor;
            }

            value = Mathf.Clamp(value, min, max);
            if (oldValue != value)
            {
                this.MarkManualSettingsChanged();
                this.ApplySettingsChanged();
            }

            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private void DrawDescription(Rect rect, string text)
        {
            ShuttleV3DialogLayout.DrawCardBackground(rect, false, false, ShuttleV3DialogStyle.SettingsInfoRowColor);
            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleV3DialogStyle.MutedTextColor;
            ShuttleV3DialogLayout.SafeLabel(new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, rect.height - 16f), text);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private void DrawHelpText(Rect rect, string text)
        {
            ShuttleV3DialogLayout.DrawCardBackground(rect, false, false, ShuttleV3DialogStyle.SettingsInfoRowColor);
            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleV3DialogStyle.MutedTextColor;
            bool oldWordWrap = Text.WordWrap;
            Text.WordWrap = true;
            ShuttleV3DialogLayout.SafeLabel(new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, rect.height - 16f), text);
            Text.WordWrap = oldWordWrap;
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private void DrawEffectiveSettingsSummary(Rect rect)
        {
            ShuttleEffectiveSettings effective = CeleTechShuttleMod.EffectiveSettings;
            ShuttleV3DialogLayout.DrawCardBackground(rect, false, false, ShuttleV3DialogStyle.SettingsInfoRowColor);
            Text.Font = GameFont.Tiny;
            GUI.color = Color.white;
            ShuttleV3DialogLayout.SafeLabel(new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, 18f), "CT_Shuttle_Settings_CurrentEffectiveSettings".Translate().ToString());
            GUI.color = ShuttleV3DialogStyle.MutedTextColor;
            ShuttleV3DialogLayout.SafeLabel(new Rect(rect.x + 10f, rect.y + 34f, rect.width - 20f, 18f), "CT_Shuttle_Settings_Summary_Visual".Translate().ToString() + ": " + CeleTechShuttleMod.GetVisualQualityLabel(effective.VisualQuality));
            ShuttleV3DialogLayout.SafeLabel(new Rect(rect.x + 10f, rect.y + 58f, rect.width - 20f, 18f), "CT_Shuttle_Settings_HeaderMeterStyle".Translate().ToString() + ": " + CeleTechShuttleMod.GetHeaderMeterStyleLabel(effective.HeaderMeterStyle));
            ShuttleV3DialogLayout.SafeLabel(new Rect(rect.x + 10f, rect.y + 82f, rect.width - 20f, 18f), "CT_Shuttle_Settings_Summary_Refresh".Translate().ToString() + ": " + effective.ControlRefreshTicks + "/" + effective.HeavyRefreshTicks);
            ShuttleV3DialogLayout.SafeLabel(new Rect(rect.x + 10f, rect.y + 106f, rect.width - 20f, 18f), "CT_Shuttle_Settings_EnableUIProfiler".Translate().ToString() + ": " + (effective.EnableUIProfiler ? "CT_Shuttle_Common_Yes".Translate().ToString() : "CT_Shuttle_Common_No".Translate().ToString()));
            ShuttleV3DialogLayout.SafeLabel(new Rect(rect.x + 10f, rect.y + 130f, rect.width - 20f, 18f), "CT_Shuttle_Settings_ForceCompactCargoList".Translate().ToString() + ": " + (effective.ForceCompactCargoList ? "CT_Shuttle_Common_Yes".Translate().ToString() : "CT_Shuttle_Common_No".Translate().ToString()));
            if (Prefs.DevMode)
            {
                ShuttleV3DialogLayout.SafeLabel(new Rect(rect.x + 10f, rect.y + 154f, rect.width - 20f, 18f), "CT_Shuttle_Settings_DevTimeCostLogging".Translate().ToString() + ": " + (effective.DevTimeCostLoggingEnabled ? "CT_Shuttle_Common_Yes".Translate().ToString() : "CT_Shuttle_Common_No".Translate().ToString()));
                ShuttleV3DialogLayout.SafeLabel(new Rect(rect.x + 10f, rect.y + 178f, rect.width - 20f, 18f), "CT_Shuttle_Settings_PerformanceSummaryLogs".Translate().ToString() + ": " + (effective.PerformanceSummaryLogsEnabled ? "CT_Shuttle_Common_Yes".Translate().ToString() : "CT_Shuttle_Common_No".Translate().ToString()));
                ShuttleV3DialogLayout.SafeLabel(new Rect(rect.x + 10f, rect.y + 202f, rect.width - 20f, 18f), "CT_Shuttle_Settings_DetailedPerformanceBreakdownLogs".Translate().ToString() + ": " + (effective.DetailedPerformanceBreakdownLogsEnabled ? "CT_Shuttle_Common_Yes".Translate().ToString() : "CT_Shuttle_Common_No".Translate().ToString()));
            }

            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private bool DrawSettingsButton(Rect rect, string label, bool selected, bool danger, string tooltip)
        {
            ShuttleEffectiveSettings effective = CeleTechShuttleMod.EffectiveSettings;
            Color color = selected
                ? ShuttleV3DialogStyle.SelectedColor
                : danger ? ShuttleV3DialogStyle.DangerButtonBgColor : ShuttleV3DialogStyle.ButtonBgColor;
            if (effective.UseTransparentButtons)
            {
                color = ShuttleV3DialogStyle.WithAlpha(color, selected ? 0.62f : 0.38f);
            }

            return ShuttleV3DialogLayout.DrawTintedButton(
                rect,
                label,
                true,
                color,
                selected ? ShuttleV3DialogStyle.BlueStatusColor : ShuttleV3DialogStyle.ButtonBorderColor,
                danger ? ShuttleV3DialogStyle.DangerButtonTextColor : ShuttleV3DialogStyle.ButtonTextColor,
                tooltip);
        }

        private float GetMainViewHeight()
        {
            if (this.selectedCategory == ShuttleSettingsDialogCategory.Responsiveness)
            {
                return 330f;
            }

            if (this.selectedCategory == ShuttleSettingsDialogCategory.Advanced)
            {
                return 620f;
            }

            return 360f;
        }

        private string GetSelectedCategoryLabel()
        {
            if (this.selectedCategory == ShuttleSettingsDialogCategory.Responsiveness)
            {
                return "CT_Shuttle_Settings_Category_Responsiveness".Translate().ToString();
            }

            if (this.selectedCategory == ShuttleSettingsDialogCategory.Advanced)
            {
                return "CT_Shuttle_Settings_Category_Advanced".Translate().ToString();
            }

            return "CT_Shuttle_Settings_Category_Appearance".Translate().ToString();
        }

        private string GetDevTimeCostLoggingLabel(bool enabled)
        {
            return enabled
                ? "CT_Shuttle_Settings_DevTimeCostLoggingOn".Translate().ToString()
                : "CT_Shuttle_Settings_DevTimeCostLoggingOff".Translate().ToString();
        }

        private string GetSelectedHelpText()
        {
            if (this.selectedCategory == ShuttleSettingsDialogCategory.Responsiveness)
            {
                return "CT_Shuttle_PerformanceSettings_Help_Responsiveness".Translate().ToString();
            }

            if (this.selectedCategory == ShuttleSettingsDialogCategory.Advanced)
            {
                return "CT_Shuttle_PerformanceSettings_Help_Advanced".Translate().ToString();
            }

            return "CT_Shuttle_PerformanceSettings_Help_Appearance".Translate().ToString();
        }

        private void ApplySettingsChanged()
        {
            ShuttleSettingsSanitizer.Sanitize(CeleTechShuttleMod.Settings);
            CeleTechShuttleMod.SaveSettingsSafe();
            if (this.onSettingsChanged != null)
            {
                this.onSettingsChanged();
            }
        }

        private void MarkManualSettingsChanged()
        {
            CeleTechShuttleMod.Settings.CaptureCurrentAsLastCustom();
            this.manualCustomMode = true;
        }

        private void ApplyPreset(ShuttlePerformancePresetKind preset, CeleTechShuttleModSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            if (preset == ShuttlePerformancePresetKind.PowerSaving)
            {
                settings.VisualQuality = ShuttleVisualQuality.Performance;
                settings.HeaderMeterStyle = ShuttleHeaderMeterStyle.Linear;
                settings.TransparentButtons = false;
                settings.HoverGlow = false;
                settings.WindowAnimations = false;
                settings.SystemUpdateAnimations = false;
                settings.CargoIcons = false;
                settings.UIRefreshPreset = ShuttleUIRefreshPreset.PowerSaver;
                settings.AdvancedOverridesEnabled = false;
                return;
            }

            if (preset == ShuttlePerformancePresetKind.Responsive)
            {
                settings.VisualQuality = ShuttleVisualQuality.Standard;
                settings.HeaderMeterStyle = ShuttleHeaderMeterStyle.HudFrame;
                settings.TransparentButtons = false;
                settings.HoverGlow = true;
                settings.WindowAnimations = true;
                settings.SystemUpdateAnimations = true;
                settings.CargoIcons = true;
                settings.UIRefreshPreset = ShuttleUIRefreshPreset.Responsive;
                settings.AdvancedOverridesEnabled = false;
                return;
            }

            if (preset == ShuttlePerformancePresetKind.Fancy)
            {
                settings.VisualQuality = ShuttleVisualQuality.Fancy;
                settings.HeaderMeterStyle = ShuttleHeaderMeterStyle.HudFrame;
                settings.TransparentButtons = true;
                settings.HoverGlow = true;
                settings.WindowAnimations = true;
                settings.SystemUpdateAnimations = true;
                settings.CargoIcons = true;
                settings.UIRefreshPreset = ShuttleUIRefreshPreset.Responsive;
                settings.AdvancedOverridesEnabled = false;
                return;
            }

            if (preset == ShuttlePerformancePresetKind.Custom)
            {
                settings.RestoreLastCustomOrKeepCurrent();
                return;
            }

            settings.VisualQuality = ShuttleVisualQuality.Standard;
            settings.HeaderMeterStyle = ShuttleHeaderMeterStyle.HudFrame;
            settings.TransparentButtons = false;
            settings.HoverGlow = true;
            settings.WindowAnimations = true;
            settings.SystemUpdateAnimations = true;
            settings.CargoIcons = true;
            settings.UIRefreshPreset = ShuttleUIRefreshPreset.Balanced;
            settings.AdvancedOverridesEnabled = false;
        }

        private ShuttlePerformancePresetKind GetActivePreset(CeleTechShuttleModSettings settings)
        {
            if (this.manualCustomMode)
            {
                return ShuttlePerformancePresetKind.Custom;
            }

            return this.ResolveCurrentPreset(settings);
        }

        private ShuttlePerformancePresetKind ResolveCurrentPreset(CeleTechShuttleModSettings settings)
        {
            if (settings == null)
            {
                return ShuttlePerformancePresetKind.Standard;
            }

            if (this.IsPowerSavingPreset(settings))
            {
                return ShuttlePerformancePresetKind.PowerSaving;
            }

            if (this.IsStandardPreset(settings))
            {
                return ShuttlePerformancePresetKind.Standard;
            }

            if (this.IsResponsivePreset(settings))
            {
                return ShuttlePerformancePresetKind.Responsive;
            }

            if (this.IsFancyPreset(settings))
            {
                return ShuttlePerformancePresetKind.Fancy;
            }

            return ShuttlePerformancePresetKind.Custom;
        }

        private bool IsPowerSavingPreset(CeleTechShuttleModSettings settings)
        {
            return settings != null &&
                !settings.AdvancedOverridesEnabled &&
                settings.VisualQuality == ShuttleVisualQuality.Performance &&
                settings.HeaderMeterStyle == ShuttleHeaderMeterStyle.Linear &&
                !settings.TransparentButtons &&
                !settings.HoverGlow &&
                !settings.WindowAnimations &&
                !settings.SystemUpdateAnimations &&
                !settings.CargoIcons &&
                settings.UIRefreshPreset == ShuttleUIRefreshPreset.PowerSaver;
        }

        private bool IsStandardPreset(CeleTechShuttleModSettings settings)
        {
            return settings != null &&
                !settings.AdvancedOverridesEnabled &&
                settings.VisualQuality == ShuttleVisualQuality.Standard &&
                settings.HeaderMeterStyle == ShuttleHeaderMeterStyle.HudFrame &&
                !settings.TransparentButtons &&
                settings.HoverGlow &&
                settings.WindowAnimations &&
                settings.SystemUpdateAnimations &&
                settings.CargoIcons &&
                settings.UIRefreshPreset == ShuttleUIRefreshPreset.Balanced;
        }

        private bool IsResponsivePreset(CeleTechShuttleModSettings settings)
        {
            return settings != null &&
                !settings.AdvancedOverridesEnabled &&
                settings.VisualQuality == ShuttleVisualQuality.Standard &&
                settings.HeaderMeterStyle == ShuttleHeaderMeterStyle.HudFrame &&
                !settings.TransparentButtons &&
                settings.HoverGlow &&
                settings.WindowAnimations &&
                settings.SystemUpdateAnimations &&
                settings.CargoIcons &&
                settings.UIRefreshPreset == ShuttleUIRefreshPreset.Responsive;
        }

        private bool IsFancyPreset(CeleTechShuttleModSettings settings)
        {
            return settings != null &&
                !settings.AdvancedOverridesEnabled &&
                settings.VisualQuality == ShuttleVisualQuality.Fancy &&
                settings.HeaderMeterStyle == ShuttleHeaderMeterStyle.HudFrame &&
                settings.TransparentButtons &&
                settings.HoverGlow &&
                settings.WindowAnimations &&
                settings.SystemUpdateAnimations &&
                settings.CargoIcons &&
                settings.UIRefreshPreset == ShuttleUIRefreshPreset.Responsive;
        }

        private void AddTooltip(Rect rect, string tooltip)
        {
            if (!string.IsNullOrEmpty(tooltip))
            {
                TooltipHandler.TipRegion(rect, tooltip);
            }
        }

    }
}
