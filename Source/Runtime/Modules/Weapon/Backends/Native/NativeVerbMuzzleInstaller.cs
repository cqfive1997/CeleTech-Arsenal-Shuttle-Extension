using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Weapons;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Replaces only the exact vanilla projectile Verb on a transient hidden gun. The authored
    /// Def and its shared VerbProperties remain untouched; unknown subclasses fail closed.
    /// </summary>
    internal sealed class NativeVerbMuzzleInstaller
    {
        internal bool TryEnsureInstalled(
            ThingWithComps gun,
            out Verb_ShuttleMuzzleShoot muzzleVerb,
            out string failureReason)
        {
            muzzleVerb = null;
            failureReason = null;
            CompEquippable equippable = gun != null
                ? gun.TryGetComp<CompEquippable>()
                : null;
            Verb original = equippable != null ? equippable.PrimaryVerb : null;
            if (original == null)
            {
                failureReason = "native-primary-verb-missing";
                return false;
            }

            muzzleVerb = original as Verb_ShuttleMuzzleShoot;
            if (muzzleVerb != null)
            {
                if (original.GetType() == typeof(Verb_ShuttleMuzzleShoot))
                {
                    return true;
                }

                failureReason = "native-primary-verb-type-unsupported:" +
                    original.GetType().FullName;
                muzzleVerb = null;
                return false;
            }

            if (original.GetType() != typeof(Verb_Shoot))
            {
                failureReason = "native-primary-verb-type-unsupported:" +
                    original.GetType().FullName;
                return false;
            }

            List<Verb> verbs = equippable.verbTracker != null
                ? equippable.verbTracker.AllVerbs
                : null;
            int index = FindReferenceIndex(verbs, original);
            if (index < 0)
            {
                failureReason = "native-primary-verb-not-owned-by-tracker";
                return false;
            }

            muzzleVerb = new Verb_ShuttleMuzzleShoot();
            muzzleVerb.loadID = original.loadID;
            muzzleVerb.verbProps = original.verbProps;
            muzzleVerb.verbTracker = original.verbTracker;
            muzzleVerb.tool = original.tool;
            muzzleVerb.maneuver = original.maneuver;
            muzzleVerb.caster = original.caster;
            muzzleVerb.castCompleteCallback = original.castCompleteCallback;
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
