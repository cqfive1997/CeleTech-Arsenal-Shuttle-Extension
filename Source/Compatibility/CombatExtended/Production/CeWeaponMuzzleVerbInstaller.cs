using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using CombatExtended;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    internal sealed class CeWeaponMuzzleVerbInstaller
    {
        internal bool TryEnsureInstalled(
            Thing gun,
            string moduleInstanceID,
            string parentSlotID,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState runtimeState,
            out CeWeaponMuzzleVerb muzzleVerb,
            out string failureReason)
        {
            muzzleVerb = null;
            failureReason = null;
            CompEquippable equippable = CeWeaponRuntimeGunAccess.GetEquippable(gun);
            Verb original = equippable != null ? equippable.PrimaryVerb : null;
            if (original == null)
            {
                failureReason = "ce-primary-verb-missing";
                return false;
            }

            muzzleVerb = original as CeWeaponMuzzleVerb;
            if (muzzleVerb != null)
            {
                muzzleVerb.ConfigureRuntimeBinding(
                    moduleInstanceID,
                    parentSlotID,
                    weaponDef,
                    runtimeState);
                return true;
            }

            if (original.GetType() != typeof(Verb_ShootCE))
            {
                failureReason = "ce-primary-verb-type-unsupported:" +
                    original.GetType().FullName;
                return false;
            }

            List<Verb> verbs = equippable.verbTracker != null
                ? equippable.verbTracker.AllVerbs
                : null;
            int index = FindReferenceIndex(verbs, original);
            if (index < 0)
            {
                failureReason = "ce-primary-verb-not-owned-by-tracker";
                return false;
            }

            muzzleVerb = new CeWeaponMuzzleVerb();
            muzzleVerb.loadID = original.loadID;
            muzzleVerb.verbProps = original.verbProps;
            muzzleVerb.verbTracker = original.verbTracker;
            muzzleVerb.tool = original.tool;
            muzzleVerb.maneuver = original.maneuver;
            muzzleVerb.caster = original.caster;
            muzzleVerb.castCompleteCallback = original.castCompleteCallback;
            muzzleVerb.ConfigureRuntimeBinding(
                moduleInstanceID,
                parentSlotID,
                weaponDef,
                runtimeState);
            verbs[index] = muzzleVerb;
            return true;
        }

        private static int FindReferenceIndex(List<Verb> verbs, Verb target)
        {
            if (verbs == null || target == null)
            {
                return -1;
            }

            for (int i = 0; i < verbs.Count; i++)
            {
                if (ReferenceEquals(verbs[i], target))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
