using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Icons;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI
{
    internal sealed class ShuttleQuickActionGizmoProvider
    {
        private readonly ShuttleControlIconRegistry iconRegistry = new ShuttleControlIconRegistry();

        internal IEnumerable<Gizmo> GetGizmos(
            Action openLoadCargoWindow,
            Action openCargoUnloadWindow,
            Action openCrewUnloadDialog,
            Action openLaunchFlow)
        {
            yield return this.BuildAction(
                "CT_Shuttle_QuickAction_QuickLoad",
                "CT_Shuttle_QuickAction_QuickLoadTooltip",
                this.iconRegistry.GetIcon("load_cargo"),
                openLoadCargoWindow);
            yield return this.BuildOpenCargoUnloadCommand(openCargoUnloadWindow);
            yield return this.BuildAction(
                "CT_Shuttle_QuickAction_UnloadCrew",
                "CT_Shuttle_QuickAction_UnloadCrewTooltip",
                this.iconRegistry.GetIcon("passenger_unloading"),
                openCrewUnloadDialog);
            yield return this.BuildAction(
                "CT_Shuttle_QuickAction_Launch",
                "CT_Shuttle_QuickAction_LaunchTooltip",
                this.iconRegistry.GetIcon("launch"),
                openLaunchFlow);
        }

        private Command_Action BuildOpenCargoUnloadCommand(Action action)
        {
            return this.BuildAction(
                "CT_Shuttle_QuickAction_Cargo",
                "CT_Shuttle_QuickAction_CargoTooltip",
                this.iconRegistry.GetIcon("cargo_shortcut"),
                action);
        }

        private Command_Action BuildAction(
            string labelKey,
            string tooltipKey,
            Texture2D icon,
            Action action)
        {
            Command_Action command = new Command_Action
            {
                defaultLabel = labelKey.Translate().ToString(),
                defaultDesc = tooltipKey.Translate().ToString(),
                icon = icon,
                action = delegate
                {
                    if (action != null)
                    {
                        action();
                    }
                }
            };

            if (action == null)
            {
                command.Disable("CT_Shuttle_Error_CoreUnavailable".Translate().ToString());
            }

            return command;
        }
    }
}
