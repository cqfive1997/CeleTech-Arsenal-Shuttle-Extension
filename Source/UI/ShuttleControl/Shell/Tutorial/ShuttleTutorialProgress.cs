using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Tutorial
{
    internal static class ShuttleTutorialProgress
    {
        private static int resetVersion;

        internal static int ResetVersion
        {
            get { return resetVersion; }
        }

        internal static bool TutorialsEnabled
        {
            get { return CeleTechShuttleMod.Settings.ShuttleControlTutorialsEnabled; }
        }

        internal static bool AutoStartEnabled
        {
            get { return CeleTechShuttleMod.Settings.ShuttleControlTutorialsAutoStart; }
        }

        internal static bool HasCompletedPage(ShuttleControlPageId page)
        {
            return HasCompletedPageKey(GetPageKey(page));
        }

        internal static bool HasCompletedPageKey(string pageKey)
        {
            if (string.IsNullOrEmpty(pageKey))
            {
                return false;
            }

            List<string> pages = GetCompletedPages();
            for (int i = 0; i < pages.Count; i++)
            {
                if (pages[i] == pageKey)
                {
                    return true;
                }
            }

            return false;
        }

        internal static void MarkPageCompleted(ShuttleControlPageId page)
        {
            MarkPageCompletedKey(GetPageKey(page));
        }

        internal static void MarkPageCompletedKey(string pageKey)
        {
            if (string.IsNullOrEmpty(pageKey))
            {
                return;
            }

            List<string> pages = GetCompletedPages();
            for (int i = 0; i < pages.Count; i++)
            {
                if (pages[i] == pageKey)
                {
                    return;
                }
            }

            pages.Add(pageKey);
            CeleTechShuttleMod.SaveSettingsSafe();
        }

        internal static bool HasCompletedContext(string contextKey)
        {
            if (string.IsNullOrEmpty(contextKey))
            {
                return false;
            }

            List<string> contexts = GetCompletedContexts();
            for (int i = 0; i < contexts.Count; i++)
            {
                if (contexts[i] == contextKey)
                {
                    return true;
                }
            }

            return false;
        }

        internal static void MarkContextCompleted(string contextKey)
        {
            if (string.IsNullOrEmpty(contextKey))
            {
                return;
            }

            List<string> contexts = GetCompletedContexts();
            for (int i = 0; i < contexts.Count; i++)
            {
                if (contexts[i] == contextKey)
                {
                    return;
                }
            }

            contexts.Add(contextKey);
            CeleTechShuttleMod.SaveSettingsSafe();
        }

        internal static void ResetAll()
        {
            CeleTechShuttleModSettings settings = CeleTechShuttleMod.Settings;
            settings.ShuttleControlTutorialsEnabled = true;
            settings.ShuttleControlTutorialsAutoStart = true;
            GetCompletedPages().Clear();
            GetCompletedContexts().Clear();
            resetVersion++;
            CeleTechShuttleMod.SaveSettingsSafe();
        }

        internal static string GetPageKey(ShuttleControlPageId page)
        {
            return page.ToString();
        }

        private static List<string> GetCompletedPages()
        {
            CeleTechShuttleModSettings settings = CeleTechShuttleMod.Settings;
            if (settings.CompletedShuttleControlTutorialPages == null)
            {
                settings.CompletedShuttleControlTutorialPages = new List<string>();
            }

            return settings.CompletedShuttleControlTutorialPages;
        }

        private static List<string> GetCompletedContexts()
        {
            CeleTechShuttleModSettings settings = CeleTechShuttleMod.Settings;
            if (settings.CompletedShuttleControlContextTutorials == null)
            {
                settings.CompletedShuttleControlContextTutorials = new List<string>();
            }

            return settings.CompletedShuttleControlContextTutorials;
        }
    }
}
