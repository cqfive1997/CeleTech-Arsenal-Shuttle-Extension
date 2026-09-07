using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Crew;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Crew
{
    internal sealed class ShuttleCrewLoadedCrewUIActions : IShuttleCrewLoadedCrewUIActions
    {
        private readonly IShuttleCommandExecutor commandExecutor;

        internal ShuttleCrewLoadedCrewUIActions(IShuttleCommandExecutor commandExecutor)
        {
            this.commandExecutor = commandExecutor;
        }

        public bool CanUnloadLoadedCrew(
            int transporterIndex,
            int loadedIndex,
            int thingIDNumber,
            string defName)
        {
            return this.commandExecutor != null &&
                transporterIndex >= 0 &&
                loadedIndex >= 0 &&
                (thingIDNumber > 0 || !string.IsNullOrEmpty(defName));
        }

        public bool UnloadLoadedCrew(
            int transporterIndex,
            int loadedIndex,
            int thingIDNumber,
            string defName)
        {
            if (!this.CanUnloadLoadedCrew(transporterIndex, loadedIndex, thingIDNumber, defName))
            {
                this.ShowReject(this.GetLoadedCrewUnloadTooltip(
                    transporterIndex,
                    loadedIndex,
                    thingIDNumber,
                    defName));
                return false;
            }

            ShuttleCommandResult result = this.commandExecutor.Execute(
                new UnloadLoadedCargoEntryCommand(
                    transporterIndex,
                    loadedIndex,
                    thingIDNumber,
                    defName,
                    1));
            return this.ShowResult(result);
        }

        public string GetLoadedCrewUnloadTooltip(
            int transporterIndex,
            int loadedIndex,
            int thingIDNumber,
            string defName)
        {
            if (this.commandExecutor == null)
            {
                return this.Tr("CT_Shuttle_Command_ExecutorUnavailable");
            }

            if (transporterIndex < 0 ||
                loadedIndex < 0 ||
                (thingIDNumber <= 0 && string.IsNullOrEmpty(defName)))
            {
                return this.Tr("CT_Shuttle_Crew_UnloadSelectionChanged");
            }

            return this.Tr("CT_Shuttle_Crew_UnloadCrewTooltip");
        }

        private bool ShowResult(ShuttleCommandResult result)
        {
            return ShuttleUICommandFeedback.ShowResult(result);
        }

        private void ShowReject(string message)
        {
            ShuttleUICommandFeedback.ShowReject(message);
        }

        private string Tr(string key)
        {
            return ShuttleUIText.Tr(key);
        }
    }
}
