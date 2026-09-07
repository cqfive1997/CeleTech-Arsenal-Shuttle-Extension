using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Defense
{
    /// <summary>
    /// Binds weapon cards to the detached assembly slot models required by module commands.
    /// </summary>
    internal sealed class V3DefenseWeaponModuleLookup
    {
        internal void Bind(
            List<V3DefenseWeaponEntryModel> weapons,
            ShuttleControlReadModel controlModel)
        {
            if (weapons == null ||
                weapons.Count == 0 ||
                controlModel == null ||
                controlModel.SegmentSlots == null)
            {
                return;
            }

            this.BindExactInstanceMatches(weapons, controlModel.SegmentSlots);
            this.BindUniqueSlotFallbacks(weapons, controlModel.SegmentSlots);
        }

        private void BindExactInstanceMatches(
            List<V3DefenseWeaponEntryModel> weapons,
            List<ShuttleControlSegmentSlotModel> segments)
        {
            for (int segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
            {
                ShuttleControlSegmentSlotModel segment = segments[segmentIndex];
                if (segment == null || segment.ModuleSlots == null)
                {
                    continue;
                }

                for (int moduleIndex = 0; moduleIndex < segment.ModuleSlots.Count; moduleIndex++)
                {
                    ShuttleControlModuleSlotModel module = segment.ModuleSlots[moduleIndex];
                    if (module == null || string.IsNullOrEmpty(module.InstalledModuleInstanceID))
                    {
                        continue;
                    }

                    for (int weaponIndex = 0; weaponIndex < weapons.Count; weaponIndex++)
                    {
                        V3DefenseWeaponEntryModel weapon = weapons[weaponIndex];
                        if (weapon != null &&
                            weapon.ModuleSlot == null &&
                            weapon.ModuleInstanceID == module.InstalledModuleInstanceID)
                        {
                            this.Bind(weapon, segment, module);
                        }
                    }
                }
            }
        }

        private void BindUniqueSlotFallbacks(
            List<V3DefenseWeaponEntryModel> weapons,
            List<ShuttleControlSegmentSlotModel> segments)
        {
            for (int weaponIndex = 0; weaponIndex < weapons.Count; weaponIndex++)
            {
                V3DefenseWeaponEntryModel weapon = weapons[weaponIndex];
                if (weapon == null || weapon.ModuleSlot != null)
                {
                    continue;
                }

                ShuttleControlSegmentSlotModel matchedSegment = null;
                ShuttleControlModuleSlotModel matchedModule = null;
                int matchCount = 0;
                for (int segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
                {
                    ShuttleControlSegmentSlotModel segment = segments[segmentIndex];
                    if (segment == null || segment.ModuleSlots == null)
                    {
                        continue;
                    }

                    for (int moduleIndex = 0; moduleIndex < segment.ModuleSlots.Count; moduleIndex++)
                    {
                        ShuttleControlModuleSlotModel module = segment.ModuleSlots[moduleIndex];
                        if (!this.IsFallbackMatch(weapon, module))
                        {
                            continue;
                        }

                        matchedSegment = segment;
                        matchedModule = module;
                        matchCount++;
                    }
                }

                if (matchCount == 1)
                {
                    this.Bind(weapon, matchedSegment, matchedModule);
                }
            }
        }

        private bool IsFallbackMatch(
            V3DefenseWeaponEntryModel weapon,
            ShuttleControlModuleSlotModel module)
        {
            return weapon != null &&
                module != null &&
                !string.IsNullOrEmpty(weapon.SlotID) &&
                weapon.SlotID == module.SlotID &&
                weapon.ModuleDefName == module.InstalledModuleDefName;
        }

        private void Bind(
            V3DefenseWeaponEntryModel weapon,
            ShuttleControlSegmentSlotModel segment,
            ShuttleControlModuleSlotModel module)
        {
            weapon.SegmentSlot = segment;
            weapon.ModuleSlot = module;
        }
    }
}
