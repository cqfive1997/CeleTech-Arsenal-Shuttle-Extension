using CeleTech.ShuttleExtension.ModularShuttle.API.SDK;
using CeleTech.ShuttleExtension.ModularShuttle.Core;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.External
{
    internal sealed class ExternalSDKLaunchCooldownSnapshotBuilder
    {
        internal ShuttleExternalLaunchCooldownSnapshot Build(
            ShuttleProfile profile,
            ShuttleRuntimeState runtimeState,
            int ticksGame)
        {
            int lastLaunchTick = -1;
            int cooldownEndTick = -1;
            int lastArrivalTick = -1;
            int configuredTotalTicks =
                profile != null && profile.Flight != null &&
                profile.Flight.LaunchCooldownTicks >= 0
                    ? profile.Flight.LaunchCooldownTicks
                    : 0;

            if (runtimeState != null)
            {
                runtimeState.EnsureInitialized();
                if (runtimeState.Launch != null)
                {
                    lastLaunchTick = runtimeState.Launch.LastLaunchTick;
                    cooldownEndTick = runtimeState.Launch.CooldownEndTick;
                    lastArrivalTick = runtimeState.Launch.LastArrivalTick;
                }
            }

            if (configuredTotalTicks <= 0)
            {
                return new ShuttleExternalLaunchCooldownSnapshot(
                    false,
                    lastLaunchTick,
                    cooldownEndTick,
                    0,
                    0,
                    lastArrivalTick);
            }

            int remainingTicks = 0;
            bool active = ticksGame >= 0 && cooldownEndTick > ticksGame;
            if (active)
            {
                remainingTicks = cooldownEndTick - ticksGame;
            }

            int observedTotalTicks = configuredTotalTicks;
            if (cooldownEndTick > lastLaunchTick && lastLaunchTick >= 0)
            {
                observedTotalTicks = cooldownEndTick - lastLaunchTick;
            }

            if (observedTotalTicks < 0)
            {
                observedTotalTicks = 0;
            }

            return new ShuttleExternalLaunchCooldownSnapshot(
                active,
                lastLaunchTick,
                cooldownEndTick,
                remainingTicks,
                observedTotalTicks,
                lastArrivalTick);
        }

        internal static ShuttleExternalLaunchCooldownSnapshot Unavailable()
        {
            return new ShuttleExternalLaunchCooldownSnapshot(
                false,
                -1,
                -1,
                0,
                0,
                -1);
        }
    }
}
