using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;

namespace CeleTech.ShuttleExtension.ModularShuttle.Profile.ExternalExpressions
{
    internal sealed class ShuttleExternalProfileExpressionSource
    {
        internal ShuttleExternalProfileExpressionSource(
            string contributorKey,
            string moduleDefName,
            string moduleLabel,
            string moduleInstanceId,
            string referenceId,
            bool builtIn)
        {
            this.ContributorKey = contributorKey;
            this.ModuleDefName = moduleDefName;
            this.ModuleLabel = moduleLabel;
            this.ModuleInstanceId = moduleInstanceId;
            this.ReferenceId = referenceId;
            this.BuiltIn = builtIn;
        }

        internal string ContributorKey { get; private set; }
        internal string ModuleDefName { get; private set; }
        internal string ModuleLabel { get; private set; }
        internal string ModuleInstanceId { get; private set; }
        internal string ReferenceId { get; private set; }
        internal bool BuiltIn { get; private set; }

        internal string CountKey
        {
            get
            {
                return (this.ContributorKey ?? string.Empty) + "|" +
                    (this.ModuleInstanceId ?? this.ReferenceId ?? string.Empty);
            }
        }

        internal static ShuttleExternalProfileExpressionSource From(
            string contributorKey,
            bool builtIn,
            ShuttleModule module)
        {
            return new ShuttleExternalProfileExpressionSource(
                contributorKey,
                module != null ? module.moduleDefName : null,
                ResolveModuleLabel(module),
                module != null ? module.ModuleInstanceID : null,
                module != null ? module.ModuleInstanceID : null,
                builtIn);
        }

        internal ShuttleExternalProfileContributionSourceSnapshot ToSnapshot()
        {
            return new ShuttleExternalProfileContributionSourceSnapshot(
                this.ContributorKey,
                this.ModuleDefName,
                this.ModuleLabel,
                this.ModuleInstanceId,
                this.ReferenceId,
                this.BuiltIn);
        }

        private static string ResolveModuleLabel(ShuttleModule module)
        {
            if (module == null)
            {
                return null;
            }

            try
            {
                return module.ModuleDef != null
                    ? module.ModuleDef.LabelCap.ToString()
                    : module.moduleDefName;
            }
            catch
            {
                return module.moduleDefName;
            }
        }
    }
}
