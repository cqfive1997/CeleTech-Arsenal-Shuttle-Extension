using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Commands
{
    internal sealed class ShuttlePaintCommandHandler : IShuttleCommandHandler
    {
        public bool CanHandle(IShuttleCommand command)
        {
            return command is SetShuttlePaintSchemeCommand ||
                command is ResetShuttlePaintSchemeCommand;
        }

        public ShuttleCommandResult Execute(IShuttleCommand command, ShuttleCommandContext context)
        {
            if (context == null || context.AssemblyState == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Command_AssemblyUnavailable".Translate().ToString());
            }

            SetShuttlePaintSchemeCommand setPaint = command as SetShuttlePaintSchemeCommand;
            if (setPaint != null)
            {
                return this.ExecuteSetPaint(context, setPaint);
            }

            ResetShuttlePaintSchemeCommand resetPaint = command as ResetShuttlePaintSchemeCommand;
            if (resetPaint != null)
            {
                return this.ExecuteResetPaint(context);
            }

            return ShuttleCommandResult.Failed("CT_Shuttle_Command_Unsupported".Translate(command != null ? command.CommandID : "-").ToString());
        }

        private ShuttleCommandResult ExecuteSetPaint(
            ShuttleCommandContext context,
            SetShuttlePaintSchemeCommand command)
        {
            ShuttlePaintScheme scheme = context.AssemblyState.PaintScheme;
            if (scheme == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Paint_CommandUnavailable".Translate().ToString());
            }

            scheme.Set(
                command.Enabled,
                command.PrimaryColor,
                command.SecondaryColor,
                command.AccentColor,
                command.PresetKey);
            this.MarkPaintChanged(context);
            return ShuttleCommandResult.Succeeded("CT_Shuttle_Paint_Applied".Translate().ToString());
        }

        private ShuttleCommandResult ExecuteResetPaint(ShuttleCommandContext context)
        {
            ShuttlePaintScheme scheme = context.AssemblyState.PaintScheme;
            if (scheme == null)
            {
                return ShuttleCommandResult.Failed("CT_Shuttle_Paint_CommandUnavailable".Translate().ToString());
            }

            scheme.ResetToDefault();
            this.MarkPaintChanged(context);
            return ShuttleCommandResult.Succeeded("CT_Shuttle_Paint_ResetApplied".Translate().ToString());
        }

        private void MarkPaintChanged(ShuttleCommandContext context)
        {
            if (context != null && context.AssemblyState != null)
            {
                context.AssemblyState.MarkDirty(ShuttleDirtyFlags.ReadModel);
            }
        }
    }
}
