using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.Defs
{
    /// <summary>
    /// Static fire-control radar hardware. Runtime weapon linking and target selection are owned
    /// by weapon runtime systems, not this Def.
    /// </summary>
    public sealed class ShuttleFireControlRadarModuleDef : ShuttleModuleBaseDef
    {
        public bool supportsAutoDefense = true;
        public bool supportsPointDefense = true;
        public float pointDefenseRadius = 35f;
        public float directFireAccuracyMultiplier = 1f;
        public float directFireAccuracyBonus;
        public float directFireAccuracyFloor;
        public float forcedMissRadiusMultiplier = 1f;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }

            if (this.ModuleType != ShuttleModuleType.FireControl)
            {
                yield return this.defName + " must use moduleTypeID fire-control.";
            }

            if (!this.IsFiniteFloat(this.pointDefenseRadius))
            {
                yield return this.defName + " has non-finite pointDefenseRadius.";
            }
            else if (this.pointDefenseRadius < 0f)
            {
                yield return this.defName + " has negative pointDefenseRadius.";
            }
            else if (this.supportsPointDefense && this.pointDefenseRadius <= 0f)
            {
                yield return this.defName + " must have positive pointDefenseRadius when supportsPointDefense is true.";
            }

            if (!this.IsFiniteFloat(this.directFireAccuracyMultiplier))
            {
                yield return this.defName + " has non-finite directFireAccuracyMultiplier.";
            }
            else if (this.directFireAccuracyMultiplier < 0f)
            {
                yield return this.defName + " has negative directFireAccuracyMultiplier.";
            }

            if (!this.IsFiniteFloat(this.directFireAccuracyBonus))
            {
                yield return this.defName + " has non-finite directFireAccuracyBonus.";
            }
            else if (this.directFireAccuracyBonus < 0f)
            {
                yield return this.defName + " has negative directFireAccuracyBonus.";
            }

            if (!this.IsFiniteFloat(this.directFireAccuracyFloor))
            {
                yield return this.defName + " has non-finite directFireAccuracyFloor.";
            }
            else if (this.directFireAccuracyFloor < 0f || this.directFireAccuracyFloor > 1f)
            {
                yield return this.defName + " has directFireAccuracyFloor outside 0..1.";
            }

            if (!this.IsFiniteFloat(this.forcedMissRadiusMultiplier))
            {
                yield return this.defName + " has non-finite forcedMissRadiusMultiplier.";
            }
            else if (this.forcedMissRadiusMultiplier < 0f)
            {
                yield return this.defName + " has negative forcedMissRadiusMultiplier.";
            }
        }
    }
}
