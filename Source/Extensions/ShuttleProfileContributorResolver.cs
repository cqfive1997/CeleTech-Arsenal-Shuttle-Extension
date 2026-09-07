using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Profile;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Contributors;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Extensions;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Extensions
{
    /// <summary>
    /// Internal profile contributor resolution for core and third-party contributors.
    /// Third-party mods register code contributors through ShuttleProfileAPI; module XML may
    /// reference those keys through ShuttleProfileContributorDefExtension.
    /// Contributors must remain side-effect free.
    /// </summary>
    internal static class ShuttleProfileContributorResolver
    {
        internal const string LogPrefix = "[CeleTech ShuttleExtension] Profile contributor ";

        private static readonly ShuttleProfileContributorRegistry registry = new ShuttleProfileContributorRegistry();
        private static readonly HashSet<string> nonNamespacedKeyWarnings = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>
        /// Registers a contributor under a stable key. Registration is explicit so profile
        /// builds never scan assemblies or instantiate contributor classes from XML.
        /// </summary>
        internal static bool RegisterExternalContributor(string key, IShuttleProfileContributor contributor)
        {
            string normalizedKey = ShuttleProfileContributorRegistry.NormalizeKey(key);
            if (normalizedKey == null)
            {
                Log.Warning(LogPrefix + "registration rejected: key is empty.");
                return false;
            }

            if (contributor == null)
            {
                Log.Warning(LogPrefix + "registration rejected for key '" + normalizedKey +
                    "': contributor is null.");
                return false;
            }

            WarnIfKeyIsNotNamespaced(normalizedKey);

            if (!registry.Register(normalizedKey, contributor))
            {
                Log.Warning(LogPrefix + "registration rejected for duplicate key '" +
                    normalizedKey + "'. First registration wins.");
                return false;
            }

            return true;
        }

        internal static List<ShuttleProfileContributorResolution> ResolveContributorsOrFallback(
            ShuttleModuleBaseDef moduleDef,
            IShuttleProfileIssueSink issues,
            string referenceID)
        {
            List<ShuttleProfileContributorResolution> result =
                new List<ShuttleProfileContributorResolution>();

            if (moduleDef == null)
            {
                result.Add(ShuttleProfileContributorResolution.NoOp("none"));
                return result;
            }

            // Explicit XML key wins when registered. If it is missing, fall back to built-in
            // module type behavior so existing defs and optional/default semantics keep working.
            ShuttleProfileContributorDefExtension extension =
                moduleDef.GetModExtension<ShuttleProfileContributorDefExtension>();
            List<string> contributorKeys = extension != null
                ? extension.GetContributorKeys()
                : new List<string>();

            for (int i = 0; i < contributorKeys.Count; i++)
            {
                string contributorKey = contributorKeys[i];
                WarnIfKeyIsNotNamespaced(contributorKey);

                IShuttleProfileContributor registeredContributor;
                if (registry.TryResolve(contributorKey, out registeredContributor))
                {
                    result.Add(new ShuttleProfileContributorResolution(
                        contributorKey,
                        registeredContributor,
                        true));
                    continue;
                }

                Log.Warning(LogPrefix + "key '" + contributorKey + "' requested by module def '" +
                    moduleDef.defName + "' is not registered. Profile build will continue.");

                AddIssue(
                    issues,
                    "profile-contributor-key-missing",
                    "Module def " + moduleDef.defName +
                    " requests profile contributor key '" + contributorKey +
                    "', but no contributor is registered. Profile build will continue.",
                    ProfileBuildIssueSeverity.Warning,
                    ProfileBuildIssueScope.Profile,
                    referenceID);
            }

            if (result.Count > 0)
            {
                return result;
            }

            IShuttleProfileContributor builtInContributor =
                BuiltInShuttleProfileContributorResolver.ResolveOrNull(moduleDef);
            if (builtInContributor != null)
            {
                result.Add(new ShuttleProfileContributorResolution(
                    "built-in/" + builtInContributor.GetType().Name,
                    builtInContributor,
                    false));
                return result;
            }

            // Unknown module types already have their own profile issue in the builder. Avoid
            // adding a second warning just because no built-in contributor exists.
            if (contributorKeys.Count == 0 && moduleDef.ModuleType != ShuttleModuleType.Unknown)
            {
                AddIssue(
                    issues,
                    "module-handler-missing",
                    "CT_Shuttle_Issue_ModuleHandlerMissing".Translate(
                        referenceID,
                        ShuttleModuleTypeCatalog.ToDisplayString(moduleDef.ModuleType)).ToString(),
                    ProfileBuildIssueSeverity.Warning,
                    ProfileBuildIssueScope.Profile,
                    referenceID);
            }

            result.Add(ShuttleProfileContributorResolution.NoOp("none"));
            return result;
        }

        internal static List<string> GetRegisteredContributorKeysSnapshot()
        {
            return registry.GetRegisteredKeysSnapshot();
        }

        internal static void LogContributorException(
            string contributorKey,
            string moduleDefName,
            Exception exception)
        {
            Log.Error(LogPrefix + "key '" + contributorKey + "' failed for module def '" +
                moduleDefName + "': " + exception);
        }

        private static void WarnIfKeyIsNotNamespaced(string key)
        {
            if (string.IsNullOrEmpty(key) || key.IndexOf('/') >= 0)
            {
                return;
            }

            if (nonNamespacedKeyWarnings.Add(key))
            {
                Log.Warning(LogPrefix + "key '" + key +
                    "' is not namespaced. Use package.id/local-key for third-party contributors.");
            }
        }

        private static void AddIssue(
            IShuttleProfileIssueSink issues,
            string code,
            string message,
            ProfileBuildIssueSeverity severity,
            ProfileBuildIssueScope scope,
            string referenceID)
        {
            if (issues == null)
            {
                return;
            }

            issues.AddIssue(code, message, severity, scope, referenceID);
        }
    }

    internal sealed class ShuttleProfileContributorResolution
    {
        internal ShuttleProfileContributorResolution(
            string contributorKey,
            IShuttleProfileContributor contributor,
            bool isExplicitKey)
        {
            this.ContributorKey = contributorKey;
            this.Contributor = contributor ?? NullShuttleProfileContributor.Instance;
            this.IsExplicitKey = isExplicitKey;
        }

        internal string ContributorKey { get; private set; }

        internal IShuttleProfileContributor Contributor { get; private set; }

        internal bool IsExplicitKey { get; private set; }

        internal static ShuttleProfileContributorResolution NoOp(string key)
        {
            return new ShuttleProfileContributorResolution(
                key,
                NullShuttleProfileContributor.Instance,
                false);
        }
    }
}
