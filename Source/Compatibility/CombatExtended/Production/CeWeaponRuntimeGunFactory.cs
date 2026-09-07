using System;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    internal sealed class CeWeaponRuntimeGunFactory
    {
        internal bool TryCreate(
            CeWeaponCompatibilitySpec spec,
            out ThingWithComps gun,
            out string failure)
        {
            gun = null;
            failure = null;
            if (spec == null)
            {
                failure = "compatibility-spec-missing";
                return false;
            }

            ThingDef runtimeGunDef = DefDatabase<ThingDef>.GetNamedSilentFail(
                spec.RuntimeGunDefName);
            if (runtimeGunDef == null)
            {
                failure = "runtime-gun-def-missing";
                return false;
            }

            try
            {
                gun = ThingMaker.MakeThing(runtimeGunDef) as ThingWithComps;
            }
            catch (Exception exception)
            {
                failure = "runtime-gun-create-exception:" + exception.GetType().Name;
                return false;
            }

            if (gun == null || gun.def != runtimeGunDef)
            {
                gun = null;
                failure = "runtime-gun-create-failed";
                return false;
            }

            return true;
        }
    }
}
