using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.API.UI;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation.External;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.ExternalModules;
using CeleTech.ShuttleExtension.ModularShuttle.UI.External;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.ExternalModules;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.ModelProviders.ExternalModules
{
    internal sealed class V3ExternalModulePanelModelProvider :
        IV3ExternalModulePanelModelProvider
    {
        private readonly IShuttleExternalModuleUIReadPort readPort;
        private readonly V3ExternalModulesText text = new V3ExternalModulesText();

        internal V3ExternalModulePanelModelProvider(
            IShuttleExternalModuleUIReadPort readPort)
        {
            this.readPort = readPort;
        }

        public V3ExternalModulesProviderPanelModel BuildProviderPanel(
            ExternalModuleUIReadModel selected,
            float contentWidth,
            float fallbackHeight,
            IShuttleExternalPanelCommandUIActions panelCommandActions,
            System.Action onCommandCompleted)
        {
            V3ExternalModulesProviderPanelModel result =
                new V3ExternalModulesProviderPanelModel();
            result.PanelHeight = fallbackHeight;
            if (selected == null)
            {
                result.Message = this.text.Tr("CT_Shuttle_ExternalRuntime_SelectExternalRuntime");
                return result;
            }

            if (!selected.HasExternalPanel)
            {
                result.Message = this.text.Tr("CT_Shuttle_ExternalRuntime_NoPanelProvider");
                return result;
            }

            if (!selected.RuntimeStateExists || !selected.RuntimeStateEnvelopeValid)
            {
                result.Message = this.text.Tr("CT_Shuttle_ExternalRuntime_PanelStateUnavailable");
                return result;
            }

            result.Registration = this.ResolvePanelRegistration(selected);
            if (result.Registration == null)
            {
                result.Message = this.text.Tr("CT_Shuttle_ExternalRuntime_NoActivePanelProvider");
                return result;
            }

            if (ExternalModulePanelGuard.IsDisabled(result.Registration))
            {
                result.Message = this.GetDisabledProviderMessage(result.Registration);
                return result;
            }

            result.Context = this.readPort != null
                ? this.readPort.BuildExternalModulePanelContext(
                    selected.ModuleInstanceID,
                    selected.RuntimeSystemKey,
                    result.Registration.FullPanelKey)
                : null;
            if (result.Context == null)
            {
                result.Message = this.text.Tr("CT_Shuttle_ExternalRuntime_PanelStateUnavailable");
                return result;
            }

            result.Context = result.Context.WithCommandExecutor(
                new ExternalModulePanelInlineCommandExecutor(
                    selected,
                    panelCommandActions,
                    onCommandCompleted));

            if (!ExternalModulePanelGuard.SafeCanShow(result.Registration, result.Context))
            {
                result.Message = ExternalModulePanelGuard.IsDisabled(result.Registration)
                    ? this.GetDisabledProviderMessage(result.Registration)
                    : this.text.Tr("CT_Shuttle_ExternalRuntime_PanelProviderDeclined");
                return result;
            }

            ExternalModulePanelCommandSink sink =
                new ExternalModulePanelCommandSink(
                    result.Registration.OwnerPackageId,
                    result.Registration.FullPanelKey,
                    selected.RuntimeSystemKey);
            ExternalModulePanelGuard.SafeCollectCommands(result.Registration, result.Context, sink);
            if (ExternalModulePanelGuard.IsDisabled(result.Registration))
            {
                result.Message = this.GetDisabledProviderMessage(result.Registration);
                result.Registration = null;
                result.Context = null;
                return result;
            }

            result.Commands = sink.GetContributionsSnapshot();
            result.PanelHeight = ExternalModulePanelGuard.SafeGetPreferredHeight(
                result.Registration,
                result.Context,
                Mathf.Max(0f, contentWidth - 18f),
                fallbackHeight);
            if (ExternalModulePanelGuard.IsDisabled(result.Registration))
            {
                result.Message = this.GetDisabledProviderMessage(result.Registration);
                result.Registration = null;
                result.Context = null;
            }

            return result;
        }

        private ExternalModulePanelRegistration ResolvePanelRegistration(
            ExternalModuleUIReadModel selected)
        {
            List<ExternalModulePanelRegistration> registrations =
                selected != null
                    ? ExternalModulePanelRegistry.GetRegistrationsForRuntimeKey(selected.RuntimeSystemKey)
                    : null;
            for (int i = 0; registrations != null && i < registrations.Count; i++)
            {
                ExternalModulePanelRegistration registration = registrations[i];
                if (registration != null && !ExternalModulePanelGuard.IsDisabled(registration))
                {
                    return registration;
                }
            }

            return registrations != null && registrations.Count > 0 ? registrations[0] : null;
        }

        private string GetDisabledProviderMessage(ExternalModulePanelRegistration registration)
        {
            string reason = ExternalModulePanelGuard.GetDisabledReason(registration);
            return string.IsNullOrEmpty(reason)
                ? this.text.Tr("CT_Shuttle_ExternalRuntime_PanelProviderDisabled")
                : reason;
        }
    }
}
