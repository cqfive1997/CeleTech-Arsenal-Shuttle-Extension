using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Crew;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared.Dialogs;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Icons;
using RimWorld;
using UnityEngine;
using Verse;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Crew.Dialogs
{
    /// <summary>
    /// V3 dialog for the Habitat recreation configuration flow.
    /// It edits local JoyKindDef-name working copies only; saves go through
    /// SetHabitatJoyKindsCommand via the supplied crew joy action surface.
    /// </summary>
    internal sealed class Dialog_ShuttleHabitatJoyConfigV3 : Window
    {
        private const float HeaderHeight = 54f;
        private const float FooterHeight = 42f;
        private const float ModuleListWidth = 210f;
        private const float RowHeight = 34f;

        private readonly IShuttleCrewJoyUIActions crewActions;
        private readonly List<ShuttleHabitatJoyConfigReadModel> configs;
        private readonly Dictionary<string, HashSet<string>> originalSelections =
            new Dictionary<string, HashSet<string>>();
        private readonly Dictionary<string, HashSet<string>> workingSelections =
            new Dictionary<string, HashSet<string>>();
        private readonly ShuttleControlIconRegistry icons = new ShuttleControlIconRegistry();

        private int selectedConfigIndex;
        private Vector2 moduleScroll;
        private Vector2 joyScroll;
        private bool applyingOnClose;

        internal Dialog_ShuttleHabitatJoyConfigV3(
            IShuttleCrewJoyUIActions crewActions,
            IReadOnlyList<ShuttleHabitatJoyConfigReadModel> configs)
        {
            this.crewActions = crewActions;
            this.configs = configs != null
                ? new List<ShuttleHabitatJoyConfigReadModel>(configs)
                : new List<ShuttleHabitatJoyConfigReadModel>();
            this.InitializeSelections();
            this.forcePause = false;
            this.doCloseX = true;
            this.closeOnClickedOutside = false;
            this.absorbInputAroundWindow = false;
            this.draggable = true;
            this.resizeable = false;
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(760f, 560f);
            }
        }

        public override void PreClose()
        {
            if (!this.applyingOnClose && this.HasUnsavedChanges())
            {
                this.applyingOnClose = true;
                this.TrySaveDirtySelections();
                this.applyingOnClose = false;
            }

            base.PreClose();
        }

        public override void DoWindowContents(Rect inRect)
        {
            ShuttleV3DialogLayout.DrawPanelBackground(inRect);
            Rect headerRect = new Rect(inRect.x + 12f, inRect.y + 10f, inRect.width - 24f, HeaderHeight);
            Rect footerRect = new Rect(inRect.x + 12f, inRect.yMax - FooterHeight - 8f, inRect.width - 24f, FooterHeight);
            Rect bodyRect = new Rect(
                inRect.x + 12f,
                headerRect.yMax + ShuttleV3DialogStyle.Gap,
                inRect.width - 24f,
                footerRect.y - headerRect.yMax - (ShuttleV3DialogStyle.Gap * 2f));

            this.DrawHeader(headerRect);
            this.DrawBody(bodyRect);
            this.DrawFooter(footerRect);
        }

        private void InitializeSelections()
        {
            for (int i = 0; i < this.configs.Count; i++)
            {
                ShuttleHabitatJoyConfigReadModel config = this.configs[i];
                if (config == null || string.IsNullOrEmpty(config.ModuleInstanceID))
                {
                    continue;
                }

                HashSet<string> selected = this.ExtractSelected(config);
                HashSet<string> clamped = this.ClampSelectionToCapacity(config, selected);
                this.originalSelections[config.ModuleInstanceID] = new HashSet<string>(clamped);
                this.workingSelections[config.ModuleInstanceID] = new HashSet<string>(clamped);
            }
        }

        private void DrawHeader(Rect rect)
        {
            Rect iconRect = new Rect(rect.x + 4f, rect.y + 4f, 42f, 42f);
            ShuttleV3DialogLayout.DrawIconOrFallback(iconRect, this.icons.GetIcon("habitat"), "H", 0.9f);

            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(iconRect.xMax + 10f, rect.y + 3f, rect.width - 260f, 26f);
            ShuttleV3DialogLayout.SafeLabel(
                titleRect,
                ShuttleUIText.Tr("CT_Shuttle_HabitatJoyConfig_Title"));
            Text.Font = GameFont.Small;

            Rect subtitleRect = new Rect(iconRect.xMax + 10f, titleRect.yMax + 2f, rect.width - 260f, 22f);
            GUI.color = ShuttleV3DialogStyle.MutedTextColor;
            ShuttleV3DialogLayout.SafeLabel(
                subtitleRect,
                "CT_Shuttle_HabitatJoy_HeaderTooltip".Translate().ToString());
            GUI.color = Color.white;

            Rect summaryRect = new Rect(rect.xMax - 230f, rect.y + 8f, 226f, 34f);
            ShuttleV3DialogLayout.DrawCardBackground(summaryRect, false, false, ShuttleV3DialogStyle.TimeChipColor);
            ShuttleV3DialogLayout.SafeLabel(
                new Rect(summaryRect.x + 8f, summaryRect.y + 7f, summaryRect.width - 16f, 20f),
                ShuttleUIText.Tr(
                    "CT_Shuttle_HabitatJoyConfig_ModuleSummary",
                    this.configs.Count,
                    this.HasUnsavedChanges()
                        ? ShuttleUIText.Tr("CT_Shuttle_HabitatJoyConfig_Dirty")
                        : ShuttleUIText.Tr("CT_Shuttle_HabitatJoyConfig_Clean")));
        }

        private void DrawBody(Rect rect)
        {
            if (this.configs.Count == 0)
            {
                this.DrawEmptyState(rect);
                return;
            }

            float gap = ShuttleV3DialogStyle.Gap;
            if (this.configs.Count > 1)
            {
                Rect modulesRect = new Rect(rect.x, rect.y, ModuleListWidth, rect.height);
                Rect configRect = new Rect(
                    modulesRect.xMax + gap,
                    rect.y,
                    rect.width - ModuleListWidth - gap,
                    rect.height);
                this.DrawModuleList(modulesRect);
                this.DrawConfigPanel(configRect);
            }
            else
            {
                this.selectedConfigIndex = 0;
                this.DrawConfigPanel(rect);
            }
        }

        private void DrawEmptyState(Rect rect)
        {
            ShuttleV3DialogLayout.DrawPanelBackground(rect);
            ShuttleV3DialogLayout.DrawSectionHeader(
                rect,
                ShuttleUIText.Tr("CT_Shuttle_HabitatJoyConfig_RecreationTypes"));
            GUI.color = ShuttleV3DialogStyle.MutedTextColor;
            ShuttleV3DialogLayout.SafeLabel(
                new Rect(rect.x + 14f, rect.y + 48f, rect.width - 28f, 80f),
                ShuttleUIText.Tr("CT_Shuttle_HabitatJoyConfig_NoConfigurableModulesDetail"));
            GUI.color = Color.white;
        }

        private void DrawModuleList(Rect rect)
        {
            ShuttleV3DialogLayout.DrawPanelBackground(rect);
            ShuttleV3DialogLayout.DrawSectionHeader(
                rect,
                ShuttleUIText.Tr("CT_Shuttle_HabitatJoyConfig_ModuleList"));
            Rect listRect = new Rect(rect.x + 10f, rect.y + 40f, rect.width - 20f, rect.height - 50f);
            Rect viewRect = new Rect(
                0f,
                0f,
                listRect.width - 16f,
                Mathf.Max(listRect.height, this.configs.Count * RowHeight));

            Widgets.BeginScrollView(listRect, ref this.moduleScroll, viewRect);
            for (int i = 0; i < this.configs.Count; i++)
            {
                ShuttleHabitatJoyConfigReadModel config = this.configs[i];
                Rect rowRect = new Rect(0f, i * RowHeight, viewRect.width, RowHeight - 5f);
                bool selected = i == this.selectedConfigIndex;
                ShuttleV3DialogLayout.DrawCardBackground(rowRect, selected, false, ShuttleV3DialogStyle.CardColor);
                Text.Font = GameFont.Small;
                ShuttleV3DialogLayout.SafeLabel(
                    new Rect(rowRect.x + 8f, rowRect.y + 6f, rowRect.width - 16f, 18f),
                    this.FitLabelText(config != null ? config.ModuleLabel : "-", rowRect.width - 16f));
                if (Widgets.ButtonInvisible(rowRect))
                {
                    this.selectedConfigIndex = i;
                    this.joyScroll = Vector2.zero;
                }
            }

            Widgets.EndScrollView();
        }

        private void DrawConfigPanel(Rect rect)
        {
            ShuttleV3DialogLayout.DrawPanelBackground(rect);
            ShuttleV3DialogLayout.DrawSectionHeader(
                rect,
                ShuttleUIText.Tr("CT_Shuttle_HabitatJoyConfig_RecreationTypes"));
            ShuttleHabitatJoyConfigReadModel config = this.GetSelectedConfig();
            if (config == null)
            {
                return;
            }

            HashSet<string> selected = this.GetWorkingSelection(config);
            Rect infoRect = new Rect(rect.x + 12f, rect.y + 42f, rect.width - 24f, 72f);
            this.DrawInfoCard(infoRect, config, selected);

            Rect listRect = new Rect(rect.x + 12f, infoRect.yMax + 10f, rect.width - 24f, rect.yMax - infoRect.yMax - 20f);
            this.DrawJoyKindList(listRect, config, selected);
        }

        private void DrawInfoCard(
            Rect rect,
            ShuttleHabitatJoyConfigReadModel config,
            HashSet<string> selected)
        {
            ShuttleV3DialogLayout.DrawCardBackground(rect, false, false, ShuttleV3DialogStyle.CardColor);
            Text.Font = GameFont.Small;
            ShuttleV3DialogLayout.SafeLabel(
                new Rect(rect.x + 10f, rect.y + 7f, rect.width - 20f, 20f),
                this.FitLabelText(config.ModuleLabel, rect.width - 20f));
            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleV3DialogStyle.MutedTextColor;
            ShuttleV3DialogLayout.SafeLabel(
                new Rect(rect.x + 10f, rect.y + 30f, rect.width - 20f, 16f),
                ShuttleUIText.Tr(
                    "CT_Shuttle_HabitatJoyConfig_SelectedCount",
                    selected.Count,
                    Mathf.Max(0, config.Capacity)));
            ShuttleV3DialogLayout.SafeLabel(
                new Rect(rect.x + 10f, rect.y + 48f, rect.width - 20f, 16f),
                ShuttleUIText.Tr("CT_Shuttle_HabitatJoyConfig_Summary") + ": " +
                    this.FitLabelText(this.BuildSelectionSummary(config, selected), rect.width - 62f));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            TooltipHandler.TipRegion(rect, this.BuildConfigTooltip(config, selected));
        }

        private void DrawJoyKindList(
            Rect listRect,
            ShuttleHabitatJoyConfigReadModel config,
            HashSet<string> selected)
        {
            IReadOnlyList<JoyKindDef> allowed = config.AllowedJoyKinds;
            int rowCount = allowed != null ? allowed.Count : 0;
            if (rowCount == 0)
            {
                this.DrawEmptyState(listRect);
                return;
            }

            Rect viewRect = new Rect(
                0f,
                0f,
                listRect.width - 16f,
                Mathf.Max(listRect.height, rowCount * RowHeight));
            Widgets.BeginScrollView(listRect, ref this.joyScroll, viewRect);
            for (int i = 0; i < rowCount; i++)
            {
                JoyKindDef joyKind = allowed[i];
                if (joyKind == null || string.IsNullOrEmpty(joyKind.defName))
                {
                    continue;
                }

                Rect rowRect = new Rect(0f, i * RowHeight, viewRect.width, RowHeight - 5f);
                this.DrawJoyKindRow(rowRect, config, selected, joyKind);
            }

            Widgets.EndScrollView();
        }

        private void DrawJoyKindRow(
            Rect rect,
            ShuttleHabitatJoyConfigReadModel config,
            HashSet<string> selected,
            JoyKindDef joyKind)
        {
            bool isSelected = selected.Contains(joyKind.defName);
            bool canSelect = isSelected || selected.Count < config.Capacity;
            Color accent = isSelected ? ShuttleV3DialogStyle.GreenStatusColor : ShuttleV3DialogStyle.MutedTextColor;
            ShuttleV3DialogLayout.DrawCardBackground(
                rect,
                isSelected,
                !canSelect,
                new Color(accent.r * 0.14f, accent.g * 0.14f, accent.b * 0.14f, 0.94f));
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, 4f, rect.height), accent);

            bool oldEnabled = GUI.enabled;
            GUI.enabled = canSelect;
            bool newSelected = isSelected;
            Rect checkboxRect = new Rect(rect.x + 10f, rect.y + 5f, rect.width - 20f, 22f);
            Widgets.CheckboxLabeled(checkboxRect, joyKind.LabelCap.ToString(), ref newSelected);
            GUI.enabled = oldEnabled;

            if (newSelected != isSelected)
            {
                if (newSelected)
                {
                    if (selected.Count < config.Capacity)
                    {
                        selected.Add(joyKind.defName);
                    }
                    else
                    {
                        Messages.Message(
                            ShuttleUIText.Tr("CT_Shuttle_HabitatJoyConfig_CapacityReached"),
                            MessageTypeDefOf.RejectInput,
                            false);
                    }
                }
                else
                {
                    selected.Remove(joyKind.defName);
                }
            }

            TooltipHandler.TipRegion(rect, this.BuildJoyKindTooltip(config, joyKind, canSelect));
        }

        private void DrawFooter(Rect rect)
        {
            Rect closeRect = new Rect(rect.xMax - 96f, rect.y + 6f, 96f, 30f);
            Rect resetRect = new Rect(closeRect.x - 104f, closeRect.y, 96f, 30f);
            Rect saveRect = new Rect(resetRect.x - 104f, closeRect.y, 96f, 30f);

            bool dirty = this.HasUnsavedChanges();
            if (ShuttleV3DialogLayout.DrawDialogButton(
                saveRect,
                ShuttleUIText.Tr("CT_Shuttle_HabitatJoyConfig_Save"),
                dirty && this.configs.Count > 0,
                ShuttleV3DialogButtonKind.Primary,
                "CT_Shuttle_HabitatJoy_SaveTooltip".Translate().ToString()))
            {
                this.TrySaveDirtySelections();
            }

            if (ShuttleV3DialogLayout.DrawDialogButton(
                resetRect,
                ShuttleUIText.Tr("CT_Shuttle_HabitatJoyConfig_Reset"),
                dirty && this.configs.Count > 0,
                ShuttleV3DialogButtonKind.Danger,
                "CT_Shuttle_HabitatJoy_ResetTooltip".Translate().ToString()))
            {
                this.ResetWorkingSelections();
            }

            if (ShuttleV3DialogLayout.DrawDialogButton(
                closeRect,
                ShuttleUIText.Tr("CT_Shuttle_UI_Close"),
                true,
                ShuttleV3DialogButtonKind.Normal,
                "CT_Shuttle_HabitatJoy_CloseTooltip".Translate().ToString()))
            {
                if (!this.HasUnsavedChanges() || this.TrySaveDirtySelections())
                {
                    this.Close();
                }
            }
        }

        private bool TrySaveDirtySelections()
        {
            if (this.crewActions == null)
            {
                Messages.Message("CT_Shuttle_Command_ExecutorUnavailable".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }

            bool savedAny = false;
            for (int i = 0; i < this.configs.Count; i++)
            {
                ShuttleHabitatJoyConfigReadModel config = this.configs[i];
                if (config == null || !this.HasSelectionChanged(config))
                {
                    continue;
                }

                HashSet<string> selected = this.GetWorkingSelection(config);
                List<string> orderedDefNames = this.GetOrderedSelectedDefNames(config, selected);
                if (!this.crewActions.CanSaveHabitatJoyKinds(config, orderedDefNames))
                {
                    Messages.Message(
                        this.crewActions.GetHabitatJoySaveTooltip(config, orderedDefNames),
                        MessageTypeDefOf.RejectInput,
                        false);
                    return false;
                }

                if (!this.crewActions.SaveHabitatJoyKinds(config, orderedDefNames))
                {
                    return false;
                }

                this.originalSelections[config.ModuleInstanceID] = new HashSet<string>(selected);
                savedAny = true;
            }

            if (!savedAny)
            {
                Messages.Message(
                    ShuttleUIText.Tr("CT_Shuttle_HabitatJoyConfig_NoChangesToSave"),
                    MessageTypeDefOf.NeutralEvent,
                    false);
            }

            return true;
        }

        private bool HasUnsavedChanges()
        {
            for (int i = 0; i < this.configs.Count; i++)
            {
                if (this.HasSelectionChanged(this.configs[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasSelectionChanged(ShuttleHabitatJoyConfigReadModel config)
        {
            if (config == null || string.IsNullOrEmpty(config.ModuleInstanceID))
            {
                return false;
            }

            HashSet<string> original = this.GetOriginalSelection(config);
            HashSet<string> working = this.GetWorkingSelection(config);
            return !original.SetEquals(working);
        }

        private void ResetWorkingSelections()
        {
            for (int i = 0; i < this.configs.Count; i++)
            {
                ShuttleHabitatJoyConfigReadModel config = this.configs[i];
                if (config == null || string.IsNullOrEmpty(config.ModuleInstanceID))
                {
                    continue;
                }

                this.workingSelections[config.ModuleInstanceID] =
                    new HashSet<string>(this.GetOriginalSelection(config));
            }
        }

        private ShuttleHabitatJoyConfigReadModel GetSelectedConfig()
        {
            if (this.configs.Count == 0)
            {
                return null;
            }

            if (this.selectedConfigIndex < 0)
            {
                this.selectedConfigIndex = 0;
            }
            else if (this.selectedConfigIndex >= this.configs.Count)
            {
                this.selectedConfigIndex = this.configs.Count - 1;
            }

            return this.configs[this.selectedConfigIndex];
        }

        private HashSet<string> GetOriginalSelection(ShuttleHabitatJoyConfigReadModel config)
        {
            HashSet<string> selected;
            if (config == null ||
                string.IsNullOrEmpty(config.ModuleInstanceID) ||
                !this.originalSelections.TryGetValue(config.ModuleInstanceID, out selected))
            {
                selected = new HashSet<string>();
            }

            return selected;
        }

        private HashSet<string> GetWorkingSelection(ShuttleHabitatJoyConfigReadModel config)
        {
            HashSet<string> selected;
            if (config == null ||
                string.IsNullOrEmpty(config.ModuleInstanceID) ||
                !this.workingSelections.TryGetValue(config.ModuleInstanceID, out selected))
            {
                selected = new HashSet<string>();
                if (config != null && !string.IsNullOrEmpty(config.ModuleInstanceID))
                {
                    this.workingSelections[config.ModuleInstanceID] = selected;
                }
            }

            return selected;
        }

        private HashSet<string> ExtractSelected(ShuttleHabitatJoyConfigReadModel config)
        {
            HashSet<string> selected = new HashSet<string>();
            if (config == null || config.SelectedJoyKinds == null)
            {
                return selected;
            }

            for (int i = 0; i < config.SelectedJoyKinds.Count; i++)
            {
                JoyKindDef joyKind = config.SelectedJoyKinds[i];
                if (joyKind != null && !string.IsNullOrEmpty(joyKind.defName))
                {
                    selected.Add(joyKind.defName);
                }
            }

            return selected;
        }

        private HashSet<string> ClampSelectionToCapacity(
            ShuttleHabitatJoyConfigReadModel config,
            HashSet<string> selected)
        {
            HashSet<string> clamped = new HashSet<string>();
            if (config == null || selected == null || config.AllowedJoyKinds == null)
            {
                return clamped;
            }

            int capacity = Mathf.Max(0, config.Capacity);
            for (int i = 0; i < config.AllowedJoyKinds.Count; i++)
            {
                JoyKindDef joyKind = config.AllowedJoyKinds[i];
                if (joyKind == null ||
                    string.IsNullOrEmpty(joyKind.defName) ||
                    !selected.Contains(joyKind.defName))
                {
                    continue;
                }

                clamped.Add(joyKind.defName);
                if (clamped.Count >= capacity)
                {
                    break;
                }
            }

            return clamped;
        }

        private List<string> GetOrderedSelectedDefNames(
            ShuttleHabitatJoyConfigReadModel config,
            HashSet<string> selected)
        {
            List<string> defNames = new List<string>();
            if (config == null || selected == null || config.AllowedJoyKinds == null)
            {
                return defNames;
            }

            for (int i = 0; i < config.AllowedJoyKinds.Count; i++)
            {
                JoyKindDef joyKind = config.AllowedJoyKinds[i];
                if (joyKind != null &&
                    !string.IsNullOrEmpty(joyKind.defName) &&
                    selected.Contains(joyKind.defName))
                {
                    defNames.Add(joyKind.defName);
                }
            }

            return defNames;
        }

        private string BuildSelectionSummary(
            ShuttleHabitatJoyConfigReadModel config,
            HashSet<string> selected)
        {
            List<string> labels = new List<string>();
            if (config == null || selected == null || config.AllowedJoyKinds == null)
            {
                return "-";
            }

            for (int i = 0; i < config.AllowedJoyKinds.Count; i++)
            {
                JoyKindDef joyKind = config.AllowedJoyKinds[i];
                if (joyKind != null &&
                    !string.IsNullOrEmpty(joyKind.defName) &&
                    selected.Contains(joyKind.defName))
                {
                    labels.Add(joyKind.LabelCap.ToString());
                }
            }

            return labels.Count > 0
                ? string.Join(", ", labels.ToArray())
                : ShuttleUIText.Tr("CT_Shuttle_HabitatJoyConfig_NoneSelected");
        }

        private string BuildConfigTooltip(
            ShuttleHabitatJoyConfigReadModel config,
            HashSet<string> selected)
        {
            return ShuttleUIText.Tr("CT_Shuttle_HabitatJoyConfig_Module") + ": " + (config != null ? config.ModuleLabel : "-") + "\n" +
                ShuttleUIText.Tr(
                    "CT_Shuttle_HabitatJoyConfig_SelectedTooltipLine",
                    selected != null ? selected.Count : 0,
                    config != null ? config.Capacity : 0) + "\n" +
                "CT_Shuttle_HabitatJoy_ModuleTooltip".Translate().ToString();
        }

        private string BuildJoyKindTooltip(
            ShuttleHabitatJoyConfigReadModel config,
            JoyKindDef joyKind,
            bool canSelect)
        {
            string text = joyKind != null ? joyKind.LabelCap.ToString() : "-";
            if (!canSelect)
            {
                text += "\n" + ShuttleUIText.Tr("CT_Shuttle_HabitatJoyConfig_CapacityReached");
            }

            text += "\n" + "CT_Shuttle_HabitatJoy_ToggleTooltip".Translate().ToString();
            return text;
        }

        private string FitLabelText(string text, float width)
        {
            return ShuttleUILayout.FitSingleLineLabelText(text, width, string.Empty, 0f);
        }

    }
}
