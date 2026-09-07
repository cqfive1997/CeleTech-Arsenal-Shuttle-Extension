using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using RimWorld;

namespace CeleTech.ShuttleExtension.ModularShuttle.Defs
{
    /// <summary>
    /// Static Prison Cell capability definition. Prisoner containment, warden work,
    /// feeding, tending, escape risk, and launch transfer belong to later runtime
    /// services rather than this Def.
    /// </summary>
    public sealed class ShuttlePrisonCellModuleDef : ShuttleModuleBaseDef
    {
        public int prisonerSlots = 1;
        public float security = 1f;
        public float comfort = 0.35f;
        public bool supportsFeeding = true;
        public bool supportsTending = true;
        public bool allowCargoFoodSupply = true;
        public bool allowRefrigeratedCargoFoodSupply = true;
        public bool requireCargoLogisticsForFoodSupply = true;
        public FoodPreferability maximumCargoFoodPreferability =
            FoodPreferability.MealLavish;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }

            if (this.ModuleType != ShuttleModuleType.PrisonCell)
            {
                yield return this.defName + " must use moduleTypeID prison-cell.";
            }

            if (this.prisonerSlots <= 0)
            {
                yield return this.defName + " must define positive prisonerSlots.";
            }

            if (!this.IsFiniteFloat(this.security))
            {
                yield return this.defName + " has non-finite security.";
            }
            else if (this.security < 0f)
            {
                yield return this.defName + " has negative security.";
            }

            if (!this.IsFiniteFloat(this.comfort))
            {
                yield return this.defName + " has non-finite comfort.";
            }
            else if (this.comfort < 0f)
            {
                yield return this.defName + " has negative comfort.";
            }

            if (!ShuttlePrisonCellSupplyConfigState.IsSupportedMaximumFoodPreferability(
                this.maximumCargoFoodPreferability))
            {
                yield return this.defName +
                    " must define maximumCargoFoodPreferability between MealAwful and MealLavish.";
            }

            if (!this.allowCargoFoodSupply && this.allowRefrigeratedCargoFoodSupply)
            {
                yield return "Warning: " + this.defName +
                    " allows refrigerated Prison Cell food supply while cargo food supply is disabled.";
            }
        }
    }
}
