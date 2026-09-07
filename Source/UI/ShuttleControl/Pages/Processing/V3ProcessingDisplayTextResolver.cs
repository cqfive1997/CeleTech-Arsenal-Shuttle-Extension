using System;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Processing
{
    internal static class V3ProcessingDisplayTextResolver
    {
        private const string CargoInventoryUnavailableText = "Cargo inventory unavailable";

        internal static string Tr(string key)
        {
            return ShuttleUIText.Tr(key);
        }

        internal static string Tr(string key, object arg0)
        {
            return ShuttleUIText.Tr(key, arg0);
        }

        internal static string Tr(string key, object arg0, object arg1)
        {
            return ShuttleUIText.Tr(key, arg0, arg1);
        }

        internal static string ResolveSafeDisplayText(string value)
        {
            return ShuttleAssemblyDisplayTextResolver.ResolveSafeDisplayText(
                ResolveRuntimeText(value));
        }

        internal static string ResolveRuntimeText(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "-";
            }

            if (string.Equals(
                value,
                CargoInventoryUnavailableText,
                StringComparison.OrdinalIgnoreCase))
            {
                return Tr("CT_Shuttle_Processing_CargoInventoryUnavailable");
            }

            if (value.StartsWith("CT_Shuttle_", StringComparison.Ordinal) &&
                Translator.CanTranslate(value))
            {
                return Tr(value);
            }

            return value;
        }

        internal static string ResolveAutoWorkTableModuleLabel(
            string moduleDefName,
            string moduleLabel)
        {
            ShuttleAutoWorkTableModuleDef moduleDef =
                GetAutoWorkTableModuleDef(moduleDefName);
            string defLabel = ResolveDefDisplayNameOrNull(moduleDef, moduleDefName);
            if (!string.IsNullOrEmpty(defLabel))
            {
                return defLabel;
            }

            return ResolveSafeDisplayText(
                !string.IsNullOrEmpty(moduleLabel) ? moduleLabel : moduleDefName);
        }

        internal static string ResolveSourceBenchLabel(
            string sourceBenchDefName,
            string sourceBenchLabel)
        {
            ThingDef sourceBenchDef = GetThingDef(sourceBenchDefName);
            string defLabel = ResolveDefDisplayNameOrNull(sourceBenchDef, sourceBenchDefName);
            if (!string.IsNullOrEmpty(defLabel))
            {
                return defLabel;
            }

            return ResolveSafeDisplayText(
                !string.IsNullOrEmpty(sourceBenchLabel)
                    ? sourceBenchLabel
                    : sourceBenchDefName);
        }

        internal static string ResolveRecipeLabel(
            string recipeDefName,
            string recipeLabel)
        {
            RecipeDef recipeDef = GetRecipeDef(recipeDefName);
            string defLabel = ResolveDefDisplayNameOrNull(recipeDef, recipeDefName);
            if (!string.IsNullOrEmpty(defLabel))
            {
                return defLabel;
            }

            return ResolveSafeDisplayText(
                !string.IsNullOrEmpty(recipeLabel) ? recipeLabel : recipeDefName);
        }

        internal static string ResolveThingLabel(
            string thingDefName,
            string thingLabel)
        {
            ThingDef thingDef = GetThingDef(thingDefName);
            string defLabel = ResolveDefDisplayNameOrNull(thingDef, thingDefName);
            if (!string.IsNullOrEmpty(defLabel))
            {
                return defLabel;
            }

            return ResolveSafeDisplayText(
                !string.IsNullOrEmpty(thingLabel) ? thingLabel : thingDefName);
        }

        internal static string ResolveCurrentRecipeLabel(
            string selectedRecipeDefName,
            string activeRecipeDefName)
        {
            string recipeDefName = !string.IsNullOrEmpty(selectedRecipeDefName)
                ? selectedRecipeDefName
                : activeRecipeDefName;
            return ResolveRecipeLabel(recipeDefName, null);
        }

        internal static string ResolveCurrentRecipeSummary(
            V3ProcessingWorkbenchModel workbench)
        {
            if (workbench == null ||
                string.IsNullOrEmpty(workbench.CurrentRecipeLabel) ||
                workbench.CurrentRecipeLabel == "-")
            {
                return Tr("CT_Shuttle_Processing_None");
            }

            return ResolveSafeDisplayText(workbench.CurrentRecipeLabel);
        }

        internal static string ResolveWorkbenchKindLabel(string key)
        {
            if (key == "Kitchen")
            {
                return Tr("CT_Shuttle_AutoWorkTable_Kind_Kitchen");
            }

            if (key == "Machining")
            {
                return Tr("CT_Shuttle_Processing_Kind_MachiningUnavailable");
            }

            if (key == "DrugLab")
            {
                return Tr("CT_Shuttle_Processing_Kind_DrugLabUnavailable");
            }

            if (key == "Research")
            {
                return Tr("CT_Shuttle_Processing_Kind_ResearchUnavailable");
            }

            return Tr("CT_Shuttle_Processing_Kind_AutoWorktable");
        }

        internal static string ResolveProductionModeLabel(string key)
        {
            if (key == "RepeatCount")
            {
                return Tr("CT_Shuttle_AutoWorkTable_Mode_RepeatCount");
            }

            if (key == "RepeatForever")
            {
                return Tr("CT_Shuttle_AutoWorkTable_Mode_RepeatForever");
            }

            if (key == "DoUntilStock")
            {
                return Tr("CT_Shuttle_AutoWorkTable_Mode_DoUntilStock");
            }

            return Tr("CT_Shuttle_AutoWorkTable_Mode_OneShot");
        }

        internal static string ResolveWorkbenchStatusLabel(string key)
        {
            if (key == "Disabled")
            {
                return Tr("CT_Shuttle_Processing_Status_Disabled");
            }

            if (key == "Unpowered")
            {
                return Tr("CT_Shuttle_Processing_Status_Unpowered");
            }

            if (key == "OutputBlocked")
            {
                return Tr("CT_Shuttle_Processing_Status_OutputBlocked");
            }

            if (key == "MissingIngredients")
            {
                return Tr("CT_Shuttle_Processing_Status_MissingIngredients");
            }

            if (key == "IngredientSystemUnavailable")
            {
                return Tr("CT_Shuttle_AutoWorkTable_IngredientSystemUnavailable");
            }

            if (key == "Paused")
            {
                return Tr("CT_Shuttle_AutoWorkTable_Paused");
            }

            if (key == "NoBills")
            {
                return Tr("CT_Shuttle_Processing_Status_NoCurrentRecipe");
            }

            if (key == "Normal")
            {
                return Tr("CT_Shuttle_Processing_Status_Normal");
            }

            if (key == "Error")
            {
                return Tr("CT_Shuttle_Processing_Status_Error");
            }

            return Tr("CT_Shuttle_Processing_Unavailable");
        }

        internal static string ResolvePolicyStatusLabel(string key)
        {
            if (key == "Completed")
            {
                return Tr("CT_Shuttle_AutoWorkTable_Completed");
            }

            if (key == "TargetSatisfied")
            {
                return Tr("CT_Shuttle_AutoWorkTable_TargetSatisfied");
            }

            if (key == "WaitingForIngredients")
            {
                return Tr("CT_Shuttle_AutoWorkTable_WaitingForIngredients");
            }

            if (key == "WaitingForCargo")
            {
                return Tr("CT_Shuttle_AutoWorkTable_IngredientSystemUnavailable");
            }

            if (key == "Paused")
            {
                return Tr("CT_Shuttle_AutoWorkTable_Paused");
            }

            if (key == "Working")
            {
                return Tr("CT_Shuttle_Processing_Status_Working");
            }

            if (key == "Disabled")
            {
                return Tr("CT_Shuttle_Processing_Status_Disabled");
            }

            if (key == "Blocked")
            {
                return Tr("CT_Shuttle_Processing_Status_Blocked");
            }

            if (key == "Ready")
            {
                return Tr("CT_Shuttle_Processing_Status_Standby");
            }

            return Tr("CT_Shuttle_Processing_Unavailable");
        }

        internal static string ResolveBillStatusLabel(string key)
        {
            if (key == "Working")
            {
                return Tr("CT_Shuttle_Processing_Status_Working");
            }

            if (key == "WaitingIngredients")
            {
                return Tr("CT_Shuttle_Processing_Status_WaitingIngredients");
            }

            if (key == "WaitingCargo")
            {
                return Tr("CT_Shuttle_AutoWorkTable_IngredientSystemUnavailable");
            }

            if (key == "Paused")
            {
                return Tr("CT_Shuttle_AutoWorkTable_Paused");
            }

            if (key == "Blocked")
            {
                return Tr("CT_Shuttle_Processing_Status_Blocked");
            }

            if (key == "Completed")
            {
                return Tr("CT_Shuttle_AutoWorkTable_Completed");
            }

            if (key == "TargetSatisfied")
            {
                return Tr("CT_Shuttle_AutoWorkTable_TargetSatisfied");
            }

            if (key == "Queued")
            {
                return Tr("CT_Shuttle_Processing_Status_Queued");
            }

            return Tr("CT_Shuttle_Processing_Unavailable");
        }

        internal static string ResolveBillIngredientStatusLabel(
            string key,
            string ingredientStatusSummary,
            string missingIngredientSummary)
        {
            if (key == "Missing")
            {
                return !string.IsNullOrEmpty(missingIngredientSummary)
                    ? ResolveRuntimeText(missingIngredientSummary)
                    : Tr("CT_Shuttle_Processing_Ingredient_Missing");
            }

            if (key == "Ready")
            {
                return !string.IsNullOrEmpty(ingredientStatusSummary)
                    ? ResolveRuntimeText(ingredientStatusSummary)
                    : Tr("CT_Shuttle_Processing_Ingredient_Ready");
            }

            if (key == "SystemUnavailable")
            {
                return !string.IsNullOrEmpty(ingredientStatusSummary)
                    ? ResolveRuntimeText(ingredientStatusSummary)
                    : Tr("CT_Shuttle_AutoWorkTable_IngredientSystemUnavailable");
            }

            return Tr("CT_Shuttle_Processing_Unavailable");
        }

        internal static string ResolveRecipeAvailabilityLabel(string key)
        {
            if (key == "Available")
            {
                return Tr("CT_Shuttle_Processing_Availability_Selectable");
            }

            if (key == "ResearchLocked")
            {
                return Tr("CT_Shuttle_Processing_Availability_ResearchLocked");
            }

            if (key == "MissingIngredients")
            {
                return Tr("CT_Shuttle_Processing_Status_MissingIngredients");
            }

            if (key == "Unsupported")
            {
                return Tr("CT_Shuttle_Processing_Availability_WorkbenchUnsupported");
            }

            return Tr("CT_Shuttle_Processing_Unavailable");
        }

        private static ShuttleAutoWorkTableModuleDef GetAutoWorkTableModuleDef(
            string moduleDefName)
        {
            return string.IsNullOrEmpty(moduleDefName)
                ? null
                : DefDatabase<ShuttleAutoWorkTableModuleDef>.GetNamedSilentFail(
                    moduleDefName);
        }

        private static RecipeDef GetRecipeDef(string recipeDefName)
        {
            return string.IsNullOrEmpty(recipeDefName)
                ? null
                : DefDatabase<RecipeDef>.GetNamedSilentFail(recipeDefName);
        }

        private static ThingDef GetThingDef(string thingDefName)
        {
            return string.IsNullOrEmpty(thingDefName)
                ? null
                : DefDatabase<ThingDef>.GetNamedSilentFail(thingDefName);
        }

        private static string ResolveDefDisplayNameOrNull(Def def, string fallback)
        {
            if (def == null)
            {
                return null;
            }

            string label = ShuttleAssemblyDisplayTextResolver.ResolveDefDisplayName(
                def,
                fallback);
            if (string.IsNullOrEmpty(label) ||
                label == "-" ||
                label == def.defName ||
                label == fallback)
            {
                return null;
            }

            return label;
        }
    }
}
