using System;
using System.Collections.Generic;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle
{
    internal static class ShuttleSettingsSanitizer
    {
        internal static void Sanitize(CeleTechShuttleModSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            settings.settingsVersion = CeleTechShuttleModSettings.CurrentSettingsVersion;
            if (!Enum.IsDefined(typeof(ShuttleVisualQuality), settings.VisualQuality))
            {
                settings.VisualQuality = CeleTechShuttleModSettings.DefaultVisualQuality;
            }

            if (!Enum.IsDefined(typeof(ShuttleHeaderMeterStyle), settings.HeaderMeterStyle))
            {
                settings.HeaderMeterStyle = CeleTechShuttleModSettings.DefaultHeaderMeterStyle;
            }

            if (!Enum.IsDefined(typeof(ShuttleUIRefreshPreset), settings.UIRefreshPreset))
            {
                settings.UIRefreshPreset = CeleTechShuttleModSettings.DefaultUIRefreshPreset;
            }

            if (!Enum.IsDefined(typeof(ShuttleVisualQuality), settings.LastCustomVisualQuality))
            {
                settings.LastCustomVisualQuality = CeleTechShuttleModSettings.DefaultVisualQuality;
            }

            if (!Enum.IsDefined(typeof(ShuttleHeaderMeterStyle), settings.LastCustomHeaderMeterStyle))
            {
                settings.LastCustomHeaderMeterStyle = CeleTechShuttleModSettings.DefaultHeaderMeterStyle;
            }

            if (!Enum.IsDefined(typeof(ShuttleUIRefreshPreset), settings.LastCustomUIRefreshPreset))
            {
                settings.LastCustomUIRefreshPreset = CeleTechShuttleModSettings.DefaultUIRefreshPreset;
            }

            settings.ControlRefreshTicksOverride = Mathf.Clamp(settings.ControlRefreshTicksOverride, 1, 120);
            settings.HeavyRefreshTicksOverride = Mathf.Clamp(settings.HeavyRefreshTicksOverride, 5, 300);
            if (settings.HeavyRefreshTicksOverride < settings.ControlRefreshTicksOverride)
            {
                settings.HeavyRefreshTicksOverride = settings.ControlRefreshTicksOverride;
            }

            settings.LastCustomControlRefreshTicksOverride = Mathf.Clamp(settings.LastCustomControlRefreshTicksOverride, 1, 120);
            settings.LastCustomHeavyRefreshTicksOverride = Mathf.Clamp(settings.LastCustomHeavyRefreshTicksOverride, 5, 300);
            if (settings.LastCustomHeavyRefreshTicksOverride < settings.LastCustomControlRefreshTicksOverride)
            {
                settings.LastCustomHeavyRefreshTicksOverride = settings.LastCustomControlRefreshTicksOverride;
            }

            if (settings.CombatTuning == null)
            {
                settings.CombatTuning = new ShuttleCombatTuningSettings();
            }

            if (settings.Other == null)
            {
                settings.Other = new ShuttleOtherSettings();
            }

            if (settings.CompletedShuttleControlTutorialPages == null)
            {
                settings.CompletedShuttleControlTutorialPages = new List<string>();
            }

            SanitizeStringList(settings.CompletedShuttleControlTutorialPages);
            if (settings.CompletedShuttleControlContextTutorials == null)
            {
                settings.CompletedShuttleControlContextTutorials = new List<string>();
            }

            SanitizeStringList(settings.CompletedShuttleControlContextTutorials);

            if (!settings.EnablePerformanceSummaryLogs)
            {
                settings.EnableDetailedPerformanceBreakdownLogs = false;
            }

            if (!settings.LastCustomEnablePerformanceSummaryLogs)
            {
                settings.LastCustomEnableDetailedPerformanceBreakdownLogs = false;
            }

            ShuttleCombatTuningSanitizer.Sanitize(settings.CombatTuning);
            settings.Other.Sanitize();
        }

        private static void SanitizeStringList(List<string> values)
        {
            if (values == null)
            {
                return;
            }

            HashSet<string> seen = new HashSet<string>();
            for (int i = values.Count - 1; i >= 0; i--)
            {
                string value = values[i];
                if (string.IsNullOrEmpty(value) || seen.Contains(value))
                {
                    values.RemoveAt(i);
                    continue;
                }

                seen.Add(value);
            }
        }
    }
}
