using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Settings;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared.Dialogs;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Settings.Dialogs
{
    /// <summary>
    /// Unified settings chrome. Category editors retain ownership of their
    /// own working copies and command/settings commit boundaries.
    /// </summary>
    internal sealed class Dialog_ShuttleSettingsHubV3 : Window
    {
        private const float HeaderHeight = 66f;
        private const float FooterHeight = 50f;
        private const float NavigationWidth = 210f;
        private const float CategoryHeaderHeight = 62f;

        private readonly List<ShuttleSettingsHubSectionEntry> sections =
            new List<ShuttleSettingsHubSectionEntry>();
        private readonly ShuttleSettingsHubNavigationDrawer navigationDrawer =
            new ShuttleSettingsHubNavigationDrawer();
        private readonly ShuttleSettingsHubFooterDrawer footerDrawer =
            new ShuttleSettingsHubFooterDrawer();
        private ShuttleSettingsHubCategory selectedCategory =
            ShuttleSettingsHubCategory.Gameplay;

        internal Dialog_ShuttleSettingsHubV3(
            IShuttleSettingsUIActions actions,
            ShuttleControlReadModel controlModel,
            ShuttleWeaponBayReadModel weaponBayModel,
            IShuttleCommandExecutor commandExecutor,
            Rot4 previewRotation)
        {
            ShuttlePaintSchemeSnapshot paintScheme =
                controlModel != null && controlModel.PaintScheme != null
                    ? controlModel.PaintScheme
                    : ShuttlePaintSchemeSnapshot.Default;
            ShuttlePrisonCellReadModel prisonCell = controlModel != null
                ? controlModel.PrisonCell
                : null;

            this.sections.Add(new ShuttleSettingsHubSectionEntry(
                ShuttleSettingsHubCategory.Gameplay,
                "CT_Shuttle_OtherSettings_GameplaySection",
                "CT_Shuttle_OtherSettings_Subtitle",
                new Dialog_ShuttleOtherSettingsV3(delegate
                {
                    Notify(actions, ShuttleSettingsChangeKind.Other);
                })));
            this.sections.Add(new ShuttleSettingsHubSectionEntry(
                ShuttleSettingsHubCategory.Performance,
                "CT_Shuttle_PerformanceSettings_Title",
                "CT_Shuttle_PerformanceSettings_Subtitle",
                new Dialog_ShuttlePerformanceSettingsV3(delegate
                {
                    Notify(actions, ShuttleSettingsChangeKind.VisualPerformance);
                })));
            this.sections.Add(new ShuttleSettingsHubSectionEntry(
                ShuttleSettingsHubCategory.Combat,
                "CT_Shuttle_CombatTuning_Title",
                "CT_Shuttle_CombatTuning_Subtitle",
                new Dialog_ShuttleCombatTuningSettingsV3(delegate
                {
                    Notify(actions, ShuttleSettingsChangeKind.CombatTuning);
                })));
            this.sections.Add(new ShuttleSettingsHubSectionEntry(
                ShuttleSettingsHubCategory.Paint,
                "CT_Shuttle_Paint_Title",
                "CT_Shuttle_Paint_Subtitle",
                new Dialog_ShuttlePaintSettingsV3(
                    paintScheme,
                    commandExecutor,
                    previewRotation,
                    delegate
                    {
                        Notify(actions, ShuttleSettingsChangeKind.Paint);
                    })));
            this.sections.Add(new ShuttleSettingsHubSectionEntry(
                ShuttleSettingsHubCategory.ShuttleDefense,
                "CT_Shuttle_ShuttleDefenseSettings_Title",
                "CT_Shuttle_ShuttleDefenseSettings_Subtitle",
                new ShuttleDefenseSettingsSection(
                    controlModel,
                    weaponBayModel,
                    commandExecutor,
                    delegate
                    {
                        Notify(actions, ShuttleSettingsChangeKind.Defense);
                    })));
            this.sections.Add(new ShuttleSettingsHubSectionEntry(
                ShuttleSettingsHubCategory.PrisonSupply,
                "CT_Shuttle_PrisonSupply_Title",
                "CT_Shuttle_PrisonSupply_Subtitle",
                new Dialog_ShuttlePrisonSupplySettingsV3(
                    prisonCell,
                    commandExecutor,
                    delegate
                    {
                        Notify(actions, ShuttleSettingsChangeKind.PrisonSupply);
                    })));

            this.forcePause = false;
            this.doCloseX = true;
            this.absorbInputAroundWindow = true;
            this.draggable = true;
            this.resizeable = false;
        }

        public override Vector2 InitialSize
        {
            get { return new Vector2(1120f, 720f); }
        }

        public override void DoWindowContents(Rect inRect)
        {
            ShuttleV3DialogLayout.DrawPanelBackground(inRect);
            Rect headerRect = new Rect(inRect.x, inRect.y, inRect.width, HeaderHeight);
            Rect footerRect = new Rect(
                inRect.x,
                inRect.yMax - FooterHeight,
                inRect.width,
                FooterHeight);
            Rect bodyRect = new Rect(
                inRect.x,
                headerRect.yMax + ShuttleV3DialogStyle.Gap,
                inRect.width,
                Mathf.Max(
                    0f,
                    footerRect.y - headerRect.yMax - (ShuttleV3DialogStyle.Gap * 2f)));
            Rect navigationRect = new Rect(
                bodyRect.x,
                bodyRect.y,
                NavigationWidth,
                bodyRect.height);
            Rect contentRect = new Rect(
                navigationRect.xMax + ShuttleV3DialogStyle.Gap,
                bodyRect.y,
                Mathf.Max(0f, bodyRect.width - NavigationWidth - ShuttleV3DialogStyle.Gap),
                bodyRect.height);

            this.DrawHeader(headerRect);
            this.selectedCategory = this.navigationDrawer.Draw(
                navigationRect,
                this.sections,
                this.selectedCategory);
            this.DrawSelectedSection(contentRect);
            ShuttleSettingsHubSectionEntry selected =
                this.FindEntry(this.selectedCategory);
            if (this.footerDrawer.Draw(
                footerRect,
                selected != null ? selected.Section : null))
            {
                this.Close(false);
            }
        }

        public override void PostClose()
        {
            for (int i = 0; i < this.sections.Count; i++)
            {
                IShuttleSettingsHubSection section = this.sections[i].Section;
                if (section != null)
                {
                    section.OnHubClosed();
                }
            }

            base.PostClose();
        }

        private void DrawHeader(Rect rect)
        {
            GameFont oldFont = Text.Font;
            Color oldColor = GUI.color;
            try
            {
                Text.Font = GameFont.Medium;
                GUI.color = ShuttleV3DialogStyle.HeaderTitleTextColor;
                ShuttleV3DialogLayout.SafeLabel(
                    new Rect(rect.x + 12f, rect.y + 5f, rect.width - 24f, 30f),
                    "CT_Shuttle_SettingsHub_Title".Translate().ToString());
                Text.Font = GameFont.Tiny;
                GUI.color = ShuttleV3DialogStyle.MutedTextColor;
                ShuttleV3DialogLayout.SafeLabel(
                    new Rect(rect.x + 12f, rect.y + 37f, rect.width - 24f, 22f),
                    "CT_Shuttle_SettingsHub_Subtitle".Translate().ToString());
            }
            finally
            {
                Text.Font = oldFont;
                GUI.color = oldColor;
            }
        }

        private void DrawSelectedSection(Rect rect)
        {
            ShuttleSettingsHubSectionEntry entry = this.FindEntry(this.selectedCategory);
            if (entry == null || entry.Section == null)
            {
                return;
            }

            ShuttleV3DialogLayout.DrawPanelBackground(rect);
            Rect headerRect = new Rect(
                rect.x + 12f,
                rect.y + 6f,
                rect.width - 24f,
                CategoryHeaderHeight - 8f);
            this.DrawCategoryHeader(headerRect, entry);
            Rect sectionRect = new Rect(
                rect.x + 10f,
                rect.y + CategoryHeaderHeight,
                rect.width - 20f,
                Mathf.Max(0f, rect.height - CategoryHeaderHeight - 10f));
            entry.Section.Draw(sectionRect);
        }

        private void DrawCategoryHeader(
            Rect rect,
            ShuttleSettingsHubSectionEntry entry)
        {
            GameFont oldFont = Text.Font;
            Color oldColor = GUI.color;
            try
            {
                Text.Font = GameFont.Small;
                GUI.color = ShuttleV3DialogStyle.HeaderTitleTextColor;
                ShuttleV3DialogLayout.SafeLabel(
                    new Rect(rect.x, rect.y, rect.width, 24f),
                    entry.LabelKey.Translate().ToString());
                Text.Font = GameFont.Tiny;
                GUI.color = ShuttleV3DialogStyle.MutedTextColor;
                ShuttleV3DialogLayout.SafeLabel(
                    new Rect(rect.x, rect.y + 27f, rect.width, 22f),
                    entry.DescriptionKey.Translate().ToString());
            }
            finally
            {
                Text.Font = oldFont;
                GUI.color = oldColor;
            }
        }

        private ShuttleSettingsHubSectionEntry FindEntry(
            ShuttleSettingsHubCategory category)
        {
            for (int i = 0; i < this.sections.Count; i++)
            {
                if (this.sections[i].Category == category)
                {
                    return this.sections[i];
                }
            }

            return null;
        }

        private static void Notify(
            IShuttleSettingsUIActions actions,
            ShuttleSettingsChangeKind kind)
        {
            if (actions != null)
            {
                actions.NotifySettingsChanged(kind);
            }
        }
    }
}
