using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Weapons;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Pure definition/runtime-shape inspection for the simple Native Verb host. It creates no
    /// gun and mutates no state. Unknown Comps and multi-Verb shapes fail closed.
    /// </summary>
    internal sealed class NativeVerbWeaponCompatibilityEvaluator
    {
        internal ShuttleWeaponCompatibilityReport Evaluate(
            ShuttleWeaponBackendProbeContext context)
        {
            if (context == null)
            {
                return Rejected("probe-context-missing");
            }

            ShuttleWeaponModuleDef weaponDef = context.WeaponDef;
            if (weaponDef == null)
            {
                return Rejected("weapon-def-missing");
            }

            if (!string.Equals(
                    context.MagazineAuthorityBackendId,
                    ShuttleWeaponRuntimeState.CoreMagazineAuthorityId,
                    StringComparison.Ordinal))
            {
                return ShuttleWeaponCompatibilityReport.NotApplicable(
                    NativeVerbWeaponBackendFactory.Id,
                    "core-magazine-authority-inactive");
            }

            ThingDef gunDef = weaponDef.weaponDef;
            if (gunDef == null)
            {
                return Rejected("gun-def-missing");
            }

            if (gunDef.thingClass == null ||
                !typeof(ThingWithComps).IsAssignableFrom(gunDef.thingClass))
            {
                return Rejected("gun-thing-class-unsupported");
            }

            string compFailure;
            if (!HasOnlySupportedComps(gunDef, out compFailure))
            {
                return Rejected(compFailure);
            }

            string verbFailure;
            if (!HasOneSupportedProjectileVerb(gunDef, out verbFailure))
            {
                return Rejected(verbFailure);
            }

            Thing existingGun = context.ExistingGun;
            if (existingGun != null)
            {
                if (existingGun.Destroyed || existingGun.def != gunDef)
                {
                    return Rejected("existing-gun-definition-mismatch");
                }

                ThingWithComps typedGun = existingGun as ThingWithComps;
                if (typedGun == null || typedGun.TryGetComp<CompEquippable>() == null)
                {
                    return Rejected("existing-gun-equippable-missing");
                }
            }

            return ShuttleWeaponCompatibilityReport.Supported(
                NativeVerbWeaponBackendFactory.Id,
                "native-definition-ready");
        }

        private static bool HasOnlySupportedComps(ThingDef gunDef, out string failureReason)
        {
            failureReason = null;
            List<CompProperties> comps = gunDef != null ? gunDef.comps : null;
            if (comps == null || comps.Count == 0)
            {
                failureReason = "equippable-comp-missing";
                return false;
            }

            int equippableCount = 0;
            for (int i = 0; i < comps.Count; i++)
            {
                CompProperties comp = comps[i];
                Type compClass = comp != null ? comp.compClass : null;
                if (compClass != null && typeof(CompEquippable).IsAssignableFrom(compClass))
                {
                    equippableCount++;
                    continue;
                }

                failureReason = "stateful-comp-adapter-required";
                return false;
            }

            if (equippableCount != 1)
            {
                failureReason = "equippable-comp-count-unsupported";
                return false;
            }

            return true;
        }

        private static bool HasOneSupportedProjectileVerb(
            ThingDef gunDef,
            out string failureReason)
        {
            failureReason = null;
            List<VerbProperties> verbs = gunDef != null ? gunDef.Verbs : null;
            if (verbs == null || verbs.Count != 1)
            {
                failureReason = "verb-count-unsupported";
                return false;
            }

            VerbProperties verb = verbs[0];
            Type verbClass = verb != null ? verb.verbClass : null;
            if (verbClass == null)
            {
                failureReason = "verb-class-missing";
                return false;
            }

            bool supportedClass = verbClass == typeof(Verb_Shoot) ||
                verbClass == typeof(Verb_ShuttleMuzzleShoot);
            if (!supportedClass)
            {
                failureReason = "verb-adapter-required";
                return false;
            }

            if (verb.defaultProjectile == null)
            {
                failureReason = "projectile-missing";
                return false;
            }

            return true;
        }

        private static ShuttleWeaponCompatibilityReport Rejected(string reasonCode)
        {
            return ShuttleWeaponCompatibilityReport.Rejected(
                NativeVerbWeaponBackendFactory.Id,
                reasonCode);
        }
    }
}
