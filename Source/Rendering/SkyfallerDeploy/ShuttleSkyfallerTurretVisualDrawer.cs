using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.SkyfallerDeploy
{
    internal sealed class ShuttleSkyfallerTurretVisualDrawer
    {
        internal static readonly ShuttleSkyfallerTurretVisualDrawer Shared =
            new ShuttleSkyfallerTurretVisualDrawer();

        private const float MinimumDrawSize = 0.05f;
        private readonly List<ShuttleWeaponTurretVisualSnapshot> snapshots =
            new List<ShuttleWeaponTurretVisualSnapshot>();

        private Material turretMaterial;
        private string loadedTexturePath;
        private bool loggedMissingTexture;

        internal void Draw(
            Skyfaller skyfaller,
            Vector3 drawLoc,
            Rot4 drawRotation,
            float extraRotation)
        {
            if (skyfaller == null || skyfaller.Graphic == null)
            {
                return;
            }

            ThingWithComps shuttle = this.ResolveShuttle(skyfaller);
            if (shuttle == null)
            {
                return;
            }

            CompModularShuttleWeaponTurretVisual turretVisual =
                shuttle.TryGetComp<CompModularShuttleWeaponTurretVisual>();
            CompProperties_ModularShuttleWeaponTurretVisual props =
                turretVisual != null
                    ? turretVisual.props as CompProperties_ModularShuttleWeaponTurretVisual
                    : null;
            if (props == null ||
                props.mounts == null ||
                props.mounts.Count == 0)
            {
                return;
            }

            CompModularShuttleCore core = shuttle.TryGetComp<CompModularShuttleCore>();
            ShuttleController controller = core != null ? core.Controller : null;
            if (controller == null)
            {
                return;
            }

            controller.BuildWeaponTurretVisualSnapshots(this.snapshots);
            if (this.snapshots.Count == 0)
            {
                return;
            }

            Material material = this.GetTurretMaterial(props);
            if (material == null)
            {
                return;
            }

            Vector3 bodyDrawLoc = drawLoc + skyfaller.Graphic.DrawOffset(drawRotation);

            for (int i = 0; i < props.mounts.Count; i++)
            {
                ShuttleWeaponTurretVisualMount mount = props.mounts[i];
                ShuttleWeaponTurretVisualSnapshot snapshot = this.FindMatchingSnapshot(mount);
                if (mount == null || snapshot == null)
                {
                    continue;
                }

                this.DrawMount(
                    props,
                    mount,
                    material,
                    bodyDrawLoc,
                    drawRotation,
                    drawLoc.y,
                    extraRotation);
            }
        }

        private ThingWithComps ResolveShuttle(Skyfaller skyfaller)
        {
            if (skyfaller == null)
            {
                return null;
            }

            FlyShipLeaving leaving = skyfaller as FlyShipLeaving;
            if (leaving != null && leaving.Contents != null)
            {
                ThingWithComps shuttle = leaving.Contents.GetShuttle() as ThingWithComps;
                if (shuttle != null)
                {
                    return shuttle;
                }
            }

            if (skyfaller.innerContainer == null ||
                skyfaller.innerContainer.Count == 0)
            {
                return null;
            }

            for (int i = 0; i < skyfaller.innerContainer.Count; i++)
            {
                ThingWithComps candidate = skyfaller.innerContainer[i] as ThingWithComps;
                if (candidate != null &&
                    candidate.TryGetComp<CompModularShuttleCore>() != null)
                {
                    return candidate;
                }
            }

            return skyfaller.innerContainer[0] as ThingWithComps;
        }

        private void DrawMount(
            CompProperties_ModularShuttleWeaponTurretVisual props,
            ShuttleWeaponTurretVisualMount mount,
            Material material,
            Vector3 bodyDrawLoc,
            Rot4 drawRotation,
            float bodyAltitude,
            float extraRotation)
        {
            Vector2 drawSize = this.GetDrawSize(props, mount);
            if (drawSize.x <= 0f || drawSize.y <= 0f)
            {
                return;
            }

            Vector3 mountPosition = bodyDrawLoc +
                ResolveSkyfallerMountOffset(mount, drawRotation, extraRotation);
            mountPosition.y = bodyAltitude +
                props.drawAltitudeOffset +
                mount.drawAltitudeOffset;

            // Draw the launch/deploy idle pose in the shuttle's airframe, not as
            // a fixed world-facing direction.
            float idleAngle = ShuttleWeaponTurretAirframe.RotateEastAuthoredAngle(
                mount.idleAngleEast,
                drawRotation);
            Quaternion turretRotation = Quaternion.AngleAxis(
                ShuttleWeaponTurretAirframe.ResolveMeshDrawAngle(
                    props.artworkRotationOffset,
                    idleAngle,
                    extraRotation),
                Vector3.up);
            Vector3 scale = new Vector3(drawSize.x, 1f, drawSize.y);
            Matrix4x4 matrix = Matrix4x4.TRS(mountPosition, turretRotation, scale);
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
        }

        private static Vector3 ResolveSkyfallerMountOffset(
            ShuttleWeaponTurretVisualMount mount,
            Rot4 drawRotation,
            float extraRotation)
        {
            Vector3 offset = ShuttleWeaponTurretAirframe.ResolveRotatedMountOffset(
                mount,
                drawRotation);
            if (Mathf.Abs(extraRotation) <= 0.0001f)
            {
                return offset;
            }

            return Quaternion.Euler(Vector3.up * extraRotation) * offset;
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

        private Material GetTurretMaterial(
            CompProperties_ModularShuttleWeaponTurretVisual props)
        {
            string texturePath = props != null ? props.turretTexturePath : null;
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
            Log.Warning("[CeleTech Shuttle] Missing shuttle skyfaller turret texture: " +
                (texturePath ?? "<null>"));
        }

        private Vector2 GetDrawSize(
            CompProperties_ModularShuttleWeaponTurretVisual props,
            ShuttleWeaponTurretVisualMount mount)
        {
            Vector2 baseSize = props != null ? props.turretDrawSize : Vector2.zero;
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

        private static bool IsFinitePositive(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;
        }
    }
}
