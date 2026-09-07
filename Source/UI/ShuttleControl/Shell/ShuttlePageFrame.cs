using UnityEngine;
using Verse;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Chrome;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell
{
    internal sealed class ShuttlePageFrame
    {
        private const int ErrorLogThrottleTicks = 250;
        private readonly ShuttleGlobalCommandBar commandBar = new ShuttleGlobalCommandBar();
        private string lastLoggedPageKey;
        private int lastLoggedTick = -999999;

        internal void Draw(
            Rect rect,
            IShuttleControlPageV3 page,
            ShuttlePageDrawContext context)
        {
            if (page == null)
            {
                return;
            }

            using (new ShuttleGUIStateScope())
            {
                try
                {
                    Rect pageRect = this.DrawGlobalChrome(rect, page, context);
                    page.Draw(pageRect, context);
                }
                catch (System.Exception exception)
                {
                    this.LogPageDrawException(page, exception);
                    this.DrawFallbackError(rect);
                }
            }
        }

        private Rect DrawGlobalChrome(
            Rect rect,
            IShuttleControlPageV3 page,
            ShuttlePageDrawContext context)
        {
            if (UsesIntegratedHeader(page))
            {
                return rect;
            }

            float commandHeight = Mathf.Min(
                ShuttleGlobalCommandBar.Height,
                Mathf.Max(0f, rect.height));
            Rect commandRect = new Rect(
                rect.x,
                rect.y,
                rect.width,
                commandHeight);

            this.commandBar.Draw(commandRect, context);

            float gap = commandHeight > 0f && rect.height > commandHeight
                ? ShuttleUIStyle.Gap
                : 0f;
            return new Rect(
                rect.x,
                rect.y + commandHeight + gap,
                rect.width,
                Mathf.Max(0f, rect.height - commandHeight - gap));
        }

        private static bool UsesIntegratedHeader(IShuttleControlPageV3 page)
        {
            return page != null &&
                (page.Page == ShuttleControlPageId.Main ||
                    page is IShuttleIntegratedHeaderPageV3);
        }

        private void LogPageDrawException(IShuttleControlPageV3 page, System.Exception exception)
        {
            string pageKey = page.Page.ToString();
            int currentTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            bool shouldLog = this.lastLoggedPageKey != pageKey ||
                currentTick - this.lastLoggedTick > ErrorLogThrottleTicks;

            if (!shouldLog)
            {
                return;
            }

            this.lastLoggedPageKey = pageKey;
            this.lastLoggedTick = currentTick;
            Log.Error(
                "[CeleTech ShuttleExtension] V3 page draw failed. Page=" +
                page.Page +
                ". Exception: " +
                exception);
        }

        private void DrawFallbackError(Rect rect)
        {
            ShuttleUILayout.DrawPanelBackground(rect);

            Rect messageRect = new Rect(
                rect.x + 16f,
                rect.y + 16f,
                Mathf.Max(0f, rect.width - 32f),
                60f);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = ShuttleUIStyle.RedStatusColor;
            Widgets.Label(
                messageRect,
                "Shuttle control page failed to draw. Check the RimWorld log for details.");
        }
    }
}
