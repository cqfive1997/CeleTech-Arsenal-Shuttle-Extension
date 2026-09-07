using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.AutoWorkTable;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Habitat;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.RefrigeratedCargo;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Scanner;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Shield;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Shield.Surface;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules
{
    /// <summary>
    /// Explicit built-in runtime registry. Keeping this hardcoded avoids reflection loading,
    /// XML class-name activation, and accidental third-party runtime API exposure.
    /// </summary>
    internal sealed class BuiltInShuttleModuleRuntimeRegistry
    {
        private readonly List<IShuttleModuleRuntimeSystem> builtInSystems =
            new List<IShuttleModuleRuntimeSystem>();
        private List<IShuttleModuleRuntimeSystem> compositeSystems =
            new List<IShuttleModuleRuntimeSystem>();
        private int externalRegistryRevision = -1;
        private bool compositeSystemsDirty = true;
        private int dispatchFingerprint;
        private bool dispatchFingerprintDirty = true;
        private readonly HashSet<string> externalDuplicateKeyWarnings =
            new HashSet<string>(StringComparer.Ordinal);

        public static BuiltInShuttleModuleRuntimeRegistry CreateDefault()
        {
            // Built-in runtime systems must be added here with explicit code registrations.
            // Do not use reflection, XML class-name activation, or external mod discovery.
            BuiltInShuttleModuleRuntimeRegistry registry = new BuiltInShuttleModuleRuntimeRegistry();
            registry.Register(ScannerRuntimeSystem.Instance);
            registry.Register(ShuttleShieldRuntimeSystem.Instance);
            registry.Register(ShuttleSurfaceShieldRuntimeSystem.Instance);
            registry.Register(ShuttleWeaponRuntimeSystem.Instance);
            registry.Register(ShuttleHabitatRuntimeSystem.Instance);
            registry.Register(AutoWorkTableRuntimeSystem.Instance);
            registry.Register(RefrigeratedCargoRuntimeSystem.Instance);
            return registry;
        }

        public IReadOnlyList<IShuttleModuleRuntimeSystem> SystemsForRead
        {
            get
            {
                this.RefreshCompositeSystemsIfNeeded();
                return this.compositeSystems;
            }
        }

        internal int DispatchFingerprint
        {
            get
            {
                this.RefreshCompositeSystemsIfNeeded();
                if (this.dispatchFingerprintDirty)
                {
                    this.dispatchFingerprint = this.ComputeDispatchFingerprint();
                    this.dispatchFingerprintDirty = false;
                }

                return this.dispatchFingerprint;
            }
        }

        /// <summary>
        /// Adds one built-in runtime system. Duplicate keys are ignored because the key is part
        /// of persisted runtime-state identity.
        /// </summary>
        internal void Register(IShuttleModuleRuntimeSystem system)
        {
            if (system == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(system.RuntimeSystemKey))
            {
                Log.Warning("[CeleTech Shuttle] Built-in module runtime system with empty key ignored.");
                return;
            }

            for (int i = 0; i < this.builtInSystems.Count; i++)
            {
                IShuttleModuleRuntimeSystem existing = this.builtInSystems[i];
                if (existing != null && existing.RuntimeSystemKey == system.RuntimeSystemKey)
                {
                    Log.Warning("[CeleTech Shuttle] Duplicate built-in module runtime system key " +
                        system.RuntimeSystemKey + " ignored.");
                    return;
                }
            }

            this.builtInSystems.Add(system);
            this.compositeSystemsDirty = true;
            this.dispatchFingerprintDirty = true;
        }

        private void RefreshCompositeSystemsIfNeeded()
        {
            int currentExternalRevision = ExternalShuttleRuntimeRegistry.Revision;
            if (!this.compositeSystemsDirty &&
                this.externalRegistryRevision == currentExternalRevision)
            {
                return;
            }

            List<IShuttleModuleRuntimeSystem> systems =
                new List<IShuttleModuleRuntimeSystem>();
            HashSet<string> usedKeys = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < this.builtInSystems.Count; i++)
            {
                IShuttleModuleRuntimeSystem system = this.builtInSystems[i];
                if (system == null)
                {
                    continue;
                }

                systems.Add(system);
                string key = this.GetRuntimeSystemKeySafe(system);
                if (!string.IsNullOrEmpty(key))
                {
                    usedKeys.Add(key);
                }
            }

            List<ExternalRuntimeRegistration> externalRegistrations =
                ExternalShuttleRuntimeRegistry.GetRegistrationsSnapshot();
            for (int i = 0; externalRegistrations != null && i < externalRegistrations.Count; i++)
            {
                ExternalRuntimeRegistration registration = externalRegistrations[i];
                if (registration == null || string.IsNullOrEmpty(registration.FullRuntimeKey))
                {
                    continue;
                }

                if (usedKeys.Contains(registration.FullRuntimeKey))
                {
                    if (this.externalDuplicateKeyWarnings.Add(registration.FullRuntimeKey))
                    {
                        Log.Warning(ExternalShuttleRuntimeRegistry.LogPrefix +
                            "external key '" + registration.FullRuntimeKey +
                            "' conflicts with an existing module runtime key. External adapter skipped.");
                    }

                    continue;
                }

                usedKeys.Add(registration.FullRuntimeKey);
                systems.Add(new ExternalShuttleModuleRuntimeSystemAdapter(registration));
            }

            this.compositeSystems = systems;
            this.externalRegistryRevision = currentExternalRevision;
            this.compositeSystemsDirty = false;
            this.dispatchFingerprintDirty = true;
            ExternalRuntimeDiagnostics.LogCompositeSummaryIfDevMode(
                this.compositeSystems.Count,
                this.ComputeDispatchFingerprint());
        }

        private int ComputeDispatchFingerprint()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + this.compositeSystems.Count;
                for (int i = 0; i < this.compositeSystems.Count; i++)
                {
                    IShuttleModuleRuntimeSystem system = this.compositeSystems[i];
                    hash = hash * 31 + StableStringHash(this.GetRuntimeSystemKeySafe(system));
                    hash = hash * 31 + this.GetTickIntervalSafe(system);
                    hash = hash * 31 + (this.GetPowerDemandParticipationSafe(system) ? 1 : 0);
                }

                return hash;
            }
        }

        private string GetRuntimeSystemKeySafe(IShuttleModuleRuntimeSystem system)
        {
            if (system == null)
            {
                return null;
            }

            try
            {
                return system.RuntimeSystemKey;
            }
            catch (Exception exception)
            {
                Log.Warning("[CeleTech Shuttle] Module runtime system key access failed. Exception: " +
                    exception);
                return null;
            }
        }

        private int GetTickIntervalSafe(IShuttleModuleRuntimeSystem system)
        {
            if (system == null)
            {
                return 0;
            }

            try
            {
                return system.TickInterval;
            }
            catch (Exception exception)
            {
                Log.Warning(ExternalShuttleRuntimeRegistry.LogPrefix +
                    "TickInterval access failed while computing dispatch fingerprint. Exception: " +
                    exception);
                return 0;
            }
        }

        private bool GetPowerDemandParticipationSafe(IShuttleModuleRuntimeSystem system)
        {
            if (system == null)
            {
                return false;
            }

            try
            {
                return system.ParticipatesInPowerDemand;
            }
            catch (Exception exception)
            {
                Log.Warning(ExternalShuttleRuntimeRegistry.LogPrefix +
                    "Power-demand participation access failed while computing dispatch fingerprint. " +
                    "Power-demand dispatch disabled for this system. Exception: " + exception);
                return false;
            }
        }

        private static int StableStringHash(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return 0;
            }

            unchecked
            {
                int hash = 23;
                for (int i = 0; i < value.Length; i++)
                {
                    hash = hash * 31 + value[i];
                }

                return hash;
            }
        }
    }
}
