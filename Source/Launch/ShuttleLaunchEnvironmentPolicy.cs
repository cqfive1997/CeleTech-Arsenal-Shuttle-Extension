using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Launch
{
    internal sealed class ShuttleLaunchEnvironmentPolicy
    {
        public bool TryGetLaunchBlocker(
            ThingWithComps host,
            bool stabilizesAdverseWeather,
            out string failureReason)
        {
            failureReason = null;
            if (host == null || !host.Spawned || host.Map == null)
            {
                return false;
            }

            if (stabilizesAdverseWeather)
            {
                return false;
            }

            WeatherDef weather = host.Map.weatherManager != null
                ? host.Map.weatherManager.CurWeatherPerceived
                : null;
            if (weather != null && weather.preventsShuttleLaunch)
            {
                failureReason = this.BuildFailureReason(weather.LabelCap.ToString());
                return true;
            }

            if (host.Map.GameConditionManager == null ||
                host.Map.GameConditionManager.ActiveConditions == null)
            {
                return false;
            }

            for (int i = 0; i < host.Map.GameConditionManager.ActiveConditions.Count; i++)
            {
                GameCondition condition = host.Map.GameConditionManager.ActiveConditions[i];
                if (condition != null &&
                    condition.def != null &&
                    condition.def.preventShuttleLaunch)
                {
                    failureReason = this.BuildFailureReason(condition.LabelCap.ToString());
                    return true;
                }
            }

            return false;
        }

        private string BuildFailureReason(string conditionLabel)
        {
            if (string.IsNullOrEmpty(conditionLabel))
            {
                conditionLabel = "CT_Shuttle_AdverseLaunchCondition".Translate().ToString();
            }

            return "CT_Shuttle_LaunchBlockedByAdverseWeather".Translate(conditionLabel).ToString();
        }
    }
}
