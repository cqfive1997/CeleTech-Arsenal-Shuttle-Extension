using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Shield.Surface;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    internal interface IShuttleSurfaceShieldPort
    {
        bool TryGetActiveSurfaceShieldStatus(
            out ShuttleSurfaceShieldStatusSnapshot snapshot);

        bool TryGetActiveSurfaceShieldVisualConfig(
            out ShuttleSurfaceShieldVisualConfigSnapshot snapshot);

        bool TrySetActiveSurfaceShieldRechargeSpeed(float multiplier);

        bool TryAbsorbIncomingDamage(
            ref DamageInfo dinfo,
            out ShuttleSurfaceShieldAbsorbResult result);
    }
}
