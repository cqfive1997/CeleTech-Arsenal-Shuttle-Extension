using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Diagnostics;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Issues;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal sealed class V3SharedMessageRowsBuilder
    {
        private readonly V3IssueRowAdapter issueRowAdapter = new V3IssueRowAdapter();

        internal List<V3SharedMessageRow> BuildCurrentIssueRows(
            ShuttlePageDrawContext context)
        {
            using (ShuttleUIProfiler.Scope(ShuttleUIProfileSection.SharedMessagePanelRows))
            {
                List<V3SharedMessageRow> rows = new List<V3SharedMessageRow>();
                ShuttlePageReadModels readModels = context != null ? context.ReadModels : null;
                ShuttleControlReadModel controlModel =
                    readModels != null ? readModels.ControlModel : null;
                IReadOnlyList<ShuttleIssueReadModel> currentIssues =
                    readModels != null ? readModels.CurrentIssues : null;
                if (currentIssues != null)
                {
                    this.issueRowAdapter.AppendSharedRows(rows, currentIssues);
                }
                else
                {
                    this.AppendIssueRows(rows, controlModel);
                }

                if (rows.Count == 0)
                {
                    rows.Add(this.CreateNormalRow("CT_Shuttle_InfoPanel_CurrentIssuesReady_Body"));
                }

                return rows;
            }
        }

        internal List<V3SharedMessageRow> BuildLaunchRows(
            ShuttlePageDrawContext context)
        {
            using (ShuttleUIProfiler.Scope(ShuttleUIProfileSection.SharedMessagePanelRows))
            {
                List<V3SharedMessageRow> rows = new List<V3SharedMessageRow>();
                ShuttlePageReadModels readModels = context != null ? context.ReadModels : null;
                IReadOnlyList<ShuttleIssueReadModel> launchDiagnostics =
                    readModels != null ? readModels.LaunchDiagnostics : null;
                if (launchDiagnostics != null)
                {
                    this.issueRowAdapter.AppendSharedRows(rows, launchDiagnostics);
                    SuppressRedundantLaunchActions(rows);
                }
                else
                {
                    ShuttleControlReadModel controlModel =
                        readModels != null ? readModels.ControlModel : null;
                    this.AppendLaunchChecklistRows(rows, controlModel);
                }

                if (rows.Count == 0)
                {
                    rows.Add(this.CreateNormalRow("CT_Shuttle_InfoPanel_LaunchChecklistPassed"));
                }

                return rows;
            }
        }

        private static void SuppressRedundantLaunchActions(
            List<V3SharedMessageRow> rows)
        {
            if (rows == null)
            {
                return;
            }

            for (int i = 0; i < rows.Count; i++)
            {
                V3SharedMessageRow row = rows[i];
                if (row == null || row.ActionKind != ShuttleIssueActionKind.OpenLaunch)
                {
                    continue;
                }

                // The player is already looking at launch diagnostics. Reopening the
                // same tab adds no navigation value and wastes the row's text space.
                row.ActionLabel = null;
                row.ActionTooltip = null;
                row.ActionEnabled = false;
                row.ActionKind = ShuttleIssueActionKind.None;
                row.NavigationTarget = null;
            }
        }

        private void AppendLaunchChecklistRows(
            List<V3SharedMessageRow> rows,
            ShuttleControlReadModel controlModel)
        {
            if (rows == null || controlModel == null || controlModel.LaunchChecklist == null)
            {
                return;
            }

            for (int i = 0; i < controlModel.LaunchChecklist.Count; i++)
            {
                ShuttleLaunchChecklistItemModel item = controlModel.LaunchChecklist[i];
                if (ShouldShowChecklistItem(item))
                {
                    rows.Add(new V3SharedMessageRow
                    {
                        Severity = item.Severity,
                        Category = "Launch",
                        Message = string.IsNullOrEmpty(item.Message) ? "-" : item.Message,
                        Tooltip = item.Tooltip
                    });
                }
            }
        }

        internal List<V3SharedMessageRow> BuildDeveloperRows(
            ShuttlePageDrawContext context)
        {
            using (ShuttleUIProfiler.Scope(ShuttleUIProfileSection.SharedMessagePanelRows))
            {
                List<V3SharedMessageRow> rows = new List<V3SharedMessageRow>();
                ShuttleControlReadModel controlModel =
                    context != null && context.ReadModels != null
                        ? context.ReadModels.ControlModel
                        : null;
                if (controlModel != null && controlModel.DeveloperDiagnostics != null)
                {
                    for (int i = 0; i < controlModel.DeveloperDiagnostics.Count; i++)
                    {
                        ShuttleDeveloperDiagnosticModel diagnostic =
                            controlModel.DeveloperDiagnostics[i];
                        if (diagnostic == null)
                        {
                            continue;
                        }

                        rows.Add(new V3SharedMessageRow
                        {
                            Severity = diagnostic.Severity,
                            Category = diagnostic.Kind,
                            Message = string.IsNullOrEmpty(diagnostic.Message) ? "-" : diagnostic.Message,
                            Tooltip = diagnostic.Tooltip
                        });
                    }
                }

                if (rows.Count == 0)
                {
                    rows.Add(this.CreateNormalRow("CT_Shuttle_InfoPanel_DeveloperDiagnosticsEmpty"));
                }

                return rows;
            }
        }

        private void AppendIssueRows(
            List<V3SharedMessageRow> rows,
            ShuttleControlReadModel controlModel)
        {
            if (controlModel == null || controlModel.Issues == null)
            {
                return;
            }

            for (int i = 0; i < controlModel.Issues.Count; i++)
            {
                ShuttleControlIssueModel issue = controlModel.Issues[i];
                if (issue == null)
                {
                    continue;
                }

                rows.Add(new V3SharedMessageRow
                {
                    Severity = issue.Severity,
                    Category = issue.Scope,
                    Message = string.IsNullOrEmpty(issue.Message) ? "-" : issue.Message
                });
            }
        }

        private V3SharedMessageRow CreateNormalRow(string messageKey)
        {
            return new V3SharedMessageRow
            {
                Severity = "OK",
                Category = "Runtime",
                Message = ShuttleUIText.Tr(messageKey),
                IsNormal = true
            };
        }

        private static bool ShouldShowChecklistItem(ShuttleLaunchChecklistItemModel item)
        {
            return item != null &&
                (item.BlocksLaunch ||
                    item.IsRiskOnly ||
                    V3SharedMessageRowDrawer.IsWarningOrWorse(item.Severity));
        }
    }
}
