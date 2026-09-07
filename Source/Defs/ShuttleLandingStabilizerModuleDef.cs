using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Defs
{
    public sealed class ShuttleLandingStabilizerModuleDef : ShuttleModuleBaseDef
    {
        public bool stabilizesAdverseWeather = true;
        public float maxWeatherSeverityBypass = 1f;
        public bool allowsStormLaunch = true;

        public bool EffectiveStabilizesAdverseWeather
        {
            get
            {
                return this.stabilizesAdverseWeather &&
                    this.allowsStormLaunch &&
                    this.maxWeatherSeverityBypass > 0f &&
                    this.maxWeatherSeverityBypass <= 1f;
            }
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }

            if (!this.IsFiniteFloat(this.maxWeatherSeverityBypass))
            {
                yield return this.defName + " has non-finite maxWeatherSeverityBypass.";
            }
            else if (this.maxWeatherSeverityBypass < 0f)
            {
                yield return this.defName + " has negative maxWeatherSeverityBypass.";
            }

            if (this.IsFiniteFloat(this.maxWeatherSeverityBypass) && this.maxWeatherSeverityBypass > 1f)
            {
                yield return this.defName + " maxWeatherSeverityBypass must not be greater than 1.";
            }

            if (this.stabilizesAdverseWeather &&
                this.allowsStormLaunch &&
                this.maxWeatherSeverityBypass <= 0f)
            {
                yield return this.defName + " maxWeatherSeverityBypass must be greater than 0 when storm launch bypass is enabled.";
            }
        }
    }
}
