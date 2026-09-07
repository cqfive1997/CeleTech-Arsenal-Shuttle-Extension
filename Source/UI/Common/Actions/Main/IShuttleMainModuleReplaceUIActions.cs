using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Main
{
    internal interface IShuttleMainModuleReplaceUIActions
    {
        bool CanReplaceModule(ShuttleControlModuleSlotModel moduleSlot);

        void OpenReplaceModule(
            ShuttleControlSegmentSlotModel segment,
            ShuttleControlModuleSlotModel moduleSlot);

        string GetModuleReplaceTooltip(
            ShuttleControlSegmentSlotModel segment,
            ShuttleControlModuleSlotModel moduleSlot);
    }
}
