using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.API.Runtime;
using Verse;

namespace CeleTech.ShuttleExtension.AdditionalModule.Compatibility.BadHygieneLite
{
    internal sealed class BHLiteModuleRuntime : ShuttleExternalModuleRuntimeSystemBase
    {
        private readonly CT_Shuttle_BHLiteIntegrationDef integration;
        private readonly BHLiteReflectionBridge bridge;

        public BHLiteModuleRuntime(CT_Shuttle_BHLiteIntegrationDef integration, BHLiteReflectionBridge bridge)
        {
            this.integration = integration;
            this.bridge = bridge;
        }

        public override int TickInterval
        {
            get
            {
                return BHLiteCompatibility.IsLiteLoaded &&
                    !BHLiteCompatibility.FullDbhHasPriority
                        ? 60
                        : 0;
            }
        }

        public override int StateSchemaVersion
        {
            get { return 2; }
        }

        public override bool AppliesTo(ShuttleExternalModuleInfo module)
        {
            return module != null &&
                BHLiteIntegrationResolver.ModuleHasLiteHygieneExtension(module.ModuleDefName, integration);
        }

        public override bool AppliesTo(ShuttleExternalLaunchModuleInfo module)
        {
            return module != null &&
                BHLiteIntegrationResolver.ModuleHasLiteHygieneExtension(module.ModuleDefName, integration);
        }

        public override void Initialize(ShuttleExternalRuntimeContext context)
        {
            if (context == null)
            {
                return;
            }

            WriteCommonState(context);
            context.State.SetBool("liteRuntimeActive", false);
            context.State.SetBool("liteBridgeResolved", false);
            context.State.SetBool("liteHygieneResolved", false);
            context.State.SetBool("liteBladderResolved", false);
            context.State.SetBool("liteThirstResolved", false);
            context.State.SetBool("liteThirstServiceEnabled", false);
            context.State.SetInt("liteServiceableOccupants", 0);
            context.State.SetBool("liteHostInfoAvailable", false);
            context.State.SetBool("liteHostSpawned", false);
            context.State.SetString("liteLastResult", "Initialized.");
            context.State.SetString("liteBlockedReason", null);
            context.State.SetString("liteMissingNeedDefs", null);
            context.State.SetString("liteOptionalNeedDiagnostic", null);
            context.State.SetInt("liteLastDiagnosticTick", -1);
            context.State.SetInt("liteNextDiagnosticTick", context.TicksGame + DiagnosticIntervalTicks);
            context.State.SetBool("liteServiceEnabled", ServiceEnabled);
            context.State.SetInt("liteLastServiceTick", -1);
            context.State.SetInt("liteNextServiceTick", context.TicksGame + ServiceIntervalTicks);
            context.State.SetInt("liteServicedPawns", 0);
            context.State.SetInt("liteServiceActions", 0);
            context.State.SetInt("liteHygieneServices", 0);
            context.State.SetInt("liteBladderServices", 0);
            context.State.SetInt("liteServiceFailures", 0);
            context.State.SetString("liteLastServicedPawnLabels", null);
            context.State.SetString("liteLastServiceFailure", null);
            context.State.SetFloat("litePowerDemandWatts", PowerDemandWatts);
        }

        public override void Reconcile(ShuttleExternalRuntimeContext context)
        {
            if (context == null)
            {
                return;
            }

            RunDiagnostic(context, false, false);
        }

        public override void Tick(ShuttleExternalRuntimeContext context)
        {
            if (context == null)
            {
                return;
            }

            int nextDiagnosticTick;
            if (!context.State.TryGetInt("liteNextDiagnosticTick", out nextDiagnosticTick))
            {
                nextDiagnosticTick = context.TicksGame;
            }

            int nextServiceTick;
            if (!context.State.TryGetInt("liteNextServiceTick", out nextServiceTick))
            {
                nextServiceTick = context.TicksGame;
            }

            bool diagnosticDue = context.TicksGame >= nextDiagnosticTick;
            bool serviceDue = context.TicksGame >= nextServiceTick;
            if (!diagnosticDue && !serviceDue)
            {
                return;
            }

            RunDiagnostic(context, diagnosticDue, serviceDue);
        }

        public override void CollectPowerDemand(ShuttleExternalPowerDemandContext context)
        {
            if (context == null ||
                !ServiceEnabled ||
                PowerDemandWatts <= 0f ||
                !BHLiteCompatibility.IsLiteLoaded ||
                BHLiteCompatibility.FullDbhHasPriority)
            {
                return;
            }

            bool runtimeActive;
            if (context.State == null ||
                !context.State.TryGetBool("liteRuntimeActive", out runtimeActive) ||
                !runtimeActive)
            {
                return;
            }

            context.AddInternalPowerDemandWatts(PowerDemandWatts);
        }

