using System;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Cargo
{
    internal sealed class V3CargoText
    {
        internal static readonly Color CardColor =
            new Color(0.044f, 0.056f, 0.070f, 0.88f);
        internal static readonly Color StrongCardColor =
            new Color(0.062f, 0.080f, 0.102f, 0.92f);
        internal static readonly Color MutedCardColor =
            new Color(0.030f, 0.036f, 0.044f, 0.78f);
        internal static readonly Color AccentColor =
            new Color(0.24f, 0.62f, 0.78f, 0.84f);
        internal static readonly Color ColdColor =
            new Color(0.30f, 0.70f, 0.92f, 1f);
        internal static readonly Color GreenColor =
            new Color(0.35f, 0.74f, 0.45f, 1f);
        internal static readonly Color YellowColor =
            new Color(0.95f, 0.72f, 0.28f, 1f);
        internal static readonly Color RedColor =
            new Color(0.90f, 0.32f, 0.26f, 1f);

        internal string Tr(string key)
        {
            return ShuttleUIText.Tr(key);
        }

        internal string Tr(string key, object arg0)
        {
            return ShuttleUIText.Tr(key, arg0);
        }

        internal string Tr(string key, object arg0, object arg1)
        {
            return ShuttleUIText.Tr(key, arg0, arg1);
        }

        internal string ValueOrDash(string value)
        {
            return ShuttleAssemblyDisplayTextResolver.ResolveSafeDisplayText(value);
        }

        internal string FitLabelText(string text, float width)
        {
            return ShuttleUILayout.FitSingleLineLabelText(text, width);
        }

        internal string FormatStackCount(int count)
        {
            return ShuttleUIText.Tr("CT_Shuttle_Cargo_StackCountFormat", Mathf.Max(0, count));
        }

        internal string FormatStackMass(int count, float massKg)
        {
            return ShuttleUIText.Tr(
                "CT_Shuttle_Cargo_StackMassFormat",
                Mathf.Max(0, count),
                Mathf.Max(0f, massKg).ToString("0.#"));
        }

        internal string FormatKgPair(float currentKg, float maxKg)
        {
            return ShuttleUIMetricFormatter.FormatKgPair(
                Mathf.Max(0f, currentKg),
                Mathf.Max(0f, maxKg));
        }

        internal string GetCategoryLabel(V3CargoCategory category, string fallback)
        {
            string labelKey = GetCategoryLabelKey(category);
            string label = !string.IsNullOrEmpty(fallback)
                ? fallback
                : this.Tr(labelKey);
            if (!string.IsNullOrEmpty(label) && label != labelKey)
            {
                return label;
            }

            ShuttleCargoCategoryUIDef def = GetCategoryDef(category);
            if (def != null)
            {
                string defLabel =
                    ShuttleAssemblyDisplayTextResolver.ResolveDefDisplayName(
                        def,
                        null);
                if (!string.IsNullOrEmpty(defLabel) && defLabel != "-")
                {
                    return defLabel;
                }
            }

            return string.IsNullOrEmpty(label) ? "-" : label;
        }

        internal string GetCategoryCardLabel(V3CargoCategory category, string fallback, float width)
        {
            string label = this.GetCategoryLabel(category, fallback);
            return this.FitLabelText(label, width);
        }

        internal int GetCategoryOrder(V3CargoCategory category)
        {
            ShuttleCargoCategoryUIDef def = GetCategoryDef(category);
            return def != null ? def.order : (int)category * 10;
        }

        internal Color GetCategoryColor(V3CargoCategory category)
        {
            ShuttleCargoCategoryUIDef def = GetCategoryDef(category);
            if (def != null)
            {
                return def.color;
            }

            if (category == V3CargoCategory.Food)
            {
                return new Color(0.43f, 0.74f, 0.35f, 1f);
            }

            if (category == V3CargoCategory.Weapons)
            {
                return new Color(0.86f, 0.42f, 0.34f, 1f);
            }

            if (category == V3CargoCategory.Apparel)
            {
                return new Color(0.72f, 0.52f, 0.88f, 1f);
            }

            if (category == V3CargoCategory.RawResources ||
                category == V3CargoCategory.Chunks)
            {
                return new Color(0.84f, 0.68f, 0.36f, 1f);
            }

            if (category == V3CargoCategory.Plants)
            {
                return new Color(0.48f, 0.70f, 0.42f, 1f);
            }

            if (category == V3CargoCategory.Corpses)
            {
                return new Color(0.64f, 0.62f, 0.62f, 1f);
            }

            return AccentColor;
        }

        private static ShuttleCargoCategoryUIDef GetCategoryDef(V3CargoCategory category)
        {
            if (category == V3CargoCategory.None)
            {
                return null;
            }

            return DefDatabase<ShuttleCargoCategoryUIDef>.GetNamedSilentFail(category.ToString());
        }

        internal Color GetLogisticsColor(string statusKey)
        {
            if (statusKey == ShuttleUIText.StatusOnline ||
                statusKey == "Connected")
            {
                return GreenColor;
            }

            if (statusKey == ShuttleUIText.StatusDisabled ||
                statusKey == "LogisticsDisabled" ||
                statusKey == ShuttleUIText.StatusOffline)
            {
                return RedColor;
            }

            if (statusKey == ShuttleUIText.StatusUnpowered ||
                statusKey == "NeedsLogistics" ||
                statusKey == "NeedsItemTransfer" ||
                statusKey == "NeedsItemConsumption" ||
                statusKey == "NeedsItemDeposit" ||
                statusKey == "Pending")
            {
                return YellowColor;
            }

            return ShuttleUIStyle.MutedTextColor;
        }

        internal string GetSourceLabel(V3CargoStackSourceKind sourceKind)
        {
            if (sourceKind == V3CargoStackSourceKind.RefrigeratedCargo)
            {
                return this.Tr("CT_Shuttle_Cargo_SourceRefrigerated");
            }

            if (sourceKind == V3CargoStackSourceKind.LoadedCargo)
            {
                return this.Tr("CT_Shuttle_Cargo_SourceLoaded");
            }

            return this.Tr("CT_Shuttle_Cargo_Unknown");
        }

        internal string GetStackFallbackText(V3CargoStackCardModel stack)
        {
            if (stack == null)
            {
                return "?";
            }

            if (stack.Category == V3CargoCategory.Food)
            {
                return "F";
            }

            if (stack.Category == V3CargoCategory.Weapons)
            {
                return "W";
            }

            if (stack.Category == V3CargoCategory.Apparel)
            {
                return "A";
            }

            if (stack.Category == V3CargoCategory.Buildings)
            {
                return "B";
            }

            return "I";
        }

        internal void AddTooltip(Rect rect, string tooltip)
        {
            ShuttleUITooltip.Tip(rect, tooltip);
        }

        internal void AddTooltip(Rect rect, Func<string> tooltipFactory)
        {
            ShuttleUITooltip.Tip(rect, tooltipFactory);
        }

        private static string GetCategoryLabelKey(V3CargoCategory category)
        {
            if (category == V3CargoCategory.Food)
            {
                return "CT_Shuttle_Cargo_Category_Food";
            }

            if (category == V3CargoCategory.Manufactured)
            {
                return "CT_Shuttle_Cargo_Category_Manufactured";
            }

            if (category == V3CargoCategory.RawResources)
            {
                return "CT_Shuttle_Cargo_Category_RawResources";
            }

            if (category == V3CargoCategory.Weapons)
            {
                return "CT_Shuttle_Cargo_Category_Weapons";
            }

            if (category == V3CargoCategory.Apparel)
            {
                return "CT_Shuttle_Cargo_Category_Apparel";
            }

            if (category == V3CargoCategory.Buildings)
            {
                return "CT_Shuttle_Cargo_Category_Buildings";
            }

            if (category == V3CargoCategory.Chunks)
            {
                return "CT_Shuttle_Cargo_Category_Chunks";
            }

            if (category == V3CargoCategory.Plants)
            {
                return "CT_Shuttle_Cargo_Category_Plants";
            }

            if (category == V3CargoCategory.Corpses)
            {
                return "CT_Shuttle_Cargo_Category_Corpses";
            }

            return "CT_Shuttle_Cargo_Category_Items";
        }
    }
}
