using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Main
{
    internal interface IShuttleMainSegmentInstallUIActions
    {
        bool CanInstallSegment(ShuttleControlSegmentSlotModel segment);

        void OpenInstallSegment(ShuttleControlSegmentSlotModel segment);

        string GetSegmentInstallTooltip(ShuttleControlSegmentSlotModel segment);

        bool CanReplaceSegment(ShuttleControlSegmentSlotModel segment);

        void OpenReplaceSegment(ShuttleControlSegmentSlotModel segment);

        string GetSegmentReplaceTooltip(ShuttleControlSegmentSlotModel segment);
    }
}
