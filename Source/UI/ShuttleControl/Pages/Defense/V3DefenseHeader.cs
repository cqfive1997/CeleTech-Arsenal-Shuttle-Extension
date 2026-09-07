using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Chrome;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Chrome;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Defense
{
    internal sealed class V3DefenseHeader
    {
        private readonly V3DefenseText text;
        private readonly ShuttlePageHeaderIdentitySpecBuilder identitySpecBuilder =
            new ShuttlePageHeaderIdentitySpecBuilder();
        private readonly ShuttlePageHeaderStatusSpecBuilder statusSpecBuilder =
            new ShuttlePageHeaderStatusSpecBuilder();
        private readonly ShuttleGlobalCommandSpecBuilder commandSpecBuilder =
            new ShuttleGlobalCommandSpecBuilder();

        internal V3DefenseHeader(V3DefenseText text)
        {
            this.text = text;
        }

        internal void Draw(Rect rect, V3DefensePageModel model, ShuttlePageDrawContext context)
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
                    this.BuildMetricSpecs(model.DefenseModel)));
        }

        private IList<ShuttleHeaderMetricSpec> BuildMetricSpecs(
            V3DefensePageReadModel defenseModel)
        {
            List<ShuttleHeaderMetricSpec> metrics =
                new List<ShuttleHeaderMetricSpec>();
            if (defenseModel == null ||
                defenseModel.Metrics == null ||
                defenseModel.Metrics.Count == 0)
            {
                return metrics;
            }

            for (int i = 0; i < defenseModel.Metrics.Count; i++)
            {
                V3DefenseMetricModel metric = defenseModel.Metrics[i];
                if (metric == null)
                {
                    continue;
                }

                metrics.Add(new ShuttleHeaderMetricSpec(
                    this.text.ValueOrDash(metric.Label),
                    this.text.ValueOrDash(metric.ShortLabel),
                    this.text.ValueOrDash(metric.Value),
                    !string.IsNullOrEmpty(metric.Tooltip)
                        ? metric.Tooltip
                        : this.text.ValueOrDash(metric.Label),
                    this.text.GetSeverityColor(metric.SeverityKey)));
            }

            return metrics;
        }
    }
}
