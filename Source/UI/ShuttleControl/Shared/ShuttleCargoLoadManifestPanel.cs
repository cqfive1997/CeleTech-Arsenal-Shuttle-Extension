using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal sealed class ShuttleCargoLoadManifestPanel
    {
        private readonly V3QueuedLoadPanel queuePanel;
        private readonly V3CargoLoadSelectedPanel planPanel;

        internal ShuttleCargoLoadManifestPanel(
            V3QueuedLoadPanel queuePanel,
            V3CargoLoadSelectedPanel planPanel)
        {
            this.queuePanel = queuePanel;
            this.planPanel = planPanel;
        }

        internal Rect QueueRect { get; private set; }

        internal Rect PlanRect { get; private set; }

        internal void Draw(Rect rect)
        {
            Rect queueRect;
            Rect planRect;
            this.CalculateSectionRects(rect, out queueRect, out planRect);
            this.QueueRect = queueRect;
            this.PlanRect = planRect;

            if (this.queuePanel != null)
            {
                this.queuePanel.Draw(queueRect);
            }

            if (this.planPanel != null)
            {
                this.planPanel.Draw(planRect);
            }
        }

        private void CalculateSectionRects(
            Rect rect,
            out Rect queueRect,
            out Rect planRect)
        {
            float gap = V3CargoLoadDialogStyle.ManifestSectionGap;
            float usableHeight = Mathf.Max(0f, rect.height - gap);
            V3QueuedLoadDialogModel queueModel = this.queuePanel != null
                ? this.queuePanel.CurrentModel
                : null;
            bool hasQueue = queueModel != null && queueModel.HasRows;
            float queueHeight;
            if (!hasQueue)
            {
                queueHeight = Mathf.Min(
                    usableHeight,
                    V3CargoLoadDialogStyle.ManifestEmptyQueueHeight);
            }
            else
            {
                float desired = usableHeight * V3CargoLoadDialogStyle.ManifestQueueRatio;
                float maximum = Mathf.Max(
                    0f,
                    usableHeight - V3CargoLoadDialogStyle.ManifestPlanMinimumHeight);
                queueHeight = Mathf.Clamp(
                    desired,
                    Mathf.Min(
                        V3CargoLoadDialogStyle.ManifestQueueMinimumHeight,
                        maximum),
                    maximum);
            }

            queueRect = new Rect(rect.x, rect.y, rect.width, queueHeight);
            planRect = new Rect(
                rect.x,
                queueRect.yMax + gap,
                rect.width,
                Mathf.Max(0f, rect.yMax - queueRect.yMax - gap));
        }
    }
}
