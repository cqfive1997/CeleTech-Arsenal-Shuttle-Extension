using System;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared.Dialogs;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal sealed class Dialog_ShuttleCargoBatchQuantityV3 : Window
    {
        private const string QuantityControlName = "CT_Shuttle_LoadCargo_BatchQuantity";
        private readonly int targetCount;
        private readonly Action<int> onApply;
        private string quantityBuffer = "1";
        private bool invalidQuantity;
        private bool focusQuantity = true;

        internal Dialog_ShuttleCargoBatchQuantityV3(
            int targetCount,
            Action<int> onApply)
        {
            this.targetCount = targetCount > 0 ? targetCount : 0;
            this.onApply = onApply;
            this.forcePause = false;
            this.doCloseX = true;
            this.closeOnClickedOutside = false;
            this.absorbInputAroundWindow = true;
            this.draggable = true;
            this.resizeable = false;
        }

        public override Vector2 InitialSize
        {
            get { return new Vector2(430f, 214f); }
        }

        public override void DoWindowContents(Rect inRect)
        {
            ShuttleV3DialogLayout.DrawPanelBackground(inRect);
            Rect contentRect = inRect.ContractedBy(14f);
            Rect titleRect = new Rect(
                contentRect.x + 2f,
                contentRect.y + 2f,
                contentRect.width - 4f,
                26f);
            Rect promptRect = new Rect(
                contentRect.x + 2f,
                titleRect.yMax + 10f,
                contentRect.width - 4f,
                38f);
            Rect inputRect = new Rect(
                contentRect.x + 2f,
                promptRect.yMax + 8f,
                150f,
                32f);
            Rect errorRect = new Rect(
                inputRect.xMax + 10f,
                inputRect.y + 5f,
                Mathf.Max(0f, contentRect.xMax - inputRect.xMax - 12f),
                22f);
            Rect buttonRow = new Rect(
                contentRect.x,
                contentRect.yMax - 34f,
                contentRect.width,
                34f);
            Rect cancelRect = new Rect(
                buttonRow.xMax - 116f,
                buttonRow.y,
                116f,
                32f);
            Rect applyRect = new Rect(
                cancelRect.x - ShuttleV3DialogStyle.Gap - 116f,
                cancelRect.y,
                116f,
                32f);

            Text.Font = GameFont.Medium;
            GUI.color = ShuttleV3DialogStyle.BlueStatusColor;
            ShuttleUILayout.SafeLabel(
                titleRect,
                V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_BatchQuantityTitle"));
            Text.Font = GameFont.Small;
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.SafeLabel(
                promptRect,
                V3CargoLoadText.Tr(
                    "CT_Shuttle_LoadCargo_BatchQuantityPrompt",
                    this.targetCount));

            GUI.color = Color.white;
            GUI.SetNextControlName(QuantityControlName);
            this.quantityBuffer = Widgets.TextField(
                inputRect,
                this.quantityBuffer ?? string.Empty);
            if (this.focusQuantity)
            {
                GUI.FocusControl(QuantityControlName);
                this.focusQuantity = false;
            }

            if (this.invalidQuantity)
            {
                GUI.color = V3CargoLoadDialogStyle.RedColor;
                ShuttleUILayout.SafeLabel(
                    errorRect,
                    V3CargoLoadText.Tr("CT_Shuttle_LoadCargo_BatchQuantityInvalid"));
            }

            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            if (V3CargoLoadDialogStyle.DrawButton(
                applyRect,
                V3CargoLoadText.Tr("CT_Shuttle_UI_Apply"),
                true,
                V3CargoLoadDialogStyle.AccentColor,
                null))
            {
                this.TryApply();
            }

            if (V3CargoLoadDialogStyle.DrawButton(
                cancelRect,
                V3CargoLoadText.Tr("CT_Shuttle_UI_Cancel"),
                true,
                ShuttleUIStyle.BorderColor,
                null))
            {
                this.Close(false);
            }

            Event evt = Event.current;
            if (evt != null &&
                evt.type == EventType.KeyDown &&
                evt.keyCode == KeyCode.Return)
            {
                this.TryApply();
                evt.Use();
            }
        }

        private void TryApply()
        {
            int quantity;
            if (!int.TryParse(this.quantityBuffer, out quantity) || quantity <= 0)
            {
                this.invalidQuantity = true;
                return;
            }

            this.invalidQuantity = false;
            if (this.onApply != null)
            {
                this.onApply(quantity);
            }

            this.Close(false);
        }
    }
}
