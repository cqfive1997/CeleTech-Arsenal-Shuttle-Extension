using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Shield;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Shield.Surface;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Commands
{
    /// <summary>
    /// Handles shield runtime-setting commands. These commands mutate only module-owned
    /// runtime payloads; they do not dirty or rebuild the shuttle profile.
    /// </summary>
    internal sealed class ShuttleShieldCommandHandler : IShuttleCommandHandler
    {
        public bool CanHandle(IShuttleCommand command)
        {
            return command is SetShuttleShieldRadiusCommand ||
                command is SetShuttleSurfaceShieldRechargeSpeedCommand;
        }

        public ShuttleCommandResult Execute(IShuttleCommand command, ShuttleCommandContext context)
        {
            if (context == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ContextUnavailable".Translate().ToString());
            }

            SetShuttleShieldRadiusCommand setRadius = command as SetShuttleShieldRadiusCommand;
            if (setRadius != null)
            {
                return this.ExecuteSetRadius(context, setRadius);
            }

            SetShuttleSurfaceShieldRechargeSpeedCommand setRechargeSpeed =
                command as SetShuttleSurfaceShieldRechargeSpeedCommand;
            if (setRechargeSpeed != null)
            {
                return this.ExecuteSetSurfaceShieldRechargeSpeed(context, setRechargeSpeed);
            }

            return ShuttleCommandResult.Failed("CT_Shuttle_Command_UnsupportedShield".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteSetRadius(
            ShuttleCommandContext context,
            SetShuttleShieldRadiusCommand command)
        {
            if (command == null || string.IsNullOrEmpty(command.ModuleInstanceID))
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ShieldRadiusMissingModuleID".Translate().ToString());
            }

            ShuttleAssemblyState assemblyState = context.AssemblyState;
            if (assemblyState == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ShuttleAssemblyStateUnavailable".Translate().ToString());
            }

            ShuttleModule module = assemblyState.GetModule(command.ModuleInstanceID);
            if (module == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ShieldModuleNotInstalled".Translate().ToString());
            }

            if (!module.IsEnabled)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ShieldModuleDisabled".Translate().ToString());
            }

            ShuttleShieldModuleDef shieldDef = module.ModuleDef as ShuttleShieldModuleDef;
            if (shieldDef == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ShieldTargetNotShieldModule".Translate().ToString());
            }

            if (!shieldDef.supportsRadiusControl)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ShieldRadiusUnsupported".Translate().ToString());
            }

            ShuttleRuntimeState runtimeState = context.GetRuntimeState();
            if (runtimeState == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ShuttleRuntimeStateUnavailable".Translate().ToString());
            }

            ShuttleShieldRuntimeState state = this.GetOrCreateShieldState(runtimeState, module);
            if (state == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ShieldRuntimeStateUnavailable".Translate().ToString());
            }

            float clampedRadius = ShuttleShieldRuntimeUtility.ClampRadiusOrDefault(
                command.SelectedRadius,
                shieldDef);
            state.SetSelectedRadius(clampedRadius);

            // Radius is common runtime player configuration. It does not alter AssemblyState or Profile.
            return ShuttleCommandResult.Succeeded("CT_Shuttle_Command_ShieldRadiusSet".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteSetSurfaceShieldRechargeSpeed(
            ShuttleCommandContext context,
            SetShuttleSurfaceShieldRechargeSpeedCommand command)
        {
            if (context == null || command == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ContextUnavailable".Translate().ToString());
            }

            float multiplier = ShuttleSurfaceShieldRuntimeState.SanitizeRechargeSpeedMultiplier(
                command.RechargeSpeedMultiplier);
            if (!context.TrySetActiveSurfaceShieldRechargeSpeed(multiplier))
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_Command_SurfaceShieldRechargeSpeedUnavailable".Translate().ToString());
            }

            return ShuttleCommandResult.Succeeded(
                "CT_Shuttle_Command_SurfaceShieldRechargeSpeedSet".Translate(multiplier.ToString("0.##")).ToString());
        }

        private ShuttleShieldRuntimeState GetOrCreateShieldState(
            ShuttleRuntimeState runtimeState,
            ShuttleModule module)
        {
            if (runtimeState == null || module == null)
            {
                return null;
            }

            runtimeState.EnsureInitialized();
            IShuttleModuleRuntimeState state = runtimeState.Modules.GetOrCreateState(
                module.ModuleInstanceID,
                ShuttleShieldRuntimeSystem.ShieldRuntimeSystemKey,
                this.CreateShieldState);

            return state as ShuttleShieldRuntimeState;
        }

        private IShuttleModuleRuntimeState CreateShieldState()
        {
            return new ShuttleShieldRuntimeState();
        }
    }
}
