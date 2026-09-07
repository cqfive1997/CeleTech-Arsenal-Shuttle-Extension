using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    public sealed class CompProperties_ModularShuttleWeaponTurretVisual : CompProperties
    {
        public string turretTexturePath =
            "Things/Building/ModularShuttle/KunPeng/DefaultTurrent";
        public Vector2 turretDrawSize = new Vector2(1.6f, 1.6f);
        public float artworkRotationOffset = 90f;
        public float rotationSpeedDegreesPerSecond = 360f;
        public float launchIdleReturnSpeedDegreesPerSecond = 120f;
        public int minLaunchIdleReturnTicks = 18;
        public float muzzleForwardOffset = 1.08f;
        public float drawAltitudeOffset = -0.02f;
        public List<ShuttleWeaponTurretVisualMount> mounts =
            new List<ShuttleWeaponTurretVisualMount>();

        public CompProperties_ModularShuttleWeaponTurretVisual()
        {
            this.compClass = typeof(CompModularShuttleWeaponTurretVisual);
        }
    }

    public sealed class ShuttleWeaponTurretVisualMount
    {
        public string moduleDefName;
        public string moduleInstanceID;
        public int parentSlotIndex = -1;
        public Vector3 localOffset = Vector3.zero;
        public float idleAngleEast = 270f;
        public float muzzleForwardOffset = -1f;
        public float drawSizeMultiplier = 1f;
        public float drawAltitudeOffset;
        public Vector3 westLocalOffsetAdjustment = Vector3.zero;
    }

    public sealed class CompModularShuttleWeaponTurretVisual : ThingComp
    {
        private const float MinimumDrawSize = 0.05f;
        private const float MinimumRotationSpeed = 1f;
        private const float LaunchIdleToleranceDegrees = 1.5f;
        private const float TicksPerSecond = 60f;
        private readonly List<ShuttleWeaponTurretVisualSnapshot> snapshots =
            new List<ShuttleWeaponTurretVisualSnapshot>();
        private readonly List<TurretAngleState> angleStates =
            new List<TurretAngleState>();

        private Material turretMaterial;
        private string loadedTexturePath;
        private bool loggedMissingTexture;
        private bool forceIdleForLaunch;
        private bool hasPendingLaunchAfterIdle;
        private IShuttleCommandExecutor pendingLaunchCommandExecutor;
        private PlanetTile pendingLaunchDestinationTile;
        private TransportersArrivalAction pendingLaunchArrivalAction;
        private int pendingLaunchIdleReturnTicks;

        private CompProperties_ModularShuttleWeaponTurretVisual Props
        {
            get
            {
                return (CompProperties_ModularShuttleWeaponTurretVisual)this.props;
            }
        }

        public override void CompTick()
        {
            base.CompTick();

            if (!this.hasPendingLaunchAfterIdle)
            {
                return;
            }

            this.AdvancePendingLaunchIdleReturn();
        }

        public override void PostDraw()
        {
            base.PostDraw();

            if (this.parent == null ||
                !this.parent.Spawned ||
                this.Props == null ||
                this.Props.mounts == null ||
                this.Props.mounts.Count == 0)
            {
                return;
            }

            Material material = this.GetTurretMaterial();
            if (material == null)
            {
                return;
            }

            CompModularShuttleCore core = this.parent.GetComp<CompModularShuttleCore>();
            ShuttleController controller = core != null ? core.Controller : null;
            if (controller == null)
            {
                return;
            }

            controller.BuildWeaponTurretVisualSnapshots(this.snapshots);
            if (this.snapshots.Count == 0)
            {
                this.angleStates.Clear();
                return;
            }

            int frame = Time.frameCount;
            for (int i = 0; i < this.Props.mounts.Count; i++)
            {
                ShuttleWeaponTurretVisualMount mount = this.Props.mounts[i];
                ShuttleWeaponTurretVisualSnapshot snapshot =
                    this.FindMatchingSnapshot(mount);
                if (mount == null || snapshot == null)
                {
                    continue;
                }

                this.DrawMount(mount, snapshot, i, frame, material);
            }

            this.TrimAngleStates(frame);
        }

        internal bool TryQueueLaunchAfterIdle(
            IShuttleCommandExecutor commandExecutor,
            PlanetTile destinationTile,
            TransportersArrivalAction arrivalAction,
            out string message)
        {
            message = null;
            if (commandExecutor == null ||
                this.parent == null ||
                !this.parent.Spawned ||
                this.Props == null ||
                this.Props.mounts == null ||
                this.Props.mounts.Count == 0)
            {
                return false;
            }

            this.forceIdleForLaunch = true;
            bool hasActiveMount;
            bool needsReturn;
            this.CheckLaunchIdleReturnNeed(
                out hasActiveMount,
                out needsReturn);
            if (!hasActiveMount || !needsReturn)
            {
                this.forceIdleForLaunch = false;
                return false;
            }

            bool wasAlreadyPending = this.hasPendingLaunchAfterIdle;
            this.pendingLaunchCommandExecutor = commandExecutor;
            this.pendingLaunchDestinationTile = destinationTile;
            this.pendingLaunchArrivalAction = arrivalAction;
            this.pendingLaunchIdleReturnTicks = 0;
            this.hasPendingLaunchAfterIdle = true;
            message = "CT_Shuttle_Launch_TurretsReturning".Translate().ToString();

            if (!wasAlreadyPending)
            {
                Messages.Message(message, MessageTypeDefOf.NeutralEvent, false);
                CameraJumper.TryHideWorld();
            }

            return true;
        }

        private void DrawMount(
            ShuttleWeaponTurretVisualMount mount,
            ShuttleWeaponTurretVisualSnapshot snapshot,
            int mountIndex,
            int frame,
            Material material)
        {
            Vector3 mountPosition = this.ResolveMountWorldPosition(mount);
            float idleAngle = this.ResolveIdleAngle(mount);
            TurretAngleState state = this.GetOrCreateAngleState(
                snapshot,
                mountIndex,
                idleAngle);
            state.LastSeenFrame = frame;
            if (!this.forceIdleForLaunch)
            {
                float targetAngle;
                if (this.TryResolveTargetAngle(snapshot, mountPosition, out targetAngle))
                {
                    state.CurrentAngle = Mathf.MoveTowardsAngle(
                        state.CurrentAngle,
                        targetAngle,
                        this.GetRotationStepDegreesForDraw());
                }
            }

            Vector2 drawSize = this.GetDrawSize(mount);
            if (drawSize.x <= 0f || drawSize.y <= 0f)
            {
                return;
            }

            mountPosition.y = this.parent.DrawPos.y +
                this.Props.drawAltitudeOffset +
                mount.drawAltitudeOffset;

            Quaternion rotation = Quaternion.AngleAxis(
                ShuttleWeaponTurretAirframe.ResolveMeshDrawAngle(
                    this.Props.artworkRotationOffset,
                    state.CurrentAngle,
                    0f),
                Vector3.up);
            Vector3 scale = new Vector3(drawSize.x, 1f, drawSize.y);
            Matrix4x4 matrix = Matrix4x4.TRS(mountPosition, rotation, scale);
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
        }

        private Vector3 ResolveMountWorldPosition(ShuttleWeaponTurretVisualMount mount)
        {
            return ShuttleWeaponTurretAirframe.ResolveHostMountWorldPosition(
                this.parent,
                mount);
        }

        private bool TryResolveTargetAngle(
            ShuttleWeaponTurretVisualSnapshot snapshot,
            Vector3 mountPosition,
            out float targetAngle)
        {
            targetAngle = 0f;
            LocalTargetInfo target = snapshot != null
                ? snapshot.BestTarget
                : LocalTargetInfo.Invalid;
            if (!target.IsValid)
            {
                return false;
            }

            Vector3 targetPosition = this.ResolveTargetPosition(target);
            Vector3 direction = targetPosition - mountPosition;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            targetAngle = ShuttleWeaponTurretAirframe.NormalizeAngle(
                Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg);
            return true;
        }

        private float ResolveIdleAngle(ShuttleWeaponTurretVisualMount mount)
        {
            float idleAngle = mount != null ? mount.idleAngleEast : 270f;
            // The launch-idle return is airframe-relative. idleAngleEast is authored
            // against the East-facing source art, then rotated into the current
            // shuttle orientation here.
            return ShuttleWeaponTurretAirframe.RotateEastAuthoredAngle(
                idleAngle,
                this.parent.Rotation);
        }

        private Vector3 ResolveTargetPosition(LocalTargetInfo target)
        {
            if (target.HasThing && target.Thing != null && target.Thing.Spawned)
            {
                return target.Thing.DrawPos;
            }

            if (target.Cell.IsValid)
            {
                return target.Cell.ToVector3Shifted();
            }

            return this.parent.DrawPos;
        }

        private ShuttleWeaponTurretVisualSnapshot FindMatchingSnapshot(
            ShuttleWeaponTurretVisualMount mount)
        {
            if (mount == null)
            {
                return null;
            }

            for (int i = 0; i < this.snapshots.Count; i++)
            {
                ShuttleWeaponTurretVisualSnapshot snapshot = this.snapshots[i];
                if (snapshot == null)
                {
                    continue;
                }

                if (mount.parentSlotIndex >= 0 &&
                    snapshot.ParentSlotIndex == mount.parentSlotIndex)
                {
                    return snapshot;
                }

                if (mount.parentSlotIndex >= 0)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(mount.moduleInstanceID) &&
                    snapshot.ModuleInstanceID == mount.moduleInstanceID)
                {
                    return snapshot;
                }

                if (string.IsNullOrEmpty(mount.moduleInstanceID) &&
                    !string.IsNullOrEmpty(mount.moduleDefName) &&
                    snapshot.ModuleDefName == mount.moduleDefName)
                {
                    return snapshot;
                }
            }

            return null;
        }

        private Material GetTurretMaterial()
        {
            string texturePath = this.Props != null ? this.Props.turretTexturePath : null;
            if (string.IsNullOrWhiteSpace(texturePath))
            {
                return null;
            }

            texturePath = texturePath.Trim();
            if (this.turretMaterial != null && this.loadedTexturePath == texturePath)
            {
                ConfigureTurretMaterial(this.turretMaterial);
                return this.turretMaterial;
            }

            Texture2D texture = ContentFinder<Texture2D>.Get(texturePath, false);
            if (texture == null)
            {
                this.LogMissingTextureOnce(texturePath);
                this.loadedTexturePath = texturePath;
                this.turretMaterial = null;
                return null;
            }

            this.loadedTexturePath = texturePath;
            this.turretMaterial = MaterialPool.MatFrom(
                texture,
                ShaderDatabase.Cutout,
                Color.white);
            ConfigureTurretMaterial(this.turretMaterial);
            return this.turretMaterial;
        }

        private static void ConfigureTurretMaterial(Material material)
        {
            if (material != null)
            {
                material.renderQueue = -1;
            }
        }

        private void LogMissingTextureOnce(string texturePath)
        {
            if (this.loggedMissingTexture)
            {
                return;
            }

            this.loggedMissingTexture = true;
            Log.Warning("[CeleTech Shuttle] Missing shuttle turret visual texture: " +
                (texturePath ?? "<null>"));
        }

        private Vector2 GetDrawSize(ShuttleWeaponTurretVisualMount mount)
        {
            Vector2 baseSize = this.Props != null
                ? this.Props.turretDrawSize
                : Vector2.zero;
            float multiplier = mount != null ? mount.drawSizeMultiplier : 1f;
            if (!IsFinitePositive(multiplier))
            {
                multiplier = 1f;
            }

            float x = IsFinitePositive(baseSize.x)
                ? Mathf.Max(MinimumDrawSize, baseSize.x * multiplier)
                : 0f;
            float y = IsFinitePositive(baseSize.y)
                ? Mathf.Max(MinimumDrawSize, baseSize.y * multiplier)
                : 0f;
            return new Vector2(x, y);
        }

        private float GetRotationStepDegreesForDraw()
        {
            float speed = this.Props != null
                ? this.Props.rotationSpeedDegreesPerSecond
                : 0f;
            if (!IsFinitePositive(speed))
            {
                speed = MinimumRotationSpeed;
            }

            float deltaTime = Mathf.Max(0.001f, Time.deltaTime);
            return speed * deltaTime;
        }

        private float GetLaunchIdleReturnStepDegreesForTick()
        {
            float speed = this.Props != null
                ? this.Props.launchIdleReturnSpeedDegreesPerSecond
                : 0f;
            if (!IsFinitePositive(speed))
            {
                speed = MinimumRotationSpeed;
            }

            return speed / TicksPerSecond;
        }

        private int GetMinimumLaunchIdleReturnTicks()
        {
            return this.Props != null && this.Props.minLaunchIdleReturnTicks > 0
                ? this.Props.minLaunchIdleReturnTicks
                : 0;
        }

        private void AdvancePendingLaunchIdleReturn()
        {
            if (this.parent == null || this.parent.Destroyed || !this.parent.Spawned)
            {
                this.ClearPendingLaunchAfterIdle();
                return;
            }

            this.forceIdleForLaunch = true;
            bool hasActiveMount;
            bool allIdle;
            this.UpdateIdleReturnAngles(
                this.GetLaunchIdleReturnStepDegreesForTick(),
                out hasActiveMount,
                out allIdle);
            this.pendingLaunchIdleReturnTicks++;
            if (hasActiveMount &&
                (!allIdle ||
                 this.pendingLaunchIdleReturnTicks < this.GetMinimumLaunchIdleReturnTicks()))
            {
                return;
            }

            this.ExecutePendingLaunchAfterIdle();
        }

        private void CheckLaunchIdleReturnNeed(
            out bool hasActiveMount,
            out bool needsReturn)
        {
            hasActiveMount = false;
            needsReturn = false;
            if (this.Props == null ||
                this.Props.mounts == null ||
                this.Props.mounts.Count == 0 ||
                !this.TryBuildSnapshotsForVisual())
            {
                return;
            }

            int frame = Time.frameCount;
            for (int i = 0; i < this.Props.mounts.Count; i++)
            {
                ShuttleWeaponTurretVisualMount mount = this.Props.mounts[i];
                ShuttleWeaponTurretVisualSnapshot snapshot =
                    this.FindMatchingSnapshot(mount);
                if (mount == null || snapshot == null)
                {
                    continue;
                }

                hasActiveMount = true;
                Vector3 mountPosition = this.ResolveMountWorldPosition(mount);
                float desiredAngle = this.ResolveIdleAngle(mount);
                TurretAngleState state = this.FindAngleState(snapshot, i);
                if (state == null)
                {
                    float initialAngle = desiredAngle;
                    float targetAngle;
                    if (this.TryResolveTargetAngle(snapshot, mountPosition, out targetAngle))
                    {
                        initialAngle = targetAngle;
                    }

                    state = this.GetOrCreateAngleState(snapshot, i, initialAngle);
                }

                state.LastSeenFrame = frame;
                if (!IsAngleClose(state.CurrentAngle, desiredAngle, LaunchIdleToleranceDegrees))
                {
                    needsReturn = true;
                }
            }
        }

        private void UpdateIdleReturnAngles(
            float stepDegrees,
            out bool hasActiveMount,
            out bool allIdle)
        {
            hasActiveMount = false;
            allIdle = true;
            if (this.Props == null ||
                this.Props.mounts == null ||
                this.Props.mounts.Count == 0 ||
                !this.TryBuildSnapshotsForVisual())
            {
                return;
            }

            int frame = Time.frameCount;
            for (int i = 0; i < this.Props.mounts.Count; i++)
            {
                ShuttleWeaponTurretVisualMount mount = this.Props.mounts[i];
                ShuttleWeaponTurretVisualSnapshot snapshot =
                    this.FindMatchingSnapshot(mount);
                if (mount == null || snapshot == null)
                {
                    continue;
                }

                hasActiveMount = true;
                Vector3 mountPosition = this.ResolveMountWorldPosition(mount);
                float desiredAngle = this.ResolveIdleAngle(mount);
                float initialAngle = desiredAngle;
                float targetAngle;
                if (this.TryResolveTargetAngle(snapshot, mountPosition, out targetAngle))
                {
                    initialAngle = targetAngle;
                }

                TurretAngleState state = this.GetOrCreateAngleState(snapshot, i, initialAngle);
                state.LastSeenFrame = frame;
                state.CurrentAngle = Mathf.MoveTowardsAngle(
                    state.CurrentAngle,
                    desiredAngle,
                    stepDegrees);

                if (!IsAngleClose(state.CurrentAngle, desiredAngle, LaunchIdleToleranceDegrees))
                {
                    allIdle = false;
                }
            }
        }

        private bool TryBuildSnapshotsForVisual()
        {
            if (this.parent == null)
            {
                this.snapshots.Clear();
                return false;
            }

            CompModularShuttleCore core = this.parent.GetComp<CompModularShuttleCore>();
            ShuttleController controller = core != null ? core.Controller : null;
            if (controller == null)
            {
                this.snapshots.Clear();
                return false;
            }

            controller.BuildWeaponTurretVisualSnapshots(this.snapshots);
            return true;
        }

        private void ExecutePendingLaunchAfterIdle()
        {
            IShuttleCommandExecutor commandExecutor = this.pendingLaunchCommandExecutor;
            PlanetTile destinationTile = this.pendingLaunchDestinationTile;
            TransportersArrivalAction arrivalAction = this.pendingLaunchArrivalAction;
            this.ClearPendingLaunchAfterIdle();

            if (commandExecutor == null)
            {
                Messages.Message(
                    "CT_Shuttle_Command_ExecutorUnavailable".Translate(),
                    MessageTypeDefOf.RejectInput,
                    false);
                return;
            }

            ShuttleCommandResult result =
                commandExecutor.Execute(new ConfirmLaunchCommand(destinationTile, arrivalAction));
            if (result == null)
            {
                Messages.Message(
                    "CT_Shuttle_Command_ContextUnavailable".Translate(),
                    MessageTypeDefOf.RejectInput,
                    false);
                return;
            }

            if (!result.Success && !string.IsNullOrEmpty(result.Message))
            {
                Messages.Message(result.Message, MessageTypeDefOf.RejectInput, false);
            }
        }

        private void ClearPendingLaunchAfterIdle()
        {
            this.hasPendingLaunchAfterIdle = false;
            this.forceIdleForLaunch = false;
            this.pendingLaunchCommandExecutor = null;
            this.pendingLaunchDestinationTile = default(PlanetTile);
            this.pendingLaunchArrivalAction = null;
            this.pendingLaunchIdleReturnTicks = 0;
        }

        private TurretAngleState GetOrCreateAngleState(
            ShuttleWeaponTurretVisualSnapshot snapshot,
            int mountIndex,
            float initialAngle)
        {
            TurretAngleState existing = this.FindAngleState(snapshot, mountIndex);
            Rot4 currentRotation = this.parent != null ? this.parent.Rotation : Rot4.North;
            if (existing != null)
            {
                if (existing.HostRotation != currentRotation)
                {
                    existing.HostRotation = currentRotation;
                    existing.CurrentAngle = initialAngle;
                }

                return existing;
            }

            TurretAngleState created = new TurretAngleState();
            created.ModuleInstanceID = snapshot != null ? snapshot.ModuleInstanceID : null;
            created.MountIndex = mountIndex;
            created.HostRotation = currentRotation;
            created.CurrentAngle = initialAngle;
            this.angleStates.Add(created);
            return created;
        }

        private TurretAngleState FindAngleState(
            ShuttleWeaponTurretVisualSnapshot snapshot,
            int mountIndex)
        {
            string moduleInstanceID = snapshot != null ? snapshot.ModuleInstanceID : null;
            for (int i = 0; i < this.angleStates.Count; i++)
            {
                TurretAngleState state = this.angleStates[i];
                if (state != null &&
                    state.MountIndex == mountIndex &&
                    state.ModuleInstanceID == moduleInstanceID)
                {
                    return state;
                }
            }

            return null;
        }

        private void TrimAngleStates(int currentFrame)
        {
            for (int i = this.angleStates.Count - 1; i >= 0; i--)
            {
                TurretAngleState state = this.angleStates[i];
                if (state == null || state.LastSeenFrame < currentFrame)
                {
                    this.angleStates.RemoveAt(i);
                }
            }
        }

        private static bool IsAngleClose(float currentAngle, float desiredAngle, float toleranceDegrees)
        {
            return Mathf.Abs(Mathf.DeltaAngle(currentAngle, desiredAngle)) <= toleranceDegrees;
        }

        private static bool IsFinitePositive(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;
        }

        private sealed class TurretAngleState
        {
            internal string ModuleInstanceID;
            internal int MountIndex;
            internal Rot4 HostRotation;
            internal float CurrentAngle;
            internal int LastSeenFrame;
        }
    }
}
