using System;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Processing
{
    internal sealed class V3ProcessingText
    {
        internal static readonly Color CardColor =
            ShuttleUIStyle.CardColor;
        internal static readonly Color StrongCardColor =
            ShuttleUIStyle.SelectedColor;
        internal static readonly Color MutedCardColor =
            ShuttleUIStyle.MutedCardColor;
        internal static readonly Color AccentColor =
            ShuttleUIStyle.BlueStatusColor;
        internal static readonly Color GreenColor =
            ShuttleUIStyle.GreenStatusColor;
        internal static readonly Color YellowColor =
            ShuttleUIStyle.YellowStatusColor;
        internal static readonly Color RedColor =
            ShuttleUIStyle.RedStatusColor;
        internal static readonly Color BlueColor =
            ShuttleUIStyle.BlueStatusColor;

        internal string Tr(string key)
        {
            return V3ProcessingDisplayTextResolver.Tr(key);
        }

        internal string ValueOrDash(string value)
        {
            return V3ProcessingDisplayTextResolver.ResolveSafeDisplayText(value);
        }

        internal string FitLabelText(string text, float width)
        {
            return ShuttleUILayout.FitSingleLineLabelText(text, width);
        }

        internal string CurrentRecipeSummary(V3ProcessingWorkbenchModel workbench)
        {
            return V3ProcessingDisplayTextResolver.ResolveCurrentRecipeSummary(workbench);
        }

        internal string FormatWatts(float watts)
        {
            return ShuttleUIMetricFormatter.FormatWattsOrUnavailable(
                watts,
                this.Tr("CT_Shuttle_Processing_Unavailable"));
        }

        internal Color GetSeverityColor(string severityKey)
        {
            string key = !string.IsNullOrEmpty(severityKey) ? severityKey : string.Empty;
            if (key == "Critical" || key == "Error")
            {
                return RedColor;
            }

            if (key == "Moderate" || key == "Warning")
            {
                return YellowColor;
            }

            if (key == "Stable" || key == "Normal")
            {
                return GreenColor;
            }

            if (key == "Missing" || key == "Pending" || key == "Unknown")
            {
                return ShuttleUIStyle.MutedTextColor;
            }

            return BlueColor;
        }

        internal Color GetStatusColor(string statusKey)
        {
            if (statusKey == "Working" ||
                statusKey == "Queued" ||
                statusKey == "Completed" ||
                statusKey == "TargetSatisfied")
            {
                return GreenColor;
            }

            if (statusKey == "Paused" ||
                statusKey == "WaitingIngredients")
            {
                return YellowColor;
            }

            if (statusKey == "WaitingCargo" ||
                statusKey == "Blocked")
            {
                return RedColor;
            }

            return ShuttleUIStyle.MutedTextColor;
        }

        internal Color GetWorkbenchStatusColor(string statusKey)
        {
            if (statusKey == "Normal")
            {
                return GreenColor;
            }

            if (statusKey == "MissingIngredients" ||
                statusKey == "NoBills" ||
                statusKey == "Disabled" ||
                statusKey == "Pending" ||
                statusKey == "Paused")
            {
                return YellowColor;
            }

            if (statusKey == "Unpowered" ||
                statusKey == "OutputBlocked" ||
                statusKey == "IngredientSystemUnavailable" ||
                statusKey == "Error")
            {
                return RedColor;
            }

            return ShuttleUIStyle.MutedTextColor;
        }

        internal Color GetRecipeAvailabilityColor(string availabilityKey)
        {
            if (availabilityKey == "Available")
            {
                return GreenColor;
            }

            if (availabilityKey == "ResearchLocked" ||
                availabilityKey == "MissingIngredients" ||
                availabilityKey == "Unsupported")
            {
                return YellowColor;
            }

            return ShuttleUIStyle.MutedTextColor;
        }

        internal Color GetRecipeIngredientColor(V3ProcessingRecipeModel recipe)
        {
            if (recipe == null)
            {
                return ShuttleUIStyle.MutedTextColor;
            }

            if (recipe.IngredientStatusKey == "Ready")
            {
                return GreenColor;
            }

            if (recipe.IngredientStatusKey == "Missing" ||
                recipe.IngredientStatusKey == "InventoryUnavailable")
            {
                return YellowColor;
            }

            return ShuttleUIStyle.MutedTextColor;
        }

        internal void AddTooltip(Rect rect, string tooltip)
        {
            ShuttleUITooltip.Tip(rect, tooltip);
        }
    }
}
