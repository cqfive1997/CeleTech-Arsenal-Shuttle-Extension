using CeleTech.ShuttleExtension.ModularShuttle.Presentation.External;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.ExternalModules
{
    internal sealed class V3ExternalModulesText
    {
        internal static readonly Color CardColor =
            ShuttleUIStyle.CardColor;
        internal static readonly Color StrongCardColor =
            ShuttleUIStyle.SelectedColor;
        internal static readonly Color MutedCardColor =
            ShuttleUIStyle.ActionRowBgColor;
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

        internal string FormatOne(float value)
        {
            return value.ToString("0.#");
        }

        internal string FormatMs(float value)
        {
            return value.ToString("0.###");
        }

        internal string FormatRuntimeLabel(string runtimeSystemKey)
        {
            return ExternalRuntimeMetadataResolver.ResolveRuntimeLabel(runtimeSystemKey);
        }

        internal string GetCategoryLabel(ExternalModuleUICategory category)
        {
            switch (category)
            {
                case ExternalModuleUICategory.ControlAndSensing:
                    return this.Tr("CT_Shuttle_ExternalRuntime_Category_Control");
                case ExternalModuleUICategory.DefenseAndTactical:
                    return this.Tr("CT_Shuttle_ExternalRuntime_Category_Defense");
                case ExternalModuleUICategory.ProductionAndSupport:
                    return this.Tr("CT_Shuttle_ExternalRuntime_Category_Support");
                default:
                    return this.Tr("CT_Shuttle_ExternalRuntime_Category_Other");
            }
        }

        internal Color GetStatusColor(ExternalModuleUIReadModel model)
        {
            if (model == null)
            {
                return ShuttleUIStyle.MutedTextColor;
            }

            if (!model.RuntimeRegistered || !model.RuntimeStateExists)
            {
                return YellowColor;
            }

            if (!model.RuntimeStateEnvelopeValid)
            {
                return RedColor;
            }

            return model.RuntimeEnabled ? GreenColor : ShuttleUIStyle.MutedTextColor;
        }

        internal string FitLabelText(string value, float width)
        {
            return ShuttleUILayout.FitSingleLineLabelText(value, width);
        }

        internal string GetModuleSpecificIconKey(ExternalModuleUIReadModel model)
        {
            return model != null && !string.IsNullOrEmpty(model.ModuleDefName)
                ? model.ModuleDefName
                : null;
        }

        internal string GetFallbackIconKey(ExternalModuleUIReadModel model)
        {
            if (model == null)
            {
                return "module_option";
            }

            if (model.ModuleTypeID == "scanner")
            {
                return "scanner_module";
            }

            if (model.ModuleTypeID == "navigation")
            {
                return "module_navigation";
            }

            if (model.ModuleTypeID == "shield")
            {
                return "shield_module";
            }

            if (model.ModuleTypeID == "cargo")
            {
                return "module_cargo";
            }

            if (model.ModuleTypeID == "medical-bay")
            {
                return "medical_bay";
            }

            if (model.ModuleTypeID == "mech-charger")
            {
                return this.GetMechChargerIconKey(model.ModuleDefName);
            }

            if (model.ModuleTypeID == "armor")
            {
                return "module_hull_armor";
            }

            if (model.ModuleTypeID == "fire-control")
            {
                return "fire_control_radar";
            }

            if (model.ModuleTypeID == "ammo-loader")
            {
                return "module_ammo_loader";
            }

            if (model.ModuleTypeID == "prison-cell")
            {
                return "module_prison_cell";
            }

            if (model.ModuleTypeID == "production")
            {
                return this.IsAutoKitchen(model.ModuleDefName)
                    ? "module_auto_kitchen"
                    : "workbench_module";
            }

            return "module_option";
        }

        internal void AddTooltip(Rect rect, string tooltip)
        {
            ShuttleUITooltip.Tip(rect, tooltip);
        }

        private string GetMechChargerIconKey(string moduleDefName)
        {
            string text = (moduleDefName ?? string.Empty).ToLowerInvariant();
            if (text.Contains("_iv") || text.Contains(" 4") || text.Contains("-4"))
            {
                return "module_mech_charger_iv";
            }

            if (text.Contains("_iii") || text.Contains(" 3") || text.Contains("-3"))
            {
                return "module_mech_charger_iii";
            }

            if (text.Contains("_ii") || text.Contains(" 2") || text.Contains("-2"))
            {
                return "module_mech_charger_ii";
            }

            return "module_mech_charger_i";
        }

        private bool IsAutoKitchen(string moduleDefName)
        {
            string text = (moduleDefName ?? string.Empty).ToLowerInvariant();
            return text.Contains("autokitchen") || text.Contains("auto_kitchen");
        }
    }
}
