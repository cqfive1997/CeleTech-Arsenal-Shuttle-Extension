using CeleTech.ShuttleExtension.ModularShuttle.API.UI;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace MyCoolMod.ShuttleRuntimeSample
{
    public sealed class AlienScannerPanelProvider : ShuttleExternalModulePanelProviderBase
    {
        public override string RuntimeSystemKey
        {
            get
            {
                return "my.cool.mod/alien-scanner-runtime";
            }
        }

        public override bool CanShow(ShuttleExternalModulePanelContext context)
        {
            return context != null &&
                context.Module != null &&
                context.RuntimeSystemKey == this.RuntimeSystemKey;
        }

        public override float GetPreferredHeight(
            ShuttleExternalModulePanelContext context,
            float width)
        {
            return 172f;
        }

        public override void DrawPanel(
            Rect rect,
            ShuttleExternalModulePanelContext context)
        {
            if (context == null)
            {
                return;
            }

            int scanCount;
            int nextScanTick;
            bool installed;
            bool launchNotified;
            context.State.TryGetInt("scanCount", out scanCount);
            context.State.TryGetInt("nextScanTick", out nextScanTick);
            context.State.TryGetBool("installed", out installed);
            context.State.TryGetBool("launchNotified", out launchNotified);

            Text.Font = GameFont.Small;
            Widgets.Label(
                new Rect(rect.x, rect.y, rect.width, 24f),
                context.Module != null ? context.Module.Label : "Alien scanner");

            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(rect.x, rect.y + 32f, rect.width, 22f), "Runtime enabled: " + context.RuntimeEnabled);
            Widgets.Label(new Rect(rect.x, rect.y + 56f, rect.width, 22f), "Scan count: " + scanCount);
            Widgets.Label(new Rect(rect.x, rect.y + 80f, rect.width, 22f), "Next scan tick: " + nextScanTick);
            Widgets.Label(new Rect(rect.x, rect.y + 104f, rect.width, 22f), "Installed: " + installed);
            Widgets.Label(new Rect(rect.x, rect.y + 128f, rect.width, 22f), "Launch notified: " + launchNotified);

            // Panel contexts are read-only. Draw status here; declare buttons in
            // CollectCommands so the host can execute commands through the command boundary.
        }

        public override void CollectCommands(
            ShuttleExternalModulePanelContext context,
            IShuttleExternalModulePanelCommandSink sink)
        {
            if (context == null || sink == null)
            {
                return;
            }

            Dictionary<string, string> args = new Dictionary<string, string>();
            args["mode"] = "deep-scan";
            sink.AddCommand(new ShuttleExternalPanelCommandContribution(
                "set-deep-scan",
                "Deep Scan",
                "Switch scanner to deep scan mode.",
                "my.cool.mod/set-scan-mode",
                args,
                context.RuntimeEnabled,
                context.RuntimeEnabled ? null : "Runtime is disabled.",
                10));
        }
    }
}
