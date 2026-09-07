using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation.External;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Chrome;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Chrome;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.ExternalModules
{
    internal sealed class V3ExternalModulesHeader
    {
        private readonly V3ExternalModulesText text;
        private readonly ShuttlePageHeaderIdentitySpecBuilder identitySpecBuilder =
            new ShuttlePageHeaderIdentitySpecBuilder();
        private readonly ShuttlePageHeaderStatusSpecBuilder statusSpecBuilder =
            new ShuttlePageHeaderStatusSpecBuilder();
        private readonly ShuttleGlobalCommandSpecBuilder commandSpecBuilder =
            new ShuttleGlobalCommandSpecBuilder();

        internal V3ExternalModulesHeader(V3ExternalModulesText text)
        {
            this.text = text;
        }

        internal void Draw(
            Rect rect,
            V3ExternalModulesPageModel model,
            ShuttlePageDrawContext context)
        {
            if (model == null || context == null)
            {
                return;
            }

            ShuttleControlReadModel controlModel =
                model.ControlModel ?? new ShuttleControlReadModel();
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
                    this.BuildMetricSpecs(model.Summary)));
        }

        private IList<ShuttleHeaderMetricSpec> BuildMetricSpecs(
            ExternalModuleUIRuntimeSummary summary)
        {
            return new List<ShuttleHeaderMetricSpec>
            {
                new ShuttleHeaderMetricSpec(
                    this.text.Tr("CT_Shuttle_ExternalRuntime_Modules"),
                    summary != null ? summary.ModuleCount.ToString() : "0",
                    null,
                    V3ExternalModulesText.BlueColor),
                new ShuttleHeaderMetricSpec(
                    this.text.Tr("CT_Shuttle_ExternalRuntime_Enabled"),
                    summary != null ? summary.EnabledCount.ToString() : "0",
                    null,
                    V3ExternalModulesText.GreenColor),
                new ShuttleHeaderMetricSpec(
                    this.text.Tr("CT_Shuttle_ExternalRuntime_Disabled"),
                    summary != null ? summary.DisabledCount.ToString() : "0",
                    null,
                    V3ExternalModulesText.RedColor),
                new ShuttleHeaderMetricSpec(
                    this.text.Tr("CT_Shuttle_ExternalRuntime_Missing"),
                    summary != null ? summary.MissingRuntimeCount.ToString() : "0",
                    null,
                    V3ExternalModulesText.YellowColor),
                new ShuttleHeaderMetricSpec(
                    this.text.Tr("CT_Shuttle_ExternalRuntime_DemandW"),
                    summary != null
                        ? this.text.FormatOne(summary.TotalLastKnownPowerDemandWatts)
                        : "0",
                    null,
                    V3ExternalModulesText.AccentColor)
            };
        }
    }
}
