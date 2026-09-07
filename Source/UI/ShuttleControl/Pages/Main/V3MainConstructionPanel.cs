using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Main;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Main
{
    internal sealed class V3MainConstructionPanel
    {
        internal const float CardHeight = 122f;

        private readonly V3MainText text;
        private readonly V3MainPanelDrawer panel;

        internal V3MainConstructionPanel(
            V3MainText text,
            V3MainPanelDrawer panel)
        {
            this.text = text;
            this.panel = panel;
        }

        internal bool HasActiveConstruction(V3MainPageModel model)
        {
            return model != null &&
                model.ControlModel != null &&
                model.ControlModel.AssemblyConstruction != null &&
                model.ControlModel.AssemblyConstruction.HasActiveOrder;
        }

        internal float GetCardHeight(V3MainPageModel model, float cardWidth)
        {
            ShuttleAssemblyConstructionReadModel construction =
                model != null && model.ControlModel != null
                    ? model.ControlModel.AssemblyConstruction
                    : null;
            return this.GetCardHeight(construction, cardWidth);
        }

        internal void Draw(Rect rect, V3MainPageModel model, ShuttlePageDrawContext context)
        {
            ShuttleAssemblyConstructionReadModel construction =
                model != null && model.ControlModel != null
                    ? model.ControlModel.AssemblyConstruction
                    : null;
            if (construction == null || !construction.HasActiveOrder)
            {
                return;
            }

            this.panel.DrawCardFrame(rect, V3MainText.YellowColor, false, false);

            IShuttleMainAssemblyConstructionUIActions actions = GetActions(context);
            string tooltip = this.BuildTooltip(construction);
            float contentWidth = this.GetCardTextWidth(rect.width);
            string materialText = this.ResolveMaterialSummary(construction);
            float materialHeight = this.GetMaterialTextHeight(materialText, contentWidth);

            Rect titleRect = new Rect(rect.x + 10f, rect.y + 6f, contentWidth, 20f);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                titleRect,
                this.ResolveLabel(construction) + " / " + this.ResolveStatusLabel(construction),
                GameFont.Tiny,
                GameFont.Tiny,
                Color.white,
                tooltip,
                TextAnchor.MiddleLeft);

            Rect targetRect = new Rect(rect.x + 10f, rect.y + 28f, contentWidth, 18f);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                targetRect,
                this.ResolveTargetLabel(construction),
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.MutedTextColor,
                tooltip,
                TextAnchor.MiddleLeft);

            string materialPercent = this.FormatConstructionPercent(
                construction.MaterialProgress01,
                construction.MaterialsReady);
            Rect materialBarRect = new Rect(rect.x + 10f, rect.y + 52f, contentWidth, 14f);
            this.panel.DrawProgressBar(
                materialBarRect,
                construction.MaterialProgress01,
                materialPercent,
                V3MainText.BlueColor);
            Rect materialRect = new Rect(rect.x + 10f, rect.y + 68f, contentWidth, materialHeight);
            this.DrawWrappedTinyLabel(materialRect, materialText, ShuttleUIStyle.MutedTextColor);

            string workPercent = this.FormatConstructionPercent(
                construction.Progress01,
                this.IsWorkComplete(construction));
            Rect workBarRect = new Rect(rect.x + 10f, materialRect.yMax + 4f, contentWidth, 14f);
            this.panel.DrawProgressBar(
                workBarRect,
                construction.Progress01,
                workPercent,
                V3MainText.GreenColor);

            Rect progressRect = new Rect(rect.x + 10f, workBarRect.yMax + 2f, contentWidth, 16f);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                progressRect,
                this.text.Tr("CT_Shuttle_Main_Progress") + ": " + workPercent +
                    (construction.AwaitingMaterials
                        ? "  " + this.text.Tr("CT_Shuttle_Main_WaitingForMaterials")
                        : string.Empty),
                GameFont.Tiny,
                GameFont.Tiny,
                ShuttleUIStyle.MutedTextColor,
                tooltip,
                TextAnchor.MiddleLeft);

            this.DrawQueuedOrders(
                rect,
                progressRect.yMax + 8f,
                construction,
                actions);

            System.Action cancelAction = actions != null &&
                actions.CanCancelAssemblyConstruction(construction)
                ? delegate { actions.CancelAssemblyConstruction(construction); }
                : (System.Action)null;
            this.panel.DrawButton(
                new Rect(rect.xMax - 104f, rect.y + 10f, 94f, 26f),
                this.text.Tr("CT_Shuttle_Main_Cancel"),
                this.text.Tr("CT_Shuttle_AssemblyConstruction_CancelTooltip"),
                cancelAction,
                ShuttleUIButtonKind.Danger);

            if (Prefs.DevMode && DebugSettings.godMode)
            {
                System.Action completeAction = actions != null &&
                    actions.CanDebugCompleteAssemblyConstruction(construction)
                    ? delegate { actions.DebugCompleteAssemblyConstruction(construction); }
                    : (System.Action)null;
                this.panel.DrawButton(
                    new Rect(rect.xMax - 104f, rect.y + 40f, 94f, 22f),
                    "[DEV] Complete",
                    construction.MaterialsReady
                        ? "[DEV] Complete the current construction order immediately through final install validation."
                        : "[DEV] Materials are missing; construction cannot be completed.",
                    completeAction,
                    ShuttleUIButtonKind.Primary);

                System.Action fillAction = actions != null &&
                    actions.CanDebugFillAssemblyConstructionMaterials(construction)
                    ? delegate { actions.DebugFillAssemblyConstructionMaterials(construction); }
                    : (System.Action)null;
                this.panel.DrawButton(
                    new Rect(rect.xMax - 104f, rect.y + 64f, 94f, 22f),
                    "[DEV] Fill",
                    construction.MaterialsReady
                        ? "[DEV] Materials are already loaded."
                        : "[DEV] Create and stage missing construction materials. This does not finish construction or install anything.",
                    fillAction,
                    ShuttleUIButtonKind.Primary);
            }

            this.text.AddTooltip(materialBarRect, tooltip);
            this.text.AddTooltip(materialRect, this.BuildMaterialTooltip(construction, materialText));
            this.text.AddTooltip(workBarRect, tooltip);
            this.text.AddTooltip(progressRect, tooltip);
            this.text.AddTooltip(rect, tooltip);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
        }

        private float GetCardHeight(
            ShuttleAssemblyConstructionReadModel construction,
            float cardWidth)
        {
            if (construction == null || !construction.HasActiveOrder)
            {
                return CardHeight;
            }

            string materialText = this.ResolveMaterialSummary(construction);
            float materialHeight = this.GetMaterialTextHeight(
                materialText,
                this.GetCardTextWidth(cardWidth));
            float height = Mathf.Max(CardHeight, 106f + materialHeight);
            int queuedCount = construction.QueuedOrders != null
                ? construction.QueuedOrders.Count
                : 0;
            if (queuedCount > 0)
            {
                height += 28f + (queuedCount * 28f);
            }

            return height;
        }

        private void DrawQueuedOrders(
            Rect cardRect,
            float top,
            ShuttleAssemblyConstructionReadModel construction,
            IShuttleMainAssemblyConstructionUIActions actions)
        {
            if (construction == null ||
                construction.QueuedOrders == null ||
                construction.QueuedOrders.Count == 0)
            {
                return;
            }

            Rect titleRect = new Rect(
                cardRect.x + 10f,
                top,
                cardRect.width - 20f,
                20f);
            ShuttleUILayout.DrawFittedSingleLineLabel(
                titleRect,
                this.text.Tr("CT_Shuttle_AssemblyConstruction_QueueTitle") +
                    " (" + construction.QueuedOrders.Count.ToString() + ")",
                GameFont.Tiny,
                GameFont.Tiny,
                V3MainText.YellowColor,
                null,
                TextAnchor.MiddleLeft);

            float rowY = titleRect.yMax + 4f;
            for (int i = 0; i < construction.QueuedOrders.Count; i++)
            {
                ShuttleAssemblyConstructionQueueItemReadModel queuedOrder =
                    construction.QueuedOrders[i];
                if (queuedOrder == null)
                {
                    continue;
                }

                Rect labelRect = new Rect(
                    cardRect.x + 10f,
                    rowY,
                    cardRect.width - 116f,
                    24f);
                string label = queuedOrder.Position.ToString() + ". " +
                    this.ResolveTranslatedOrSafe(queuedOrder.TargetLabel);
                ShuttleUILayout.DrawFittedSingleLineLabel(
                    labelRect,
                    label,
                    GameFont.Tiny,
                    GameFont.Tiny,
                    ShuttleUIStyle.MutedTextColor,
                    this.ResolveTranslatedOrSafe(queuedOrder.CostSummary),
                    TextAnchor.MiddleLeft);

                System.Action cancelAction = actions != null &&
                    actions.CanCancelQueuedAssemblyConstruction(queuedOrder)
                    ? delegate { actions.CancelQueuedAssemblyConstruction(queuedOrder); }
                    : (System.Action)null;
                this.panel.DrawButton(
                    new Rect(cardRect.xMax - 104f, rowY, 94f, 24f),
                    this.text.Tr("CT_Shuttle_Main_Cancel"),
                    this.text.Tr("CT_Shuttle_AssemblyConstruction_CancelQueuedTooltip"),
                    cancelAction,
                    ShuttleUIButtonKind.Danger);
                rowY += 28f;
            }
        }

        private string BuildTooltip(ShuttleAssemblyConstructionReadModel construction)
        {
            string tooltip = this.ResolveTargetLabel(construction) + "\n" +
                this.ResolveStatusLabel(construction) + "\n" +
                this.ResolveMaterialSummary(construction) + "\n" +
                this.ResolveLabel(construction) + ": " +
                construction.WorkDone.ToString() + "/" +
                construction.WorkTotal.ToString() + "\n" +
                this.text.Tr("CT_Shuttle_Main_Cost") + ": " +
                this.ResolveTranslatedOrSafe(construction.CostSummary);
            string lastFailureReason = this.ResolveTranslatedOrSafe(construction.LastFailureReason);
            if (!string.IsNullOrEmpty(lastFailureReason) && lastFailureReason != "-")
            {
                tooltip = tooltip + "\n" + lastFailureReason;
            }

            return tooltip;
        }

        private string BuildMaterialTooltip(
            ShuttleAssemblyConstructionReadModel construction,
            string materialText)
        {
            if (construction != null && !string.IsNullOrEmpty(construction.MaterialSummary))
            {
                return this.ResolveTranslatedOrSafe(construction.MaterialSummary);
            }

            return this.text.ValueOrDash(materialText);
        }

        private string ResolveLabel(ShuttleAssemblyConstructionReadModel construction)
        {
            string value = construction != null ? construction.Label : null;
            if (string.IsNullOrEmpty(value))
            {
                return this.text.Tr("CT_Shuttle_Main_Construction");
            }

            return this.ResolveTranslatedOrSafe(value);
        }

        private string ResolveStatusLabel(ShuttleAssemblyConstructionReadModel construction)
        {
            string value = construction != null ? construction.StatusLabel : null;
            return this.ResolveTranslatedOrSafe(value);
        }

        private string ResolveTargetLabel(ShuttleAssemblyConstructionReadModel construction)
        {
            string value = construction != null ? construction.TargetLabel : null;
            return this.ResolveTranslatedOrSafe(value);
        }

        private string ResolveMaterialSummary(ShuttleAssemblyConstructionReadModel construction)
        {
            if (construction == null)
            {
                return "-";
            }

            if (construction.MaterialsReady)
            {
                return this.text.Tr("CT_Shuttle_Main_MaterialsLoaded");
            }

            return this.ResolveTranslatedOrSafe(construction.MaterialSummary);
        }

        private string ResolveTranslatedOrSafe(string value)
        {
            string translated = TranslateIfAvailable(value);
            return !string.IsNullOrEmpty(translated)
                ? translated
                : this.text.ValueOrDash(value);
        }

        private static string TranslateIfAvailable(string value)
        {
            if (string.IsNullOrEmpty(value) || !Translator.CanTranslate(value))
            {
                return null;
            }

            string translated = value.Translate().ToString();
            return string.IsNullOrEmpty(translated) || translated == value
                ? null
                : translated;
        }

        private string FormatConstructionPercent(float value01, bool complete)
        {
            int percent = Mathf.FloorToInt(Mathf.Clamp01(value01) * 100f);
            percent = Mathf.Clamp(percent, 0, 100);
            if (complete)
            {
                percent = 100;
            }
            else
            {
                percent = Mathf.Min(percent, 99);
            }

            return percent.ToString() + "%";
        }

        private bool IsWorkComplete(ShuttleAssemblyConstructionReadModel construction)
        {
            return construction != null &&
                construction.WorkTotal > 0 &&
                construction.WorkDone >= construction.WorkTotal;
        }

        private float GetCardTextWidth(float cardWidth)
        {
            return Mathf.Max(40f, cardWidth - 126f);
        }

        private float GetMaterialTextHeight(string value, float width)
        {
            if (width <= 1f)
            {
                return 16f;
            }

            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            bool oldWordWrap = Text.WordWrap;

            try
            {
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.UpperLeft;
                Text.WordWrap = true;
                return Mathf.Max(
                    16f,
                    Mathf.Ceil(Text.CalcHeight(this.text.ValueOrDash(value), width)));
            }
            finally
            {
                Text.Font = oldFont;
                Text.Anchor = oldAnchor;
                Text.WordWrap = oldWordWrap;
            }
        }

        private void DrawWrappedTinyLabel(Rect rect, string value, Color color)
        {
            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            bool oldWordWrap = Text.WordWrap;
            Color oldColor = GUI.color;

            try
            {
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.UpperLeft;
                Text.WordWrap = true;
                GUI.color = color;
                Widgets.Label(rect, this.text.ValueOrDash(value));
            }
            finally
            {
                Text.Font = oldFont;
                Text.Anchor = oldAnchor;
                Text.WordWrap = oldWordWrap;
                GUI.color = oldColor;
            }
        }

        private static IShuttleMainAssemblyConstructionUIActions GetActions(
            ShuttlePageDrawContext context)
        {
            return context != null && context.MainPageContext != null
                ? context.MainPageContext.AssemblyConstructionActions
                : null;
        }
    }
}
