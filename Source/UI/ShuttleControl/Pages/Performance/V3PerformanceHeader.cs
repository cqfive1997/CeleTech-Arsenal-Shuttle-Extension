using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Chrome;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Chrome;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Performance
{
    internal sealed class V3PerformanceHeader
    {
        private readonly ShuttlePageHeaderIdentitySpecBuilder identitySpecBuilder =
            new ShuttlePageHeaderIdentitySpecBuilder();
        private readonly ShuttlePageHeaderStatusSpecBuilder statusSpecBuilder =
            new ShuttlePageHeaderStatusSpecBuilder();
        private readonly ShuttleGlobalCommandSpecBuilder commandSpecBuilder =
            new ShuttleGlobalCommandSpecBuilder();

        internal void Draw(
            Rect rect,
            ShuttlePerformanceDashboardReadModel dashboard,
            ShuttlePageDrawContext context)
        {
            if (context == null)
            {
                return;
            }

            ShuttleControlReadModel controlModel =
                context.ReadModels != null && context.ReadModels.ControlModel != null
                    ? context.ReadModels.ControlModel
                    : new ShuttleControlReadModel();
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
                    this.BuildMetrics(dashboard)));
        }

        private IList<ShuttleHeaderMetricSpec> BuildMetrics(
            ShuttlePerformanceDashboardReadModel dashboard)
        {
            ShuttlePerformanceDashboardReadModel model = dashboard ??
                ShuttlePerformanceDashboardReadModel.Empty;
            string state = model.IsCapturing
                ? ShuttleUIText.Tr("CT_Shuttle_Performance_Capturing")
                : ShuttleUIText.Tr("CT_Shuttle_Performance_Cached");
            return new List<ShuttleHeaderMetricSpec>
            {
                new ShuttleHeaderMetricSpec(
                    ShuttleUIText.Tr("CT_Shuttle_Performance_State"),
                    state,
                    null,
                    model.IsCapturing
                        ? ShuttleUIStyle.GreenStatusColor
                        : ShuttleUIStyle.YellowStatusColor),
                new ShuttleHeaderMetricSpec(
                    ShuttleUIText.Tr("CT_Shuttle_Performance_ActiveTicks"),
                    model.ActiveTickSamples.ToString(),
                    null,
                    ShuttleUIStyle.BlueStatusColor),
                new ShuttleHeaderMetricSpec(
                    ShuttleUIText.Tr("CT_Shuttle_Performance_ProfileRevision"),
                    model.ProfileRevision.ToString(),
                    null,
                    ShuttleUIStyle.MutedTextColor),
                new ShuttleHeaderMetricSpec(
                    ShuttleUIText.Tr("CT_Shuttle_Performance_ActiveModules"),
                    model.ModuleMetrics != null
                        ? model.ModuleMetrics.Count.ToString()
                        : "0",
                    null,
                    ShuttleUIStyle.GreenStatusColor)
            };
        }
    }
}
