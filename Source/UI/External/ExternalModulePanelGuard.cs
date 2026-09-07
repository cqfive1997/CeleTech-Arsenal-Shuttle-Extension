using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.API.UI;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.External
{
    internal static class ExternalModulePanelGuard
    {
        private const string LogPrefix = "[CeleTech ShuttleExtension] UI ";
        private const int ExceptionDisableThreshold = 3;

        private static readonly Dictionary<string, int> exceptionCounts =
            new Dictionary<string, int>(System.StringComparer.Ordinal);
        private static readonly Dictionary<string, string> disabledReasons =
            new Dictionary<string, string>(System.StringComparer.Ordinal);

        internal static bool IsDisabled(ExternalModulePanelRegistration registration)
        {
            return registration != null &&
                !string.IsNullOrEmpty(registration.FullPanelKey) &&
                disabledReasons.ContainsKey(registration.FullPanelKey);
        }

        internal static string GetDisabledReason(ExternalModulePanelRegistration registration)
        {
            if (registration == null || string.IsNullOrEmpty(registration.FullPanelKey))
            {
                return null;
            }

            string reason;
            return disabledReasons.TryGetValue(registration.FullPanelKey, out reason)
                ? reason
                : null;
        }

        internal static void ClearSessionState()
        {
            exceptionCounts.Clear();
            disabledReasons.Clear();
        }

        internal static bool SafeCanShow(
            ExternalModulePanelRegistration registration,
            ShuttleExternalModulePanelContext context)
        {
            if (registration == null || registration.Provider == null || IsDisabled(registration))
            {
                return false;
            }

            try
            {
                return registration.Provider.CanShow(context);
            }
            catch (System.Exception exception)
            {
                RecordException("CanShow", registration, context, exception);
                return false;
            }
        }

        internal static float SafeGetPreferredHeight(
            ExternalModulePanelRegistration registration,
            ShuttleExternalModulePanelContext context,
            float width,
            float fallbackHeight)
        {
            if (registration == null || registration.Provider == null || IsDisabled(registration))
            {
                return fallbackHeight;
            }

            try
            {
                float height = registration.Provider.GetPreferredHeight(context, width);
                if (float.IsNaN(height) || float.IsInfinity(height) || height <= 0f)
                {
                    return fallbackHeight;
                }

                return Mathf.Clamp(height, 120f, 2000f);
            }
            catch (System.Exception exception)
            {
                RecordException("GetPreferredHeight", registration, context, exception);
                return fallbackHeight;
            }
        }

        internal static void SafeDrawPanel(
            ExternalModulePanelRegistration registration,
            Rect rect,
            ShuttleExternalModulePanelContext context)
        {
            if (registration == null || registration.Provider == null || IsDisabled(registration))
            {
                return;
            }

            Color oldColor = GUI.color;
            TextAnchor oldAnchor = Text.Anchor;
            GameFont oldFont = Text.Font;
            Matrix4x4 oldMatrix = GUI.matrix;
            bool oldWordWrap = Text.WordWrap;
            try
            {
                registration.Provider.DrawPanel(rect, context);
            }
            catch (System.Exception exception)
            {
                RecordException("DrawPanel", registration, context, exception);
            }
            finally
            {
                GUI.color = oldColor;
                Text.Anchor = oldAnchor;
                Text.Font = oldFont;
                GUI.matrix = oldMatrix;
                Text.WordWrap = oldWordWrap;
            }
        }

        internal static void SafeCollectCommands(
            ExternalModulePanelRegistration registration,
            ShuttleExternalModulePanelContext context,
            IShuttleExternalModulePanelCommandSink sink)
        {
            if (registration == null ||
                registration.Provider == null ||
                sink == null ||
                IsDisabled(registration))
            {
                return;
            }

            try
            {
                registration.Provider.CollectCommands(context, sink);
            }
            catch (System.Exception exception)
            {
                RecordException("CollectCommands", registration, context, exception);
            }
        }

        private static void RecordException(
            string phase,
            ExternalModulePanelRegistration registration,
            ShuttleExternalModulePanelContext context,
            System.Exception exception)
        {
            string panelKey = registration != null ? registration.FullPanelKey : "<null>";
            string runtimeKey = registration != null ? registration.RuntimeSystemKey : "<null>";
            string moduleInstanceId = context != null && context.Module != null
                ? context.Module.ModuleInstanceId
                : "<null>";

            int count = 0;
            exceptionCounts.TryGetValue(panelKey, out count);
            count++;
            exceptionCounts[panelKey] = count;

            if (count <= ExceptionDisableThreshold || Prefs.DevMode)
            {
                Log.Error(LogPrefix + "panel provider " + phase + " exception. PanelKey=" +
                    panelKey + ", RuntimeSystemKey=" + runtimeKey +
                    ", ModuleInstanceId=" + moduleInstanceId +
                    ", Count=" + count + ". Exception: " + exception);
            }

            if (count >= ExceptionDisableThreshold && !disabledReasons.ContainsKey(panelKey))
            {
                string reason = ShuttleUIText.Tr(
                    "CT_Shuttle_ExternalRuntime_ProviderDisabledAfterExceptions",
                    count,
                    phase);
                disabledReasons[panelKey] = reason;
                Log.Warning(LogPrefix + "panel provider disabled for this session. PanelKey=" +
                    panelKey + ", RuntimeSystemKey=" + runtimeKey +
                    ", ModuleInstanceId=" + moduleInstanceId + ". " + reason);
            }
        }
    }
}
