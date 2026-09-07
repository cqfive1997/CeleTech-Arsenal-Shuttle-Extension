using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using CombatExtended;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    internal sealed class CeWeaponMagazineMigrationPreflight
    {
        private readonly CeWeaponRuntimeGunFactory gunFactory =
            new CeWeaponRuntimeGunFactory();
        private readonly CeWeaponMagazineAccessor magazineAccessor =
            new CeWeaponMagazineAccessor();

        internal CeWeaponMagazineMigrationReport Inspect(
            string moduleInstanceID,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState state)
        {
            CeWeaponMagazineMigrationReport report =
                new CeWeaponMagazineMigrationReport();
            report.ModuleInstanceID = moduleInstanceID;
            report.ModuleDefName = weaponDef != null ? weaponDef.defName : null;

            CeWeaponCompatibilitySpec spec = CeWeaponCompatibilitySpec.ForWeapon(
                report.ModuleDefName);
            if (spec == null || weaponDef == null || state == null)
            {
                report.Failure = "installed-weapon-state-or-spec-missing";
                return report;
            }

            report.RuntimeGunDefName = spec.RuntimeGunDefName;
            report.AmmoDefName = spec.AmmoDefName;
            if (weaponDef.weaponDef == null ||
                weaponDef.weaponDef.defName != spec.SourceWeaponDefName)
            {
                report.Failure = "installed-weapon-def-mismatch";
                return report;
            }

            ShuttleWeaponAmmoState source = state.AmmoForRuntimeOnly;
            int sourceLoadedBefore = source.LoadedAmmoCount;
            string sourceSelectedBefore = source.SelectedAmmoDefName;
            report.SourceLoadedCount = sourceLoadedBefore;
            report.SourceCapacity = source.MagazineCapacity;
            if (source.MagazineCapacity != spec.MagazineCapacity)
            {
                report.Failure = "core-magazine-capacity-mismatch";
                return report;
            }

            if (sourceSelectedBefore != spec.AmmoDefName)
            {
                report.Failure = "core-selected-ammo-mismatch";
                return report;
            }

            ThingWithComps transientGun;
            string failure;
            if (!this.gunFactory.TryCreate(spec, out transientGun, out failure))
            {
                report.Failure = failure;
                return report;
            }

            CompAmmoUser magazine;
            AmmoDef ammoDef;
            if (!this.magazineAccessor.TryResolve(
                    transientGun,
                    spec,
                    out magazine,
                    out ammoDef,
                    out failure) ||
                !this.magazineAccessor.TryStageExact(
                    magazine,
                    ammoDef,
                    sourceLoadedBefore,
                    out failure))
            {
                report.Failure = failure;
                return report;
            }

            report.StagedLoadedCount = magazine.CurMagCount;
            report.StagedCapacity = magazine.MagSize;
            report.SourceUnchanged =
                source.LoadedAmmoCount == sourceLoadedBefore &&
                source.SelectedAmmoDefName == sourceSelectedBefore;
            report.Ready = report.SourceUnchanged &&
                report.StagedLoadedCount == report.SourceLoadedCount &&
                report.StagedCapacity == report.SourceCapacity;
            if (!report.Ready)
            {
                report.Failure = "migration-conservation-check-failed";
            }

            return report;
        }
    }
}
