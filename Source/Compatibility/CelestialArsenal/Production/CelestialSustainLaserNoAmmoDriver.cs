using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CelestialArsenal
{
    /// <summary>
    /// Explicit neutral facets for an energy weapon with no magazine or reload transaction.
    /// </summary>
    internal sealed class CelestialSustainLaserNoAmmoDriver :
        IShuttleWeaponAmmoReadDriver,
        IShuttleWeaponAmmoCommandDriver,
        IShuttleWeaponManualReloadDriver,
        IShuttleWeaponRemovalRefundDriver
    {
        private const string NoAmmoReason = "particle-lance-no-ammunition-system";

        public bool TryBuildSnapshot(
            ShuttleModuleRuntimeContext context,
            out ShuttleWeaponAmmoSnapshot snapshot)
        {
            snapshot = new ShuttleWeaponAmmoSnapshot();
            snapshot.HasAmmoSystem = false;
            return true;
        }

        public bool TrySelectAmmo(
            string moduleInstanceID,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState weaponState,
            string ammoDefName,
            out string failureReason)
        {
            return Reject(out failureReason);
        }

        public bool TryRequestReload(
            string moduleInstanceID,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState weaponState,
            ShuttleWeaponReloadRequestKind requestKind,
            out string failureReason)
        {
            return Reject(out failureReason);
        }

        public bool TryCancelReload(
            string moduleInstanceID,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState weaponState,
            out string failureReason)
        {
            return Reject(out failureReason);
        }

        public bool TrySetAutoReloadEnabled(
            string moduleInstanceID,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState weaponState,
            bool enabled,
            out string failureReason)
        {
            return Reject(out failureReason);
        }

        public bool TrySetManualReloadAllowed(
            string moduleInstanceID,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState weaponState,
            bool enabled,
            out string failureReason)
        {
            return Reject(out failureReason);
        }

        public bool TrySetLogisticsAutoFeedEnabled(
            string moduleInstanceID,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState weaponState,
            bool enabled,
            out string failureReason)
        {
            return Reject(out failureReason);
        }

        public bool TryGetPlan(
            ShuttleModuleRuntimeContext context,
            out ShuttleWeaponManualReloadPlan plan,
            out string failureReason)
        {
            plan = null;
            return Reject(out failureReason);
        }

        public bool TryClaim(
            ShuttleModuleRuntimeContext context,
            Pawn pawn,
            int jobLoadID,
            ThingDef requiredAmmoThingDef,
            out ShuttleWeaponManualReloadPlan plan,
            out string failureReason)
        {
            plan = null;
            return Reject(out failureReason);
        }

        public bool TryComplete(
            ShuttleModuleRuntimeContext context,
            Pawn pawn,
            int jobLoadID,
            out string failureReason)
        {
            return Reject(out failureReason);
        }

        public bool TryCollect(
            ShuttleModuleRuntimeContext context,
            ShuttleModuleRemovalRefundCollector collector,
            out string failureReason)
        {
            failureReason = null;
            return true;
        }

        private static bool Reject(out string failureReason)
        {
            failureReason = NoAmmoReason;
            return false;
        }
    }
}
