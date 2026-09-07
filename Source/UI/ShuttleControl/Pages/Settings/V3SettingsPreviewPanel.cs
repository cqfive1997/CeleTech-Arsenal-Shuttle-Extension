using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Paint;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Settings
{
    internal sealed class V3SettingsPreviewPanel
    {
        private const float PanelContentTopOffset = 44f;

        private readonly V3SettingsRowDrawer rowDrawer;

        internal V3SettingsPreviewPanel(V3SettingsRowDrawer rowDrawer)
        {
            this.rowDrawer = rowDrawer;
        }

        internal void Draw(
            Rect rect,
            V3SettingsPageModel model,
            V3SettingsPageState state,
            ShuttlePageDrawContext context)
        {
            if (this.rowDrawer == null)
            {
                return;
            }

            this.rowDrawer.DrawPanelTitle(
                rect,
                ShuttleUIText.Tr("CT_Shuttle_Settings_ShuttlePreview"));
            Rect inner = ShuttleUIPanelChrome.GetPanelInnerRect(rect, PanelContentTopOffset);
            this.DrawShuttleBrowser(inner, model != null ? model.ControlModel : null, context);
        }

        private void DrawShuttleBrowser(
            Rect rect,
            ShuttleControlReadModel controlModel,
            ShuttlePageDrawContext context)
        {
            ShuttleUILayout.DrawCardBackground(
                rect,
                false,
                false,
                ShuttleUIStyle.SettingsPreviewCardColor);
            this.DrawPreviewDecorations(rect);

            ShuttlePaintSchemeSnapshot paint =
                controlModel != null && controlModel.PaintScheme != null
                    ? controlModel.PaintScheme
                    : ShuttlePaintSchemeSnapshot.Default;
            Rect previewRect = new Rect(
                rect.x + 22f,
                rect.y + Mathf.Max(18f, rect.height * 0.08f),
                Mathf.Max(0f, rect.width - 44f),
                Mathf.Min(
                    rect.height * 0.84f,
                    Mathf.Max(280f, rect.height * 0.78f)));
            this.DrawPaintPreview(previewRect, paint, controlModel, context);
        }

        private void DrawPaintPreview(
            Rect rect,
            ShuttlePaintSchemeSnapshot paint,
            ShuttleControlReadModel controlModel,
            ShuttlePageDrawContext context)
        {
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            Texture preview;
            if (context != null &&
                context.Services != null &&
                context.Services.PaintBrowserPreviewProvider != null &&
                context.Services.PaintBrowserPreviewProvider.TryGetPreview(
                    paint,
                    ShuttlePaintPreviewWeaponStationMask.FromControlModel(controlModel),
                    context.PreviewRotation,
                    out preview))
            {
                ShuttlePaintPreviewDrawer.Draw(rect, preview);
                return;
            }

            Texture2D fallback = context != null &&
                context.Services != null &&
                context.Services.Icons != null
                    ? context.Services.Icons.GetIcon("shuttle_kunpeng")
                    : null;
            if (fallback != null)
            {
                ShuttlePaintPreviewDrawer.Draw(rect, fallback, paint);
                return;
            }

            ShuttleUIPanelChrome.DrawCenteredEmptyPanelMessage(
                rect,
                ShuttleUIText.Tr("CT_Shuttle_Paint_Preview"));
        }

        private void DrawPreviewDecorations(Rect rect)
        {
            Color corner = ShuttleUIStyle.WithAlpha(ShuttleUIStyle.BlueStatusColor, 0.72f);
            this.DrawHudCorner(rect.x + 12f, rect.y + 12f, 1f, 1f, corner);
            this.DrawHudCorner(rect.xMax - 12f, rect.y + 12f, -1f, 1f, corner);
            this.DrawHudCorner(rect.x + 12f, rect.yMax - 12f, 1f, -1f, corner);
            this.DrawHudCorner(rect.xMax - 12f, rect.yMax - 12f, -1f, -1f, corner);
        }

        private void DrawHudCorner(float x, float y, float xDir, float yDir, Color color)
        {
            Rect horizontal = xDir < 0f
                ? new Rect(x - 32f, y, 32f, 2f)
                : new Rect(x, y, 32f, 2f);
            Rect vertical = yDir < 0f
                ? new Rect(x, y - 32f, 2f, 32f)
                : new Rect(x, y, 2f, 32f);
            Widgets.DrawBoxSolid(horizontal, color);
            Widgets.DrawBoxSolid(vertical, color);
        }
    }
}
