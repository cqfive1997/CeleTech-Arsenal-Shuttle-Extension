using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    /// <summary>
    /// Durable private payload for one installed shuttle weapon module.
    /// It owns only runtime backend objects and hidden cycle fields; UI/profile/launch snapshots
    /// must not expose warmup, cooldown, targets, or gun internals.
    /// </summary>
    internal sealed class ShuttleWeaponRuntimeState : IShuttleModuleRuntimeState
    {
        private const int CurrentSaveVersion = 9;
        // Serialized compatibility discriminator. It identifies the core magazine authority,
        // not a selectable execution backend.
        internal const string CoreMagazineAuthorityId = "legacy";

        private int saveVersion = CurrentSaveVersion;
        private Thing gun;
        private int burstWarmupTicksLeft;
        private int burstCooldownTicksLeft;
        private int activePowerTicksLeft;
        private bool holdFire;
        private bool fireControlLinked = true;
        private ShuttleWeaponFireControlMode fireControlMode = ShuttleWeaponFireControlMode.AutoDefense;
        private ShuttleWeaponTargetPriority targetPriority = ShuttleWeaponTargetPriority.ClosestHostile;
        private bool autoFireEnabled = true;
        private LocalTargetInfo currentTarget = LocalTargetInfo.Invalid;
        private LocalTargetInfo forcedTarget = LocalTargetInfo.Invalid;
        private string lastForcedTargetFailureCode;
        private string magazineAuthorityBackendId;
        private ShuttleWeaponAmmoState ammo = new ShuttleWeaponAmmoState();
        private Thing boundGun;
        private Thing boundHost;
        // Muzzle cursor is runtime-only. It is not saved because exact barrel alternation
        // has no gameplay meaning across load/rebind and should not churn save data.
        private int nextMuzzleIndex;
        private IntVec3 lastResolvedMuzzleCell = IntVec3.Invalid;
        private Vector3 lastResolvedMuzzleDrawPos = Vector3.zero;
        private int idleTargetScanPhase = -1;
        private int idleTargetScanPhaseInterval = -1;
        // Backend selection is derived from the installed module Def and active runtime
        // environment. It is deliberately not saved and is rebuilt after load/reconcile.
        private ShuttleWeaponBackendBinding backendBinding;
        private string backendBindingWeaponDefName;

        internal Thing GunForRuntimeOnly
        {
            get
            {
                return this.gun;
            }
            set
            {
                this.gun = value;
            }
        }

        internal ShuttleWeaponAmmoState AmmoForRuntimeOnly
        {
            get
            {
                if (this.ammo == null)
                {
                    this.ammo = new ShuttleWeaponAmmoState();
                }

                return this.ammo;
            }
        }

        internal string MagazineAuthorityBackendIdForRuntimeOnly
        {
            get
            {
                return string.IsNullOrEmpty(this.magazineAuthorityBackendId)
                    ? CoreMagazineAuthorityId
                    : this.magazineAuthorityBackendId;
            }
        }

        internal bool IsMagazineAuthorityTransferIdleForRuntimeOnly()
        {
            if (this.MagazineAuthorityBackendIdForRuntimeOnly !=
                CoreMagazineAuthorityId ||
                this.burstWarmupTicksLeft > 0 ||
                this.burstCooldownTicksLeft > 0 ||
                this.activePowerTicksLeft > 0)
            {
                return false;
            }

            ShuttleWeaponAmmoState ammoState = this.AmmoForRuntimeOnly;
            if (ammoState.ReloadInProgress ||
                ammoState.ManualReloadJobActive ||
                ammoState.ReloadExecutorKind != ShuttleWeaponReloadExecutorKind.None)
            {
                return false;
            }

            ThingWithComps typedGun = this.gun as ThingWithComps;
            CompEquippable equippable = typedGun != null
                ? typedGun.TryGetComp<CompEquippable>()
                : null;
            Verb verb = equippable != null ? equippable.PrimaryVerb : null;
            return verb == null || verb.state != VerbState.Bursting;
        }

        internal bool TryCommitMagazineAuthorityTransferForRuntimeOnly(
            string expectedBackendId,
            string nextBackendId,
            Thing expectedGun,
            Thing nextGun,
            string expectedAmmoDefName,
            int expectedLoadedCount,
            out string failureReason)
        {
            failureReason = null;
            if (string.IsNullOrEmpty(expectedBackendId) ||
                string.IsNullOrEmpty(nextBackendId) ||
                nextGun == null ||
                nextGun.Destroyed ||
                this.MagazineAuthorityBackendIdForRuntimeOnly != expectedBackendId)
            {
                failureReason = "authority-transfer-context-changed";
                return false;
            }

            if (!ReferenceEquals(this.gun, expectedGun))
            {
                failureReason = "authority-transfer-gun-changed";
                return false;
            }

            if (this.burstWarmupTicksLeft > 0 ||
                this.burstCooldownTicksLeft > 0 ||
                this.activePowerTicksLeft > 0)
            {
                failureReason = "weapon-cycle-active";
                return false;
            }

            if (!this.AmmoForRuntimeOnly.TryRelinquishMagazineAuthorityForRuntimeOnly(
                    expectedAmmoDefName,
                    expectedLoadedCount,
                    out failureReason))
            {
                return false;
            }

            this.gun = nextGun;
            this.magazineAuthorityBackendId = nextBackendId == CoreMagazineAuthorityId
                ? null
                : nextBackendId;
            this.ResetCurrentTargetForRuntimeOnly();
            this.ClearVerbBindingForRuntimeOnly();
            this.ClearBackendBindingForRuntimeOnly();
            return true;
        }

        internal bool TryRollbackMagazineAuthorityTransferForRuntimeOnly(
            string expectedBackendId,
            string restoredBackendId,
            Thing expectedGun,
            Thing restoredGun,
            string restoredAmmoDefName,
            int restoredLoadedCount)
        {
            if (string.IsNullOrEmpty(expectedBackendId) ||
                string.IsNullOrEmpty(restoredBackendId) ||
                this.MagazineAuthorityBackendIdForRuntimeOnly != expectedBackendId ||
                !ReferenceEquals(this.gun, expectedGun) ||
                !this.AmmoForRuntimeOnly.TryRestoreRelinquishedMagazineForRuntimeOnly(
                    restoredAmmoDefName,
                    restoredLoadedCount))
            {
                return false;
            }

            this.gun = restoredGun;
            this.magazineAuthorityBackendId = restoredBackendId == CoreMagazineAuthorityId
                ? null
                : restoredBackendId;
            this.ResetCurrentTargetForRuntimeOnly();
            this.ClearVerbBindingForRuntimeOnly();
            this.ClearBackendBindingForRuntimeOnly();
            return true;
        }

        internal bool TryGetBackendBindingForRuntimeOnly(
            ShuttleWeaponModuleDef weaponDef,
            out ShuttleWeaponBackendBinding binding)
        {
            binding = null;
            string weaponDefName = weaponDef != null ? weaponDef.defName : null;
            if (this.backendBinding == null ||
                string.IsNullOrEmpty(weaponDefName) ||
                this.backendBindingWeaponDefName != weaponDefName)
            {
                return false;
            }

            binding = this.backendBinding;
            return true;
        }

        internal void SetBackendBindingForRuntimeOnly(
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponBackendBinding binding)
        {
            this.backendBinding = binding;
            this.backendBindingWeaponDefName = weaponDef != null ? weaponDef.defName : null;
        }

        internal void ClearBackendBindingForRuntimeOnly()
        {
            this.backendBinding = null;
            this.backendBindingWeaponDefName = null;
        }

        internal int GetWarmupTicksForRuntimeOnly()
        {
            return this.burstWarmupTicksLeft;
        }

        internal int GetCooldownTicksForRuntimeOnly()
        {
            return this.burstCooldownTicksLeft;
        }

        internal int GetActivePowerTicksForRuntimeOnly()
        {
            return this.activePowerTicksLeft;
        }

        internal void SetWarmupTicksForRuntimeOnly(int ticks)
        {
            this.burstWarmupTicksLeft = Max(0, ticks);
        }

        internal void SetCooldownTicksForRuntimeOnly(int ticks)
        {
            this.burstCooldownTicksLeft = Max(0, ticks);
        }

        internal void SetActivePowerTicksForRuntimeOnly(int ticks)
        {
            this.activePowerTicksLeft = Max(0, ticks);
        }

        internal LocalTargetInfo GetCurrentTargetForRuntimeOnly()
        {
            return this.currentTarget;
        }

        internal void SetCurrentTargetForRuntimeOnly(LocalTargetInfo target)
        {
            this.currentTarget = target.IsValid ? target : LocalTargetInfo.Invalid;
        }

        internal LocalTargetInfo GetForcedTargetForRuntimeOnly()
        {
            return this.forcedTarget;
        }

        internal void SetForcedTargetForRuntimeOnly(LocalTargetInfo target)
        {
            this.forcedTarget = target.IsValid ? target : LocalTargetInfo.Invalid;
            if (this.forcedTarget.IsValid)
            {
                this.lastForcedTargetFailureCode = null;
            }
        }

        internal string GetLastForcedTargetFailureForRuntimeOnly()
        {
            return this.lastForcedTargetFailureCode;
        }

        internal void SetLastForcedTargetFailureForRuntimeOnly(string reasonCode)
        {
            this.lastForcedTargetFailureCode = string.IsNullOrEmpty(reasonCode)
                ? null
                : reasonCode;
        }

        internal void ClearLastForcedTargetFailureForRuntimeOnly()
        {
            this.lastForcedTargetFailureCode = null;
        }

        internal bool HasForcedTargetForRuntimeOnly()
        {
            return this.forcedTarget.IsValid;
        }

        internal bool GetHoldFireForRuntimeOnly()
        {
            return this.holdFire;
        }

        internal bool FireControlLinked
        {
            get
            {
                return this.fireControlLinked;
            }
        }

        internal ShuttleWeaponFireControlMode FireControlMode
        {
            get
            {
                return this.fireControlMode;
            }
        }

        internal ShuttleWeaponTargetPriority TargetPriority
        {
            get
            {
                return this.targetPriority;
            }
        }

        internal bool AutoFireEnabled
        {
            get
            {
                return this.autoFireEnabled;
            }
        }

        internal void SetHoldFireForRuntimeOnly(bool value)
        {
            this.holdFire = value;
            if (value)
            {
                this.ResetCurrentTargetForRuntimeOnly();
                this.SetWarmupTicksForRuntimeOnly(0);
            }
        }

        internal void SetFireControlLinked(bool linked)
        {
            this.fireControlLinked = linked;
        }

        internal void SetFireControlMode(ShuttleWeaponFireControlMode mode)
        {
            this.fireControlMode = SanitizeFireControlMode(mode);
        }

        internal void SetTargetPriority(ShuttleWeaponTargetPriority priority)
        {
            this.targetPriority = SanitizeTargetPriority(priority);
        }

        internal void SetAutoFireEnabled(bool enabled)
        {
            this.autoFireEnabled = enabled;
        }

        public void EnsureInitialized()
        {
            this.SanitizeForRuntimeOnly();
        }

        internal void ResetCurrentTargetForRuntimeOnly()
        {
            this.currentTarget = LocalTargetInfo.Invalid;
        }

        internal void ResetForcedTargetForRuntimeOnly()
        {
            this.ClearForcedTargetForRuntimeOnly();
        }

        internal void ClearForcedTargetForRuntimeOnly()
        {
            this.forcedTarget = LocalTargetInfo.Invalid;
        }

        internal bool HasWarmupTicksForRuntimeOnly()
        {
            return this.burstWarmupTicksLeft > 0;
        }

        internal bool HasActivePowerTicksForRuntimeOnly()
        {
            return this.activePowerTicksLeft > 0;
        }

        internal bool HasCooldownTicksForRuntimeOnly()
        {
            return this.burstCooldownTicksLeft > 0;
        }

        internal bool IsHoldFireForRuntimeOnly()
        {
            return this.holdFire;
        }

        internal bool AreVerbsBoundForRuntimeOnly(ThingWithComps host, Thing gun)
        {
            return host != null && gun != null && this.boundHost == host && this.boundGun == gun;
        }

        internal void MarkVerbsBoundForRuntimeOnly(ThingWithComps host, Thing gun)
        {
            this.boundHost = host;
            this.boundGun = gun;
        }

        internal void ClearVerbBindingForRuntimeOnly()
        {
            this.boundHost = null;
            this.boundGun = null;
        }

        internal int GetCurrentMuzzleIndexForRuntimeOnly(int muzzleCount)
        {
            if (muzzleCount <= 0)
            {
                return 0;
            }

            if (this.nextMuzzleIndex < 0)
            {
                this.nextMuzzleIndex = 0;
            }

            if (this.nextMuzzleIndex >= muzzleCount)
            {
                this.nextMuzzleIndex %= muzzleCount;
            }

            return this.nextMuzzleIndex;
        }

        internal int GetAndAdvanceMuzzleIndexForRuntimeOnly(int muzzleCount)
        {
            if (muzzleCount <= 0)
            {
                return 0;
            }

            int index = this.GetCurrentMuzzleIndexForRuntimeOnly(muzzleCount);
            this.nextMuzzleIndex = (index + 1) % muzzleCount;
            return index;
        }

        internal void SetLastResolvedMuzzleForRuntimeOnly(IntVec3 cell, Vector3 drawPos)
        {
            this.lastResolvedMuzzleCell = cell.IsValid ? cell : IntVec3.Invalid;
            this.lastResolvedMuzzleDrawPos = drawPos;
        }

        internal IntVec3 GetLastResolvedMuzzleCellForRuntimeOnly()
        {
            return this.lastResolvedMuzzleCell;
        }

        internal Vector3 GetLastResolvedMuzzleDrawPosForRuntimeOnly()
        {
            return this.lastResolvedMuzzleDrawPos;
        }

        internal int GetIdleTargetScanPhaseForRuntimeOnly(
            string moduleInstanceID,
            int interval)
        {
            if (interval <= 1)
            {
                return 0;
            }

            if (this.idleTargetScanPhase >= 0 &&
                this.idleTargetScanPhase < interval &&
                this.idleTargetScanPhaseInterval == interval)
            {
                return this.idleTargetScanPhase;
            }

            int hash = !string.IsNullOrEmpty(moduleInstanceID)
                ? GenText.StableStringHash(moduleInstanceID)
                : 0;
            int phase = hash % interval;
            if (phase < 0)
            {
                phase += interval;
            }

            this.idleTargetScanPhase = phase;
            this.idleTargetScanPhaseInterval = interval;
            return this.idleTargetScanPhase;
        }

        internal void SanitizeForRuntimeOnly()
        {
            this.burstWarmupTicksLeft = Max(0, this.burstWarmupTicksLeft);
            this.burstCooldownTicksLeft = Max(0, this.burstCooldownTicksLeft);
            this.activePowerTicksLeft = Max(0, this.activePowerTicksLeft);
            this.fireControlMode = SanitizeFireControlMode(this.fireControlMode);
            this.targetPriority = SanitizeTargetPriority(this.targetPriority);
            this.AmmoForRuntimeOnly.SanitizeBasic();

            if (!this.currentTarget.IsValid)
            {
                this.currentTarget = LocalTargetInfo.Invalid;
            }

            if (!this.forcedTarget.IsValid)
            {
                this.forcedTarget = LocalTargetInfo.Invalid;
            }

            if (string.IsNullOrEmpty(this.lastForcedTargetFailureCode))
            {
                this.lastForcedTargetFailureCode = null;
            }

            if (string.IsNullOrEmpty(this.magazineAuthorityBackendId) ||
                this.magazineAuthorityBackendId == CoreMagazineAuthorityId)
            {
                this.magazineAuthorityBackendId = null;
            }

            if (this.nextMuzzleIndex < 0)
            {
                this.nextMuzzleIndex = 0;
            }

            if (this.idleTargetScanPhase < -1)
            {
                this.idleTargetScanPhase = -1;
            }

            if (this.idleTargetScanPhaseInterval < -1)
            {
                this.idleTargetScanPhaseInterval = -1;
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.saveVersion, "saveVersion", 0);
            Scribe_Deep.Look(ref this.gun, "gun");
            Scribe_Values.Look(ref this.burstWarmupTicksLeft, "burstWarmupTicksLeft", 0);
            Scribe_Values.Look(ref this.burstCooldownTicksLeft, "burstCooldownTicksLeft", 0);
            Scribe_Values.Look(ref this.activePowerTicksLeft, "activePowerTicksLeft", 0);
            Scribe_Values.Look(ref this.holdFire, "holdFire", false);
            Scribe_Values.Look(ref this.fireControlLinked, "fireControlLinked", true);
            Scribe_Values.Look(
                ref this.fireControlMode,
                "fireControlMode",
                ShuttleWeaponFireControlMode.AutoDefense);
            Scribe_Values.Look(
                ref this.targetPriority,
                "targetPriority",
                ShuttleWeaponTargetPriority.ClosestHostile);
            Scribe_Values.Look(ref this.autoFireEnabled, "autoFireEnabled", true);
            Scribe_TargetInfo.Look(ref this.currentTarget, "currentTarget");
            Scribe_TargetInfo.Look(ref this.forcedTarget, "forcedTarget");
            Scribe_Values.Look(
                ref this.lastForcedTargetFailureCode,
                "lastForcedTargetFailureCode");
            Scribe_Values.Look(
                ref this.magazineAuthorityBackendId,
                "magazineAuthorityBackendId");
            Scribe_Deep.Look(ref this.ammo, "ammo");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                int loadedVersion = this.saveVersion;

                this.WarnIfLoadedVersionIsNewer(loadedVersion);
                this.SanitizeForRuntimeOnly();
                this.saveVersion = CurrentSaveVersion;
            }
        }

        private static ShuttleWeaponFireControlMode SanitizeFireControlMode(ShuttleWeaponFireControlMode mode)
        {
            switch (mode)
            {
                case ShuttleWeaponFireControlMode.Offline:
                case ShuttleWeaponFireControlMode.ManualOnly:
                case ShuttleWeaponFireControlMode.AutoDefense:
                case ShuttleWeaponFireControlMode.PointDefense:
                    return mode;
                default:
                    return ShuttleWeaponFireControlMode.AutoDefense;
            }
        }

        private static ShuttleWeaponTargetPriority SanitizeTargetPriority(ShuttleWeaponTargetPriority priority)
        {
            switch (priority)
            {
                case ShuttleWeaponTargetPriority.ClosestHostile:
                case ShuttleWeaponTargetPriority.RaidersFirst:
                case ShuttleWeaponTargetPriority.MechanoidsFirst:
                case ShuttleWeaponTargetPriority.ManhuntersFirst:
                case ShuttleWeaponTargetPriority.HighThreatFirst:
                case ShuttleWeaponTargetPriority.ForcedTargetOnly:
                    return priority;
                default:
                    return ShuttleWeaponTargetPriority.ClosestHostile;
            }
        }

        private void WarnIfLoadedVersionIsNewer(int loadedVersion)
        {
            if (loadedVersion > CurrentSaveVersion)
            {
                Log.Warning("[CeleTech Shuttle] Loaded ShuttleWeaponRuntimeState save version " +
                    loadedVersion + " newer than supported version " + CurrentSaveVersion +
                    ". Keeping loaded weapon runtime state intact.");
            }
        }

        private static int Max(int a, int b)
        {
            return a > b ? a : b;
        }
    }
}
