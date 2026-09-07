using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Tutorial
{
    internal sealed class ShuttleTutorialDefinition
    {
        internal readonly ShuttleControlPageId Page;
        internal readonly List<ShuttleTutorialStep> Steps;

        internal ShuttleTutorialDefinition(
            ShuttleControlPageId page,
            List<ShuttleTutorialStep> steps)
        {
            this.Page = page;
            this.Steps = steps ?? new List<ShuttleTutorialStep>();
        }
    }
}
