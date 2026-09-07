using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.SkyfallerDeploy
{
    internal sealed class ShuttleSkyfallerDeployVisualAssets
    {
        private const string LogCategory = "ShuttleSkyfallerDeploy";
        private const string LandingGearTextureBasePath = "Things/Building/ModularShuttle/KunPeng/KunPengLandingGearLayer";
        private static readonly string[] RotationSuffixes =
        {
            "_north",
            "_east",
            "_south",
            "_west"
        };

        internal static readonly ShuttleSkyfallerDeployVisualAssets Shared =
            new ShuttleSkyfallerDeployVisualAssets();

        private bool resolved;
        private readonly Texture2D[] landingGearTextures =
            new Texture2D[RotationSuffixes.Length];
        private readonly Material[] landingGearMaterials =
            new Material[RotationSuffixes.Length];

        internal Material LandingGearMaterialFor(Rot4 rotation)
        {
            if (!this.TryResolve())
            {
                return null;
            }

            int index = rotation.AsInt;
            if (index < 0 || index >= this.landingGearMaterials.Length)
            {
                index = Rot4.North.AsInt;
            }

            return this.landingGearMaterials[index];
        }

        internal bool TryResolve()
        {
            if (!this.resolved || !this.HasAllLandingGearMaterials())
            {
                for (int i = 0; i < RotationSuffixes.Length; i++)
                {
                    string texturePath = LandingGearTextureBasePath + RotationSuffixes[i];
                    this.landingGearTextures[i] =
                        this.LoadTexture(texturePath, "landing gear " + RotationSuffixes[i]);
                    this.landingGearMaterials[i] =
                        this.MakeOverlayMaterial(
                            this.landingGearTextures[i],
                            "landing gear " + RotationSuffixes[i]);
                }

                this.resolved = this.HasAllLandingGearMaterials();
            }

            return this.resolved;
        }

        private bool HasAllLandingGearMaterials()
        {
            for (int i = 0; i < this.landingGearMaterials.Length; i++)
            {
                if (this.landingGearTextures[i] == null ||
                    this.landingGearMaterials[i] == null)
                {
                    return false;
                }
            }

            return true;
        }

        private Texture2D LoadTexture(string texturePath, string label)
        {
            Texture2D texture = ContentFinder<Texture2D>.Get(texturePath, false);
            if (texture == null)
            {
                ShuttleLog.WarnOnce(
                    LogCategory,
                    "missing-texture|" + texturePath,
                    "Shuttle deploy overlay texture is missing. label=" + label + ", path=" + texturePath);
            }

            return texture;
        }

        private Material MakeOverlayMaterial(Texture2D texture, string label)
        {
            if (texture == null)
            {
                return null;
            }

            Material material = MaterialPool.MatFrom(texture, ShaderDatabase.Cutout, Color.white);
            if (material == null || material == BaseContent.BadMat)
            {
                ShuttleLog.WarnOnce(
                    LogCategory,
                    "missing-material|" + label,
                    "Shuttle deploy overlay material could not be created. label=" + label);
                return null;
            }

            return material;
        }
    }
}
