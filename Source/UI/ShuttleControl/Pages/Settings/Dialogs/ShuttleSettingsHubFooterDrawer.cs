using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared.Dialogs;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Settings.Dialogs
{
    internal sealed class ShuttleSettingsHubFooterDrawer
    {
        internal bool Draw(Rect rect, IShuttleSettingsHubSection section)
        {
            if (section == null)
            {
                return false;
            }

            Rect resetRect = new Rect(rect.x + 8f, rect.y + 8f, 150f, 34f);
            Rect cancelRect = new Rect(rect.xMax - 260f, rect.y + 8f, 120f, 34f);
            Rect applyRect = new Rect(rect.xMax - 130f, rect.y + 8f, 122f, 34f);
            if (ShuttleV3DialogLayout.DrawDialogButton(
                resetRect,
                "CT_Shuttle_Settings_ResetDefaults".Translate().ToString(),
                true,
                ShuttleV3DialogButtonKind.Normal,
                "CT_Shuttle_Settings_ResetConfirm".Translate().ToString()))
            {
                section.Reset();
            }

            this.DrawCommitStatus(
                new Rect(
                    resetRect.xMax + 16f,
                    rect.y + 10f,
                    Mathf.Max(0f, cancelRect.x - resetRect.xMax - 26f),
                    30f),
                section);

            string cancelLabel = section.CommitMode == ShuttleSettingsHubCommitMode.Immediate
                ? "CT_Shuttle_Settings_Close".Translate().ToString()
                : "CT_Shuttle_UI_Cancel".Translate().ToString();
            bool closeRequested = ShuttleV3DialogLayout.DrawDialogButton(
                cancelRect,
                cancelLabel,
                true,
                ShuttleV3DialogButtonKind.Normal,
                null);

            bool explicitCommit = section.CommitMode == ShuttleSettingsHubCommitMode.Explicit;
            if (ShuttleV3DialogLayout.DrawDialogButton(
                applyRect,
                explicitCommit
                    ? "CT_Shuttle_UI_Apply".Translate().ToString()
                    : "CT_Shuttle_SettingsHub_Immediate".Translate().ToString(),
                explicitCommit && section.CanApply,
                ShuttleV3DialogButtonKind.Primary,
                explicitCommit ? section.ApplyDisabledReason : null))
            {
                section.Apply();
            }

            return closeRequested;
        }

        private void DrawCommitStatus(Rect rect, IShuttleSettingsHubSection section)
        {
            GameFont oldFont = Text.Font;
            Color oldColor = GUI.color;
            TextAnchor oldAnchor = Text.Anchor;
            try
            {
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleRight;
                if (section.CommitMode == ShuttleSettingsHubCommitMode.Immediate)
                {
                    GUI.color = ShuttleV3DialogStyle.BlueStatusColor;
                    ShuttleV3DialogLayout.SafeLabel(
                        rect,
                        "CT_Shuttle_SettingsHub_ImmediateHint".Translate().ToString());
                }
                else if (section.HasPendingChanges)
                {
                    GUI.color = ShuttleV3DialogStyle.YellowStatusColor;
                    ShuttleV3DialogLayout.SafeLabel(
                        rect,
                        "CT_Shuttle_SettingsHub_PendingHint".Translate().ToString());
                }
            }
            finally
            {
                Text.Font = oldFont;
                GUI.color = oldColor;
                Text.Anchor = oldAnchor;
            }
        }
    }
}
