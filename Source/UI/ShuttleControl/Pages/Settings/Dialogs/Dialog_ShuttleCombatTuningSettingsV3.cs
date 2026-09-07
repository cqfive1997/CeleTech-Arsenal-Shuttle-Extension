using System;
using CeleTech.ShuttleExtension.ModularShuttle;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared.Dialogs;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Settings.Dialogs
{
    internal enum ShuttleCombatTuningCategory
    {
        Weapon,
        Shield,
        Armor,
        FireControl,
        Advanced
    }

    internal sealed class Dialog_ShuttleCombatTuningSettingsV3 : Window, IShuttleSettingsHubSection
    {
        private readonly Action onSettingsChanged;
        private ShuttleCombatTuningCategory selectedCategory = ShuttleCombatTuningCategory.Weapon;
        private Vector2 mainScroll = Vector2.zero;
        private Vector2 helpScroll = Vector2.zero;

        internal Dialog_ShuttleCombatTuningSettingsV3(Action onSettingsChanged)
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
                return new Vector2(
                    ShuttleV3DialogMetrics.CombatWindowWidth,
                    ShuttleV3DialogMetrics.CombatWindowHeight);
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
            Rect headerRect = new Rect(inRect.x, inRect.y, inRect.width, ShuttleV3DialogMetrics.CombatHeaderHeight);
            Rect bottomRect = new Rect(
                inRect.x,
                inRect.yMax - ShuttleV3DialogMetrics.CombatBottomHeight,
                inRect.width,
                ShuttleV3DialogMetrics.CombatBottomHeight);
            Rect bodyRect = new Rect(
                inRect.x,
                headerRect.yMax + ShuttleV3DialogStyle.Gap,
                inRect.width,
                bottomRect.y - headerRect.yMax - (ShuttleV3DialogStyle.Gap * 2f));

            this.DrawHeader(headerRect);
            this.DrawBody(bodyRect);
            this.DrawBottomBar(bottomRect);
        }

        public void Draw(Rect rect)
        {
            ShuttleCombatTuningSettings tuning = this.GetTuningSettings();
            Rect toggleRect = new Rect(rect.x, rect.y, rect.width, 38f);
            this.DrawCheckboxRow(
                toggleRect,
                "CT_Shuttle_CombatTuning_EnableAdvanced".Translate().ToString(),
                ref tuning.AdvancedCombatTuningEnabled,
                true,
                "CT_Shuttle_CombatTuning_EnableAdvancedTooltip".Translate().ToString());

            Rect tabsRect = new Rect(
                rect.x,
                toggleRect.yMax + ShuttleV3DialogStyle.Gap,
                rect.width,
                40f);
            this.DrawCategoryTabs(tabsRect);
            Rect panelsRect = new Rect(
                rect.x,
                tabsRect.yMax + ShuttleV3DialogStyle.Gap,
                rect.width,
                Mathf.Max(0f, rect.yMax - tabsRect.yMax - ShuttleV3DialogStyle.Gap));
            float helpWidth = Mathf.Min(
                ShuttleV3DialogMetrics.CombatRightWidth,
                Mathf.Max(220f, panelsRect.width * 0.31f));
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
            this.GetTuningSettings().ResetToDefaults();
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
                "CT_Shuttle_CombatTuning_Title".Translate().ToString());
            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleV3DialogStyle.MutedTextColor;
            ShuttleV3DialogLayout.SafeLabel(
                new Rect(rect.x + 12f, rect.y + 38f, rect.width - 24f, 24f),
                "CT_Shuttle_CombatTuning_Subtitle".Translate().ToString());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private void DrawBody(Rect rect)
        {
            float gap = ShuttleV3DialogStyle.Gap;
            Rect leftRect = new Rect(rect.x, rect.y, ShuttleV3DialogMetrics.CombatLeftWidth, rect.height);
            Rect rightRect = new Rect(
                rect.xMax - ShuttleV3DialogMetrics.CombatRightWidth,
                rect.y,
                ShuttleV3DialogMetrics.CombatRightWidth,
                rect.height);
            Rect centerRect = new Rect(
                leftRect.xMax + gap,
                rect.y,
                rect.width - ShuttleV3DialogMetrics.CombatLeftWidth - ShuttleV3DialogMetrics.CombatRightWidth - (gap * 2f),
                rect.height);
            this.DrawCategoryPanel(leftRect);
            this.DrawSettingsPanel(centerRect);
            this.DrawHelpPanel(rightRect);
        }

        private void DrawCategoryPanel(Rect rect)
        {
            ShuttleV3DialogLayout.DrawPanelBackground(rect);
            ShuttleV3DialogLayout.DrawSectionHeader(rect, "CT_Shuttle_Settings_Settings".Translate().ToString());
            Rect inner = new Rect(
                rect.x + ShuttleV3DialogMetrics.LargeGap,
                rect.y + ShuttleV3DialogMetrics.CombatSectionTop,
                rect.width - (ShuttleV3DialogMetrics.LargeGap * 2f),
                rect.height - ShuttleV3DialogMetrics.CombatSectionBottomPadding);
            float y = inner.y;
            for (int i = 0; i < CombatTuningSettingDefinitions.Categories.Length; i++)
            {
                CombatTuningCategoryDefinition category =
                    CombatTuningSettingDefinitions.Categories[i];
                this.DrawCategoryButton(
                    new Rect(inner.x, y, inner.width, ShuttleV3DialogMetrics.CombatCategoryButtonHeight),
                    category.Category,
                    this.Tr(category.LabelKey));
                y += ShuttleV3DialogMetrics.CombatCategoryButtonPitch;
            }
        }

        private void DrawCategoryTabs(Rect rect)
        {
            ShuttleV3DialogLayout.DrawPanelBackground(rect);
            float gap = ShuttleV3DialogStyle.SmallGap;
            int count = CombatTuningSettingDefinitions.Categories.Length;
            float buttonWidth = count > 0
                ? (rect.width - 24f - (gap * (count - 1))) / count
                : 0f;
            float x = rect.x + 12f;
            for (int i = 0; i < count; i++)
            {
                CombatTuningCategoryDefinition category =
                    CombatTuningSettingDefinitions.Categories[i];
                this.DrawCategoryButton(
                    new Rect(x, rect.y + 5f, buttonWidth, rect.height - 10f),
                    category.Category,
                    this.Tr(category.LabelKey));
                x += buttonWidth + gap;
            }
        }

        private void DrawCategoryButton(Rect rect, ShuttleCombatTuningCategory category, string label)
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
            Rect inner = new Rect(
                rect.x + ShuttleV3DialogMetrics.LargeGap,
                rect.y + ShuttleV3DialogMetrics.CombatSectionTop,
                rect.width - (ShuttleV3DialogMetrics.LargeGap * 2f),
                rect.height - ShuttleV3DialogMetrics.CombatSectionBottomPadding);
            Rect viewRect = new Rect(
                0f,
                0f,
                Mathf.Max(0f, inner.width - ShuttleV3DialogMetrics.CombatScrollBarReserve),
                Mathf.Max(inner.height, this.GetMainViewHeight()));
            Widgets.BeginScrollView(inner, ref this.mainScroll, viewRect);
            float y = 0f;
            ShuttleCombatTuningSettings tuning = this.GetTuningSettings();
            bool enabled = tuning.AdvancedCombatTuningEnabled;
            if (!enabled)
            {
                this.DrawDescription(
                    new Rect(0f, y, viewRect.width, ShuttleV3DialogMetrics.CombatDescriptionShortHeight),
                    "CT_Shuttle_CombatTuning_DefaultBalance".Translate().ToString());
                y += ShuttleV3DialogMetrics.CombatDescriptionShortHeight + ShuttleV3DialogMetrics.StandardGap + 2f;
            }

            if (this.selectedCategory == ShuttleCombatTuningCategory.Advanced)
            {
                this.DrawAdvancedSettings(viewRect, tuning, ref y);
            }
            else
            {
                this.DrawSettingRows(viewRect, tuning, enabled, ref y, this.selectedCategory);
            }

            Widgets.EndScrollView();
        }

        private void DrawHelpPanel(Rect rect)
        {
            ShuttleV3DialogLayout.DrawPanelBackground(rect);
            ShuttleV3DialogLayout.DrawSectionHeader(rect, "CT_Shuttle_CombatTuning_CurrentEffectiveSummary".Translate().ToString());
            Rect inner = new Rect(
                rect.x + ShuttleV3DialogMetrics.LargeGap,
                rect.y + ShuttleV3DialogMetrics.CombatSectionTop,
                rect.width - (ShuttleV3DialogMetrics.LargeGap * 2f),
                rect.height - ShuttleV3DialogMetrics.CombatSectionBottomPadding);
            Rect viewRect = new Rect(
                0f,
                0f,
                Mathf.Max(0f, inner.width - ShuttleV3DialogMetrics.CombatScrollBarReserve),
                Mathf.Max(inner.height, ShuttleV3DialogMetrics.CombatHelpViewHeight));
            Widgets.BeginScrollView(inner, ref this.helpScroll, viewRect);
            float y = 0f;
            this.DrawHelpText(
                new Rect(0f, y, viewRect.width, ShuttleV3DialogMetrics.CombatHelpTextHeight),
                this.GetSelectedHelpText());
            y += ShuttleV3DialogMetrics.CombatHelpTextPitch;
            this.DrawEffectiveCombatSummary(new Rect(0f, y, viewRect.width, ShuttleV3DialogMetrics.CombatSummaryHeight));
            Widgets.EndScrollView();
        }

        private void DrawBottomBar(Rect rect)
        {
            ShuttleCombatTuningSettings tuning = this.GetTuningSettings();
            Rect toggleRect = new Rect(rect.x + 8f, rect.y + 7f, 310f, 34f);
            Rect resetRect = new Rect(toggleRect.xMax + 8f, rect.y + 7f, 170f, 34f);
            Rect closeRect = new Rect(rect.xMax - 128f, rect.y + 7f, 120f, 34f);
            this.DrawCheckboxRow(toggleRect, "CT_Shuttle_CombatTuning_EnableAdvanced".Translate().ToString(), ref tuning.AdvancedCombatTuningEnabled, true, "CT_Shuttle_CombatTuning_EnableAdvancedTooltip".Translate().ToString());
            if (this.DrawSettingsButton(resetRect, "CT_Shuttle_CombatTuning_ResetDefaults".Translate().ToString(), false, true, "CT_Shuttle_Settings_ResetConfirm".Translate().ToString()))
            {
                this.Reset();
            }

            if (this.DrawSettingsButton(closeRect, "CT_Shuttle_Settings_Close".Translate().ToString(), false, false, null))
            {
                this.Close(false);
            }
        }

        private void DrawSettingRows(
            Rect viewRect,
            ShuttleCombatTuningSettings tuning,
            bool enabled,
            ref float y,
            ShuttleCombatTuningCategory category)
        {
            if (category == ShuttleCombatTuningCategory.Weapon)
            {
                float topOffThreshold = tuning.WeaponAutomaticTopOffThreshold;
                bool topOffChanged = this.DrawFloatStepperRow(
                    new Rect(0f, y, viewRect.width, ShuttleV3DialogMetrics.CombatSettingRowHeight),
                    this.Tr("CT_Shuttle_CombatTuning_WeaponAutomaticTopOffThreshold"),
                    ref topOffThreshold,
                    0f,
                    10000f,
                    5f,
                    true,
                    this.Tr("CT_Shuttle_CombatTuning_WeaponAutomaticTopOffThresholdTooltip"));
                if (topOffChanged)
                {
                    tuning.WeaponAutomaticTopOffThreshold = Mathf.RoundToInt(topOffThreshold);
                    this.ApplySettingsChanged();
                }

                y += ShuttleV3DialogMetrics.CombatSettingRowPitch;
            }

            for (int i = 0; i < CombatTuningSettingDefinitions.Settings.Length; i++)
            {
                DefenseSettingDefinition definition =
                    CombatTuningSettingDefinitions.Settings[i];
                if (definition.Category != category)
                {
                    continue;
                }

                float value = definition.Getter(tuning);
                bool changed = this.DrawFloatStepperRow(
                    new Rect(0f, y, viewRect.width, ShuttleV3DialogMetrics.CombatSettingRowHeight),
                    this.Tr(definition.LabelKey),
                    ref value,
                    definition.Min,
                    definition.Max,
                    definition.Step,
                    enabled,
                    this.TranslateOptional(definition.TooltipKey));
                if (changed)
                {
                    definition.Setter(tuning, value);
                    this.ApplySettingsChanged();
                }

                y += ShuttleV3DialogMetrics.CombatSettingRowPitch;
            }
        }

        private void DrawAdvancedSettings(Rect viewRect, ShuttleCombatTuningSettings tuning, ref float y)
        {
            this.DrawDescription(
                new Rect(0f, y, viewRect.width, ShuttleV3DialogMetrics.CombatDescriptionTallHeight),
                "CT_Shuttle_CombatTuning_SandboxWarning".Translate().ToString());
            y += ShuttleV3DialogMetrics.CombatDescriptionTallHeight + ShuttleV3DialogMetrics.StandardGap + 2f;
            this.DrawDescription(
                new Rect(0f, y, viewRect.width, ShuttleV3DialogMetrics.CombatDescriptionShortHeight),
                "CT_Shuttle_CombatTuning_AppliesImmediately".Translate().ToString());
            y += ShuttleV3DialogMetrics.CombatDescriptionShortHeight + ShuttleV3DialogMetrics.StandardGap + 2f;
            this.DrawDescription(
                new Rect(0f, y, viewRect.width, ShuttleV3DialogMetrics.CombatDescriptionShortHeight),
                "CT_Shuttle_CombatTuning_RestartNotRequired".Translate().ToString());
            y += ShuttleV3DialogMetrics.CombatDescriptionShortHeight + (ShuttleV3DialogMetrics.StandardGap * 2f);
            this.DrawEffectiveCombatSummary(new Rect(0f, y, viewRect.width, ShuttleV3DialogMetrics.CombatSummaryHeight));
        }

        private bool DrawFloatStepperRow(Rect rect, string label, ref float value, float min, float max, float step, bool enabled, string tooltip)
        {
            float oldValue = value;
            ShuttleV3DialogLayout.DrawCardBackground(rect, false, false, enabled ? ShuttleV3DialogStyle.SettingsInfoRowColor : ShuttleV3DialogStyle.CardColor);
            Text.Font = GameFont.Tiny;
            GUI.color = enabled ? Color.white : ShuttleV3DialogStyle.MutedTextColor;
            ShuttleV3DialogLayout.SafeLabel(
                new Rect(
                    rect.x + ShuttleV3DialogMetrics.LargeGap,
                    rect.y + ShuttleV3DialogMetrics.CombatSettingLabelTop,
                    rect.width - ShuttleV3DialogMetrics.CombatSettingLabelRightReserve,
                    ShuttleV3DialogMetrics.CombatSettingLabelHeight),
                label);
            Rect minusRect = new Rect(
                rect.xMax - ShuttleV3DialogMetrics.CombatStepperMinusRight,
                rect.y + ShuttleV3DialogMetrics.CombatStepperTop,
                ShuttleV3DialogMetrics.CombatStepperButtonWidth,
                ShuttleV3DialogMetrics.CombatStepperButtonHeight);
            Rect valueRect = new Rect(
                rect.xMax - ShuttleV3DialogMetrics.CombatStepperValueRight,
                rect.y + ShuttleV3DialogMetrics.CombatStepperTop,
                ShuttleV3DialogMetrics.CombatStepperValueWidth,
                ShuttleV3DialogMetrics.CombatStepperButtonHeight);
            Rect plusRect = new Rect(
                rect.xMax - ShuttleV3DialogMetrics.CombatStepperPlusRight,
                rect.y + ShuttleV3DialogMetrics.CombatStepperTop,
                ShuttleV3DialogMetrics.CombatStepperButtonWidth,
                ShuttleV3DialogMetrics.CombatStepperButtonHeight);
            TextAnchor oldAnchor = Text.Anchor;
            try
            {
                if (enabled && this.DrawSettingsButton(minusRect, "-", false, false, null))
                {
                    value -= step;
                }

                ShuttleV3DialogLayout.DrawCardBackground(valueRect, false, false, ShuttleV3DialogStyle.CardColor);
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = enabled ? Color.white : ShuttleV3DialogStyle.MutedTextColor;
                ShuttleV3DialogLayout.SafeLabel(valueRect, this.FormatValue(value));
                if (enabled && this.DrawSettingsButton(plusRect, "+", false, false, null))
                {
                    value += step;
                }
            }
            finally
            {
                Text.Anchor = oldAnchor;
            }

            value = ShuttleCombatTuningSanitizer.ClampFinite(value, min, max, this.GetSafeFallback(min, max));
            this.AddTooltip(rect, tooltip);
            bool changed = enabled &&
                Math.Abs(oldValue - value) > ShuttleV3DialogMetrics.CombatValueEpsilon;

            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            return changed;
        }

        private void DrawCheckboxRow(Rect rect, string label, ref bool value, bool enabled, string tooltip)
        {
            bool oldValue = value;
            ShuttleV3DialogLayout.DrawCardBackground(rect, false, false, ShuttleV3DialogStyle.SettingsInfoRowColor);
            Rect checkboxRect = new Rect(rect.x + 10f, rect.y + 9f, 18f, 18f);
            if (enabled)
            {
                Widgets.Checkbox(checkboxRect.position, ref value);
            }
            else
            {
                bool preview = value;
                Widgets.Checkbox(checkboxRect.position, ref preview);
            }

            Text.Font = GameFont.Tiny;
            GUI.color = enabled ? Color.white : ShuttleV3DialogStyle.MutedTextColor;
            ShuttleV3DialogLayout.SafeLabel(new Rect(rect.x + 36f, rect.y + 8f, rect.width - 46f, 20f), label);
            this.AddTooltip(rect, tooltip);
            if (enabled && oldValue != value)
            {
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
            bool oldWordWrap = Text.WordWrap;
            Text.WordWrap = true;
            ShuttleV3DialogLayout.SafeLabel(new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, rect.height - 16f), text);
            Text.WordWrap = oldWordWrap;
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private void DrawHelpText(Rect rect, string text)
        {
            this.DrawDescription(rect, text);
        }

        private void DrawEffectiveCombatSummary(Rect rect)
        {
            ShuttleEffectiveCombatTuning effective = CeleTechShuttleMod.EffectiveCombatTuning;
            ShuttleV3DialogLayout.DrawCardBackground(rect, false, false, ShuttleV3DialogStyle.SettingsInfoRowColor);
            Text.Font = GameFont.Tiny;
            GUI.color = Color.white;
            ShuttleV3DialogLayout.SafeLabel(new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, 18f), "CT_Shuttle_CombatTuning_CurrentEffectiveSummary".Translate().ToString());
            GUI.color = ShuttleV3DialogStyle.MutedTextColor;
            ShuttleV3DialogLayout.SafeLabel(new Rect(rect.x + 10f, rect.y + 34f, rect.width - 20f, 18f), "CT_Shuttle_CombatTuning_EnableAdvanced".Translate().ToString() + ": " + (effective.AdvancedCombatTuningEnabled ? "CT_Shuttle_Common_Yes".Translate().ToString() : "CT_Shuttle_Common_No".Translate().ToString()));
            ShuttleV3DialogLayout.SafeLabel(new Rect(rect.x + 10f, rect.y + 58f, rect.width - 20f, 18f), "CT_Shuttle_CombatTuning_WeaponCooldown".Translate().ToString() + ": " + this.FormatMultiplier(effective.WeaponCooldownMultiplier));
            ShuttleV3DialogLayout.SafeLabel(new Rect(rect.x + 10f, rect.y + 82f, rect.width - 20f, 18f), "CT_Shuttle_CombatTuning_WeaponAmmoCapacity".Translate().ToString() + ": " + this.FormatMultiplier(effective.WeaponAmmoCapacityMultiplier));
            ShuttleV3DialogLayout.SafeLabel(new Rect(rect.x + 10f, rect.y + 106f, rect.width - 20f, 18f), "CT_Shuttle_CombatTuning_ShieldHitPoints".Translate().ToString() + ": " + this.FormatMultiplier(effective.ShieldHitPointsMultiplier));
            ShuttleV3DialogLayout.SafeLabel(new Rect(rect.x + 10f, rect.y + 130f, rect.width - 20f, 18f), "CT_Shuttle_CombatTuning_ShieldDamageTaken".Translate().ToString() + ": " + this.FormatMultiplier(effective.ShieldDamageTakenMultiplier));
            ShuttleV3DialogLayout.SafeLabel(new Rect(rect.x + 10f, rect.y + 154f, rect.width - 20f, 18f), "CT_Shuttle_CombatTuning_HullHitPoints".Translate().ToString() + ": " + this.FormatMultiplier(effective.HullHitPointsMultiplier));
            ShuttleV3DialogLayout.SafeLabel(new Rect(rect.x + 10f, rect.y + 178f, rect.width - 20f, 18f), "CT_Shuttle_CombatTuning_FlatDamageReduction".Translate().ToString() + ": " + this.FormatMultiplier(effective.FlatDamageReductionMultiplier));
            ShuttleV3DialogLayout.SafeLabel(new Rect(rect.x + 10f, rect.y + 202f, rect.width - 20f, 18f), "CT_Shuttle_CombatTuning_PointDefenseRadius".Translate().ToString() + ": " + this.FormatMultiplier(effective.FireControlPointDefenseRadiusMultiplier));
            ShuttleV3DialogLayout.SafeLabel(new Rect(rect.x + 10f, rect.y + 226f, rect.width - 20f, 18f), "CT_Shuttle_CombatTuning_AccuracyBonus".Translate().ToString() + ": " + this.FormatBonus(effective.FireControlAccuracyBonus));
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
                color = ShuttleV3DialogStyle.WithAlpha(
                    color,
                    selected
                        ? ShuttleV3DialogMetrics.TransparentButtonSelectedAlpha
                        : ShuttleV3DialogMetrics.TransparentButtonAlpha);
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

        private ShuttleCombatTuningSettings GetTuningSettings()
        {
            CeleTechShuttleModSettings settings = CeleTechShuttleMod.Settings;
            if (settings.CombatTuning == null)
            {
                settings.CombatTuning = new ShuttleCombatTuningSettings();
            }

            ShuttleCombatTuningSanitizer.Sanitize(settings.CombatTuning);
            return settings.CombatTuning;
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

        private float GetMainViewHeight()
        {
            if (this.selectedCategory == ShuttleCombatTuningCategory.Shield ||
                this.selectedCategory == ShuttleCombatTuningCategory.Armor)
            {
                return ShuttleV3DialogMetrics.CombatTallViewHeight;
            }

            if (this.selectedCategory == ShuttleCombatTuningCategory.Advanced)
            {
                return ShuttleV3DialogMetrics.CombatTallViewHeight;
            }

            return ShuttleV3DialogMetrics.CombatDefaultViewHeight;
        }

        private string GetSelectedCategoryLabel()
        {
            return this.Tr(CombatTuningSettingDefinitions.GetCategory(this.selectedCategory).LabelKey);
        }

        private string GetSelectedHelpText()
        {
            CombatTuningCategoryDefinition category =
                CombatTuningSettingDefinitions.GetCategory(this.selectedCategory);
            string label = this.Tr(category.LabelKey);
            if (this.selectedCategory == ShuttleCombatTuningCategory.Shield)
            {
                return this.Tr(category.HelpKey) + "\n\n" + this.Tr(category.SecondaryHelpKey);
            }

            if (this.selectedCategory == ShuttleCombatTuningCategory.Advanced)
            {
                return this.Tr(category.HelpKey);
            }

            return label + ": " + this.Tr(category.HelpKey);
        }

        private string FormatValue(float value)
        {
            return value.ToString("0.##");
        }

        private float GetSafeFallback(float min, float max)
        {
            if (min <= 1f && max >= 1f)
            {
                return 1f;
            }

            if (min <= 0f && max >= 0f)
            {
                return 0f;
            }

            return min;
        }

        private string FormatMultiplier(float value)
        {
            return "CT_Shuttle_CombatTuning_MultiplierFormat".Translate(value.ToString("0.##")).ToString();
        }

        private string FormatBonus(float value)
        {
            return "CT_Shuttle_CombatTuning_BonusFormat".Translate(value.ToString("+0.##;-0.##;0")).ToString();
        }

        private void AddTooltip(Rect rect, string tooltip)
        {
            if (!string.IsNullOrEmpty(tooltip))
            {
                TooltipHandler.TipRegion(rect, tooltip);
            }
        }

        private string TranslateOptional(string key)
        {
            return string.IsNullOrEmpty(key) ? null : this.Tr(key);
        }

        private string Tr(string key)
        {
            return key.Translate().ToString();
        }
    }
}
