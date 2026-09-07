using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Cargo;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    internal sealed class V3CargoBayContentsRowDrawer
    {
        internal const float RowHeight = 56f;
        internal const float RowStride = 60f;
        private const float InfoButtonSize = 24f;
        private const float InfoButtonMargin = 8f;
        private const float PassiveInfoIconInset = 4f;

        private static readonly Color CardColor =
            new Color(0.070f, 0.083f, 0.105f, 0.74f);
        private static readonly Color SelectedCardColor =
            new Color(0.095f, 0.125f, 0.155f, 0.88f);
        private static readonly Color AccentColor =
            new Color(0.24f, 0.62f, 0.78f, 0.84f);

        internal bool Draw(
            Rect rect,
            ShuttleCargoStackActionTarget stack,
            bool selected)
        {
            if (stack == null)
            {
                return false;
            }

            Widgets.DrawBoxSolid(rect, selected ? SelectedCardColor : CardColor);
            ShuttleUILayout.DrawRectBorder(
                rect,
                selected ? AccentColor : ShuttleUIStyle.SubtleBorderColor,
                selected ? ShuttleUIStyle.ThickBorder : ShuttleUIStyle.ThinBorder);
            if (selected)
            {
                Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, 4f, rect.height), AccentColor);
            }

            bool hovered = Mouse.IsOver(rect);
            if (hovered)
            {
                Widgets.DrawHighlight(rect);
            }

            Rect iconRect = new Rect(rect.x + 8f, rect.y + 14f, 28f, 28f);
            this.DrawStackIcon(iconRect, stack);
            Rect infoRect = new Rect(
                rect.xMax - InfoButtonMargin - InfoButtonSize,
                rect.y + 5f,
                InfoButtonSize,
                InfoButtonSize);
            float labelRight = stack.DisplayThing != null
                ? infoRect.x - 6f
                : rect.xMax - InfoButtonMargin;

            Rect labelRect = new Rect(
                iconRect.xMax + 10f,
                rect.y + 8f,
                Mathf.Max(32f, labelRight - iconRect.xMax - 10f),
                22f);
            Rect sourceRect = new Rect(
                rect.xMax - 118f,
                rect.y + 32f,
                110f,
                18f);
            Rect countRect = new Rect(
                iconRect.xMax + 10f,
                rect.y + 32f,
                Mathf.Max(32f, sourceRect.x - iconRect.xMax - 16f),
                18f);

            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            ShuttleUILayout.SafeLabel(
                labelRect,
                this.FitLabelText(this.GetStackLabel(stack), labelRect.width));
            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.SafeLabel(
                countRect,
                this.FitLabelText(this.GetStackDetailText(stack), countRect.width));
            ShuttleUILayout.SafeLabel(
                sourceRect,
                this.FitLabelText(this.GetSourceText(stack), sourceRect.width));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            this.DrawInfoAction(infoRect, stack.DisplayThing, hovered);

            TooltipHandler.TipRegion(
                rect,
                this.GetStackLabel(stack) + "\n" +
                this.GetStackDetailText(stack) + "\n" +
                this.GetSourceText(stack));

            Event evt = Event.current;
            if (evt != null &&
                evt.type == EventType.MouseDown &&
                evt.button == 0 &&
                rect.Contains(evt.mousePosition))
            {
                evt.Use();
                return true;
            }

            return false;
        }

        private void DrawInfoAction(Rect rect, Thing displayThing, bool hovered)
        {
            if (displayThing == null)
            {
                return;
            }

            if (hovered)
            {
                Widgets.InfoCardButtonCentered(rect, displayThing);
                return;
            }

            Color previousColor = GUI.color;
            GUI.color = ShuttleUIStyle.MutedTextColor;
            GUI.DrawTexture(rect.ContractedBy(PassiveInfoIconInset), TexButton.Info);
            GUI.color = previousColor;
        }

        private void DrawStackIcon(Rect rect, ShuttleCargoStackActionTarget stack)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.030f, 0.036f, 0.044f, 0.78f));
            if (stack != null && ShuttleThingIconDrawer.Draw(rect.ContractedBy(2f), stack.DisplayThing))
            {
                ShuttleUILayout.DrawRectBorder(
                    rect,
                    ShuttleUIStyle.SubtleBorderColor,
                    ShuttleUIStyle.ThinBorder);
                return;
            }

            Widgets.DrawBoxSolid(rect.ContractedBy(9f), AccentColor);

            ShuttleUILayout.DrawRectBorder(
                rect,
                ShuttleUIStyle.SubtleBorderColor,
                ShuttleUIStyle.ThinBorder);
        }

        private string GetStackLabel(ShuttleCargoStackActionTarget stack)
        {
            return stack != null && !string.IsNullOrEmpty(stack.Label)
                ? stack.Label
                : ShuttleUIText.Tr("CT_Shuttle_Cargo_LoadedEntryUnavailable");
        }

        private string GetStackDetailText(ShuttleCargoStackActionTarget stack)
        {
            int count = stack != null ? stack.StackCount : 0;
            string massText = stack != null
                ? Mathf.Max(0f, stack.MassKg).ToString("0.#")
                : "0";
            return ShuttleUIText.Tr("CT_Shuttle_Cargo_StackMassFormat", count, massText);
        }

        private string GetSourceText(ShuttleCargoStackActionTarget stack)
        {
            if (stack != null &&
                stack.SourceKind == ShuttleCargoStackActionSourceKind.RefrigeratedCargo)
            {
                return ShuttleUIText.Tr("CT_Shuttle_Cargo_SourceRefrigerated");
            }

            if (stack != null &&
                stack.SourceKind == ShuttleCargoStackActionSourceKind.LoadedCargo)
            {
                return ShuttleUIText.Tr("CT_Shuttle_Cargo_SourceLoaded");
            }

            return ShuttleUIText.Tr("CT_Shuttle_Cargo_Unknown");
        }

        private string FitLabelText(string text, float width)
        {
            return ShuttleUILayout.FitSingleLineLabelText(
                text,
                width,
                ShuttleUIText.Tr("CT_Shuttle_Cargo_Unknown"),
                8f);
        }
    }
}
