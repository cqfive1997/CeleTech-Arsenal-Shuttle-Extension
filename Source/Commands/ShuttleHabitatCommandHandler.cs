using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Profile;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Commands
{
    /// <summary>
    /// Handles shuttle Habitat occupant commands through a narrow occupancy service.
    /// </summary>
    internal sealed class ShuttleHabitatCommandHandler : IShuttleCommandHandler
    {
        private readonly ShuttleHabitatOccupancyService occupancyService = new ShuttleHabitatOccupancyService();

        public bool CanHandle(IShuttleCommand command)
        {
            return command is EjectHabitatOccupantsCommand ||
                command is EjectHabitatOccupantCommand ||
                command is SetHabitatJoyKindsCommand;
        }

        public ShuttleCommandResult Execute(IShuttleCommand command, ShuttleCommandContext context)
        {
            if (context == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_ContextUnavailable".Translate().ToString());
            }

            EjectHabitatOccupantsCommand ejectHabitatOccupants = command as EjectHabitatOccupantsCommand;
            if (ejectHabitatOccupants != null)
            {
                return this.ExecuteEjectHabitatOccupants(context, ejectHabitatOccupants);
            }

            EjectHabitatOccupantCommand ejectHabitatOccupant = command as EjectHabitatOccupantCommand;
            if (ejectHabitatOccupant != null)
            {
                return this.ExecuteEjectHabitatOccupant(context, ejectHabitatOccupant);
            }

            SetHabitatJoyKindsCommand setJoyKinds = command as SetHabitatJoyKindsCommand;
            if (setJoyKinds != null)
            {
                return this.ExecuteSetHabitatJoyKinds(context, setJoyKinds);
            }

            return ShuttleCommandResult.Failed("CT_Shuttle_Command_Unsupported".Translate(command.CommandID).ToString());
        }

        private ShuttleCommandResult ExecuteEjectHabitatOccupants(
            ShuttleCommandContext context,
            EjectHabitatOccupantsCommand command)
        {
            string failureReason;
            bool hadOccupants;
            if (!this.occupancyService.TryEjectAllHabitatOccupants(
                context.Host,
                out hadOccupants,
                out failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            return hadOccupants
                ? ShuttleCommandResult.Succeeded("CT_Shuttle_Command_EjectHabitatOccupants_Label".Translate().ToString())
                : ShuttleCommandResult.Succeeded("CT_Shuttle_Command_EjectHabitatOccupants_NoOccupants".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteEjectHabitatOccupant(
            ShuttleCommandContext context,
            EjectHabitatOccupantCommand command)
        {
            if (command == null || command.PawnThingID <= 0)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_EjectHabitatOccupant_Failed".Translate().ToString());
            }

            string failureReason;
            bool hadOccupant;
            if (!this.occupancyService.TryEjectHabitatOccupant(
                context.Host,
                command.PawnThingID,
                out hadOccupant,
                out failureReason))
            {
                return ShuttleCommandResult.Failed(failureReason);
            }

            return hadOccupant
                ? ShuttleCommandResult.Succeeded("CT_Shuttle_Command_EjectHabitatOccupant_Label".Translate().ToString())
                : ShuttleCommandResult.Succeeded("CT_Shuttle_Command_EjectHabitatOccupant_NoOccupant".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteSetHabitatJoyKinds(
            ShuttleCommandContext context,
            SetHabitatJoyKindsCommand command)
        {
            if (command == null || string.IsNullOrEmpty(command.ModuleInstanceID))
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_SetHabitatJoyKindsMissing".Translate().ToString());
            }

            ShuttleAssemblyState assemblyState = context.AssemblyState;
            if (assemblyState == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_AssemblyUnavailable".Translate().ToString());
            }

            ShuttleModule module = assemblyState.GetModule(command.ModuleInstanceID);
            if (module == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_HabitatJoyModuleMissing".Translate().ToString());
            }

            if (!module.IsEnabled)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_HabitatModuleDisabled".Translate().ToString());
            }

            ShuttleHabitatModuleDef habitatDef = module.GetModuleDef<ShuttleHabitatModuleDef>();
            if (habitatDef == null || !habitatDef.habitatSupportsJoy)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_HabitatJoyUnsupported".Translate().ToString());
            }

            List<JoyKindDef> requestedJoyKinds = this.ResolveJoyKinds(command.JoyKindDefNames);
            int requestedAllowedCount = this.CountAllowedJoyKinds(
                requestedJoyKinds,
                habitatDef.allowedJoyKinds);
            if (requestedAllowedCount > habitatDef.habitatJoyKindCapacity)
            {
                return ShuttleCommandResult.Failed(
                    "CT_Shuttle_HabitatJoyConfig_CapacityReached".Translate().ToString());
            }

            List<JoyKindDef> sanitizedJoyKinds = HabitatJoySelectionUtility.SanitizeSelection(
                requestedJoyKinds,
                habitatDef.allowedJoyKinds,
                habitatDef.habitatJoyKindCapacity);
            if (sanitizedJoyKinds.Count == 0)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_HabitatJoyKindsEmpty".Translate().ToString());
            }

            IReadOnlyList<JoyKindDef> defaultJoyKinds =
                HabitatJoySelectionUtility.GetSelectedJoyKinds(null, habitatDef);

            if (this.AreSameJoyKinds(sanitizedJoyKinds, defaultJoyKinds))
            {
                module.RemoveInstanceConfig(HabitatJoySelectionUtility.HabitatJoyKindsConfigKey);
            }
            else
            {
                module.SetInstanceConfig(
                    HabitatJoySelectionUtility.HabitatJoyKindsConfigKey,
                    HabitatJoySelectionUtility.SerializeSelection(sanitizedJoyKinds));
            }

            context.MarkProfileDirty(ProfileDirtyReason.AssemblyChanged);
            return ShuttleCommandResult.Succeeded(
                "CT_Shuttle_Command_HabitatJoyKindsUpdated".Translate().ToString(),
                true);
        }

        private List<JoyKindDef> ResolveJoyKinds(IReadOnlyList<string> joyKindDefNames)
        {
            List<JoyKindDef> joyKinds = new List<JoyKindDef>();
            if (joyKindDefNames == null)
            {
                return joyKinds;
            }

            for (int i = 0; i < joyKindDefNames.Count; i++)
            {
                string defName = joyKindDefNames[i] != null ? joyKindDefNames[i].Trim() : null;
                if (string.IsNullOrEmpty(defName))
                {
                    continue;
                }

                JoyKindDef joyKind = DefDatabase<JoyKindDef>.GetNamedSilentFail(defName);
                if (joyKind != null && !joyKinds.Contains(joyKind))
                {
                    joyKinds.Add(joyKind);
                }
            }

            return joyKinds;
        }

        private bool AreSameJoyKinds(
            IReadOnlyList<JoyKindDef> first,
            IReadOnlyList<JoyKindDef> second)
        {
            int firstCount = this.CountNonNullJoyKinds(first);
            int secondCount = this.CountNonNullJoyKinds(second);
            if (firstCount != secondCount)
            {
                return false;
            }

            if (first == null)
            {
                return secondCount == 0;
            }

            for (int i = 0; i < first.Count; i++)
            {
                JoyKindDef joyKind = first[i];
                if (joyKind != null && !this.ContainsJoyKind(second, joyKind))
                {
                    return false;
                }
            }

            return true;
        }

        private int CountNonNullJoyKinds(IReadOnlyList<JoyKindDef> joyKinds)
        {
            if (joyKinds == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < joyKinds.Count; i++)
            {
                if (joyKinds[i] != null)
                {
                    count++;
                }
            }

            return count;
        }

        private bool ContainsJoyKind(IReadOnlyList<JoyKindDef> joyKinds, JoyKindDef joyKind)
        {
            if (joyKinds == null || joyKind == null)
            {
                return false;
            }

            for (int i = 0; i < joyKinds.Count; i++)
            {
                if (joyKinds[i] == joyKind)
                {
                    return true;
                }
            }

            return false;
        }

        private int CountAllowedJoyKinds(
            IReadOnlyList<JoyKindDef> requestedJoyKinds,
            IReadOnlyList<JoyKindDef> allowedJoyKinds)
        {
            if (requestedJoyKinds == null || allowedJoyKinds == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < requestedJoyKinds.Count; i++)
            {
                JoyKindDef joyKind = requestedJoyKinds[i];
                if (joyKind != null && this.ContainsJoyKind(allowedJoyKinds, joyKind))
                {
                    count++;
                }
            }

            return count;
        }
    }
}
