using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Defs
{
    /// <summary>
    /// Static weapon module tuning. Runtime firing, targeting, spawned guns, and loaded ammo
    /// are owned by weapon runtime state rather than this Def.
    /// </summary>
    public sealed class ShuttleWeaponModuleDef : ShuttleModuleBaseDef
    {
        // Static role used by profile/UI summaries. It does not start targeting or firing by itself.
        public ShuttleWeaponRole weaponRole;
        // Def reference for the weapon archetype. No gun Thing is spawned or persisted by this Def.
        public ThingDef weaponDef;
        // Number of physical mounts represented by this module for capacity reporting.
        public int mountCount = 1;
        // Optional fallback muzzle source cells relative to the shuttle host position, authored
        // for north-facing shuttles and rotated at runtime with the host. Kun Peng visual turret
        // mounts normally provide the projectile origin instead.
        public List<IntVec3> muzzleCellOffsets;
        // Additional transient internal-bus demand while the weapon is actively cycling.
        // This is watts, not a one-shot Wd cost, and is not added to profile idle demand.
        public float firingPowerDrawWatts;
        // Runtime firing cadence tuning. Idle auto-scan uses this as the minimum effective scan interval.
        public int scanIntervalTicks = 30;
        public int warmupTicks = 30;
        public int cooldownTicks = 120;
        public bool canAutoFire = true;
        // Allows a constrained, pawn-only auto-defense fallback when no Fire Control Core link exists.
        public bool canAutoFireWithoutFireControl;
        // Extra automatic acquisition cap for the no-fire-control fallback. <= 0 uses weapon range.
        public float fallbackAutoFireRange;
        public bool canSetForcedTarget = true;
        public bool requiresScanner;
        public bool requiresNavigationComputer;
        // Projectile-domain point-defense tuning. These values do not affect ordinary hostile
        // Thing targeting and do not create runtime state by themselves.
        public bool canInterceptProjectiles;
        public bool pointDefenseExplosiveOrOverheadOnly = true;
        public float pointDefenseMaxIncomingSpeed;
        public float pointDefenseProtectionRadius;
        public int pointDefenseMaxAssignmentsPerThreat = 1;
        public int pointDefenseAssignmentLeaseTicks = 30;
        // Legacy/static capacity hint. Active ammo behavior is defined by
        // ShuttleWeaponModuleAmmoExtension and ShuttleWeaponAmmoState.
        public int maxAmmo;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }

            if (this.ModuleType != ShuttleModuleType.Weapon)
            {
                yield return this.defName + " must use moduleTypeID weapon.";
            }

            if (!this.ContainsOnlySegmentType(ShuttleSegmentType.Weapon))
            {
                yield return this.defName + " must install only into weapon segment type.";
            }

            if (!this.ContainsOnlyModuleSlotType(ShuttleModuleType.Weapon))
            {
                yield return this.defName + " must install only into weapon module slot type.";
            }

            if (this.weaponRole == ShuttleWeaponRole.Unknown)
            {
                yield return this.defName + " must define weaponRole.";
            }

            if (this.weaponDef == null)
            {
                yield return this.defName + " must define weaponDef.";
            }

            if (this.mountCount < 1)
            {
                yield return this.defName + " has mountCount less than 1.";
            }

            if (this.muzzleCellOffsets != null)
            {
                for (int i = 0; i < this.muzzleCellOffsets.Count; i++)
                {
                    if (!this.muzzleCellOffsets[i].IsValid)
                    {
                        yield return this.defName + " has invalid muzzleCellOffsets entry at index " + i + ".";
                    }
                }
            }

            if (!this.IsFiniteFloat(this.firingPowerDrawWatts))
            {
                yield return this.defName + " has non-finite firingPowerDrawWatts.";
            }
            else if (this.firingPowerDrawWatts < 0f)
            {
                yield return this.defName + " has negative firingPowerDrawWatts.";
            }

            if (this.scanIntervalTicks < 0)
            {
                yield return this.defName + " has negative scanIntervalTicks.";
            }

            if (this.warmupTicks < 0)
            {
                yield return this.defName + " has negative warmupTicks.";
            }

            if (this.cooldownTicks < 0)
            {
                yield return this.defName + " has negative cooldownTicks.";
            }

            if (!this.IsFiniteFloat(this.fallbackAutoFireRange))
            {
                yield return this.defName + " has non-finite fallbackAutoFireRange.";
            }
            else if (this.fallbackAutoFireRange < 0f)
            {
                yield return this.defName + " has negative fallbackAutoFireRange.";
            }

            if (this.maxAmmo < 0)
            {
                yield return this.defName + " has negative maxAmmo.";
            }

            if (!this.IsFiniteFloat(this.pointDefenseMaxIncomingSpeed) ||
                this.pointDefenseMaxIncomingSpeed < 0f)
            {
                yield return this.defName + " has invalid pointDefenseMaxIncomingSpeed.";
            }

            if (!this.IsFiniteFloat(this.pointDefenseProtectionRadius) ||
                this.pointDefenseProtectionRadius < 0f)
            {
                yield return this.defName + " has invalid pointDefenseProtectionRadius.";
            }

            if (this.canInterceptProjectiles && this.pointDefenseProtectionRadius <= 0f)
            {
                yield return this.defName + " must define positive pointDefenseProtectionRadius when projectile interception is enabled.";
            }

            if (this.pointDefenseMaxAssignmentsPerThreat < 1)
            {
                yield return this.defName + " has pointDefenseMaxAssignmentsPerThreat less than 1.";
            }

            if (this.pointDefenseAssignmentLeaseTicks < 1)
            {
                yield return this.defName + " has pointDefenseAssignmentLeaseTicks less than 1.";
            }
        }

        private bool ContainsOnlySegmentType(ShuttleSegmentType expectedType)
        {
            IReadOnlyList<ShuttleSegmentType> segmentTypes = this.InstallableSegmentTypeEnums;
            if (segmentTypes == null || segmentTypes.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < segmentTypes.Count; i++)
            {
                if (segmentTypes[i] != expectedType)
                {
                    return false;
                }
            }

            return true;
        }

        private bool ContainsOnlyModuleSlotType(ShuttleModuleType expectedType)
        {
            IReadOnlyList<ShuttleModuleType> slotTypes = this.InstallableModuleSlotTypeEnums;
            if (slotTypes == null || slotTypes.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < slotTypes.Count; i++)
            {
                if (slotTypes[i] != expectedType)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