        private void RunDiagnostic(ShuttleExternalRuntimeContext context, bool scheduled, bool allowService)
        {
            WriteCommonState(context);
            if (scheduled)
            {
                context.State.SetInt("liteLastDiagnosticTick", context.TicksGame);
                context.State.SetInt("liteNextDiagnosticTick", context.TicksGame + DiagnosticIntervalTicks);
            }

            if (allowService)
            {
                context.State.SetInt("liteNextServiceTick", context.TicksGame + ServiceIntervalTicks);
            }

            if (BHLiteCompatibility.FullDbhHasPriority)
            {
                BHLiteCompatibility.LogFullPriorityOnce();
                ClearDiagnosticState(context);
                context.State.SetString("liteBlockedReason", "Bad Hygiene Lite hidden because full Dubs Bad Hygiene is active.");
                context.State.SetString("liteLastResult", "Full Dubs Bad Hygiene is active; Lite runtime diagnostic only.");
                return;
            }

            if (!BHLiteCompatibility.IsLiteLoaded)
            {
                ClearDiagnosticState(context);
                context.State.SetString("liteBlockedReason", "Bad Hygiene Lite is not active.");
                context.State.SetString("liteLastResult", "Bad Hygiene Lite is not active.");
                return;
            }

            bool resolved = bridge != null && bridge.Resolve();
            context.State.SetBool("liteBridgeResolved", resolved);
            context.State.SetBool("liteHygieneResolved", bridge != null && bridge.HygieneResolved);
            context.State.SetBool("liteBladderResolved", bridge != null && bridge.BladderResolved);
            context.State.SetBool("liteThirstResolved", bridge != null && bridge.ThirstResolved);
            context.State.SetBool("liteThirstServiceEnabled", false);
            context.State.SetString("liteMissingNeedDefs", bridge != null ? bridge.MissingRequiredNeedDefs : null);
            context.State.SetString("liteOptionalNeedDiagnostic", bridge != null ? bridge.OptionalNeedDiagnostic : null);

            if (!resolved)
            {
                ClearOperationalState(context);
                context.State.SetString("liteBlockedReason", "Bad Hygiene Lite required need defs are missing.");
                context.State.SetString("liteLastResult", "Bad Hygiene Lite need diagnostic failed.");
                return;
            }

            int serviceableOccupants = CountServiceableHumanlikeOccupants(context);
            context.State.SetInt("liteServiceableOccupants", serviceableOccupants);
            WriteHostDiagnostic(context);
            context.State.SetBool("liteServiceEnabled", ServiceEnabled);
            context.State.SetFloat("litePowerDemandWatts", PowerDemandWatts);
            if (!ServiceEnabled)
            {
                ClearOperationalState(context);
                context.State.SetString("liteBlockedReason", "Bad Hygiene Lite service is disabled.");
                context.State.SetString("liteLastResult", "Bad Hygiene Lite service is disabled.");
                return;
            }

            if (RequireInternalBusPowered && !context.InternalBusPowered)
            {
                ClearOperationalState(context);
                context.State.SetString("liteBlockedReason", "Internal shuttle bus is offline.");
                context.State.SetString("liteLastResult", "Internal shuttle bus is offline.");
                return;
            }

            if (!context.SupportsOccupantRead)
            {
                ClearOperationalState(context);
                context.State.SetString("liteBlockedReason", "Waiting for public shuttle occupant API.");
                context.State.SetString("liteLastResult", "Waiting for public shuttle occupant API.");
                return;
            }

            context.State.SetBool("liteRuntimeActive", true);
            context.State.SetString("liteBlockedReason", null);
            if (allowService)
            {
                ServiceOccupants(context);
            }
            else
            {
                context.State.SetString("liteLastResult", "Bad Hygiene Lite service ready.");
            }
        }

