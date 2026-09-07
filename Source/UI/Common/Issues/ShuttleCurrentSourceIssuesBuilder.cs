using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Issues
{
    internal sealed class ShuttleCurrentSourceIssuesBuilder
    {
        private readonly ShuttleReadModelIssueMessageFormatter issueMessageFormatter =
            new ShuttleReadModelIssueMessageFormatter();

        internal void AddIssues(
            ShuttleControlReadModel model,
            List<ShuttleIssueReadModel> issues)
        {
            if (model == null || model.Issues == null || issues == null)
            {
                return;
            }

            for (int i = 0; i < model.Issues.Count; i++)
            {
                ShuttleControlIssueModel source = model.Issues[i];
                if (source == null)
                {
                    continue;
                }

                string rawMessage = !string.IsNullOrEmpty(source.Message)
                    ? source.Message
                    : "-";
                string title = this.issueMessageFormatter.FormatIssueMessage(
                    model,
                    source,
                    rawMessage);
                ShuttleIssueCategory category =
                    ShuttleIssueClassifier.ResolveCategory(source.Scope, source.Code);
                if (this.IsLaunchOnlyIssue(source, category))
                {
                    continue;
                }

                ShuttleIssueActionKind actionKind =
                    ShuttleIssueActionResolver.ResolveCurrentActionKind(
                        category,
                        source.Code,
                        source.Scope);
                ShuttleIssueReadModelSet.AddIssueIfUnique(issues, new ShuttleIssueReadModel
                {
                    StableId = this.BuildStableModelIssueId(source),
                    Severity = ShuttleIssueClassifier.ResolveSeverity(source.Severity),
                    Category = category,
                    Title = title,
                    Detail = ShuttleIssuePlayerTextHelper.BuildDetail(
                        model,
                        source.Code,
                        category,
                        title,
                        rawMessage),
                    Evidence = ShuttleIssuePlayerTextHelper.BuildEvidence(
                        model,
                        source.Code,
                        category,
                        source.ReferenceID),
                    ActionKind = actionKind,
                    ActionLabel = ShuttleIssueActionResolver.ResolveActionLabel(actionKind),
                    NavigationTarget = ShuttleIssueNavigationTargetFactory.BuildNavigationTarget(
                        model,
                        category,
                        source.Code,
                        source.ReferenceID),
                    SortPriority = 100
                });
            }
        }

        private bool IsLaunchOnlyIssue(
            ShuttleControlIssueModel source,
            ShuttleIssueCategory category)
        {
            if (source == null)
            {
                return false;
            }

            string code = !string.IsNullOrEmpty(source.Code)
                ? source.Code.ToLowerInvariant()
                : string.Empty;
            if (category == ShuttleIssueCategory.Launch ||
                code == "cockpit-colonist-required")
            {
                return true;
            }

            return code.Contains("launch");
        }

        private string BuildStableModelIssueId(ShuttleControlIssueModel issue)
        {
            if (issue == null)
            {
                return "model:unknown";
            }

            string code = !string.IsNullOrEmpty(issue.Code) ? issue.Code : "issue";
            string scope = !string.IsNullOrEmpty(issue.Scope) ? issue.Scope : "scope";
            string reference = !string.IsNullOrEmpty(issue.ReferenceID) ? issue.ReferenceID : "no-ref";
            return "model:" + code + ":" + scope + ":" + reference;
        }
    }
}
