using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.API.UI;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.External
{
    internal sealed class ExternalModulePanelCommandSink :
        IShuttleExternalModulePanelCommandSink
    {
        private const string LogPrefix = "[CeleTech ShuttleExtension] UI ";

        private static readonly HashSet<string> warningKeys =
            new HashSet<string>(System.StringComparer.Ordinal);

        private readonly string ownerPackageId;
        private readonly string panelKey;
        private readonly string runtimeSystemKey;
        private readonly List<ShuttleExternalPanelCommandContribution> contributions =
            new List<ShuttleExternalPanelCommandContribution>();
        private readonly HashSet<string> localKeys =
            new HashSet<string>(System.StringComparer.Ordinal);

        internal ExternalModulePanelCommandSink(
            string ownerPackageId,
            string panelKey,
            string runtimeSystemKey)
        {
            this.ownerPackageId = NormalizeKey(ownerPackageId);
            this.panelKey = NormalizeKey(panelKey);
            this.runtimeSystemKey = ShuttleRuntimeSystemKeyUtility.Normalize(runtimeSystemKey);
        }

        public void AddCommand(ShuttleExternalPanelCommandContribution contribution)
        {
            try
            {
                this.AddCommandInternal(contribution);
            }
            catch (System.Exception exception)
            {
                LogWarning("ignored command contribution due to unexpected exception. Exception: " + exception);
            }
        }

        internal List<ShuttleExternalPanelCommandContribution> GetContributionsSnapshot()
        {
            List<ShuttleExternalPanelCommandContribution> snapshot =
                new List<ShuttleExternalPanelCommandContribution>(this.contributions);
            snapshot.Sort(CompareContributions);
            return snapshot;
        }

        private void AddCommandInternal(ShuttleExternalPanelCommandContribution contribution)
        {
            if (contribution == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(contribution.LocalKey))
            {
                LogWarningOnce(
                    "empty-local-key",
                    null,
                    contribution.CommandKey,
                    "ignored command contribution with empty localKey.");
                return;
            }

            if (string.IsNullOrWhiteSpace(contribution.CommandKey))
            {
                LogWarningOnce(
                    "empty-command-key",
                    contribution.LocalKey,
                    null,
                    "ignored command contribution '" + contribution.LocalKey +
                    "' with empty commandKey.");
                return;
            }

            if (string.IsNullOrEmpty(this.ownerPackageId) ||
                !contribution.CommandKey.StartsWith(
                    this.ownerPackageId + "/",
                    System.StringComparison.Ordinal))
            {
                LogWarningOnce(
                    "cross-owner-command-key",
                    contribution.LocalKey,
                    contribution.CommandKey,
                    "ignored command contribution '" + contribution.LocalKey +
                    "' because commandKey must be owned by " + this.ownerPackageId +
                    ". CommandKey=" + contribution.CommandKey + ".");
                return;
            }

            string localKey = contribution.LocalKey.Trim();
            if (!this.localKeys.Add(localKey))
            {
                LogWarningOnce(
                    "duplicate-local-key",
                    localKey,
                    contribution.CommandKey,
                    "ignored duplicate command contribution localKey '" +
                    localKey + "'. First contribution wins.");
                return;
            }

            this.contributions.Add(contribution);
        }

        private void LogWarningOnce(
            string reason,
            string localKey,
            string commandKey,
            string message)
        {
            string warningKey =
                (this.panelKey ?? "<null>") + "|" +
                (localKey ?? "<null>") + "|" +
                (commandKey ?? "<null>") + "|" +
                (reason ?? "<null>");
            if (warningKeys.Add(warningKey) || Prefs.DevMode)
            {
                this.LogWarning(message);
            }
        }

        private void LogWarning(string message)
        {
            Log.Warning(LogPrefix + "panel command sink " + message +
                " PanelKey=" + (this.panelKey ?? "<null>") +
                ", RuntimeSystemKey=" + (this.runtimeSystemKey ?? "<null>") + ".");
        }

        private static int CompareContributions(
            ShuttleExternalPanelCommandContribution left,
            ShuttleExternalPanelCommandContribution right)
        {
            int orderCompare = (left != null ? left.Order : 0).CompareTo(right != null ? right.Order : 0);
            if (orderCompare != 0)
            {
                return orderCompare;
            }

            return System.StringComparer.Ordinal.Compare(
                left != null ? left.LocalKey : string.Empty,
                right != null ? right.LocalKey : string.Empty);
        }

        private static string NormalizeKey(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
