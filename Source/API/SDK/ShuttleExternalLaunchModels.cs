using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.API.SDK
{
    public enum ShuttleExternalLaunchHostStateKind
    {
        Unknown,
        Unavailable,
        SpawnedMap,
        CaravanHeld,
        InTransit
    }

    public enum ShuttleExternalLaunchIssueSeverity
    {
        Info,
        Warning,
        Blocker,
        Unavailable
    }

    public enum ShuttleExternalLaunchIssueSourceKind
    {
        Unknown,
        Profile,
        Assembly,
        Runtime,
        Power,
        Cargo,
        Crew,
        Environment,
        HolderTransfer,
        RefrigeratedCargo,
        ModuleRuntime,
        ExternalLaunchRule,
        World
    }

    public sealed class ShuttleExternalLaunchReadSnapshot
    {
        public ShuttleExternalLaunchReadSnapshot(
            bool available,
            string unavailableReason,
            int profileRevision,
            int ticksGame,
            ShuttleExternalLaunchHostStateKind hostStateKind,
            ShuttleExternalLaunchReadinessSnapshot readiness,
            ShuttleExternalLaunchPayloadSnapshot payload,
            ShuttleExternalLaunchCostSnapshot cost,
            ShuttleExternalLaunchCooldownSnapshot cooldown,
            IEnumerable<ShuttleExternalLaunchIssueSnapshot> issues,
            bool destinationQuoteSupported,
            string destinationQuotePolicy)
            : this(
                available,
                unavailableReason,
                null,
                profileRevision,
                ticksGame,
                hostStateKind,
                readiness,
                payload,
                cost,
                cooldown,
                issues,
                destinationQuoteSupported,
                destinationQuotePolicy,
                null)
        {
        }

        public ShuttleExternalLaunchReadSnapshot(
            bool available,
            string unavailableReason,
            string hostStableId,
            int profileRevision,
            int ticksGame,
            ShuttleExternalLaunchHostStateKind hostStateKind,
            ShuttleExternalLaunchReadinessSnapshot readiness,
            ShuttleExternalLaunchPayloadSnapshot payload,
            ShuttleExternalLaunchCostSnapshot cost,
            ShuttleExternalLaunchCooldownSnapshot cooldown,
            IEnumerable<ShuttleExternalLaunchIssueSnapshot> issues,
            bool destinationQuoteSupported,
            string destinationQuotePolicy,
            ShuttleExternalLaunchRuleDiagnosticsSnapshot ruleDiagnostics)
        {
            ShuttleExternalLaunchRuleDiagnosticsSnapshot diagnostics =
                ruleDiagnostics ?? ShuttleExternalLaunchRuleDiagnosticsSnapshot.Empty();

            this.Available = available;
            this.UnavailableReason = unavailableReason;
            this.HostStableId = hostStableId;
            this.ProfileRevision = profileRevision;
            this.TicksGame = ticksGame;
            this.HostStateKind = hostStateKind;
            this.Readiness = readiness;
            this.Payload = payload;
            this.Cost = cost;
            this.Cooldown = cooldown;
            this.Issues = ShuttleExternalSDKCollections.Copy(issues);
            this.DestinationQuoteSupported = destinationQuoteSupported;
            this.DestinationQuotePolicy = destinationQuotePolicy;
            this.RuleDiagnostics = diagnostics;
            this.ExternalRulesEvaluated = diagnostics.ExternalRulesEvaluated;
            this.ExternalRuleIssueCount = diagnostics.ExternalRuleIssueCount;
            this.ExternalRuleBlockerCount = diagnostics.ExternalRuleBlockerCount;
            this.ExternalRulesAffectSdkReadinessOnly =
                diagnostics.ExternalRulesAffectSdkReadinessOnly;
        }

        public bool Available { get; private set; }
        public string UnavailableReason { get; private set; }
        public string HostStableId { get; private set; }
        public int ProfileRevision { get; private set; }
        public int TicksGame { get; private set; }
        public ShuttleExternalLaunchHostStateKind HostStateKind { get; private set; }
        public ShuttleExternalLaunchReadinessSnapshot Readiness { get; private set; }
        public ShuttleExternalLaunchPayloadSnapshot Payload { get; private set; }
        public ShuttleExternalLaunchCostSnapshot Cost { get; private set; }
        public ShuttleExternalLaunchCooldownSnapshot Cooldown { get; private set; }
        public IReadOnlyList<ShuttleExternalLaunchIssueSnapshot> Issues { get; private set; }
        public bool DestinationQuoteSupported { get; private set; }
        public string DestinationQuotePolicy { get; private set; }
        public ShuttleExternalLaunchRuleDiagnosticsSnapshot RuleDiagnostics { get; private set; }
        public bool ExternalRulesEvaluated { get; private set; }
        public int ExternalRuleIssueCount { get; private set; }
        public int ExternalRuleBlockerCount { get; private set; }
        public bool ExternalRulesAffectSdkReadinessOnly { get; private set; }
    }

    public sealed class ShuttleExternalLaunchReadinessSnapshot
    {
        public ShuttleExternalLaunchReadinessSnapshot(
            bool canLaunchNow,
            bool canOpenLaunchFlow,
            int blockerCount,
            int warningCount,
            int issueCount,
            bool hasLaunchController,
            bool hasTransporterBackend,
            bool cargoLoadingComplete,
            bool storedEnergyReadyForBaseCost,
            bool cooldownActive,
            bool environmentBlocked,
            bool holderTransferBlocked,
            bool refrigeratedCargoBlocked,
            bool moduleRuntimeBlocked)
            : this(
                canLaunchNow,
                canOpenLaunchFlow,
                blockerCount,
                warningCount,
                issueCount,
                hasLaunchController,
                hasTransporterBackend,
                cargoLoadingComplete,
                storedEnergyReadyForBaseCost,
                cooldownActive,
                environmentBlocked,
                holderTransferBlocked,
                refrigeratedCargoBlocked,
                moduleRuntimeBlocked,
                false,
                0,
                0,
                false)
        {
        }

        public ShuttleExternalLaunchReadinessSnapshot(
            bool canLaunchNow,
            bool canOpenLaunchFlow,
            int blockerCount,
            int warningCount,
            int issueCount,
            bool hasLaunchController,
            bool hasTransporterBackend,
            bool cargoLoadingComplete,
            bool storedEnergyReadyForBaseCost,
            bool cooldownActive,
            bool environmentBlocked,
            bool holderTransferBlocked,
            bool refrigeratedCargoBlocked,
            bool moduleRuntimeBlocked,
            bool externalRulesEvaluated,
            int externalRuleIssueCount,
            int externalRuleBlockerCount,
            bool externalRulesAffectSdkReadinessOnly)
        {
            this.CanLaunchNow = canLaunchNow;
            this.CanOpenLaunchFlow = canOpenLaunchFlow;
            this.BlockerCount = blockerCount;
            this.WarningCount = warningCount;
            this.IssueCount = issueCount;
            this.HasLaunchController = hasLaunchController;
            this.HasTransporterBackend = hasTransporterBackend;
            this.CargoLoadingComplete = cargoLoadingComplete;
            this.StoredEnergyReadyForBaseCost = storedEnergyReadyForBaseCost;
            this.CooldownActive = cooldownActive;
            this.EnvironmentBlocked = environmentBlocked;
            this.HolderTransferBlocked = holderTransferBlocked;
            this.RefrigeratedCargoBlocked = refrigeratedCargoBlocked;
            this.ModuleRuntimeBlocked = moduleRuntimeBlocked;
            this.ExternalRulesEvaluated = externalRulesEvaluated;
            this.ExternalRuleIssueCount = externalRuleIssueCount;
            this.ExternalRuleBlockerCount = externalRuleBlockerCount;
            this.ExternalRulesAffectSdkReadinessOnly =
                externalRulesAffectSdkReadinessOnly;
        }

        public bool CanLaunchNow { get; private set; }
        public bool CanOpenLaunchFlow { get; private set; }
        public int BlockerCount { get; private set; }
        public int WarningCount { get; private set; }
        public int IssueCount { get; private set; }
        public bool HasLaunchController { get; private set; }
        public bool HasTransporterBackend { get; private set; }
        public bool CargoLoadingComplete { get; private set; }
        public bool StoredEnergyReadyForBaseCost { get; private set; }
        public bool CooldownActive { get; private set; }
        public bool EnvironmentBlocked { get; private set; }
        public bool HolderTransferBlocked { get; private set; }
        public bool RefrigeratedCargoBlocked { get; private set; }
        public bool ModuleRuntimeBlocked { get; private set; }
        public bool ExternalRulesEvaluated { get; private set; }
        public int ExternalRuleIssueCount { get; private set; }
        public int ExternalRuleBlockerCount { get; private set; }
        public bool ExternalRulesAffectSdkReadinessOnly { get; private set; }
    }

    public sealed class ShuttleExternalLaunchIssueSnapshot
    {
        public ShuttleExternalLaunchIssueSnapshot(
            string issueKey,
            ShuttleExternalLaunchIssueSeverity severity,
            string messageKey,
            string message,
            bool blocksLaunch,
            ShuttleExternalLaunchIssueSourceKind sourceKind,
            string sourceId,
            string referenceId,
            string suggestedActionKey)
            : this(
                issueKey,
                severity,
                messageKey,
                message,
                blocksLaunch,
                sourceKind,
                sourceId,
                referenceId,
                suggestedActionKey,
                false,
                true,
                false)
        {
        }

        public ShuttleExternalLaunchIssueSnapshot(
            string issueKey,
            ShuttleExternalLaunchIssueSeverity severity,
            string messageKey,
            string message,
            bool blocksLaunch,
            ShuttleExternalLaunchIssueSourceKind sourceKind,
            string sourceId,
            string referenceId,
            string suggestedActionKey,
            bool fromExternalLaunchRuleProvider,
            bool enforcedInActualLaunch,
            bool affectsSdkReadinessOnly)
        {
            this.IssueKey = issueKey;
            this.Severity = severity;
            this.MessageKey = messageKey;
            this.Message = message;
            this.BlocksLaunch = blocksLaunch;
            this.SourceKind = sourceKind;
            this.SourceId = sourceId;
            this.ReferenceId = referenceId;
            this.SuggestedActionKey = suggestedActionKey;
            this.FromExternalLaunchRuleProvider = fromExternalLaunchRuleProvider;
            this.EnforcedInActualLaunch = enforcedInActualLaunch;
            this.AffectsSdkReadinessOnly = affectsSdkReadinessOnly;
        }

        public string IssueKey { get; private set; }
        public ShuttleExternalLaunchIssueSeverity Severity { get; private set; }
        public string MessageKey { get; private set; }
        public string Message { get; private set; }
        public bool BlocksLaunch { get; private set; }
        public ShuttleExternalLaunchIssueSourceKind SourceKind { get; private set; }
        public string SourceId { get; private set; }
        public string ReferenceId { get; private set; }
        public string SuggestedActionKey { get; private set; }
        public bool FromExternalLaunchRuleProvider { get; private set; }
        public bool EnforcedInActualLaunch { get; private set; }
        public bool AffectsSdkReadinessOnly { get; private set; }
    }

    public sealed class ShuttleExternalLaunchPayloadSnapshot
    {
        public ShuttleExternalLaunchPayloadSnapshot(
            float structuralMassKg,
            float loadedNormalCargoMassKg,
            float queuedCargoMassKg,
            float refrigeratedCargoMassKg,
            float medicalBayPatientMassKg,
            float externalRuntimeMassKg,
            float payloadMassKg,
            float totalLaunchMassKg,
            float cargoCapacityKg,
            bool cargoCapacityExceeded,
            int loadedStackCount,
            int queuedStackCount)
        {
            this.StructuralMassKg = structuralMassKg;
            this.LoadedNormalCargoMassKg = loadedNormalCargoMassKg;
            this.QueuedCargoMassKg = queuedCargoMassKg;
            this.RefrigeratedCargoMassKg = refrigeratedCargoMassKg;
            this.MedicalBayPatientMassKg = medicalBayPatientMassKg;
            this.ExternalRuntimeMassKg = externalRuntimeMassKg;
            this.PayloadMassKg = payloadMassKg;
            this.TotalLaunchMassKg = totalLaunchMassKg;
            this.CargoCapacityKg = cargoCapacityKg;
            this.CargoCapacityExceeded = cargoCapacityExceeded;
            this.LoadedStackCount = loadedStackCount;
            this.QueuedStackCount = queuedStackCount;
        }

        public float StructuralMassKg { get; private set; }
        public float LoadedNormalCargoMassKg { get; private set; }
        public float QueuedCargoMassKg { get; private set; }
        public float RefrigeratedCargoMassKg { get; private set; }
        public float MedicalBayPatientMassKg { get; private set; }
        public float ExternalRuntimeMassKg { get; private set; }
        public float PayloadMassKg { get; private set; }
        public float TotalLaunchMassKg { get; private set; }
        public float CargoCapacityKg { get; private set; }
        public bool CargoCapacityExceeded { get; private set; }
        public int LoadedStackCount { get; private set; }
        public int QueuedStackCount { get; private set; }
    }

    public sealed class ShuttleExternalLaunchCostSnapshot
    {
        public ShuttleExternalLaunchCostSnapshot(
            float baseLaunchEnergyWd,
            float energyPerTileWd,
            float energyPerKgTileWd,
            float payloadLiftEnergyPerKgWd,
            float payloadLiftEnergyWd,
            float reserveEnergyWd,
            float storedEnergyWd,
            float availableEnergyWd,
            float requiredEnergyWd,
            int maxDistanceTilesAtCurrentLoad,
            int hardRangeCapTiles,
            float rangeDistanceFactorUsed,
            bool destinationSpecific,
            bool canPayRequiredEnergy)
        {
            this.BaseLaunchEnergyWd = baseLaunchEnergyWd;
            this.EnergyPerTileWd = energyPerTileWd;
            this.EnergyPerKgTileWd = energyPerKgTileWd;
            this.PayloadLiftEnergyPerKgWd = payloadLiftEnergyPerKgWd;
            this.PayloadLiftEnergyWd = payloadLiftEnergyWd;
            this.ReserveEnergyWd = reserveEnergyWd;
            this.StoredEnergyWd = storedEnergyWd;
            this.AvailableEnergyWd = availableEnergyWd;
            this.RequiredEnergyWd = requiredEnergyWd;
            this.MaxDistanceTilesAtCurrentLoad = maxDistanceTilesAtCurrentLoad;
            this.HardRangeCapTiles = hardRangeCapTiles;
            this.RangeDistanceFactorUsed = rangeDistanceFactorUsed;
            this.DestinationSpecific = destinationSpecific;
            this.CanPayRequiredEnergy = canPayRequiredEnergy;
        }

        public float BaseLaunchEnergyWd { get; private set; }
        public float EnergyPerTileWd { get; private set; }
        public float EnergyPerKgTileWd { get; private set; }
        public float PayloadLiftEnergyPerKgWd { get; private set; }
        public float PayloadLiftEnergyWd { get; private set; }
        public float ReserveEnergyWd { get; private set; }
        public float StoredEnergyWd { get; private set; }
        public float AvailableEnergyWd { get; private set; }
        public float RequiredEnergyWd { get; private set; }
        public int MaxDistanceTilesAtCurrentLoad { get; private set; }
        public int HardRangeCapTiles { get; private set; }
        public float RangeDistanceFactorUsed { get; private set; }
        public bool DestinationSpecific { get; private set; }
        public bool CanPayRequiredEnergy { get; private set; }
    }

    public sealed class ShuttleExternalLaunchCooldownSnapshot
    {
        private readonly int lastLaunchTick;
        private readonly int cooldownEndTick;

        public ShuttleExternalLaunchCooldownSnapshot(
            bool cooldownActive,
            int lastLaunchTick,
            int cooldownEndTick,
            int cooldownRemainingTicks,
            int cooldownTotalTicks,
            int lastArrivalTick)
        {
            this.CooldownActive = cooldownActive;
            this.lastLaunchTick = lastLaunchTick;
            this.cooldownEndTick = cooldownEndTick;
            this.CooldownRemainingTicks = cooldownRemainingTicks;
            this.CooldownTotalTicks = cooldownTotalTicks;
            this.LastArrivalTick = lastArrivalTick;
        }

        public bool CooldownActive { get; private set; }
        public int LastLaunchTick
        {
            get
            {
                return this.lastLaunchTick;
            }
        }

        public int CooldownEndTick
        {
            get
            {
                return this.cooldownEndTick;
            }
        }

        public int CooldownRemainingTicks { get; private set; }
        public int CooldownTotalTicks { get; private set; }
        public int LastArrivalTick { get; private set; }
    }

    public sealed class ShuttleExternalLaunchQuoteRequest
    {
        public ShuttleExternalLaunchQuoteRequest()
            : this(false)
        {
        }

        public ShuttleExternalLaunchQuoteRequest(bool includeDestinationQuote)
        {
            this.IncludeDestinationQuote = includeDestinationQuote;
        }

        public bool IncludeDestinationQuote { get; private set; }
    }

    public sealed class ShuttleExternalLaunchQuoteResult
    {
        public ShuttleExternalLaunchQuoteResult(
            bool available,
            string unavailableReason,
            bool destinationSpecific,
            bool destinationQuoteSupported,
            string destinationQuotePolicy,
            ShuttleExternalLaunchReadinessSnapshot readiness,
            ShuttleExternalLaunchPayloadSnapshot payload,
            ShuttleExternalLaunchCostSnapshot cost,
            ShuttleExternalLaunchCooldownSnapshot cooldown,
            IEnumerable<ShuttleExternalLaunchIssueSnapshot> issues)
            : this(
                available,
                unavailableReason,
                destinationSpecific,
                destinationQuoteSupported,
                destinationQuotePolicy,
                readiness,
                payload,
                cost,
                cooldown,
                issues,
                null)
        {
        }

        public ShuttleExternalLaunchQuoteResult(
            bool available,
            string unavailableReason,
            bool destinationSpecific,
            bool destinationQuoteSupported,
            string destinationQuotePolicy,
            ShuttleExternalLaunchReadinessSnapshot readiness,
            ShuttleExternalLaunchPayloadSnapshot payload,
            ShuttleExternalLaunchCostSnapshot cost,
            ShuttleExternalLaunchCooldownSnapshot cooldown,
            IEnumerable<ShuttleExternalLaunchIssueSnapshot> issues,
            ShuttleExternalLaunchRuleDiagnosticsSnapshot ruleDiagnostics)
        {
            ShuttleExternalLaunchRuleDiagnosticsSnapshot diagnostics =
                ruleDiagnostics ?? ShuttleExternalLaunchRuleDiagnosticsSnapshot.Empty();

            this.Available = available;
            this.UnavailableReason = unavailableReason;
            this.DestinationSpecific = destinationSpecific;
            this.DestinationQuoteSupported = destinationQuoteSupported;
            this.DestinationQuotePolicy = destinationQuotePolicy;
            this.Readiness = readiness;
            this.Payload = payload;
            this.Cost = cost;
            this.Cooldown = cooldown;
            this.Issues = ShuttleExternalSDKCollections.Copy(issues);
            this.RuleDiagnostics = diagnostics;
            this.ExternalRulesEvaluated = diagnostics.ExternalRulesEvaluated;
            this.ExternalRuleIssueCount = diagnostics.ExternalRuleIssueCount;
            this.ExternalRuleBlockerCount = diagnostics.ExternalRuleBlockerCount;
            this.ExternalRulesAffectSdkReadinessOnly =
                diagnostics.ExternalRulesAffectSdkReadinessOnly;
        }

        public bool Available { get; private set; }
        public string UnavailableReason { get; private set; }
        public bool DestinationSpecific { get; private set; }
        public bool DestinationQuoteSupported { get; private set; }
        public string DestinationQuotePolicy { get; private set; }
        public ShuttleExternalLaunchReadinessSnapshot Readiness { get; private set; }
        public ShuttleExternalLaunchPayloadSnapshot Payload { get; private set; }
        public ShuttleExternalLaunchCostSnapshot Cost { get; private set; }
        public ShuttleExternalLaunchCooldownSnapshot Cooldown { get; private set; }
        public IReadOnlyList<ShuttleExternalLaunchIssueSnapshot> Issues { get; private set; }
        public ShuttleExternalLaunchRuleDiagnosticsSnapshot RuleDiagnostics { get; private set; }
        public bool ExternalRulesEvaluated { get; private set; }
        public int ExternalRuleIssueCount { get; private set; }
        public int ExternalRuleBlockerCount { get; private set; }
        public bool ExternalRulesAffectSdkReadinessOnly { get; private set; }
    }
}
