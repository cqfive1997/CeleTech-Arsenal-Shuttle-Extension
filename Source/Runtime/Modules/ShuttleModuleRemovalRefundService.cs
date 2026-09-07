using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules
{
    /// <summary>
    /// Builds a detached refund quote for one installed module. It never creates Things,
    /// mutates runtime state, or decides where refunds are returned.
    /// </summary>
    internal sealed class ShuttleModuleRemovalRefundService
    {
        private readonly BuiltInShuttleModuleRuntimeRegistry registry;
        private readonly ShuttleRuntimeDispatchSupport support;

        internal ShuttleModuleRemovalRefundService(
            BuiltInShuttleModuleRuntimeRegistry registry,
            ShuttleRuntimeDispatchSupport support)
        {
            this.registry = registry;
            this.support = support;
        }

        internal bool TryCollect(
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            ShuttleModule module,
            IShuttleStoredEnergySink storedEnergySink,
            int ticksGame,
            out List<ThingDefCountClass> refunds,
            out string failureReason)
        {
            ShuttleModuleRemovalRefundCollector collector =
                new ShuttleModuleRemovalRefundCollector();
            refunds = collector.BuildList();
            failureReason = null;
            if (module == null || this.registry == null || this.support == null)
            {
                return true;
            }

            IReadOnlyList<IShuttleModuleRuntimeSystem> systems = this.registry.SystemsForRead;
            for (int i = 0; systems != null && i < systems.Count; i++)
            {
                IShuttleModuleRuntimeSystem system = systems[i];
                IShuttleModuleRemovalRefundContributor contributor =
                    system as IShuttleModuleRemovalRefundContributor;
                if (contributor == null)
                {
                    continue;
                }

                string appliesFailureReason;
                if (!this.support.AppliesToSystem(
                        system,
                        module,
                        "removal refund collection",
                        out appliesFailureReason))
                {
                    if (!string.IsNullOrEmpty(appliesFailureReason))
                    {
                        failureReason = "CT_Shuttle_Command_ModuleRemovalRefundFailed"
                            .Translate()
                            .ToString();
                        return false;
                    }

                    continue;
                }

                IShuttleModuleRuntimeState state = null;
                if (runtimeState != null && runtimeState.Modules != null)
                {
                    runtimeState.Modules.TryGetState(
                        module.ModuleInstanceID,
                        system.RuntimeSystemKey,
                        out state);
                }

                // Missing payloads are allowed for recovery. With no durable state there is no
                // loaded count to quote, and blocking removal would strand damaged saves.
                if (state == null)
                {
                    this.support.LogMissingState(
                        module,
                        system,
                        "removal refund collection");
                    continue;
                }

                ShuttleModuleRuntimeContext context = this.support.CreateRuntimeContext(
                    host,
                    assemblyState,
                    profile,
                    runtimeState,
                    module,
                    state,
                    storedEnergySink,
                    null,
                    null,
                    ticksGame);
                try
                {
                    if (!contributor.TryCollectRemovalRefunds(
                            context,
                            collector,
                            out failureReason))
                    {
                        refunds = collector.BuildList();
                        return false;
                    }
                }
                catch (Exception exception)
                {
                    Log.ErrorOnce(
                        "[CeleTech Shuttle] Module runtime removal refund collection failed for " +
                        module.ModuleInstanceID + " / " + system.RuntimeSystemKey +
                        ". Exception: " + exception,
                        this.support.MakeRuntimeLogHash(
                            "removal-refund-collection",
                            module.ModuleInstanceID,
                            system.RuntimeSystemKey,
                            exception.GetType().FullName + exception.Message));
                    failureReason = "CT_Shuttle_Command_ModuleRemovalRefundFailed"
                        .Translate()
                        .ToString();
                    refunds = collector.BuildList();
                    return false;
                }
            }

            refunds = collector.BuildList();
            return true;
        }
    }
}
