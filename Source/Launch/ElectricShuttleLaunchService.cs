using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Bridges;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Flight;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using CeleTech.ShuttleExtension.ModularShuttle.World;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Launch
{
    public sealed class ElectricShuttleLaunchService
    {
        private const string IncomingSkyfallerDefName = "CT_ModularShuttleIncoming";

        private readonly IShuttleStoredEnergySink storedEnergySink;
        private readonly ShuttleLaunchValidator validator;
        private readonly IShuttleCargoBackend cargoBackend;
        private readonly IShuttleSkyfallerFactory skyfallerFactory;
        private readonly ShuttleModuleRuntimeCoordinator moduleRuntimeCoordinator;

        internal ElectricShuttleLaunchService(
            IShuttleStoredEnergySink storedEnergySink,
            ShuttleLaunchValidator validator,
            IShuttleCargoBackend cargoBackend,
            IShuttleSkyfallerFactory skyfallerFactory,
            ShuttleModuleRuntimeCoordinator moduleRuntimeCoordinator)
        {
            this.storedEnergySink = storedEnergySink;
            this.validator = validator;
            this.cargoBackend = cargoBackend;
            this.skyfallerFactory = skyfallerFactory;
            this.moduleRuntimeCoordinator = moduleRuntimeCoordinator;
        }

        public ShuttleLaunchResult ExecuteLaunch(ShuttleLaunchExecutionInput input)
        {
            if (input == null)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_InputUnavailable".Translate().ToString());
            }

            ShuttleLaunchRequest request = input.Request;
            ShuttleFlightEnergyQuote quote = input.Quote;
            bool clearDevHabitatJoySpikeOnExit =
                ShuttleHolderLaunchTransferService.IsDevHabitatJoyRealLaunchSpikeEnabled(
                    request != null ? request.Host : null);
            bool clearDevHabitatMixedSpikeOnExit =
                ShuttleHolderLaunchTransferService.IsDevHabitatMixedRealLaunchSpikeEnabled(
                    request != null ? request.Host : null);
            bool clearDevMedicalBayRollbackSpikeOnExit =
                ShuttleHolderLaunchTransferService.IsDevMedicalBayRealLaunchRollbackSpikeEnabled(
                    request != null ? request.Host : null);
            bool clearDevMedicalBayRestoreSpikeOnExit = false;

            try
            {
            ShuttleLaunchResult validation = this.validator.Validate(request, quote);
            if (!validation.Success)
            {
                return validation;
            }

            ShuttleHolderLaunchTransferTransaction holderTransferTransaction =
                ShuttleHolderLaunchTransferParticipants.CreateTransaction(request.Host);
            if (!holderTransferTransaction.CanUseAll(
                request.Host,
                request.ArrivalAction,
                out string holderTransferPreflightFailureReason))
            {
                return ShuttleLaunchResult.Failed(
                    holderTransferPreflightFailureReason ??
                    "CT_Shuttle_Launch_Failed_HolderTransferManifestActive".Translate().ToString(),
                    quote);
            }

            ShuttleLaunchPayload payload = this.BuildLaunchPayload(input.ProfileRevision, request, quote);
            PreparedLaunchTransfer preparedTransfer;
            ShuttleLaunchResult prepareResult = this.TryPrepareLaunchTransfer(request, quote, out preparedTransfer);
            if (!prepareResult.Success)
            {
                return prepareResult;
            }

            ShuttleRuntimeState runtimeState = request.RuntimeState;
            ShuttleProfile profile = request.Profile;
            int previousLastLaunchTick = runtimeState.Launch.LastLaunchTick;
            int previousCooldownEndTick = runtimeState.Launch.CooldownEndTick;

            // Launch energy is paid only after all predictable launch transfer inputs are ready.
            // Grid export and legacy vanilla launch/power comps are not energy sources.
            if (this.storedEnergySink == null ||
                !this.storedEnergySink.TryConsumeStoredEnergyWd(runtimeState, quote.RequiredEnergyWd))
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_StoredChargeInsufficient".Translate().ToString(), quote);
            }

            int ticksGame = ShuttleTickUtility.TicksGameOrZero();
            runtimeState.Launch.LastLaunchTick = ticksGame;
            runtimeState.Launch.CooldownEndTick = ticksGame + request.CooldownTicks;

            List<ShuttleLaunchCargoHandoff> committedHandoffs;
            ShuttleLaunchResult commitResult = this.TryCommitLaunchTransfer(preparedTransfer, quote, out committedHandoffs);
            if (!commitResult.Success)
            {
                this.RollbackLaunchRuntime(
                    runtimeState,
                    profile,
                    quote.RequiredEnergyWd,
                    previousLastLaunchTick,
                    previousCooldownEndTick);
                return this.FailAfterRollbackCommittedCargo(
                    preparedTransfer,
                    committedHandoffs,
                    commitResult.Message,
                    quote);
            }

            ShuttleLaunchResult groupIDValidation = this.ValidateCommittedHandoffGroupIDs(committedHandoffs, quote);
            if (!groupIDValidation.Success)
            {
                this.RollbackLaunchRuntime(
                    runtimeState,
                    profile,
                    quote.RequiredEnergyWd,
                    previousLastLaunchTick,
                    previousCooldownEndTick);
                return this.FailAfterRollbackCommittedCargo(
                    preparedTransfer,
                    committedHandoffs,
                    groupIDValidation.Message,
                    quote);
            }

            string passengerWeaponCargoFailure;
            if (!this.TryNormalizePassengerWeaponCargo(
                committedHandoffs,
                out passengerWeaponCargoFailure))
            {
                this.RollbackLaunchRuntime(
                    runtimeState,
                    profile,
                    quote.RequiredEnergyWd,
                    previousLastLaunchTick,
                    previousCooldownEndTick);
                return this.FailAfterRollbackCommittedCargo(
                    preparedTransfer,
                    committedHandoffs,
                    "CT_Shuttle_Launch_Failed_CargoHandoffFailed".Translate(
                        passengerWeaponCargoFailure ?? "Passenger weapon cargo transfer failed.").ToString(),
                    quote);
            }

            ShuttleAssemblyState liveAssemblyState = this.ResolveAssemblyState(request.Host);
            bool refrigeratedCargoExported = false;
            if (ShuttleRefrigeratedCargoLaunchTransferService.HasColdCargoForLaunch(
                request.Host,
                liveAssemblyState,
                runtimeState))
            {
                int exportedColdStacks;
                string refrigeratedExportFailureReason;
                if (!ShuttleRefrigeratedCargoLaunchTransferService.TryExportColdCargoForLaunch(
                    request.Host,
                    liveAssemblyState,
                    runtimeState,
                    out exportedColdStacks,
                    out refrigeratedExportFailureReason))
                {
                    this.RollbackLaunchRuntime(
                        runtimeState,
                        profile,
                        quote.RequiredEnergyWd,
                        previousLastLaunchTick,
                        previousCooldownEndTick);
                    return this.FailAfterRollbackCommittedCargo(
                        preparedTransfer,
                        committedHandoffs,
                        refrigeratedExportFailureReason,
                        quote);
                }

                refrigeratedCargoExported = exportedColdStacks > 0;
            }

            if (ShuttleHolderLaunchTransferService.NeedsMedicalBayPatientLaunchTransfer(request.Host))
            {
                clearDevMedicalBayRestoreSpikeOnExit =
                    ShuttleHolderLaunchTransferService.IsDevMedicalBayRealLaunchRestoreSpikeEnabled(request.Host);
            }

            ShuttleHolderLaunchTransferTransactionResult holderExportResult;
            if (!holderTransferTransaction.TryExportAll(
                request.Host,
                committedHandoffs,
                out holderExportResult))
            {
                if (!holderExportResult.HandoffsClear)
                {
                    string refrigeratedRollbackFailure =
                        this.RollbackRefrigeratedCargoLaunchExportIfNeeded(
                            request,
                            ref refrigeratedCargoExported,
                            liveAssemblyState,
                            holderExportResult.FailedParticipantKey ?? "holder export failure with uncleared handoffs");
                    this.RollbackLaunchRuntime(
                        runtimeState,
                        profile,
                        quote.RequiredEnergyWd,
                        previousLastLaunchTick,
                        previousCooldownEndTick);
                    return ShuttleLaunchResult.Failed(
                        this.AppendFailureDiagnostic(holderExportResult.Message, refrigeratedRollbackFailure),
                        quote);
                }

                string refrigeratedRollbackFailureAfterHolderExport =
                    this.RollbackRefrigeratedCargoLaunchExportIfNeeded(
                    request,
                    ref refrigeratedCargoExported,
                    liveAssemblyState,
                    holderExportResult.FailedParticipantKey ?? "holder export failure");
                this.RollbackLaunchRuntime(
                    runtimeState,
                    profile,
                    quote.RequiredEnergyWd,
                    previousLastLaunchTick,
                    previousCooldownEndTick);
                return this.FailAfterRollbackCommittedCargo(
                    preparedTransfer,
                    committedHandoffs,
                    this.AppendFailureDiagnostic(holderExportResult.Message, refrigeratedRollbackFailureAfterHolderExport),
                    quote);
            }

            bool medicalBayPatientExported = holderTransferTransaction.WasParticipantExported(
                ShuttleHolderLaunchTransferParticipants.MedicalBayKey);
            preparedTransfer.CommittedHandoffs = committedHandoffs;
            if (Prefs.DevMode && medicalBayPatientExported)
            {
                Log.Message("[CeleTech Shuttle] MedicalBay patient launch transfer step=spawn-leaving-skyfaller.");
            }

            ShuttleLaunchResult transferResult = this.SpawnPreparedLeavingSkyfallers(preparedTransfer, quote, payload);
            if (!transferResult.Success)
            {
                ShuttleHolderLaunchTransferTransactionResult holderRollbackResult;
                if (!holderTransferTransaction.TryRollbackExportedForLaunchFailure(
                    request.Host,
                    committedHandoffs,
                    preparedTransfer.Map,
                    "leaving skyfaller failure",
                    out holderRollbackResult))
                {
                    string refrigeratedRollbackFailure =
                        this.RollbackRefrigeratedCargoLaunchExportIfNeeded(
                            request,
                            ref refrigeratedCargoExported,
                            liveAssemblyState,
                            "leaving skyfaller failure after holder rollback failure");
                    string holderRollbackFailureMessage =
                        this.AppendFailureDiagnostic(holderRollbackResult.Message, refrigeratedRollbackFailure);
                    this.RollbackLaunchRuntime(
                        runtimeState,
                        profile,
                        quote.RequiredEnergyWd,
                        previousLastLaunchTick,
                        previousCooldownEndTick);
                    PreparedLeavingSkyfallerCleanupResult cleanupResult =
                        this.CleanupPreparedLeavingSkyfallers(
                            preparedTransfer,
                            request.Host,
                            committedHandoffs);
                    if (cleanupResult.Status == PreparedLeavingSkyfallerCleanupStatus.FatalBlocked)
                    {
                        return ShuttleLaunchResult.Failed(
                            holderRollbackFailureMessage +
                            " Prepared leaving skyfaller cleanup was blocked to avoid Vanish data loss: " +
                            (cleanupResult.FailureReason ?? "null"),
                            quote);
                    }

                    return ShuttleLaunchResult.Failed(
                        holderRollbackFailureMessage,
                        quote);
                }
                string refrigeratedRollbackFailureAfterSkyfaller =
                    this.RollbackRefrigeratedCargoLaunchExportIfNeeded(
                        request,
                        ref refrigeratedCargoExported,
                        liveAssemblyState,
                        "leaving skyfaller failure");
                string transferFailureMessage =
                    this.AppendFailureDiagnostic(transferResult.Message, refrigeratedRollbackFailureAfterSkyfaller);

                this.RollbackLaunchRuntime(
                    runtimeState,
                    profile,
                    quote.RequiredEnergyWd,
                    previousLastLaunchTick,
                    previousCooldownEndTick);
                LaunchCargoRollbackResult cargoRollbackAfterSkyfallerFailure =
                    this.RollbackCommittedCargo(preparedTransfer, committedHandoffs);
                if (cargoRollbackAfterSkyfallerFailure.Status == LaunchCargoRollbackStatus.FatalUnresolved)
                {
                    return ShuttleLaunchResult.Failed(
                        transferFailureMessage +
                        " Launch cargo rollback fatal unresolved; prepared leaving skyfaller cleanup was not run: " +
                        (cargoRollbackAfterSkyfallerFailure.FailureReason ?? "null"),
                        quote);
                }

                PreparedLeavingSkyfallerCleanupResult cleanupAfterRollback =
                    this.CleanupPreparedLeavingSkyfallers(
                        preparedTransfer,
                        request.Host,
                        committedHandoffs);
                if (cleanupAfterRollback.Status == PreparedLeavingSkyfallerCleanupStatus.FatalBlocked)
                {
                    return ShuttleLaunchResult.Failed(
                        transferFailureMessage +
                        " Prepared leaving skyfaller cleanup was blocked to avoid Vanish data loss: " +
                        (cleanupAfterRollback.FailureReason ?? "null"),
                        quote);
                }

                return string.IsNullOrEmpty(refrigeratedRollbackFailureAfterSkyfaller)
                    ? transferResult
                    : ShuttleLaunchResult.Failed(transferFailureMessage, quote);
            }

            LaunchCargoFinalizeResult finalizeResult = this.FinalizeCommittedCargo(preparedTransfer, committedHandoffs);
            if (finalizeResult.Status == LaunchCargoFinalizeStatus.FatalBlocked)
            {
                return ShuttleLaunchResult.Failed(
                    "[CeleTech Shuttle] Launch cargo finalize blocked after skyfaller spawn; launch success notification was not sent. " +
                    (finalizeResult.FailureReason ?? "null"),
                    quote);
            }

            if (Prefs.DevMode && medicalBayPatientExported)
            {
                Log.Message("[CeleTech Shuttle] MedicalBay patient launch transfer step=launch-success.");
            }

            if (runtimeState.PassengerBoardingIntents != null)
            {
                runtimeState.PassengerBoardingIntents.Clear();
            }

            this.NotifyModuleRuntimeLaunchSucceeded(request);
            CameraJumper.TryHideWorld();
            return ShuttleLaunchResult.Succeeded("CT_Shuttle_Launch_Succeeded".Translate().ToString(), quote);
            }
            finally
            {
                if (clearDevHabitatJoySpikeOnExit)
                {
                    ShuttleHolderLaunchTransferService.ClearDevHabitatJoyRealLaunchSpike(
                        request != null ? request.Host : null);
                }

                if (clearDevHabitatMixedSpikeOnExit)
                {
                    ShuttleHolderLaunchTransferService.ClearDevHabitatMixedRealLaunchSpike(
                        request != null ? request.Host : null);
                }

                if (clearDevMedicalBayRollbackSpikeOnExit)
                {
                    ShuttleHolderLaunchTransferService.ClearDevMedicalBayRealLaunchRollbackSpike(
                        request != null ? request.Host : null);
                }

                if (clearDevMedicalBayRestoreSpikeOnExit)
                {
                    ShuttleHolderLaunchTransferService.ClearDevMedicalBayRealLaunchRestoreSpike(
                        request != null ? request.Host : null);
                }
            }
        }

        internal ShuttleLaunchResult ExecuteDevMedicalBayPatientLaunchRollbackTest(
            ThingWithComps host,
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState)
        {
            if (!Prefs.DevMode)
            {
                return ShuttleLaunchResult.Failed("[CeleTech Shuttle] Dev MedicalBay real-launch rollback test is DevMode-only.");
            }

            Log.Message("[CeleTech Shuttle] Dev MedicalBay real-launch rollback test step=begin.");
            if (host == null || !host.Spawned || host.Map == null)
            {
                return ShuttleLaunchResult.Failed("[CeleTech Shuttle] Dev MedicalBay real-launch rollback test requires a spawned shuttle.");
            }

            Log.Message("[CeleTech Shuttle] Dev MedicalBay real-launch rollback test step=prevalidate.");
            string medicalBayFailureReason;
            if (!ShuttleHolderLaunchTransferService.CanUseMedicalBayPatientLaunchTransfer(
                host,
                out medicalBayFailureReason))
            {
                return ShuttleLaunchResult.Failed(medicalBayFailureReason);
            }

            if (!ShuttleHolderLaunchTransferService.NeedsMedicalBayPatientLaunchTransfer(host))
            {
                return ShuttleLaunchResult.Succeeded("[CeleTech Shuttle] Dev MedicalBay real-launch rollback test no-op: no MedicalBay patients are held.", null);
            }

            Log.Message("[CeleTech Shuttle] Dev MedicalBay real-launch rollback test step=create-synthetic-handoff.");
            ShuttleFlightEnergyQuote quote = new ShuttleFlightEnergyQuote
            {
                CanLaunch = true,
                AvailableEnergyWd = runtimeState != null ? runtimeState.Power.StoredEnergyWd : 0f,
                RequiredEnergyWd = 0f
            };
            List<ShuttleLaunchCargoHandoff> committedHandoffs;
            string handoffFailureReason;
            if (!this.TryCreateDevMedicalBaySyntheticHandoff(host, out committedHandoffs, out handoffFailureReason))
            {
                return ShuttleLaunchResult.Failed(handoffFailureReason, quote);
            }

            Log.Message("[CeleTech Shuttle] Dev MedicalBay real-launch rollback test synthetic handoff created. This test does not commit ordinary cargo or open world targeting.");
            Log.Message("[CeleTech Shuttle] Dev MedicalBay real-launch rollback test step=export-medicalbay.");
            if (!ShuttleHolderLaunchTransferService.TryExportMedicalBayPatientsToLaunchHandoff(
                host,
                committedHandoffs,
                out medicalBayFailureReason))
            {
                bool exportFailureRollbackSucceeded;
                bool exportFailurePawnsQuarantined;
                string exportFailureRecoveryNotice;
                bool exportFailureHandoffsClear =
                    ShuttleHolderLaunchTransferService.TryRollbackMedicalBayPatientLaunchExportWithRecovery(
                        host,
                        committedHandoffs,
                        out exportFailureRollbackSucceeded,
                        out exportFailurePawnsQuarantined,
                        out exportFailureRecoveryNotice);
                this.LogMedicalBayPatientRollbackRecoveryForDev(
                    exportFailureRollbackSucceeded,
                    exportFailurePawnsQuarantined,
                    exportFailureHandoffsClear,
                    "Dev MedicalBay direct export failure",
                    exportFailureRecoveryNotice);

                return ShuttleLaunchResult.Failed(
                    "[CeleTech Shuttle] Dev MedicalBay real-launch rollback test export failed. " +
                    medicalBayFailureReason +
                    " recovery=" +
                    (exportFailureRecoveryNotice ?? "null"),
                    quote);
            }

            Log.Message("[CeleTech Shuttle] Dev MedicalBay real-launch rollback test step=rollback-medicalbay.");
            string medicalBayRollbackNotice;
            bool medicalBayRollbackSucceeded;
            bool medicalBayPawnsQuarantined;
            bool medicalBayHandoffsClear =
                ShuttleHolderLaunchTransferService.TryRollbackMedicalBayPatientLaunchExportWithRecovery(
                    host,
                    committedHandoffs,
                    out medicalBayRollbackSucceeded,
                    out medicalBayPawnsQuarantined,
                    out medicalBayRollbackNotice);
            this.LogMedicalBayPatientRollbackRecoveryForDev(
                medicalBayRollbackSucceeded,
                medicalBayPawnsQuarantined,
                medicalBayHandoffsClear,
                "Dev MedicalBay direct forced rollback",
                medicalBayRollbackNotice);

            if (!medicalBayHandoffsClear)
            {
                return ShuttleLaunchResult.Failed(
                    "[CeleTech Shuttle] Dev MedicalBay real-launch rollback test stopped before ordinary cargo rollback. " +
                    (medicalBayRollbackNotice ?? "null"),
                    quote);
            }

            Log.Message("[CeleTech Shuttle] Dev MedicalBay real-launch rollback test step=completed.");
            return ShuttleLaunchResult.Succeeded(
                "[CeleTech Shuttle] Dev MedicalBay real-launch rollback test completed: patients exported to a synthetic real ActiveTransporter handoff and forced rollback ran. Ordinary cargo was not touched. " +
                (medicalBayRollbackNotice ?? "null"),
                quote);
        }

        private bool TryCreateDevMedicalBaySyntheticHandoff(
            ThingWithComps host,
            out List<ShuttleLaunchCargoHandoff> handoffs,
            out string failureReason)
        {
            handoffs = new List<ShuttleLaunchCargoHandoff>();
            failureReason = null;
            CompTransporter transporter = host != null ? host.TryGetComp<CompTransporter>() : null;
            if (host == null || transporter == null)
            {
                failureReason = "[CeleTech Shuttle] Dev MedicalBay real-launch rollback test requires the shuttle host CompTransporter.";
                return false;
            }

            ActiveTransporter activeTransporter = ThingMaker.MakeThing(ThingDefOf.ActiveDropPod, null) as ActiveTransporter;
            if (activeTransporter == null)
            {
                failureReason = "[CeleTech Shuttle] Dev MedicalBay real-launch rollback test could not create an ActiveTransporter.";
                return false;
            }

            activeTransporter.Contents = new ActiveTransporterInfo();
            if (activeTransporter.Contents == null || activeTransporter.Contents.innerContainer == null)
            {
                failureReason = "[CeleTech Shuttle] Dev MedicalBay real-launch rollback test could not create an ActiveTransporterInfo container.";
                return false;
            }

            // Do not call ActiveTransporterInfo.SetShuttle here. Vanilla SetShuttle
            // despawns and stores the shuttle Thing itself, which is only safe during
            // a real launch handoff. This synthetic handoff is a patient-transfer
            // test container only, so it must start empty and must not own the host.
            activeTransporter.Rotation = host.Rotation;
            int groupID = 0;

            ShuttleLaunchCargoHandoffPlan plan = new ShuttleLaunchCargoHandoffPlan(
                transporter,
                host.Position,
                groupID,
                ThingDefOf.ActiveDropPod,
                null,
                host.Rotation,
                true);
            handoffs.Add(new ShuttleLaunchCargoHandoff(plan, activeTransporter));
            return true;
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

        private string RollbackRefrigeratedCargoLaunchExportIfNeeded(
            ShuttleLaunchRequest request,
            ref bool refrigeratedCargoExported,
            ShuttleAssemblyState assemblyState,
            string failureContext)
        {
            if (!refrigeratedCargoExported || request == null)
            {
                return null;
            }

            refrigeratedCargoExported = false;
            string rollbackNotice;
            if (!ShuttleRefrigeratedCargoLaunchTransferService.TryRollbackExport(
                request.Host,
                assemblyState,
                request.RuntimeState,
                out rollbackNotice))
            {
                Log.Error("[CeleTech Shuttle] Refrigerated cargo launch export rollback failed after " +
                    (failureContext ?? "launch failure") +
                    ": " +
                    rollbackNotice);
                return "Refrigerated cargo launch export rollback failed: " +
                    (rollbackNotice ?? "null");
            }

            if (Prefs.DevMode && !string.IsNullOrEmpty(rollbackNotice))
            {
                Log.Warning("[CeleTech Shuttle] Refrigerated cargo launch export rolled back after " +
                    (failureContext ?? "launch failure") +
                    ": " +
                    rollbackNotice);
            }

            return null;
        }

        private string AppendFailureDiagnostic(string failureMessage, string diagnostic)
        {
            if (string.IsNullOrEmpty(diagnostic))
            {
                return failureMessage;
            }

            return (failureMessage ?? "Launch failed.") + " " + diagnostic;
        }

        private void RollbackHabitatLaunchExportsIfNeeded(
            ShuttleLaunchRequest request,
            List<ShuttleLaunchCargoHandoff> committedHandoffs,
            bool needsHabitatMixedLaunchTransfer,
            bool needsHabitatJoyLaunchTransfer,
            string failureContext)
        {
            if (request == null)
            {
                return;
            }

            if (needsHabitatMixedLaunchTransfer)
            {
                ShuttleHolderLaunchTransferService.TryRollbackHabitatMixedLaunchExport(
                    request.Host,
                    committedHandoffs,
                    out string mixedRollbackNotice);
                if (Prefs.DevMode && !string.IsNullOrEmpty(mixedRollbackNotice))
                {
                    Log.Warning("[CeleTech Shuttle] Habitat mixed launch export rolled back after " +
                        (failureContext ?? "launch failure") +
                        ": " +
                        mixedRollbackNotice);
                }

                return;
            }

            if (needsHabitatJoyLaunchTransfer)
            {
                ShuttleHolderLaunchTransferService.TryRollbackHabitatJoyLaunchExport(
                    request.Host,
                    committedHandoffs,
                    out string joyRollbackNotice);
                if (Prefs.DevMode && !string.IsNullOrEmpty(joyRollbackNotice))
                {
                    Log.Warning("[CeleTech Shuttle] Habitat joy launch export rolled back after " +
                        (failureContext ?? "launch failure") +
                        ": " +
                        joyRollbackNotice);
                }
            }

            ShuttleHolderLaunchTransferService.TryRollbackHabitatLivingLaunchExport(
                request.Host,
                committedHandoffs,
                out string habitatRollbackNotice);
            if (Prefs.DevMode && !string.IsNullOrEmpty(habitatRollbackNotice))
            {
                Log.Warning("[CeleTech Shuttle] Living Habitat launch export rolled back after " +
                    (failureContext ?? "launch failure") +
                    ": " +
                    habitatRollbackNotice);
            }
        }

        private void LogMedicalBayPatientRollbackRecoveryForDev(
            bool rollbackSucceeded,
            bool pawnsQuarantined,
            bool handoffsClear,
            string context,
            string notice)
        {
            string prefix = "[CeleTech Shuttle] Dev MedicalBay rollback recovery. context=" +
                (context ?? "null") +
                " ";
            if (rollbackSucceeded)
            {
                if (Prefs.DevMode)
                {
                    Log.Message(prefix + "MedicalBay rollback succeeded. " + (notice ?? "null"));
                }

                return;
            }

            if (pawnsQuarantined)
            {
                if (Prefs.DevMode)
                {
                    Log.Warning(prefix + "MedicalBay rollback failed but pawns were quarantined in MedicalBayPatientStagingThings. " +
                        (notice ?? "null"));
                }

                return;
            }

            if (!handoffsClear)
            {
                Log.Error(prefix +
                    "MedicalBay rollback failed and pawns remain in committed handoffs; normal cargo rollback was not allowed to silently consume them. " +
                    (notice ?? "null"));
                return;
            }

            if (Prefs.DevMode)
            {
                Log.Warning(prefix +
                    "MedicalBay rollback failed, but no MedicalBay manifest pawns remain in committed handoffs. Manifest remains active for diagnostics/recovery. " +
                    (notice ?? "null"));
            }
        }

        private void NotifyModuleRuntimeLaunchSucceeded(ShuttleLaunchRequest request)
        {
            if (this.moduleRuntimeCoordinator == null || request == null)
            {
                return;
            }

            try
            {
                ShuttleModuleRuntimeStateBucket moduleStates = request.RuntimeState != null
                    ? request.RuntimeState.Modules
                    : null;
                this.moduleRuntimeCoordinator.NotifyLaunchSucceeded(request.AssemblySnapshot, moduleStates);
            }
            catch (Exception exception)
            {
                Log.Error("[CeleTech Shuttle] Module runtime launch success notification failed after launch success. Exception: " + exception);
            }
        }

        private ShuttleLaunchPayload BuildLaunchPayload(
            int profileRevision,
            ShuttleLaunchRequest request,
            ShuttleFlightEnergyQuote quote)
        {
            ShuttleLaunchPayload payload = new ShuttleLaunchPayload();
            payload.Host = request.Host;
            payload.OriginTile = request.Host != null ? request.Host.Tile : default(PlanetTile);
            payload.DestinationTile = request.DestinationTile;
            payload.ArrivalAction = request.ArrivalAction;
            payload.WorldObjectDef = request.WorldObjectDef;
            payload.LeavingSkyfallerDef = request.LeavingSkyfallerDef;
            payload.IncomingSkyfallerDef = DefDatabase<ThingDef>.GetNamedSilentFail(IncomingSkyfallerDefName);
            payload.EnergySpentWd = quote != null ? quote.RequiredEnergyWd : 0f;
            payload.LoadedCargoMassKg = quote != null ? quote.LoadedCargoMassKg : 0f;
            payload.ProfileRevision = profileRevision;
            return payload;
        }

        private ShuttleLaunchResult TryPrepareLaunchTransfer(
            ShuttleLaunchRequest request,
            ShuttleFlightEnergyQuote quote,
            out PreparedLaunchTransfer preparedTransfer)
        {
            preparedTransfer = null;

            if (request.LeavingSkyfallerDef == null)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_LeavingSkyfallerDefUnavailable".Translate().ToString(), quote);
            }

            if (request.ActiveTransporterDef == null)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_ActiveTransporterDefUnavailable".Translate().ToString(), quote);
            }

            if (request.WorldObjectDef == null)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_WorldObjectDefUnavailable".Translate().ToString(), quote);
            }

            if (this.skyfallerFactory == null)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_SkyfallerFactoryUnavailable".Translate().ToString(), quote);
            }

            if (this.cargoBackend == null)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_CargoBackendUnavailable".Translate().ToString(), quote);
            }

            ThingWithComps host = request.Host;
            Map map = host.Map;

            List<ShuttleLaunchCargoHandoffPlan> plans;
            string failureReason;
            try
            {
                if (!this.cargoBackend.PlanLaunchCargoHandoffs(
                    host,
                    map,
                    request.ActiveTransporterDef,
                    out plans,
                    out failureReason))
                {
                    return ShuttleLaunchResult.Failed(failureReason, quote);
                }
            }
            catch (Exception exception)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_CargoHandoffFailed".Translate(exception.Message).ToString(), quote);
            }

            if (plans == null || plans.Count == 0)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_NoTransporterToLaunch".Translate().ToString(), quote);
            }

            for (int i = 0; i < plans.Count; i++)
            {
                ShuttleLaunchCargoHandoffPlan plan = plans[i];
                if (plan == null || plan.SourceTransporter == null)
                {
                    return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_HandoffPlanInvalid".Translate().ToString(), quote);
                }

                if (!plan.SpawnCell.IsValid || !plan.SpawnCell.InBounds(map))
                {
                    return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_SpawnCellInvalid".Translate().ToString(), quote);
                }
            }

            preparedTransfer = new PreparedLaunchTransfer(map, plans);
            return ShuttleLaunchResult.Succeeded("CT_Shuttle_Launch_TransferPrepared".Translate().ToString(), quote);
        }

        private ShuttleLaunchResult TryCommitLaunchTransfer(
            PreparedLaunchTransfer preparedTransfer,
            ShuttleFlightEnergyQuote quote,
            out List<ShuttleLaunchCargoHandoff> committedHandoffs)
        {
            committedHandoffs = null;

            if (preparedTransfer == null || preparedTransfer.Map == null || preparedTransfer.Plans == null || preparedTransfer.Plans.Count == 0)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_PreparedTransferUnavailable".Translate().ToString(), quote);
            }

            if (this.cargoBackend == null)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_CargoBackendUnavailable".Translate().ToString(), quote);
            }

            string failureReason;
            try
            {
                if (!this.cargoBackend.CommitLaunchCargoHandoffs(
                    preparedTransfer.Plans,
                    preparedTransfer.Map,
                    out committedHandoffs,
                    out failureReason))
                {
                    return ShuttleLaunchResult.Failed(failureReason, quote);
                }
            }
            catch (Exception exception)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_CargoCommitFailed".Translate(exception.Message).ToString(), quote);
            }

            if (committedHandoffs == null || committedHandoffs.Count == 0)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_NoTransporterToLaunch".Translate().ToString(), quote);
            }

            return ShuttleLaunchResult.Succeeded("CT_Shuttle_Launch_CargoCommitted".Translate().ToString(), quote);
        }

        private ShuttleLaunchResult ValidateCommittedHandoffGroupIDs(
            List<ShuttleLaunchCargoHandoff> handoffs,
            ShuttleFlightEnergyQuote quote)
        {
            if (handoffs == null || handoffs.Count == 0)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_PreparedHandoffsUnavailable".Translate().ToString(), quote);
            }

            for (int i = 0; i < handoffs.Count; i++)
            {
                ShuttleLaunchCargoHandoff handoff = handoffs[i];
                if (handoff == null || handoff.GroupID < 0)
                {
                    return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_InvalidTransporterGroupID".Translate().ToString(), quote);
                }
            }

            return ShuttleLaunchResult.Succeeded("CT_Shuttle_Launch_CargoCommitted".Translate().ToString(), quote);
        }

        private ShuttleLaunchResult SpawnPreparedLeavingSkyfallers(
            PreparedLaunchTransfer preparedTransfer,
            ShuttleFlightEnergyQuote quote,
            ShuttleLaunchPayload payload)
        {
            if (preparedTransfer == null || preparedTransfer.Map == null)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_PreparedTransferUnavailable".Translate().ToString(), quote);
            }

            if (preparedTransfer.CommittedHandoffs == null || preparedTransfer.CommittedHandoffs.Count == 0)
            {
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_PreparedHandoffsUnavailable".Translate().ToString(), quote);
            }

            try
            {
                for (int i = 0; i < preparedTransfer.CommittedHandoffs.Count; i++)
                {
                    ShuttleLaunchCargoHandoff handoff = preparedTransfer.CommittedHandoffs[i];
                    if (handoff == null || handoff.GroupID < 0)
                    {
                        return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_InvalidTransporterGroupID".Translate().ToString(), quote);
                    }

                    payload.GroupID = handoff.GroupID;
                    FlyShipLeaving leaving = this.skyfallerFactory.CreateLeavingSkyfaller(payload, handoff.ActiveTransporter);
                    if (leaving == null)
                    {
                        return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_SkyfallerCreateFailed".Translate().ToString(), quote);
                    }

                    preparedTransfer.PreparedLeavingSkyfallers.Add(new PreparedLeavingSkyfaller(leaving, handoff.SpawnCell));
                }
            }
            catch (Exception exception)
            {
                string reason = BuildUserFacingExceptionMessage(exception);
                Log.Error("[CeleTech Shuttle] Leaving skyfaller create failed before spawn. " +
                    BuildExceptionDetails(exception));
                return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_SkyfallerCreateFailedWithReason".Translate(reason).ToString(), quote);
            }

            Map previousCurrentMap = Current.Game != null ? Current.Game.CurrentMap : null;
            try
            {
                if (Current.Game != null)
                {
                    Current.Game.CurrentMap = preparedTransfer.Map;
                }

                try
                {
                    for (int i = 0; i < preparedTransfer.PreparedLeavingSkyfallers.Count; i++)
                    {
                        PreparedLeavingSkyfaller prepared = preparedTransfer.PreparedLeavingSkyfallers[i];
                        prepared.SpawnedThing = GenSpawn.Spawn(prepared.Leaving, prepared.SpawnCell, preparedTransfer.Map, WipeMode.Vanish);
                    }
                }
                catch (Exception exception)
                {
                    return ShuttleLaunchResult.Failed("CT_Shuttle_Launch_Failed_SkyfallerSpawnFailed".Translate(exception.Message).ToString(), quote);
                }
            }
            finally
            {
                if (Current.Game != null)
                {
                    Current.Game.CurrentMap = previousCurrentMap;
                }
            }

            return ShuttleLaunchResult.Succeeded("CT_Shuttle_Launch_SkyfallerSpawned".Translate().ToString(), quote);
        }

        private static string BuildUserFacingExceptionMessage(Exception exception)
        {
            Exception root = UnwrapException(exception);
            if (root == null)
            {
                return "unknown exception";
            }

            return root.GetType().Name + ": " + (root.Message ?? "<null>");
        }

        private static string BuildExceptionDetails(Exception exception)
        {
            if (exception == null)
            {
                return "exception=<null>";
            }

            string details = "exception=" + exception.GetType().FullName +
                ": " + (exception.Message ?? "<null>");
            Exception inner = exception.InnerException;
            int depth = 0;
            while (inner != null && depth < 8)
            {
                details += "\ninner[" + depth.ToString() + "]=" +
                    inner.GetType().FullName + ": " +
                    (inner.Message ?? "<null>");
                inner = inner.InnerException;
                depth++;
            }

            return details + "\nstack=" + exception;
        }

        private static Exception UnwrapException(Exception exception)
        {
            Exception current = exception;
            int depth = 0;
            while (current != null && current.InnerException != null && depth < 8)
            {
                current = current.InnerException;
                depth++;
            }

            return current ?? exception;
        }

        private ShuttleLaunchResult FailAfterRollbackCommittedCargo(
            PreparedLaunchTransfer preparedTransfer,
            List<ShuttleLaunchCargoHandoff> committedHandoffs,
            string failureMessage,
            ShuttleFlightEnergyQuote quote)
        {
            LaunchCargoRollbackResult rollbackResult = this.RollbackCommittedCargo(
                preparedTransfer,
                committedHandoffs);
            if (rollbackResult.Status == LaunchCargoRollbackStatus.FatalUnresolved)
            {
                return ShuttleLaunchResult.Failed(
                    (failureMessage ?? "Launch failed.") +
                    " Launch cargo rollback fatal unresolved: " +
                    (rollbackResult.FailureReason ?? "null"),
                    quote);
            }

            return ShuttleLaunchResult.Failed(failureMessage, quote);
        }

        private bool TryNormalizePassengerWeaponCargo(
            List<ShuttleLaunchCargoHandoff> committedHandoffs,
            out string failureReason)
        {
            failureReason = null;
            if (committedHandoffs == null || committedHandoffs.Count == 0)
            {
                failureReason = "Passenger weapon cargo transfer has no committed flight container.";
                return false;
            }

            for (int i = 0; i < committedHandoffs.Count; i++)
            {
                ShuttleLaunchCargoHandoff handoff = committedHandoffs[i];
                if (handoff == null ||
                    handoff.ActiveTransporter == null ||
                    handoff.ActiveTransporter.Contents == null)
                {
                    failureReason = "Passenger weapon cargo transfer found an invalid committed handoff.";
                    return false;
                }

                int movedWeaponCount;
                if (!ShuttlePassengerWeaponCargoTransfer.TryMoveFromTransporter(
                    handoff.ActiveTransporter.Contents,
                    out movedWeaponCount,
                    out failureReason))
                {
                    return false;
                }
            }

            return true;
        }

        private LaunchCargoRollbackResult RollbackCommittedCargo(PreparedLaunchTransfer preparedTransfer, List<ShuttleLaunchCargoHandoff> committedHandoffs)
        {
            if (this.cargoBackend == null || committedHandoffs == null || committedHandoffs.Count == 0)
            {
                return LaunchCargoRollbackResult.Success();
            }

            Map map = preparedTransfer != null ? preparedTransfer.Map : null;
            return this.cargoBackend.RollbackLaunchCargoHandoffs(committedHandoffs, map);
        }

        private LaunchCargoFinalizeResult FinalizeCommittedCargo(PreparedLaunchTransfer preparedTransfer, List<ShuttleLaunchCargoHandoff> committedHandoffs)
        {
            if (this.cargoBackend == null)
            {
                return LaunchCargoFinalizeResult.Success();
            }

            Map map = preparedTransfer != null ? preparedTransfer.Map : null;
            return this.cargoBackend.FinalizeLaunchCargoHandoffs(committedHandoffs, map);
        }

        private PreparedLeavingSkyfallerCleanupResult CleanupPreparedLeavingSkyfallers(
            PreparedLaunchTransfer preparedTransfer,
            ThingWithComps shuttleHost,
            List<ShuttleLaunchCargoHandoff> committedHandoffs)
        {
            if (preparedTransfer == null || preparedTransfer.PreparedLeavingSkyfallers == null)
            {
                return PreparedLeavingSkyfallerCleanupResult.Success();
            }

            bool anyRecovered = false;
            CompShuttleHolderLaunchTransferState recoveryState = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleHolderLaunchTransferState>()
                : null;
            for (int i = 0; i < preparedTransfer.PreparedLeavingSkyfallers.Count; i++)
            {
                PreparedLeavingSkyfaller prepared = preparedTransfer.PreparedLeavingSkyfallers[i];
                Thing spawnedThing = prepared != null ? prepared.SpawnedThing : null;
                if (spawnedThing == null && prepared != null && prepared.Leaving != null && prepared.Leaving.Spawned)
                {
                    spawnedThing = prepared.Leaving;
                }

                if (spawnedThing != null && spawnedThing.Spawned && !spawnedThing.Destroyed)
                {
                    string preCleanupDump;
                    if (this.ContainsRecoverableHeldThings(spawnedThing, out preCleanupDump))
                    {
                        string recoveryFailureReason;
                        if (!this.TryRecoverHeldThingsBeforeVanish(
                            spawnedThing,
                            preparedTransfer.Map,
                            prepared != null ? prepared.SpawnCell : IntVec3.Invalid,
                            recoveryState,
                            committedHandoffs,
                            out recoveryFailureReason))
                        {
                            string blockedDump;
                            this.ContainsRecoverableHeldThings(spawnedThing, out blockedDump);
                            string failureReason = "Prepared leaving skyfaller still contains recoverable Things; refusing DestroyMode.Vanish. preCleanup=" +
                                (preCleanupDump ?? "null") +
                                " remaining=" +
                                (blockedDump ?? "null") +
                                " recovery=" +
                                (recoveryFailureReason ?? "null");
                            Log.Error("[CeleTech Shuttle] " + failureReason);
                            return PreparedLeavingSkyfallerCleanupResult.Fatal(failureReason);
                        }

                        anyRecovered = true;
                    }

                    string finalDump;
                    if (this.ContainsRecoverableHeldThings(spawnedThing, out finalDump))
                    {
                        string failureReason = "Prepared leaving skyfaller still contains recoverable Things after recovery; refusing DestroyMode.Vanish. remaining=" +
                            (finalDump ?? "null");
                        Log.Error("[CeleTech Shuttle] " + failureReason);
                        return PreparedLeavingSkyfallerCleanupResult.Fatal(failureReason);
                    }

                    spawnedThing.Destroy(DestroyMode.Vanish);
                }
            }

            return anyRecovered
                ? PreparedLeavingSkyfallerCleanupResult.RecoveredAndDestroyed()
                : PreparedLeavingSkyfallerCleanupResult.Success();
        }

        private bool ContainsRecoverableHeldThings(Thing thing, out string debugDump)
        {
            return ShuttleHeldThingScanner.ContainsRecoverableHeldThings(thing, out debugDump);
        }

        private bool TryRecoverHeldThingsBeforeVanish(
            Thing thing,
            Map map,
            IntVec3 cell,
            CompShuttleHolderLaunchTransferState recoveryState,
            List<ShuttleLaunchCargoHandoff> committedHandoffs,
            out string failureReason)
        {
            failureReason = null;
            List<ShuttleHeldThingOwnerRecord> owners =
                ShuttleHeldThingScanner.CollectHeldThingOwners(thing);

            bool allRecovered = true;
            List<string> failures = new List<string>();
            for (int i = owners.Count - 1; i >= 0; i--)
            {
                ShuttleHeldThingOwnerRecord ownerRecord = owners[i];
                ThingOwner owner = ownerRecord != null ? ownerRecord.Owner : null;
                if (owner == null)
                {
                    continue;
                }

                for (int j = owner.Count - 1; j >= 0; j--)
                {
                    Thing heldThing = owner[j];
                    if (!ShuttleHeldThingScanner.IsRecoverableHeldThing(heldThing))
                    {
                        continue;
                    }

                    string recoverFailure;
                    if (this.TryRecoverSingleHeldThingBeforeVanish(
                        heldThing,
                        owner,
                        ownerRecord.Path,
                        map,
                        cell,
                        recoveryState,
                        committedHandoffs,
                        out recoverFailure))
                    {
                        continue;
                    }

                    allRecovered = false;
                    failures.Add(recoverFailure ?? this.DescribeThingForCleanup(heldThing));
                }
            }

            if (!allRecovered)
            {
                failureReason = string.Join(" | ", failures.ToArray());
            }

            return allRecovered;
        }

        private bool TryRecoverSingleHeldThingBeforeVanish(
            Thing thing,
            ThingOwner currentOwner,
            string ownerPath,
            Map map,
            IntVec3 cell,
            CompShuttleHolderLaunchTransferState recoveryState,
            List<ShuttleLaunchCargoHandoff> committedHandoffs,
            out string failureReason)
        {
            failureReason = null;
            if (thing == null || thing.Destroyed)
            {
                failureReason = "Recoverable held thing is null or destroyed. owner=" + (ownerPath ?? "null");
                return false;
            }

            ShuttleLaunchCargoHandoff matchingHandoff = this.FindHandoffForHeldThing(
                thing,
                currentOwner,
                committedHandoffs);
            if (matchingHandoff != null &&
                matchingHandoff.SourceTransporter != null &&
                matchingHandoff.SourceTransporter.parent == thing)
            {
                if (this.TryDropHeldThingToMap(
                    currentOwner,
                    thing,
                    map,
                    matchingHandoff.SpawnCell.IsValid ? matchingHandoff.SpawnCell : cell))
                {
                    Log.Warning("[CeleTech Shuttle] Recovered shuttle host from prepared leaving skyfaller before Vanish cleanup. thing=" +
                        this.DescribeThingForCleanup(thing));
                    return true;
                }
            }

            ThingOwner preferredOwner = this.ResolvePreferredCleanupRecoveryOwner(
                thing,
                currentOwner,
                committedHandoffs);
            if (preferredOwner != null && preferredOwner.TryAddOrTransfer(thing, false))
            {
                Log.Warning("[CeleTech Shuttle] Recovered held thing from prepared leaving skyfaller to its launch source before Vanish cleanup. thing=" +
                    this.DescribeThingForCleanup(thing) +
                    " owner=" +
                    (ownerPath ?? "null"));
                return true;
            }

            if (recoveryState != null)
            {
                ShuttleTransferRecoveryStatus recoveryStatus;
                string recoveryFailureReason;
                if (recoveryState.TryRecoverTransferThing(
                    thing,
                    "Prepared leaving skyfaller cleanup before DestroyMode.Vanish. owner=" +
                        (ownerPath ?? "null"),
                    null,
                    null,
                    map,
                    cell,
                    out recoveryStatus,
                    out recoveryFailureReason))
                {
                    Log.Warning("[CeleTech Shuttle] Recovered held thing from prepared leaving skyfaller before Vanish cleanup. thing=" +
                        this.DescribeThingForCleanup(thing) +
                        " status=" +
                        recoveryStatus);
                    return true;
                }

                failureReason = recoveryFailureReason;
            }

            if (this.TryDropHeldThingToMap(currentOwner, thing, map, cell))
            {
                Log.Warning("[CeleTech Shuttle] Dropped held thing from prepared leaving skyfaller before Vanish cleanup. thing=" +
                    this.DescribeThingForCleanup(thing));
                return true;
            }

            failureReason = "Could not recover held thing before prepared leaving skyfaller Vanish cleanup. thing=" +
                this.DescribeThingForCleanup(thing) +
                " owner=" +
                (ownerPath ?? "null") +
                " recovery=" +
                (failureReason ?? "null");
            return false;
        }

        private ShuttleLaunchCargoHandoff FindHandoffForHeldThing(
            Thing thing,
            ThingOwner currentOwner,
            List<ShuttleLaunchCargoHandoff> committedHandoffs)
        {
            if (thing == null || committedHandoffs == null)
            {
                return null;
            }

            for (int i = 0; i < committedHandoffs.Count; i++)
            {
                ShuttleLaunchCargoHandoff handoff = committedHandoffs[i];
                if (handoff == null ||
                    handoff.ActiveTransporter == null ||
                    handoff.ActiveTransporter.Contents == null)
                {
                    continue;
                }

                ThingOwner activeContents = handoff.ActiveTransporter.Contents.innerContainer;
                if ((currentOwner != null && object.ReferenceEquals(currentOwner, activeContents)) ||
                    (activeContents != null && activeContents.Contains(thing)))
                {
                    return handoff;
                }
            }

            return null;
        }

        private ThingOwner ResolvePreferredCleanupRecoveryOwner(
            Thing thing,
            ThingOwner currentOwner,
            List<ShuttleLaunchCargoHandoff> committedHandoffs)
        {
            ShuttleLaunchCargoHandoff handoff = this.FindHandoffForHeldThing(
                thing,
                currentOwner,
                committedHandoffs);
            CompTransporter sourceTransporter = handoff != null ? handoff.SourceTransporter : null;
            if (sourceTransporter == null || sourceTransporter.parent == null || sourceTransporter.parent == thing)
            {
                return null;
            }

            return sourceTransporter.GetDirectlyHeldThings();
        }

        private bool TryDropHeldThingToMap(
            ThingOwner owner,
            Thing thing,
            Map map,
            IntVec3 cell)
        {
            if (thing == null || thing.Destroyed || map == null || !cell.IsValid || !cell.InBounds(map))
            {
                return false;
            }

            if (thing.Spawned)
            {
                return true;
            }

            Thing resultingThing;
            if (owner != null && owner.Contains(thing))
            {
                return owner.TryDrop(
                    thing,
                    cell,
                    map,
                    ThingPlaceMode.Near,
                    out resultingThing,
                    null,
                    null);
            }

            try
            {
                return GenPlace.TryPlaceThing(thing, cell, map, ThingPlaceMode.Near);
            }
            catch (Exception exception)
            {
                Log.Error("[CeleTech Shuttle] Prepared leaving skyfaller cleanup map recovery threw: " + exception);
                return false;
            }
        }

        private string DescribeThingForCleanup(Thing thing)
        {
            return ShuttleHeldThingScanner.DescribeThing(thing);
        }

        private void RollbackLaunchRuntime(
            ShuttleRuntimeState runtimeState,
            ShuttleProfile profile,
            float refundEnergyWd,
            int previousLastLaunchTick,
            int previousCooldownEndTick)
        {
            if (runtimeState == null)
            {
                return;
            }

            runtimeState.EnsureInitialized();
            if (refundEnergyWd > 0f)
            {
                float capacityWd = this.GetEnergyStorageCapacityWd(profile);
                runtimeState.Power.StoredEnergyWd = this.Clamp(runtimeState.Power.StoredEnergyWd + refundEnergyWd, 0f, capacityWd);
            }

            runtimeState.Launch.LastLaunchTick = previousLastLaunchTick;
            runtimeState.Launch.CooldownEndTick = previousCooldownEndTick;
        }

        private float GetEnergyStorageCapacityWd(ShuttleProfile profile)
        {
            return profile != null && profile.Power != null ? this.Max(0f, profile.Power.EnergyStorageCapacityWd) : 0f;
        }

        private float Clamp(float value, float min, float max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }

        private float Max(float a, float b)
        {
            return a > b ? a : b;
        }

        private sealed class PreparedLaunchTransfer
        {
            public PreparedLaunchTransfer(Map map, List<ShuttleLaunchCargoHandoffPlan> plans)
            {
                this.Map = map;
                this.Plans = plans;
                this.PreparedLeavingSkyfallers = new List<PreparedLeavingSkyfaller>();
            }

            public Map Map { get; private set; }
            public List<ShuttleLaunchCargoHandoffPlan> Plans { get; private set; }
            public List<ShuttleLaunchCargoHandoff> CommittedHandoffs { get; set; }
            public List<PreparedLeavingSkyfaller> PreparedLeavingSkyfallers { get; private set; }
        }

        private sealed class PreparedLeavingSkyfaller
        {
            public PreparedLeavingSkyfaller(FlyShipLeaving leaving, IntVec3 spawnCell)
            {
                this.Leaving = leaving;
                this.SpawnCell = spawnCell;
            }

            public FlyShipLeaving Leaving { get; private set; }
            public IntVec3 SpawnCell { get; private set; }
            public Thing SpawnedThing { get; set; }
        }

        private enum PreparedLeavingSkyfallerCleanupStatus
        {
            Success,
            RecoveredAndDestroyed,
            FatalBlocked
        }

        private sealed class PreparedLeavingSkyfallerCleanupResult
        {
            private PreparedLeavingSkyfallerCleanupResult(
                PreparedLeavingSkyfallerCleanupStatus status,
                string failureReason)
            {
                this.Status = status;
                this.FailureReason = failureReason;
            }

            public PreparedLeavingSkyfallerCleanupStatus Status { get; private set; }
            public string FailureReason { get; private set; }

            public static PreparedLeavingSkyfallerCleanupResult Success()
            {
                return new PreparedLeavingSkyfallerCleanupResult(
                    PreparedLeavingSkyfallerCleanupStatus.Success,
                    null);
            }

            public static PreparedLeavingSkyfallerCleanupResult RecoveredAndDestroyed()
            {
                return new PreparedLeavingSkyfallerCleanupResult(
                    PreparedLeavingSkyfallerCleanupStatus.RecoveredAndDestroyed,
                    null);
            }

            public static PreparedLeavingSkyfallerCleanupResult Fatal(string failureReason)
            {
                return new PreparedLeavingSkyfallerCleanupResult(
                    PreparedLeavingSkyfallerCleanupStatus.FatalBlocked,
                    failureReason);
            }
        }

    }
}
