using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules
{
    internal sealed class ShuttleRuntimeMassContributionService
    {
        private readonly BuiltInShuttleModuleRuntimeRegistry registry;
        private readonly ShuttleRuntimeDispatchSupport support;

        internal ShuttleRuntimeMassContributionService(
            BuiltInShuttleModuleRuntimeRegistry registry,
            ShuttleRuntimeDispatchSupport support)
        {
            this.registry = registry;
            this.support = support;
        }

        internal ShuttleRuntimeMassContributionSnapshot CollectMassContributions(
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            int ticksGame)
        {
            ShuttleRuntimeMassContributionCollector collector =
                new ShuttleRuntimeMassContributionCollector();
            if (assemblyState == null ||
                runtimeState == null ||
                runtimeState.Modules == null ||
                this.registry == null ||
                this.support == null)
            {
                return collector.BuildSnapshot();
            }

            IReadOnlyList<IShuttleModuleRuntimeSystem> systems = this.registry.SystemsForRead;
            IReadOnlyList<ShuttleModule> modules = assemblyState.Modules;
            for (int systemIndex = 0; systems != null && systemIndex < systems.Count; systemIndex++)
            {
                IShuttleModuleRuntimeSystem system = systems[systemIndex];
                if (system == null)
                {
                    continue;
                }

                for (int moduleIndex = 0; modules != null && moduleIndex < modules.Count; moduleIndex++)
                {
                    ShuttleModule module = modules[moduleIndex];
                    if (module == null || !module.IsEnabled)
                    {
                        continue;
                    }

                    string appliesFailureReason;
                    if (!this.support.AppliesToSystem(
                            system,
                            module,
                            "mass contribution collection",
                            out appliesFailureReason))
                    {
                        continue;
                    }

                    IShuttleModuleRuntimeState state;
                    if (!runtimeState.Modules.TryGetState(
                            module.ModuleInstanceID,
                            system.RuntimeSystemKey,
                            out state))
                    {
                        continue;
                    }

                    ShuttleModuleRuntimeContext context = this.support.CreateRuntimeContext(
                        host,
                        assemblyState,
                        profile,
                        runtimeState,
                        module,
                        state,
                        null,
                        null,
                        null,
                        ticksGame);
                    this.support.TryRunRuntimeAction(
                        system,
                        module,
                        "mass contribution collection",
                        () => system.CollectMassContribution(context, collector));
                }
            }

            return collector.BuildSnapshot();
        }
    }
}
