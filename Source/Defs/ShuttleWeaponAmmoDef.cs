using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Defs
{
    public sealed class ShuttleWeaponAmmoDef : Def
    {
        public string ammoCategory;
        public float massKg;
        public float marketValue;
        public float damageMultiplier = 1f;
        public float armorPenetrationMultiplier = 1f;
        public float explosionRadius;
        public float shieldDamageMultiplier = 1f;
        public float heatPerShot;
        public string projectileDefName;
        public string iconPath;
        public bool playerCraftable = true;
        public int workAmount;
        public List<ThingDefCountClass> costList;

        public ThingDef AmmoThingDef
        {
            get
            {
                return DefDatabase<ThingDef>.GetNamedSilentFail(this.defName);
            }
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }

            if (this.massKg < 0f)
            {
                yield return this.defName + " has negative massKg.";
            }

            if (this.marketValue < 0f)
            {
                yield return this.defName + " has negative marketValue.";
            }

            if (this.workAmount < 0)
            {
                yield return this.defName + " has negative workAmount.";
            }

            if (!string.IsNullOrEmpty(this.projectileDefName) &&
                DefDatabase<ThingDef>.GetNamedSilentFail(this.projectileDefName) == null)
            {
                yield return this.defName + " references missing projectileDefName " + this.projectileDefName + ".";
            }

            if (this.AmmoThingDef == null)
            {
                yield return this.defName + " has no matching ThingDef for cargo consumption.";
            }

            if (this.costList != null)
            {
                for (int i = 0; i < this.costList.Count; i++)
                {
                    ThingDefCountClass cost = this.costList[i];
                    if (cost == null || cost.thingDef == null)
                    {
                        yield return this.defName + " has a null costList entry at index " + i + ".";
                    }
                    else if (cost.count <= 0)
                    {
                        yield return this.defName + " has non-positive cost count for " + cost.thingDef.defName + ".";
                    }
                }
            }
        }
    }
}
