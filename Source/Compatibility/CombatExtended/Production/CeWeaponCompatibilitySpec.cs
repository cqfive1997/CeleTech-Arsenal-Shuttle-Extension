namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    internal sealed class CeWeaponCompatibilitySpec
    {
        private CeWeaponCompatibilitySpec(
            string moduleDefName,
            string sourceWeaponDefName,
            string runtimeGunDefName,
            string ammoDefName,
            string ammoSetDefName,
            string projectileDefName,
            int magazineCapacity)
        {
            this.ModuleDefName = moduleDefName;
            this.SourceWeaponDefName = sourceWeaponDefName;
            this.RuntimeGunDefName = runtimeGunDefName;
            this.AmmoDefName = ammoDefName;
            this.AmmoSetDefName = ammoSetDefName;
            this.ProjectileDefName = projectileDefName;
            this.MagazineCapacity = magazineCapacity;
        }

        internal string ModuleDefName { get; private set; }

        internal string SourceWeaponDefName { get; private set; }

        internal string RuntimeGunDefName { get; private set; }

        internal string AmmoDefName { get; private set; }

        internal string AmmoSetDefName { get; private set; }

        internal string ProjectileDefName { get; private set; }

        internal int MagazineCapacity { get; private set; }

        internal static CeWeaponCompatibilitySpec ForWeapon(string moduleDefName)
        {
            switch (moduleDefName)
            {
                case "CT_Shuttle_Module_6mmPointDefense":
                    return new CeWeaponCompatibilitySpec(
                        moduleDefName,
                        "CT_Shuttle_Weapon_6mmPointDefenseGun",
                        "CT_Shuttle_CE_RuntimeGun_6mmPointDefense",
                        "CT_ShuttleAmmo_6mmAP",
                        "CT_ShuttleAmmoSet_PointDefense",
                        "CT_Shuttle_CE_Projectile_6mmAP",
                        1000);
                case "CT_Shuttle_Module_40mmCIWS":
                    return new CeWeaponCompatibilitySpec(
                        moduleDefName,
                        "CT_Shuttle_Weapon_40mmCIWSCannon",
                        "CT_Shuttle_CE_RuntimeGun_40mmCIWS",
                        "CT_ShuttleAmmo_40mmAPHE",
                        "CT_ShuttleAmmoSet_40mmCIWS",
                        "CT_Shuttle_CE_Projectile_40mmAPHE",
                        280);
                default:
                    return null;
            }
        }
    }
}
