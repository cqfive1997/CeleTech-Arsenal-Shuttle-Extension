using System;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Settings;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Composition;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Settings.Dialogs
{
    internal sealed class ShuttleSettingsDialogLauncher :
        IShuttleSettingsDialogLauncher
    {
        private readonly Func<Rot4> getPreviewRotation;

        internal ShuttleSettingsDialogLauncher()
            : this(null)
        {
        }

        internal ShuttleSettingsDialogLauncher(Func<Rot4> getPreviewRotation)
        {
            this.getPreviewRotation = getPreviewRotation;
        }

        public void OpenSettingsHub(
            IShuttleSettingsUIActions actions,
            ShuttleControlReadModel controlModel,
            ShuttleWeaponBayReadModel weaponBayModel,
            IShuttleCommandExecutor commandExecutor)
        {
            Find.WindowStack.Add(new Dialog_ShuttleSettingsHubV3(
                actions,
                controlModel,
                weaponBayModel,
                commandExecutor,
                this.getPreviewRotation != null ? this.getPreviewRotation() : Rot4.East));
        }
    }
}
