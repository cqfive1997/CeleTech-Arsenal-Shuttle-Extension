using CeleTech.ShuttleExtension.ModularShuttle.API.Commands;
using Verse;

namespace CeleTech.ShuttleExtension.AdditionalModule.Compatibility.DubsBadHygiene
{
    internal sealed class DBHPipeCheckCommandHandler : IShuttleExternalCommandHandler
    {
        public const string LocalCommandKey = "dbh-pipe-check-now";

        public ShuttleExternalCommandResult Execute(ShuttleExternalCommandContext context)
        {
            if (!Prefs.DevMode)
            {
                return ShuttleExternalCommandResult.Failed(Tr("CT_Shuttle_Addon_DBH_PipeCheck_DevModeOnly"));
            }

            if (context == null || context.State == null)
            {
                return ShuttleExternalCommandResult.Failed(Tr("CT_Shuttle_Addon_DBH_PipeCheck_ContextMissing"));
            }

            context.State.SetBool("forcePipeCheckNow", true);
            context.State.SetString("lastPipeResult", Tr("CT_Shuttle_Addon_DBH_PipeCheck_RequestedWaiting"));
            return ShuttleExternalCommandResult.Succeeded(Tr("CT_Shuttle_Addon_DBH_PipeCheck_Requested"));
        }

        private static string Tr(string key)
        {
            return key.Translate().ToString();
        }
    }
}
