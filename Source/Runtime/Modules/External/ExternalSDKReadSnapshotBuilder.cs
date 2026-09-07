using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.API.Runtime;
using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Profile;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyConstruction;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal static class ExternalSDKReadSnapshotBuilder
    {
        internal static ShuttleExternalReadSnapshot Build(ShuttleController controller)
        {
            if (controller == null)
            {
                return Unavailable("shuttle controller is unavailable");
            }

            ShuttleProfile profile = controller.CurrentProfile;
            ShuttleRuntimeState runtimeState = controller.GetLaunchRuntimeState();
            ShuttleAssemblyState assemblyState = controller.AssemblyState;
            int ticksGame = ShuttleTickUtility.TicksGameOrMinusOne();
            ShuttleExternalHostSnapshot host = BuildHostSnapshot(controller);
            ShuttleExternalIntegrationHealthReport health =
                ExternalSDKHealthReportBuilder.Build(assemblyState, runtimeState);

            return new ShuttleExternalReadSnapshot(
                true,
                null,
                profile != null ? profile.Revision : controller.ProfileRevision,
                ticksGame,
                host,
                BuildAssemblySnapshot(assemblyState, runtimeState, host),
                BuildProfileSnapshot(profile),
                BuildRuntimeSummary(health),
                BuildPowerSnapshot(profile, runtimeState),
                BuildCargoSummary(controller, profile),
                BuildLaunchSummary(profile, runtimeState, ticksGame),
                health);
        }

        internal static ShuttleExternalReadSnapshot Unavailable(string reason)
        {
            return new ShuttleExternalReadSnapshot(
                false,
                reason,
                0,
                ShuttleTickUtility.TicksGameOrMinusOne(),
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                ExternalSDKHealthReportBuilder.Build(null, null));
        }

        private static ShuttleExternalHostSnapshot BuildHostSnapshot(ShuttleController controller)
        {
            try
            {
                if (controller == null || controller.ShuttleHost == null)
                {
                    return null;
                }

                ShuttleExternalHostInfo hostInfo;
                ShuttleExternalHostReadPort port =
                    new ShuttleExternalHostReadPort(controller.ShuttleHost);
                if (!port.TryGetHostInfo(out hostInfo) || hostInfo == null)
                {
                    return null;
                }

                return new ShuttleExternalHostSnapshot(
                    hostInfo.ThingIdNumber,
                    hostInfo.ThingId,
                    hostInfo.DefName,
                    hostInfo.Label,
                    hostInfo.FactionDefName,
                    hostInfo.Spawned,
                    hostInfo.MapUniqueId,
                    hostInfo.Tile,
                    hostInfo.PositionX,
                    hostInfo.PositionY,
                    hostInfo.PositionZ,
                    hostInfo.RotationAsInt,
                    CopyCells(hostInfo.OccupiedCells),
                    CopyCells(hostInfo.AdjacentExternalCells));
            }
            catch
            {
                return null;
            }
        }

        private static List<ShuttleExternalCellSnapshot> CopyCells(
            IReadOnlyList<IntVec3> cells)
        {
            List<ShuttleExternalCellSnapshot> result =
                new List<ShuttleExternalCellSnapshot>();
            for (int i = 0; cells != null && i < cells.Count; i++)
            {
                IntVec3 cell = cells[i];
                result.Add(new ShuttleExternalCellSnapshot(cell.x, cell.y, cell.z));
            }

            return result;
        }

        private static ShuttleExternalAssemblySnapshot BuildAssemblySnapshot(
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            ShuttleExternalHostSnapshot host)
        {
            try
            {
                if (assemblyState == null)
                {
                    return new ShuttleExternalAssemblySnapshot(
                        false,
                        "assembly state is unavailable",
                        host != null ? host.HostStableId : null,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0,
                        false,
                        false,
                        null);
                }

                assemblyState.EnsureInitialized();
                List<ShuttleExternalModuleSummarySnapshot> rows =
                    new List<ShuttleExternalModuleSummarySnapshot>();
                int enabled = 0;
                int disabled = 0;
                int externalCount = 0;
                IReadOnlyList<ShuttleModule> modules = assemblyState.Modules;
                for (int i = 0; modules != null && i < modules.Count; i++)
                {
                    ShuttleModule module = modules[i];
                    if (module == null)
                    {
                        continue;
                    }

                    if (module.IsEnabled)
                    {
                        enabled++;
                    }
                    else
                    {
                        disabled++;
                    }

                    List<string> runtimeKeys =
                        ExternalRuntimeBindingUtility.GetRuntimeSystemKeys(module.ModuleDef);
                    bool externallyExtensible = runtimeKeys != null && runtimeKeys.Count > 0;
                    if (externallyExtensible)
                    {
                        externalCount++;
                    }

                    ShuttleModuleSlot slot = assemblyState.GetModuleSlot(
                        module.ParentSegmentInstanceID,
                        module.ParentSlotID);
                    rows.Add(new ShuttleExternalModuleSummarySnapshot(
                        module.ModuleInstanceID,
                        module.moduleDefName,
                        ResolveModuleLabel(module),
                        module.ParentSegmentInstanceID,
                        module.ParentSlotID,
                        slot != null ? slot.SlotTypeID : null,
                        module.IsEnabled,
                        true,
                        !IsModuleUnderConstruction(runtimeState, module),
                        IsModuleUnderConstruction(runtimeState, module),
                        runtimeState != null &&
                            runtimeState.ModuleRemoval != null &&
                            runtimeState.ModuleRemoval.IsRemovingModule(module.ModuleInstanceID),
                        externallyExtensible,
                        runtimeKeys));
                }

                return new ShuttleExternalAssemblySnapshot(
                    true,
                    null,
                    host != null ? host.HostStableId : null,
                    assemblyState.Segments != null ? assemblyState.Segments.Count : 0,
                    modules != null ? modules.Count : 0,
                    modules != null ? modules.Count : 0,
                    enabled,
                    disabled,
                    externalCount,
                    runtimeState != null &&
                        runtimeState.AssemblyConstruction != null &&
                        runtimeState.AssemblyConstruction.HasActiveOrder,
                    runtimeState != null &&
                        runtimeState.ModuleRemoval != null &&
                        runtimeState.ModuleRemoval.HasActiveRemoval,
                    rows);
            }
            catch (Exception exception)
            {
                return new ShuttleExternalAssemblySnapshot(
                    false,
                    "assembly snapshot failed: " + exception.GetType().Name,
                    host != null ? host.HostStableId : null,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    false,
                    false,
                    null);
            }
        }

        private static string ResolveModuleLabel(ShuttleModule module)
        {
            if (module == null)
            {
                return null;
            }

            try
            {
                return module.ModuleDef != null
                    ? module.ModuleDef.LabelCap.ToString()
                    : module.moduleDefName;
            }
            catch
            {
                return module.moduleDefName;
            }
        }

        private static bool IsModuleUnderConstruction(
            ShuttleRuntimeState runtimeState,
            ShuttleModule module)
        {
            if (runtimeState == null ||
                runtimeState.AssemblyConstruction == null ||
                runtimeState.AssemblyConstruction.ActiveOrder == null ||
                module == null)
            {
                return false;
            }

            ShuttleAssemblyConstructionOrder order =
                runtimeState.AssemblyConstruction.ActiveOrder;
            if (!order.IsActive ||
                (order.Kind != ShuttleAssemblyConstructionKind.Module &&
                    order.Kind != ShuttleAssemblyConstructionKind.ModuleReplacement))
            {
                return false;
            }

            return order.SegmentInstanceID == module.ParentSegmentInstanceID &&
                order.ModuleSlotID == module.ParentSlotID;
        }

        private static ShuttleExternalProfileSnapshot BuildProfileSnapshot(
            ShuttleProfile profile)
        {
            try
            {
                if (profile == null)
                {
                    return new ShuttleExternalProfileSnapshot(
                        false,
                        "profile is unavailable",
                        0,
                        0f,
                        0f,
                        0f,
                        0,
                        0f,
                        0f,
                        0f,
                        0,
                        0,
                        false,
                        0,
                        false,
                        0,
                        false,
                        0,
                        false,
                        0,
                        0,
                        0,
                        0,
                        ShuttleExternalProfileExtensionSnapshot.Empty());
                }

                int warnings;
                int errors;
                CountIssues(profile, out warnings, out errors);
                int defenseCount =
                    (profile.Shield != null ? profile.Shield.SurfaceShieldModuleCount : 0) +
                    (profile.Shield != null ? profile.Shield.VanillaInterceptorShieldModuleCount : 0) +
                    (profile.Weapon != null ? profile.Weapon.TotalWeaponModules : 0);

                return new ShuttleExternalProfileSnapshot(
                    true,
                    null,
                    profile.Revision,
                    profile.Mass != null ? profile.Mass.TotalMass : 0f,
                    profile.Mass != null ? profile.Mass.MassCapacity : 0f,
                    profile.Cargo != null ? profile.Cargo.CargoMassCapacityKg : 0f,
                    profile.Flight != null ? profile.Flight.HardRangeCapTiles : 0,
                    profile.Power != null ? profile.Power.EnergyStorageCapacityWd : 0f,
                    profile.Power != null ? profile.Power.ReactorGenerationWatts : 0f,
                    profile.Power != null ? profile.Power.InternalIdleDemandWatts : 0f,
                    profile.Crew != null ? profile.Crew.CrewCapacity : 0,
                    profile.Cargo != null ? (int)profile.Cargo.CargoRegionCount : 0,
                    profile.MedicalBay != null && profile.MedicalBay.HasMedicalBay,
                    profile.MedicalBay != null ? profile.MedicalBay.MedicalPatientSlots : 0,
                    profile.PrisonCell != null && profile.PrisonCell.HasPrisonCell,
                    profile.PrisonCell != null ? profile.PrisonCell.PrisonerSlots : 0,
                    profile.MechCharger != null && profile.MechCharger.HasMechCharger,
                    profile.MechCharger != null ? profile.MechCharger.MechChargeSlots : 0,
                    defenseCount > 0,
                    defenseCount,
                    profile.Issues != null ? profile.Issues.Count : 0,
                    warnings,
                    errors,
                    profile.ExternalExpressions);
            }
            catch (Exception exception)
            {
                return new ShuttleExternalProfileSnapshot(
                    false,
                    "profile snapshot failed: " + exception.GetType().Name,
                    0,
                    0f,
                    0f,
                    0f,
                    0,
                    0f,
                    0f,
                    0f,
                    0,
                    0,
                    false,
                    0,
                    false,
                    0,
                    false,
                    0,
                    false,
                    0,
                    0,
                    0,
                    0,
                    ShuttleExternalProfileExtensionSnapshot.Empty());
            }
        }

        private static ShuttleExternalRuntimeSummarySnapshot BuildRuntimeSummary(
            ShuttleExternalIntegrationHealthReport health)
        {
            int externalCount = 0;
            int enabledCount = 0;
            int failedCount = 0;
            int migrationFailedCount = 0;
            IReadOnlyList<ShuttleExternalRuntimeHealthSnapshot> rows =
                health != null ? health.RuntimeRows : null;
            for (int i = 0; rows != null && i < rows.Count; i++)
            {
                ShuttleExternalRuntimeHealthSnapshot row = rows[i];
                if (row == null || !row.StateAvailable)
                {
                    continue;
                }

                externalCount++;
                if (row.RuntimeEnabledKnown && row.RuntimeEnabled)
                {
                    enabledCount++;
                }

                if (row.InitializationFailed || row.MigrationFailed || row.RuntimeExecutionFailed)
                {
                    failedCount++;
                }

                if (row.MigrationFailed)
                {
                    migrationFailedCount++;
                }
            }

            return new ShuttleExternalRuntimeSummarySnapshot(
                externalCount,
                enabledCount,
                failedCount,
                migrationFailedCount,
                rows);
        }

        private static ShuttleExternalPowerSnapshot BuildPowerSnapshot(
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState)
        {
            try
            {
                if (runtimeState == null || runtimeState.Power == null)
                {
                    return new ShuttleExternalPowerSnapshot(
                        false,
                        "power state is unavailable",
                        false,
                        0f,
                        0f,
                        0f,
                        0f,
                        0f,
                        0f,
                        false);
                }

                return new ShuttleExternalPowerSnapshot(
                    true,
                    null,
                    runtimeState.Power.InternalBusPowered,
                    runtimeState.Power.StoredEnergyWd,
                    profile != null && profile.Power != null
                        ? profile.Power.EnergyStorageCapacityWd
                        : runtimeState.Power.LastAppliedEnergyCapacityWd,
                    runtimeState.Power.LastReactorGenerationWatts,
                    runtimeState.Power.LastInternalDemandWatts,
                    runtimeState.Power.LastGridExportWatts,
                    runtimeState.Power.LastUnmetDemandWatts,
                    runtimeState.Power.LastUnmetDemandWatts > 0.0001f);
            }
            catch (Exception exception)
            {
                return new ShuttleExternalPowerSnapshot(
                    false,
                    "power snapshot failed: " + exception.GetType().Name,
                    false,
                    0f,
                    0f,
                    0f,
                    0f,
                    0f,
                    0f,
                    false);
            }
        }

        private static ShuttleExternalCargoSummarySnapshot BuildCargoSummary(
            ShuttleController controller,
            ShuttleProfile profile)
        {
            try
            {
                ShuttleCargoSnapshot snapshot =
                    controller != null ? controller.BuildLaunchCargoSnapshot() : null;
                if (snapshot == null)
                {
                    return new ShuttleExternalCargoSummarySnapshot(
                        false,
                        "cargo snapshot is unavailable",
                        0f,
                        profile != null && profile.Cargo != null
                            ? profile.Cargo.CargoMassCapacityKg
                            : 0f,
                        0f,
                        0f,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0f);
                }

                float capacity = snapshot.MassCapacity > 0f
                    ? snapshot.MassCapacity
                    : profile != null && profile.Cargo != null
                        ? profile.Cargo.CargoMassCapacityKg
                        : 0f;
                float used = snapshot.CargoMassUsage > 0f
                    ? snapshot.CargoMassUsage
                    : snapshot.TotalPlannedMassKg;
                float free = capacity - used;
                if (free < 0f)
                {
                    free = 0f;
                }

                return new ShuttleExternalCargoSummarySnapshot(
                    true,
                    null,
                    snapshot.TotalPlannedMassKg,
                    capacity,
                    used,
                    free,
                    snapshot.LoadedStackCount,
                    snapshot.LoadedThingCount,
                    snapshot.AssignedStackCount,
                    snapshot.AssignedThingCount,
                    snapshot.BlockedStackCount,
                    snapshot.RefrigeratedStackCount,
                    snapshot.RefrigeratedThingCount,
                    snapshot.RefrigeratedMassKg);
            }
            catch (Exception exception)
            {
                return new ShuttleExternalCargoSummarySnapshot(
                    false,
                    "cargo snapshot failed: " + exception.GetType().Name,
                    0f,
                    profile != null && profile.Cargo != null ? profile.Cargo.CargoMassCapacityKg : 0f,
                    0f,
                    0f,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0f);
            }
        }

        private static ShuttleExternalLaunchSummarySnapshot BuildLaunchSummary(
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            int ticksGame)
        {
            try
            {
                int warnings;
                int errors;
                CountIssues(profile, out warnings, out errors);
                int configuredCooldownTicks = profile != null && profile.Flight != null
                    ? profile.Flight.LaunchCooldownTicks
                    : 0;
                bool cooldownActive = configuredCooldownTicks > 0 &&
                    runtimeState != null &&
                    runtimeState.Launch != null &&
                    runtimeState.Launch.CooldownEndTick > ticksGame;
                int cooldownEndTick = runtimeState != null && runtimeState.Launch != null
                    ? runtimeState.Launch.CooldownEndTick
                    : -1;
                int cooldownRemaining = cooldownActive ? cooldownEndTick - ticksGame : 0;
                float storedEnergy = runtimeState != null && runtimeState.Power != null
                    ? runtimeState.Power.StoredEnergyWd
                    : 0f;
                float baseLaunchEnergy = profile != null && profile.Flight != null
                    ? profile.Flight.BaseLaunchEnergyWd
                    : 0f;
                bool energyReady = storedEnergy + 0.0001f >= baseLaunchEnergy;
                int blockers = errors + (cooldownActive ? 1 : 0) + (!energyReady ? 1 : 0);

                return new ShuttleExternalLaunchSummarySnapshot(
                    profile != null,
                    profile != null ? null : "profile is unavailable",
                    profile != null && blockers == 0,
                    (profile != null && profile.Issues != null ? profile.Issues.Count : 0) +
                        (cooldownActive ? 1 : 0) +
                        (!energyReady ? 1 : 0),
                    blockers,
                    warnings,
                    cooldownActive,
                    cooldownEndTick,
                    cooldownRemaining,
                    energyReady,
                    storedEnergy,
                    baseLaunchEnergy,
                    profile != null && profile.Flight != null
                        ? profile.Flight.HardRangeCapTiles
                        : 0);
            }
            catch (Exception exception)
            {
                return new ShuttleExternalLaunchSummarySnapshot(
                    false,
                    "launch snapshot failed: " + exception.GetType().Name,
                    false,
                    0,
                    0,
                    0,
                    false,
                    -1,
                    0,
                    false,
                    0f,
                    0f,
                    0);
            }
        }

        private static void CountIssues(
            ShuttleProfile profile,
            out int warnings,
            out int errors)
        {
            warnings = 0;
            errors = 0;
            IReadOnlyList<ProfileBuildIssue> issues =
                profile != null ? profile.Issues : null;
            for (int i = 0; issues != null && i < issues.Count; i++)
            {
                ProfileBuildIssue issue = issues[i];
                if (issue == null)
                {
                    continue;
                }

                if (issue.Severity == ProfileBuildIssueSeverity.Error)
                {
                    errors++;
                }
                else if (issue.Severity == ProfileBuildIssueSeverity.Warning)
                {
                    warnings++;
                }
            }
        }
    }
}
