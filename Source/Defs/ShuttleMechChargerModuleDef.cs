using System.Collections.Generic;

namespace CeleTech.ShuttleExtension.ModularShuttle.Defs
{
    /// <summary>
    /// Static capability definition for shuttle mech charging bay modules.
    /// Live held mechs and charge progress belong to CompShuttleMechChargerOccupancy,
    /// not to this def or to ShuttleProfile.
    /// </summary>
    public sealed class ShuttleMechChargerModuleDef : ShuttleModuleBaseDef
    {
        public int mechChargeSlots = 4;
        public float chargeRateFactor = 1f;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }

            if (this.mechChargeSlots < 0)
            {
                yield return this.defName + " has negative mechChargeSlots.";
            }

            if (!this.IsFinitePositive(this.chargeRateFactor))
            {
                yield return this.defName + " must define finite positive chargeRateFactor.";
            }
        }

        private bool IsFinitePositive(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;
        }
    }
}
