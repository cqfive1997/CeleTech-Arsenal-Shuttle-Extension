using System;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle
{
    public sealed class CeleTechShuttleMod : Mod
    {
        private static CeleTechShuttleModSettings settings;
        private static ShuttleEffectiveSettings effectiveSettings;
        private static ShuttleEffectiveCombatTuning effectiveCombatTuning;

        public CeleTechShuttleMod(ModContentPack content)
            : base(content)
        {
            settings = this.GetSettings<CeleTechShuttleModSettings>();
            ShuttleSettingsSanitizer.Sanitize(settings);
            RebuildEffectiveSettings();
        }

        internal static CeleTechShuttleModSettings Settings
        {
            get
            {
                if (settings == null)
                {
                    settings = CeleTechShuttleModSettings.CreateDefault();
                    ShuttleSettingsSanitizer.Sanitize(settings);
                }

                return settings;
            }
        }

        internal static ShuttleEffectiveSettings EffectiveSettings
        {
            get
            {
                if (effectiveSettings == null)
                {
                    RebuildEffectiveSettings();
                }

                return effectiveSettings ?? ShuttleEffectiveSettings.Default;
            }
        }

        internal static ShuttleEffectiveCombatTuning EffectiveCombatTuning
        {
            get
            {
                if (effectiveCombatTuning == null)
                {
                    RebuildEffectiveSettings();
                }

                return effectiveCombatTuning ?? ShuttleEffectiveCombatTuning.Default;
            }
        }

        internal static bool ShouldLogDevTimeCosts
        {
            get
            {
                // Compatibility alias for older low-frequency diagnostics. PerfSummary profilers use ShuttleDiagnosticGate directly.
                return ShuttleDiagnosticGate.ShouldLogBasicDiagnostics;
            }
        }

        internal static void RebuildEffectiveSettings()
        {
            effectiveSettings = ShuttleEffectiveSettings.FromSettings(Settings);
            effectiveCombatTuning = ShuttleEffectiveCombatTuning.FromSettings(Settings);
        }

        internal static void SaveSettingsSafe()
        {
            CeleTechShuttleModSettings activeSettings = Settings;
            ShuttleSettingsSanitizer.Sanitize(activeSettings);
            RebuildEffectiveSettings();
            try
            {
                activeSettings.Write();
            }
            catch (Exception exception)
            {
                if (Prefs.DevMode)
                {
                    Log.WarningOnce(
                        "[CeleTech Shuttle] Could not save Shuttle Extension mod settings. Exception: " + exception,
                        MakeSaveSettingsWarningHash(exception));
                }
            }
        }

        public override string SettingsCategory()
        {
            return "CeleTech Arsenal - Shuttle Extension";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            CeleTechShuttleModSettings activeSettings = Settings;
            ShuttleEffectiveSettings effective = EffectiveSettings;
            bool changed = false;
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);
            listing.Label("CT_Shuttle_Settings_EffectiveSummary".Translate().ToString());
            listing.Gap(6f);
            changed |= this.DrawEnumButton(
                listing,
                "CT_Shuttle_Settings_VisualQuality".Translate().ToString(),
                GetVisualQualityLabel(activeSettings.VisualQuality),
                delegate
                {
                    activeSettings.VisualQuality = NextVisualQuality(activeSettings.VisualQuality);
                });
            changed |= this.DrawEnumButton(
                listing,
                "CT_Shuttle_Settings_HeaderMeterStyle".Translate().ToString(),
                GetHeaderMeterStyleLabel(activeSettings.HeaderMeterStyle),
                delegate
                {
                    activeSettings.HeaderMeterStyle = NextEnum(activeSettings.HeaderMeterStyle);
                });
            changed |= this.DrawEnumButton(
                listing,
                "CT_Shuttle_Settings_UIRefreshPreset".Translate().ToString(),
                GetRefreshPresetLabel(activeSettings.UIRefreshPreset),
                delegate
                {
                    activeSettings.UIRefreshPreset = NextEnum(activeSettings.UIRefreshPreset);
                });
            listing.Label("CT_Shuttle_Settings_CurrentEffectiveRefresh".Translate(effective.ControlRefreshTicks, effective.HeavyRefreshTicks).ToString());
            bool oldTransparentButtons = activeSettings.TransparentButtons;
            bool oldHoverGlow = activeSettings.HoverGlow;
            bool oldWindowAnimations = activeSettings.WindowAnimations;
            bool oldSystemUpdateAnimations = activeSettings.SystemUpdateAnimations;
            bool oldCargoIcons = activeSettings.CargoIcons;
            bool oldAdvancedOverridesEnabled = activeSettings.AdvancedOverridesEnabled;
            bool oldForceCompactCargoList = activeSettings.ForceCompactCargoList;
            bool oldEnableUIProfiler = activeSettings.EnableUIProfiler;
            bool oldDevTimeCostLoggingEnabled = activeSettings.DevTimeCostLoggingEnabled;
            bool oldEnablePerformanceSummaryLogs = activeSettings.EnablePerformanceSummaryLogs;
            bool oldEnableDetailedPerformanceBreakdownLogs =
                activeSettings.EnableDetailedPerformanceBreakdownLogs;
            listing.CheckboxLabeled("CT_Shuttle_Settings_TransparentButtons".Translate().ToString(), ref activeSettings.TransparentButtons);
            listing.CheckboxLabeled("CT_Shuttle_Settings_HoverGlow".Translate().ToString(), ref activeSettings.HoverGlow);
            listing.CheckboxLabeled("CT_Shuttle_Settings_WindowAnimations".Translate().ToString(), ref activeSettings.WindowAnimations);
            listing.CheckboxLabeled("CT_Shuttle_Settings_SystemUpdateAnimations".Translate().ToString(), ref activeSettings.SystemUpdateAnimations);
            listing.CheckboxLabeled("CT_Shuttle_Settings_CargoIcons".Translate().ToString(), ref activeSettings.CargoIcons);
            listing.CheckboxLabeled("CT_Shuttle_Settings_AdvancedOverrides".Translate().ToString(), ref activeSettings.AdvancedOverridesEnabled);
            if (activeSettings.AdvancedOverridesEnabled)
            {
                changed |= this.DrawIntStepper(
                    listing,
                    "CT_Shuttle_Settings_ControlRefreshTicks".Translate().ToString(),
                    ref activeSettings.ControlRefreshTicksOverride,
                    1,
                    120,
                    1);
                changed |= this.DrawIntStepper(
                    listing,
                    "CT_Shuttle_Settings_HeavyRefreshTicks".Translate().ToString(),
                    ref activeSettings.HeavyRefreshTicksOverride,
                    5,
                    300,
                    5);
            }

            listing.CheckboxLabeled("CT_Shuttle_Settings_ForceCompactCargoList".Translate().ToString(), ref activeSettings.ForceCompactCargoList);
            listing.CheckboxLabeled("CT_Shuttle_Settings_EnableUIProfiler".Translate().ToString(), ref activeSettings.EnableUIProfiler, "CT_Shuttle_Settings_EnableUIProfilerTooltip".Translate().ToString());
            if (Prefs.DevMode)
            {
                listing.CheckboxLabeled(
                    "CT_Shuttle_Settings_DevTimeCostLogging".Translate().ToString(),
                    ref activeSettings.DevTimeCostLoggingEnabled,
                    "CT_Shuttle_Settings_DevTimeCostLoggingTooltip".Translate().ToString());
                listing.CheckboxLabeled(
                    "CT_Shuttle_Settings_PerformanceSummaryLogs".Translate().ToString(),
                    ref activeSettings.EnablePerformanceSummaryLogs,
                    "CT_Shuttle_Settings_PerformanceSummaryLogsTooltip".Translate().ToString());
                if (activeSettings.EnablePerformanceSummaryLogs)
                {
                    listing.CheckboxLabeled(
                        "CT_Shuttle_Settings_DetailedPerformanceBreakdownLogs".Translate().ToString(),
                        ref activeSettings.EnableDetailedPerformanceBreakdownLogs,
                        "CT_Shuttle_Settings_DetailedPerformanceBreakdownLogsTooltip".Translate().ToString());
                }
                else
                {
                    activeSettings.EnableDetailedPerformanceBreakdownLogs = false;
                }
            }

            ShuttleEffectiveSettings previewEffective = ShuttleEffectiveSettings.FromSettings(activeSettings);
            listing.Label("CT_Shuttle_Settings_CurrentEffectiveRefresh".Translate(
                previewEffective.ControlRefreshTicks,
                previewEffective.HeavyRefreshTicks).ToString());
            changed |= oldTransparentButtons != activeSettings.TransparentButtons ||
                oldHoverGlow != activeSettings.HoverGlow ||
                oldWindowAnimations != activeSettings.WindowAnimations ||
                oldSystemUpdateAnimations != activeSettings.SystemUpdateAnimations ||
                oldCargoIcons != activeSettings.CargoIcons ||
                oldAdvancedOverridesEnabled != activeSettings.AdvancedOverridesEnabled ||
                oldForceCompactCargoList != activeSettings.ForceCompactCargoList ||
                oldEnableUIProfiler != activeSettings.EnableUIProfiler ||
                oldDevTimeCostLoggingEnabled != activeSettings.DevTimeCostLoggingEnabled ||
                oldEnablePerformanceSummaryLogs != activeSettings.EnablePerformanceSummaryLogs ||
                oldEnableDetailedPerformanceBreakdownLogs !=
                    activeSettings.EnableDetailedPerformanceBreakdownLogs;
            listing.Gap(8f);
            if (listing.ButtonText("CT_Shuttle_Settings_ResetDefaults".Translate().ToString()))
            {
                activeSettings.ResetPerformanceToDefaults();
                changed = true;
            }

            listing.End();
            if (changed)
            {
                SaveSettingsSafe();
            }
        }

        private bool DrawEnumButton(Listing_Standard listing, string label, string value, Action onClick)
        {
            Rect rect = listing.GetRect(30f);
            Widgets.Label(new Rect(rect.x, rect.y + 5f, rect.width * 0.48f, rect.height), label);
            if (Widgets.ButtonText(new Rect(rect.x + (rect.width * 0.52f), rect.y, rect.width * 0.48f, rect.height), value))
            {
                if (onClick != null)
                {
                    onClick();
                }

                return true;
            }

            return false;
        }

        private bool DrawIntStepper(
            Listing_Standard listing,
            string label,
            ref int value,
            int min,
            int max,
            int step)
        {
            int oldValue = value;
            Rect rect = listing.GetRect(30f);
            Widgets.Label(new Rect(rect.x, rect.y + 5f, rect.width * 0.48f, rect.height), label);
            Rect minusRect = new Rect(rect.x + (rect.width * 0.52f), rect.y, 34f, rect.height);
            Rect valueRect = new Rect(minusRect.xMax + 4f, rect.y, 64f, rect.height);
            Rect plusRect = new Rect(valueRect.xMax + 4f, rect.y, 34f, rect.height);
            if (Widgets.ButtonText(minusRect, "-"))
            {
                value -= Mathf.Max(1, step);
            }

            Widgets.Label(valueRect, Mathf.Clamp(value, min, max).ToString());
            if (Widgets.ButtonText(plusRect, "+"))
            {
                value += Mathf.Max(1, step);
            }

            value = Mathf.Clamp(value, min, max);
            return oldValue != value;
        }

        internal static string GetVisualQualityLabel(ShuttleVisualQuality value)
        {
            if (value == ShuttleVisualQuality.Minimal)
            {
                return "CT_Shuttle_Settings_Preset_Minimal".Translate().ToString();
            }

            if (value == ShuttleVisualQuality.Performance)
            {
                return "CT_Shuttle_Settings_Preset_Performance".Translate().ToString();
            }

            if (value == ShuttleVisualQuality.Fancy)
            {
                return "CT_Shuttle_Settings_Preset_Fancy".Translate().ToString();
            }

            return "CT_Shuttle_Settings_Preset_Standard".Translate().ToString();
        }

        internal static ShuttleVisualQuality NextVisualQuality(ShuttleVisualQuality value)
        {
            if (value == ShuttleVisualQuality.Minimal)
            {
                return ShuttleVisualQuality.Performance;
            }

            if (value == ShuttleVisualQuality.Performance)
            {
                return ShuttleVisualQuality.Standard;
            }

            if (value == ShuttleVisualQuality.Standard)
            {
                return ShuttleVisualQuality.Fancy;
            }

            return ShuttleVisualQuality.Minimal;
        }

        internal static string GetHeaderMeterStyleLabel(ShuttleHeaderMeterStyle value)
        {
            if (value == ShuttleHeaderMeterStyle.NumericOnly)
            {
                return "CT_Shuttle_Settings_Meter_NumericOnly".Translate().ToString();
            }

            if (value == ShuttleHeaderMeterStyle.Linear)
            {
                return "CT_Shuttle_Settings_Meter_Linear".Translate().ToString();
            }

            if (value == ShuttleHeaderMeterStyle.Circular)
            {
                return "CT_Shuttle_Settings_Meter_CircularFallback".Translate().ToString();
            }

            return "CT_Shuttle_Settings_Meter_HudFrame".Translate().ToString();
        }

        internal static string GetRefreshPresetLabel(ShuttleUIRefreshPreset value)
        {
            if (value == ShuttleUIRefreshPreset.PowerSaver)
            {
                return "CT_Shuttle_Settings_Refresh_PowerSaver".Translate().ToString();
            }

            if (value == ShuttleUIRefreshPreset.Responsive)
            {
                return "CT_Shuttle_Settings_Refresh_Responsive".Translate().ToString();
            }

            if (value == ShuttleUIRefreshPreset.Immediate)
            {
                return "CT_Shuttle_Settings_Refresh_Immediate".Translate().ToString();
            }

            return "CT_Shuttle_Settings_Refresh_Balanced".Translate().ToString();
        }

        internal static T NextEnum<T>(T value)
            where T : struct
        {
            T[] values = (T[])Enum.GetValues(typeof(T));
            int index = Array.IndexOf(values, value);
            if (index < 0 || index >= values.Length - 1)
            {
                return values.Length > 0 ? values[0] : value;
            }

            return values[index + 1];
        }

        private static int MakeSaveSettingsWarningHash(Exception exception)
        {
            unchecked
            {
                int hash = 739;
                hash = (hash * 397) + StableStringHash("CeleTechShuttleMod.SaveSettingsSafe");
                hash = (hash * 397) + StableStringHash(exception != null && exception.GetType() != null
                    ? exception.GetType().FullName
                    : null);
                return hash;
            }
        }

        private static int StableStringHash(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return 0;
            }

            unchecked
            {
                int hash = 17;
                for (int i = 0; i < value.Length; i++)
                {
                    hash = (hash * 31) + value[i];
                }

                return hash;
            }
        }
    }
}
