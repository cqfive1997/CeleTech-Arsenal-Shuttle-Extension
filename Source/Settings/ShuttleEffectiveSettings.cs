using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle
{
    internal sealed class ShuttleEffectiveSettings
    {
        internal readonly ShuttleVisualQuality VisualQuality;
        internal readonly ShuttleHeaderMeterStyle HeaderMeterStyle;
        internal readonly ShuttleUIRefreshPreset UIRefreshPreset;
        internal readonly int ControlRefreshTicks;
        internal readonly int HeavyRefreshTicks;
        internal readonly bool UseTransparentButtons;
        internal readonly bool UseHoverGlow;
        internal readonly bool UseWindowAnimations;
        internal readonly bool UseSystemUpdateAnimations;
        internal readonly bool UseCargoIcons;
        internal readonly bool ForceCompactCargoList;
        private readonly bool enableUIProfilerRaw;
        private readonly bool devTimeCostLoggingEnabledRaw;
        private readonly bool performanceSummaryLogsEnabledRaw;
        private readonly bool detailedPerformanceBreakdownLogsEnabledRaw;

        internal bool EnableUIProfiler
        {
            get
            {
                return this.enableUIProfilerRaw && Prefs.DevMode;
            }
        }

        internal bool DevTimeCostLoggingEnabled
        {
            get
            {
                return this.devTimeCostLoggingEnabledRaw && Prefs.DevMode;
            }
        }

        internal bool PerformanceSummaryLogsEnabled
        {
            get
            {
                return this.performanceSummaryLogsEnabledRaw && Prefs.DevMode;
            }
        }

        internal bool DetailedPerformanceBreakdownLogsEnabled
        {
            get
            {
                return this.performanceSummaryLogsEnabledRaw &&
                    this.detailedPerformanceBreakdownLogsEnabledRaw &&
                    Prefs.DevMode;
            }
        }

        private ShuttleEffectiveSettings(
            ShuttleVisualQuality visualQuality,
            ShuttleHeaderMeterStyle headerMeterStyle,
            ShuttleUIRefreshPreset uiRefreshPreset,
            int controlRefreshTicks,
            int heavyRefreshTicks,
            bool useTransparentButtons,
            bool useHoverGlow,
            bool useWindowAnimations,
            bool useSystemUpdateAnimations,
            bool useCargoIcons,
            bool forceCompactCargoList,
            bool enableUIProfiler,
            bool devTimeCostLoggingEnabled,
            bool performanceSummaryLogsEnabled,
            bool detailedPerformanceBreakdownLogsEnabled)
        {
            this.VisualQuality = visualQuality;
            this.HeaderMeterStyle = headerMeterStyle;
            this.UIRefreshPreset = uiRefreshPreset;
            this.ControlRefreshTicks = controlRefreshTicks;
            this.HeavyRefreshTicks = heavyRefreshTicks;
            this.UseTransparentButtons = useTransparentButtons;
            this.UseHoverGlow = useHoverGlow;
            this.UseWindowAnimations = useWindowAnimations;
            this.UseSystemUpdateAnimations = useSystemUpdateAnimations;
            this.UseCargoIcons = useCargoIcons;
            this.ForceCompactCargoList = forceCompactCargoList;
            this.enableUIProfilerRaw = enableUIProfiler;
            this.devTimeCostLoggingEnabledRaw = devTimeCostLoggingEnabled;
            this.performanceSummaryLogsEnabledRaw = performanceSummaryLogsEnabled;
            this.detailedPerformanceBreakdownLogsEnabledRaw =
                detailedPerformanceBreakdownLogsEnabled;
        }

        internal static ShuttleEffectiveSettings FromSettings(CeleTechShuttleModSettings settings)
        {
            if (settings == null)
            {
                settings = CeleTechShuttleModSettings.CreateDefault();
            }

            ShuttleSettingsSanitizer.Sanitize(settings);

            int controlTicks;
            int heavyTicks;
            GetPresetTicks(settings.UIRefreshPreset, out controlTicks, out heavyTicks);
            if (settings.AdvancedOverridesEnabled)
            {
                controlTicks = Mathf.Clamp(settings.ControlRefreshTicksOverride, 1, 120);
                heavyTicks = Mathf.Clamp(settings.HeavyRefreshTicksOverride, 5, 300);
                if (heavyTicks < controlTicks)
                {
                    heavyTicks = controlTicks;
                }
            }

            return new ShuttleEffectiveSettings(
                settings.VisualQuality,
                settings.HeaderMeterStyle,
                settings.UIRefreshPreset,
                controlTicks,
                heavyTicks,
                settings.TransparentButtons,
                settings.HoverGlow,
                settings.WindowAnimations,
                settings.SystemUpdateAnimations,
                settings.CargoIcons,
                settings.ForceCompactCargoList,
                settings.EnableUIProfiler,
                settings.DevTimeCostLoggingEnabled,
                settings.EnablePerformanceSummaryLogs,
                settings.EnableDetailedPerformanceBreakdownLogs);
        }

        internal static ShuttleEffectiveSettings Default
        {
            get
            {
                return FromSettings(CeleTechShuttleModSettings.CreateDefault());
            }
        }

        private static void GetPresetTicks(
            ShuttleUIRefreshPreset preset,
            out int controlTicks,
            out int heavyTicks)
        {
            if (preset == ShuttleUIRefreshPreset.PowerSaver)
            {
                controlTicks = 30;
                heavyTicks = 120;
                return;
            }

            if (preset == ShuttleUIRefreshPreset.Responsive)
            {
                controlTicks = 5;
                heavyTicks = 30;
                return;
            }

            if (preset == ShuttleUIRefreshPreset.Immediate)
            {
                controlTicks = 1;
                heavyTicks = 10;
                return;
            }

            controlTicks = 15;
            heavyTicks = 60;
        }
    }
}
