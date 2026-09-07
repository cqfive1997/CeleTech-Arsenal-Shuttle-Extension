using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Defense
{
    /// <summary>
    /// Projects typed ammunition-loader presence and per-segment enabled availability.
    /// </summary>
    internal sealed class V3DefenseAutoLoaderProjection
    {
        internal bool HasAnyInstalled(ShuttleControlReadModel controlModel)
        {
            if (controlModel == null || controlModel.SegmentSlots == null)
            {
                return false;
            }

            for (int i = 0; i < controlModel.SegmentSlots.Count; i++)
            {
                if (this.SegmentHasLoader(controlModel.SegmentSlots[i], false))
                {
                    return true;
                }
            }

            return false;
        }

        internal void ApplyWeaponAvailability(V3DefensePageReadModel model)
        {
            if (model == null || model.Weapons == null)
            {
                return;
            }

            for (int i = 0; i < model.Weapons.Count; i++)
            {
                V3DefenseWeaponEntryModel weapon = model.Weapons[i];
                if (weapon != null)
                {
                    weapon.AutoLoaderAvailable =
                        this.SegmentHasLoader(weapon.SegmentSlot, true);
                }
            }
        }

        private bool SegmentHasLoader(
            ShuttleControlSegmentSlotModel segment,
            bool requireEnabled)
        {
            if (segment == null || segment.ModuleSlots == null)
            {
                return false;
            }

            for (int i = 0; i < segment.ModuleSlots.Count; i++)
            {
                ShuttleControlModuleSlotModel module = segment.ModuleSlots[i];
                if (IsLoader(module) &&
                    (!requireEnabled || module.InstalledModuleEnabled))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsLoader(ShuttleControlModuleSlotModel module)
        {
            return module != null &&
                !string.IsNullOrEmpty(module.InstalledModuleInstanceID) &&
                ShuttleModuleTypeCatalog.ParseModuleType(
                    module.InstalledModuleTypeID) == ShuttleModuleType.AmmoLoader;
        }
    }
}
