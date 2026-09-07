using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle
{
    internal sealed class CeleTechShuttleModSettings : ModSettings
    {
        internal const int CurrentSettingsVersion = 10;
        internal const ShuttleVisualQuality DefaultVisualQuality = ShuttleVisualQuality.Fancy;
        internal const ShuttleHeaderMeterStyle DefaultHeaderMeterStyle = ShuttleHeaderMeterStyle.HudFrame;
        internal const ShuttleUIRefreshPreset DefaultUIRefreshPreset = ShuttleUIRefreshPreset.Responsive;
        internal const bool DefaultAdvancedOverridesEnabled = false;
        internal const int DefaultControlRefreshTicksOverride = 5;
        internal const int DefaultHeavyRefreshTicksOverride = 30;
        internal const bool DefaultTransparentButtons = true;
        internal const bool DefaultHoverGlow = true;
        internal const bool DefaultWindowAnimations = true;
        internal const bool DefaultSystemUpdateAnimations = true;
        internal const bool DefaultCargoIcons = true;
        internal const bool DefaultForceCompactCargoList = false;
        internal const bool DefaultShowPerformanceDashboard = true;

        public int settingsVersion = CurrentSettingsVersion;
        public ShuttleVisualQuality VisualQuality = DefaultVisualQuality;
        public ShuttleHeaderMeterStyle HeaderMeterStyle = DefaultHeaderMeterStyle;
        public ShuttleUIRefreshPreset UIRefreshPreset = DefaultUIRefreshPreset;
        public bool AdvancedOverridesEnabled = DefaultAdvancedOverridesEnabled;
        public int ControlRefreshTicksOverride = DefaultControlRefreshTicksOverride;
        public int HeavyRefreshTicksOverride = DefaultHeavyRefreshTicksOverride;
        public bool TransparentButtons = DefaultTransparentButtons;
        public bool HoverGlow = DefaultHoverGlow;
        public bool WindowAnimations = DefaultWindowAnimations;
        public bool SystemUpdateAnimations = DefaultSystemUpdateAnimations;
        public bool CargoIcons = DefaultCargoIcons;
        public bool ForceCompactCargoList = DefaultForceCompactCargoList;
        public bool ShowPerformanceDashboard = DefaultShowPerformanceDashboard;
        public bool EnableUIProfiler = false;
        public bool DevTimeCostLoggingEnabled = false;
        public bool EnablePerformanceSummaryLogs = false;
        public bool EnableDetailedPerformanceBreakdownLogs = false;
        public bool ShuttleControlTutorialsEnabled = true;
        public bool ShuttleControlTutorialsAutoStart = true;
        public List<string> CompletedShuttleControlTutorialPages = new List<string>();
        public List<string> CompletedShuttleControlContextTutorials = new List<string>();
        public bool HasLastCustomPerformanceSettings = false;
        public ShuttleVisualQuality LastCustomVisualQuality = DefaultVisualQuality;
        public ShuttleHeaderMeterStyle LastCustomHeaderMeterStyle = DefaultHeaderMeterStyle;
        public ShuttleUIRefreshPreset LastCustomUIRefreshPreset = DefaultUIRefreshPreset;
        public bool LastCustomAdvancedOverridesEnabled = DefaultAdvancedOverridesEnabled;
        public int LastCustomControlRefreshTicksOverride = DefaultControlRefreshTicksOverride;
        public int LastCustomHeavyRefreshTicksOverride = DefaultHeavyRefreshTicksOverride;
        public bool LastCustomTransparentButtons = DefaultTransparentButtons;
        public bool LastCustomHoverGlow = DefaultHoverGlow;
        public bool LastCustomWindowAnimations = DefaultWindowAnimations;
        public bool LastCustomSystemUpdateAnimations = DefaultSystemUpdateAnimations;
        public bool LastCustomCargoIcons = DefaultCargoIcons;
        public bool LastCustomForceCompactCargoList = DefaultForceCompactCargoList;
        public bool LastCustomEnableUIProfiler = false;
        public bool LastCustomDevTimeCostLoggingEnabled = false;
        public bool LastCustomEnablePerformanceSummaryLogs = false;
        public bool LastCustomEnableDetailedPerformanceBreakdownLogs = false;
        public ShuttleCombatTuningSettings CombatTuning = new ShuttleCombatTuningSettings();
        public ShuttleOtherSettings Other = new ShuttleOtherSettings();

        internal static CeleTechShuttleModSettings CreateDefault()
        {
            return new CeleTechShuttleModSettings();
        }

        internal void ResetToDefaults()
        {
            this.settingsVersion = CurrentSettingsVersion;
            this.ResetPerformanceToDefaults();
            this.ShowPerformanceDashboard = DefaultShowPerformanceDashboard;
            this.ShuttleControlTutorialsEnabled = true;
            this.ShuttleControlTutorialsAutoStart = true;
            if (this.CompletedShuttleControlTutorialPages == null)
            {
                this.CompletedShuttleControlTutorialPages = new List<string>();
            }
            else
            {
                this.CompletedShuttleControlTutorialPages.Clear();
            }

            if (this.CompletedShuttleControlContextTutorials == null)
            {
                this.CompletedShuttleControlContextTutorials = new List<string>();
            }
            else
            {
                this.CompletedShuttleControlContextTutorials.Clear();
            }

            this.CombatTuning = new ShuttleCombatTuningSettings();
            this.Other = new ShuttleOtherSettings();
        }

        internal void ResetPerformanceToDefaults()
        {
            this.settingsVersion = CurrentSettingsVersion;
            this.VisualQuality = DefaultVisualQuality;
            this.HeaderMeterStyle = DefaultHeaderMeterStyle;
            this.UIRefreshPreset = DefaultUIRefreshPreset;
            this.AdvancedOverridesEnabled = DefaultAdvancedOverridesEnabled;
            this.ControlRefreshTicksOverride = DefaultControlRefreshTicksOverride;
            this.HeavyRefreshTicksOverride = DefaultHeavyRefreshTicksOverride;
            this.TransparentButtons = DefaultTransparentButtons;
            this.HoverGlow = DefaultHoverGlow;
            this.WindowAnimations = DefaultWindowAnimations;
            this.SystemUpdateAnimations = DefaultSystemUpdateAnimations;
            this.CargoIcons = DefaultCargoIcons;
            this.ForceCompactCargoList = DefaultForceCompactCargoList;
            this.EnableUIProfiler = false;
            this.DevTimeCostLoggingEnabled = false;
            this.EnablePerformanceSummaryLogs = false;
            this.EnableDetailedPerformanceBreakdownLogs = false;
            this.ResetLastCustomPerformanceDefaults();
        }

        private void ResetLastCustomPerformanceDefaults()
        {
            this.HasLastCustomPerformanceSettings = false;
            this.LastCustomVisualQuality = DefaultVisualQuality;
            this.LastCustomHeaderMeterStyle = DefaultHeaderMeterStyle;
            this.LastCustomUIRefreshPreset = DefaultUIRefreshPreset;
            this.LastCustomAdvancedOverridesEnabled = DefaultAdvancedOverridesEnabled;
            this.LastCustomControlRefreshTicksOverride = DefaultControlRefreshTicksOverride;
            this.LastCustomHeavyRefreshTicksOverride = DefaultHeavyRefreshTicksOverride;
            this.LastCustomTransparentButtons = DefaultTransparentButtons;
            this.LastCustomHoverGlow = DefaultHoverGlow;
            this.LastCustomWindowAnimations = DefaultWindowAnimations;
            this.LastCustomSystemUpdateAnimations = DefaultSystemUpdateAnimations;
            this.LastCustomCargoIcons = DefaultCargoIcons;
            this.LastCustomForceCompactCargoList = DefaultForceCompactCargoList;
            this.LastCustomEnableUIProfiler = false;
            this.LastCustomDevTimeCostLoggingEnabled = false;
            this.LastCustomEnablePerformanceSummaryLogs = false;
            this.LastCustomEnableDetailedPerformanceBreakdownLogs = false;
        }

        internal void CaptureCurrentAsLastCustom()
        {
            ShuttleSettingsSanitizer.Sanitize(this);
            this.HasLastCustomPerformanceSettings = true;
            this.LastCustomVisualQuality = this.VisualQuality;
            this.LastCustomHeaderMeterStyle = this.HeaderMeterStyle;
            this.LastCustomUIRefreshPreset = this.UIRefreshPreset;
            this.LastCustomAdvancedOverridesEnabled = this.AdvancedOverridesEnabled;
            this.LastCustomControlRefreshTicksOverride = this.ControlRefreshTicksOverride;
            this.LastCustomHeavyRefreshTicksOverride = this.HeavyRefreshTicksOverride;
            this.LastCustomTransparentButtons = this.TransparentButtons;
            this.LastCustomHoverGlow = this.HoverGlow;
            this.LastCustomWindowAnimations = this.WindowAnimations;
            this.LastCustomSystemUpdateAnimations = this.SystemUpdateAnimations;
            this.LastCustomCargoIcons = this.CargoIcons;
            this.LastCustomForceCompactCargoList = this.ForceCompactCargoList;
            this.LastCustomEnableUIProfiler = this.EnableUIProfiler;
            this.LastCustomDevTimeCostLoggingEnabled = this.DevTimeCostLoggingEnabled;
            this.LastCustomEnablePerformanceSummaryLogs = this.EnablePerformanceSummaryLogs;
            this.LastCustomEnableDetailedPerformanceBreakdownLogs =
                this.EnableDetailedPerformanceBreakdownLogs;
        }

        internal void RestoreLastCustomOrKeepCurrent()
        {
            if (!this.HasLastCustomPerformanceSettings)
            {
                this.CaptureCurrentAsLastCustom();
                return;
            }

            this.VisualQuality = this.LastCustomVisualQuality;
            this.HeaderMeterStyle = this.LastCustomHeaderMeterStyle;
            this.UIRefreshPreset = this.LastCustomUIRefreshPreset;
            this.AdvancedOverridesEnabled = this.LastCustomAdvancedOverridesEnabled;
            this.ControlRefreshTicksOverride = this.LastCustomControlRefreshTicksOverride;
            this.HeavyRefreshTicksOverride = this.LastCustomHeavyRefreshTicksOverride;
            this.TransparentButtons = this.LastCustomTransparentButtons;
            this.HoverGlow = this.LastCustomHoverGlow;
            this.WindowAnimations = this.LastCustomWindowAnimations;
            this.SystemUpdateAnimations = this.LastCustomSystemUpdateAnimations;
            this.CargoIcons = this.LastCustomCargoIcons;
            this.ForceCompactCargoList = this.LastCustomForceCompactCargoList;
            this.EnableUIProfiler = this.LastCustomEnableUIProfiler;
            this.DevTimeCostLoggingEnabled = this.LastCustomDevTimeCostLoggingEnabled;
            this.EnablePerformanceSummaryLogs = this.LastCustomEnablePerformanceSummaryLogs;
            this.EnableDetailedPerformanceBreakdownLogs =
                this.LastCustomEnableDetailedPerformanceBreakdownLogs;
            ShuttleSettingsSanitizer.Sanitize(this);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.settingsVersion, "settingsVersion", CurrentSettingsVersion);
            Scribe_Values.Look(ref this.VisualQuality, "VisualQuality", DefaultVisualQuality);
            Scribe_Values.Look(ref this.HeaderMeterStyle, "HeaderMeterStyle", DefaultHeaderMeterStyle);
            Scribe_Values.Look(ref this.UIRefreshPreset, "UIRefreshPreset", DefaultUIRefreshPreset);
            Scribe_Values.Look(ref this.AdvancedOverridesEnabled, "AdvancedOverridesEnabled", DefaultAdvancedOverridesEnabled);
            Scribe_Values.Look(ref this.ControlRefreshTicksOverride, "ControlRefreshTicksOverride", DefaultControlRefreshTicksOverride);
            Scribe_Values.Look(ref this.HeavyRefreshTicksOverride, "HeavyRefreshTicksOverride", DefaultHeavyRefreshTicksOverride);
            Scribe_Values.Look(ref this.TransparentButtons, "TransparentButtons", DefaultTransparentButtons);
            Scribe_Values.Look(ref this.HoverGlow, "HoverGlow", DefaultHoverGlow);
            Scribe_Values.Look(ref this.WindowAnimations, "WindowAnimations", DefaultWindowAnimations);
            Scribe_Values.Look(ref this.SystemUpdateAnimations, "SystemUpdateAnimations", DefaultSystemUpdateAnimations);
            Scribe_Values.Look(ref this.CargoIcons, "CargoIcons", DefaultCargoIcons);
            Scribe_Values.Look(ref this.ForceCompactCargoList, "ForceCompactCargoList", DefaultForceCompactCargoList);
            Scribe_Values.Look(
                ref this.ShowPerformanceDashboard,
                "ShowPerformanceDashboard",
                DefaultShowPerformanceDashboard);
            bool deprecatedUseV3ControlUI = false;
            Scribe_Values.Look(ref deprecatedUseV3ControlUI, "UseV3ControlUI", false);
            Scribe_Values.Look(ref this.EnableUIProfiler, "EnableUIProfiler", false);
            Scribe_Values.Look(ref this.DevTimeCostLoggingEnabled, "DevTimeCostLoggingEnabled", false);
            Scribe_Values.Look(ref this.EnablePerformanceSummaryLogs, "EnablePerformanceSummaryLogs", false);
            Scribe_Values.Look(
                ref this.EnableDetailedPerformanceBreakdownLogs,
                "EnableDetailedPerformanceBreakdownLogs",
                false);
            Scribe_Values.Look(
                ref this.ShuttleControlTutorialsEnabled,
                "ShuttleControlTutorialsEnabled",
                true);
            Scribe_Values.Look(
                ref this.ShuttleControlTutorialsAutoStart,
                "ShuttleControlTutorialsAutoStart",
                true);
            Scribe_Collections.Look(
                ref this.CompletedShuttleControlTutorialPages,
                "CompletedShuttleControlTutorialPages",
                LookMode.Value);
            Scribe_Collections.Look(
                ref this.CompletedShuttleControlContextTutorials,
                "CompletedShuttleControlContextTutorials",
                LookMode.Value);
            Scribe_Values.Look(ref this.HasLastCustomPerformanceSettings, "HasLastCustomPerformanceSettings", false);
            Scribe_Values.Look(ref this.LastCustomVisualQuality, "LastCustomVisualQuality", DefaultVisualQuality);
            Scribe_Values.Look(ref this.LastCustomHeaderMeterStyle, "LastCustomHeaderMeterStyle", DefaultHeaderMeterStyle);
            Scribe_Values.Look(ref this.LastCustomUIRefreshPreset, "LastCustomUIRefreshPreset", DefaultUIRefreshPreset);
            Scribe_Values.Look(ref this.LastCustomAdvancedOverridesEnabled, "LastCustomAdvancedOverridesEnabled", DefaultAdvancedOverridesEnabled);
            Scribe_Values.Look(ref this.LastCustomControlRefreshTicksOverride, "LastCustomControlRefreshTicksOverride", DefaultControlRefreshTicksOverride);
            Scribe_Values.Look(ref this.LastCustomHeavyRefreshTicksOverride, "LastCustomHeavyRefreshTicksOverride", DefaultHeavyRefreshTicksOverride);
            Scribe_Values.Look(ref this.LastCustomTransparentButtons, "LastCustomTransparentButtons", DefaultTransparentButtons);
            Scribe_Values.Look(ref this.LastCustomHoverGlow, "LastCustomHoverGlow", DefaultHoverGlow);
            Scribe_Values.Look(ref this.LastCustomWindowAnimations, "LastCustomWindowAnimations", DefaultWindowAnimations);
            Scribe_Values.Look(ref this.LastCustomSystemUpdateAnimations, "LastCustomSystemUpdateAnimations", DefaultSystemUpdateAnimations);
            Scribe_Values.Look(ref this.LastCustomCargoIcons, "LastCustomCargoIcons", DefaultCargoIcons);
            Scribe_Values.Look(ref this.LastCustomForceCompactCargoList, "LastCustomForceCompactCargoList", DefaultForceCompactCargoList);
            Scribe_Values.Look(ref this.LastCustomEnableUIProfiler, "LastCustomEnableUIProfiler", false);
            Scribe_Values.Look(ref this.LastCustomDevTimeCostLoggingEnabled, "LastCustomDevTimeCostLoggingEnabled", false);
            Scribe_Values.Look(ref this.LastCustomEnablePerformanceSummaryLogs, "LastCustomEnablePerformanceSummaryLogs", false);
            Scribe_Values.Look(
                ref this.LastCustomEnableDetailedPerformanceBreakdownLogs,
                "LastCustomEnableDetailedPerformanceBreakdownLogs",
                false);
            Scribe_Deep.Look(ref this.CombatTuning, "CombatTuning");
            Scribe_Deep.Look(ref this.Other, "Other");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                int loadedSettingsVersion = this.settingsVersion;
                if (this.CombatTuning == null)
                {
                    this.CombatTuning = new ShuttleCombatTuningSettings();
                }

                if (this.Other == null)
                {
                    this.Other = new ShuttleOtherSettings();
                }

                if (loadedSettingsVersion < 4)
                {
                    this.Other.AllowColonistOnboardDeviceUseAtPlayerHome =
                        ShuttleOtherSettings.DefaultAllowColonistOnboardDeviceUseAtPlayerHome;
                    this.Other.AllowMechOnboardDeviceUseAtPlayerHome =
                        ShuttleOtherSettings.DefaultAllowMechOnboardDeviceUseAtPlayerHome;
                }

                if (loadedSettingsVersion < 7)
                {
                    float legacyEnergyMultiplier = loadedSettingsVersion < 6
                        ? this.Other.LegacyPayloadLiftEnergyPerKgWd /
                            ShuttleOtherSettings.LegacyDefaultPayloadLiftEnergyPerKgWd
                        : this.Other.LegacyPayloadLiftEnergyMultiplier;
                    this.Other.PayloadCapacityMultiplier =
                        ShuttleOtherSettings.ConvertLegacyEnergyMultiplierToCapacity(
                            legacyEnergyMultiplier);
                }

                if (loadedSettingsVersion < 8)
                {
                    this.Other.ShareRefrigeratedCargoMassWithOverallCapacity =
                        ShuttleOtherSettings.DefaultShareRefrigeratedCargoMassWithOverallCapacity;
                    this.Other.IndependentRefrigeratedCargoCapacityRatio =
                        ShuttleOtherSettings.DefaultIndependentRefrigeratedCargoCapacityRatio;
                }

                if (loadedSettingsVersion < 6)
                {
                    this.Other.ConstructionWorkMultiplier =
                        ShuttleOtherSettings.DefaultConstructionWorkMultiplier;
                }

                if (this.CompletedShuttleControlTutorialPages == null)
                {
                    this.CompletedShuttleControlTutorialPages = new List<string>();
                }

                if (this.CompletedShuttleControlContextTutorials == null)
                {
                    this.CompletedShuttleControlContextTutorials = new List<string>();
                }

                if (loadedSettingsVersion < 2 &&
                    this.HasLegacyStandardPerformanceDefaults())
                {
                    this.ApplyDefaultPerformanceSettings();
                }

                ShuttleSettingsSanitizer.Sanitize(this);
                CeleTechShuttleMod.RebuildEffectiveSettings();
            }
        }

        private bool HasLegacyStandardPerformanceDefaults()
        {
            return this.VisualQuality == ShuttleVisualQuality.Standard &&
                this.HeaderMeterStyle == ShuttleHeaderMeterStyle.HudFrame &&
                this.UIRefreshPreset == ShuttleUIRefreshPreset.Balanced &&
                !this.AdvancedOverridesEnabled &&
                this.ControlRefreshTicksOverride == 15 &&
                this.HeavyRefreshTicksOverride == 60 &&
                !this.TransparentButtons &&
                this.HoverGlow &&
                this.WindowAnimations &&
                this.SystemUpdateAnimations &&
                this.CargoIcons &&
                !this.ForceCompactCargoList;
        }

        private void ApplyDefaultPerformanceSettings()
        {
            this.VisualQuality = DefaultVisualQuality;
            this.HeaderMeterStyle = DefaultHeaderMeterStyle;
            this.UIRefreshPreset = DefaultUIRefreshPreset;
            this.AdvancedOverridesEnabled = DefaultAdvancedOverridesEnabled;
            this.ControlRefreshTicksOverride = DefaultControlRefreshTicksOverride;
            this.HeavyRefreshTicksOverride = DefaultHeavyRefreshTicksOverride;
            this.TransparentButtons = DefaultTransparentButtons;
            this.HoverGlow = DefaultHoverGlow;
            this.WindowAnimations = DefaultWindowAnimations;
            this.SystemUpdateAnimations = DefaultSystemUpdateAnimations;
            this.CargoIcons = DefaultCargoIcons;
            this.ForceCompactCargoList = DefaultForceCompactCargoList;
            if (!this.HasLastCustomPerformanceSettings)
            {
                this.LastCustomVisualQuality = DefaultVisualQuality;
                this.LastCustomHeaderMeterStyle = DefaultHeaderMeterStyle;
                this.LastCustomUIRefreshPreset = DefaultUIRefreshPreset;
                this.LastCustomAdvancedOverridesEnabled = DefaultAdvancedOverridesEnabled;
                this.LastCustomControlRefreshTicksOverride = DefaultControlRefreshTicksOverride;
                this.LastCustomHeavyRefreshTicksOverride = DefaultHeavyRefreshTicksOverride;
                this.LastCustomTransparentButtons = DefaultTransparentButtons;
                this.LastCustomHoverGlow = DefaultHoverGlow;
                this.LastCustomWindowAnimations = DefaultWindowAnimations;
                this.LastCustomSystemUpdateAnimations = DefaultSystemUpdateAnimations;
                this.LastCustomCargoIcons = DefaultCargoIcons;
                this.LastCustomForceCompactCargoList = DefaultForceCompactCargoList;
            }
        }
    }
}
