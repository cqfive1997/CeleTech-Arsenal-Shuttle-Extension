using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Defs
{
    public sealed class ShuttleNavigationComputerModuleDef : ShuttleModuleBaseDef
    {
        public int rangeBonusTiles;
        public float launchCooldownFactor = 1f;
        public bool providesAutonomousLaunchControl;
        public bool providesSecureSignalLink;

        public int EffectiveRangeBonusTiles
        {
            get
            {
                return this.rangeBonusTiles > 0 ? this.rangeBonusTiles : 0;
            }
        }

        public float EffectiveLaunchCooldownFactor
        {
            get
            {
                if (this.launchCooldownFactor < 0f || this.launchCooldownFactor > 1f)
                {
                    return 1f;
                }

                return this.launchCooldownFactor;
            }
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }

            if (this.rangeBonusTiles < 0)
            {
                yield return this.defName + " has negative rangeBonusTiles.";
            }

            if (!this.IsFiniteFloat(this.launchCooldownFactor))
            {
                yield return this.defName + " has non-finite launchCooldownFactor.";
            }
            else if (this.launchCooldownFactor < 0f)
            {
                yield return this.defName + " launchCooldownFactor must not be negative.";
            }

            if (this.IsFiniteFloat(this.launchCooldownFactor) && this.launchCooldownFactor > 1f)
            {
                yield return this.defName + " launchCooldownFactor must not be greater than 1.";
            }
        }
    }
}
