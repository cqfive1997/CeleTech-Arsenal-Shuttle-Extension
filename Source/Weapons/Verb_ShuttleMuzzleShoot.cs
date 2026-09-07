using System;
using System.Reflection;
using UnityEngine;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Weapons
{
    /// <summary>
    /// Shuttle projectile verb that keeps the shuttle host as caster while letting weapon runtime
    /// provide the muzzle cell and draw origin. Vanilla Verb_LaunchProjectile has no public hook
    /// for a non-caster source cell, so this mirrors the narrow launch path used by projectiles.
    /// The base Verb burst scheduler owns soundCast/soundCastTail after a successful shot.
    /// </summary>
    public class Verb_ShuttleMuzzleShoot : Verb_LaunchProjectile, IShuttleMuzzleVerb
    {
        private const float DirectFireMissRadius = 2f;

        private IntVec3 shuttleMuzzleCell = IntVec3.Invalid;
        private Vector3 shuttleMuzzleDrawPos = Vector3.zero;
        private bool hasShuttleMuzzleSource;
        private float directFireAccuracyMultiplier = 1f;
        private float directFireAccuracyBonus;
        private float directFireAccuracyFloor;
        private float forcedMissRadiusMultiplier = 1f;
        private int lastShuttleShotTick = -1;
        private int shotsFiredInCurrentBurst;

        public int LastShuttleShotTick
        {
            get { return this.lastShuttleShotTick; }
        }

        internal int ShotsFiredInCurrentBurst
        {
            get { return this.shotsFiredInCurrentBurst; }
        }

        internal int EffectiveShotsPerBurstForDiagnostics
        {
            get { return this.ShotsPerBurst; }
        }

        protected override int ShotsPerBurst
        {
            // Verb_LaunchProjectile defaults to one shot. Shuttle guns author ordinary ranged
            // burst counts, so preserve the exact Verb_Shoot contract without changing the
            // custom shuttle muzzle/launch path.
            get { return base.BurstShotCount; }
        }

        public override void WarmupComplete()
        {
            this.shotsFiredInCurrentBurst = 0;
            base.WarmupComplete();
        }

        public void SetShuttleMuzzleSource(IntVec3 sourceCell, Vector3 sourceDrawPos)
        {
            this.shuttleMuzzleCell = sourceCell.IsValid ? sourceCell : IntVec3.Invalid;
            this.shuttleMuzzleDrawPos = sourceDrawPos;
            this.hasShuttleMuzzleSource = sourceCell.IsValid;
        }

        public void SetShuttleFireControlTuning(
            float directFireAccuracyMultiplier,
            float directFireAccuracyBonus,
            float directFireAccuracyFloor,
            float forcedMissRadiusMultiplier)
        {
            this.directFireAccuracyMultiplier = this.SanitizeNonNegativeFinite(
                directFireAccuracyMultiplier,
                1f);
            this.directFireAccuracyBonus = this.SanitizeFinite(
                directFireAccuracyBonus,
                0f);
            this.directFireAccuracyFloor = Mathf.Clamp01(this.SanitizeNonNegativeFinite(
                directFireAccuracyFloor,
                0f));
            this.forcedMissRadiusMultiplier = this.SanitizeNonNegativeFinite(
                forcedMissRadiusMultiplier,
                1f);
        }

        protected override bool TryCastShot()
        {
            if (this.caster == null)
            {
                Log.WarningOnce(
                    "[CeleTech Shuttle] Shuttle muzzle verb cannot fire without a caster.",
                    77362001);
                return false;
            }

            Map map = this.caster.Map;
            if (map == null)
            {
                Log.WarningOnce(
                    "[CeleTech Shuttle] Shuttle muzzle verb cannot fire without a caster map.",
                    77362002);
                return false;
            }

            if (this.currentTarget.HasThing &&
                this.currentTarget.Thing != null &&
                this.currentTarget.Thing.Map != map)
            {
                Log.WarningOnce(
                    "[CeleTech Shuttle] Shuttle muzzle verb target is on a different map.",
                    77362003);
                return false;
            }

            ThingDef projectileDef = this.Projectile;
            if (projectileDef == null)
            {
                Log.WarningOnce(
                    "[CeleTech Shuttle] Shuttle muzzle verb has no projectile def.",
                    77362004);
                return false;
            }

            IntVec3 sourceCell = this.ResolveSourceCell(map);
            ShootLine shootLine;
            if (!this.TryFindShootLineFromTo(
                sourceCell,
                this.currentTarget,
                out shootLine,
                false))
            {
                return false;
            }

            Thing projectileThing = this.MakeProjectileThing(projectileDef);
            if (projectileThing == null)
            {
                return false;
            }

            bool useMissHitFlags;
            LocalTargetInfo launchTarget = this.ResolveLaunchTarget(out useMissHitFlags);
            ProjectileHitFlags hitFlags = this.ResolveHitFlags(useMissHitFlags);
            Vector3 sourceDrawPos = this.ResolveSourceDrawPos(sourceCell);

            Projectile projectile = projectileThing as Projectile;
            if (projectile != null)
            {
                GenSpawn.Spawn(projectile, shootLine.Source, map);
                projectile.Launch(
                    this.caster,
                    sourceDrawPos,
                    launchTarget,
                    this.currentTarget,
                    hitFlags,
                    this.preventFriendlyFire,
                    this.EquipmentSource,
                    null);
                this.ThrowShuttleMuzzleFlash(sourceDrawPos, map);
                this.RecordShuttleShot();
                return true;
            }

            if (this.TryLaunchCombatExtendedProjectile(
                projectileThing,
                projectileDef,
                shootLine.Source,
                map,
                sourceDrawPos,
                launchTarget,
                this.currentTarget))
            {
                this.ThrowShuttleMuzzleFlash(sourceDrawPos, map);
                this.RecordShuttleShot();
                return true;
            }

            this.WarnUnsupportedProjectileThing(projectileDef, projectileThing);
            return false;
        }

        private void RecordShuttleShot()
        {
            this.shotsFiredInCurrentBurst++;
            this.lastShuttleShotTick = Find.TickManager != null
                ? Find.TickManager.TicksGame
                : -1;
        }

        private Thing MakeProjectileThing(ThingDef projectileDef)
        {
            Thing thing = projectileDef != null
                ? ThingMaker.MakeThing(projectileDef)
                : null;
            if (thing != null)
            {
                return thing;
            }

            string defName = projectileDef != null ? projectileDef.defName : "<null>";
            Log.WarningOnce(
                "[CeleTech Shuttle] Shuttle muzzle projectile def " +
                defName +
                " did not create a projectile thing.",
                WarningKeyFor(defName, 77362008));
            return null;
        }

        private bool TryLaunchCombatExtendedProjectile(
            Thing projectileThing,
            ThingDef projectileDef,
            IntVec3 sourceCell,
            Map map,
            Vector3 sourceDrawPos,
            LocalTargetInfo launchTarget,
            LocalTargetInfo intendedTarget)
        {
            if (!IsCombatExtendedProjectile(projectileThing))
            {
                return false;
            }

            MethodInfo longLaunchMethod = FindCombatExtendedLongLaunchMethod(projectileThing.GetType());
            MethodInfo shortLaunchMethod = FindCombatExtendedShortLaunchMethod(projectileThing.GetType());
            string defName = projectileDef != null ? projectileDef.defName : "<null>";
            if (longLaunchMethod == null && shortLaunchMethod == null)
            {
                Log.WarningOnce(
                    "[CeleTech Shuttle] Combat Extended projectile " +
                    defName +
                    " does not expose a supported Launch signature. The shot was cancelled before spawning.",
                    WarningKeyFor(defName, 77362006));
                return false;
            }

            Vector2 origin = new Vector2(sourceDrawPos.x, sourceDrawPos.z);
            Vector3 destinationVector = launchTarget.IsValid
                ? launchTarget.CenterVector3
                : sourceDrawPos;
            float shotHeight = ResolveCombatExtendedShotHeight(projectileThing, this.caster, sourceDrawPos);
            float targetHeight = ResolveCombatExtendedTargetHeight(projectileThing, launchTarget);
            Vector3 sourceVector = new Vector3(sourceDrawPos.x, shotHeight, sourceDrawPos.z);
            Vector3 targetVector = new Vector3(destinationVector.x, targetHeight, destinationVector.z);
            float shotSpeed = ResolveCombatExtendedShotSpeed(projectileDef);
            float shotAngle = ResolveCombatExtendedShotAngle(projectileDef, sourceVector, targetVector, shotSpeed);
            float shotRotation = ResolveCombatExtendedShotRotation(sourceVector, targetVector);
            float distance = new Vector2(
                destinationVector.x - sourceDrawPos.x,
                destinationVector.z - sourceDrawPos.z).magnitude;
            float minCollisionDistance = ResolveCombatExtendedMinCollisionDistance(distance);

            try
            {
                projectileThing = GenSpawn.Spawn(projectileThing, sourceCell, map);
                TrySetCombatExtendedField(projectileThing, "intendedTarget", intendedTarget);
                TrySetCombatExtendedField(projectileThing, "intendedTargetHeight", targetHeight);
                TrySetCombatExtendedField(projectileThing, "canTargetSelf", false);
                TrySetCombatExtendedField(projectileThing, "minCollisionDistance", minCollisionDistance);
                TrySetCombatExtendedField(projectileThing, "AccuracyFactor", 1f);
                TrySetCombatExtendedField(projectileThing, "OriginIV3", sourceCell);

                if (longLaunchMethod != null)
                {
                    longLaunchMethod.Invoke(
                        projectileThing,
                        new object[]
                        {
                            this.caster,
                            origin,
                            shotAngle,
                            shotRotation,
                            shotHeight,
                            shotSpeed,
                            this.EquipmentSource,
                            distance
                        });
                }
                else
                {
                    TrySetCombatExtendedField(projectileThing, "shotAngle", shotAngle);
                    TrySetCombatExtendedField(projectileThing, "shotRotation", shotRotation);
                    TrySetCombatExtendedField(projectileThing, "shotHeight", shotHeight);
                    TrySetCombatExtendedField(projectileThing, "shotSpeed", shotSpeed);
                    shortLaunchMethod.Invoke(
                        projectileThing,
                        new object[]
                        {
                            this.caster,
                            origin,
                            this.EquipmentSource
                        });
                }

                return true;
            }
            catch (Exception ex)
            {
                if (projectileThing != null &&
                    projectileThing.Spawned &&
                    !projectileThing.Destroyed)
                {
                    projectileThing.Destroy(DestroyMode.Vanish);
                }

                Exception baseException = ex.GetBaseException();
                Log.WarningOnce(
                    "[CeleTech Shuttle] Failed to launch Combat Extended projectile " +
                    defName +
                    ": " +
                    (baseException != null ? baseException.Message : ex.Message),
                    WarningKeyFor(defName, 77362007));
                return false;
            }
        }

        private void WarnUnsupportedProjectileThing(ThingDef projectileDef, Thing thing)
        {
            string defName = projectileDef != null ? projectileDef.defName : "<null>";
            string thingClass = projectileDef != null && projectileDef.thingClass != null
                ? projectileDef.thingClass.FullName
                : "<null>";
            string actualClass = thing != null ? thing.GetType().FullName : "<null>";
            Log.WarningOnce(
                "[CeleTech Shuttle] Shuttle muzzle projectile def " +
                defName +
                " did not create a supported projectile. thingClass=" +
                thingClass +
                ", actualClass=" +
                actualClass +
                ". Supported projectile bases are Verse.Projectile and CombatExtended.ProjectileCE; the shot was cancelled before spawning.",
                WarningKeyFor(defName, 77362005));
        }

        private static bool IsCombatExtendedProjectile(Thing thing)
        {
            Type type = thing != null ? thing.GetType() : null;
            while (type != null)
            {
                if (type.FullName == "CombatExtended.ProjectileCE")
                {
                    return true;
                }

                type = type.BaseType;
            }

            return false;
        }

        private static MethodInfo FindCombatExtendedLongLaunchMethod(Type type)
        {
            return type != null
                ? type.GetMethod(
                    "Launch",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new Type[]
                    {
                        typeof(Thing),
                        typeof(Vector2),
                        typeof(float),
                        typeof(float),
                        typeof(float),
                        typeof(float),
                        typeof(Thing),
                        typeof(float)
                    },
                    null)
                : null;
        }

        private static MethodInfo FindCombatExtendedShortLaunchMethod(Type type)
        {
            return type != null
                ? type.GetMethod(
                    "Launch",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new Type[]
                    {
                        typeof(Thing),
                        typeof(Vector2),
                        typeof(Thing)
                    },
                    null)
                : null;
        }

        private static float ResolveCombatExtendedShotHeight(
            Thing projectileThing,
            Thing caster,
            Vector3 sourceDrawPos)
        {
            float shotHeight;
            if (TryGetCombatExtendedCollisionFloat(projectileThing, caster, "shotHeight", out shotHeight) &&
                shotHeight > 0f)
            {
                return shotHeight;
            }

            if (!float.IsNaN(sourceDrawPos.y) &&
                !float.IsInfinity(sourceDrawPos.y) &&
                sourceDrawPos.y > 0.05f &&
                sourceDrawPos.y < 2.5f)
            {
                return sourceDrawPos.y;
            }

            return 0.85f;
        }

        private static float ResolveCombatExtendedTargetHeight(
            Thing projectileThing,
            LocalTargetInfo launchTarget)
        {
            Thing targetThing = launchTarget.HasThing ? launchTarget.Thing : null;
            float targetHeight;
            if (targetThing != null &&
                TryGetCombatExtendedCollisionFloat(projectileThing, targetThing, "MiddleHeight", out targetHeight) &&
                targetHeight > 0f)
            {
                return targetHeight;
            }

            return targetThing != null ? 0.85f : 0f;
        }

        private static float ResolveCombatExtendedShotSpeed(ThingDef projectileDef)
        {
            float speed = projectileDef != null && projectileDef.projectile != null
                ? projectileDef.projectile.speed
                : 0f;
            return speed > 0f ? speed : 1f;
        }

        private static float ResolveCombatExtendedShotRotation(Vector3 source, Vector3 target)
        {
            Vector3 direction = target - source;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return 0f;
            }

            return (-90f + Mathf.Rad2Deg * Mathf.Atan2(direction.z, direction.x)) % 360f;
        }

        private static float ResolveCombatExtendedShotAngle(
            ThingDef projectileDef,
            Vector3 source,
            Vector3 target,
            float shotSpeed)
        {
            float gravityPerWidth;
            if (!TryGetCombatExtendedProjectileFloat(
                    projectileDef,
                    "GravityPerWidth",
                    out gravityPerWidth) ||
                gravityPerWidth <= 0f ||
                shotSpeed <= 0f)
            {
                return 0f;
            }

            Vector2 source2 = new Vector2(source.x, source.z);
            Vector2 target2 = new Vector2(target.x, target.z);
            float range = (target2 - source2).magnitude;
            if (range <= 0.001f)
            {
                return 0f;
            }

            float speedSquared = shotSpeed * shotSpeed;
            float heightDifference = target.y - source.y;
            float discriminant =
                (speedSquared * speedSquared) -
                (gravityPerWidth *
                 ((gravityPerWidth * range * range) +
                  (2f * heightDifference * speedSquared)));
            if (float.IsNaN(discriminant) || discriminant < 0f)
            {
                return 0f;
            }

            float root = Mathf.Sqrt(discriminant);
            float flyOverheadFactor = projectileDef != null &&
                projectileDef.projectile != null &&
                projectileDef.projectile.flyOverhead
                ? 1f
                : -1f;
            float denominator = gravityPerWidth * range;
            if (Mathf.Abs(denominator) <= 0.0001f)
            {
                return 0f;
            }

            float angle = Mathf.Atan((speedSquared + (flyOverheadFactor * root)) / denominator);
            if (float.IsNaN(angle) || float.IsInfinity(angle))
            {
                return 0f;
            }

            return angle;
        }

        private static float ResolveCombatExtendedMinCollisionDistance(float targetDistance)
        {
            if (targetDistance <= 0f)
            {
                return 0f;
            }

            const float shortRangeMinCollisionDistance = 1.5f;
            const float longRangeMinCollisionDistanceMultiplier = 0.2f;
            if (targetDistance <= shortRangeMinCollisionDistance / longRangeMinCollisionDistanceMultiplier)
            {
                return Mathf.Min(shortRangeMinCollisionDistance, targetDistance * 0.75f);
            }

            return targetDistance * longRangeMinCollisionDistanceMultiplier;
        }

        private static bool TryGetCombatExtendedCollisionFloat(
            Thing projectileThing,
            Thing thing,
            string memberName,
            out float value)
        {
            value = 0f;
            if (projectileThing == null || thing == null)
            {
                return false;
            }

            Type collisionType = projectileThing.GetType().Assembly.GetType("CombatExtended.CollisionVertical");
            if (collisionType == null)
            {
                return false;
            }

            ConstructorInfo constructor = collisionType.GetConstructor(new Type[] { typeof(Thing) });
            if (constructor == null)
            {
                return false;
            }

            object collision = constructor.Invoke(new object[] { thing });
            return TryGetFloatMember(collision, memberName, out value);
        }

        private static bool TryGetCombatExtendedProjectileFloat(
            ThingDef projectileDef,
            string memberName,
            out float value)
        {
            value = 0f;
            object projectileProperties = projectileDef != null ? projectileDef.projectile : null;
            return TryGetFloatMember(projectileProperties, memberName, out value);
        }

        private static bool TrySetCombatExtendedField(object instance, string fieldName, object value)
        {
            Type type = instance != null ? instance.GetType() : null;
            while (type != null)
            {
                FieldInfo field = type.GetField(
                    fieldName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    field.SetValue(instance, value);
                    return true;
                }

                type = type.BaseType;
            }

            return false;
        }

        private static bool TryGetFloatMember(object instance, string memberName, out float value)
        {
            value = 0f;
            Type type = instance != null ? instance.GetType() : null;
            while (type != null)
            {
                FieldInfo field = type.GetField(
                    memberName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null && TryConvertFloat(field.GetValue(instance), out value))
                {
                    return true;
                }

                PropertyInfo property = type.GetProperty(
                    memberName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (property != null &&
                    property.GetIndexParameters().Length == 0 &&
                    property.CanRead &&
                    TryConvertFloat(property.GetValue(instance, null), out value))
                {
                    return true;
                }

                type = type.BaseType;
            }

            return false;
        }

        private static bool TryConvertFloat(object raw, out float value)
        {
            if (raw is float)
            {
                value = (float)raw;
                return true;
            }

            if (raw is int)
            {
                value = (int)raw;
                return true;
            }

            value = 0f;
            return false;
        }

        private IntVec3 ResolveSourceCell(Map map)
        {
            if (this.hasShuttleMuzzleSource &&
                this.shuttleMuzzleCell.IsValid &&
                (map == null || this.shuttleMuzzleCell.InBounds(map)))
            {
                return this.shuttleMuzzleCell;
            }

            return this.caster != null ? this.caster.Position : IntVec3.Invalid;
        }

        private Vector3 ResolveSourceDrawPos(IntVec3 sourceCell)
        {
            if (this.hasShuttleMuzzleSource && this.shuttleMuzzleDrawPos != Vector3.zero)
            {
                return this.shuttleMuzzleDrawPos;
            }

            if (this.caster != null)
            {
                return this.caster.Spawned ? this.caster.DrawPos : this.caster.Position.ToVector3Shifted();
            }

            return sourceCell.IsValid ? sourceCell.ToVector3Shifted() : Vector3.zero;
        }

        private LocalTargetInfo ResolveLaunchTarget(out bool useMissHitFlags)
        {
            useMissHitFlags = false;
            float originalForcedMissRadius = this.verbProps != null
                ? this.verbProps.ForcedMissRadius
                : 0f;
            if (originalForcedMissRadius > 0f)
            {
                float effectiveForcedMissRadius =
                    Mathf.Max(0f, originalForcedMissRadius * this.forcedMissRadiusMultiplier);
                if (effectiveForcedMissRadius > 0f)
                {
                    useMissHitFlags = true;
                    return new LocalTargetInfo(this.GetForcedMissTarget(effectiveForcedMissRadius));
                }

                return this.currentTarget;
            }

            if (!this.DirectFireShotHits())
            {
                useMissHitFlags = true;
                return new LocalTargetInfo(this.GetForcedMissTarget(DirectFireMissRadius));
            }

            return this.currentTarget;
        }

        private bool DirectFireShotHits()
        {
            if (!this.currentTarget.IsValid)
            {
                return true;
            }

            ShotReport report = ShotReport.HitReportFor(this.caster, this, this.currentTarget);
            float chance = report.AimOnTargetChance_IgnoringPosture;
            chance = (chance * this.directFireAccuracyMultiplier) + this.directFireAccuracyBonus;
            if (this.directFireAccuracyFloor > 0f)
            {
                chance = Mathf.Max(chance, this.directFireAccuracyFloor);
            }

            return Rand.Chance(Mathf.Clamp01(chance));
        }

        private ProjectileHitFlags ResolveHitFlags(bool useMissHitFlags)
        {
            if (useMissHitFlags)
            {
                ProjectileHitFlags missFlags = ProjectileHitFlags.NonTargetWorld;
                if (this.canHitNonTargetPawnsNow)
                {
                    missFlags |= ProjectileHitFlags.NonTargetPawns;
                }

                return missFlags;
            }

            ProjectileHitFlags hitFlags = ProjectileHitFlags.IntendedTarget;
            if (this.canHitNonTargetPawnsNow)
            {
                hitFlags |= ProjectileHitFlags.NonTargetPawns;
            }

            if (this.verbProps != null && this.verbProps.canGoWild)
            {
                hitFlags |= ProjectileHitFlags.NonTargetWorld;
            }

            return hitFlags;
        }

        private float SanitizeNonNegativeFinite(float value, float fallback)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
            {
                return fallback;
            }

            return value;
        }

        private float SanitizeFinite(float value, float fallback)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return fallback;
            }

            return value;
        }

        private static int WarningKeyFor(string text, int salt)
        {
            unchecked
            {
                int hash = salt;
                if (!string.IsNullOrEmpty(text))
                {
                    for (int i = 0; i < text.Length; i++)
                    {
                        hash = (hash * 397) ^ text[i];
                    }
                }

                return hash;
            }
        }

        private void ThrowShuttleMuzzleFlash(Vector3 sourceDrawPos, Map map)
        {
            if (this.verbProps == null ||
                map == null ||
                this.verbProps.muzzleFlashScale <= 0f ||
                sourceDrawPos == Vector3.zero)
            {
                return;
            }

            FleckMaker.Static(sourceDrawPos, map, FleckDefOf.ShotFlash, this.verbProps.muzzleFlashScale);
        }
    }
}
