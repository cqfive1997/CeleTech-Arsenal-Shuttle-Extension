using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Chrome;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Chrome
{
    internal sealed class ShuttleGlobalCommandBar
    {
        internal const float Height = 42f;

        private const float CommandButtonSize = 34f;
        private const float CommandGap = 8f;
        private const float RightInset = 12f;

        private readonly ShuttleGlobalCommandSpecBuilder commandSpecBuilder =
            new ShuttleGlobalCommandSpecBuilder();

        internal static float CommandTilesWidth
        {
            get { return (CommandButtonSize * 4f) + (CommandGap * 3f); }
        }

        internal void Draw(Rect rect, ShuttlePageDrawContext context)
        {
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            ShuttleUILayout.DrawHeaderBackground(rect);
            this.DrawCommandTiles(rect, context, RightInset);
        }

        internal void DrawCommandTiles(Rect rect, ShuttlePageDrawContext context)
        {
            this.DrawCommandTiles(rect, context, 0f);
        }

        private void DrawCommandTiles(
            Rect rect,
            ShuttlePageDrawContext context,
            float rightInset)
        {
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            IList<ShuttleHeaderCommandSpec> commands =
                this.commandSpecBuilder.Build(context);

            float totalWidth = CommandTilesWidth;
            float x = rect.xMax - totalWidth - rightInset;
            float y = rect.y + ((rect.height - CommandButtonSize) / 2f);

            for (int i = 0; i < commands.Count; i++)
            {
                this.DrawCommandButton(
                    new Rect(x, y, CommandButtonSize, CommandButtonSize),
                    commands[i]);
                x += CommandButtonSize + CommandGap;
            }
        }

        private void DrawCommandButton(
            Rect rect,
            ShuttleHeaderCommandSpec command)
        {
            ShuttleUIHeaderCommandButtonSpec spec =
                new ShuttleUIHeaderCommandButtonSpec(
                rect,
                command.Icon,
                command.OnClick,
                command.Tooltip,
                command.Tooltip);
            spec.FallbackText = GetFallbackText(command.Label);
            spec.Enabled = command.Enabled && command.OnClick != null;

            if (ShuttleUIHeaderCommandButtonDrawer.Draw(spec))
            {
                ShuttleUICommandFeedback.ShowReject(
                    ShuttleUIText.Tr("CT_Shuttle_Command_ContextUnavailable"));
            }
        }

        private static string GetFallbackText(string label)
        {
            return string.IsNullOrEmpty(label) ? "?" : label.Substring(0, 1);
        }
    }
}
