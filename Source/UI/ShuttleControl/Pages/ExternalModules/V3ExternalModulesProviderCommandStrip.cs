using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.API.UI;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation.External;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.ExternalModules;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.ExternalModules
{
    internal sealed class V3ExternalModulesProviderCommandStrip
    {
        private readonly V3ExternalModulesText text;
        private readonly V3ExternalModulesPanelDrawer panel;

        internal V3ExternalModulesProviderCommandStrip(
            V3ExternalModulesText text,
            V3ExternalModulesPanelDrawer panel)
        {
            this.text = text;
            this.panel = panel;
        }

        internal float GetHeight(
            IReadOnlyList<ShuttleExternalPanelCommandContribution> commands,
            float width)
        {
            int count = commands != null ? commands.Count : 0;
            if (count <= 0)
            {
                return 0f;
            }

            int perRow = Mathf.Max(1, Mathf.FloorToInt((width + 6f) / 104f));
            int rows = Mathf.CeilToInt((float)count / perRow);
            return Mathf.Clamp(8f + rows * 32f, 40f, 104f);
        }

        internal void Draw(
            Rect rect,
            ExternalModuleUIReadModel selected,
            IReadOnlyList<ShuttleExternalPanelCommandContribution> commands,
            IShuttleExternalPanelCommandUIActions actions,
            System.Action onCommandCompleted)
        {
            if (commands == null || commands.Count == 0)
            {
                return;
            }

            ShuttleUILayout.DrawCardBackground(
                rect,
                false,
                false,
                ShuttleUIStyle.ActionRowBgColor);
            float gap = 6f;
            float x = rect.x + 6f;
            float y = rect.y + 6f;
            float buttonWidth = Mathf.Min(118f, Mathf.Max(88f, (rect.width - 18f) / 2f));
            for (int i = 0; i < commands.Count; i++)
            {
                ShuttleExternalPanelCommandContribution contribution = commands[i];
                if (contribution == null)
                {
                    continue;
                }

                if (x + buttonWidth > rect.xMax - 6f)
                {
                    x = rect.x + 6f;
                    y += 32f;
                }

                if (y + 26f > rect.yMax - 4f)
                {
                    this.DrawMoreCommands(new Rect(x, y, rect.xMax - x - 6f, 22f), commands.Count - i);
                    return;
                }

                this.DrawCommandButton(
                    new Rect(x, y, buttonWidth, 26f),
                    selected,
                    contribution,
                    actions,
                    onCommandCompleted);
                x += buttonWidth + gap;
            }
        }

        private void DrawCommandButton(
            Rect rect,
            ExternalModuleUIReadModel selected,
            ShuttleExternalPanelCommandContribution contribution,
            IShuttleExternalPanelCommandUIActions actions,
            System.Action onCommandCompleted)
        {
            string disabledReason = actions != null
                ? actions.GetPanelCommandDisabledReason(selected, contribution)
                : this.text.Tr("CT_Shuttle_ExternalRuntime_CommandExecutorUnavailable");
            bool enabled = string.IsNullOrEmpty(disabledReason);
            string tooltip = enabled ? contribution.Tooltip : disabledReason;
            if (this.panel.DrawButton(
                rect,
                contribution.Label,
                enabled,
                V3ExternalModulesText.BlueColor,
                tooltip) &&
                actions != null)
            {
                actions.ExecutePanelCommand(selected, contribution, onCommandCompleted);
            }
        }

        private void DrawMoreCommands(Rect rect, int count)
        {
            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.SafeLabel(
                rect,
                this.text.Tr("CT_Shuttle_ExternalRuntime_MoreCommands", count));
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
        }
    }
}
