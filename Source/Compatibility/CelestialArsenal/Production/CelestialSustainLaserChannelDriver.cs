using System;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;

namespace CeleTech.ShuttleExtension.Compatibility.CelestialArsenal
{
    internal sealed class CelestialSustainLaserChannelDriver : IShuttleWeaponChannelDriver
    {
        private const string PrimaryChannelId = "primary";
        private readonly CelestialSustainLaserHost host;

        internal CelestialSustainLaserChannelDriver(CelestialSustainLaserHost host)
        {
            this.host = host;
        }

        public int GetChannelCount(ShuttleModuleRuntimeContext context)
        {
            return this.HasVerb(context) ? 1 : 0;
        }

        public string GetChannelId(ShuttleModuleRuntimeContext context, int channelIndex)
        {
            return channelIndex == 0 && this.HasVerb(context) ? PrimaryChannelId : null;
        }

        public string GetSelectedChannelId(ShuttleModuleRuntimeContext context)
        {
            return this.HasVerb(context) ? PrimaryChannelId : null;
        }

        public bool TrySelectChannel(
            ShuttleModuleRuntimeContext context,
            string channelId,
            out string failureReason)
        {
            failureReason = null;
            if (!this.HasVerb(context))
            {
                failureReason = "celestial-sustain-channel-unavailable";
                return false;
            }

            if (!string.Equals(channelId, PrimaryChannelId, StringComparison.Ordinal))
            {
                failureReason = "celestial-sustain-channel-unknown";
                return false;
            }

            return true;
        }

        private bool HasVerb(ShuttleModuleRuntimeContext context)
        {
            return this.host != null && this.host.GetVerb(context) != null;
        }
    }
}
