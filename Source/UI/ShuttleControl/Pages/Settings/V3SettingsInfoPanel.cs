using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Settings
{
    internal sealed class V3SettingsInfoPanel
    {
        private const float PanelContentTopOffset = 44f;

        private readonly V3SettingsRowDrawer rowDrawer;
        private readonly V3SettingsReleaseNotesDrawer releaseNotesDrawer =
            new V3SettingsReleaseNotesDrawer();

        internal V3SettingsInfoPanel(V3SettingsRowDrawer rowDrawer)
        {
            this.rowDrawer = rowDrawer;
        }

        internal void Draw(
            Rect rect,
            V3SettingsPageModel model,
            V3SettingsPageState state)
        {
            if (this.rowDrawer == null || state == null)
            {
                return;
            }

            this.rowDrawer.DrawPanelTitle(
                rect,
                ShuttleUIText.Tr("CT_Shuttle_Settings_Updates"));
            Rect inner = this.GetPanelInnerRect(rect);
            float gap = 10f;
            float availableHeight = Mathf.Max(0f, inner.height - gap);
            float upperHeight = availableHeight * 0.65f;
            if (availableHeight >= 310f)
            {
                upperHeight = Mathf.Clamp(
                    upperHeight,
                    180f,
                    availableHeight - 130f);
            }

            Rect updateRect = new Rect(inner.x, inner.y, inner.width, upperHeight);
            Rect guideRect = new Rect(
                inner.x,
                updateRect.yMax + gap,
                inner.width,
                Mathf.Max(0f, inner.yMax - updateRect.yMax - gap));

            this.releaseNotesDrawer.Draw(
                updateRect,
                model != null && model.SettingsModel != null
                    ? model.SettingsModel.ReleaseNotes
                    : null,
                model != null && model.SettingsModel != null
                    ? model.SettingsModel.ModVersion
                    : null,
                ref state.UpdateScroll);
            V3SettingsSubTitleDrawer.Draw(
                guideRect,
                ShuttleUIText.Tr("CT_Shuttle_Settings_GameplayGuide"));
            this.rowDrawer.DrawInfoCards(
                new Rect(
                    guideRect.x,
                    guideRect.y + 28f,
                    guideRect.width,
                    guideRect.height - 28f),
                model != null && model.SettingsModel != null
                    ? model.SettingsModel.GameplayGuideCards
                    : null,
                ref state.GuideScroll);
        }

        private Rect GetPanelInnerRect(Rect rect)
        {
            return new Rect(
                rect.x + 10f,
                rect.y + PanelContentTopOffset,
                Mathf.Max(0f, rect.width - 20f),
                Mathf.Max(0f, rect.height - PanelContentTopOffset - 10f));
        }
    }
}
