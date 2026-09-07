using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.Defs
{
    /// <summary>
    /// Static whole-hull plating contribution. This only affects derived profile values;
    /// live damage routing and current hull integrity belong to later runtime work.
    /// </summary>
    public sealed class ShuttleHullPlatingModuleDef : ShuttleModuleBaseDef
    {
        public int hullHitPointsBonus;
        public float sharpDamageMultiplier = 1f;
        public float bluntDamageMultiplier = 1f;
        public float heatDamageMultiplier = 1f;
        public float explosionDamageMultiplier = 1f;
        public float empDamageMultiplier = 1f;
        public float flatDamageReduction;
        public int stuffCostCount;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }

            if (this.ModuleType != ShuttleModuleType.Armor)
            {
                yield return this.defName + " must use moduleTypeID armor.";
            }

            if (this.hullHitPointsBonus < 0)
            {
                yield return this.defName + " has negative hullHitPointsBonus.";
            }

            foreach (string error in this.ValidatePositiveMultiplier(
                this.sharpDamageMultiplier,
                "sharpDamageMultiplier"))
            {
                yield return error;
            }

            foreach (string error in this.ValidatePositiveMultiplier(
                this.bluntDamageMultiplier,
                "bluntDamageMultiplier"))
            {
                yield return error;
            }

            foreach (string error in this.ValidatePositiveMultiplier(
                this.heatDamageMultiplier,
                "heatDamageMultiplier"))
            {
                yield return error;
            }

            foreach (string error in this.ValidatePositiveMultiplier(
                this.explosionDamageMultiplier,
                "explosionDamageMultiplier"))
            {
                yield return error;
            }

            foreach (string error in this.ValidatePositiveMultiplier(
                this.empDamageMultiplier,
                "empDamageMultiplier"))
            {
                yield return error;
            }

            if (!this.IsFiniteFloat(this.flatDamageReduction))
            {
                yield return this.defName + " has non-finite flatDamageReduction.";
            }
            else if (this.flatDamageReduction < 0f)
            {
                yield return this.defName + " has negative flatDamageReduction.";
            }

            if (this.stuffCostCount < 0)
            {
                yield return this.defName + " has negative stuffCostCount.";
            }
        }

        private IEnumerable<string> ValidatePositiveMultiplier(float value, string fieldName)
        {
            if (!this.IsFiniteFloat(value))
            {
                yield return this.defName + " has non-finite " + fieldName + ".";
            }
            else if (value <= 0f)
            {
                yield return this.defName + " has non-positive " + fieldName + ".";
            }
        }
    }
}
