using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Chrome;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Chrome
{
    internal sealed class ShuttleGlobalCommandSpecBuilder
    {
        internal IList<ShuttleHeaderCommandSpec> Build(ShuttlePageDrawContext context)
        {
            ShuttleGlobalChromeActions actions =
                context != null ? context.GlobalChromeActions : null;

            return new List<ShuttleHeaderCommandSpec>
            {
                this.BuildCommandSpec(
                    context,
                    "CT_Shuttle_UI_LoadCargo",
                    "CT_Shuttle_Header_LoadCargoTooltip",
                    "load_cargo",
                    actions != null ? actions.OpenLoadCargo : null),
                this.BuildCommandSpec(
                    context,
                    "CT_Shuttle_UI_UnloadCargo",
                    "CT_Shuttle_Header_UnloadCargoTooltip",
                    "cargo_shortcut",
                    actions != null ? actions.OpenCargoUnload : null),
                this.BuildCommandSpec(
                    context,
                    "CT_Shuttle_UI_PageSwitch",
                    "CT_Shuttle_Header_PageSwitchTooltip",
                    "view_switch",
                    actions != null ? actions.OpenPageMenu : null),
                this.BuildCommandSpec(
                    context,
                    "CT_Shuttle_UI_Launch",
                    "CT_Shuttle_Header_LaunchTooltip",
                    "launch",
                    actions != null ? actions.OpenLaunch : null)
            };
        }

        private ShuttleHeaderCommandSpec BuildCommandSpec(
            ShuttlePageDrawContext context,
            string labelKey,
            string tooltipKey,
            string iconKey,
            Action action)
        {
            return new ShuttleHeaderCommandSpec(
                ShuttleUIText.Tr(labelKey),
                ShuttleUIText.Tr(tooltipKey),
                this.GetIcon(context, iconKey),
                action,
                action != null);
        }

        private Texture2D GetIcon(ShuttlePageDrawContext context, string key)
        {
            return context != null &&
                context.Services != null &&
                context.Services.Icons != null
                    ? context.Services.Icons.GetIcon(key)
                    : null;
        }
    }
}
