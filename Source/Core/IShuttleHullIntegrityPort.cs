using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Hull;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    internal interface IShuttleHullIntegrityPort
    {
        bool TryApplyIncomingHullDamage(
            ref DamageInfo dinfo,
            out ShuttleHullDamageResult result);
    }
}
