using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated
{
    internal sealed class ShuttleRefrigeratedCargoAutoTransferConfig
    {
        internal ShuttleRefrigeratedCargoAutoTransferConfig(
            string moduleInstanceID,
            bool autoTransferEnabled,
            bool hasCustomAutoTransferFilter,
            ThingFilter autoTransferFilter)
        {
            this.ModuleInstanceID = moduleInstanceID;
            this.AutoTransferEnabled = autoTransferEnabled;
            this.HasCustomAutoTransferFilter = hasCustomAutoTransferFilter;
            this.autoTransferFilterSource = autoTransferFilter;
            this.autoTransferFilterResolved = autoTransferFilter == null;
        }

        internal string ModuleInstanceID { get; private set; }
        internal bool AutoTransferEnabled { get; private set; }
        internal bool HasCustomAutoTransferFilter { get; private set; }
        internal ThingFilter AutoTransferFilter
        {
            get
            {
                if (!this.autoTransferFilterResolved)
                {
                    this.autoTransferFilter = CopyFilterOrNull(this.autoTransferFilterSource);
                    this.autoTransferFilterSource = null;
                    this.autoTransferFilterResolved = true;
                }

                return this.autoTransferFilter;
            }
        }

        private ThingFilter autoTransferFilter;
        private ThingFilter autoTransferFilterSource;
        private bool autoTransferFilterResolved;

        internal static ShuttleRefrigeratedCargoAutoTransferConfig FromModuleDef(
            string moduleInstanceID,
            ShuttleRefrigeratedCargoModuleDef moduleDef)
        {
            bool hasDefFilter = moduleDef != null && moduleDef.autoTransferFilter != null;
            return new ShuttleRefrigeratedCargoAutoTransferConfig(
                moduleInstanceID,
                moduleDef != null && moduleDef.autoTransferEnabledByDefault,
                !hasDefFilter,
                hasDefFilter
                    ? moduleDef.autoTransferFilter
                    : CreateDisallowAllFilter());
        }

        internal static ThingFilter CreateDisallowAllFilter()
        {
            ThingFilter filter = ThingFilter.CreateOnlyEverStorableThingFilter();
            filter.SetDisallowAll(null, null);
            return filter;
        }

        internal static ThingFilter CopyFilterOrNull(ThingFilter source)
        {
            if (source == null)
            {
                return null;
            }

            ThingFilter copy = ThingFilter.CreateOnlyEverStorableThingFilter();
            copy.CopyAllowancesFrom(source);
            return copy;
        }
    }
}
