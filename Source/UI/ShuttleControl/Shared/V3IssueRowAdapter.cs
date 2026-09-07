using System.Collections.Generic;
using System.Text;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Chrome;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Issues;
using ChromeIssueSeverity = CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Chrome.ShuttleIssueSeverity;
using CommonIssueSeverity = CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Issues.ShuttleIssueSeverity;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal sealed class V3IssueRowAdapter
    {
        internal int AppendSharedRows(
            List<V3SharedMessageRow> rows,
            IReadOnlyList<ShuttleIssueReadModel> issues)
        {
            if (rows == null || issues == null)
            {
                return 0;
            }

            int added = 0;
            for (int i = 0; i < issues.Count; i++)
            {
                ShuttleIssueReadModel issue = issues[i];
                if (issue == null)
                {
                    continue;
                }

                rows.Add(this.ToSharedRow(issue));
                added++;
            }

            return added;
        }

        internal int AppendChromeRows(
            List<ShuttleIssueRowSpec> rows,
            IReadOnlyList<ShuttleIssueReadModel> issues)
        {
            if (rows == null || issues == null)
            {
                return 0;
            }

            int added = 0;
            for (int i = 0; i < issues.Count; i++)
            {
                ShuttleIssueReadModel issue = issues[i];
                if (issue == null)
                {
                    continue;
                }

                rows.Add(this.ToChromeRow(issue));
                added++;
            }

            return added;
        }

        private V3SharedMessageRow ToSharedRow(ShuttleIssueReadModel issue)
        {
            return new V3SharedMessageRow
            {
                Severity = this.ResolveSharedSeverity(issue.Severity),
                Category = this.ResolveSharedCategory(issue.Category),
                Message = this.ResolveMessage(issue),
                Tooltip = this.BuildTooltip(issue),
                ActionLabel = this.ResolveChromeActionLabel(issue),
                ActionTooltip = this.BuildActionTooltip(issue),
                ActionEnabled = issue.ActionKind != ShuttleIssueActionKind.None,
                ActionKind = issue.ActionKind,
                NavigationTarget = issue.NavigationTarget
            };
        }

        private ShuttleIssueRowSpec ToChromeRow(ShuttleIssueReadModel issue)
        {
            string tooltip = this.BuildTooltip(issue);
            return new ShuttleIssueRowSpec(
                this.ResolveChromeSeverity(issue.Severity),
                this.ResolveBadgeLabel(issue.Severity),
                this.ResolveChromeCategoryLabel(issue.Category),
                this.ResolveMessage(issue),
                tooltip,
                this.ResolveChromeActionLabel(issue),
                tooltip,
                false);
        }

        private string ResolveMessage(ShuttleIssueReadModel issue)
        {
            if (issue == null)
            {
                return ShuttleUILayout.ResolveSafeDisplayText(null);
            }

            if (!string.IsNullOrEmpty(issue.Title))
            {
                return issue.Title;
            }

            if (!string.IsNullOrEmpty(issue.Detail))
            {
                return issue.Detail;
            }

            return ShuttleUILayout.ResolveSafeDisplayText(issue.StableId);
        }

        private string BuildTooltip(ShuttleIssueReadModel issue)
        {
            if (issue == null)
            {
                return ShuttleUILayout.ResolveSafeDisplayText(null);
            }

            StringBuilder builder = new StringBuilder();
            this.AppendSection(builder, issue.Title);
            this.AppendSection(builder, issue.Detail);
            this.AppendEvidence(builder, issue.Evidence);

            return builder.Length > 0
                ? builder.ToString()
                : this.ResolveMessage(issue);
        }

        private string BuildActionTooltip(ShuttleIssueReadModel issue)
        {
            if (issue == null || string.IsNullOrEmpty(issue.ActionLabel))
            {
                return null;
            }

            StringBuilder builder = new StringBuilder();
            this.AppendSection(builder, issue.ActionLabel);
            this.AppendSection(builder, this.ResolveMessage(issue));
            return builder.ToString();
        }

        private void AppendEvidence(
            StringBuilder builder,
            IReadOnlyList<ShuttleIssueEvidenceLine> evidence)
        {
            if (builder == null || evidence == null || evidence.Count == 0)
            {
                return;
            }

            bool started = false;
            for (int i = 0; i < evidence.Count; i++)
            {
                ShuttleIssueEvidenceLine line = evidence[i];
                if (line == null)
                {
                    continue;
                }

                string text = this.BuildEvidenceLineText(line);
                if (string.IsNullOrEmpty(text))
                {
                    continue;
                }

                if (!started)
                {
                    this.AppendSectionSeparator(builder);
                    started = true;
                }

                builder.AppendLine(text);
            }
        }

        private string BuildEvidenceLineText(ShuttleIssueEvidenceLine line)
        {
            if (line == null)
            {
                return null;
            }

            bool hasLabel = !string.IsNullOrEmpty(line.Label);
            bool hasValue = !string.IsNullOrEmpty(line.Value);
            if (hasLabel && hasValue)
            {
                return line.Label + ": " + line.Value;
            }

            return hasLabel ? line.Label : line.Value;
        }

        private void AppendSection(StringBuilder builder, string text)
        {
            if (builder == null || string.IsNullOrEmpty(text))
            {
                return;
            }

            this.AppendSectionSeparator(builder);
            builder.Append(text);
        }

        private void AppendSectionSeparator(StringBuilder builder)
        {
            if (builder != null && builder.Length > 0)
            {
                builder.AppendLine();
                builder.AppendLine();
            }
        }

        private string ResolveSharedSeverity(CommonIssueSeverity severity)
        {
            if (severity == CommonIssueSeverity.Blocking ||
                severity == CommonIssueSeverity.Error)
            {
                return ShuttleUIText.SeverityError;
            }

            if (severity == CommonIssueSeverity.Warning)
            {
                return ShuttleUIText.SeverityWarning;
            }

            return CommonIssueSeverity.Info.ToString();
        }

        private ChromeIssueSeverity ResolveChromeSeverity(CommonIssueSeverity severity)
        {
            if (severity == CommonIssueSeverity.Blocking)
            {
                return ChromeIssueSeverity.Critical;
            }

            if (severity == CommonIssueSeverity.Error)
            {
                return ChromeIssueSeverity.Error;
            }

            if (severity == CommonIssueSeverity.Warning)
            {
                return ChromeIssueSeverity.Warning;
            }

            return ChromeIssueSeverity.Info;
        }

        private string ResolveBadgeLabel(CommonIssueSeverity severity)
        {
            if (severity == CommonIssueSeverity.Blocking ||
                severity == CommonIssueSeverity.Error)
            {
                return ShuttleUIText.Tr("CT_Shuttle_InfoPanel_Summary_ErrorShort");
            }

            if (severity == CommonIssueSeverity.Warning)
            {
                return ShuttleUIText.Tr("CT_Shuttle_InfoPanel_Summary_WarningShort");
            }

            return ShuttleUIText.Tr("CT_Shuttle_InfoPanel_Summary_InfoShort");
        }

        private string ResolveSharedCategory(ShuttleIssueCategory category)
        {
            if (category == ShuttleIssueCategory.Cargo ||
                category == ShuttleIssueCategory.Loading ||
                category == ShuttleIssueCategory.Refrigeration)
            {
                return "Cargo";
            }

            if (category == ShuttleIssueCategory.PrisonCell)
            {
                return "Prison";
            }

            if (category == ShuttleIssueCategory.General)
            {
                return "Runtime";
            }

            return category.ToString();
        }

        private string ResolveChromeCategoryLabel(ShuttleIssueCategory category)
        {
            string key = "CT_Shuttle_Issue_Category_" + category.ToString();
            string translated = ShuttleUIText.Tr(key);
            return !string.IsNullOrEmpty(translated) && translated != key
                ? translated
                : ShuttleControlDisplayNameResolver.ResolveCategoryLabel(category.ToString());
        }

        private string ResolveChromeActionLabel(ShuttleIssueReadModel issue)
        {
            if (issue == null ||
                issue.ActionKind == ShuttleIssueActionKind.None ||
                string.IsNullOrEmpty(issue.ActionLabel))
            {
                return null;
            }

            return issue.ActionLabel;
        }
    }
}
