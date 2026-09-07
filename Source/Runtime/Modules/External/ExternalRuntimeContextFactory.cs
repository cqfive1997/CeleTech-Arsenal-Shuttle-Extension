using System;
using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;
using CeleTech.ShuttleExtension.ModularShuttle.API.Runtime;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Launch;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal static class ExternalRuntimeContextFactory
    {
        internal static ShuttleExternalRuntimeContext CreateRuntimeContext(
            ExternalRuntimeRegistration registration,
            ShuttleModule module,
            ExternalModuleRuntimeState state,
            int ticksGame)
        {
            if (registration == null)
            {
                return null;
            }

            return new ShuttleExternalRuntimeContext(
                registration.OwnerPackageId,
                registration.FullRuntimeKey,
                ExternalRuntimeInfoFactory.CreateModuleInfo(module),
                ticksGame,
                new ExternalRuntimeStateStore(state),
                false,
                null,
                null,
                null);
        }

        internal static ShuttleExternalRuntimeContext CreateRuntimeContext(
            ExternalRuntimeRegistration registration,
            ShuttleModuleRuntimeContext internalContext,
            ExternalModuleRuntimeState state)
        {
            if (registration == null || internalContext == null)
            {
                return null;
            }

            Func<float, bool> trackedStoredEnergyConsumer =
                ExternalRuntimeMetricsRegistry.CreateTrackedStoredEnergyConsumer(
                    internalContext.ModuleInstanceID,
                    registration.FullRuntimeKey,
                    internalContext.TryConsumeStoredEnergyWd);

            return new ShuttleExternalRuntimeContext(
                registration.OwnerPackageId,
                registration.FullRuntimeKey,
                ExternalRuntimeInfoFactory.CreateModuleInfo(internalContext),
                internalContext.TicksGame,
                new ExternalRuntimeStateStore(state),
                internalContext.InternalBusPowered,
                trackedStoredEnergyConsumer,
                new ShuttleExternalOccupantReadPort(internalContext.Host),
                new ShuttleExternalHostReadPort(internalContext.Host),
                ResolveLaunchOccupantHandoffProvider(internalContext.Host));
        }

        internal static ShuttleExternalPowerDemandContext CreatePowerDemandContext(
            ShuttleModule module,
            ExternalModuleRuntimeState state)
        {
            return new ShuttleExternalPowerDemandContext(
                ExternalRuntimeInfoFactory.CreateModuleInfo(module),
                new ExternalRuntimeStateReader(state));
        }

        internal static ShuttleExternalPowerDemandContext CreatePowerDemandContext(
            ShuttleModuleRuntimeContext internalContext,
            ExternalModuleRuntimeState state)
        {
            if (internalContext == null)
            {
                return null;
            }

            return new ShuttleExternalPowerDemandContext(
                ExternalRuntimeInfoFactory.CreateModuleInfo(internalContext),
                new ExternalRuntimeStateReader(state));
        }

        internal static ShuttleExternalPowerDemandContext CreatePowerDemandContext(
            ShuttleExternalModuleInfo moduleInfo,
            IShuttleExternalRuntimeStateReader stateReader)
        {
            if (moduleInfo == null || stateReader == null)
            {
                return null;
            }

            return new ShuttleExternalPowerDemandContext(moduleInfo, stateReader);
        }

        internal static ShuttleExternalMassContributionContext CreateMassContributionContext(
            ShuttleModuleRuntimeContext internalContext,
            ExternalModuleRuntimeState state)
        {
            if (internalContext == null)
            {
                return null;
            }

            return new ShuttleExternalMassContributionContext(
                ExternalRuntimeInfoFactory.CreateModuleInfo(internalContext),
                new ExternalRuntimeStateReader(state),
                internalContext.TicksGame);
        }

        internal static ShuttleExternalLaunchValidationContext CreateLaunchValidationContext(
            ShuttleModule module,
            ExternalModuleRuntimeState state)
        {
            return new ShuttleExternalLaunchValidationContext(
                ExternalRuntimeInfoFactory.CreateLaunchModuleInfo(module),
                new ExternalRuntimeStateReader(state));
        }

        internal static ShuttleExternalLaunchContext CreateLaunchContext(
            ShuttleModule module,
            ExternalModuleRuntimeState state)
        {
            return new ShuttleExternalLaunchContext(
                ExternalRuntimeInfoFactory.CreateLaunchModuleInfo(module),
                new ExternalRuntimeStateReader(state));
        }

        internal static ShuttleExternalLaunchValidationContext CreateLaunchValidationContext(
            ExternalRuntimeRegistration registration,
            ShuttleLaunchModuleRecord moduleRecord,
            ExternalModuleRuntimeState state)
        {
            if (registration == null || moduleRecord == null)
            {
                return null;
            }

            return new ShuttleExternalLaunchValidationContext(
                ExternalRuntimeInfoFactory.CreateLaunchModuleInfo(moduleRecord),
                new ExternalRuntimeStateReader(state));
        }

        internal static ShuttleExternalLaunchContext CreateLaunchContext(
            ExternalRuntimeRegistration registration,
            ShuttleLaunchModuleRecord moduleRecord,
            ExternalModuleRuntimeState state)
        {
            if (registration == null || moduleRecord == null)
            {
                return null;
            }

            return new ShuttleExternalLaunchContext(
                ExternalRuntimeInfoFactory.CreateLaunchModuleInfo(moduleRecord),
                new ExternalRuntimeStateReader(state));
        }

        private static IShuttleExternalLaunchOccupantHandoffProvider ResolveLaunchOccupantHandoffProvider(
            ThingWithComps host)
        {
            IShuttleExternalLaunchOccupantHandoffProvider provider = null;
            CompModularShuttleCore core = host != null
                ? host.TryGetComp<CompModularShuttleCore>()
                : null;
            return core != null &&
                core.TryGetExternalLaunchOccupantHandoffProvider(out provider)
                    ? provider
                    : null;
        }
    }
}
