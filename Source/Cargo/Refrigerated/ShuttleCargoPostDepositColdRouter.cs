using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Transactions;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated
{
    /// <summary>
    /// Routes only stacks named by a successful ordinary-cargo deposit receipt. Policy stays
    /// here; actual holder mutation and rollback remain in the cold-transfer service.
    /// </summary>
    internal sealed class ShuttleCargoPostDepositColdRouter :
        IShuttleCargoPostDepositRouter
    {
        private readonly ThingWithComps host;
        private readonly ShuttleAssemblyState assemblyState;
        private readonly ShuttleRuntimeState runtimeState;
        private readonly ShuttleProfile profile;
        private readonly IShuttleCargoColdTransferService coldTransferService;
        private readonly ShuttleColdTransferQueryService queryService;
        private readonly ShuttleColdTransferPreflightService preflightService =
            new ShuttleColdTransferPreflightService();

        internal ShuttleCargoPostDepositColdRouter(
            ThingWithComps host,
            ShuttleAssemblyState assemblyState,
            ShuttleRuntimeState runtimeState,
            ShuttleProfile profile,
            IShuttleCargoBackend cargoBackend,
            IShuttleCargoColdTransferService coldTransferService)
        {
            this.host = host;
            this.assemblyState = assemblyState;
            this.runtimeState = runtimeState;
            this.profile = profile;
            this.coldTransferService = coldTransferService;
            this.queryService = new ShuttleColdTransferQueryService(host, cargoBackend);
        }

        public bool TryRoute(
            ShuttleCargoDepositReceipt receipt,
            string reason,
            out int movedStackCount,
            out int movedThingCount,
            out string failureReason)
        {
            movedStackCount = 0;
            movedThingCount = 0;
            failureReason = null;
            if (receipt == null || !receipt.HasStacks)
            {
                return true;
            }

            if (!this.CanAttemptRouting() || this.HasActiveRefrigeratedLaunchTransfer())
            {
                return true;
            }

            CompShuttleRefrigeratedCargoRegistry registry = this.host != null
                ? this.host.TryGetComp<CompShuttleRefrigeratedCargoRegistry>()
                : null;
            if (registry == null || this.assemblyState == null)
            {
                return true;
            }

            registry.Reconcile(this.assemblyState, this.runtimeState);
            IReadOnlyList<CargoStackRef> stackRefs = receipt.StackRefs;
            for (int i = 0; stackRefs != null && i < stackRefs.Count; i++)
            {
                CargoStackRef stackRef = stackRefs[i];
                int loadedIndex;
                Thing sourceThing;
                string resolveFailure;
                if (!this.queryService.TryResolveLoadedCargoByIdentity(
                        stackRef,
                        out loadedIndex,
                        out sourceThing,
                        out resolveFailure))
                {
                    failureReason = resolveFailure;
                    return false;
                }

                int moveCount;
                ShuttleModule coldModule;
                if (!this.TrySelectColdDestination(
                        registry,
                        sourceThing,
                        out coldModule,
                        out moveCount))
                {
                    continue;
                }

                int movedCount;
                string transferFailure;
                if (!this.coldTransferService.TryTransferLoadedCargoToCold(
                        coldModule.ModuleInstanceID,
                        stackRef.SourceIndex,
                        loadedIndex,
                        stackRef.ThingIDNumber,
                        stackRef.DefName,
                        moveCount,
                        string.IsNullOrEmpty(reason)
                            ? "post-deposit-cold-routing"
                            : reason,
                        out movedCount,
                        out transferFailure))
                {
                    failureReason = transferFailure ??
                        "Post-deposit refrigerated transfer failed.";
                    return false;
                }

                if (movedCount > 0)
                {
                    movedStackCount++;
                    movedThingCount += movedCount;
                }
            }

            return true;
        }

        private bool TrySelectColdDestination(
            CompShuttleRefrigeratedCargoRegistry registry,
            Thing thing,
            out ShuttleModule selectedModule,
            out int moveCount)
        {
            selectedModule = null;
            moveCount = 0;
            IReadOnlyList<ShuttleModule> modules = this.assemblyState != null
                ? this.assemblyState.Modules
                : null;
            for (int i = 0; modules != null && i < modules.Count; i++)
            {
                ShuttleModule module = modules[i];
                ShuttleRefrigeratedCargoModuleDef moduleDef = module != null
                    ? module.ModuleDef as ShuttleRefrigeratedCargoModuleDef
                    : null;
                if (module == null ||
                    moduleDef == null ||
                    !module.IsEnabled ||
                    string.IsNullOrEmpty(module.ModuleInstanceID))
                {
                    continue;
                }

                ShuttleRefrigeratedCargoAutoTransferConfig config =
                    this.ResolveAutoTransferConfig(module.ModuleInstanceID, moduleDef);
                if (config == null || !config.AutoTransferEnabled)
                {
                    continue;
                }

                RefrigeratedCargoRecord record;
                if (!registry.TryGetRecord(module.ModuleInstanceID, out record) ||
                    record == null ||
                    (moduleDef.autoTransferOnlyWhenCoolingActive &&
                        !record.CoolingActive) ||
                    !ShuttleColdTransferMatcher.PassesAutoTransferSelection(
                        thing,
                        moduleDef,
                        config.AutoTransferFilter,
                        config.HasCustomAutoTransferFilter))
                {
                    continue;
                }

                int candidateCount = thing != null ? thing.stackCount : 0;
                float fullMassKg = CargoDisplayUtility.GetThingMass(
                    thing,
                    candidateCount);
                string capacityFailure;
                if (!this.preflightService.HasColdCapacity(
                        record,
                        this.profile,
                        registry,
                        fullMassKg,
                        out capacityFailure))
                {
                    if (!moduleDef.allowPartialStackTransfer)
                    {
                        continue;
                    }

                    candidateCount = this.preflightService
                        .GetMaxPartialCountForColdCapacity(
                            thing,
                            record,
                            this.profile,
                            registry);
                    if (candidateCount <= 0)
                    {
                        continue;
                    }
                }

                selectedModule = module;
                moveCount = candidateCount;
                return true;
            }

            return false;
        }

        private ShuttleRefrigeratedCargoAutoTransferConfig ResolveAutoTransferConfig(
            string moduleInstanceID,
            ShuttleRefrigeratedCargoModuleDef moduleDef)
        {
            ShuttleRefrigeratedCargoConfigState configState = this.assemblyState != null
                ? this.assemblyState.RefrigeratedCargoConfig
                : null;
            return configState != null
                ? configState.BuildEffectiveAutoTransferConfig(
                    moduleInstanceID,
                    moduleDef)
                : ShuttleRefrigeratedCargoAutoTransferConfig.FromModuleDef(
                    moduleInstanceID,
                    moduleDef);
        }

        private bool CanAttemptRouting()
        {
            return this.host != null &&
                this.coldTransferService != null &&
                this.queryService != null &&
                this.profile != null &&
                this.profile.CargoLogistics != null &&
                this.profile.CargoLogistics.HasCargoLogistics &&
                this.profile.CargoLogistics.SupportsItemTransfer;
        }

        private bool HasActiveRefrigeratedLaunchTransfer()
        {
            CompShuttleHolderLaunchTransferState transferState = this.host != null
                ? this.host.TryGetComp<CompShuttleHolderLaunchTransferState>()
                : null;
            return transferState != null &&
                transferState.HasRefrigeratedCargoLaunchTransfer;
        }
    }
}
