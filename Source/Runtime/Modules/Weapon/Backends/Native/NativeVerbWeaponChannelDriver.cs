using System;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    internal sealed class NativeVerbWeaponChannelDriver : IShuttleWeaponChannelDriver
    {
        internal const string PrimaryChannelId = "primary";

        private readonly NativeVerbWeaponHost host;

        internal NativeVerbWeaponChannelDriver(NativeVerbWeaponHost host)
        {
            this.host = host;
        }

        public int GetChannelCount(ShuttleModuleRuntimeContext context)
        {
            return this.HasPrimaryVerb(context) ? 1 : 0;
        }

        public string GetChannelId(ShuttleModuleRuntimeContext context, int channelIndex)
        {
            return channelIndex == 0 && this.HasPrimaryVerb(context)
                ? PrimaryChannelId
                : null;
        }

        public string GetSelectedChannelId(ShuttleModuleRuntimeContext context)
        {
            return this.HasPrimaryVerb(context) ? PrimaryChannelId : null;
        }

        public bool TrySelectChannel(
            ShuttleModuleRuntimeContext context,
            string channelId,
            out string failureReason)
        {
            failureReason = null;
            if (!this.HasPrimaryVerb(context))
            {
                failureReason = "native-channel-unavailable";
                return false;
            }

            if (!string.Equals(channelId, PrimaryChannelId, StringComparison.Ordinal))
            {
                failureReason = "native-channel-unknown";
                return false;
            }

            return true;
        }

        private bool HasPrimaryVerb(ShuttleModuleRuntimeContext context)
        {
            Verb verb = this.host != null ? this.host.GetPrimaryVerb(context) : null;
            return verb != null;
        }
    }
}
