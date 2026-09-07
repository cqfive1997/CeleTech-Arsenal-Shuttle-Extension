using System;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal sealed class Dialog_ShuttleCargoUnloadSelectionV3 : Window
    {
        private readonly V3CargoUnloadRowModel row;
        private readonly Action<int> apply;
        private string buffer;
        private bool focused;

        internal Dialog_ShuttleCargoUnloadSelectionV3(
            V3CargoUnloadRowModel row,
            int current,
            Action<int> apply)
        {
            this.row = row;
            this.apply = apply;
            this.buffer = current.ToString();
            this.forcePause = false;
            this.doCloseX = true;
            this.closeOnClickedOutside = true;
            this.absorbInputAroundWindow = true;
        }

        public override Vector2 InitialSize
        {
            get { return new Vector2(420f, 235f); }
        }

        public override void DoWindowContents(Rect inRect)
        {
            ShuttleUILayout.DrawPanelBackground(inRect);
            Text.Font = GameFont.Medium;
            ShuttleUILayout.SafeLabel(
                new Rect(10f, 8f, inRect.width - 20f, 30f),
                V3CargoLoadText.Tr("CT_Shuttle_Unload_ChooseQuantity"));
            Text.Font = GameFont.Small;

            Rect summary = new Rect(10f, 48f, inRect.width - 20f, 44f);
            V3CargoLoadDialogStyle.DrawSurface(summary);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                summary.ContractedBy(8f),
                (this.row != null ? this.row.Label : "-") + "  " +
                    V3CargoLoadText.Tr(
                        "CT_Shuttle_Unload_AvailableCount",
                        this.row != null ? this.row.AvailableCount : 0),
                GameFont.Small,
                GameFont.Tiny,
                Color.white,
                null,
                TextAnchor.MiddleLeft);

            Rect slider = new Rect(10f, 105f, inRect.width - 20f, 28f);
            int value = this.Parse();
            value = Mathf.RoundToInt(Widgets.HorizontalSlider(
                slider,
                value,
                0f,
                this.row != null ? this.row.AvailableCount : 0,
                true));
            this.buffer = value.ToString();

            Rect field = new Rect(10f, 140f, inRect.width - 20f, 30f);
            GUI.SetNextControlName("CT_Shuttle_UnloadSelection_Count");
            this.buffer = Widgets.TextField(field, this.buffer ?? string.Empty);
            if (!this.focused)
            {
                GUI.FocusControl("CT_Shuttle_UnloadSelection_Count");
                this.focused = true;
            }

            Rect cancel = new Rect(10f, inRect.yMax - 38f, 110f, 30f);
            Rect applyRect = new Rect(inRect.xMax - 120f, cancel.y, 110f, 30f);
            if (V3CargoLoadDialogStyle.DrawButton(
                cancel,
                V3CargoLoadText.Tr("CT_Shuttle_UI_Cancel"),
                true,
                ShuttleUIStyle.BorderColor,
                string.Empty))
            {
                this.Close();
            }

            if (V3CargoLoadDialogStyle.DrawButton(
                applyRect,
                V3CargoLoadText.Tr("CT_Shuttle_UI_Apply"),
                true,
                V3CargoLoadDialogStyle.AccentColor,
                string.Empty))
            {
                this.ApplyAndClose();
            }

            if (Event.current != null &&
                Event.current.type == EventType.KeyDown &&
                (Event.current.keyCode == KeyCode.Return ||
                    Event.current.keyCode == KeyCode.KeypadEnter))
            {
                Event.current.Use();
                this.ApplyAndClose();
            }
        }

        private int Parse()
        {
            int parsed;
            if (!int.TryParse(this.buffer, out parsed))
            {
                parsed = 0;
            }

            int max = this.row != null ? this.row.AvailableCount : 0;
            return Mathf.Clamp(parsed, 0, max);
        }

        private void ApplyAndClose()
        {
            if (this.apply != null)
            {
                this.apply(this.Parse());
            }

            this.Close();
        }
    }
}
