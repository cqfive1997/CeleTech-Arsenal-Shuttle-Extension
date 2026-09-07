using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Chrome;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Chrome;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Settings
{
    internal sealed class V3SettingsHeader
    {
        private readonly ShuttlePageHeaderIdentitySpecBuilder identitySpecBuilder =
            new ShuttlePageHeaderIdentitySpecBuilder();
        private readonly ShuttlePageHeaderStatusSpecBuilder statusSpecBuilder =
            new ShuttlePageHeaderStatusSpecBuilder();
        private readonly ShuttleGlobalCommandSpecBuilder commandSpecBuilder =
            new ShuttleGlobalCommandSpecBuilder();

        internal void Draw(
            Rect rect,
            V3SettingsPageModel model,
            ShuttlePageDrawContext context)
        {
            if (model == null || context == null)
            {
                return;
            }

            ShuttleControlReadModel controlModel =
                model.ControlModel ?? new ShuttleControlReadModel();
            ShuttleSettingsReadModel settingsModel = model.SettingsModel;
            ShuttleCargoSnapshot cargoSnapshot =
                context.ReadModels != null && context.ReadModels.CargoSnapshot != null
                    ? context.ReadModels.CargoSnapshot
                    : new ShuttleCargoSnapshot();
            ShuttleWeaponBayReadModel weaponBayModel =
                context.ReadModels != null && context.ReadModels.WeaponBayModel != null
                    ? context.ReadModels.WeaponBayModel
                    : ShuttleWeaponBayReadModel.Empty;

            ShuttleControlHeaderDrawer.Draw(
                rect,
                this.identitySpecBuilder.GetTitle(controlModel),
                this.identitySpecBuilder.GetLogo(context),
                this.statusSpecBuilder.Build(
                    controlModel,
                    cargoSnapshot,
                    weaponBayModel,
                    ShuttlePageHeaderShieldMode.AggregateAnyMaxHitPoints),
                this.commandSpecBuilder.Build(context),
                new ShuttleHeaderRibbonSpec(
                    ShuttleHeaderProgressSpec.Empty,
                    this.BuildMetricSpecs(settingsModel)));
        }

        private IList<ShuttleHeaderMetricSpec> BuildMetricSpecs(
            ShuttleSettingsReadModel model)
        {
            return new List<ShuttleHeaderMetricSpec>
            {
                new ShuttleHeaderMetricSpec(
                    ShuttleUIText.Tr("CT_Shuttle_Settings_Version"),
                    ResolveValue(model != null ? model.ModVersion : null),
                    null,
                    ShuttleUIStyle.BlueStatusColor),
                new ShuttleHeaderMetricSpec(
                    ShuttleUIText.Tr("CT_Shuttle_Settings_Authors"),
                    ResolveValue(model != null ? model.AuthorsSummary : null),
                    null,
                    ShuttleUIStyle.GreenStatusColor),
                new ShuttleHeaderMetricSpec(
                    ShuttleUIText.Tr("CT_Shuttle_Settings_Credits"),
                    ResolveValue(model != null ? model.CreditsSummary : null),
                    model != null ? model.CreditsTooltip : null,
                    ShuttleUIStyle.YellowStatusColor)
            };
        }

        private static string ResolveValue(string value)
        {
            return string.IsNullOrEmpty(value) ? "-" : value;
        }
    }
}
