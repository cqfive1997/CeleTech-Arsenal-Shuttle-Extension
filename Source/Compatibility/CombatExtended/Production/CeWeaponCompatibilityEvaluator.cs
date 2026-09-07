using CombatExtended;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    /// <summary>
    /// Performs only typed Def compatibility checks for the two authored shuttle weapons.
    /// It creates no gun, magazine, Verb, owner token, runtime state, or cargo transaction.
    /// </summary>
    internal sealed class CeWeaponCompatibilityEvaluator
    {
        internal ShuttleWeaponCompatibilityReport Evaluate(
            ShuttleWeaponBackendProbeContext context,
            string backendId)
        {
            ShuttleWeaponModuleDef weaponDef = context != null ? context.WeaponDef : null;
            if (weaponDef == null)
            {
                return ShuttleWeaponCompatibilityReport.NotApplicable(
                    backendId,
                    "probe-context-missing");
            }

            CeWeaponCompatibilitySpec spec = CeWeaponCompatibilitySpec.ForWeapon(
                weaponDef.defName);
            if (spec == null)
            {
                return ShuttleWeaponCompatibilityReport.NotApplicable(
                    backendId,
                    "weapon-not-authored-ce");
            }

            ShuttleWeaponModuleAmmoExtension extension =
                weaponDef.GetModExtension<ShuttleWeaponModuleAmmoExtension>();
            if (extension == null || extension.ammoSet == null)
            {
                return AuthoredFailure(
                    backendId,
                    "core-ammo-extension-missing");
            }

            if (extension.ammoSet.defName != spec.AmmoSetDefName)
            {
                return AuthoredFailure(
                    backendId,
                    "core-ammo-set-mismatch");
            }

            AmmoDef ammoDef = DefDatabase<AmmoDef>.GetNamedSilentFail(spec.AmmoDefName);
            if (ammoDef == null)
            {
                return AuthoredFailure(
                    backendId,
                    "ce-ammo-def-missing");
            }

            AmmoSetDef ammoSet = DefDatabase<AmmoSetDef>.GetNamedSilentFail(
                spec.AmmoSetDefName);
            if (ammoSet == null)
            {
                return AuthoredFailure(
                    backendId,
                    "ce-ammo-set-missing");
            }

            AmmoLink link = FindAmmoLink(ammoSet, ammoDef);
            if (link == null)
            {
                return AuthoredFailure(
                    backendId,
                    "ce-ammo-link-missing");
            }

            if (link.projectile == null || link.projectile.defName != spec.ProjectileDefName)
            {
                return AuthoredFailure(
                    backendId,
                    "ce-projectile-mismatch");
            }

            if (weaponDef.weaponDef == null ||
                weaponDef.weaponDef.defName != spec.SourceWeaponDefName)
            {
                return AuthoredFailure(
                    backendId,
                    "source-weapon-def-mismatch");
            }

            if (DefDatabase<ThingDef>.GetNamedSilentFail(spec.RuntimeGunDefName) == null)
            {
                return AuthoredFailure(
                    backendId,
                    "ce-runtime-gun-def-missing");
            }

            if (context.MagazineAuthorityBackendId == backendId)
            {
                Thing existingGun = context.ExistingGun;
                CompAmmoUser magazine = CeWeaponRuntimeGunAccess.GetMagazine(existingGun);
                if (existingGun == null ||
                    existingGun.def == null ||
                    existingGun.def.defName != spec.RuntimeGunDefName ||
                    magazine == null ||
                    magazine.MagSize != spec.MagazineCapacity)
                {
                    return ShuttleWeaponCompatibilityReport.Rejected(
                        backendId,
                        "ce-authoritative-runtime-state-invalid");
                }

                return ShuttleWeaponCompatibilityReport.Supported(
                    backendId,
                    "ce-authority-active");
            }

            if (context.MagazineAuthorityBackendId != "legacy")
            {
                return ShuttleWeaponCompatibilityReport.NotApplicable(
                    backendId,
                    "authority-context-unavailable");
            }

            if (!context.AuthorityTransferReady)
            {
                return ShuttleWeaponCompatibilityReport.NotApplicable(
                    backendId,
                    "authority-transfer-not-idle");
            }

            return ShuttleWeaponCompatibilityReport.Supported(
                backendId,
                "authority-transfer-ready");
        }

        private static AmmoLink FindAmmoLink(AmmoSetDef ammoSet, AmmoDef ammoDef)
        {
            if (ammoSet == null || ammoDef == null || ammoSet.ammoTypes == null)
            {
                return null;
            }

            for (int i = 0; i < ammoSet.ammoTypes.Count; i++)
            {
                AmmoLink link = ammoSet.ammoTypes[i];
                if (link != null && link.ammo == ammoDef)
                {
                    return link;
                }
            }

            return null;
        }

        private static ShuttleWeaponCompatibilityReport AuthoredFailure(
            string backendId,
            string reasonCode)
        {
            return ShuttleWeaponCompatibilityReport.Rejected(backendId, reasonCode);
        }

    }
}
