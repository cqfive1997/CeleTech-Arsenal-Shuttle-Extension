using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CelestialArsenal
{
    /// <summary>
    /// Pure typed admission policy for the shuttle-owned sustained particle lance.
    /// </summary>
    internal sealed class CelestialSustainLaserCompatibilityEvaluator
    {
        internal ShuttleWeaponCompatibilityReport Evaluate(
            ShuttleWeaponBackendProbeContext context)
        {
            ShuttleWeaponModuleDef moduleDef = context != null ? context.WeaponDef : null;
            ThingDef gunDef = moduleDef != null ? moduleDef.weaponDef : null;
            if (gunDef == null)
            {
                return NotApplicable("particle-lance-shape-absent");
            }

            List<VerbProperties> verbs = gunDef.Verbs;
            bool shuttleVerb = HasExactVerb(
                verbs,
                typeof(Verb_ShuttleCelestialSustainLaser));
            bool hasData = HasExactComp<CompShuttleSustainLaserData>(gunDef.comps);
            if (!shuttleVerb && !hasData)
            {
                return NotApplicable("particle-lance-shape-absent");
            }

            if (gunDef.thingClass == null ||
                !typeof(ThingWithComps).IsAssignableFrom(gunDef.thingClass))
            {
                return Rejected("particle-lance-thing-class-unsupported");
            }

            if (verbs == null || verbs.Count != 1 || !shuttleVerb)
            {
                return Rejected("particle-lance-verb-shape-unsupported");
            }

            string compFailure;
            if (!HasSupportedCompShape(gunDef.comps, out compFailure))
            {
                return Rejected(compFailure);
            }

            Thing existingGun = context != null ? context.ExistingGun : null;
            if (existingGun != null &&
                (existingGun.Destroyed || existingGun.def != gunDef))
            {
                return Rejected("particle-lance-existing-gun-mismatch");
            }

            string authority = context != null
                ? context.MagazineAuthorityBackendId
                : null;
            if (!string.IsNullOrEmpty(authority) &&
                authority != ShuttleWeaponRuntimeState.CoreMagazineAuthorityId)
            {
                return Rejected("particle-lance-magazine-authority-unsupported");
            }

            return ShuttleWeaponCompatibilityReport.Supported(
                CelestialSustainLaserBackendFactory.Id,
                CelestialSustainLaserBackendFactory.SupportedReason);
        }

        internal static bool MatchesExactDefinitionShape(ShuttleWeaponModuleDef moduleDef)
        {
            ThingDef gunDef = moduleDef != null ? moduleDef.weaponDef : null;
            if (gunDef == null ||
                !HasExactVerb(gunDef.Verbs, typeof(Verb_ShuttleCelestialSustainLaser)))
            {
                return false;
            }

            string failureReason;
            return HasSupportedCompShape(gunDef.comps, out failureReason);
        }

        private static bool HasExactVerb(List<VerbProperties> verbs, Type verbType)
        {
            return verbs != null && verbs.Count == 1 && verbs[0] != null &&
                verbs[0].verbClass == verbType;
        }

        private static bool HasSupportedCompShape(
            List<CompProperties> comps,
            out string failureReason)
        {
            failureReason = null;
            int equippableCount = 0;
            int dataCount = 0;
            int forbiddableCount = 0;
            int styleableCount = 0;
            for (int i = 0; comps != null && i < comps.Count; i++)
            {
                CompProperties props = comps[i];
                Type compClass = props != null ? props.compClass : null;
                if (compClass != null && typeof(CompEquippable).IsAssignableFrom(compClass))
                {
                    equippableCount++;
                }
                else if (compClass == typeof(CompShuttleSustainLaserData))
                {
                    dataCount++;
                }
                else if (compClass == typeof(CompForbiddable))
                {
                    forbiddableCount++;
                }
                else if (compClass == typeof(CompStyleable))
                {
                    styleableCount++;
                }
                else
                {
                    failureReason = "particle-lance-comp-shape-unsupported";
                    return false;
                }
            }

            // BaseWeaponTurret inherits these two passive BaseWeapon Comps. They own no
            // hidden-gun tick or firing state, but remain part of the resolved ThingDef shape.
            if (equippableCount != 1 || dataCount != 1 ||
                forbiddableCount != 1 || styleableCount != 1)
            {
                failureReason = "particle-lance-comp-count-unsupported";
                return false;
            }

            return true;
        }

        private static bool HasExactComp<TComp>(List<CompProperties> comps)
            where TComp : ThingComp
        {
            for (int i = 0; comps != null && i < comps.Count; i++)
            {
                if (comps[i] != null && comps[i].compClass == typeof(TComp))
                {
                    return true;
                }
            }

            return false;
        }

        private static ShuttleWeaponCompatibilityReport NotApplicable(string reason)
        {
            return ShuttleWeaponCompatibilityReport.NotApplicable(
                CelestialSustainLaserBackendFactory.Id,
                reason);
        }

        private static ShuttleWeaponCompatibilityReport Rejected(string reason)
        {
            return ShuttleWeaponCompatibilityReport.Rejected(
                CelestialSustainLaserBackendFactory.Id,
                reason);
        }
    }
}
