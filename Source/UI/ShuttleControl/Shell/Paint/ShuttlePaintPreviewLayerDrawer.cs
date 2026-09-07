using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Paint
{
    internal sealed class ShuttlePaintPreviewLayerDrawer
    {
        private const string HostThingDefName = "CT_ModularShuttleHost";
        private const string FallbackLandingGearTexturePath =
            "Things/Building/ModularShuttle/KunPeng/KunPengLandingGearLayer";
        private const string FallbackTurretTexturePath =
            "Things/Building/ModularShuttle/KunPeng/DefaultTurrent";
        private const float MinimumDrawSize = 0.05f;
        private static readonly string[] RotationSuffixes =
        {
            "_north",
            "_east",
            "_south",
            "_west"
        };

        private bool resolved;
        private int resolvedRotationIndex = -1;
        private Vector2 hostDrawSize = new Vector2(7f, 9f);
        private Vector2 turretDrawSize = new Vector2(2.60f, 2.60f);
        private float artworkRotationOffset = 90f;
        private List<ShuttleWeaponTurretVisualMount> turretMounts;
        private Texture2D landingGearTexture;
        private Texture2D turretTexture;
        private Texture2D bodyTexture;
        private Material landingGearMaterial;
        private Material turretMaterial;
        private Material bodyMaterial;

        internal void DrawLandedUnderlay(
            RenderTexture preview,
            Texture2D bodyTexture,
            int weaponStationMask,
            Rot4 rotation)
        {
            if (preview == null || preview.width <= 0 || preview.height <= 0)
            {
                return;
            }

            this.Resolve(rotation);
            Rect contentRect = GetAspectFitRect(preview, bodyTexture);

            Matrix4x4 oldMatrix = GUI.matrix;
            Color oldColor = GUI.color;
            try
            {
                GUI.color = Color.white;
                GL.PushMatrix();
                GL.LoadPixelMatrix(0f, preview.width, preview.height, 0f);

                this.DrawLandingGear(contentRect);
                this.DrawTurrets(contentRect, weaponStationMask, rotation);
            }
            finally
            {
                GL.PopMatrix();
                GUI.matrix = oldMatrix;
                GUI.color = oldColor;
            }
        }

        internal void DrawBody(RenderTexture preview, Texture2D texture)
        {
            if (preview == null || texture == null)
            {
                return;
            }

            Matrix4x4 oldMatrix = GUI.matrix;
            Color oldColor = GUI.color;
            try
            {
                GUI.color = Color.white;
                GL.PushMatrix();
                GL.LoadPixelMatrix(0f, preview.width, preview.height, 0f);
                DrawTexture(
                    GetAspectFitRect(preview, texture),
                    texture,
                    this.GetBodyMaterial(texture));
            }
            finally
            {
                GL.PopMatrix();
                GUI.matrix = oldMatrix;
                GUI.color = oldColor;
            }
        }

        internal void DrawPaintOverlay(
            RenderTexture preview,
            Texture2D texture,
            Material material)
        {
            if (preview == null || texture == null || material == null)
            {
                return;
            }

            Matrix4x4 oldMatrix = GUI.matrix;
            Color oldColor = GUI.color;
            try
            {
                GUI.color = Color.white;
                GL.PushMatrix();
                GL.LoadPixelMatrix(0f, preview.width, preview.height, 0f);
                DrawTexture(
                    GetAspectFitRect(preview, texture),
                    texture,
                    material);
            }
            finally
            {
                GL.PopMatrix();
                GUI.matrix = oldMatrix;
                GUI.color = oldColor;
            }
        }

        private void DrawLandingGear(Rect contentRect)
        {
            if (this.landingGearTexture == null)
            {
                return;
            }

            DrawTexture(
                contentRect,
                this.landingGearTexture,
                this.landingGearMaterial);
        }

        private void DrawTurrets(Rect contentRect, int weaponStationMask, Rot4 rotation)
        {
            if (this.turretTexture == null ||
                this.turretMounts == null ||
                this.turretMounts.Count == 0 ||
                weaponStationMask == 0)
            {
                return;
            }

            for (int i = 0; i < this.turretMounts.Count; i++)
            {
                ShuttleWeaponTurretVisualMount mount = this.turretMounts[i];
                if (mount == null ||
                    !ShuttlePaintPreviewWeaponStationMask.HasStation(
                        weaponStationMask,
                        mount.parentSlotIndex))
                {
                    continue;
                }

                Rect rect = this.GetTurretRect(contentRect, mount, rotation);
                if (rect.width <= 0f || rect.height <= 0f)
                {
                    continue;
                }

                this.DrawRotatedTurret(rect, this.GetTurretAngle(mount, rotation));
            }
        }

        private Rect GetTurretRect(
            Rect contentRect,
            ShuttleWeaponTurretVisualMount mount,
            Rot4 rotation)
        {
            if (this.hostDrawSize.x <= MinimumDrawSize ||
                this.hostDrawSize.y <= MinimumDrawSize)
            {
                return Rect.zero;
            }

            Vector2 drawSize = this.GetTurretDrawSize(mount);
            if (drawSize.x <= MinimumDrawSize || drawSize.y <= MinimumDrawSize)
            {
                return Rect.zero;
            }

            Vector3 offset = ShuttleWeaponTurretAirframe.ResolveRotatedMountOffset(
                mount,
                rotation);
            float centerX = (contentRect.x + (contentRect.width * 0.5f)) +
                ((offset.x / this.hostDrawSize.x) * contentRect.width);
            float centerY = (contentRect.y + (contentRect.height * 0.5f)) -
                ((offset.z / this.hostDrawSize.y) * contentRect.height);
            float width = (drawSize.x / this.hostDrawSize.x) * contentRect.width;
            float height = (drawSize.y / this.hostDrawSize.y) * contentRect.height;
            return new Rect(
                centerX - (width * 0.5f),
                centerY - (height * 0.5f),
                width,
                height);
        }

        private Vector2 GetTurretDrawSize(ShuttleWeaponTurretVisualMount mount)
        {
            float multiplier = mount != null ? mount.drawSizeMultiplier : 1f;
            if (!IsFinitePositive(multiplier))
            {
                multiplier = 1f;
            }

            return new Vector2(
                Mathf.Max(MinimumDrawSize, this.turretDrawSize.x * multiplier),
                Mathf.Max(MinimumDrawSize, this.turretDrawSize.y * multiplier));
        }

        private float GetTurretAngle(ShuttleWeaponTurretVisualMount mount, Rot4 rotation)
        {
            float idleAngle = mount != null ? mount.idleAngleEast : 270f;
            return ShuttleWeaponTurretAirframe.NormalizeAngle(
                this.artworkRotationOffset +
                ShuttleWeaponTurretAirframe.RotateEastAuthoredAngle(idleAngle, rotation));
        }

        private void DrawRotatedTurret(Rect rect, float angle)
        {
            if (Mathf.Abs(Mathf.DeltaAngle(0f, angle)) <= 0.01f)
            {
                DrawTexture(rect, this.turretTexture, this.turretMaterial);
                return;
            }

            Matrix4x4 oldMatrix = GUI.matrix;
            try
            {
                GUIUtility.RotateAroundPivot(angle, rect.center);
                DrawTexture(rect, this.turretTexture, this.turretMaterial);
            }
            finally
            {
                GUI.matrix = oldMatrix;
            }
        }

        private void Resolve(Rot4 rotation)
        {
            if (this.resolved && this.resolvedRotationIndex == rotation.AsInt)
            {
                return;
            }

            this.resolved = true;
            this.resolvedRotationIndex = rotation.AsInt;
            ThingDef hostDef = DefDatabase<ThingDef>.GetNamedSilentFail(HostThingDefName);
            if (hostDef != null &&
                hostDef.graphicData != null &&
                hostDef.graphicData.drawSize.x > MinimumDrawSize &&
                hostDef.graphicData.drawSize.y > MinimumDrawSize)
            {
                this.hostDrawSize = hostDef.graphicData.drawSize;
            }

            CompProperties_ModularShuttleLandingGearVisual landingGearProps =
                FindCompProperties<CompProperties_ModularShuttleLandingGearVisual>(hostDef);
            string landingGearPath = landingGearProps != null &&
                !string.IsNullOrWhiteSpace(landingGearProps.texturePath)
                    ? landingGearProps.texturePath.Trim()
                    : FallbackLandingGearTexturePath;
            this.landingGearTexture = ContentFinder<Texture2D>.Get(
                landingGearPath + GetRotationSuffix(rotation),
                false);
            this.landingGearMaterial = CreateOverlayMaterial(this.landingGearTexture);

            CompProperties_ModularShuttleWeaponTurretVisual turretProps =
                FindCompProperties<CompProperties_ModularShuttleWeaponTurretVisual>(hostDef);
            if (turretProps != null)
            {
                if (turretProps.turretDrawSize.x > MinimumDrawSize &&
                    turretProps.turretDrawSize.y > MinimumDrawSize)
                {
                    this.turretDrawSize = turretProps.turretDrawSize;
                }

                this.artworkRotationOffset = turretProps.artworkRotationOffset;
                this.turretMounts = turretProps.mounts;
            }

            string turretPath = turretProps != null &&
                !string.IsNullOrWhiteSpace(turretProps.turretTexturePath)
                    ? turretProps.turretTexturePath.Trim()
                    : FallbackTurretTexturePath;
            this.turretTexture = ContentFinder<Texture2D>.Get(turretPath, false);
            this.turretMaterial = CreateOverlayMaterial(this.turretTexture);
        }

        private Material GetBodyMaterial(Texture2D texture)
        {
            if (texture == null)
            {
                return null;
            }

            if (this.bodyMaterial != null && this.bodyTexture == texture)
            {
                return this.bodyMaterial;
            }

            this.bodyTexture = texture;
            this.bodyMaterial = CreateOverlayMaterial(texture);
            return this.bodyMaterial;
        }

        private static T FindCompProperties<T>(ThingDef thingDef)
            where T : CompProperties
        {
            if (thingDef == null || thingDef.comps == null)
            {
                return null;
            }

            for (int i = 0; i < thingDef.comps.Count; i++)
            {
                T props = thingDef.comps[i] as T;
                if (props != null)
                {
                    return props;
                }
            }

            return null;
        }

        private static Material CreateOverlayMaterial(Texture2D texture)
        {
            if (texture == null)
            {
                return null;
            }

            return MaterialPool.MatFrom(
                texture,
                ShaderDatabase.Cutout,
                Color.white);
        }

        private static void DrawTexture(Rect rect, Texture2D texture, Material material)
        {
            if (texture == null)
            {
                return;
            }

            if (material != null)
            {
                Graphics.DrawTexture(rect, texture, material);
                return;
            }

            Graphics.DrawTexture(rect, texture);
        }

        private static Rect GetAspectFitRect(RenderTexture preview, Texture2D texture)
        {
            Rect fullRect = new Rect(0f, 0f, preview.width, preview.height);
            if (texture == null || texture.width <= 0 || texture.height <= 0)
            {
                return fullRect;
            }

            float sourceAspect = texture.width / (float)texture.height;
            float targetAspect = preview.width / (float)preview.height;
            if (Mathf.Abs(sourceAspect - targetAspect) <= 0.001f)
            {
                return fullRect;
            }

            if (sourceAspect > targetAspect)
            {
                float height = preview.width / sourceAspect;
                return new Rect(
                    0f,
                    (preview.height - height) * 0.5f,
                    preview.width,
                    height);
            }

            float width = preview.height * sourceAspect;
            return new Rect(
                (preview.width - width) * 0.5f,
                0f,
                width,
                preview.height);
        }

        private static string GetRotationSuffix(Rot4 rotation)
        {
            int index = rotation.AsInt;
            return index >= 0 && index < RotationSuffixes.Length
                ? RotationSuffixes[index]
                : "_east";
        }

        private static bool IsFinitePositive(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;
        }
    }
}
