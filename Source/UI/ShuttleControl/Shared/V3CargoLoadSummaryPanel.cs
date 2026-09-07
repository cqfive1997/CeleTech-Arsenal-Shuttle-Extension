using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal sealed class V3CargoLoadSummaryPanel
    {
        private readonly V3CargoLoadSelectionModel selectionModel;

        internal V3CargoLoadSummaryPanel(V3CargoLoadSelectionModel selectionModel)
        {
            this.selectionModel = selectionModel;
        }

        internal void Draw(
            Rect rect,
            ShuttleLoadCargoReadModel readModel,
            int passengerCount,
            int cargoCount)
        {
            Rect meterRect = new Rect(
                rect.x,
                rect.y + 4f,
                rect.width,
                V3CargoLoadDialogStyle.FooterMeterHeight);
            ShuttleUILayout.DrawLinearMeter(
                meterRect,
                this.GetMassUsagePct(),
                this.GetCapacityColor());

            Rect textRect = new Rect(
                rect.x,
                meterRect.yMax + 5f,
                rect.width,
                Mathf.Max(0f, rect.yMax - meterRect.yMax - 5f));
            float stateWidth = Mathf.Min(230f, textRect.width * 0.42f);
            Rect capacityRect = new Rect(
                textRect.x,
                textRect.y,
                Mathf.Max(0f, textRect.width - stateWidth - 8f),
                textRect.height);
            Rect stateRect = new Rect(
                capacityRect.xMax + 8f,
                textRect.y,
                stateWidth,
                textRect.height);

            ShuttleUILayout.DrawFittedSingleLineLabel(
                capacityRect,
                V3CargoLoadText.GetCapacitySummaryText(this.selectionModel),
                GameFont.Tiny,
                GameFont.Tiny,
                this.GetCapacityColor(),
                V3CargoLoadText.GetSelectedSummaryText(this.selectionModel),
                TextAnchor.MiddleLeft);
            string stateText = this.GetStateText(readModel, passengerCount + cargoCount);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                stateRect,
                stateText,
                GameFont.Tiny,
                GameFont.Tiny,
                this.GetStateColor(readModel, passengerCount + cargoCount),
                V3CargoLoadText.GetCurrentLoadSummary(readModel),
                TextAnchor.MiddleRight);
        }

        private float GetMassUsagePct()
        {
            float available = this.selectionModel != null
                ? this.selectionModel.AvailableMass()
                : 0f;
            if (available <= 0f)
            {
                return this.selectionModel != null && this.selectionModel.SelectedMass() > 0f
                    ? 1f
                    : 0f;
            }

            return Mathf.Clamp01(this.selectionModel.SelectedMass() / available);
        }

        private Color GetCapacityColor()
        {
            if (this.selectionModel != null && this.selectionModel.HasSelectionOverCapacity())
            {
                return V3CargoLoadDialogStyle.RedColor;
            }

            return this.GetMassUsagePct() >= 0.85f
                ? V3CargoLoadDialogStyle.YellowColor
                : V3CargoLoadDialogStyle.GreenColor;
        }

        private string GetStateText(ShuttleLoadCargoReadModel readModel, int loadableCount)
        {
            if (readModel == null || !readModel.HasTransporter)
            {
                return V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_Unavailable");
            }

            if (loadableCount <= 0)
            {
                return V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_NoLoadableObjectsState");
            }

            if (this.selectionModel == null || this.selectionModel.SelectedCount() <= 0)
            {
                return V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_NoSelection");
            }

            if (this.selectionModel.HasSelectionOverCapacity())
            {
                return V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_OverMass");
            }

            if (readModel.HasQueuedLoads)
            {
                return V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_ReplaceQueueState");
            }

            return V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_Ready");
        }

        private Color GetStateColor(ShuttleLoadCargoReadModel readModel, int loadableCount)
        {
            if (readModel == null || !readModel.HasTransporter)
            {
                return V3CargoLoadDialogStyle.YellowColor;
            }

            if (loadableCount <= 0 ||
                this.selectionModel == null ||
                this.selectionModel.SelectedCount() <= 0)
            {
                return ShuttleUIStyle.MutedTextColor;
            }

            if (this.selectionModel.HasSelectionOverCapacity())
            {
                return V3CargoLoadDialogStyle.RedColor;
            }

            return readModel.HasQueuedLoads
                ? V3CargoLoadDialogStyle.YellowColor
                : V3CargoLoadDialogStyle.GreenColor;
        }
    }
}
