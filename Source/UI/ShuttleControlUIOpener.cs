using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Crew;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.ModalLaunchers;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI
{
    internal static class ShuttleControlUIOpener
    {
        internal static void OpenMainControlWindow(
            Thing host,
            ShuttleController controller,
            IShuttleControlBootAnimationPort bootAnimationPort)
        {
            OpenControlWindow(
                host,
                controller,
                bootAnimationPort,
                ShuttleControlPageId.Main);
        }

        internal static void OpenCargoControlPage(
            Thing host,
            ShuttleController controller,
            IShuttleControlBootAnimationPort bootAnimationPort)
        {
            OpenControlWindow(
                host,
                controller,
                bootAnimationPort,
                ShuttleControlPageId.Cargo);
        }

        internal static void OpenDefenseControlPage(
            Thing host,
            ShuttleController controller,
            IShuttleControlBootAnimationPort bootAnimationPort)
        {
            OpenControlWindow(
                host,
                controller,
                bootAnimationPort,
                ShuttleControlPageId.Defense);
        }

        internal static void OpenQuickLoadCargoWindow(ShuttleController controller)
        {
            ShuttleControllerUIPort uiPort = CreateUIPort(controller);
            if (uiPort == null)
            {
                ReportUIOpenFailure(null, "controller is unavailable");
                return;
            }

            ShuttleCargoLoadModalLauncher launcher =
                new ShuttleCargoLoadModalLauncher(
                    uiPort,
                    uiPort,
                    delegate { },
                    delegate(bool value) { });
            launcher.OpenLoadCargoWindow(new ShuttleCargoLoadUIActions(uiPort));
        }

        internal static void OpenQuickCargoUnloadWindow(ShuttleController controller)
        {
            ShuttleControllerUIPort uiPort = CreateUIPort(controller);
            if (uiPort == null)
            {
                ReportUIOpenFailure(null, "controller is unavailable");
                return;
            }

            ShuttleCargoUnloadModalLauncher launcher =
                new ShuttleCargoUnloadModalLauncher(
                    uiPort,
                    uiPort,
                    uiPort,
                    delegate { },
                    delegate(bool value) { });
            launcher.Open();
        }

        internal static void OpenQuickCrewUnloadDialog(ShuttleController controller)
        {
            ShuttleControllerUIPort uiPort = CreateUIPort(controller);
            if (uiPort == null)
            {
                ReportUIOpenFailure(null, "controller is unavailable");
                return;
            }

            ShuttleCrewUnloadDialogLauncher launcher = new ShuttleCrewUnloadDialogLauncher();
            launcher.OpenCrewFromPorts(uiPort, uiPort, uiPort);
        }

        internal static void OpenQuickLaunchFlow(ShuttleController controller)
        {
            ShuttleControllerUIPort uiPort = CreateUIPort(controller);
            if (uiPort == null)
            {
                ReportUIOpenFailure(null, "controller is unavailable");
                return;
            }

            ShuttleLaunchModalLauncher launcher =
                new ShuttleLaunchModalLauncher(
                    uiPort,
                    delegate { },
                    delegate { });
            launcher.OpenLaunchTargeting();
        }

        internal static void CloseOpenControlWindowForHost(Thing host)
        {
            Dialog_ShuttleControl dialog = FindOpenControlDialogForHost(host);
            if (dialog != null)
            {
                dialog.Close(false);
            }
        }

        internal static Dialog_ShuttleControl FindOpenControlDialogForHost(Thing host)
        {
            if (host == null || Find.WindowStack == null)
            {
                return null;
            }

            IList<Window> windows = Find.WindowStack.Windows;
            if (windows == null)
            {
                return null;
            }

            for (int i = windows.Count - 1; i >= 0; i--)
            {
                Dialog_ShuttleControl dialog = windows[i] as Dialog_ShuttleControl;
                if (dialog != null && dialog.IsForHost(host))
                {
                    return dialog;
                }
            }

            return null;
        }

        private static void OpenControlWindow(
            Thing host,
            ShuttleController controller,
            IShuttleControlBootAnimationPort bootAnimationPort,
            ShuttleControlPageId initialPage)
        {
            Dialog_ShuttleControl existingDialog =
                FindOpenControlDialogForHost(host);
            if (existingDialog != null)
            {
                existingDialog.TrySwitchToPage(initialPage);
                return;
            }

            ShuttleControllerUIPort uiPort = CreateUIPort(controller);
            if (uiPort == null)
            {
                ReportUIOpenFailure(host, "controller is unavailable");
                return;
            }

            try
            {
                Find.WindowStack.Add(new Dialog_ShuttleControl(
                    uiPort,
                    uiPort,
                    uiPort,
                    uiPort,
                    uiPort,
                    uiPort,
                    bootAnimationPort,
                    host,
                    initialPage));
            }
            catch (Exception exception)
            {
                ReportUIOpenFailure(
                    host,
                    "V3 dialog composition failed: " + exception);
            }
        }

        private static ShuttleControllerUIPort CreateUIPort(ShuttleController controller)
        {
            return controller != null
                ? new ShuttleControllerUIPort(controller)
                : null;
        }

        private static void ReportUIOpenFailure(Thing host, string reason)
        {
            string logMessage = "[CeleTech Shuttle] Shuttle control UI is unavailable";
            if (!string.IsNullOrEmpty(reason))
            {
                logMessage += ": " + reason;
            }

            Log.Error(logMessage);
            string playerMessage =
                "CT_Shuttle_UI_ControlUnavailable".Translate().ToString();
            if (host != null)
            {
                Messages.Message(
                    playerMessage,
                    host,
                    MessageTypeDefOf.RejectInput,
                    false);
                return;
            }

            Messages.Message(
                playerMessage,
                MessageTypeDefOf.RejectInput,
                false);
        }
    }
}
