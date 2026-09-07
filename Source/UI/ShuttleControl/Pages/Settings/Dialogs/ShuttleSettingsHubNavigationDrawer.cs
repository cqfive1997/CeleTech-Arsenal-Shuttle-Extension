using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared.Dialogs;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Settings.Dialogs
{
    internal sealed class ShuttleSettingsHubNavigationDrawer
    {
        internal ShuttleSettingsHubCategory Draw(
            Rect rect,
            IList<ShuttleSettingsHubSectionEntry> sections,
            ShuttleSettingsHubCategory selectedCategory)
        {
            ShuttleV3DialogLayout.DrawPanelBackground(rect);
            Rect inner = new Rect(rect.x + 10f, rect.y + 10f, rect.width - 20f, rect.height - 20f);
            float y = inner.y;
            this.DrawGroupLabel(
                new Rect(inner.x, y, inner.width, 24f),
                "CT_Shuttle_SettingsHub_GlobalGroup".Translate().ToString());
            y += 30f;
            y = this.DrawButton(inner, y, sections, ShuttleSettingsHubCategory.Gameplay, ref selectedCategory);
            y = this.DrawButton(inner, y, sections, ShuttleSettingsHubCategory.Performance, ref selectedCategory);
            y = this.DrawButton(inner, y, sections, ShuttleSettingsHubCategory.Combat, ref selectedCategory);
            y += 12f;
            this.DrawGroupLabel(
                new Rect(inner.x, y, inner.width, 24f),
                "CT_Shuttle_SettingsHub_ShuttleGroup".Translate().ToString());
            y += 30f;
            y = this.DrawButton(inner, y, sections, ShuttleSettingsHubCategory.Paint, ref selectedCategory);
            y = this.DrawButton(inner, y, sections, ShuttleSettingsHubCategory.ShuttleDefense, ref selectedCategory);
            this.DrawButton(inner, y, sections, ShuttleSettingsHubCategory.PrisonSupply, ref selectedCategory);
            return selectedCategory;
        }

        private float DrawButton(
            Rect inner,
            float y,
            IList<ShuttleSettingsHubSectionEntry> sections,
            ShuttleSettingsHubCategory category,
            ref ShuttleSettingsHubCategory selectedCategory)
        {
            ShuttleSettingsHubSectionEntry entry = FindEntry(sections, category);
            if (entry == null)
            {
                return y;
            }

            Rect buttonRect = new Rect(inner.x, y, inner.width, 42f);
            bool selected = selectedCategory == category;
            if (ShuttleV3DialogLayout.DrawTintedButton(
                buttonRect,
                entry.LabelKey.Translate().ToString(),
                true,
                selected
                    ? ShuttleV3DialogStyle.SelectedColor
                    : ShuttleV3DialogStyle.ButtonBgColor,
                selected
                    ? ShuttleV3DialogStyle.BlueStatusColor
                    : ShuttleV3DialogStyle.ButtonBorderColor,
                ShuttleV3DialogStyle.ButtonTextColor,
                entry.DescriptionKey.Translate().ToString()))
            {
                selectedCategory = category;
            }

            return buttonRect.yMax + 8f;
        }

        private void DrawGroupLabel(Rect rect, string label)
        {
            GameFont oldFont = Text.Font;
            Color oldColor = GUI.color;
            try
            {
                Text.Font = GameFont.Tiny;
                GUI.color = ShuttleV3DialogStyle.MutedTextColor;
                ShuttleV3DialogLayout.SafeLabel(rect, label);
            }
            finally
            {
                Text.Font = oldFont;
                GUI.color = oldColor;
            }
        }

        private static ShuttleSettingsHubSectionEntry FindEntry(
            IList<ShuttleSettingsHubSectionEntry> sections,
            ShuttleSettingsHubCategory category)
        {
            if (sections == null)
            {
                return null;
            }

            for (int i = 0; i < sections.Count; i++)
            {
                if (sections[i].Category == category)
                {
                    return sections[i];
                }
            }

            return null;
        }
    }
}
