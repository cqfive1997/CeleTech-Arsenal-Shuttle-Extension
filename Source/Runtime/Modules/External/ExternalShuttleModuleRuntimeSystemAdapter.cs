using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.API.Runtime;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Launch;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalShuttleModuleRuntimeSystemAdapter : ShuttleModuleRuntimeSystemBase
    {
        private readonly ExternalRuntimeRegistration registration;
        private bool warnedNullRegistration;
        private bool warnedTickIntervalException;
        private bool warnedNegativeTickInterval;
        private bool warnedStateSchemaVersionException;
        private bool warnedStateTypeMismatch;

        internal ExternalShuttleModuleRuntimeSystemAdapter(ExternalRuntimeRegistration registration)
        {
            this.registration = registration;
            if (registration == null)
            {
                this.WarnNullRegistration();
            }
        }

        public override string RuntimeSystemKey
        {
            get
            {
                return this.registration != null ? this.registration.FullRuntimeKey : null;
            }
        }

        public override int TickInterval
        {
            get
            {
                if (this.registration == null || this.registration.System == null)
                {
                    this.WarnNullRegistration();
                    return 0;
                }

                try
                {
                    int tickInterval = this.registration.System.TickInterval;
                    if (tickInterval < 0)
                    {
                        if (!this.warnedNegativeTickInterval)
                        {
                            this.warnedNegativeTickInterval = true;
                            Log.WarningOnce(ExternalShuttleRuntimeRegistry.LogPrefix +
                                "key '" + this.registration.FullRuntimeKey +
                                "' returned negative TickInterval " + tickInterval +
                                ". Tick disabled.",
                                this.MakeAdapterWarningHash(
                                    "negative-tick-interval",
                                    null,
                                    tickInterval.ToString()));
                        }

                        return 0;
                    }

                    return tickInterval;
                }
                catch (Exception exception)
                {
                    if (!this.warnedTickIntervalException)
                    {
                        this.warnedTickIntervalException = true;
                        Log.WarningOnce(ExternalShuttleRuntimeRegistry.LogPrefix +
                            "key '" + this.registration.FullRuntimeKey +
                            "' TickInterval failed. Tick disabled. Exception: " + exception,
                            this.MakeAdapterWarningHash(
                                "tick-interval-exception",
                                null,
                                exception.GetType().FullName + exception.Message));
                    }

                    return 0;
                }
            }
        }

        public override bool ParticipatesInPowerDemand
        {
            get
            {
                // Preserve compatibility for existing external systems. The public SDK does not
                // currently require a separate power-participation declaration.
                return true;
            }
        }

        public override bool AppliesTo(ShuttleModule module)
        {
            if (this.registration == null || this.registration.System == null || module == null)
            {
                return false;
            }

            if (!ExternalRuntimeBindingUtility.ModuleHasRuntimeKey(
                module,
                this.registration.FullRuntimeKey))
            {
                return false;
            }

            try
            {
                return this.registration.System.AppliesTo(
                    ExternalRuntimeInfoFactory.CreateModuleInfo(module));
            }
            catch (Exception exception)
            {
                Log.ErrorOnce(ExternalShuttleRuntimeRegistry.LogPrefix +
                    "key '" + this.GetRuntimeDebugKey() +
                    "' AppliesTo failed for " +
                    (module.ModuleDef != null ? module.ModuleDef.defName : "missing def") +
                    ". Exception: " + exception,
                    this.MakeAdapterWarningHash(
                        "applies-to-exception",
                        module.ModuleInstanceID,
                        exception.GetType().FullName + exception.Message));
                return false;
            }
        }

        public override bool AppliesTo(ShuttleLaunchModuleRecord moduleRecord)
        {
            if (this.registration == null || this.registration.System == null || moduleRecord == null)
            {
                return false;
            }

            if (!ExternalRuntimeBindingUtility.LaunchModuleRecordHasRuntimeKey(
                moduleRecord,
                this.registration.FullRuntimeKey))
            {
                return false;
            }

            try
            {
                return this.registration.System.AppliesTo(
                    ExternalRuntimeInfoFactory.CreateLaunchModuleInfo(moduleRecord));
            }
            catch (Exception exception)
            {
                Log.ErrorOnce(ExternalShuttleRuntimeRegistry.LogPrefix +
                    "key '" + this.GetRuntimeDebugKey() +
                    "' launch AppliesTo failed for " +
                    this.GetLaunchModuleDebugName(moduleRecord) +
                    ". Exception: " + exception,
                    this.MakeAdapterWarningHash(
                        "launch-applies-to-exception",
                        moduleRecord != null ? moduleRecord.ModuleInstanceID : null,
                        exception.GetType().FullName + exception.Message));
                return false;
            }
        }

        public override IShuttleModuleRuntimeState CreateState()
        {
            if (this.registration == null)
            {
                this.WarnNullRegistration();
                return null;
            }

            return new ExternalModuleRuntimeState(
                this.registration.OwnerPackageId,
                this.registration.FullRuntimeKey,
                this.GetSafeStateSchemaVersion());
        }

        public override void Reconcile(ShuttleModuleRuntimeContext context)
        {
            ExternalModuleRuntimeState state = this.GetExternalStateOrNull(context, "reconcile");
            ShuttleExternalRuntimeContext externalContext =
                ExternalRuntimeContextFactory.CreateRuntimeContext(this.registration, context, state);
            if (!this.PrepareStateBeforeRuntimeCall(externalContext, state))
            {
                return;
            }

            state.RefreshPowerDemandDispatchCache(externalContext.Module);

            if (!this.IsRuntimeEnabled(state))
            {
                return;
            }

            long start = ExternalRuntimeMetricsRegistry.GetTimestamp();
            try
            {
                this.registration.System.Reconcile(externalContext);
            }
            catch (Exception exception)
            {
                this.MarkRuntimeExecutionFailed(
                    state,
                    "Reconcile",
                    context != null ? context.ModuleInstanceID : null,
                    context != null ? context.ModuleDefName : null,
                    context != null ? context.TicksGame : -1,
                    exception);
            }
            finally
            {
                ExternalRuntimeMetricsRegistry.RecordReconcileDuration(
                    context.ModuleInstanceID,
                    this.registration.FullRuntimeKey,
                    start);
            }
        }

        public override void CollectPowerDemand(ShuttleModuleRuntimeContext context)
        {
            ExternalModuleRuntimeState state = this.GetExternalStateOrNull(context, "power demand collection");
            if (state == null)
            {
                return;
            }

            if (!this.IsRuntimeEnabled(state))
            {
                ExternalRuntimeMetricsRegistry.RecordLastPowerDemandWatts(
                    context != null ? context.ModuleInstanceID : null,
                    this.GetRuntimeDebugKey(),
                    0f);
                return;
            }

            if (state.HasRuntimeFailure)
            {
                this.LogStateFailureBlockedOnce(
                    "power demand collection",
                    context != null ? context.ModuleInstanceID : null,
                    context != null ? context.ModuleDefName : null,
                    state,
                    this.GetSafeStateSchemaVersion());
                ExternalRuntimeMetricsRegistry.RecordLastPowerDemandWatts(
                    context != null ? context.ModuleInstanceID : null,
                    this.GetRuntimeDebugKey(),
                    0f);
                return;
            }

            string blockedReason;
            if (!this.IsStatePreparedForExternalCall(
                state,
                this.GetSafeStateSchemaVersion(),
                "power demand collection",
                context != null ? context.ModuleInstanceID : null,
                context != null ? context.ModuleDefName : null,
                out blockedReason))
            {
                ExternalRuntimeMetricsRegistry.RecordLastPowerDemandWatts(
                    context != null ? context.ModuleInstanceID : null,
                    this.GetRuntimeDebugKey(),
                    0f);
                return;
            }

            ShuttleExternalModuleInfo moduleInfo;
            IShuttleExternalRuntimeStateReader stateReader;
            if (!state.TryGetPowerDemandDispatchCache(out moduleInfo, out stateReader))
            {
                Log.WarningOnce(
                    ExternalShuttleRuntimeRegistry.LogPrefix +
                    "key '" + this.GetRuntimeDebugKey() +
                    "' has no prepared power-demand DTO cache for module " +
                    (context != null ? context.ModuleInstanceID : "unknown") +
                    ". Runtime reconcile must run before power collection.",
                    this.MakeAdapterWarningHash(
                        "power-demand-cache-missing",
                        context != null ? context.ModuleInstanceID : null,
                        null));
                ExternalRuntimeMetricsRegistry.RecordLastPowerDemandWatts(
                    context != null ? context.ModuleInstanceID : null,
                    this.GetRuntimeDebugKey(),
                    0f);
                return;
            }

            ShuttleExternalPowerDemandContext externalContext =
                ExternalRuntimeContextFactory.CreatePowerDemandContext(moduleInfo, stateReader);
            if (externalContext == null)
            {
                return;
            }

            long start = ExternalRuntimeMetricsRegistry.GetTimestamp();
            bool succeeded = false;
            try
            {
                this.registration.System.CollectPowerDemand(externalContext);
                succeeded = true;
            }
            catch (Exception exception)
            {
                this.MarkRuntimeExecutionFailed(
                    state,
                    "CollectPowerDemand",
                    context != null ? context.ModuleInstanceID : null,
                    context != null ? context.ModuleDefName : null,
                    context != null ? context.TicksGame : -1,
                    exception);
            }
            finally
            {
                ExternalRuntimeMetricsRegistry.RecordPowerDemandDurationAndWatts(
                    context.ModuleInstanceID,
                    this.registration.FullRuntimeKey,
                    start,
                    externalContext.InternalPowerDemandWatts);
            }

            if (!succeeded || state.HasRuntimeFailure)
            {
                return;
            }

            context.AddInternalPowerDemandWatts(externalContext.InternalPowerDemandWatts);
        }

        public override void CollectMassContribution(
            ShuttleModuleRuntimeContext context,
            ShuttleRuntimeMassContributionCollector collector)
        {
            ExternalModuleRuntimeState state = this.GetExternalStateOrNull(context, "mass contribution collection");
            if (state == null || collector == null)
            {
                return;
            }

            if (!this.IsRuntimeEnabled(state))
            {
                return;
            }

            if (state.HasRuntimeFailure)
            {
                this.LogStateFailureBlockedOnce(
                    "mass contribution collection",
                    context != null ? context.ModuleInstanceID : null,
                    context != null ? context.ModuleDefName : null,
                    state,
                    this.GetSafeStateSchemaVersion());
                return;
            }

            ShuttleExternalRuntimeContext runtimeContext =
                ExternalRuntimeContextFactory.CreateRuntimeContext(this.registration, context, state);
            if (!this.PrepareStateBeforeRuntimeCall(runtimeContext, state))
            {
                return;
            }

            ShuttleExternalMassContributionContext externalContext =
                ExternalRuntimeContextFactory.CreateMassContributionContext(context, state);
            if (externalContext == null)
            {
                return;
            }

            try
            {
                this.registration.System.CollectMassContribution(externalContext);
            }
            catch (Exception exception)
            {
                this.MarkRuntimeExecutionFailed(
                    state,
                    "CollectMassContribution",
                    context != null ? context.ModuleInstanceID : null,
                    context != null ? context.ModuleDefName : null,
                    context != null ? context.TicksGame : -1,
                    exception);
                return;
            }

            IReadOnlyList<ShuttleExternalMassContributionEntry> contributions =
                externalContext.ContributionsForRead;
            for (int i = 0; contributions != null && i < contributions.Count; i++)
            {
                ShuttleExternalMassContributionEntry contribution = contributions[i];
                if (contribution == null)
                {
                    continue;
                }

                collector.Add(
                    this.registration.FullRuntimeKey,
                    context != null ? context.ModuleInstanceID : null,
                    context != null ? context.ModuleDefName : null,
                    contribution.MassKg,
                    contribution.LabelKey,
                    contribution.DebugLabel,
                    context != null ? context.TicksGame : -1);
            }
        }

        public override void Tick(ShuttleModuleRuntimeContext context)
        {
            ExternalModuleRuntimeState state = this.GetExternalStateOrNull(context, "tick");
            ShuttleExternalRuntimeContext externalContext =
                ExternalRuntimeContextFactory.CreateRuntimeContext(this.registration, context, state);
            if (!this.PrepareStateBeforeRuntimeCall(externalContext, state))
            {
                return;
            }

            if (!this.IsRuntimeEnabled(state))
            {
                return;
            }

            long start = ExternalRuntimeMetricsRegistry.GetTimestamp();
            try
            {
                this.registration.System.Tick(externalContext);
            }
            catch (Exception exception)
            {
                this.MarkRuntimeExecutionFailed(
                    state,
                    "Tick",
                    context != null ? context.ModuleInstanceID : null,
                    context != null ? context.ModuleDefName : null,
                    context != null ? context.TicksGame : -1,
                    exception);
            }
            finally
            {
                ExternalRuntimeMetricsRegistry.RecordTickDuration(
                    context.ModuleInstanceID,
                    this.registration.FullRuntimeKey,
                    start);
            }
        }

        public override bool CanRemove(ShuttleModuleRuntimeContext context, out string reason)
        {
            reason = null;
            ExternalModuleRuntimeState state = this.GetExternalStateOrNull(context, "removal validation");
            ShuttleExternalRuntimeContext externalContext =
                ExternalRuntimeContextFactory.CreateRuntimeContext(this.registration, context, state);

            try
            {
                if (state == null)
                {
                    reason = "External runtime system " + this.GetRuntimeDebugKey() +
                        " has no valid external runtime state during removal validation.";
                    return false;
                }

                if (externalContext == null)
                {
                    reason = "External runtime system " + this.GetRuntimeDebugKey() +
                        " could not create removal validation context.";
                    return false;
                }

                if (!this.PrepareStateBeforeRuntimeCall(externalContext, state))
                {
                    reason = "External runtime system " + this.GetRuntimeDebugKey() +
                        " could not prepare state during removal validation.";
                    return false;
                }

                if (!this.IsRuntimeEnabled(state))
                {
                    reason = null;
                    return true;
                }

                return this.registration.System.CanRemove(externalContext, out reason);
            }
            catch (Exception exception)
            {
                reason = "External runtime system " + this.GetRuntimeDebugKey() +
                    " failed during removal validation.";
                this.MarkRuntimeExecutionFailed(
                    state,
                    "CanRemove",
                    context != null ? context.ModuleInstanceID : null,
                    context != null ? context.ModuleDefName : null,
                    context != null ? context.TicksGame : -1,
                    exception);
                return false;
            }
        }

        public override void OnInstalled(ShuttleModuleRuntimeContext context)
        {
            ExternalModuleRuntimeState state = this.GetExternalStateOrNull(context, "install notification");
            ShuttleExternalRuntimeContext externalContext =
                ExternalRuntimeContextFactory.CreateRuntimeContext(this.registration, context, state);
            if (!this.PrepareStateBeforeRuntimeCall(externalContext, state))
            {
                return;
            }

            try
            {
                this.registration.System.OnInstalled(externalContext);
            }
            catch (Exception exception)
            {
                this.MarkRuntimeExecutionFailed(
                    state,
                    "OnInstalled",
                    context != null ? context.ModuleInstanceID : null,
                    context != null ? context.ModuleDefName : null,
                    context != null ? context.TicksGame : -1,
                    exception);
            }
        }

        public override void OnRemoved(ShuttleModuleRuntimeContext context)
        {
            ExternalModuleRuntimeState state = this.GetExternalStateOrNull(context, "removal notification");
            ShuttleExternalRuntimeContext externalContext =
                ExternalRuntimeContextFactory.CreateRuntimeContext(this.registration, context, state);
            if (!this.PrepareStateBeforeRuntimeCall(externalContext, state))
            {
                return;
            }

            try
            {
                this.registration.System.OnRemoved(externalContext);
            }
            catch (Exception exception)
            {
                this.MarkRuntimeExecutionFailed(
                    state,
                    "OnRemoved",
                    context != null ? context.ModuleInstanceID : null,
                    context != null ? context.ModuleDefName : null,
                    context != null ? context.TicksGame : -1,
                    exception);
            }
        }

        public override void OnArrived(ShuttleModuleRuntimeContext context)
        {
            ExternalModuleRuntimeState state = this.GetExternalStateOrNull(context, "arrival notification");
            ShuttleExternalRuntimeContext externalContext =
                ExternalRuntimeContextFactory.CreateRuntimeContext(this.registration, context, state);
            if (!this.PrepareStateBeforeRuntimeCall(externalContext, state))
            {
                return;
            }

            if (!this.IsRuntimeEnabled(state))
            {
                return;
            }

            try
            {
                this.registration.System.OnArrived(externalContext);
            }
            catch (Exception exception)
            {
                this.MarkRuntimeExecutionFailed(
                    state,
                    "OnArrived",
                    context != null ? context.ModuleInstanceID : null,
                    context != null ? context.ModuleDefName : null,
                    context != null ? context.TicksGame : -1,
                    exception);
            }
        }

        public override bool PreLaunchValidate(
            ShuttleModulePreLaunchValidationContext context,
            out string reason)
        {
            reason = null;
            ShuttleLaunchModuleRecord moduleRecord = context != null ? context.ModuleRecord : null;
            ExternalModuleRuntimeState state;
            if (!this.TryGetExternalLaunchValidationState(context, out state, out reason))
            {
                return false;
            }

            if (!this.IsRuntimeEnabled(state))
            {
                reason = null;
                return true;
            }

            if (state.HasRuntimeFailure)
            {
                reason = this.GetBlockedByFailureReason(state, moduleRecord);
                this.LogStateFailureBlockedOnce(
                    "launch validation",
                    moduleRecord != null ? moduleRecord.ModuleInstanceID : null,
                    moduleRecord != null ? moduleRecord.ModuleDefName : null,
                    state,
                    this.GetSafeStateSchemaVersion());
                return false;
            }

            if (!this.IsStatePreparedForExternalCall(
                state,
                this.GetSafeStateSchemaVersion(),
                "launch validation",
                moduleRecord != null ? moduleRecord.ModuleInstanceID : null,
                moduleRecord != null ? moduleRecord.ModuleDefName : null,
                out reason))
            {
                return false;
            }

            ShuttleExternalLaunchValidationContext externalContext =
                ExternalRuntimeContextFactory.CreateLaunchValidationContext(
                    this.registration,
                    moduleRecord,
                    state);
            if (externalContext == null)
            {
                reason = "External runtime system " + this.GetRuntimeDebugKey() +
                    " could not create launch validation context for " +
                    this.GetLaunchModuleDebugName(moduleRecord) + ".";
                return false;
            }

            try
            {
                bool canLaunch = this.registration.System.PreLaunchValidate(
                    externalContext,
                    out reason);
                if (!canLaunch && string.IsNullOrEmpty(reason))
                {
                    reason = "External runtime system " + this.GetRuntimeDebugKey() +
                        " blocked launch for " + this.GetLaunchModuleDebugName(moduleRecord) + ".";
                }

                return canLaunch;
            }
            catch (Exception exception)
            {
                reason = "External runtime system " + this.GetRuntimeDebugKey() +
                    " failed during launch validation for " +
                    this.GetLaunchModuleDebugName(moduleRecord) + ".";
                this.MarkRuntimeExecutionFailed(
                    state,
                    "PreLaunchValidate",
                    moduleRecord != null ? moduleRecord.ModuleInstanceID : null,
                    moduleRecord != null ? moduleRecord.ModuleDefName : null,
                    ShuttleTickUtility.TicksGameOrZero(),
                    exception);
                return false;
            }
        }

        public override void OnLaunchSucceeded(ShuttleModuleLaunchContext context)
        {
            ShuttleLaunchModuleRecord moduleRecord = context != null ? context.ModuleRecord : null;
            ExternalModuleRuntimeState state = this.GetExternalLaunchStateOrNull(
                context,
                "launch success notification");
            if (state == null)
            {
                return;
            }

            if (!this.IsRuntimeEnabled(state))
            {
                return;
            }

            if (state.HasRuntimeFailure)
            {
                this.LogStateFailureBlockedOnce(
                    "launch success notification",
                    moduleRecord != null ? moduleRecord.ModuleInstanceID : null,
                    moduleRecord != null ? moduleRecord.ModuleDefName : null,
                    state,
                    this.GetSafeStateSchemaVersion());
                return;
            }

            string blockedReason;
            if (!this.IsStatePreparedForExternalCall(
                state,
                this.GetSafeStateSchemaVersion(),
                "launch success notification",
                moduleRecord != null ? moduleRecord.ModuleInstanceID : null,
                moduleRecord != null ? moduleRecord.ModuleDefName : null,
                out blockedReason))
            {
                return;
            }

            ShuttleExternalLaunchContext externalContext =
                ExternalRuntimeContextFactory.CreateLaunchContext(
                    this.registration,
                    moduleRecord,
                    state);
            if (externalContext == null)
            {
                Log.WarningOnce(ExternalShuttleRuntimeRegistry.LogPrefix +
                    "key '" + this.GetRuntimeDebugKey() +
                    "' could not create launch success context for " +
                    this.GetLaunchModuleDebugName(moduleRecord) + ".",
                    this.MakeAdapterWarningHash(
                        "launch-success-context-null",
                        moduleRecord != null ? moduleRecord.ModuleInstanceID : null,
                        null));
                return;
            }

            try
            {
                this.registration.System.OnLaunchSucceeded(externalContext);
            }
            catch (Exception exception)
            {
                this.MarkRuntimeExecutionFailed(
                    state,
                    "OnLaunchSucceeded",
                    moduleRecord != null ? moduleRecord.ModuleInstanceID : null,
                    moduleRecord != null ? moduleRecord.ModuleDefName : null,
                    ShuttleTickUtility.TicksGameOrZero(),
                    exception);
            }
        }

        private bool PrepareStateBeforeRuntimeCall(
            ShuttleExternalRuntimeContext externalContext,
            ExternalModuleRuntimeState state)
        {
            if (this.registration == null || this.registration.System == null ||
                externalContext == null || state == null)
            {
                return false;
            }

            state.EnsureIdentityDefaults(
                this.registration.OwnerPackageId,
                this.registration.FullRuntimeKey);

            int targetSchema = this.GetSafeStateSchemaVersion();
            if (state.HasRuntimeFailure)
            {
                this.LogStateFailureBlockedOnce(
                    "runtime preparation",
                    externalContext.Module != null ? externalContext.Module.ModuleInstanceId : null,
                    externalContext.Module != null ? externalContext.Module.ModuleDefName : null,
                    state,
                    targetSchema);
                return false;
            }

            if (!this.TryPrepareSchemaBeforeRuntimeCall(
                externalContext,
                state,
                targetSchema,
                "runtime preparation",
                externalContext.Module != null ? externalContext.Module.ModuleInstanceId : null,
                externalContext.Module != null ? externalContext.Module.ModuleDefName : null))
            {
                return false;
            }

            if (!state.Initialized)
            {
                try
                {
                    this.registration.System.Initialize(externalContext);
                }
                catch (Exception exception)
                {
                    string failureReason = this.BuildFailureReason(
                        "Initialize",
                        externalContext,
                        state.SchemaVersion,
                        targetSchema,
                        exception);
                    state.MarkInitializationFailed(
                        failureReason,
                        externalContext.TicksGame);
                    this.LogPrepareFailureOnce("Initialize", failureReason, exception);
                    return false;
                }

                state.MarkInitialized();
            }

            return true;
        }

        private ExternalModuleRuntimeState GetExternalStateOrNull(
            ShuttleModuleRuntimeContext context,
            string actionName)
        {
            if (context == null)
            {
                return null;
            }

            ExternalModuleRuntimeState state = context.State as ExternalModuleRuntimeState;
            if (state != null)
            {
                return state;
            }

            if (!this.warnedStateTypeMismatch)
            {
                this.warnedStateTypeMismatch = true;
                Log.Warning(ExternalShuttleRuntimeRegistry.LogPrefix +
                    "key '" + this.GetRuntimeDebugKey() +
                    "' expected ExternalModuleRuntimeState during " + actionName +
                    " but received " +
                    (context.State != null ? context.State.GetType().FullName : "null") + ".");
            }

            return null;
        }

        private bool TryGetExternalLaunchValidationState(
            ShuttleModulePreLaunchValidationContext context,
            out ExternalModuleRuntimeState state,
            out string reason)
        {
            state = null;
            reason = null;
            ShuttleLaunchModuleRecord moduleRecord = context != null ? context.ModuleRecord : null;

            if (context == null)
            {
                reason = "External runtime system " + this.GetRuntimeDebugKey() +
                    " has no launch validation context.";
                this.LogLaunchWarningOnce(
                    "launch-validation-context-null",
                    null,
                    reason);
                return false;
            }

            if (context.StateForRead == null || !context.StateForRead.HasState)
            {
                reason = "External runtime system " + this.GetRuntimeDebugKey() +
                    " has no external runtime state during launch validation for " +
                    this.GetLaunchModuleDebugName(moduleRecord) + ".";
                this.LogLaunchWarningOnce(
                    "launch-validation-state-missing",
                    moduleRecord,
                    reason);
                return false;
            }

            if (context.StateForRead.TryGetStateView(out state) && state != null)
            {
                return true;
            }

            reason = "External runtime system " + this.GetRuntimeDebugKey() +
                " expected ExternalModuleRuntimeState during launch validation for " +
                this.GetLaunchModuleDebugName(moduleRecord) +
                " but received " +
                (context.StateForRead.RuntimeStateTypeName ?? "null") + ".";
            this.LogLaunchWarningOnce(
                "launch-validation-state-type-mismatch",
                moduleRecord,
                reason);
            return false;
        }

        private ExternalModuleRuntimeState GetExternalLaunchStateOrNull(
            ShuttleModuleLaunchContext context,
            string actionName)
        {
            ShuttleLaunchModuleRecord moduleRecord = context != null ? context.ModuleRecord : null;
            if (context == null)
            {
                this.LogLaunchWarningOnce(
                    actionName + "-context-null",
                    null,
                    "key '" + this.GetRuntimeDebugKey() +
                    "' has no launch context during " + actionName + ".");
                return null;
            }

            ExternalModuleRuntimeState state = context.State as ExternalModuleRuntimeState;
            if (state != null)
            {
                return state;
            }

            this.LogLaunchWarningOnce(
                actionName + "-state-type-mismatch",
                moduleRecord,
                "key '" + this.GetRuntimeDebugKey() +
                "' expected ExternalModuleRuntimeState during " + actionName +
                " for " + this.GetLaunchModuleDebugName(moduleRecord) +
                " but received " +
                (context.State != null ? context.State.GetType().FullName : "null") + ".");
            return null;
        }

        private int GetSafeStateSchemaVersion()
        {
            if (this.registration == null || this.registration.System == null)
            {
                this.WarnNullRegistration();
                return 1;
            }

            try
            {
                int schemaVersion = this.registration.System.StateSchemaVersion;
                return schemaVersion > 0 ? schemaVersion : 1;
            }
            catch (Exception exception)
            {
                if (!this.warnedStateSchemaVersionException)
                {
                    this.warnedStateSchemaVersionException = true;
                    Log.Warning(ExternalShuttleRuntimeRegistry.LogPrefix +
                        "key '" + this.registration.FullRuntimeKey +
                        "' StateSchemaVersion failed. Using schema version 1. Exception: " +
                        exception);
                }

                return 1;
            }
        }

        private void WarnNullRegistration()
        {
            if (this.warnedNullRegistration)
            {
                return;
            }

            this.warnedNullRegistration = true;
            Log.Warning(ExternalShuttleRuntimeRegistry.LogPrefix +
                "external adapter created with null registration.");
        }

        private string GetRuntimeDebugKey()
        {
            return this.registration != null && !string.IsNullOrEmpty(this.registration.FullRuntimeKey)
                ? this.registration.FullRuntimeKey
                : "null";
        }

        private bool IsRuntimeEnabled(ExternalModuleRuntimeState state)
        {
            return ExternalRuntimeStateUtility.IsRuntimeEnabled(state);
        }

        private bool IsStatePreparedForExternalCall(
            ExternalModuleRuntimeState state,
            int targetSchema,
            string actionName,
            string moduleInstanceID,
            string moduleDefName,
            out string reason)
        {
            reason = null;
            if (state == null)
            {
                reason = "External runtime system " + this.GetRuntimeDebugKey() +
                    " has no runtime state during " + actionName + ".";
                this.LogUnpreparedStateBlockedOnce(
                    actionName,
                    moduleInstanceID,
                    moduleDefName,
                    reason);
                return false;
            }

            if (state.HasRuntimeFailure)
            {
                reason = "External runtime system " + this.GetRuntimeDebugKey() +
                    " is blocked by a previous runtime failure.";
                this.LogStateFailureBlockedOnce(
                    actionName,
                    moduleInstanceID,
                    moduleDefName,
                    state,
                    targetSchema);
                return false;
            }

            if (!state.Initialized)
            {
                reason = "External runtime system " + this.GetRuntimeDebugKey() +
                    " state has not been initialized yet for module " +
                    (moduleInstanceID ?? "unknown") +
                    " (" + (moduleDefName ?? "missing def") + ").";
                this.LogUnpreparedStateBlockedOnce(
                    actionName,
                    moduleInstanceID,
                    moduleDefName,
                    reason);
                return false;
            }

            if (state.SchemaVersion != targetSchema)
            {
                int loadedSchema = state.SchemaVersion;
                reason = "External runtime system " + this.GetRuntimeDebugKey() +
                    " state schema " + loadedSchema +
                    " is not prepared for target schema " + targetSchema +
                    " before " + actionName +
                    ". Runtime sync must migrate and initialize it before this call.";
                this.LogUnpreparedStateBlockedOnce(
                    actionName,
                    moduleInstanceID,
                    moduleDefName,
                    reason);
                return false;
            }

            return true;
        }

        private string BuildFailureReason(
            string operation,
            ShuttleExternalRuntimeContext externalContext,
            int loadedSchema,
            int targetSchema,
            Exception exception)
        {
            return "External runtime " + operation + " failed for runtime key '" +
                this.GetRuntimeDebugKey() + "', module " +
                this.GetRuntimeModuleDebugName(externalContext) +
                ", loaded schema " + loadedSchema +
                ", target schema " + targetSchema +
                ". Exception: " +
                (exception != null ? exception.GetType().Name + ": " + exception.Message : "null");
        }

        private bool TryPrepareSchemaBeforeRuntimeCall(
            ShuttleExternalRuntimeContext externalContext,
            ExternalModuleRuntimeState state,
            int targetSchema,
            string actionName,
            string moduleInstanceID,
            string moduleDefName)
        {
            if (state == null)
            {
                return false;
            }

            int loadedSchema = state.SchemaVersion;
            if (loadedSchema == targetSchema)
            {
                return true;
            }

            if (loadedSchema > targetSchema)
            {
                string futureReason = "External runtime migration blocked for runtime key '" +
                    this.GetRuntimeDebugKey() + "', module " +
                    (moduleInstanceID ?? "unknown") +
                    " (" + (moduleDefName ?? "missing def") + ")" +
                    " during " + actionName +
                    ". Loaded schema " + loadedSchema +
                    " is newer than target schema " + targetSchema +
                    ". State values were preserved.";
                state.MarkMigrationFailed(
                    futureReason,
                    externalContext != null ? externalContext.TicksGame : ShuttleTickUtility.TicksGameOrZero());
                Log.WarningOnce(
                    ExternalShuttleRuntimeRegistry.LogPrefix + futureReason,
                    this.MakeAdapterWarningHash(
                        "migration-future-schema",
                        moduleInstanceID,
                        loadedSchema + "->" + targetSchema));
                return false;
            }

            Log.Message(
                ExternalShuttleRuntimeRegistry.LogPrefix +
                "starting external runtime migration for runtime key '" +
                this.GetRuntimeDebugKey() +
                "', module " + (moduleInstanceID ?? "unknown") +
                " (" + (moduleDefName ?? "missing def") + ")" +
                ". Loaded schema " + loadedSchema +
                ", target schema " + targetSchema + ".");

            try
            {
                this.registration.System.Migrate(externalContext, loadedSchema);
            }
            catch (Exception exception)
            {
                string failureReason = this.BuildFailureReason(
                    "Migrate",
                    externalContext,
                    loadedSchema,
                    targetSchema,
                    exception);
                state.MarkMigrationFailed(
                    failureReason,
                    externalContext != null ? externalContext.TicksGame : ShuttleTickUtility.TicksGameOrZero());
                Log.ErrorOnce(
                    ExternalShuttleRuntimeRegistry.LogPrefix +
                    failureReason +
                    " State values were preserved and future calls for this external runtime state will be skipped. " +
                    exception,
                    this.MakeAdapterWarningHash(
                        "migration-failure",
                        moduleInstanceID,
                        exception.GetType().FullName + exception.Message));
                return false;
            }

            state.SetSchemaVersion(targetSchema);
            Log.Message(
                ExternalShuttleRuntimeRegistry.LogPrefix +
                "external runtime migration succeeded for runtime key '" +
                this.GetRuntimeDebugKey() +
                "', module " + (moduleInstanceID ?? "unknown") +
                " (" + (moduleDefName ?? "missing def") + ")" +
                ". Loaded schema " + loadedSchema +
                ", target schema " + targetSchema +
                ". State values were preserved unless migration changed them.");
            return true;
        }

        private string GetBlockedByFailureReason(
            ExternalModuleRuntimeState state,
            ShuttleLaunchModuleRecord moduleRecord)
        {
            return "External runtime system " + this.GetRuntimeDebugKey() +
                " is unavailable for " + this.GetLaunchModuleDebugName(moduleRecord) +
                " because a previous runtime failure occurred. Last failure: " +
                (state != null && !string.IsNullOrEmpty(state.LastFailureMessage)
                    ? state.LastFailureMessage
                    : "unknown");
        }

        private void MarkRuntimeExecutionFailed(
            ExternalModuleRuntimeState state,
            string operation,
            string moduleInstanceID,
            string moduleDefName,
            int ticksGame,
            Exception exception)
        {
            if (state == null)
            {
                return;
            }

            string failureReason = this.BuildExecutionFailureReason(
                operation,
                moduleInstanceID,
                moduleDefName,
                state.SchemaVersion,
                exception);
            state.MarkRuntimeExecutionFailed(failureReason, ticksGame);
            Log.ErrorOnce(
                ExternalShuttleRuntimeRegistry.LogPrefix +
                failureReason + " Future calls for this external runtime state will be skipped. " +
                exception,
                this.MakeAdapterWarningHash(
                    "execution-failure-" + operation,
                    moduleInstanceID,
                    exception != null ? exception.GetType().FullName + exception.Message : null));
        }

        private string BuildExecutionFailureReason(
            string operation,
            string moduleInstanceID,
            string moduleDefName,
            int schemaVersion,
            Exception exception)
        {
            return "External runtime " + operation + " failed for runtime key '" +
                this.GetRuntimeDebugKey() + "', module " +
                (moduleInstanceID ?? "unknown") +
                " (" + (moduleDefName ?? "missing def") + ")" +
                ", schema " + schemaVersion +
                ". Exception: " +
                (exception != null ? exception.GetType().Name + ": " + exception.Message : "null");
        }

        private void LogPrepareFailureOnce(
            string operation,
            string failureReason,
            Exception exception)
        {
            Log.ErrorOnce(
                ExternalShuttleRuntimeRegistry.LogPrefix +
                failureReason + " Future calls for this external runtime state will be skipped. " +
                exception,
                this.MakeAdapterWarningHash(operation, failureReason, null));
        }

        private void LogStateFailureBlockedOnce(
            string actionName,
            string moduleInstanceID,
            string moduleDefName,
            ExternalModuleRuntimeState state,
            int targetSchema)
        {
            string reason = state != null && !string.IsNullOrEmpty(state.LastFailureMessage)
                ? state.LastFailureMessage
                : "unknown prior runtime failure";
            Log.WarningOnce(
                ExternalShuttleRuntimeRegistry.LogPrefix +
                "skipping " + actionName +
                " for runtime key '" + this.GetRuntimeDebugKey() +
                "', module " + (moduleInstanceID ?? "unknown") +
                " (" + (moduleDefName ?? "missing def") + ")" +
                ", state schema " + (state != null ? state.SchemaVersion.ToString() : "null") +
                ", target schema " + targetSchema +
                ". Last failure: " + reason,
                this.MakeAdapterWarningHash(
                    "blocked-" + actionName,
                    moduleInstanceID,
                    reason));
        }

        private void LogUnpreparedStateBlockedOnce(
            string actionName,
            string moduleInstanceID,
            string moduleDefName,
            string reason)
        {
            Log.WarningOnce(
                ExternalShuttleRuntimeRegistry.LogPrefix +
                "skipping " + actionName +
                " for runtime key '" + this.GetRuntimeDebugKey() +
                "', module " + (moduleInstanceID ?? "unknown") +
                " (" + (moduleDefName ?? "missing def") + "). " +
                (reason ?? "Runtime state is not prepared."),
                this.MakeAdapterWarningHash(
                    "unprepared-" + actionName,
                    moduleInstanceID,
                    reason));
        }

        private void DevSchemaResetState(
            ExternalModuleRuntimeState state,
            int targetSchema,
            string actionName,
            string moduleInstanceID,
            string moduleDefName)
        {
            if (state == null)
            {
                return;
            }

            int loadedSchema = state.SchemaVersion;
            state.DevSchemaReset(targetSchema);
            Log.WarningOnce(
                ExternalShuttleRuntimeRegistry.LogPrefix +
                "explicit DevMode schema reset for runtime key '" + this.GetRuntimeDebugKey() +
                "', module " + (moduleInstanceID ?? "unknown") +
                " (" + (moduleDefName ?? "missing def") + ") during " + actionName +
                ". Loaded schema " + loadedSchema +
                ", target schema " + targetSchema +
                ". State values and previous runtime failure flags were cleared.",
                this.MakeAdapterWarningHash(
                    "dev-schema-reset-" + actionName,
                    moduleInstanceID,
                    loadedSchema + "->" + targetSchema));
        }

        private void LogLaunchWarningOnce(
            string actionName,
            ShuttleLaunchModuleRecord moduleRecord,
            string reason)
        {
            Log.WarningOnce(
                ExternalShuttleRuntimeRegistry.LogPrefix + reason,
                this.MakeAdapterWarningHash(
                    actionName,
                    moduleRecord != null ? moduleRecord.ModuleInstanceID : null,
                    reason));
        }

        private string GetRuntimeModuleDebugName(ShuttleExternalRuntimeContext externalContext)
        {
            if (externalContext == null || externalContext.Module == null)
            {
                return "null module";
            }

            string defName = !string.IsNullOrEmpty(externalContext.Module.ModuleDefName)
                ? externalContext.Module.ModuleDefName
                : "missing def";
            return externalContext.Module.ModuleInstanceId + " (" + defName + ")";
        }

        private int MakeAdapterWarningHash(
            string actionName,
            string moduleOrReason,
            string reason)
        {
            unchecked
            {
                int hash = 23;
                hash = (hash * 31) + this.GetRuntimeDebugKey().GetHashCode();
                hash = (hash * 31) + (actionName != null ? actionName.GetHashCode() : 0);
                hash = (hash * 31) + (moduleOrReason != null ? moduleOrReason.GetHashCode() : 0);
                hash = (hash * 31) + (reason != null ? reason.GetHashCode() : 0);
                return hash;
            }
        }

        private string GetLaunchModuleDebugName(ShuttleLaunchModuleRecord moduleRecord)
        {
            if (moduleRecord == null)
            {
                return "null module";
            }

            string defName = !string.IsNullOrEmpty(moduleRecord.ModuleDefName)
                ? moduleRecord.ModuleDefName
                : "missing def";
            return moduleRecord.ModuleInstanceID + " (" + defName + ")";
        }
    }
}
