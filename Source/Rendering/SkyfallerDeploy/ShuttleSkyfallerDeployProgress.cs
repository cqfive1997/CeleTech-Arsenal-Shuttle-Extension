using RimWorld;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.SkyfallerDeploy
{
    internal static class ShuttleSkyfallerDeployProgress
    {
        private const float DeployStartTicksToImpact = 70f;
        private const float DeployCompleteTicksToImpact = 18f;
        private const float LeavingRetractCompleteAnimationTime = 0.18f;
        private const float DrawThreshold = 0.001f;

        internal static float IncomingDeployProgress(Skyfaller skyfaller)
        {
            if (skyfaller == null)
            {
                return 0f;
            }

            float rawProgress = Mathf.InverseLerp(
                DeployStartTicksToImpact,
                DeployCompleteTicksToImpact,
                skyfaller.ticksToImpact);
            return Smooth01(Mathf.Clamp01(rawProgress));
        }

        internal static float LeavingDeployProgress(Skyfaller skyfaller, float timeInAnimation)
        {
            if (skyfaller == null)
            {
                return 0f;
            }

            float rawProgress = Mathf.InverseLerp(
                0f,
                LeavingRetractCompleteAnimationTime,
                Mathf.Clamp01(timeInAnimation));
            return 1f - Smooth01(Mathf.Clamp01(rawProgress));
        }

        internal static float Smooth01(float progress)
        {
            progress = Mathf.Clamp01(progress);
            return progress * progress * (3f - 2f * progress);
        }

        internal static bool ShouldDrawDeployLayers(float progress)
        {
            return progress > DrawThreshold;
        }
    }
}
