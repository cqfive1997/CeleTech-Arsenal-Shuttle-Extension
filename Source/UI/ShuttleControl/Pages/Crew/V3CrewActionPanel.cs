using System;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Actions.Crew;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Crew
{
    [StaticConstructorOnStartup]
    internal sealed class V3CrewActionPanel
    {
        private static readonly Texture2D ExpandIcon =
            ContentFinder<Texture2D>.Get("UI/ShuttleControl/Actions/Expand_128", false);

        private readonly V3CrewText text;
        private readonly V3CrewPanelDrawer panelDrawer;

        internal V3CrewActionPanel(V3CrewText text)
        {
            this.text = text;
            this.panelDrawer = new V3CrewPanelDrawer(text);
        }

        internal void DrawCrewOperations(
            Rect rect,
            ShuttleControlReadModel model,
            ShuttlePageDrawContext context,
            Action onUnloadCrew)
        {
            this.panelDrawer.DrawPanelTitle(rect, this.text.Tr("CT_Shuttle_Crew_Operations"));
            Rect inner = this.panelDrawer.GetPanelInnerRect(rect, 38f);
            Rect joyRect = new Rect(inner.x, inner.y, inner.width, 34f);
            Rect unloadRect = new Rect(inner.x, joyRect.yMax + 8f, inner.width, 34f);
            IShuttleCrewJoyUIActions joyActions = GetJoyActions(context);

            bool joyEnabled = joyActions != null && joyActions.CanOpenHabitatJoyConfig(model);
            this.DrawActionButton(
                joyRect,
                this.text.Tr("CT_Shuttle_Habitat_JoyConfig"),
                joyEnabled,
                false,
                joyActions != null ? joyActions.GetHabitatJoyConfigTooltip(model) : this.GetNotWiredTooltip(),
                delegate
                {
                    if (joyActions != null)
                    {
                        joyActions.OpenHabitatJoyConfig(model);
                    }
                });

            bool unloadEnabled = onUnloadCrew != null;
            this.DrawActionButton(
                unloadRect,
                this.text.Tr("CT_Shuttle_Crew_UnloadCrew"),
                unloadEnabled,
                false,
                unloadEnabled
                    ? this.text.Tr("CT_Shuttle_Crew_UnloadCrewTooltip")
                    : this.GetNotWiredTooltip(),
                onUnloadCrew);
        }

        internal bool DrawCrewCardActionButton(Rect rect, string tooltip)
        {
            bool hovered = Mouse.IsOver(rect);
            Color border = hovered ? V3CrewText.BlueColor : V3CrewVisuals.WithAlpha(ShuttleUIStyle.MutedTextColor, 0.55f);
            Widgets.DrawBoxSolid(rect, hovered ? V3CrewText.StrongCardColor : V3CrewText.CardColor);
            ShuttleUILayout.DrawRectBorder(
                rect,
                border,
                hovered ? ShuttleUIStyle.ThickBorder : ShuttleUIStyle.ThinBorder);

            Rect iconRect = new Rect(rect.x + 4f, rect.y + 4f, rect.width - 8f, rect.height - 8f);
            if (ExpandIcon != null)
            {
                GUI.color = hovered ? Color.white : V3CrewVisuals.WithAlpha(Color.white, 0.82f);
                Widgets.DrawTextureFitted(iconRect, ExpandIcon, 1f);
                GUI.color = Color.white;
            }
            else
            {
                Widgets.DrawBoxSolid(
                    new Rect(rect.x + 7f, rect.y + 7f, rect.width - 14f, rect.height - 14f),
                    V3CrewVisuals.WithAlpha(border, 0.72f));
            }

            this.text.AddTooltip(
                rect,
                string.IsNullOrEmpty(tooltip)
                    ? this.text.Tr("CT_ShuttleCrew_ViewDetails")
                    : tooltip);
            return Widgets.ButtonInvisible(rect);
        }

        internal void OpenVanillaInfo(V3CrewCardModel card)
        {
            if (card == null || card.DisplayThing == null || card.DisplayThing.Destroyed)
            {
                ShuttleUICommandFeedback.ShowReject(
                    this.text.Tr("CT_ShuttleCrew_NoVanillaInfoTarget"),
                    false);
                return;
            }

            Find.Selector.ClearSelection();
            Find.Selector.Select(card.DisplayThing);
        }

        internal bool CanRemoveCrewCard(
            V3CrewCardModel card,
            ShuttlePageDrawContext context)
        {
            if (card == null)
            {
                return false;
            }

            if (IsLoadedCrewSource(card.SourceKind))
            {
                IShuttleCrewLoadedCrewUIActions actions = GetLoadedCrewActions(context);
                return actions != null && actions.CanUnloadLoadedCrew(
                    card.TransporterIndex,
                    card.LoadedIndex,
                    card.ThingIDNumber,
                    card.DefName);
            }

            if (card.SourceKind == V3CrewCardSourceKind.Habitat)
            {
                IShuttleCrewHabitatUIActions actions = GetHabitatActions(context);
                return actions != null && actions.CanEjectHabitatOccupant(card.ThingIDNumber);
            }

            if (card.SourceKind == V3CrewCardSourceKind.MedicalBay)
            {
                IShuttleCrewMedicalPatientUIActions actions = GetMedicalPatientActions(context);
                return actions != null && actions.CanEjectMedicalPatient(card.ThingIDNumber);
            }

            if (card.SourceKind == V3CrewCardSourceKind.MechCharger)
            {
                IShuttleCrewMechChargerUIActions actions = GetMechChargerActions(context);
                return actions != null && actions.CanEjectChargingMech(card.ThingIDNumber);
            }

            return false;
        }

        internal void RemoveCrewCard(
            V3CrewCardModel card,
            ShuttlePageDrawContext context)
        {
            if (card == null)
            {
                return;
            }

            if (IsLoadedCrewSource(card.SourceKind))
            {
                IShuttleCrewLoadedCrewUIActions actions = GetLoadedCrewActions(context);
                if (actions != null)
                {
                    actions.UnloadLoadedCrew(
                        card.TransporterIndex,
                        card.LoadedIndex,
                        card.ThingIDNumber,
                        card.DefName);
                }
            }
            else if (card.SourceKind == V3CrewCardSourceKind.Habitat)
            {
                IShuttleCrewHabitatUIActions actions = GetHabitatActions(context);
                if (actions != null)
                {
                    actions.EjectHabitatOccupant(card.ThingIDNumber);
                }
            }
            else if (card.SourceKind == V3CrewCardSourceKind.MedicalBay)
            {
                IShuttleCrewMedicalPatientUIActions actions = GetMedicalPatientActions(context);
                if (actions != null)
                {
                    actions.EjectMedicalPatient(card.ThingIDNumber);
                }
            }
            else if (card.SourceKind == V3CrewCardSourceKind.MechCharger)
            {
                IShuttleCrewMechChargerUIActions actions = GetMechChargerActions(context);
                if (actions != null)
                {
                    actions.EjectChargingMech(card.ThingIDNumber);
                }
            }
        }

        internal string GetCrewRemoveTooltip(
            V3CrewCardModel card,
            ShuttlePageDrawContext context)
        {
            if (card == null)
            {
                return this.text.Tr("CT_ShuttleCrew_MemberSourceIncomplete");
            }

            if (IsLoadedCrewSource(card.SourceKind))
            {
                IShuttleCrewLoadedCrewUIActions actions = GetLoadedCrewActions(context);
                return actions != null
                    ? actions.GetLoadedCrewUnloadTooltip(
                        card.TransporterIndex,
                        card.LoadedIndex,
                        card.ThingIDNumber,
                        card.DefName)
                    : this.GetNotWiredTooltip();
            }

            if (card.SourceKind == V3CrewCardSourceKind.Habitat)
            {
                IShuttleCrewHabitatUIActions actions = GetHabitatActions(context);
                return actions != null
                    ? actions.GetHabitatOccupantEjectTooltip(card.ThingIDNumber)
                    : this.GetNotWiredTooltip();
            }

            if (card.SourceKind == V3CrewCardSourceKind.MedicalBay)
            {
                IShuttleCrewMedicalPatientUIActions actions = GetMedicalPatientActions(context);
                return actions != null
                    ? actions.GetMedicalPatientEjectTooltip(card.ThingIDNumber)
                    : this.GetNotWiredTooltip();
            }

            if (card.SourceKind == V3CrewCardSourceKind.MechCharger)
            {
                IShuttleCrewMechChargerUIActions actions = GetMechChargerActions(context);
                return actions != null
                    ? actions.GetChargingMechEjectTooltip(card.ThingIDNumber)
                    : this.GetNotWiredTooltip();
            }

            if (card.SourceKind == V3CrewCardSourceKind.PrisonCell)
            {
                return this.text.Tr("CT_ShuttleCrew_PrisonCellRemoveFromPrisonCellPage");
            }

            return this.text.Tr("CT_ShuttleCrew_NoRemovableRecord");
        }

        private void DrawActionButton(
            Rect rect,
            string label,
            bool enabled,
            bool danger,
            string tooltip,
            Action onClick)
        {
            if (!this.panelDrawer.DrawButton(rect, label, enabled, danger, tooltip))
            {
                return;
            }

            if (enabled && onClick != null)
            {
                onClick();
            }
            else
            {
                ShuttleUICommandFeedback.ShowReject(
                    string.IsNullOrEmpty(tooltip) ? this.GetNotWiredTooltip() : tooltip,
                    false);
            }
        }

        private static IShuttleCrewLoadedCrewUIActions GetLoadedCrewActions(
            ShuttlePageDrawContext context)
        {
            if (context != null && context.CrewPageContext != null)
            {
                return context.CrewPageContext.LoadedCrewActions;
            }

            return null;
        }

        private static IShuttleCrewHabitatUIActions GetHabitatActions(
            ShuttlePageDrawContext context)
        {
            if (context != null && context.CrewPageContext != null)
            {
                return context.CrewPageContext.HabitatActions;
            }

            return null;
        }

        private static IShuttleCrewJoyUIActions GetJoyActions(
            ShuttlePageDrawContext context)
        {
            if (context != null && context.CrewPageContext != null)
            {
                return context.CrewPageContext.JoyActions;
            }

            return null;
        }

        private static IShuttleCrewMedicalPatientUIActions GetMedicalPatientActions(
            ShuttlePageDrawContext context)
        {
            if (context != null && context.CrewPageContext != null)
            {
                return context.CrewPageContext.MedicalPatientActions;
            }

            return null;
        }

        private static IShuttleCrewMechChargerUIActions GetMechChargerActions(
            ShuttlePageDrawContext context)
        {
            if (context != null && context.CrewPageContext != null)
            {
                return context.CrewPageContext.MechChargerActions;
            }

            return null;
        }

        private string GetNotWiredTooltip()
        {
            return this.text.Tr("CT_Shuttle_UI_ActionUnavailableYet");
        }

        private static bool IsLoadedCrewSource(V3CrewCardSourceKind sourceKind)
        {
            return sourceKind == V3CrewCardSourceKind.Cockpit ||
                sourceKind == V3CrewCardSourceKind.CargoLoaded;
        }
    }
}
