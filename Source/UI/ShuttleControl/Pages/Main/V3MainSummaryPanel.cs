using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Main
{
    internal sealed class V3MainSummaryPanel
    {
        private const int CardCount = 6;

        private readonly V3MainText text;
        private readonly V3MainPanelDrawer panel;

        internal V3MainSummaryPanel(V3MainText text, V3MainPanelDrawer panel)
        {
            this.text = text;
            this.panel = panel;
        }

        internal void Draw(
            Rect rect,
            V3MainPageModel model,
            V3MainPageState state,
            ShuttlePageDrawContext context,
            V3MainExternalRuntimePanel externalRuntimePanel)
        {
            if (model == null)
            {
                return;
            }

            ShuttleControlReadModel controlModel =
                model.ControlModel ?? new ShuttleControlReadModel();
            ShuttleCargoSnapshot cargoSnapshot =
                model.CargoSnapshot ?? new ShuttleCargoSnapshot();
            float gap = 8f;
            float cardWidth = (rect.width - (gap * (CardCount - 1))) / CardCount;
            float x = rect.x;
            this.DrawProfileCard(new Rect(x, rect.y, cardWidth, rect.height), controlModel);
            x += cardWidth + gap;
            this.DrawAssemblyCard(new Rect(x, rect.y, cardWidth, rect.height), controlModel);
            x += cardWidth + gap;
            this.DrawModuleCard(new Rect(x, rect.y, cardWidth, rect.height), controlModel);
            x += cardWidth + gap;
            this.DrawMassCard(new Rect(x, rect.y, cardWidth, rect.height), controlModel);
            x += cardWidth + gap;
            this.DrawLaunchCard(new Rect(x, rect.y, cardWidth, rect.height), controlModel, cargoSnapshot);
            x += cardWidth + gap;
            externalRuntimePanel.DrawSummaryCard(
                new Rect(x, rect.y, cardWidth, rect.height),
                model,
                state,
                context,
                this.panel);
        }

        private void DrawProfileCard(Rect rect, ShuttleControlReadModel model)
        {
            this.panel.DrawSummaryCard(
                rect,
                this.text.Tr("CT_Shuttle_Main_Profile"),
                this.text.Tr("CT_Shuttle_Main_ProfileRevisionShort", model.ProfileRevision),
                model.IsProfileDirty ? V3MainText.YellowColor : V3MainText.GreenColor,
                this.text.Tr("CT_Shuttle_Main_Profile"));
        }

        private void DrawAssemblyCard(Rect rect, ShuttleControlReadModel model)
        {
            this.panel.DrawSummaryCard(
                rect,
                this.text.Tr("CT_Shuttle_UI_Assembly"),
                this.text.FormatCountPair(
                    model.InstalledSegmentCount,
                    model.SegmentSlotCount),
                V3MainText.BlueColor,
                this.text.Tr("CT_Shuttle_UI_Assembly"));
        }

        private void DrawModuleCard(Rect rect, ShuttleControlReadModel model)
        {
            this.panel.DrawSummaryCard(
                rect,
                this.text.Tr("CT_Shuttle_Main_FunctionModules"),
                this.text.FormatCountPair(
                    model.InstalledModuleCount,
                    model.ModuleSlotCount),
                V3MainText.AccentColor,
                this.text.Tr("CT_Shuttle_Main_FunctionModules"));
        }

        private void DrawMassCard(Rect rect, ShuttleControlReadModel model)
        {
            string value = ShuttleUIMetricFormatter.FormatKgPair(
                Mathf.Max(0f, model.TotalMass),
                Mathf.Max(0f, model.MassCapacity));
            this.panel.DrawSummaryCard(
                rect,
                this.text.Tr("CT_Shuttle_Main_Mass"),
                value,
                V3MainText.YellowColor,
                value);
        }

        private void DrawLaunchCard(
            Rect rect,
            ShuttleControlReadModel model,
            ShuttleCargoSnapshot cargoSnapshot)
        {
            string value = this.text.FormatLaunchReady(model.LaunchReadyProgress);
            string tooltip = this.text.Tr("CT_Shuttle_Main_Range") + ": " +
                model.HardRangeCapTiles.ToString() + " " +
                this.text.Tr("CT_Shuttle_Main_Tiles");
            if (cargoSnapshot != null && cargoSnapshot.TotalPlannedMassKg > 0f)
            {
                tooltip = tooltip + "\n" + this.text.Tr("CT_Shuttle_Main_Cargo") +
                    ": " + ShuttleUIMetricFormatter.FormatKg(cargoSnapshot.TotalPlannedMassKg);
            }

            this.panel.DrawSummaryCard(
                rect,
                this.text.Tr("CT_Shuttle_Main_LaunchReady"),
                value,
                model.LaunchReadyProgress >= 1f ? V3MainText.GreenColor : V3MainText.YellowColor,
                tooltip);
        }
    }
}
