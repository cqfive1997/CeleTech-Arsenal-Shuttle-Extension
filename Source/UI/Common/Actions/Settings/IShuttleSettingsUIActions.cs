using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Settings
{
    internal interface IShuttleSettingsUIActions
    {
        bool TutorialsEnabled { get; }

        bool TutorialsAutoStart { get; }

        void OpenSettingsHub(
            ShuttleControlReadModel controlModel,
            ShuttleWeaponBayReadModel weaponBayModel);

        void SetTutorialsEnabled(bool enabled);

        void SetTutorialsAutoStart(bool enabled);

        void ResetTutorialProgress();

        void NotifySettingsChanged(ShuttleSettingsChangeKind changeKind);
    }
}
