using System;
using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Internal deterministic selector for shuttle weapon backends. Registration and probing
    /// are separate from runtime execution; this class never ticks, fires, reloads, or mutates
    /// weapon state.
    /// </summary>
    internal sealed class ShuttleWeaponBackendRegistry
    {
        private static readonly ShuttleWeaponBackendRegistry SharedRegistry = CreateDefault();

        private readonly List<IShuttleWeaponBackendFactory> factories =
            new List<IShuttleWeaponBackendFactory>();

        internal static ShuttleWeaponBackendRegistry Shared
        {
            get { return SharedRegistry; }
        }

        private static ShuttleWeaponBackendRegistry CreateDefault()
        {
            ShuttleWeaponBackendRegistry registry = new ShuttleWeaponBackendRegistry();
            registry.Register(new NativeVerbWeaponBackendFactory());
            return registry;
        }

        internal bool Register(IShuttleWeaponBackendFactory factory)
        {
            if (factory == null || string.IsNullOrEmpty(factory.BackendId))
            {
                Log.Warning("[CeleTech Shuttle] Weapon backend registration rejected: missing factory or backend ID.");
                return false;
            }

            for (int i = 0; i < this.factories.Count; i++)
            {
                IShuttleWeaponBackendFactory existing = this.factories[i];
                if (existing != null &&
                    string.Equals(existing.BackendId, factory.BackendId, StringComparison.Ordinal))
                {
                    Log.Warning("[CeleTech Shuttle] Duplicate weapon backend ID " +
                        factory.BackendId + " ignored.");
                    return false;
                }
            }

            this.factories.Add(factory);
            return true;
        }

        internal bool TryResolve(
            ShuttleWeaponBackendProbeContext context,
            out ShuttleWeaponBackendBinding binding,
            out ShuttleWeaponCompatibilityReport report)
        {
            binding = null;
            report = null;
            IShuttleWeaponBackendFactory selectedFactory = null;
            ShuttleWeaponCompatibilityReport selectedReport = null;
            IShuttleWeaponBackendFactory rejectingFactory = null;
            ShuttleWeaponCompatibilityReport rejectingReport = null;

            for (int i = 0; i < this.factories.Count; i++)
            {
                IShuttleWeaponBackendFactory factory = this.factories[i];
                if (factory == null)
                {
                    continue;
                }

                ShuttleWeaponCompatibilityReport candidate = factory.Probe(context);
                if (candidate == null)
                {
                    continue;
                }

                if (!candidate.IsSupported)
                {
                    if (candidate.BlocksLowerPriorityFallback &&
                        (rejectingFactory == null || factory.Priority > rejectingFactory.Priority))
                    {
                        rejectingFactory = factory;
                        rejectingReport = candidate;
                    }
                    else if (report == null)
                    {
                        report = candidate;
                    }

                    continue;
                }

                if (selectedFactory == null || factory.Priority > selectedFactory.Priority)
                {
                    selectedFactory = factory;
                    selectedReport = candidate;
                }
            }

            // A specialized backend that recognizes but cannot safely host a weapon must
            // prevent an equal/lower-priority generic backend from running it partially.
            if (rejectingFactory != null &&
                (selectedFactory == null || rejectingFactory.Priority >= selectedFactory.Priority))
            {
                report = rejectingReport;
                return false;
            }

            if (selectedFactory == null)
            {
                return false;
            }

            ShuttleWeaponBackendBinding created = selectedFactory.CreateBinding(context);
            if (created == null ||
                !created.IsComplete ||
                !string.Equals(created.BackendId, selectedFactory.BackendId, StringComparison.Ordinal))
            {
                report = ShuttleWeaponCompatibilityReport.Rejected(
                    selectedFactory.BackendId,
                    "binding-incomplete");
                return false;
            }

            binding = created;
            report = selectedReport;
            return true;
        }
    }
}
