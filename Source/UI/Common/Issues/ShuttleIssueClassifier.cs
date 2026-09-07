namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Issues
{
    internal static class ShuttleIssueClassifier
    {
        internal static ShuttleIssueSeverity ResolveSeverity(string severity)
        {
            string lower = !string.IsNullOrEmpty(severity)
                ? severity.ToLowerInvariant()
                : string.Empty;
            if (lower.Contains("block") || lower.Contains("fatal"))
            {
                return ShuttleIssueSeverity.Blocking;
            }

            if (lower.Contains("error"))
            {
                return ShuttleIssueSeverity.Error;
            }

            if (lower.Contains("warn"))
            {
                return ShuttleIssueSeverity.Warning;
            }

            return ShuttleIssueSeverity.Info;
        }

        internal static ShuttleIssueCategory ResolveCategory(string scope, string code)
        {
            string text = ((scope ?? string.Empty) + " " + (code ?? string.Empty)).ToLowerInvariant();
            if (text.Contains("cargo") || text.Contains("transporter"))
            {
                return ShuttleIssueCategory.Cargo;
            }

            if (text.Contains("load"))
            {
                return ShuttleIssueCategory.Loading;
            }

            if (text.Contains("cold") || text.Contains("refriger"))
            {
                return ShuttleIssueCategory.Refrigeration;
            }

            if (text.Contains("cockpit") || text.Contains("crew"))
            {
                return ShuttleIssueCategory.Crew;
            }

            if (text.Contains("launch"))
            {
                return ShuttleIssueCategory.Launch;
            }

            if (text.Contains("assembly") ||
                text.Contains("segment") ||
                text.Contains("module") ||
                text.Contains("slot"))
            {
                return ShuttleIssueCategory.Assembly;
            }

            if (text.Contains("power") || text.Contains("energy") || text.Contains("battery"))
            {
                return ShuttleIssueCategory.Power;
            }

            if (text.Contains("medical") || text.Contains("patient") || text.Contains("medicine"))
            {
                return ShuttleIssueCategory.Medical;
            }

            if (text.Contains("prison"))
            {
                return ShuttleIssueCategory.PrisonCell;
            }

            if (IsDefenseIssueText(text))
            {
                return ShuttleIssueCategory.Defense;
            }

            return ShuttleIssueCategory.General;
        }

        internal static bool IsDefenseIssueText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            return text.Contains("defense") ||
                text.Contains("weapon") ||
                text.Contains("weaponbay") ||
                text.Contains("weapon-bay") ||
                text.Contains("firecontrol") ||
                text.Contains("fire-control") ||
                text.Contains("ammoloader") ||
                text.Contains("ammo-loader") ||
                text.Contains("shield");
        }

        internal static bool IsAssemblyInstallIssueText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            return text.Contains("required") ||
                text.Contains("missing") ||
                text.Contains("empty") ||
                text.Contains("slot");
        }
    }
}
