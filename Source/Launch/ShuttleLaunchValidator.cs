using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AI.Habitat;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Flight;
using CeleTech.ShuttleExtension.ModularShuttle.Profile;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Launch
{
    public sealed class ShuttleLaunchValidator
    {
        private const float Epsilon = 0.0001f;
        private const string HabitatOccupiedIssueCode = "habitat-occupied";
        private const string MedicalBayOccupiedIssueCode = "medical-bay-occupied";
        private const string HolderTransferManifestActiveIssueCode = "holder-transfer-manifest-active";
        private const string LaunchAssemblyUnavailableKey = "CT_Shuttle_Launch_Failed_Assembly" + "StateUnavailable";
        private static readonly ShuttleLaunchEnvironmentPolicy EnvironmentPolicy = new ShuttleLaunchEnvironmentPolicy();
        private readonly IShuttleCargoBackend cargoBackend;
        private readonly ShuttleModuleRuntimeCoordinator moduleRuntimeCoordinator;

        internal ShuttleLaunchValidator(
            IShuttleCargoBackend cargoBackend,
            ShuttleModuleRuntimeCoordinator moduleRuntimeCoordinator)
        {
            this.cargoBackend = cargoBackend;
            this.moduleRuntimeCoordinator = moduleRuntimeCoordinator;
        }

        public ShuttleLaunchResult Validate(ShuttleLaunchRequest request, ShuttleFlightEnergyQuote quote)
        {
            if (request == null)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_InputUnavailable".Translate().ToString());
            }

            ThingWithComps host = request.Host;
            if (host == null)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_HostUnavailable".Translate().ToString(), quote);
            }

            if (!host.Spawned)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_NotSpawned".Translate().ToString(), quote);
            }

            if (host.Map == null)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_MapUnavailable".Translate().ToString(), quote);
            }

            if (request.Profile == null)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_ProfileUnavailable".Translate().ToString(), quote);
            }

            if (request.RuntimeState == null)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_RuntimeUnavailable".Translate().ToString(), quote);
            }


            request.RuntimeState.EnsureInitialized();
            if (request.RuntimeState.CargoUnload != null &&
                request.RuntimeState.CargoUnload.IsActive)
            {
                return ShuttleLaunchResult.Failed(
                    "CT_Shuttle_Launch_Failed_CargoUnloadingActive".Translate().ToString(),
                    quote);
            }

            if (request.AssemblyReadinessIssues == null)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_ReadinessUnavailable".Translate().ToString(), quote);
            }

            if (this.HasActiveMedicalProcedure(request))
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_ActiveMedicalProcedure".Translate().ToString(), quote);
            }

            CompShuttleHolderLaunchTransferState holderTransferState = host.TryGetComp<CompShuttleHolderLaunchTransferState>();
            if (holderTransferState != null && holderTransferState.HasAnyActiveOrRecoveryTransfer)
            {
                this.LogHolderTransferManifestLaunchBlocker(holderTransferState);
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_HolderTransferManifestActive".Translate().ToString(), quote);
            }

            string activeMedicalBayTransferReason;
            if (holderTransferState != null &&
                holderTransferState.HasActiveMedicalBayPatientLocalTransfer(out activeMedicalBayTransferReason))
            {
                Log.Warning("[CeleTech Shuttle] Launch blocked because MedicalBay patient transfer is active: " +
                    (activeMedicalBayTransferReason ?? "unknown"));
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_HolderTransferManifestActive".Translate().ToString(), quote);
            }

            RefrigeratedCargoCoolingStatus status;
            string refrigeratedCargoFailureReason;
            if (ShuttleRefrigeratedCargoLaunchTransferService.TryGetLaunchReadinessBlocker(
                host,
                this.ResolveAssemblyState(host),
                request.RuntimeState,
                out status,
                out refrigeratedCargoFailureReason))
            {
                return ShuttleLaunchResult.Failed(refrigeratedCargoFailureReason, quote);
            }

            CompShuttleHabitatOccupancy habitatOccupancy;
            if (HabitatUtility.TryGetHabitatOccupancy(host, out habitatOccupancy) &&
                habitatOccupancy.HasAnyOccupants)
            {
                string habitatJoyTransferFailureReason;
                if (ShuttleHolderLaunchTransferService.NeedsHabitatMixedLaunchTransfer(host))
                {
                    bool canUseHabitatMixedTransfer = ShuttleHolderLaunchTransferService.CanUseHabitatMixedLaunchTransfer(
                        host,
                        request.ArrivalAction,
                        out habitatJoyTransferFailureReason);
                    ShuttleHolderLaunchTransferService.LogHabitatLivingArrivalDiagnostics(
                        host,
                        request.DestinationTile,
                        request.ArrivalAction,
                        "launch-validation-mixed",
                        canUseHabitatMixedTransfer,
                        habitatJoyTransferFailureReason,
                        "mixed");
                    if (canUseHabitatMixedTransfer)
                    {
                        this.LogHabitatMixedLaunchTransferAllowed(habitatOccupancy);
                    }
                    else
                    {
                        this.LogHabitatLivingLaunchTransferRejected(habitatJoyTransferFailureReason);
                        this.LogHabitatLaunchBlocker(habitatOccupancy);
                        string failureKey = ShuttleHolderLaunchTransferService.GetHabitatLivingLaunchFailureKey(
                            request.ArrivalAction,
                            habitatJoyTransferFailureReason);
                        return ShuttleLaunchResult.Failed(failureKey.Translate().ToString(), quote);
                    }
                }
                else if (ShuttleHolderLaunchTransferService.NeedsHabitatJoyLaunchTransfer(host))
                {
                    bool canUseHabitatJoyTransfer = ShuttleHolderLaunchTransferService.CanUseHabitatJoyLaunchTransfer(
                        host,
                        request.ArrivalAction,
                        out habitatJoyTransferFailureReason);
                    ShuttleHolderLaunchTransferService.LogHabitatLivingArrivalDiagnostics(
                        host,
                        request.DestinationTile,
                        request.ArrivalAction,
                        "launch-validation-joy",
                        canUseHabitatJoyTransfer,
                        habitatJoyTransferFailureReason,
                        "joy-only");
                    if (canUseHabitatJoyTransfer)
                    {
                        this.LogHabitatJoyLaunchTransferAllowed(habitatOccupancy);
                    }
                    else
                    {
                        this.LogHabitatLivingLaunchTransferRejected(habitatJoyTransferFailureReason);
                        this.LogHabitatLaunchBlocker(habitatOccupancy);
                        string failureKey = ShuttleHolderLaunchTransferService.GetHabitatLivingLaunchFailureKey(
                            request.ArrivalAction,
                            habitatJoyTransferFailureReason);
                        return ShuttleLaunchResult.Failed(failureKey.Translate().ToString(), quote);
                    }
                }
                else
                {
                    string habitatTransferFailureReason;
                    bool canUseHabitatLivingTransfer = ShuttleHolderLaunchTransferService.CanUseHabitatLivingLaunchTransfer(
                        host,
                        request.ArrivalAction,
                        out habitatTransferFailureReason);
                    ShuttleHolderLaunchTransferService.LogHabitatLivingArrivalDiagnostics(
                        host,
                        request.DestinationTile,
                        request.ArrivalAction,
                        "launch-validation",
                        canUseHabitatLivingTransfer,
                        habitatTransferFailureReason,
                        "sleep-dining");
                    if (canUseHabitatLivingTransfer)
                    {
                        this.LogHabitatLivingLaunchTransferAllowed(habitatOccupancy);
                    }
                    else
                    {
                        this.LogHabitatLivingLaunchTransferRejected(habitatTransferFailureReason);
                        this.LogHabitatLaunchBlocker(habitatOccupancy);
                        string failureKey = ShuttleHolderLaunchTransferService.GetHabitatLivingLaunchFailureKey(
                            request.ArrivalAction,
                            habitatTransferFailureReason);
                        return ShuttleLaunchResult.Failed(failureKey.Translate().ToString(), quote);
                    }
                }
            }

            CompShuttleMedicalBayOccupancy medicalBayOccupancy = host.TryGetComp<CompShuttleMedicalBayOccupancy>();
            if (medicalBayOccupancy != null && medicalBayOccupancy.HasPatients)
            {
                string medicalBayTransferFailureReason;
                bool canUseMedicalBayTransfer;
                bool hasHabitatTransfer = ShuttleHolderLaunchTransferService.NeedsAnyHabitatLaunchTransfer(host);
                bool hasMechChargerTransfer = ShuttleHolderLaunchTransferService.NeedsMechChargerLaunchTransfer(host);
                if (hasHabitatTransfer && hasMechChargerTransfer)
                {
                    canUseMedicalBayTransfer =
                        ShuttleHolderLaunchTransferService.CanUseMedicalBayPatientLaunchTransferWithFullHolderConcurrency(
                            host,
                            request.ArrivalAction,
                            out medicalBayTransferFailureReason);
                }
                else if (hasHabitatTransfer)
                {
                    canUseMedicalBayTransfer =
                        ShuttleHolderLaunchTransferService.CanUseMedicalBayPatientLaunchTransferWithHabitatConcurrency(
                            host,
                            request.ArrivalAction,
                            out medicalBayTransferFailureReason);
                }
                else
                {
                    canUseMedicalBayTransfer =
                        ShuttleHolderLaunchTransferService.CanUseMedicalBayPatientLaunchTransfer(
                            host,
                            request.ArrivalAction,
                            out medicalBayTransferFailureReason);
                }

                if (canUseMedicalBayTransfer)
                {
                    this.LogMedicalBayLaunchTransferAllowed(medicalBayOccupancy);
                }
                else
                {
                    this.LogMedicalBayLaunchTransferRejected(medicalBayTransferFailureReason);
                    this.LogMedicalBayLaunchBlocker(medicalBayOccupancy);
                    string failureMessage = !medicalBayTransferFailureReason.NullOrEmpty()
                        ? medicalBayTransferFailureReason
                        : "CT_Shuttle_Launch_Failed_MedicalBayOccupied".Translate().ToString();
                    return ShuttleLaunchResult.Failed(failureMessage, quote);
                }
            }

            CompShuttleMechChargerOccupancy mechChargerOccupancy = host.TryGetComp<CompShuttleMechChargerOccupancy>();
            if (mechChargerOccupancy != null && mechChargerOccupancy.HasChargingMechs)
            {
                string mechChargerTransferFailureReason;
                bool canUseMechChargerTransfer;
                bool hasHabitatTransfer = ShuttleHolderLaunchTransferService.NeedsAnyHabitatLaunchTransfer(host);
                bool hasMedicalBayTransfer = ShuttleHolderLaunchTransferService.NeedsMedicalBayPatientLaunchTransfer(host);
                if (hasHabitatTransfer && hasMedicalBayTransfer)
                {
                    canUseMechChargerTransfer = ShuttleHolderLaunchTransferService.CanUseMechChargerLaunchTransferWithFullHolderConcurrency(
                        host,
                        request.ArrivalAction,
                        out mechChargerTransferFailureReason);
                }
                else if (hasMedicalBayTransfer)
                {
                    canUseMechChargerTransfer = ShuttleHolderLaunchTransferService.CanUseMechChargerLaunchTransferWithMedicalConcurrency(
                        host,
                        request.ArrivalAction,
                        out mechChargerTransferFailureReason);
                }
                else
                {
                    canUseMechChargerTransfer = ShuttleHolderLaunchTransferService.CanUseMechChargerLaunchTransferWithHabitatConcurrency(
                        host,
                        request.ArrivalAction,
                        out mechChargerTransferFailureReason);
                }

                if (!canUseMechChargerTransfer)
                {
                    string failureMessage = !mechChargerTransferFailureReason.NullOrEmpty()
                        ? mechChargerTransferFailureReason
                        : "CT_Shuttle_Launch_Failed_MechChargerTransferUnavailable".Translate("unknown").ToString();
                    return ShuttleLaunchResult.Failed(failureMessage, quote);
                }
            }

            CompShuttlePrisonCellOccupancy prisonCellOccupancy = host.TryGetComp<CompShuttlePrisonCellOccupancy>();
            if (prisonCellOccupancy != null && prisonCellOccupancy.HasPrisoners)
            {
                string prisonCellTransferFailureReason;
                bool canUsePrisonCellTransfer =
                    ShuttleHolderLaunchTransferService.CanUsePrisonCellPrisonerLaunchTransfer(
                        host,
                        request.ArrivalAction,
                        out prisonCellTransferFailureReason);
                if (!canUsePrisonCellTransfer)
                {
                    string failureMessage = !prisonCellTransferFailureReason.NullOrEmpty()
                        ? prisonCellTransferFailureReason
                        : "CT_Shuttle_Launch_Failed_PrisonCellTransferUnavailable".Translate().ToString();
                    return ShuttleLaunchResult.Failed(failureMessage, quote);
                }
            }

            ProfileBuildIssue assemblyReadinessError = this.GetFirstAssemblyReadinessError(request.AssemblyReadinessIssues, host);
            if (assemblyReadinessError != null)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_AssemblyNotReady".Translate(assemblyReadinessError.Message).ToString(), quote);
            }

            if (request.AssemblySnapshot == null)
            {
                return ShuttleLaunchResult.Failed(LaunchAssemblyUnavailableKey.Translate().ToString(), quote);
            }

            string environmentFailureReason;
            if (EnvironmentPolicy.TryGetLaunchBlocker(
                host,
                request.Profile.Command != null && request.Profile.Command.StabilizesAdverseWeather,
                out environmentFailureReason))
            {
                return ShuttleLaunchResult.Failed(environmentFailureReason, quote);
            }

            if (!request.DestinationTile.Valid)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_DestinationTileInvalid".Translate().ToString(), quote);
            }

            if (!host.Tile.Valid)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_OriginTileInvalid".Translate().ToString(), quote);
            }

            if (host.Tile.Layer != request.DestinationTile.Layer &&
                !host.Tile.Layer.HasConnectionPathTo(request.DestinationTile.Layer))
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_NoWorldPath".Translate().ToString(), quote);
            }

            if (request.DestinationTile.Tile == null || Find.World.Impassable(request.DestinationTile))
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_DestinationNotLaunchable".Translate().ToString(), quote);
            }

            WorldObject worldObject = Find.WorldObjects.WorldObjectAt<WorldObject>(request.DestinationTile);
            if (worldObject != null && !worldObject.def.validLaunchTarget)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_InvalidWorldObjectTarget".Translate().ToString(), quote);
            }

            if (!ShuttleSignalJammerAccessPolicy.CanReach(worldObject, request.Profile))
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_SignalJammerRequired".Translate().ToString(), quote);
            }

            if (this.cargoBackend == null || this.cargoBackend.ResolveTransportersForLaunch(host).Count == 0)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_TransporterBackendUnavailable".Translate().ToString(), quote);
            }

            ShuttlePassengerBoardingIntentRecord passengerBlocker;
            if (ShuttlePassengerBoardingIntentSystem.TryGetLaunchBlocker(
                host,
                request.RuntimeState,
                this.cargoBackend,
                out passengerBlocker))
            {
                string pawnLabel = passengerBlocker != null &&
                    !string.IsNullOrEmpty(passengerBlocker.PawnLabel)
                        ? passengerBlocker.PawnLabel
                        : "CT_Shuttle_Cargo_PassengerUnknown".Translate().ToString();
                return ShuttleLaunchResult.Failed(
                    "CT_Shuttle_Launch_Failed_PassengerNotAboard".Translate(
                        pawnLabel).ToString(),
                    quote);
            }

            float queuedCargoMassKg;
            if (!this.cargoBackend.TryGetQueuedMassKg(request.CargoSnapshot, out queuedCargoMassKg))
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_CargoSnapshotUnavailable".Translate().ToString(), quote);
            }

            if (queuedCargoMassKg > Epsilon || this.cargoBackend.HasPendingLoadQueue(host))
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_CargoLoadingIncomplete".Translate().ToString(), quote);
            }

            float loadedCargoMassKg;
            if (!this.cargoBackend.TryGetLoadedMassKg(request.CargoSnapshot, out loadedCargoMassKg))
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_CargoSnapshotUnavailable".Translate().ToString(), quote);
            }

            if (!ShuttleLaunchCrewRequirementUtility.HasLaunchController(request.CargoSnapshot, host, request.Profile))
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_CockpitColonistRequired".Translate().ToString(), quote);
            }

            ShuttleCargoCapacityFailure capacityFailure =
                ShuttleRefrigeratedCargoCapacityPolicy.ValidateLoadedCargo(
                    request.Profile,
                    request.CargoSnapshot);
            if (capacityFailure ==
                ShuttleCargoCapacityFailure.PositiveBaseCapacityRequired)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_PositiveCargoCapacityRequired".Translate().ToString(), quote);
            }

            if (capacityFailure == ShuttleCargoCapacityFailure.BaseCargoExceeded)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_CargoExceedsCapacity".Translate().ToString(), quote);
            }

            if (capacityFailure ==
                ShuttleCargoCapacityFailure.RefrigeratedCargoExceeded)
            {
                return ShuttleLaunchResult.Failed(
                    "CT_Shuttle_Launch_Failed_RefrigeratedCargoExceedsCapacity"
                        .Translate()
                        .ToString(),
                    quote);
            }

            if (this.cargoBackend.AnyLaunchTransporterPositionRoofed(host, host.Map))
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_UnderRoof".Translate().ToString(), quote);
            }

            if (!host.Position.InBounds(host.Map))
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_PositionInvalid".Translate().ToString(), quote);
            }

            request.RuntimeState.EnsureInitialized();
            int ticksGame = ShuttleTickUtility.TicksGameOrZero();
            if (this.GetLaunchCooldownTicks(request.Profile) > 0 &&
                request.RuntimeState.Launch.CooldownEndTick > ticksGame)
            {
                int remainingTicks = request.RuntimeState.Launch.CooldownEndTick - ticksGame;
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_CooldownRemaining".Translate(remainingTicks.ToStringTicksToPeriod(true, false, true, true, false)).ToString(), quote);
            }

            if (quote == null)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_QuoteUnavailable".Translate().ToString(), quote);
            }

            if (!quote.CanLaunch)
            {
                return ShuttleLaunchResult.Failed(quote.FailureReason, quote);
            }

            if (request.RuntimeState.Power.StoredEnergyWd + Epsilon < quote.RequiredEnergyWd)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_StoredChargeInsufficient".Translate().ToString(), quote);
            }

            string moduleRuntimeFailureReason;
            try
            {
                if (this.moduleRuntimeCoordinator != null &&
                    !this.moduleRuntimeCoordinator.PreLaunchValidate(
                        request.AssemblySnapshot,
                        request.RuntimeState.Modules,
                        out moduleRuntimeFailureReason))
                {
                    if (string.IsNullOrEmpty(moduleRuntimeFailureReason))
                    {
                        moduleRuntimeFailureReason = "CT_Shuttle_Launch_Failed_RuntimeValidation".Translate().ToString();
                    }

                    return ShuttleLaunchResult.Failed(moduleRuntimeFailureReason, quote);
                }
            }
            catch (System.Exception exception)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_RuntimeValidationWithReason".Translate(exception.Message).ToString(), quote);
            }

            return ShuttleLaunchResult.Succeeded("CT_Shuttle_Launch_Validated".Translate().ToString(), quote);
        }

        private int GetLaunchCooldownTicks(ShuttleProfile profile)
        {
            return profile != null && profile.Flight != null && profile.Flight.LaunchCooldownTicks >= 0
                ? profile.Flight.LaunchCooldownTicks
                : ShuttleProfileTuningDef.FallbackLaunchCooldownTicks;
        }

        private ShuttleAssemblyState ResolveAssemblyState(ThingWithComps host)
        {
            CompModularShuttleCore core = host != null
                ? host.TryGetComp<CompModularShuttleCore>()
                : null;
            return core != null && core.Controller != null
                ? core.Controller.AssemblyState
                : null;
        }

        private bool HasActiveMedicalProcedure(ShuttleLaunchRequest request)
        {
            if (request == null)
            {
                return false;
            }

            if (request.RuntimeState != null &&
                request.RuntimeState.MedicalProcedures != null &&
                request.RuntimeState.MedicalProcedures.HasActiveProcedure)
            {
                return true;
            }

            CompShuttleMedicalProcedureOccupancy procedureOccupancy =
                request.Host != null ? request.Host.TryGetComp<CompShuttleMedicalProcedureOccupancy>() : null;
            return procedureOccupancy != null && procedureOccupancy.HasActiveDoctors;
        }

        private void LogHabitatLaunchBlocker(CompShuttleHabitatOccupancy occupancy)
        {
            if (!Prefs.DevMode || occupancy == null)
            {
                return;
            }

            if (!ShuttleLogThrottle.Global.ShouldLog(this.GetLaunchValidatorLogKey("habitat-blocker", occupancy.parent)))
            {
                return;
            }

            List<Pawn> occupants = occupancy.OccupantsForReading;
            Log.Message(
                "[CeleTech Shuttle] Launch blocked: " +
                HabitatOccupiedIssueCode +
                " occupantCount=" +
                (occupants != null ? occupants.Count : 0) +
                " occupants=" +
                this.FormatPawnLabels(occupants));
        }

        private void LogMedicalBayLaunchBlocker(CompShuttleMedicalBayOccupancy occupancy)
        {
            if (!Prefs.DevMode || occupancy == null)
            {
                return;
            }

            if (!ShuttleLogThrottle.Global.ShouldLog(this.GetLaunchValidatorLogKey("medical-blocker", occupancy.parent)))
            {
                return;
            }

            List<Pawn> patients = occupancy.HeldPatients;
            Log.Message(
                "[CeleTech Shuttle] Launch blocked: " +
                MedicalBayOccupiedIssueCode +
                " patientCount=" +
                (patients != null ? patients.Count : 0) +
                " patients=" +
                this.FormatPawnLabels(patients));
        }

        private void LogMedicalBayLaunchTransferAllowed(CompShuttleMedicalBayOccupancy occupancy)
        {
            if (!Prefs.DevMode || occupancy == null)
            {
                return;
            }

            if (!ShuttleLogThrottle.Global.ShouldLog(this.GetLaunchValidatorLogKey("medical-transfer-allowed", occupancy.parent)))
            {
                return;
            }

            Log.Message(
                "[CeleTech Shuttle] MedicalBay patient launch transfer allowed validation for held patients. patientCount=" +
                occupancy.PatientCount);
        }

        private void LogMedicalBayLaunchTransferRejected(string reason)
        {
            if (!Prefs.DevMode)
            {
                return;
            }

            if (!ShuttleLogThrottle.Global.ShouldLog("LaunchValidator:medical-transfer-rejected:" + (reason ?? "null")))
            {
                return;
            }

            Log.Warning("[CeleTech Shuttle] MedicalBay patient launch transfer rejected: " + (reason ?? "null"));
        }

        private void LogHolderTransferManifestLaunchBlocker(CompShuttleHolderLaunchTransferState state)
        {
            if (!Prefs.DevMode || state == null)
            {
                return;
            }

            if (!ShuttleLogThrottle.Global.ShouldLog(this.GetLaunchValidatorLogKey("holder-manifest-blocker", state.parent)))
            {
                return;
            }

            Log.Message(
                "[CeleTech Shuttle] Launch blocked: " +
                HolderTransferManifestActiveIssueCode +
                " transferBlocker=" +
                state.DescribeActiveOrRecoveryTransferBlocker() +
                " manifest=" +
                state.DumpManifestForDebug());
        }

        private string GetLaunchValidatorLogKey(string category, Thing thing)
        {
            return "LaunchValidator:" +
                (category ?? "unknown") +
                ":" +
                (thing != null ? thing.thingIDNumber : -1);
        }

        private void LogHabitatLivingLaunchTransferAllowed(CompShuttleHabitatOccupancy occupancy)
        {
            if (!Prefs.DevMode || occupancy == null)
            {
                return;
            }

            if (!ShuttleLogThrottle.Global.ShouldLog(this.GetLaunchValidatorLogKey("habitat-living-transfer-allowed", occupancy.parent)))
            {
                return;
            }

            Log.Message(
                "[CeleTech Shuttle] Living Habitat launch transfer allowed validation for supported sleep/dining occupants only. " +
                "sleeping=" +
                occupancy.SleepingOccupantCount +
                " dining=" +
                occupancy.DiningOccupantCount);
        }

        private void LogHabitatJoyLaunchTransferAllowed(CompShuttleHabitatOccupancy occupancy)
        {
            if (!Prefs.DevMode || occupancy == null)
            {
                return;
            }

            if (!ShuttleLogThrottle.Global.ShouldLog(this.GetLaunchValidatorLogKey("habitat-joy-transfer-allowed", occupancy.parent)))
            {
                return;
            }

            Log.Message(
                "[CeleTech Shuttle] Habitat joy launch transfer allowed validation for joy-only occupants. " +
                "joy=" +
                occupancy.JoyOccupantCount);
        }

        private void LogHabitatMixedLaunchTransferAllowed(CompShuttleHabitatOccupancy occupancy)
        {
            if (!Prefs.DevMode || occupancy == null)
            {
                return;
            }

            if (!ShuttleLogThrottle.Global.ShouldLog(this.GetLaunchValidatorLogKey("habitat-mixed-transfer-allowed", occupancy.parent)))
            {
                return;
            }

            Log.Message(
                "[CeleTech Shuttle] Habitat mixed launch transfer allowed validation for mixed sleep/dining + joy occupants. " +
                "sleeping=" +
                occupancy.SleepingOccupantCount +
                " dining=" +
                occupancy.DiningOccupantCount +
                " joy=" +
                occupancy.JoyOccupantCount);
        }

        private void LogHabitatLivingLaunchTransferRejected(string reason)
        {
            if (!Prefs.DevMode)
            {
                return;
            }

            if (!ShuttleLogThrottle.Global.ShouldLog("LaunchValidator:habitat-living-transfer-rejected:" + (reason ?? "null")))
            {
                return;
            }

            Log.Warning("[CeleTech Shuttle] Living Habitat launch transfer rejected: " + reason);
        }

        private string FormatPawnLabels(List<Pawn> pawns)
        {
            if (pawns == null || pawns.Count == 0)
            {
                return "none";
            }

            string result = null;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                string label = pawn != null ? pawn.LabelShort : "null";
                result = string.IsNullOrEmpty(result) ? label : result + ", " + label;
            }

            return result;
        }

        private ProfileBuildIssue GetFirstAssemblyReadinessError(IReadOnlyList<ProfileBuildIssue> issues, ThingWithComps host)
        {
            if (issues == null)
            {
                return null;
            }

            for (int i = 0; i < issues.Count; i++)
            {
                ProfileBuildIssue issue = issues[i];
                if (issue != null && issue.Severity == ProfileBuildIssueSeverity.Error)
                {
                    if (this.ShouldIgnoreHabitatOccupiedReadinessIssueForLivingTransfer(issue, host))
                    {
                        continue;
                    }

                    if (this.ShouldIgnoreMedicalBayOccupiedReadinessIssueForTransfer(issue, host))
                    {
                        continue;
                    }

                    return issue;
                }
            }

            return null;
        }

        private bool ShouldIgnoreHabitatOccupiedReadinessIssueForLivingTransfer(ProfileBuildIssue issue, ThingWithComps host)
        {
            if (issue == null || issue.Code != HabitatOccupiedIssueCode)
            {
                return false;
            }

            string failureReason;
            if (ShuttleHolderLaunchTransferService.NeedsHabitatMixedLaunchTransfer(host))
            {
                return ShuttleHolderLaunchTransferService.CanUseHabitatMixedLaunchTransfer(host, out failureReason);
            }

            if (ShuttleHolderLaunchTransferService.NeedsHabitatJoyLaunchTransfer(host))
            {
                return ShuttleHolderLaunchTransferService.CanUseHabitatJoyLaunchTransfer(host, out failureReason);
            }

            return ShuttleHolderLaunchTransferService.CanUseHabitatLivingLaunchTransfer(host, out failureReason);
        }

        private bool ShouldIgnoreMedicalBayOccupiedReadinessIssueForTransfer(
            ProfileBuildIssue issue,
            ThingWithComps host)
        {
            if (issue == null || issue.Code != MedicalBayOccupiedIssueCode)
            {
                return false;
            }

            string failureReason;
            bool hasHabitatTransfer = ShuttleHolderLaunchTransferService.NeedsAnyHabitatLaunchTransfer(host);
            bool hasMechChargerTransfer = ShuttleHolderLaunchTransferService.NeedsMechChargerLaunchTransfer(host);
            if (hasHabitatTransfer && hasMechChargerTransfer)
            {
                return ShuttleHolderLaunchTransferService.CanUseMedicalBayPatientLaunchTransferWithFullHolderConcurrency(
                    host,
                    out failureReason);
            }

            if (hasHabitatTransfer)
            {
                return ShuttleHolderLaunchTransferService.CanUseMedicalBayPatientLaunchTransferWithHabitatConcurrency(
                    host,
                    out failureReason);
            }

            return ShuttleHolderLaunchTransferService.CanUseMedicalBayPatientLaunchTransfer(host, out failureReason);
        }

    }
}
