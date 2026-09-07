using CeleTech.ShuttleExtension.ModularShuttle.API.Commands;

namespace MyCoolMod.ShuttleRuntimeSample
{
    public sealed class SetScanModeCommandHandler : IShuttleExternalCommandHandler
    {
        public ShuttleExternalCommandResult Execute(
            ShuttleExternalCommandContext context)
        {
            if (context == null || context.Command == null)
            {
                return ShuttleExternalCommandResult.Failed("Scanner command context is unavailable.");
            }

            string mode;
            if (context.Command.Arguments == null ||
                !context.Command.Arguments.TryGetValue("mode", out mode) ||
                string.IsNullOrEmpty(mode))
            {
                mode = "standard";
            }

            context.State.SetString("scanMode", mode);
            return ShuttleExternalCommandResult.Succeeded("Scanner mode set to " + mode + ".");
        }
    }
}
