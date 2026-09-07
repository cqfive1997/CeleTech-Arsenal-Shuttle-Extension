using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.Presentation
{
    internal interface IShuttleControlBootAnimationPort
    {
        bool HasShownControlPanelFirstBootAnimation { get; }

        bool HasPendingControlPanelSystemUpdateAnimation { get; }

        string LastShownControlPanelSystemUpdateVersion { get; }

        string PendingControlPanelSystemUpdateVersion { get; }

        IReadOnlyList<string> PendingSystemUpdateStepKeys { get; }

        void MarkControlPanelFirstBootAnimationShown();

        void ScheduleControlPanelSystemUpdateAnimation(IReadOnlyList<string> stepKeys);

        void ScheduleControlPanelSystemUpdateAnimationForVersion(IReadOnlyList<string> stepKeys, string version);

        void MarkControlPanelSystemUpdateAnimationShown();

        void RecordControlPanelSystemUpdateVersion(string version);

        bool ConsumeForcedControlPanelSystemUpdateScheduleForDev();

        void ForceControlPanelSystemUpdateScheduleForDev();
    }
}
