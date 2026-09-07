using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Rendering.Paint;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Defense
{
    /// <summary>
    /// Resolves fixed-east weapon anchors from authored turret mount data.
    /// </summary>
    internal sealed class V3DefenseWeaponMapProjection
    {
        private const string HostThingDefName = "CT_ModularShuttleHost";
        private const float MinimumDrawSize = 0.05f;
        private const float BrowserPreviewWidth = 512f;
        private const float BrowserPreviewHeight = 360f;

        private bool resolved;
        private Vector2 hostDrawSize = new Vector2(7f, 9f);
        private List<ShuttleWeaponTurretVisualMount> turretMounts;
        private Texture2D eastSourceTexture;

        internal Vector2 ResolveEastAnchor(
            V3DefenseWeaponEntryModel weapon,
            int index)
        {
            Vector2 anchor;
            if (this.TryResolveMountAnchor(weapon, out anchor))
            {
                return anchor;
            }

            anchor = weapon != null ? weapon.UIAnchor : Vector2.zero;
            if (anchor == Vector2.zero)
            {
                anchor = GetFallbackAnchor(index);
            }

            return ClampAnchor(ResolveEastAuthoredAnchor(anchor));
        }

        internal Rect GetEastPreviewBodyRect(Rect previewOuter)
        {
            this.EnsureResolved();
            Rect previewRect = GetAspectFitRect(
                previewOuter,
                BrowserPreviewWidth,
                BrowserPreviewHeight);
            if (this.eastSourceTexture == null)
            {
                return previewRect;
            }

            return GetAspectFitRect(
                previewRect,
                this.eastSourceTexture.width,
                this.eastSourceTexture.height);
        }

        private bool TryResolveMountAnchor(
            V3DefenseWeaponEntryModel weapon,
            out Vector2 anchor)
        {
            anchor = Vector2.zero;
            if (weapon == null || weapon.SlotIndex <= 0)
            {
                return false;
            }

            this.EnsureResolved();
            if (this.hostDrawSize.x <= MinimumDrawSize ||
                this.hostDrawSize.y <= MinimumDrawSize)
            {
                return false;
            }

            ShuttleWeaponTurretVisualMount mount =
                this.FindMountForSlotIndex(weapon.SlotIndex);
            if (mount == null)
            {
                return false;
            }

            Vector3 offset = ShuttleWeaponTurretAirframe.ResolveRotatedMountOffset(
                mount,
                Rot4.East);
            anchor = ClampAnchor(new Vector2(
                0.5f + (offset.x / this.hostDrawSize.x),
                0.5f - (offset.z / this.hostDrawSize.y)));
            return true;
        }

        private void EnsureResolved()
        {
            if (this.resolved)
            {
                return;
            }

            this.resolved = true;
            ThingDef hostDef = DefDatabase<ThingDef>.GetNamedSilentFail(
                HostThingDefName);
            if (hostDef != null &&
                hostDef.graphicData != null &&
                hostDef.graphicData.drawSize.x > MinimumDrawSize &&
                hostDef.graphicData.drawSize.y > MinimumDrawSize)
            {
                this.hostDrawSize = hostDef.graphicData.drawSize;
            }

            CompProperties_ModularShuttleWeaponTurretVisual turretProps =
                FindCompProperties<CompProperties_ModularShuttleWeaponTurretVisual>(hostDef);
            this.turretMounts = turretProps != null ? turretProps.mounts : null;
            this.eastSourceTexture = ContentFinder<Texture2D>.Get(
                ShuttlePaintVisualAssetSet.GetLandedBrowserPaintSourceTexturePath(
                    Rot4.East),
                false);
        }

        private ShuttleWeaponTurretVisualMount FindMountForSlotIndex(int slotIndex)
        {
            if (this.turretMounts == null)
            {
                return null;
            }

            for (int i = 0; i < this.turretMounts.Count; i++)
            {
                ShuttleWeaponTurretVisualMount mount = this.turretMounts[i];
                if (mount != null && mount.parentSlotIndex == slotIndex)
                {
                    return mount;
                }
            }

            return null;
        }

        private static Vector2 ResolveEastAuthoredAnchor(Vector2 anchor)
        {
            Vector3 normalizedOffset = new Vector3(
                anchor.x - 0.5f,
                0f,
                0.5f - anchor.y);
            Vector3 rotatedOffset =
                ShuttleWeaponTurretAirframe.RotateEastAuthoredOffset(
                    normalizedOffset,
                    Rot4.East);
            return new Vector2(
                0.5f + rotatedOffset.x,
                0.5f - rotatedOffset.z);
        }

        private static Vector2 GetFallbackAnchor(int index)
        {
            int safeIndex = Mathf.Abs(index) % 8;
            if (safeIndex == 0)
            {
                return new Vector2(0.26f, 0.34f);
            }

            if (safeIndex == 1)
            {
                return new Vector2(0.74f, 0.34f);
            }

            if (safeIndex == 2)
            {
                return new Vector2(0.34f, 0.50f);
            }

            if (safeIndex == 3)
            {
                return new Vector2(0.66f, 0.50f);
            }

            if (safeIndex == 4)
            {
                return new Vector2(0.50f, 0.24f);
            }

            if (safeIndex == 5)
            {
                return new Vector2(0.22f, 0.64f);
            }

            if (safeIndex == 6)
            {
                return new Vector2(0.78f, 0.64f);
            }

            return new Vector2(0.50f, 0.72f);
        }

        private static Vector2 ClampAnchor(Vector2 anchor)
        {
            return new Vector2(
                Mathf.Clamp01(anchor.x),
                Mathf.Clamp01(anchor.y));
        }

        private static Rect GetAspectFitRect(
            Rect outer,
            float sourceWidth,
            float sourceHeight)
        {
            if (sourceWidth <= MinimumDrawSize ||
                sourceHeight <= MinimumDrawSize ||
                outer.width <= MinimumDrawSize ||
                outer.height <= MinimumDrawSize)
            {
                return outer;
            }

            float sourceAspect = sourceWidth / sourceHeight;
            float targetAspect = outer.width / outer.height;
            if (Mathf.Abs(sourceAspect - targetAspect) <= 0.001f)
            {
                return outer;
            }

            if (sourceAspect > targetAspect)
            {
                float height = outer.width / sourceAspect;
                return new Rect(
                    outer.x,
                    outer.y + ((outer.height - height) * 0.5f),
                    outer.width,
                    height);
            }

            float width = outer.height * sourceAspect;
            return new Rect(
                outer.x + ((outer.width - width) * 0.5f),
                outer.y,
                width,
                outer.height);
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
    }
}
