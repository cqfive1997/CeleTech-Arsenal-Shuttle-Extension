using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Main
{
    internal interface IShuttleMainModuleInstallUIActions
    {
        bool CanInstallModule(ShuttleControlModuleSlotModel moduleSlot);

        void OpenInstallModule(
            ShuttleControlSegmentSlotModel segment,
            ShuttleControlModuleSlotModel moduleSlot);

        string GetModuleInstallTooltip(
            ShuttleControlSegmentSlotModel segment,
            ShuttleControlModuleSlotModel moduleSlot);
    }
}