        private void WriteCommonState(ShuttleExternalRuntimeContext context)
        {
            context.State.SetBool("liteLoaded", BHLiteCompatibility.IsLiteLoaded);
            context.State.SetBool("fullDbhLoaded", BHLiteCompatibility.IsFullDbhLoaded);
            context.State.SetBool("liteHiddenByFullDbh", BHLiteCompatibility.FullDbhHasPriority);
            context.State.SetBool("internalBusPowered", context.InternalBusPowered);
            context.State.SetString("liteAvailabilityReason", BHLiteCompatibility.ResolveAvailabilityReason());
            context.State.SetString("runtimeLabel", ResolveTranslation(integration != null ? integration.runtimeLabelKey : null, "Bad Hygiene Lite Integration"));
            context.State.SetString("runtimeDescription", ResolveTranslation(integration != null ? integration.runtimeDescriptionKey : null, "Provides Bad Hygiene Lite shuttle hygiene service."));
            context.State.SetString("runtimeSystemKey", integration != null ? integration.runtimeSystemKey : null);
            context.State.SetBool("liteServiceEnabled", ServiceEnabled);
            context.State.SetFloat("litePowerDemandWatts", PowerDemandWatts);
        }

        private void ClearDiagnosticState(ShuttleExternalRuntimeContext context)
        {
            if (context == null)
            {
                return;
            }

            context.State.SetBool("liteRuntimeActive", false);
            context.State.SetBool("liteBridgeResolved", false);
            context.State.SetBool("liteHygieneResolved", false);
            context.State.SetBool("liteBladderResolved", false);
            context.State.SetBool("liteThirstResolved", false);
            context.State.SetBool("liteThirstServiceEnabled", false);
            context.State.SetInt("liteServiceableOccupants", 0);
            context.State.SetBool("liteHostInfoAvailable", false);
            context.State.SetBool("liteHostSpawned", false);
            context.State.SetString("liteMissingNeedDefs", null);
            context.State.SetString("liteOptionalNeedDiagnostic", null);
            ClearServiceState(context, true);
        }

        private void ClearOperationalState(ShuttleExternalRuntimeContext context)
        {
            if (context == null)
            {
                return;
            }

            context.State.SetBool("liteRuntimeActive", false);
            context.State.SetInt("liteServiceableOccupants", 0);
            ClearServiceState(context, false);
            context.State.SetBool("liteHostInfoAvailable", false);
            context.State.SetBool("liteHostSpawned", false);
        }

        private void ClearServiceState(ShuttleExternalRuntimeContext context, bool resetSchedule)
        {
            if (context == null)
            {
                return;
            }

            context.State.SetInt("liteServicedPawns", 0);
            context.State.SetInt("liteServiceActions", 0);
            context.State.SetInt("liteHygieneServices", 0);
            context.State.SetInt("liteBladderServices", 0);
            context.State.SetInt("liteServiceFailures", 0);
            context.State.SetString("liteLastServicedPawnLabels", null);
            context.State.SetString("liteLastServiceFailure", null);
            if (resetSchedule)
            {
                context.State.SetInt("liteLastServiceTick", -1);
                context.State.SetInt("liteNextServiceTick", context.TicksGame + ServiceIntervalTicks);
            }
        }

