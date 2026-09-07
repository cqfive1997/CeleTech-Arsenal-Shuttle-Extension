using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Processing;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Composition;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Processing.Dialogs
{
    internal sealed class ShuttleProcessingDialogLauncher :
        IShuttleProcessingDialogLauncher
    {
        public void OpenProductionPolicy(
            IShuttleProcessingProductionPolicyActions actions,
            ShuttleProcessingOrderActionTarget order)
        {
            if (actions == null || order == null)
            {
                return;
            }

            Find.WindowStack.Add(new Dialog_ShuttleProcessingProductionPolicyV3(
                actions,
                order));
        }

        public void OpenIngredientFilter(
            IShuttleProcessingIngredientFilterActions actions,
            ShuttleProcessingOrderActionTarget order)
        {
            if (actions == null || order == null)
            {
                return;
            }

            Find.WindowStack.Add(new Dialog_ShuttleProcessingIngredientFilterV3(
                actions,
                order));
        }
    }
}
