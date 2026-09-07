using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Launch;
using CeleTech.ShuttleExtension.ModularShuttle.Profile;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    public sealed partial class ShuttleController
    {
        // Read-model/readiness issue builder only. It constructs ProfileBuildIssue
        // lists and records dev diagnostics through existing owner helpers; except
        // for existing read/diagnostic helpers, it must not execute launch or mutate
        // holder-transfer, cargo, runtime, or gameplay state. Issue code, message,
        // scope, severity, referenceID, and append order are UI/launch checklist
        // behavior and should be changed only deliberately.
        private static class ShuttleControllerReadinessIssueBuilder
        {
            internal static IReadOnlyList<ProfileBuildIssue> BuildAssemblyReadinessIssues(
                ShuttleController owner)
            {
                return BuildAssemblyReadinessIssues(owner, null, null, true, true);
            }

            internal static IReadOnlyList<ProfileBuildIssue> BuildAssemblyReadinessIssues(
                ShuttleController owner,
                ShuttleProfile currentProfile,
                ShuttleCargoSnapshot cargoSnapshot)
            {
                return BuildAssemblyReadinessIssues(
                    owner,
                    currentProfile,
                    cargoSnapshot,
                    true,
                    false);
            }

            internal static IReadOnlyList<ProfileBuildIssue> BuildAssemblyReadinessIssuesForExternalLaunchRead(
                ShuttleController owner)
            {
                return BuildAssemblyReadinessIssues(owner, null, null, false, true);
            }

            internal static IReadOnlyList<ProfileBuildIssue> BuildAssemblyReadinessIssuesForExternalLaunchRead(
                ShuttleController owner,
                ShuttleProfile currentProfile,
                ShuttleCargoSnapshot cargoSnapshot)
            {
                return BuildAssemblyReadinessIssues(
                    owner,
                    currentProfile,
                    cargoSnapshot,
                    false,
                    false);
            }

            private static IReadOnlyList<ProfileBuildIssue> BuildAssemblyReadinessIssues(
                ShuttleController owner,
                ShuttleProfile currentProfile,
                ShuttleCargoSnapshot cargoSnapshot,
                bool includeExternalRuntimePreflight,
                bool buildCargoSnapshot)
            {
                List<ProfileBuildIssue> issues;
                if (currentProfile == null)
                {
                    currentProfile = owner.GetProfileForRead();
                }

                if (currentProfile != null && currentProfile.Issues != null)
                {
                    issues = new List<ProfileBuildIssue>(currentProfile.Issues);
                }
                else
                {
                    issues = SharedAssemblyReadinessValidator.Validate(owner.assemblyState);
                    if (issues == null)
                    {
                        issues = new List<ProfileBuildIssue>();
                    }
                }

                AppendAssemblyIntegrityReadinessIssue(owner, issues);
                AppendHolderTransferManifestReadinessIssue(owner, issues);
                AppendActiveMedicalProcedureReadinessIssue(owner, issues);
                AppendRefrigeratedCargoReadinessIssue(owner, issues);
                AppendHabitatOccupancyReadinessIssue(owner, issues);
                AppendMedicalBayOccupancyReadinessIssue(owner, issues);
                AppendMechChargerOccupancyReadinessIssue(owner, issues);
                AppendPrisonCellOccupancyReadinessIssue(owner, issues);
                if (buildCargoSnapshot)
                {
                    cargoSnapshot = TryBuildCargoSnapshot(owner, issues);
                }

                AppendLaunchValidatorReadinessIssues(
                    owner,
                    issues,
                    currentProfile,
                    cargoSnapshot,
                    includeExternalRuntimePreflight);
                return issues;
            }

            private static ShuttleCargoSnapshot TryBuildCargoSnapshot(
                ShuttleController owner,
                List<ProfileBuildIssue> issues)
            {
                try
                {
                    return owner.BuildCargoSnapshot();
                }
                catch (System.Exception exception)
                {
                    AddReadinessIssueIfMissing(
                        issues,
                        LaunchCargoBackendUnavailableIssueCode,
                        "CT_Shuttle_Launch_Failed_CargoSnapshotUnavailable".Translate().ToString(),
                        GetHostReferenceID(owner));
                    owner.AddDeveloperRuntimeDiagnostic(
                        "cargo-snapshot-readiness-failed",
                        "Cargo snapshot readiness build failed: " + exception.Message,
                        "Launch",
                        "Warning",
                        "ReadModel",
                        GetHostReferenceID(owner),
                        null);
                    return null;
                }
            }

            private static void AppendAssemblyIntegrityReadinessIssue(
                ShuttleController owner,
                List<ProfileBuildIssue> issues)
            {
                if (owner == null || issues == null || owner.assemblyState == null)
                {
                    return;
                }

                owner.assemblyState.EnsureInitialized();
                owner.assemblyState.RebuildIndexes();
                ShuttleAssemblyIntegrityReport report = owner.assemblyState.LastIntegrityReport;
                if (report == null || !report.HasBlockingIssues)
                {
                    return;
                }

                AddReadinessIssueIfMissing(
                    issues,
                    ShuttleAssemblyIntegrityReport.BlockingPlayerIssueCode,
                    "CT_Shuttle_Issue_AssemblyTopologyCorrupted".Translate().ToString(),
                    GetHostReferenceID(owner));
                owner.AddDeveloperRuntimeDiagnostic(
                    ShuttleAssemblyIntegrityReport.BlockingPlayerIssueCode,
                    report.BuildDeveloperSummary(8),
                    "Issue",
                    "Error",
                    "Assembly",
                    GetHostReferenceID(owner),
                    null);
            }

            private static void AppendLaunchValidatorReadinessIssues(
                ShuttleController owner,
                List<ProfileBuildIssue> issues,
                ShuttleProfile currentProfile,
                ShuttleCargoSnapshot cargoSnapshot,
                bool includeExternalRuntimePreflight)
            {
                if (issues == null)
                {
                    return;
                }

                if (owner.cargoBackend == null ||
                    owner.shuttleHost == null ||
                    owner.cargoBackend.ResolveTransportersForLaunch(owner.shuttleHost).Count == 0)
                {
                    AddReadinessIssueIfMissing(
                        issues,
                        LaunchCargoBackendUnavailableIssueCode,
                        "CT_Shuttle_Launch_Failed_TransporterBackendUnavailable".Translate().ToString(),
                        GetHostReferenceID(owner));
                }

                if (currentProfile != null &&
                    currentProfile.Command != null &&
                    currentProfile.Command.HasCockpit &&
                    cargoSnapshot != null &&
                    cargoSnapshot.HasTransporter &&
                    !ShuttleLaunchCrewRequirementUtility.HasLaunchController(cargoSnapshot, owner.shuttleHost, currentProfile))
                {
                    AddReadinessIssueIfMissing(
                        issues,
                        "cockpit-colonist-required",
                        "CT_Shuttle_Issue_CockpitColonistRequired".Translate().ToString(),
                        "cockpit");
                }

                if (cargoSnapshot != null)
                {
                    if (cargoSnapshot.QueuedMassKg > 0.0001f ||
                        (owner.cargoBackend != null &&
                            owner.shuttleHost != null &&
                            owner.cargoBackend.HasPendingLoadQueue(owner.shuttleHost)))
                    {
                        AddReadinessIssueIfMissing(
                            issues,
                            LaunchCargoLoadingIncompleteIssueCode,
                            "CT_Shuttle_Launch_Failed_CargoLoadingIncomplete".Translate().ToString(),
                            GetHostReferenceID(owner));
                    }

                    ShuttleCargoCapacityFailure capacityFailure =
                        ShuttleRefrigeratedCargoCapacityPolicy.ValidateLoadedCargo(
                            currentProfile,
                            cargoSnapshot);
                    if (capacityFailure ==
                        ShuttleCargoCapacityFailure.PositiveBaseCapacityRequired)
                    {
                        AddReadinessIssueIfMissing(
                            issues,
                            LaunchCargoCapacityUnavailableIssueCode,
                            "CT_Shuttle_Launch_Failed_PositiveCargoCapacityRequired".Translate().ToString(),
                            GetHostReferenceID(owner));
                    }

                    if (capacityFailure ==
                        ShuttleCargoCapacityFailure.BaseCargoExceeded)
                    {
                        AddReadinessIssueIfMissing(
                            issues,
                            LaunchCargoMassExceedsCapacityIssueCode,
                            "CT_Shuttle_Launch_Failed_CargoExceedsCapacity".Translate().ToString(),
                            GetHostReferenceID(owner));
                    }

                    if (capacityFailure ==
                        ShuttleCargoCapacityFailure.RefrigeratedCargoExceeded)
                    {
                        AddReadinessIssueIfMissing(
                            issues,
                            LaunchCargoMassExceedsCapacityIssueCode,
                            "CT_Shuttle_Launch_Failed_RefrigeratedCargoExceedsCapacity"
                                .Translate()
                                .ToString(),
                            GetHostReferenceID(owner));
                    }
                }

                if (owner.cargoBackend != null &&
                    owner.shuttleHost != null &&
                    owner.shuttleHost.Map != null &&
                    owner.cargoBackend.AnyLaunchTransporterPositionRoofed(
                        owner.shuttleHost,
                        owner.shuttleHost.Map))
                {
                    AddReadinessIssueIfMissing(
                        issues,
                        LaunchUnderRoofIssueCode,
                        "CT_Shuttle_Launch_Failed_UnderRoof".Translate().ToString(),
                        GetHostReferenceID(owner));
                }

                owner.EnsureRuntimeState();
                if (owner.runtimeState != null &&
                    owner.runtimeState.CargoUnload != null &&
                    owner.runtimeState.CargoUnload.IsActive)
                {
                    AddReadinessIssueIfMissing(
                        issues,
                        LaunchCargoUnloadingActiveIssueCode,
                        "CT_Shuttle_Launch_Failed_CargoUnloadingActive".Translate().ToString(),
                        GetHostReferenceID(owner));
                }

                int ticksGame = ShuttleTickUtility.TicksGameOrZero();
                if (owner.runtimeState != null &&
                    owner.runtimeState.Launch != null &&
                    GetLaunchCooldownTicks(currentProfile) > 0 &&
                    owner.runtimeState.Launch.CooldownEndTick > ticksGame)
                {
                    int remainingTicks = owner.runtimeState.Launch.CooldownEndTick - ticksGame;
                    AddReadinessIssueIfMissing(
                        issues,
                        LaunchCooldownIssueCode,
                        "CT_Shuttle_Launch_Failed_CooldownRemaining".Translate(
                            remainingTicks.ToStringTicksToPeriod(true, false, true, true, false)).ToString(),
                        GetHostReferenceID(owner));
                }

                if (currentProfile != null &&
                    currentProfile.Flight != null &&
                    currentProfile.Flight.HardRangeCapTiles <= 0)
                {
                    AddReadinessIssueIfMissing(
                        issues,
                        LaunchRangeUnavailableIssueCode,
                        "CT_Shuttle_Launch_Failed_BeyondCurrentRange".Translate().ToString(),
                        GetHostReferenceID(owner));
                }

                if (currentProfile != null &&
                    currentProfile.Flight != null &&
                    owner.runtimeState != null &&
                    owner.runtimeState.Power != null &&
                    owner.runtimeState.Power.StoredEnergyWd + 0.0001f < currentProfile.Flight.BaseLaunchEnergyWd)
                {
                    AddReadinessIssueIfMissing(
                        issues,
                        LaunchStoredEnergyInsufficientIssueCode,
                        "CT_Shuttle_Launch_Failed_StoredChargeInsufficient".Translate().ToString(),
                        GetHostReferenceID(owner));
                }

                string environmentFailureReason;
                ShuttleLaunchEnvironmentPolicy environmentPolicy = new ShuttleLaunchEnvironmentPolicy();
                if (environmentPolicy.TryGetLaunchBlocker(
                    owner.shuttleHost,
                    currentProfile != null &&
                        currentProfile.Command != null &&
                        currentProfile.Command.StabilizesAdverseWeather,
                    out environmentFailureReason))
                {
                    AddReadinessIssueIfMissing(
                        issues,
                        LaunchEnvironmentBlockedIssueCode,
                        environmentFailureReason,
                        GetHostReferenceID(owner));
                }

                string runtimeFailureReason = null;
                try
                {
                    ShuttleLaunchAssemblySnapshot assemblySnapshot = owner.BuildLaunchAssemblySnapshot();
                    bool runtimePreflightPassed = true;
                    if (owner.moduleRuntimeCoordinator != null &&
                        owner.runtimeState != null)
                    {
                        runtimePreflightPassed = includeExternalRuntimePreflight
                            ? owner.moduleRuntimeCoordinator.PreLaunchValidate(
                                assemblySnapshot,
                                owner.runtimeState.Modules,
                                out runtimeFailureReason)
                            : owner.moduleRuntimeCoordinator
                                .PreLaunchValidateWithoutExternalRuntimeSystems(
                                    assemblySnapshot,
                                    owner.runtimeState.Modules,
                                    out runtimeFailureReason);
                    }

                    if (!runtimePreflightPassed)
                    {
                        AddReadinessIssueIfMissing(
                            issues,
                            LaunchRuntimePreflightIssueCode,
                            !string.IsNullOrEmpty(runtimeFailureReason)
                                ? runtimeFailureReason
                                : "CT_Shuttle_Launch_Failed_RuntimeValidation".Translate().ToString(),
                            GetHostReferenceID(owner));
                    }
                }
                catch (System.Exception exception)
                {
                    AddReadinessIssueIfMissing(
                        issues,
                        LaunchRuntimePreflightIssueCode,
                        "CT_Shuttle_Launch_Failed_RuntimeValidationWithReason".Translate(exception.Message).ToString(),
                        GetHostReferenceID(owner));
                    owner.AddDeveloperRuntimeDiagnostic(
                        "runtime-prelaunch-readiness-failed",
                        "Runtime prelaunch readiness threw: " + exception.Message,
                        "Launch",
                        "Warning",
                        "ReadModel",
                        GetHostReferenceID(owner),
                        null);
                }
            }

            private static int GetLaunchCooldownTicks(ShuttleProfile profile)
            {
                return profile != null && profile.Flight != null && profile.Flight.LaunchCooldownTicks >= 0
                    ? profile.Flight.LaunchCooldownTicks
                    : ShuttleProfileTuningDef.FallbackLaunchCooldownTicks;
            }

            private static void AppendActiveMedicalProcedureReadinessIssue(
                ShuttleController owner,
                List<ProfileBuildIssue> issues)
            {
                if (issues == null || !owner.HasActiveMedicalProcedure())
                {
                    return;
                }

                issues.Add(new ProfileBuildIssue(
                    ActiveMedicalProcedureIssueCode,
                    "CT_Shuttle_Issue_ActiveMedicalProcedure".Translate().ToString(),
                    ProfileBuildIssueSeverity.Error,
                    ProfileBuildIssueScope.Assembly,
                    owner.shuttleHost != null
                        ? owner.shuttleHost.thingIDNumber.ToString()
                        : ActiveMedicalProcedureIssueCode));
            }

            private static void AppendRefrigeratedCargoReadinessIssue(
                ShuttleController owner,
                List<ProfileBuildIssue> issues)
            {
                if (issues == null || owner.shuttleHost == null)
                {
                    return;
                }

                RefrigeratedCargoCoolingStatus status;
                string failureReason;
                if (!ShuttleRefrigeratedCargoLaunchTransferService.TryGetLaunchReadinessBlocker(
                    owner.shuttleHost,
                    owner.assemblyState,
                    owner.runtimeState,
                    out status,
                    out failureReason))
                {
                    return;
                }

                issues.Add(new ProfileBuildIssue(
                    RefrigeratedCargoLaunchTransferUnavailableIssueCode,
                    failureReason,
                    ProfileBuildIssueSeverity.Error,
                    ProfileBuildIssueScope.Assembly,
                    status != null && !string.IsNullOrEmpty(status.ModuleInstanceID)
                        ? status.ModuleInstanceID
                        : owner.shuttleHost.thingIDNumber.ToString()));
            }

            private static void AppendHabitatOccupancyReadinessIssue(
                ShuttleController owner,
                List<ProfileBuildIssue> issues)
            {
                if (issues == null || owner.shuttleHost == null)
                {
                    return;
                }

                CompShuttleHabitatOccupancy occupancy =
                    owner.shuttleHost.TryGetComp<CompShuttleHabitatOccupancy>();
                if (occupancy == null || !occupancy.HasAnyOccupants)
                {
                    return;
                }

                CompShuttleHolderLaunchTransferState state =
                    owner.shuttleHost.TryGetComp<CompShuttleHolderLaunchTransferState>();
                if (state != null && state.HasActiveManifest)
                {
                    return;
                }

                string transferFailureReason;
                if (ShuttleHolderLaunchTransferService.NeedsHabitatMixedLaunchTransfer(owner.shuttleHost))
                {
                    if (ShuttleHolderLaunchTransferService.CanUseHabitatMixedLaunchTransfer(
                        owner.shuttleHost,
                        out transferFailureReason))
                    {
                        return;
                    }
                }
                else if (ShuttleHolderLaunchTransferService.NeedsHabitatJoyLaunchTransfer(owner.shuttleHost))
                {
                    if (ShuttleHolderLaunchTransferService.CanUseHabitatJoyLaunchTransfer(
                        owner.shuttleHost,
                        out transferFailureReason))
                    {
                        return;
                    }
                }
                else if (ShuttleHolderLaunchTransferService.CanUseHabitatLivingLaunchTransfer(
                    owner.shuttleHost,
                    out transferFailureReason))
                {
                    return;
                }

                string issueKey =
                    ShuttleHolderLaunchTransferService.GetHabitatLivingReadinessIssueKey(transferFailureReason);
                string issueCode = issueKey == "CT_Shuttle_Issue_HabitatJoyOccupied"
                    ? HabitatJoyOccupiedIssueCode
                    : HabitatTransferInvalidStateIssueCode;
                issues.Add(new ProfileBuildIssue(
                    issueCode,
                    issueKey.Translate().ToString(),
                    ProfileBuildIssueSeverity.Error,
                    ProfileBuildIssueScope.Assembly,
                    owner.shuttleHost.thingIDNumber.ToString()));
            }

            private static void AppendMedicalBayOccupancyReadinessIssue(
                ShuttleController owner,
                List<ProfileBuildIssue> issues)
            {
                if (issues == null || owner.shuttleHost == null)
                {
                    return;
                }

                CompShuttleMedicalBayOccupancy occupancy =
                    owner.shuttleHost.TryGetComp<CompShuttleMedicalBayOccupancy>();
                if (occupancy == null || !occupancy.HasPatients)
                {
                    return;
                }

                string medicalBayTransferFailureReason;
                bool canUseMedicalBayTransfer;
                bool hasHabitatTransfer =
                    ShuttleHolderLaunchTransferService.NeedsAnyHabitatLaunchTransfer(owner.shuttleHost);
                bool hasMechChargerTransfer =
                    ShuttleHolderLaunchTransferService.NeedsMechChargerLaunchTransfer(owner.shuttleHost);
                if (hasHabitatTransfer && hasMechChargerTransfer)
                {
                    canUseMedicalBayTransfer =
                        ShuttleHolderLaunchTransferService.CanUseMedicalBayPatientLaunchTransferWithFullHolderConcurrency(
                            owner.shuttleHost,
                            out medicalBayTransferFailureReason);
                }
                else if (hasHabitatTransfer)
                {
                    canUseMedicalBayTransfer =
                        ShuttleHolderLaunchTransferService.CanUseMedicalBayPatientLaunchTransferWithHabitatConcurrency(
                            owner.shuttleHost,
                            out medicalBayTransferFailureReason);
                }
                else
                {
                    canUseMedicalBayTransfer =
                        ShuttleHolderLaunchTransferService.CanUseMedicalBayPatientLaunchTransfer(
                            owner.shuttleHost,
                            out medicalBayTransferFailureReason);
                }

                if (canUseMedicalBayTransfer)
                {
                    return;
                }

                string issueMessage = !medicalBayTransferFailureReason.NullOrEmpty()
                    ? medicalBayTransferFailureReason
                    : "CT_Shuttle_Issue_MedicalBayOccupied".Translate().ToString();
                issues.Add(new ProfileBuildIssue(
                    MedicalBayOccupiedIssueCode,
                    issueMessage,
                    ProfileBuildIssueSeverity.Error,
                    ProfileBuildIssueScope.Assembly,
                    owner.shuttleHost.thingIDNumber.ToString()));
            }

            private static void AppendMechChargerOccupancyReadinessIssue(
                ShuttleController owner,
                List<ProfileBuildIssue> issues)
            {
                if (issues == null || owner.shuttleHost == null)
                {
                    return;
                }

                CompShuttleMechChargerOccupancy occupancy =
                    owner.shuttleHost.TryGetComp<CompShuttleMechChargerOccupancy>();
                if (occupancy == null || !occupancy.HasChargingMechs)
                {
                    return;
                }

                string mechChargerTransferFailureReason;
                bool canUseMechChargerTransfer;
                bool hasHabitatTransfer =
                    ShuttleHolderLaunchTransferService.NeedsAnyHabitatLaunchTransfer(owner.shuttleHost);
                bool hasMedicalBayTransfer =
                    ShuttleHolderLaunchTransferService.NeedsMedicalBayPatientLaunchTransfer(owner.shuttleHost);
                if (hasHabitatTransfer && hasMedicalBayTransfer)
                {
                    canUseMechChargerTransfer =
                        ShuttleHolderLaunchTransferService.CanUseMechChargerLaunchTransferWithFullHolderConcurrency(
                            owner.shuttleHost,
                            out mechChargerTransferFailureReason);
                }
                else if (hasMedicalBayTransfer)
                {
                    canUseMechChargerTransfer =
                        ShuttleHolderLaunchTransferService.CanUseMechChargerLaunchTransferWithMedicalConcurrency(
                            owner.shuttleHost,
                            out mechChargerTransferFailureReason);
                }
                else
                {
                    canUseMechChargerTransfer =
                        ShuttleHolderLaunchTransferService.CanUseMechChargerLaunchTransferWithHabitatConcurrency(
                            owner.shuttleHost,
                            out mechChargerTransferFailureReason);
                }

                if (canUseMechChargerTransfer)
                {
                    return;
                }

                string issueMessage = !mechChargerTransferFailureReason.NullOrEmpty()
                    ? mechChargerTransferFailureReason
                    : "CT_Shuttle_Issue_MechChargerTransferUnavailable".Translate().ToString();
                issues.Add(new ProfileBuildIssue(
                    MechChargerOccupiedIssueCode,
                    issueMessage,
                    ProfileBuildIssueSeverity.Error,
                    ProfileBuildIssueScope.Assembly,
                    owner.shuttleHost.thingIDNumber.ToString()));
            }

            private static void AppendPrisonCellOccupancyReadinessIssue(
                ShuttleController owner,
                List<ProfileBuildIssue> issues)
            {
                if (issues == null || owner.shuttleHost == null)
                {
                    return;
                }

                CompShuttlePrisonCellOccupancy occupancy =
                    owner.shuttleHost.TryGetComp<CompShuttlePrisonCellOccupancy>();
                if (occupancy == null || !occupancy.HasPrisoners)
                {
                    return;
                }

                string prisonCellTransferFailureReason;
                if (ShuttleHolderLaunchTransferService.CanUsePrisonCellPrisonerLaunchTransfer(
                    owner.shuttleHost,
                    out prisonCellTransferFailureReason))
                {
                    return;
                }

                string issueMessage = !prisonCellTransferFailureReason.NullOrEmpty()
                    ? prisonCellTransferFailureReason
                    : "CT_Shuttle_Issue_PrisonCellTransferUnavailable".Translate().ToString();
                issues.Add(new ProfileBuildIssue(
                    PrisonCellOccupiedIssueCode,
                    issueMessage,
                    ProfileBuildIssueSeverity.Error,
                    ProfileBuildIssueScope.Assembly,
                    owner.shuttleHost.thingIDNumber.ToString()));
            }

            private static void AppendHolderTransferManifestReadinessIssue(
                ShuttleController owner,
                List<ProfileBuildIssue> issues)
            {
                if (issues == null || owner.shuttleHost == null)
                {
                    return;
                }

                CompShuttleHolderLaunchTransferState state =
                    owner.shuttleHost.TryGetComp<CompShuttleHolderLaunchTransferState>();
                if (state == null)
                {
                    return;
                }

                string activeMedicalBayTransferReason;
                if (!state.HasAnyActiveOrRecoveryTransfer &&
                    !state.HasActiveMedicalBayPatientLocalTransfer(out activeMedicalBayTransferReason))
                {
                    return;
                }

                issues.Add(new ProfileBuildIssue(
                    HolderTransferManifestActiveIssueCode,
                    "CT_Shuttle_Issue_HolderTransferManifestActive".Translate().ToString(),
                    ProfileBuildIssueSeverity.Error,
                    ProfileBuildIssueScope.Assembly,
                    owner.shuttleHost.thingIDNumber.ToString()));
            }

            private static void AddReadinessIssueIfMissing(
                List<ProfileBuildIssue> issues,
                string code,
                string message,
                string referenceID)
            {
                if (issues == null || string.IsNullOrEmpty(code))
                {
                    return;
                }

                for (int i = 0; i < issues.Count; i++)
                {
                    ProfileBuildIssue existing = issues[i];
                    if (existing != null && existing.Code == code)
                    {
                        return;
                    }
                }

                issues.Add(new ProfileBuildIssue(
                    code,
                    message,
                    ProfileBuildIssueSeverity.Error,
                    ProfileBuildIssueScope.Assembly,
                    referenceID));
            }

            private static string GetHostReferenceID(ShuttleController owner)
            {
                return owner.shuttleHost != null
                    ? owner.shuttleHost.thingIDNumber.ToString()
                    : "shuttle";
            }
        }
    }
}
