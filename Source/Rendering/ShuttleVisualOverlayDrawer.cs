using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering
{
    [StaticConstructorOnStartup]
    internal static class ShuttleVisualOverlayDrawer
    {
        private const string LightTexturePath = "Things/Building/ModularShuttle/KunPeng/Light";
        private const string ShadowTexturePath = "Things/Building/ModularShuttle/KunPeng/Shadow";
        private const float LightHeightOffset = 0.035f;
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private static readonly string[] RotationSuffixes =
        {
            "_north",
            "_east",
            "_south",
            "_west"
        };

        private static Material lightMaterial;
        private static Material shadowMaterial;
        private static readonly Material[] directionalLightMaterials =
            new Material[RotationSuffixes.Length];
        private static readonly Material[] directionalShadowMaterials =
            new Material[RotationSuffixes.Length];
        private static readonly bool[] directionalLightResolved =
            new bool[RotationSuffixes.Length];
        private static readonly bool[] directionalShadowResolved =
            new bool[RotationSuffixes.Length];
        private static MaterialPropertyBlock propertyBlock;

        internal static float IncomingGroundShadowAlpha(float timeInAnimation)
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(timeInAnimation));
        }

        internal static float LeavingGroundShadowAlpha(float timeInAnimation)
        {
            return 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(timeInAnimation));
        }

        internal static void DrawThingGroundShadow(Thing thing, Vector3 drawLoc, float alpha)
        {
            if (thing == null)
            {
                return;
            }

            DrawGraphicOverlay(
                thing.Graphic,
                thing.Rotation,
                drawLoc,
                0f,
                GetShadowMaterial(thing.Rotation),
                AltitudeLayer.Shadows.AltitudeFor(),
                false,
                alpha);
        }

        internal static void DrawThingLightOverlay(Thing thing, Vector3 drawLoc)
        {
            if (thing == null)
            {
                return;
            }

            DrawGraphicOverlay(
                thing.Graphic,
                thing.Rotation,
                drawLoc,
                0f,
                GetLightMaterial(thing.Rotation),
                drawLoc.y + LightHeightOffset,
                true,
                1f);
        }

        internal static void DrawSkyfallerGroundShadow(
            Skyfaller skyfaller,
            Vector3 groundDrawLoc,
            Rot4 drawRotation,
            float alpha)
        {
            if (skyfaller == null)
            {
                return;
            }

            DrawGraphicOverlay(
                skyfaller.Graphic,
                drawRotation,
                groundDrawLoc,
                0f,
                GetShadowMaterial(drawRotation),
                AltitudeLayer.Shadows.AltitudeFor(),
                false,
                alpha);
        }

        internal static void DrawSkyfallerLightOverlay(
            Skyfaller skyfaller,
            Vector3 drawLoc,
            Rot4 drawRotation,
            float extraRotation)
        {
            if (skyfaller == null)
            {
                return;
            }

            DrawGraphicOverlay(
                skyfaller.Graphic,
                drawRotation,
                drawLoc,
                extraRotation,
                GetLightMaterial(drawRotation),
                drawLoc.y + LightHeightOffset,
                true,
                1f);
        }

        private static void DrawGraphicOverlay(
            Graphic graphic,
            Rot4 rot,
            Vector3 drawLoc,
            float extraRotation,
            Material material,
            float altitude,
            bool addTopAltitudeBias,
            float alpha)
        {
            if (graphic == null || material == null || alpha <= 0f)
            {
                return;
            }

            Mesh mesh = graphic.MeshAt(rot);
            if (mesh == null)
            {
                return;
            }

            Quaternion rotation = graphic.QuatFromRot(rot);
            if (extraRotation != 0f)
            {
                rotation *= Quaternion.Euler(Vector3.up * extraRotation);
            }

            if (addTopAltitudeBias && graphic.data != null && graphic.data.addTopAltitudeBias)
            {
                rotation *= Quaternion.Euler(Vector3.left * 2f);
            }

            Vector3 finalDrawLoc = drawLoc + graphic.DrawOffset(rot);
            finalDrawLoc.y = altitude;

            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(finalDrawLoc, rotation, Vector3.one);

            MaterialPropertyBlock block = GetPropertyBlock();
            block.Clear();
            block.SetColor(ColorProperty, new Color(1f, 1f, 1f, Mathf.Clamp01(alpha)));

            Graphics.DrawMesh(mesh, matrix, material, 0, null, 0, block);
        }

        private static Material GetLightMaterial(Rot4 rotation)
        {
            Material directional = GetDirectionalMaterial(
                LightTexturePath,
                rotation,
                ShaderDatabase.MoteGlow,
                directionalLightMaterials,
                directionalLightResolved);
            return directional ?? GetDefaultLightMaterial();
        }

        private static Material GetDefaultLightMaterial()
        {
            if (lightMaterial == null)
            {
                Texture2D texture = ContentFinder<Texture2D>.Get(LightTexturePath, false);
                if (texture != null)
                {
                    lightMaterial = MaterialPool.MatFrom(texture, ShaderDatabase.MoteGlow, Color.white);
                }
            }

            return lightMaterial;
        }

        private static Material GetShadowMaterial(Rot4 rotation)
        {
            Material directional = GetDirectionalMaterial(
                ShadowTexturePath,
                rotation,
                ShaderDatabase.Transparent,
                directionalShadowMaterials,
                directionalShadowResolved);
            return directional ?? GetDefaultShadowMaterial();
        }

        private static Material GetDefaultShadowMaterial()
        {
            if (shadowMaterial == null)
            {
                Texture2D texture = ContentFinder<Texture2D>.Get(ShadowTexturePath, false);
                if (texture != null)
                {
                    shadowMaterial = MaterialPool.MatFrom(texture, ShaderDatabase.Transparent, Color.white);
                }
            }

            return shadowMaterial;
        }

        private static Material GetDirectionalMaterial(
            string baseTexturePath,
            Rot4 rotation,
            Shader shader,
            Material[] materials,
            bool[] resolved)
        {
            int index = rotation.AsInt;
            if (index < 0 ||
                index >= RotationSuffixes.Length ||
                materials == null ||
                resolved == null ||
                index >= materials.Length ||
                index >= resolved.Length)
            {
                return null;
            }

            if (resolved[index])
            {
                return materials[index];
            }

            resolved[index] = true;
            Texture2D texture = ContentFinder<Texture2D>.Get(
                baseTexturePath + RotationSuffixes[index],
                false);
            if (texture == null)
            {
                return null;
            }

            materials[index] = MaterialPool.MatFrom(texture, shader, Color.white);
            return materials[index];
        }

        private static MaterialPropertyBlock GetPropertyBlock()
        {
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            return propertyBlock;
        }
    }
}
