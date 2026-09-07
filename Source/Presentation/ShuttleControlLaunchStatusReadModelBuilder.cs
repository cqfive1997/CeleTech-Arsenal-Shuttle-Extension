using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Presentation
{
    internal sealed class ShuttleControlLaunchStatusReadModelBuilder
    {
        internal void ApplyLaunchCooldown(ShuttleControlReadModel model, ShuttleRuntimeState runtimeState)
        {
            if (model == null)
            {
                return;
            }

            int configuredTotalTicks = Mathf.Max(0, model.LaunchCooldownTicks);
            model.LaunchCooldownTotalTicks = configuredTotalTicks;
            model.LaunchCooldownRemainingTicks = 0;
            model.LaunchReadyProgress = 1f;
            model.HasLaunchCooldown = false;

            if (runtimeState == null || configuredTotalTicks <= 0)
            {
                return;
            }

            runtimeState.EnsureInitialized();
            int ticksGame = ShuttleTickUtility.TicksGameOrMinusOne();
            if (ticksGame < 0 || runtimeState.Launch.CooldownEndTick <= ticksGame)
            {
                return;
            }

            int remainingTicks = Mathf.Max(0, runtimeState.Launch.CooldownEndTick - ticksGame);
            int observedTotalTicks = 0;
            if (runtimeState.Launch.LastLaunchTick >= 0 &&
                runtimeState.Launch.CooldownEndTick > runtimeState.Launch.LastLaunchTick)
            {
                observedTotalTicks = Mathf.Max(
                    0,
                    runtimeState.Launch.CooldownEndTick - runtimeState.Launch.LastLaunchTick);
            }

            int totalTicks = configuredTotalTicks;
            if (observedTotalTicks > totalTicks)
            {
                totalTicks = observedTotalTicks;
            }

            if (remainingTicks > totalTicks)
            {
                totalTicks = remainingTicks;
            }

            model.LaunchCooldownTotalTicks = totalTicks;
            model.LaunchCooldownRemainingTicks = remainingTicks;
            model.HasLaunchCooldown = remainingTicks > 0;
            model.LaunchReadyProgress = totalTicks > 0
                ? Mathf.Clamp01(1f - ((float)remainingTicks / totalTicks))
                : 1f;
        }
    }
}
