using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Main
{
    internal interface IShuttleMainSegmentModulesEnablementUIActions
    {
        bool CanSetSegmentModulesEnabled(
            ShuttleControlSegmentSlotModel segment,
            bool enabled);

        void SetSegmentModulesEnabled(
            ShuttleControlSegmentSlotModel segment,
            bool enabled);

        string GetSegmentModulesEnablementTooltip(
            ShuttleControlSegmentSlotModel segment,
            bool enabled);
    }
}
