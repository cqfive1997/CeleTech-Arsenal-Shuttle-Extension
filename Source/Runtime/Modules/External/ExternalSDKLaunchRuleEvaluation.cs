using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalSDKLaunchRuleEvaluation
    {
        internal ExternalSDKLaunchRuleEvaluation(
            List<ShuttleExternalLaunchIssueSnapshot> issues,
            ShuttleExternalLaunchRuleDiagnosticsSnapshot diagnostics)
        {
            this.Issues = issues ?? new List<ShuttleExternalLaunchIssueSnapshot>();
            this.Diagnostics =
                diagnostics ?? ShuttleExternalLaunchRuleDiagnosticsSnapshot.Empty();
        }

        internal List<ShuttleExternalLaunchIssueSnapshot> Issues { get; private set; }
        internal ShuttleExternalLaunchRuleDiagnosticsSnapshot Diagnostics
        {
            get;
            private set;
        }

        internal static ExternalSDKLaunchRuleEvaluation Empty()
        {
            return new ExternalSDKLaunchRuleEvaluation(
                new List<ShuttleExternalLaunchIssueSnapshot>(),
                ShuttleExternalLaunchRuleDiagnosticsSnapshot.Empty());
        }
    }
}
