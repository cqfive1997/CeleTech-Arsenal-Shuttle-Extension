using System;
using System.Collections.Generic;
using CombatExtended;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended.RuntimeProbe
{
    internal static class CeRuntimeProbeMuzzleVerbInstaller
    {
        internal static bool TryInstall(
            ThingWithComps gun,
            CeRuntimeProbeMuzzleSource source,
            CeRuntimeProbeMuzzleObservation observation,
            out CeRuntimeProbeMuzzleVerb replacement,
            out string failure)
        {
            replacement = null;
            failure = null;
            CompEquippable equippable = CeRuntimeProbeGunAccess.GetEquippable(gun);
            Verb original = equippable != null ? equippable.PrimaryVerb : null;
            if (original == null)
            {
                failure = "The hidden gun has no primary Verb to replace.";
                return false;
            }

            if (original.GetType() != typeof(Verb_ShootCE))
            {
                failure = "The muzzle probe accepts only an exact stock Verb_ShootCE; resolved type was " +
                    original.GetType().FullName + ".";
                return false;
            }

            if (source == null || observation == null)
            {
                failure = "The muzzle source or observation sink is unavailable.";
                return false;
            }

            List<Verb> verbs = equippable.verbTracker.AllVerbs;
            int index = -1;
            for (int i = 0; i < verbs.Count; i++)
            {
                if (ReferenceEquals(verbs[i], original))
                {
                    index = i;
                    break;
                }
            }

            if (index < 0)
            {
                failure = "The primary CE Verb is not owned by its VerbTracker list.";
                return false;
            }

            replacement = new CeRuntimeProbeMuzzleVerb();
            replacement.loadID = original.loadID;
            replacement.verbProps = original.verbProps;
            replacement.verbTracker = original.verbTracker;
            replacement.tool = original.tool;
            replacement.maneuver = original.maneuver;
            replacement.caster = original.caster;
            replacement.castCompleteCallback = original.castCompleteCallback;
            replacement.Configure(source, observation);
            verbs[index] = replacement;
            observation.ReplacementInstalled = true;
            return true;
        }
    }
}
