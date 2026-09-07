using System;
using System.Collections.Generic;
using CombatExtended;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended.RuntimeProbe
{
    internal sealed class CeRuntimeProbeCandidate
    {
        internal CeRuntimeProbeCandidate(
            ThingDef weaponDef,
            AmmoSetDef initialAmmoSet,
            AmmoDef initialAmmo,
            int magazineSize)
        {
            this.WeaponDef = weaponDef;
            this.InitialAmmoSet = initialAmmoSet;
            this.InitialAmmo = initialAmmo;
            this.MagazineSize = magazineSize;
        }

        internal ThingDef WeaponDef { get; private set; }

        internal AmmoSetDef InitialAmmoSet { get; private set; }

        internal AmmoDef InitialAmmo { get; private set; }

        internal int MagazineSize { get; private set; }

        internal string MenuLabel
        {
            get
            {
                return this.WeaponDef.LabelCap + " [" + this.WeaponDef.defName + "]" +
                    " | CE ammo: " + this.InitialAmmo.LabelCap +
                    " [" + this.InitialAmmo.defName + "]" +
                    " | mag " + this.MagazineSize;
            }
        }
    }

    internal static class CeRuntimeProbeCandidateResolver
    {
        internal static List<CeRuntimeProbeCandidate> Resolve()
        {
            List<CeRuntimeProbeCandidate> results = new List<CeRuntimeProbeCandidate>();
            List<ThingDef> defs = DefDatabase<ThingDef>.AllDefsListForReading;
            for (int i = 0; i < defs.Count; i++)
            {
                CeRuntimeProbeCandidate candidate;
                if (TryResolve(defs[i], out candidate))
                {
                    results.Add(candidate);
                }
            }

            results.Sort(delegate(CeRuntimeProbeCandidate left, CeRuntimeProbeCandidate right)
            {
                return string.Compare(left.MenuLabel, right.MenuLabel, StringComparison.OrdinalIgnoreCase);
            });
            return results;
        }

        private static bool TryResolve(ThingDef def, out CeRuntimeProbeCandidate candidate)
        {
            candidate = null;
            if (def == null || def.thingClass == null ||
                !typeof(ThingWithComps).IsAssignableFrom(def.thingClass))
            {
                return false;
            }

            CompProperties_AmmoUser ammoProperties =
                def.GetCompProperties<CompProperties_AmmoUser>();
            if (ammoProperties == null || ammoProperties.magazineSize <= 1 ||
                ammoProperties.ammoSet == null ||
                ammoProperties.ammoSet.ammoTypes.NullOrEmpty())
            {
                return false;
            }

            bool hasCeShootVerb = false;
            List<VerbProperties> verbs = def.Verbs;
            for (int i = 0; i < verbs.Count; i++)
            {
                Type verbClass = verbs[i] != null ? verbs[i].verbClass : null;
                if (verbClass != null && typeof(Verb_ShootCE).IsAssignableFrom(verbClass))
                {
                    hasCeShootVerb = true;
                    break;
                }
            }

            if (!hasCeShootVerb)
            {
                return false;
            }

            AmmoDef initialAmmo = ammoProperties.ammoSet.ammoTypes[0].ammo;
            if (initialAmmo == null)
            {
                return false;
            }

            candidate = new CeRuntimeProbeCandidate(
                def,
                ammoProperties.ammoSet,
                initialAmmo,
                ammoProperties.magazineSize);
            return true;
        }
    }
}
