using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Chrome;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Chrome;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.PrisonCell
{
    internal sealed class V3PrisonCellHeader
    {
        private readonly ShuttlePageHeaderIdentitySpecBuilder identitySpecBuilder =
            new ShuttlePageHeaderIdentitySpecBuilder();
        private readonly ShuttlePageHeaderStatusSpecBuilder statusSpecBuilder =
            new ShuttlePageHeaderStatusSpecBuilder();
        private readonly ShuttleGlobalCommandSpecBuilder commandSpecBuilder =
            new ShuttleGlobalCommandSpecBuilder();

        internal V3PrisonCellHeader()
        {
        }

        internal void Draw(
            Rect rect,
            V3PrisonCellPageModel model,
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
                    this.BuildMetricSpecs(model.PrisonCellModel)));
        }

        private IList<ShuttleHeaderMetricSpec> BuildMetricSpecs(
            ShuttlePrisonCellReadModel prisonModel)
        {
            List<ShuttleHeaderMetricSpec> metrics =
                new List<ShuttleHeaderMetricSpec>();
            bool hasPrisonCell = prisonModel != null && prisonModel.HasPrisonCell;
            int candidateCount =
                prisonModel != null && prisonModel.Candidates != null
                    ? prisonModel.Candidates.Count
                    : 0;

            metrics.Add(new ShuttleHeaderMetricSpec(
                ShuttleUIText.Tr("CT_Shuttle_PrisonCell_PageTitle"),
                hasPrisonCell
                    ? ShuttleUIText.Tr("CT_Shuttle_PrisonCell_Installed")
                    : ShuttleUIText.Tr("CT_Shuttle_PrisonCell_NotInstalled"),
                hasPrisonCell
                    ? ShuttleUIText.Tr("CT_Shuttle_PrisonCell_Installed")
                    : ShuttleUIText.Tr("CT_Shuttle_PrisonCell_NotInstalled"),
                hasPrisonCell
                    ? V3PrisonCellText.GreenColor
                    : ShuttleUIStyle.MutedTextColor));
            metrics.Add(new ShuttleHeaderMetricSpec(
                ShuttleUIText.Tr("CT_Shuttle_PrisonCell_Capacity"),
                prisonModel != null
                    ? prisonModel.PrisonerCount.ToString() + " / " + prisonModel.PrisonerSlots.ToString()
                    : "0 / 0",
                null,
                prisonModel != null && prisonModel.FreePrisonerSlots > 0
                    ? V3PrisonCellText.GreenColor
                    : V3PrisonCellText.YellowColor));
            metrics.Add(new ShuttleHeaderMetricSpec(
                ShuttleUIText.Tr("CT_Shuttle_PrisonCell_FreeSlots"),
                prisonModel != null ? prisonModel.FreePrisonerSlots.ToString() : "0",
                null,
                V3PrisonCellText.BlueColor));
            metrics.Add(new ShuttleHeaderMetricSpec(
                ShuttleUIText.Tr("CT_Shuttle_PrisonCell_Candidates"),
                candidateCount.ToString(),
                null,
                V3PrisonCellText.YellowColor));

            return metrics;
        }
    }
}
