using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Defs
{
    public sealed class ShuttleWeaponAmmoSetDef : Def
    {
        public List<ShuttleWeaponAmmoDef> ammoDefs;
        public ShuttleWeaponAmmoDef defaultAmmo;
        public bool allowAmmoSwitching = true;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }

            if (this.ammoDefs == null || this.ammoDefs.Count == 0)
            {
                yield return this.defName + " has no ammoDefs.";
                yield break;
            }

            bool containsDefault = false;
            for (int i = 0; i < this.ammoDefs.Count; i++)
            {
                ShuttleWeaponAmmoDef ammo = this.ammoDefs[i];
                if (ammo == null)
                {
                    yield return this.defName + " has a null ammoDefs entry at index " + i + ".";
                    continue;
                }

                if (ammo == this.defaultAmmo)
                {
                    containsDefault = true;
                }

                for (int j = i + 1; j < this.ammoDefs.Count; j++)
                {
                    if (ammo == this.ammoDefs[j])
                    {
                        yield return this.defName + " includes duplicate ammo def " + ammo.defName + ".";
                    }
                }
            }

            if (this.defaultAmmo == null)
            {
                yield return this.defName + " has no defaultAmmo.";
            }
            else if (!containsDefault)
            {
                yield return this.defName + " defaultAmmo " + this.defaultAmmo.defName + " is not in ammoDefs.";
            }
        }
    }
}
