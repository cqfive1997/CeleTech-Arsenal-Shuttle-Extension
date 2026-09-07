using System;
using CeleTech.ShuttleExtension.ModularShuttle;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Settings;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Tutorial;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Settings
{
    internal sealed class ShuttleSettingsUIActions : IShuttleSettingsUIActions
    {
        private readonly Action<ShuttleSettingsChangeKind> onSettingsChanged;
        private readonly Action<
            IShuttleSettingsUIActions,
            ShuttleControlReadModel,
            ShuttleWeaponBayReadModel> openSettingsHub;

        internal ShuttleSettingsUIActions(
            Action<ShuttleSettingsChangeKind> onSettingsChanged,
            Action<
                IShuttleSettingsUIActions,
                ShuttleControlReadModel,
                ShuttleWeaponBayReadModel> openSettingsHub)
        {
            this.onSettingsChanged = onSettingsChanged;
            this.openSettingsHub = openSettingsHub;
        }

        public bool TutorialsEnabled
        {
            get { return CeleTechShuttleMod.Settings.ShuttleControlTutorialsEnabled; }
        }

        public bool TutorialsAutoStart
        {
            get { return CeleTechShuttleMod.Settings.ShuttleControlTutorialsAutoStart; }
        }

        public void OpenSettingsHub(
            ShuttleControlReadModel controlModel,
            ShuttleWeaponBayReadModel weaponBayModel)
        {
            if (this.openSettingsHub != null)
            {
                this.openSettingsHub(this, controlModel, weaponBayModel);
            }
        }

        public void SetTutorialsEnabled(bool enabled)
        {
            CeleTechShuttleMod.Settings.ShuttleControlTutorialsEnabled = enabled;
            CeleTechShuttleMod.SaveSettingsSafe();
        }

        public void SetTutorialsAutoStart(bool enabled)
        {
            CeleTechShuttleMod.Settings.ShuttleControlTutorialsAutoStart = enabled;
            CeleTechShuttleMod.SaveSettingsSafe();
        }

        public void ResetTutorialProgress()
        {
            ShuttleTutorialProgress.ResetAll();
            ShuttleUICommandFeedback.ShowSuccess(
                ShuttleUIText.Tr("CT_Shuttle_Tutorial_ResetComplete"));
        }

        public void NotifySettingsChanged(ShuttleSettingsChangeKind changeKind)
        {
            if (this.onSettingsChanged != null)
            {
                this.onSettingsChanged(changeKind);
            }
        }

    }
}
