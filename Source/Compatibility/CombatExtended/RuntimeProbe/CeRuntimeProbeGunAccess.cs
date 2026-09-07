using System;
using CombatExtended;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended.RuntimeProbe
{
    internal static class CeRuntimeProbeGunAccess
    {
        internal static CompEquippable GetEquippable(ThingWithComps gun)
        {
            return gun != null ? gun.TryGetComp<CompEquippable>() : null;
        }

        internal static Verb_ShootCE GetVerb(ThingWithComps gun)
        {
            CompEquippable equippable = GetEquippable(gun);
            return equippable != null ? equippable.PrimaryVerb as Verb_ShootCE : null;
        }

        internal static CompAmmoUser GetAmmo(ThingWithComps gun)
        {
            return gun != null ? gun.TryGetComp<CompAmmoUser>() : null;
        }

        internal static CompFireModes GetModes(ThingWithComps gun)
        {
            return gun != null ? gun.TryGetComp<CompFireModes>() : null;
        }

        internal static bool TryBind(
            ThingWithComps gun,
            Thing host,
            Action completionCallback)
        {
            Verb_ShootCE verb = GetVerb(gun);
            if (verb == null || host == null)
            {
                return false;
            }

            verb.caster = host;
            verb.castCompleteCallback = completionCallback;
            return true;
        }

        internal static void Release(ThingWithComps gun)
        {
            Verb_ShootCE verb = GetVerb(gun);
            if (verb == null)
            {
                return;
            }

            verb.Reset();
            verb.caster = null;
            verb.castCompleteCallback = null;
        }
    }
}
