using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using CombatExtended;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended
{
    /// <summary>
    /// Quotes the sole CE magazine before removal. The existing removal transaction remains
    /// responsible for creating, holding, depositing or dropping the refund Things.
    /// </summary>
    internal sealed class CeWeaponRemovalRefundGuard : IShuttleWeaponRemovalRefundDriver
    {
        public bool TryCollect(
            ShuttleModuleRuntimeContext context,
            ShuttleModuleRemovalRefundCollector collector,
            out string failureReason)
        {
            failureReason = null;
            ShuttleWeaponRuntimeState state = context != null
                ? context.State as ShuttleWeaponRuntimeState
                : null;
            CompAmmoUser magazine = state != null
                ? CeWeaponRuntimeGunAccess.GetMagazine(state.GunForRuntimeOnly)
                : null;
            if (state == null || magazine == null ||
                state.MagazineAuthorityBackendIdForRuntimeOnly != CeWeaponBackendFactory.Id)
            {
                failureReason = "CT_Shuttle_WeaponAmmo_RemovalRefundUnavailable"
                    .Translate()
                    .ToString();
                return false;
            }

            int loadedCount = magazine.CurMagCount;
            if (loadedCount <= 0)
            {
                return true;
            }

            AmmoDef ammoDef = magazine.SelectedAmmo ?? magazine.CurrentAmmo;
            CeWeaponCompatibilitySpec spec = context.ModuleDef != null
                ? CeWeaponCompatibilitySpec.ForWeapon(context.ModuleDef.defName)
                : null;
            if (ammoDef == null || spec == null ||
                ammoDef.defName != spec.AmmoDefName ||
                magazine.CurrentAmmo != ammoDef ||
                magazine.SelectedAmmo != ammoDef ||
                loadedCount > magazine.MagSize ||
                collector == null)
            {
                failureReason = "CT_Shuttle_WeaponAmmo_RemovalRefundUnavailable"
                    .Translate()
                    .ToString();
                return false;
            }

            collector.Add(ammoDef, loadedCount);
            return true;
        }
    }
}
