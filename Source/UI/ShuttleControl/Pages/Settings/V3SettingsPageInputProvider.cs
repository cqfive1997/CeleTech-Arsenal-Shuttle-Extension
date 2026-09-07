using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Inputs;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Settings
{
    internal sealed class V3SettingsPageInputProvider : IShuttlePageInputProvider
    {
        public void FillInputs(
            ShuttlePageDrawContext context,
            ShuttlePageInputBuildContext inputs)
        {
            if (context == null || inputs == null)
            {
                return;
            }

            context.SettingsInputs =
                new V3SettingsPageInputs(
                    inputs.ControlModel,
                    inputs.WeaponBayModel);
        }
    }
}
