using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Crew
{
    internal interface IShuttleCrewJoyUIActions
    {
        bool CanOpenHabitatJoyConfig(ShuttleControlReadModel model);

        bool OpenHabitatJoyConfig(ShuttleControlReadModel model);

        string GetHabitatJoyConfigTooltip(ShuttleControlReadModel model);

        bool CanSaveHabitatJoyKinds(
            ShuttleHabitatJoyConfigReadModel config,
            IReadOnlyList<string> joyKindDefNames);

        bool SaveHabitatJoyKinds(
            ShuttleHabitatJoyConfigReadModel config,
            IReadOnlyList<string> joyKindDefNames);

        string GetHabitatJoySaveTooltip(
            ShuttleHabitatJoyConfigReadModel config,
            IReadOnlyList<string> joyKindDefNames);
    }
}
