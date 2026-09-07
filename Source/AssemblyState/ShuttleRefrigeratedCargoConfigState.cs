using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.AssemblyState
{
    /// <summary>
    /// Durable per-module configuration for refrigerated cargo display and automatic transfer.
    /// This is static assembly configuration, not runtime state and not cargo ownership truth.
    /// </summary>
    public sealed class ShuttleRefrigeratedCargoConfigState : IExposable
    {
        private List<ShuttleRefrigeratedCargoModuleConfig> modules =
            new List<ShuttleRefrigeratedCargoModuleConfig>();

        public IReadOnlyList<ShuttleRefrigeratedCargoModuleConfig> Modules
        {
            get
            {
                this.EnsureInitialized();
                return this.modules;
            }
        }

        public void EnsureInitialized()
        {
            if (this.modules == null)
            {
                this.modules = new List<ShuttleRefrigeratedCargoModuleConfig>();
            }

            for (int i = this.modules.Count - 1; i >= 0; i--)
            {
                ShuttleRefrigeratedCargoModuleConfig config = this.modules[i];
                if (config == null || string.IsNullOrEmpty(config.ModuleInstanceID))
                {
                    this.modules.RemoveAt(i);
                    continue;
                }

                config.EnsureInitialized();
            }
        }

        public bool TryGetModuleConfig(
            string moduleInstanceID,
            out ShuttleRefrigeratedCargoModuleConfig config)
        {
            config = null;
            if (string.IsNullOrEmpty(moduleInstanceID))
            {
                return false;
            }

            this.EnsureInitialized();
            for (int i = 0; i < this.modules.Count; i++)
            {
                ShuttleRefrigeratedCargoModuleConfig candidate = this.modules[i];
                if (candidate != null && candidate.ModuleInstanceID == moduleInstanceID)
                {
                    config = candidate;
                    return true;
                }
            }

            return false;
        }

        public ShuttleRefrigeratedCargoModuleConfig GetOrCreateModuleConfig(
            string moduleInstanceID,
            ShuttleRefrigeratedCargoModuleDef moduleDef)
        {
            if (string.IsNullOrEmpty(moduleInstanceID))
            {
                return null;
            }

            this.EnsureInitialized();
            ShuttleRefrigeratedCargoModuleConfig config;
            if (this.TryGetModuleConfig(moduleInstanceID, out config))
            {
                return config;
            }

            config = new ShuttleRefrigeratedCargoModuleConfig(
                moduleInstanceID,
                moduleDef);
            config.EnsureInitialized();
            this.modules.Add(config);
            return config;
        }

        public bool RemoveModuleConfig(string moduleInstanceID)
        {
            if (string.IsNullOrEmpty(moduleInstanceID) || this.modules == null)
            {
                return false;
            }

            for (int i = this.modules.Count - 1; i >= 0; i--)
            {
                ShuttleRefrigeratedCargoModuleConfig config = this.modules[i];
                if (config != null && config.ModuleInstanceID == moduleInstanceID)
                {
                    this.modules.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        internal ShuttleRefrigeratedCargoAutoTransferConfig BuildEffectiveAutoTransferConfig(
            string moduleInstanceID,
            ShuttleRefrigeratedCargoModuleDef moduleDef)
        {
            ShuttleRefrigeratedCargoModuleConfig config;
            if (this.TryGetModuleConfig(moduleInstanceID, out config) && config != null)
            {
                return config.BuildEffectiveAutoTransferConfig(moduleDef);
            }

            return ShuttleRefrigeratedCargoAutoTransferConfig.FromModuleDef(
                moduleInstanceID,
                moduleDef);
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(ref this.modules, "modules", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.EnsureInitialized();
            }
        }
    }

    public sealed class ShuttleRefrigeratedCargoModuleConfig : IExposable
    {
        private string moduleInstanceID;
        private string label;
        private bool autoTransferEnabled;
        private bool hasCustomAutoTransferFilter;
        private ThingFilter autoTransferFilter;

        public ShuttleRefrigeratedCargoModuleConfig()
        {
        }

        internal ShuttleRefrigeratedCargoModuleConfig(
            string moduleInstanceID,
            ShuttleRefrigeratedCargoModuleDef moduleDef)
        {
            this.moduleInstanceID = moduleInstanceID;
            this.autoTransferEnabled =
                moduleDef != null && moduleDef.autoTransferEnabledByDefault;
            if (moduleDef == null || moduleDef.autoTransferFilter == null)
            {
                this.hasCustomAutoTransferFilter = true;
                this.autoTransferFilter =
                    ShuttleRefrigeratedCargoAutoTransferConfig.CreateDisallowAllFilter();
            }
        }

        public string ModuleInstanceID
        {
            get
            {
                return this.moduleInstanceID;
            }
        }

        public string Label
        {
            get
            {
                return this.label;
            }
        }

        public bool AutoTransferEnabled
        {
            get
            {
                return this.autoTransferEnabled;
            }
        }

        public bool HasCustomAutoTransferFilter
        {
            get
            {
                return this.hasCustomAutoTransferFilter;
            }
        }

        public ThingFilter AutoTransferFilterForRead
        {
            get
            {
                return this.autoTransferFilter;
            }
        }

        public void EnsureInitialized()
        {
            if (string.IsNullOrEmpty(this.label))
            {
                this.label = null;
            }

            if (this.hasCustomAutoTransferFilter && this.autoTransferFilter == null)
            {
                this.autoTransferFilter = ThingFilter.CreateOnlyEverStorableThingFilter();
            }
        }

        public void SetLabel(string value)
        {
            string trimmed = value != null ? value.Trim() : null;
            this.label = string.IsNullOrEmpty(trimmed) ? null : trimmed;
        }

        public void SetAutoTransferEnabled(bool enabled)
        {
            this.autoTransferEnabled = enabled;
        }

        public void SetCustomAutoTransferFilter(ThingFilter filter)
        {
            this.hasCustomAutoTransferFilter = true;
            this.autoTransferFilter =
                ShuttleRefrigeratedCargoAutoTransferConfig.CopyFilterOrNull(filter) ??
                ThingFilter.CreateOnlyEverStorableThingFilter();
        }

        public void ClearCustomAutoTransferFilter()
        {
            this.hasCustomAutoTransferFilter = false;
            this.autoTransferFilter = null;
        }

        internal ShuttleRefrigeratedCargoAutoTransferConfig BuildEffectiveAutoTransferConfig(
            ShuttleRefrigeratedCargoModuleDef moduleDef)
        {
            ThingFilter filter = this.hasCustomAutoTransferFilter
                ? this.autoTransferFilter
                : moduleDef != null
                    ? moduleDef.autoTransferFilter
                    : null;

            return new ShuttleRefrigeratedCargoAutoTransferConfig(
                this.moduleInstanceID,
                this.autoTransferEnabled,
                this.hasCustomAutoTransferFilter,
                filter);
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.moduleInstanceID, "moduleInstanceID");
            Scribe_Values.Look(ref this.label, "label");
            Scribe_Values.Look(ref this.autoTransferEnabled, "autoTransferEnabled", false);
            Scribe_Values.Look(ref this.hasCustomAutoTransferFilter, "hasCustomAutoTransferFilter", false);
            Scribe_Deep.Look(ref this.autoTransferFilter, "autoTransferFilter");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.EnsureInitialized();
            }
        }
    }
}