        private void ServiceOccupants(ShuttleExternalRuntimeContext context)
        {
            IReadOnlyList<ShuttleExternalOccupantInfo> occupants = context.GetOccupants(
                new ShuttleExternalOccupantQuery
                {
                    HumanlikeOnly = true,
                    ServiceableOnly = true
                });

            if (occupants == null)
            {
                occupants = new List<ShuttleExternalOccupantInfo>();
            }

            int serviceableCount = CountServiceableHumanlikeOccupants(occupants);
            context.State.SetInt("liteServiceableOccupants", serviceableCount);
            context.State.SetInt("liteServicedPawns", 0);
            context.State.SetInt("liteServiceActions", 0);
            context.State.SetInt("liteHygieneServices", 0);
            context.State.SetInt("liteBladderServices", 0);
            context.State.SetInt("liteServiceFailures", 0);
            context.State.SetString("liteLastServicedPawnLabels", null);
            context.State.SetString("liteLastServiceFailure", null);

            int processedPawnCount = 0;
            int servicedPawnCount = 0;
            int serviceActions = 0;
            int failures = 0;
            bool anyOccupantNeededService = false;
            string lastFailure = null;
            List<string> servicedLabels = new List<string>();

            for (int i = 0; i < occupants.Count; i++)
            {
                ShuttleExternalOccupantInfo occupant = occupants[i];
                Pawn pawn = occupant != null ? occupant.UnsafeLivePawn : null;
                if (!CanServiceOccupant(occupant, pawn))
                {
                    continue;
                }

                if (processedPawnCount >= MaxPawnsPerService)
                {
                    break;
                }

                processedPawnCount++;
                bool servicedThisPawn = false;
                try
                {
                    bool neededService;
                    if (TryServiceHygiene(pawn, out neededService))
                    {
                        serviceActions++;
                        servicedThisPawn = true;
                        AddInt(context, "liteHygieneServices", 1);
                    }
                    else if (neededService && bridge != null && !string.IsNullOrEmpty(bridge.LastFailureReason))
                    {
                        failures++;
                        lastFailure = bridge.LastFailureReason;
                    }

                    anyOccupantNeededService = anyOccupantNeededService || neededService;

                    if (TryServiceBladder(pawn, out neededService))
                    {
                        serviceActions++;
                        servicedThisPawn = true;
                        AddInt(context, "liteBladderServices", 1);
                    }
                    else if (neededService && bridge != null && !string.IsNullOrEmpty(bridge.LastFailureReason))
                    {
                        failures++;
                        lastFailure = bridge.LastFailureReason;
                    }

                    anyOccupantNeededService = anyOccupantNeededService || neededService;
                }
                catch (System.Exception exception)
                {
                    failures++;
                    lastFailure = exception.GetType().Name;
                }

                if (servicedThisPawn)
                {
                    servicedPawnCount++;
                    AddPawnLabel(servicedLabels, pawn);
                }
            }

            context.State.SetInt("liteServicedPawns", servicedPawnCount);
            context.State.SetInt("liteServiceActions", serviceActions);
            context.State.SetInt("liteServiceFailures", failures);
            context.State.SetInt("liteLastServiceTick", context.TicksGame);
            context.State.SetString("liteLastServicedPawnLabels", servicedLabels.Count > 0 ? string.Join(", ", servicedLabels.ToArray()) : null);
            context.State.SetString("liteLastServiceFailure", lastFailure);
            context.State.SetString("liteBlockedReason", ResolveServiceBlockReason(failures, serviceActions));
            context.State.SetString(
                "liteLastResult",
                ResolveServiceResult(serviceableCount, processedPawnCount, servicedPawnCount, serviceActions, failures, anyOccupantNeededService));
        }

        private bool TryServiceHygiene(Pawn pawn, out bool neededService)
        {
            neededService = false;
            float hygiene;
            if (bridge == null ||
                !bridge.TryReadHygiene(pawn, out hygiene) ||
                hygiene < 0f ||
                hygiene >= HygieneServiceThreshold)
            {
                return false;
            }

            neededService = true;
            return bridge.TryCleanHygiene(pawn, HygieneServiceGain, HygieneServiceTarget);
        }

        private bool TryServiceBladder(Pawn pawn, out bool neededService)
        {
            neededService = false;
            float bladder;
            if (bridge == null ||
                !bridge.TryReadBladder(pawn, out bladder) ||
                bladder < 0f ||
                bladder > BladderServiceThreshold)
            {
                return false;
            }

            neededService = true;
            int dumpCallsUsed;
            return bridge.TryServiceBladder(pawn, BladderServiceTarget, BladderDumpCallsPerService, out dumpCallsUsed);
        }

        private void WriteHostDiagnostic(ShuttleExternalRuntimeContext context)
        {
            context.State.SetBool("liteHostInfoAvailable", false);
            context.State.SetBool("liteHostSpawned", false);
            if (context == null || !context.SupportsHostRead)
            {
                return;
            }

            ShuttleExternalHostInfo hostInfo;
            if (context.TryGetHostInfo(out hostInfo) && hostInfo != null)
            {
                context.State.SetBool("liteHostInfoAvailable", true);
                context.State.SetBool("liteHostSpawned", hostInfo.Spawned);
            }
        }

