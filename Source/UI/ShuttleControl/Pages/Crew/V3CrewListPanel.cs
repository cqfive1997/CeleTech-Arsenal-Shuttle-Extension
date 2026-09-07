using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Crew
{
    internal sealed class V3CrewListPanel
    {
        private const float CrewCardHeight = 126f;
        private const float CompactCrewCardHeight = 104f;
        private const float PanelContentTopOffset = 44f;

        private readonly V3CrewText text;
        private readonly V3CrewPanelDrawer panelDrawer;
        private readonly V3CrewCardDrawer cardDrawer;

        internal V3CrewListPanel(V3CrewText text)
        {
            this.text = text;
            this.panelDrawer = new V3CrewPanelDrawer(text);
            this.cardDrawer = new V3CrewCardDrawer(text, this.panelDrawer);
        }

        internal void DrawGroup(
            Rect rect,
            string title,
            List<V3CrewCardModel> cards,
            V3CrewGroupKind group,
            ref Vector2 scroll,
            bool compact,
            V3CrewPageState state,
            Action<Rect, V3CrewCardModel> drawActionButton)
        {
            this.panelDrawer.DrawPanelTitle(rect, title);
            Rect listRect = this.panelDrawer.GetPanelInnerRect(rect, PanelContentTopOffset);
            float rowHeight = compact ? CompactCrewCardHeight : CrewCardHeight;
            int rowCount = cards != null && cards.Count > 0 ? cards.Count : 1;
            Rect viewRect = new Rect(
                0f,
                0f,
                Mathf.Max(0f, listRect.width - 16f),
                Mathf.Max(listRect.height, rowCount * rowHeight));
            Widgets.BeginScrollView(listRect, ref scroll, viewRect);
            try
            {
                if (cards == null || cards.Count == 0)
                {
                    this.DrawEmptyCrewCard(
                        new Rect(0f, 0f, viewRect.width, rowHeight - 6f),
                        group,
                        compact);
                    return;
                }

                for (int i = 0; i < cards.Count; i++)
                {
                    Rect rowRect = new Rect(0f, i * rowHeight, viewRect.width, rowHeight - 6f);
                    this.cardDrawer.DrawCrewCard(rowRect, cards[i], compact, state, drawActionButton);
                }
            }
            finally
            {
                Widgets.EndScrollView();
            }
        }

        private void DrawEmptyCrewCard(Rect rect, V3CrewGroupKind group, bool compact)
        {
            this.panelDrawer.DrawCardBackground(rect, false, true, V3CrewText.DisabledCardColor);
            Rect iconRect = new Rect(rect.x + 10f, rect.y + 12f, compact ? 34f : 42f, compact ? 34f : 42f);
            this.text.DrawThingIcon(iconRect, null, "-", null);
            Text.Font = GameFont.Small;
            GUI.color = ShuttleUIStyle.MutedTextColor;
            ShuttleUILayout.SafeLabel(
                new Rect(iconRect.xMax + 10f, rect.y + 12f, rect.width - iconRect.width - 24f, 24f),
                this.text.Tr("CT_Shuttle_Crew_NoMembers"));
            Text.Font = GameFont.Tiny;
            ShuttleUILayout.SafeLabel(
                new Rect(iconRect.xMax + 10f, rect.y + 40f, rect.width - iconRect.width - 24f, 24f),
                this.GetEmptyGroupText(group));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private string GetEmptyGroupText(V3CrewGroupKind group)
        {
            if (group == V3CrewGroupKind.Human)
            {
                return this.text.Tr("CT_ShuttleCrew_NoMembersHuman");
            }

            if (group == V3CrewGroupKind.Mech)
            {
                return this.text.Tr("CT_ShuttleCrew_NoMechanoids");
            }

            if (group == V3CrewGroupKind.Animal)
            {
                return this.text.Tr("CT_ShuttleCrew_NoAnimals");
            }

            return this.text.Tr("CT_ShuttleCrew_NoEntities");
        }
    }
}
