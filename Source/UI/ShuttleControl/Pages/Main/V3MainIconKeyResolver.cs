using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Main
{
    internal sealed class V3MainIconKeyResolver
    {
        internal string GetSegmentSpecificIconKey(ShuttleControlSegmentSlotModel segment)
        {
            if (segment == null)
            {
                return null;
            }

            if (IsSegmentInstalled(segment) &&
                !string.IsNullOrEmpty(segment.InstalledSegmentDefName))
            {
                return segment.InstalledSegmentDefName;
            }

            return !string.IsNullOrEmpty(segment.DefaultSegmentDefName)
                ? segment.DefaultSegmentDefName
                : null;
        }

        internal string GetSegmentTypeIconKey(ShuttleControlSegmentSlotModel segment)
        {
            if (IsEmptyOptionalSegmentSlot(segment))
            {
                return "segment_slot_option";
            }

            ShuttleSegmentType type = ShuttleSegmentType.Unknown;
            if (segment != null)
            {
                type = ShuttleSegmentTypeCatalog.ParseSegmentType(segment.SegmentTypeID);
                if (type == ShuttleSegmentType.Unknown)
                {
                    type = ShuttleSegmentTypeCatalog.ParseSlotType(segment.SlotTypeID);
                }
            }

            switch (type)
            {
                case ShuttleSegmentType.Cockpit:
                    return "segment_slot_cockpit";
                case ShuttleSegmentType.Power:
                    return "segment_slot_power";
                case ShuttleSegmentType.Living:
                    return "segment_slot_living";
                case ShuttleSegmentType.Cargo:
                    return "segment_slot_cargo";
                case ShuttleSegmentType.Support:
                    return "segment_slot_support";
                case ShuttleSegmentType.Weapon:
                    return "segment_slot_weapon";
            }

            return this.GetSegmentSlotIconKeyFromText(JoinLower(
                segment != null ? segment.SegmentTypeID : null,
                segment != null ? segment.SlotTypeID : null,
                segment != null ? segment.InstalledSegmentDefName : null,
                segment != null ? segment.DefaultSegmentDefName : null,
                segment != null ? segment.InstalledSegmentLabel : null,
                segment != null ? segment.DefaultSegmentLabel : null));
        }

        internal string GetModuleSpecificIconKey(ShuttleControlModuleSlotModel moduleSlot)
        {
            return moduleSlot != null &&
                IsModuleInstalled(moduleSlot) &&
                !string.IsNullOrEmpty(moduleSlot.InstalledModuleDefName)
                    ? moduleSlot.InstalledModuleDefName
                    : null;
        }

        internal string GetModuleTypeIconKey(ShuttleControlModuleSlotModel moduleSlot)
        {
            if (moduleSlot == null)
            {
                return "module_option";
            }

            if (IsModuleInstalled(moduleSlot))
            {
                string byType = this.GetModuleTypeIconKeyByType(
                    ShuttleModuleTypeCatalog.ParseModuleType(
                        moduleSlot.InstalledModuleTypeID));
                if (!string.IsNullOrEmpty(byType))
                {
                    return byType;
                }

                return this.GetInstalledModuleIconKeyFromText(JoinLower(
                    moduleSlot.InstalledModuleDefName,
                    moduleSlot.InstalledModuleLabel,
                    moduleSlot.InstalledModuleTypeID));
            }

            string emptyByType = this.GetModuleTypeIconKeyByType(
                ShuttleModuleTypeCatalog.ParseSlotType(moduleSlot.SlotTypeID));
            if (!string.IsNullOrEmpty(emptyByType))
            {
                return emptyByType;
            }

            return this.GetEmptyModuleSlotIconKeyFromText(JoinLower(
                moduleSlot.SlotTypeID,
                moduleSlot.SlotID));
        }

        internal string GetCandidateIconKey(V3MainInstallCandidateModel candidate, bool segmentCandidate)
        {
            if (candidate != null && !string.IsNullOrEmpty(candidate.Id))
            {
                return candidate.Id;
            }

            return segmentCandidate ? "segment_slot_option" : "module_option";
        }

        internal string GetSegmentFallbackGlyph(ShuttleControlSegmentSlotModel segment, string label)
        {
            string key = this.GetSegmentKindKey(segment);
            if (!string.IsNullOrEmpty(key))
            {
                return key.Substring(0, 1).ToUpperInvariant();
            }

            return string.IsNullOrEmpty(label)
                ? "S"
                : label.Substring(0, 1).ToUpperInvariant();
        }

        internal string GetModuleFallbackGlyph(ShuttleControlModuleSlotModel moduleSlot, bool installed)
        {
            if (!installed)
            {
                return "+";
            }

            string key = this.GetModuleTypeIconKey(moduleSlot);
            if (!string.IsNullOrEmpty(key) && key.StartsWith("module_"))
            {
                string trimmed = key.Substring("module_".Length);
                return !string.IsNullOrEmpty(trimmed)
                    ? trimmed.Substring(0, 1).ToUpperInvariant()
                    : "M";
            }

            return "M";
        }

        private string GetModuleTypeIconKeyByType(ShuttleModuleType type)
        {
            switch (type)
            {
                case ShuttleModuleType.Cargo:
                    return "module_cargo";
                case ShuttleModuleType.Reactor:
                    return "module_compact_fission_reactor";
                case ShuttleModuleType.Battery:
                    return "module_composite_battery_bank";
                case ShuttleModuleType.PowerRegulator:
                    return "module_modular_energy_storage";
                case ShuttleModuleType.Cockpit:
                    return "module_cockpit";
                case ShuttleModuleType.Navigation:
                    return "module_navigation";
                case ShuttleModuleType.Support:
                case ShuttleModuleType.Scanner:
                    return "module_option";
                case ShuttleModuleType.Habitat:
                    return "habitat";
                case ShuttleModuleType.Shield:
                    return "module_area_shield";
                case ShuttleModuleType.Weapon:
                    return "module_weapon_40mm_close_in";
                case ShuttleModuleType.MedicalBay:
                    return "module_medical_bay_i";
                case ShuttleModuleType.Production:
                    return "module_workbench_auto";
                case ShuttleModuleType.MechCharger:
                    return "module_mech_charger_i";
                case ShuttleModuleType.Armor:
                    return "module_hull_armor";
                case ShuttleModuleType.FireControl:
                    return "fire_control_radar";
                case ShuttleModuleType.PrisonCell:
                    return "module_prison_cell";
                case ShuttleModuleType.AmmoLoader:
                    return "module_ammo_loader";
            }

            return null;
        }

        private string GetSegmentKindKey(ShuttleControlSegmentSlotModel segment)
        {
            if (IsEmptyOptionalSegmentSlot(segment))
            {
                return "optional";
            }

            return this.GetSegmentKindKeyFromText(JoinLower(
                segment != null ? segment.SegmentTypeID : null,
                segment != null ? segment.SlotTypeID : null,
                segment != null ? segment.InstalledSegmentDefName : null,
                segment != null ? segment.DefaultSegmentDefName : null));
        }

        private string GetSegmentSlotIconKeyFromText(string text)
        {
            string kind = this.GetSegmentKindKeyFromText(text);
            switch (kind)
            {
                case "cockpit":
                    return "segment_slot_cockpit";
                case "power":
                    return "segment_slot_power";
                case "support":
                    return "segment_slot_support";
                case "weapon":
                    return "segment_slot_weapon";
                case "cargo":
                    return "segment_slot_cargo";
                case "living":
                    return "segment_slot_living";
                default:
                    return "segment_slot_option";
            }
        }

        private string GetSegmentKindKeyFromText(string text)
        {
            string safeText = text ?? string.Empty;
            if (safeText.Contains("cockpit") || safeText.Contains("command"))
            {
                return "cockpit";
            }

            if (safeText.Contains("power") ||
                safeText.Contains("reactor") ||
                safeText.Contains("battery"))
            {
                return "power";
            }

            if (safeText.Contains("support") || safeText.Contains("utility"))
            {
                return "support";
            }

            if (safeText.Contains("weapon"))
            {
                return "weapon";
            }

            if (safeText.Contains("cargo") || safeText.Contains("freight"))
            {
                return "cargo";
            }

            if (safeText.Contains("living") ||
                safeText.Contains("habitat") ||
                safeText.Contains("medical"))
            {
                return "living";
            }

            return "optional";
        }

        private string GetInstalledModuleIconKeyFromText(string text)
        {
            string safeText = text ?? string.Empty;
            if (ContainsAny(safeText, "ammo loader", "ammoloader", "ammo_loader", "ammo-loader"))
            {
                return "module_ammo_loader";
            }

            if (ContainsAny(safeText, "cargo logistics", "cargologisticscore", "cargo_logistics", "cargo loader", "cargo_loader", "latch"))
            {
                return "module_cargo_logistics_core";
            }

            if (ContainsAny(safeText, "refriger", "cold"))
            {
                return "module_refrigerated_cargo";
            }

            if (safeText.Contains("cargo"))
            {
                return "module_cargo";
            }

            if (ContainsAny(safeText, "shuttlecontroler", "shuttle controller", "controller", "main control", "main_control", "drive control", "drivecontrol"))
            {
                return "module_shuttle_controller";
            }

            if (ContainsAny(safeText, "navigation", " nav", "_nav"))
            {
                return "module_navigation";
            }

            if (safeText.Contains("cockpit"))
            {
                return "module_cockpit";
            }

            if (ContainsAny(safeText, "hullplating", "hull plating", "hull_plating", "hull-plating", "armor", "armour", "plating"))
            {
                return "module_hull_armor";
            }

            if (ContainsAny(safeText, "firecontrol", "fire control", "fire_control", "fire-control", "radar"))
            {
                return "fire_control_radar";
            }

            if (ContainsAny(safeText, "prisoncell", "prison cell", "prison_cell", "prison-cell", "brig"))
            {
                return "module_prison_cell";
            }

            if (safeText.Contains("habitat") && HasTier(safeText, "iv", "4"))
            {
                return "module_habitat_iv";
            }

            if (safeText.Contains("habitat") && HasTier(safeText, "iii", "3"))
            {
                return "module_habitat_iii";
            }

            if (safeText.Contains("habitat") && HasTier(safeText, "ii", "2"))
            {
                return "module_habitat_ii";
            }

            if (safeText.Contains("habitat"))
            {
                return "module_habitat_i";
            }

            if (safeText.Contains("recreation") && HasTier(safeText, "iv", "4"))
            {
                return "module_recreation_iv";
            }

            if (safeText.Contains("recreation") && HasTier(safeText, "iii", "3"))
            {
                return "module_recreation_iii";
            }

            if (safeText.Contains("recreation") && HasTier(safeText, "ii", "2"))
            {
                return "module_recreation_ii";
            }

            if (safeText.Contains("recreation"))
            {
                return "module_recreation_i";
            }

            if (ContainsAny(safeText, "zero", "zeropoint", "zero point", "vacuum"))
            {
                return "module_zero_point_vacuum_reactor";
            }

            if (ContainsAny(safeText, "fusion", "fusionpulse"))
            {
                return "module_fusion_pulse_reactor";
            }

            if (ContainsAny(safeText, "compact", "fission", "compactfission"))
            {
                return "module_compact_fission_reactor";
            }

            if (ContainsAny(safeText, "high-density", "high density", "highdensity"))
            {
                return "module_high_density_battery_bank";
            }

            if (ContainsAny(safeText, "modular energy", "modularenergy", "energy storage", "energystorage"))
            {
                return "module_modular_energy_storage";
            }

            if (ContainsAny(safeText, "composite", "battery"))
            {
                return "module_composite_battery_bank";
            }

            if (IsSurfaceShieldText(safeText) && HasTier(safeText, "iv", "4"))
            {
                return "module_surface_shield_iv";
            }

            if (IsSurfaceShieldText(safeText) && HasTier(safeText, "iii", "3"))
            {
                return "module_surface_shield_iii";
            }

            if (IsSurfaceShieldText(safeText) && HasTier(safeText, "ii", "2"))
            {
                return "module_surface_shield_ii";
            }

            if (IsSurfaceShieldText(safeText))
            {
                return "module_surface_shield_i";
            }

            if (ContainsAny(safeText, "area shield", "areashield", "area") ||
                safeText.Contains("shield"))
            {
                return "module_area_shield";
            }

            if (ContainsAny(safeText, "land", "stabilizer", "stablizer"))
            {
                return "module_landing_stabilizer";
            }

            if (safeText.Contains("mech") && HasTier(safeText, "iv", "4"))
            {
                return "module_mech_charger_iv";
            }

            if (safeText.Contains("mech") && HasTier(safeText, "iii", "3"))
            {
                return "module_mech_charger_iii";
            }

            if (safeText.Contains("mech") && HasTier(safeText, "ii", "2"))
            {
                return "module_mech_charger_ii";
            }

            if (safeText.Contains("mech"))
            {
                return "module_mech_charger_i";
            }

            if (safeText.Contains("medical") && HasTier(safeText, "iv", "4"))
            {
                return "module_medical_bay_iv";
            }

            if (safeText.Contains("medical") && HasTier(safeText, "iii", "3"))
            {
                return "module_medical_bay_iii";
            }

            if (safeText.Contains("medical") && HasTier(safeText, "ii", "2"))
            {
                return "module_medical_bay_ii";
            }

            if (safeText.Contains("medical"))
            {
                return "module_medical_bay_i";
            }

            if (ContainsAny(safeText, "70", "rocket", "missile"))
            {
                return "weapons";
            }

            if (ContainsAny(safeText, "6mm", "point"))
            {
                return "module_weapon_6mm_point";
            }

            if (ContainsAny(safeText, "40mm", "close", "ciws", "weapon"))
            {
                return "module_weapon_40mm_close_in";
            }

            if (ContainsAny(safeText, "kitchen", "cook", "meal", "butcher"))
            {
                return "module_auto_kitchen";
            }

            if (ContainsAny(safeText, "workbench", "work table", "work_table", "production", "autobench"))
            {
                return "module_workbench_auto";
            }

            return "module_option";
        }

        private string GetEmptyModuleSlotIconKeyFromText(string text)
        {
            string safeText = text ?? string.Empty;
            if (safeText.Contains("cargo"))
            {
                return "module_cargo";
            }

            if (safeText.Contains("cockpit"))
            {
                return "module_cockpit";
            }

            if (ContainsAny(safeText, "navigation", " nav", "_nav"))
            {
                return "module_navigation";
            }

            if (safeText.Contains("reactor"))
            {
                return "module_compact_fission_reactor";
            }

            if (safeText.Contains("battery"))
            {
                return "module_composite_battery_bank";
            }

            if (safeText.Contains("shield"))
            {
                return "module_area_shield";
            }

            if (ContainsAny(safeText, "armor", "armour", "hull", "plating"))
            {
                return "module_hull_armor";
            }

            if (ContainsAny(safeText, "fire-control", "fire_control", "fire control", "firecontrol", "radar"))
            {
                return "fire_control_radar";
            }

            if (ContainsAny(safeText, "ammo-loader", "ammo_loader", "ammo loader", "ammoloader"))
            {
                return "module_ammo_loader";
            }

            if (ContainsAny(safeText, "prison-cell", "prison_cell", "prison cell", "prisoncell", "brig"))
            {
                return "module_prison_cell";
            }

            if (safeText.Contains("weapon"))
            {
                return "module_weapon_40mm_close_in";
            }

            if (safeText.Contains("medical"))
            {
                return "module_medical_bay_i";
            }

            if (ContainsAny(safeText, "production", "workbench", "work_table", "work table"))
            {
                return "module_workbench_auto";
            }

            if (safeText.Contains("mech"))
            {
                return "module_mech_charger_i";
            }

            if (safeText.Contains("support"))
            {
                return "module_landing_stabilizer";
            }

            return "module_option";
        }

        private static bool IsSegmentInstalled(ShuttleControlSegmentSlotModel segment)
        {
            return segment != null && !string.IsNullOrEmpty(segment.InstalledSegmentInstanceID);
        }

        private static bool IsEmptyOptionalSegmentSlot(ShuttleControlSegmentSlotModel segment)
        {
            return segment != null &&
                !IsSegmentInstalled(segment) &&
                !segment.IsRequired &&
                !segment.IsFixed &&
                !segment.IsLocked;
        }

        private static bool IsModuleInstalled(ShuttleControlModuleSlotModel moduleSlot)
        {
            return moduleSlot != null && !string.IsNullOrEmpty(moduleSlot.InstalledModuleInstanceID);
        }

        private static bool IsSurfaceShieldText(string text)
        {
            return ContainsAny(text, "surface shield", "surfaceshield", "surface_shield");
        }

        private static bool HasTier(string text, string roman, string number)
        {
            string safeText = text ?? string.Empty;
            return safeText.Contains("_" + roman) ||
                safeText.Contains(" " + roman) ||
                safeText.Contains("-" + roman) ||
                safeText.Contains("mk" + number) ||
                safeText.Contains("_" + number) ||
                safeText.Contains(" " + number) ||
                safeText.Contains("-" + number);
        }

        private static bool ContainsAny(string text, params string[] needles)
        {
            string safeText = text ?? string.Empty;
            if (needles == null)
            {
                return false;
            }

            for (int i = 0; i < needles.Length; i++)
            {
                if (!string.IsNullOrEmpty(needles[i]) &&
                    safeText.Contains(needles[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static string JoinLower(params string[] values)
        {
            if (values == null || values.Length == 0)
            {
                return string.Empty;
            }

            string result = string.Empty;
            for (int i = 0; i < values.Length; i++)
            {
                if (!string.IsNullOrEmpty(values[i]))
                {
                    result += " " + values[i].ToLowerInvariant();
                }
            }

            return result;
        }
    }
}