        private static int CountServiceableHumanlikeOccupants(ShuttleExternalRuntimeContext context)
        {
            if (context == null || !context.SupportsOccupantRead)
            {
                return 0;
            }

            IReadOnlyList<ShuttleExternalOccupantInfo> occupants = context.GetOccupants(
                new ShuttleExternalOccupantQuery
                {
                    HumanlikeOnly = true,
                    ServiceableOnly = true
                });
            if (occupants == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < occupants.Count; i++)
            {
                ShuttleExternalOccupantInfo occupant = occupants[i];
                Pawn pawn = occupant != null ? occupant.UnsafeLivePawn : null;
                if (occupant != null &&
                    occupant.CanReceiveExternalService &&
                    occupant.IsHumanlike &&
                    !occupant.IsMechanoid &&
                    pawn != null &&
                    pawn.RaceProps != null &&
                    pawn.RaceProps.Humanlike &&
                    !pawn.Dead &&
                    !pawn.Destroyed)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountServiceableHumanlikeOccupants(IReadOnlyList<ShuttleExternalOccupantInfo> occupants)
        {
            if (occupants == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < occupants.Count; i++)
            {
                ShuttleExternalOccupantInfo occupant = occupants[i];
                Pawn pawn = occupant != null ? occupant.UnsafeLivePawn : null;
                if (CanServiceOccupant(occupant, pawn))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool CanServiceOccupant(ShuttleExternalOccupantInfo occupant, Pawn pawn)
        {
            return occupant != null &&
                occupant.CanReceiveExternalService &&
                occupant.IsHumanlike &&
                !occupant.IsMechanoid &&
                pawn != null &&
                pawn.RaceProps != null &&
                pawn.RaceProps.Humanlike &&
                !pawn.Dead &&
                !pawn.Destroyed;
        }

        private static void AddPawnLabel(List<string> labels, Pawn pawn)
        {
            if (labels == null || pawn == null)
            {
                return;
            }

            labels.Add(pawn.LabelShortCap);
        }

        private static void AddInt(ShuttleExternalRuntimeContext context, string key, int amount)
        {
            if (context == null || context.State == null || string.IsNullOrEmpty(key) || amount == 0)
            {
                return;
            }

            int value;
            context.State.TryGetInt(key, out value);
            context.State.SetInt(key, value + amount);
        }

        private static string ResolveServiceBlockReason(int failures, int serviceActions)
        {
            if (serviceActions > 0 || failures <= 0)
            {
                return null;
            }

            return "Bad Hygiene Lite service failed for one or more occupants.";
        }

        private static string ResolveServiceResult(
            int serviceableCount,
            int processedPawnCount,
            int servicedPawnCount,
            int serviceActions,
            int failures,
            bool anyOccupantNeededService)
        {
            if (serviceActions > 0)
            {
                return "Serviced " + serviceActions + " Bad Hygiene Lite action(s) for " + servicedPawnCount + " pawn(s).";
            }

            if (serviceableCount <= 0)
            {
                return "No serviceable shuttle occupants.";
            }

            if (failures > 0)
            {
                return "Bad Hygiene Lite service failed for " + failures + " processed occupant(s).";
            }

            if (anyOccupantNeededService)
            {
                return "Bad Hygiene Lite service was needed but did not change any need.";
            }

            return "No occupants currently need hygiene or toilet service among " + processedPawnCount + " processed occupant(s).";
        }

        private int DiagnosticIntervalTicks
        {
            get
            {
                return integration != null && integration.diagnosticIntervalTicks > 0
                    ? integration.diagnosticIntervalTicks
                    : 600;
            }
        }

        private bool ServiceEnabled
        {
            get { return integration != null && integration.serviceEnabled; }
        }

        private int ServiceIntervalTicks
        {
            get
            {
                return integration != null && integration.serviceIntervalTicks > 0
                    ? integration.serviceIntervalTicks
                    : 600;
            }
        }

        private int MaxPawnsPerService
        {
            get
            {
                return integration != null && integration.maxPawnsPerService > 0
                    ? integration.maxPawnsPerService
                    : 4;
            }
        }

        private float HygieneServiceThreshold
        {
            get { return Clamp01(integration != null ? integration.hygieneServiceThreshold : 0.65f); }
        }

        private float HygieneServiceGain
        {
            get { return integration != null ? Clamp01(integration.hygieneServiceGain) : 0.05f; }
        }

        private float HygieneServiceTarget
        {
            get { return Clamp01(integration != null ? integration.hygieneServiceTarget : 0.75f); }
        }

        private float BladderServiceThreshold
        {
            get { return Clamp01(integration != null ? integration.bladderServiceThreshold : 0.35f); }
        }

        private float BladderServiceTarget
        {
            get { return Clamp01(integration != null ? integration.bladderServiceTarget : 0.60f); }
        }

        private int BladderDumpCallsPerService
        {
            get
            {
                return integration != null && integration.bladderDumpCallsPerService > 0
                    ? integration.bladderDumpCallsPerService
                    : 30;
            }
        }

        private bool RequireInternalBusPowered
        {
            get { return integration == null || integration.requireInternalBusPowered; }
        }

        private float PowerDemandWatts
        {
            get { return integration != null && integration.powerDemandWatts > 0f ? integration.powerDemandWatts : 0f; }
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }

            return value > 1f ? 1f : value;
        }

        private static string ResolveTranslation(string key, string fallback)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return fallback;
            }

            return key.Trim().Translate().ToString();
        }
    }
}
