using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon
{
    internal sealed class ShuttleWeaponMuzzleResolver
    {
        internal ShuttleWeaponMuzzleSource Resolve(
            ThingWithComps host,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState runtimeState)
        {
            return this.Resolve(host, null, weaponDef, runtimeState, LocalTargetInfo.Invalid, false);
        }

        internal ShuttleWeaponMuzzleSource Resolve(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState runtimeState,
            LocalTargetInfo target)
        {
            return this.Resolve(
                context != null ? context.Host : null,
                context != null ? context.ParentSlotID : null,
                weaponDef,
                runtimeState,
                target,
                false);
        }

        internal ShuttleWeaponMuzzleSource ResolveAndAdvance(
            ThingWithComps host,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState runtimeState)
        {
            // Use this only for the committed shot. Validation calls Resolve so repeated checks
            // cannot rotate the muzzle index before a projectile actually launches.
            return this.Resolve(host, null, weaponDef, runtimeState, LocalTargetInfo.Invalid, true);
        }

        internal ShuttleWeaponMuzzleSource ResolveAndAdvance(
            ShuttleModuleRuntimeContext context,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState runtimeState,
            LocalTargetInfo target)
        {
            // Use this only for the committed shot. Validation calls Resolve so repeated checks
            // cannot rotate the muzzle index before a projectile actually launches.
            return this.Resolve(
                context != null ? context.Host : null,
                context != null ? context.ParentSlotID : null,
                weaponDef,
                runtimeState,
                target,
                true);
        }

        internal ShuttleWeaponMuzzleSource Resolve(
            ThingWithComps host,
            string parentSlotID,
            ShuttleWeaponModuleDef weaponDef,
            ShuttleWeaponRuntimeState runtimeState,
            LocalTargetInfo target,
            bool advanceMuzzle)
        {
            if (host == null)
            {
                return ShuttleWeaponMuzzleSource.Fallback(IntVec3.Invalid, Vector3.zero);
            }

            Map map = host.Map;
            ShuttleWeaponMuzzleSource visualSource;
            if (this.TryResolveVisualTurretMuzzle(
                    host,
                    parentSlotID,
                    runtimeState,
                    target,
                    map,
                    out visualSource))
            {
                return visualSource;
            }

            IReadOnlyList<IntVec3> offsets = weaponDef != null ? weaponDef.muzzleCellOffsets : null;
            if (offsets == null || offsets.Count == 0 || runtimeState == null)
            {
                return ShuttleWeaponMuzzleSource.Fallback(host.Position, this.GetHostDrawPos(host));
            }

            int index = advanceMuzzle
                ? runtimeState.GetAndAdvanceMuzzleIndexForRuntimeOnly(offsets.Count)
                : runtimeState.GetCurrentMuzzleIndexForRuntimeOnly(offsets.Count);
            if (index < 0 || index >= offsets.Count)
            {
                return ShuttleWeaponMuzzleSource.Fallback(host.Position, this.GetHostDrawPos(host));
            }

            IntVec3 offset = offsets[index];
            if (!offset.IsValid)
            {
                return ShuttleWeaponMuzzleSource.Fallback(host.Position, this.GetHostDrawPos(host));
            }

            IntVec3 rotatedOffset = RotateOffset(offset, host.Rotation);
            IntVec3 sourceCell = host.Position + rotatedOffset;
            if (!sourceCell.IsValid || (map != null && !sourceCell.InBounds(map)))
            {
                return ShuttleWeaponMuzzleSource.Fallback(host.Position, this.GetHostDrawPos(host));
            }

            return new ShuttleWeaponMuzzleSource(
                sourceCell,
                sourceCell.ToVector3Shifted(),
                false,
                index);
        }

        private bool TryResolveVisualTurretMuzzle(
            ThingWithComps host,
            string parentSlotID,
            ShuttleWeaponRuntimeState runtimeState,
            LocalTargetInfo target,
            Map map,
            out ShuttleWeaponMuzzleSource source)
        {
            source = default(ShuttleWeaponMuzzleSource);
            if (host == null)
            {
                return false;
            }

            CompModularShuttleWeaponTurretVisual turretVisual =
                host.TryGetComp<CompModularShuttleWeaponTurretVisual>();
            CompProperties_ModularShuttleWeaponTurretVisual props =
                turretVisual != null
                    ? turretVisual.props as CompProperties_ModularShuttleWeaponTurretVisual
                    : null;
            if (props == null || props.mounts == null || props.mounts.Count == 0)
            {
                return false;
            }

            int parentSlotIndex = ParseParentSlotIndex(parentSlotID);
            if (parentSlotIndex < 0)
            {
                return false;
            }

            ShuttleWeaponTurretVisualMount mount =
                this.FindMountForParentSlotIndex(props.mounts, parentSlotIndex);
            if (mount == null)
            {
                return false;
            }

            Vector3 mountPosition = this.ResolveMountWorldPosition(host, mount);
            float aimAngle;
            if (!this.TryResolveAimAngle(host, mountPosition, runtimeState, target, out aimAngle))
            {
                aimAngle = ShuttleWeaponTurretAirframe.RotateEastAuthoredAngle(
                    mount.idleAngleEast,
                    host.Rotation);
            }

            float muzzleForwardOffset = this.ResolveMuzzleForwardOffset(props, mount);
            Vector3 muzzleDrawPos = mountPosition +
                ShuttleWeaponTurretAirframe.DirectionFromAngle(aimAngle) * muzzleForwardOffset;
            IntVec3 muzzleCell = muzzleDrawPos.ToIntVec3();
            if (!muzzleCell.IsValid || (map != null && !muzzleCell.InBounds(map)))
            {
                return false;
            }

            source = new ShuttleWeaponMuzzleSource(
                muzzleCell,
                muzzleDrawPos,
                false,
                0);
            return true;
        }

        private ShuttleWeaponTurretVisualMount FindMountForParentSlotIndex(
            IReadOnlyList<ShuttleWeaponTurretVisualMount> mounts,
            int parentSlotIndex)
        {
            if (mounts == null || parentSlotIndex < 0)
            {
                return null;
            }

            for (int i = 0; i < mounts.Count; i++)
            {
                ShuttleWeaponTurretVisualMount mount = mounts[i];
                if (mount != null && mount.parentSlotIndex == parentSlotIndex)
                {
                    return mount;
                }
            }

            return null;
        }

        private Vector3 ResolveMountWorldPosition(
            ThingWithComps host,
            ShuttleWeaponTurretVisualMount mount)
        {
            return ShuttleWeaponTurretAirframe.ResolveHostMountWorldPosition(host, mount);
        }

        private bool TryResolveAimAngle(
            ThingWithComps host,
            Vector3 mountPosition,
            ShuttleWeaponRuntimeState runtimeState,
            LocalTargetInfo target,
            out float aimAngle)
        {
            aimAngle = 0f;
            LocalTargetInfo aimTarget = target.IsValid
                ? target
                : this.ResolveRuntimeTarget(runtimeState);
            if (!aimTarget.IsValid)
            {
                return false;
            }

            Vector3 targetPosition = this.ResolveTargetPosition(host, aimTarget);
            Vector3 direction = targetPosition - mountPosition;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            aimAngle = ShuttleWeaponTurretAirframe.NormalizeAngle(
                Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg);
            return true;
        }

        private LocalTargetInfo ResolveRuntimeTarget(ShuttleWeaponRuntimeState runtimeState)
        {
            if (runtimeState == null)
            {
                return LocalTargetInfo.Invalid;
            }

            LocalTargetInfo currentTarget = runtimeState.GetCurrentTargetForRuntimeOnly();
            if (currentTarget.IsValid)
            {
                return currentTarget;
            }

            LocalTargetInfo forcedTarget = runtimeState.GetForcedTargetForRuntimeOnly();
            return forcedTarget.IsValid ? forcedTarget : LocalTargetInfo.Invalid;
        }

        private Vector3 ResolveTargetPosition(ThingWithComps host, LocalTargetInfo target)
        {
            if (target.HasThing && target.Thing != null && target.Thing.Spawned)
            {
                return target.Thing.DrawPos;
            }

            if (target.Cell.IsValid)
            {
                return target.Cell.ToVector3Shifted();
            }

            return this.GetHostDrawPos(host);
        }

        private float ResolveMuzzleForwardOffset(
            CompProperties_ModularShuttleWeaponTurretVisual props,
            ShuttleWeaponTurretVisualMount mount)
        {
            float value = mount != null && mount.muzzleForwardOffset >= 0f
                ? mount.muzzleForwardOffset
                : props != null ? props.muzzleForwardOffset : 0f;
            if (!IsFinite(value) || value < 0f)
            {
                return 0f;
            }

            return value;
        }

        private Vector3 GetHostDrawPos(ThingWithComps host)
        {
            if (host == null)
            {
                return Vector3.zero;
            }

            return host.Spawned ? host.DrawPos : host.Position.ToVector3Shifted();
        }

        private static int ParseParentSlotIndex(string parentSlotID)
        {
            const string Marker = ".slot.";
            if (string.IsNullOrEmpty(parentSlotID))
            {
                return -1;
            }

            int markerIndex = parentSlotID.LastIndexOf(Marker);
            if (markerIndex < 0)
            {
                return -1;
            }

            int index = markerIndex + Marker.Length;
            if (index >= parentSlotID.Length)
            {
                return -1;
            }

            int value = 0;
            for (; index < parentSlotID.Length; index++)
            {
                char c = parentSlotID[index];
                if (c < '0' || c > '9')
                {
                    return -1;
                }

                int digit = c - '0';
                if (value > (int.MaxValue - digit) / 10)
                {
                    return -1;
                }

                value = (value * 10) + digit;
            }

            return value;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static IntVec3 RotateOffset(IntVec3 offset, Rot4 rotation)
        {
            if (rotation == Rot4.East)
            {
                return new IntVec3(offset.z, offset.y, -offset.x);
            }

            if (rotation == Rot4.South)
            {
                return new IntVec3(-offset.x, offset.y, -offset.z);
            }

            if (rotation == Rot4.West)
            {
                return new IntVec3(-offset.z, offset.y, offset.x);
            }

            return offset;
        }
    }

    internal struct ShuttleWeaponMuzzleSource
    {
        internal readonly IntVec3 Cell;
        internal readonly Vector3 DrawPos;
        internal readonly bool UsesFallback;
        internal readonly int MuzzleIndex;

        internal ShuttleWeaponMuzzleSource(
            IntVec3 cell,
            Vector3 drawPos,
            bool usesFallback,
            int muzzleIndex)
        {
            this.Cell = cell;
            this.DrawPos = drawPos;
            this.UsesFallback = usesFallback;
            this.MuzzleIndex = muzzleIndex;
        }

        internal static ShuttleWeaponMuzzleSource Fallback(IntVec3 cell, Vector3 drawPos)
        {
            return new ShuttleWeaponMuzzleSource(cell, drawPos, true, 0);
        }
    }
}
