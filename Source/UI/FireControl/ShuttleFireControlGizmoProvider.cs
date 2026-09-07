using System;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Defense;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Defense;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Icons;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.FireControl
{
    internal static class ShuttleFireControlGizmoProvider
    {
        internal static Gizmo_ShuttleFireControl Create(
            IShuttleWeaponBayReadPort readPort,
            IShuttleCommandExecutor commandExecutor,
            Action openDefensePage)
        {
            if (readPort == null || commandExecutor == null)
            {
                return null;
            }

            ShuttleFireControlGizmoModelBuilder modelBuilder =
                new ShuttleFireControlGizmoModelBuilder(readPort);
            ShuttleFireControlGizmoActionTargetBuilder targetBuilder =
                new ShuttleFireControlGizmoActionTargetBuilder();
            IShuttleDefenseWeaponGroupUIActions groupActions =
                new ShuttleDefenseWeaponGroupUIActions(commandExecutor);
            ShuttleFireControlGizmoTargeter targeter =
                new ShuttleFireControlGizmoTargeter(
                    modelBuilder,
                    targetBuilder,
                    groupActions);
            ShuttleControlIconRegistry icons = new ShuttleControlIconRegistry();

            ShuttleFireControlGizmoActions actions =
                new ShuttleFireControlGizmoActions(
                    modelBuilder,
                    targetBuilder,
                    targeter,
                    groupActions,
                    openDefensePage);

            return new Gizmo_ShuttleFireControl(
                modelBuilder,
                actions,
                new ShuttleFireControlGizmoButtonDrawer(),
                new ShuttleFireControlGizmoTextures(
                    icons.GetIcon("fire_control_open_all"),
                    icons.GetIcon("fire_control_hold_all"),
                    icons.GetIcon("fire_control_target_all"),
                    icons.GetIcon("fire_control_clear_all"),
                    icons.GetIcon("fire_control_mode"),
                    icons.GetIcon("fire_control_link"),
                    icons.GetIcon("fire_control_reload_all"),
                    icons.GetIcon("fire_control_open_defense")));
        }
    }
}
