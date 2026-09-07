using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Crew;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Crew
{
    internal sealed class Dialog_ShuttleCrewUnloadV3 : Window
    {
        private const float RowHeight = 76f;
        private const float RowGap = 6f;
        private const float FooterHeight = 42f;

        private readonly List<V3CrewUnloadOption> options;
        private readonly HashSet<string> selectedKeys = new HashSet<string>();
        private readonly IShuttleCrewLoadedCrewUIActions loadedCrewActions;
        private readonly IShuttleCrewHabitatUIActions habitatActions;
        private readonly IShuttleCrewMechChargerUIActions mechChargerActions;
        private readonly V3CrewUnloadCardDrawer cardDrawer =
            new V3CrewUnloadCardDrawer();
        private readonly V3CrewUnloadBatchExecutor executor =
            new V3CrewUnloadBatchExecutor();
        private Vector2 scroll;

        internal Dialog_ShuttleCrewUnloadV3(
            List<V3CrewUnloadOption> options,
            IShuttleCrewLoadedCrewUIActions loadedCrewActions,
            IShuttleCrewHabitatUIActions habitatActions,
            IShuttleCrewMechChargerUIActions mechChargerActions)
        {
            this.options = options != null
                ? new List<V3CrewUnloadOption>(options)
                : new List<V3CrewUnloadOption>();
            this.loadedCrewActions = loadedCrewActions;
            this.habitatActions = habitatActions;
            this.mechChargerActions = mechChargerActions;
            this.doCloseX = true;
            this.closeOnClickedOutside = false;
            this.absorbInputAroundWindow = true;
            this.draggable = true;
            this.resizeable = true;
            this.InitializeSelection();
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(
                    Mathf.Min(740f, Mathf.Max(520f, Verse.UI.screenWidth - 120f)),
                    Mathf.Min(620f, Mathf.Max(420f, Verse.UI.screenHeight - 120f)));
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            bool oldWordWrap = Text.WordWrap;

            try
            {
                ShuttleUILayout.DrawPanelBackground(inRect);
                this.DrawHeader(inRect);
                this.DrawOptionsList(this.GetListRect(inRect));
                this.DrawFooter(this.GetFooterRect(inRect));
            }
            finally
            {
                Text.Font = oldFont;
                Text.Anchor = oldAnchor;
                Text.WordWrap = oldWordWrap;
                GUI.color = Color.white;
            }
        }

        private void InitializeSelection()
        {
            this.selectedKeys.Clear();
            for (int i = 0; i < this.options.Count; i++)
            {
                V3CrewUnloadOption option = this.options[i];
                if (option != null &&
                    option.CanUnload &&
                    option.SelectedByDefault &&
                    !string.IsNullOrEmpty(option.StableKey))
                {
                    this.selectedKeys.Add(option.StableKey);
                }
            }
        }

        private void DrawHeader(Rect inRect)
        {
            Rect titleRect = new Rect(inRect.x + 12f, inRect.y + 8f, inRect.width - 24f, 30f);
            Rect descRect = new Rect(inRect.x + 12f, inRect.y + 42f, inRect.width - 24f, 42f);

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = false;
            GUI.color = V3CrewText.AccentColor;
            ShuttleUILayout.SafeLabel(titleRect, this.Tr("CT_Shuttle_Crew_UnloadCrewTitle"));

            Text.Font = GameFont.Small;
            Text.WordWrap = true;
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.SafeLabel(descRect, this.Tr("CT_Shuttle_Crew_UnloadCrewDesc"));
            GUI.color = Color.white;
        }

        private Rect GetListRect(Rect inRect)
        {
            return new Rect(
                inRect.x + 12f,
                inRect.y + 90f,
                inRect.width - 24f,
                Mathf.Max(0f, inRect.height - 90f - FooterHeight - 12f));
        }

        private Rect GetFooterRect(Rect inRect)
        {
            return new Rect(
                inRect.x + 12f,
                inRect.yMax - FooterHeight,
                inRect.width - 24f,
                FooterHeight);
        }

        private void DrawOptionsList(Rect rect)
        {
            float viewHeight = Mathf.Max(rect.height, this.options.Count * (RowHeight + RowGap));
            Rect viewRect = new Rect(0f, 0f, rect.width - 16f, viewHeight);
            Widgets.BeginScrollView(rect, ref this.scroll, viewRect);

            float y = 0f;
            for (int i = 0; i < this.options.Count; i++)
            {
                V3CrewUnloadOption option = this.options[i];
                Rect rowRect = new Rect(0f, y, viewRect.width, RowHeight);
                bool selected = option != null && this.selectedKeys.Contains(option.StableKey);
                if (this.cardDrawer.Draw(rowRect, option, selected))
                {
                    this.ToggleSelection(option);
                }

                y += RowHeight + RowGap;
            }

            Widgets.EndScrollView();
        }

        private void DrawFooter(Rect rect)
        {
            float buttonWidth = Mathf.Min(136f, (rect.width - 24f) / 4f);
            float gap = 8f;
            Rect selectAllRect = new Rect(rect.x, rect.y + 6f, buttonWidth, 30f);
            Rect selectNoneRect = new Rect(selectAllRect.xMax + gap, rect.y + 6f, buttonWidth, 30f);
            Rect cancelRect = new Rect(rect.xMax - buttonWidth, rect.y + 6f, buttonWidth, 30f);
            Rect executeRect = new Rect(cancelRect.x - gap - buttonWidth, rect.y + 6f, buttonWidth, 30f);

            if (this.DrawButton(selectAllRect, this.Tr("CT_Shuttle_Crew_UnloadSelectAll"), true, ShuttleUIStyle.BorderColor))
            {
                this.SelectAll();
            }

            if (this.DrawButton(selectNoneRect, this.Tr("CT_Shuttle_Crew_UnloadSelectNone"), true, ShuttleUIStyle.BorderColor))
            {
                this.selectedKeys.Clear();
            }

            if (this.DrawButton(executeRect, this.Tr("CT_Shuttle_Crew_UnloadSelected"), this.GetSelectedCount() > 0, V3CrewText.AccentColor))
            {
                this.ExecuteSelected();
            }

            if (this.DrawButton(cancelRect, this.Tr("CT_Shuttle_UI_Cancel"), true, ShuttleUIStyle.BorderColor))
            {
                this.Close(false);
            }
        }

        private bool DrawButton(Rect rect, string label, bool enabled, Color color)
        {
            return V3CargoLoadDialogStyle.DrawButton(rect, label, enabled, color, null);
        }

        private void ToggleSelection(V3CrewUnloadOption option)
        {
            if (option == null || !option.CanUnload || string.IsNullOrEmpty(option.StableKey))
            {
                return;
            }

            if (this.selectedKeys.Contains(option.StableKey))
            {
                this.selectedKeys.Remove(option.StableKey);
            }
            else
            {
                this.selectedKeys.Add(option.StableKey);
            }
        }

        private void SelectAll()
        {
            this.selectedKeys.Clear();
            for (int i = 0; i < this.options.Count; i++)
            {
                V3CrewUnloadOption option = this.options[i];
                if (option != null && option.CanUnload && !string.IsNullOrEmpty(option.StableKey))
                {
                    this.selectedKeys.Add(option.StableKey);
                }
            }
        }

        private int GetSelectedCount()
        {
            int count = 0;
            for (int i = 0; i < this.options.Count; i++)
            {
                V3CrewUnloadOption option = this.options[i];
                if (option != null && this.selectedKeys.Contains(option.StableKey))
                {
                    count++;
                }
            }

            return count;
        }

        private void ExecuteSelected()
        {
            List<V3CrewUnloadOption> selected = this.GetSelectedOptions();
            V3CrewUnloadBatchResult result = this.executor.ExecuteCrew(
                selected,
                this.loadedCrewActions,
                this.habitatActions,
                this.mechChargerActions);
            if (result != null && !string.IsNullOrEmpty(result.Message))
            {
                Messages.Message(result.Message, result.MessageType, false);
            }

            if (result != null && result.ShouldCloseDialog)
            {
                this.Close(false);
            }
        }

        private List<V3CrewUnloadOption> GetSelectedOptions()
        {
            List<V3CrewUnloadOption> selected = new List<V3CrewUnloadOption>();
            for (int i = 0; i < this.options.Count; i++)
            {
                V3CrewUnloadOption option = this.options[i];
                if (option != null && this.selectedKeys.Contains(option.StableKey))
                {
                    selected.Add(option);
                }
            }

            return selected;
        }

        private string Tr(string key)
        {
            return ShuttleUIText.Tr(key);
        }
    }
}
