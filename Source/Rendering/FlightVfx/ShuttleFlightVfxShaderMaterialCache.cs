using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using CeleTech.ShuttleExtension.ModularShuttle.Rendering.CustomShaders;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx
{
    internal sealed class ShuttleFlightVfxShaderMaterialCache
    {
        internal static readonly ShuttleFlightVfxShaderMaterialCache Shared =
            new ShuttleFlightVfxShaderMaterialCache();

        private const string LogCategory = "ShuttleFlightVfxFlame";

        private Material tailMainGlowMaterial;
        private Material tailMainCoreMaterial;
        private Material vtolGlowMaterial;
        private Material vtolCoreMaterial;
        private bool unsupportedTemplateWarned;

        internal bool IsReady
        {
            get
            {
                return this.EnsureMaterials();
            }
        }

        internal Material GetGlowMaterial(ShuttleThrusterKind kind)
        {
            if (!this.EnsureMaterials())
            {
                return null;
            }

            return kind == ShuttleThrusterKind.TailMain ?
                this.tailMainGlowMaterial :
                this.vtolGlowMaterial;
        }

        internal Material GetCoreMaterial(ShuttleThrusterKind kind)
        {
            if (!this.EnsureMaterials())
            {
                return null;
            }

            return kind == ShuttleThrusterKind.TailMain ?
                this.tailMainCoreMaterial :
                this.vtolCoreMaterial;
        }

        private bool EnsureMaterials()
        {
            if (this.HasRuntimeMaterials())
            {
                return true;
            }

            Material glowTemplate;
            Material coreTemplate;
            if (!ShuttleFlightVfxShaderLoader.TryLoadTemplateMaterials(out glowTemplate, out coreTemplate))
            {
                return false;
            }

            if (!this.SupportsRequiredProperties(glowTemplate) ||
                !this.SupportsRequiredProperties(coreTemplate))
            {
                this.WarnUnsupportedTemplateOnce();
                return false;
            }

            this.tailMainGlowMaterial = this.CreateRuntimeMaterial(glowTemplate, "CMC_TailMainShaderGlow");
            this.tailMainCoreMaterial = this.CreateRuntimeMaterial(coreTemplate, "CMC_TailMainShaderCore");
            this.vtolGlowMaterial = this.CreateRuntimeMaterial(glowTemplate, "CMC_VtolShaderGlow");
            this.vtolCoreMaterial = this.CreateRuntimeMaterial(coreTemplate, "CMC_VtolShaderCore");

            return this.HasRuntimeMaterials();
        }

        private bool HasRuntimeMaterials()
        {
            return IsValid(this.tailMainGlowMaterial) &&
                IsValid(this.tailMainCoreMaterial) &&
                IsValid(this.vtolGlowMaterial) &&
                IsValid(this.vtolCoreMaterial);
        }

        private Material CreateRuntimeMaterial(Material template, string name)
        {
            if (!IsValid(template))
            {
                return null;
            }

            Material material = new Material(template);
            material.name = name;
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", Color.white);
            }

            return material;
        }

        private bool SupportsRequiredProperties(Material material)
        {
            return UnityCustomShaderMaterialValidator.Validate(
                material,
                ShuttleCustomShaderMaterialContracts.Flame).IsValid;
        }

        private void WarnUnsupportedTemplateOnce()
        {
            if (this.unsupportedTemplateWarned)
            {
                return;
            }

            this.unsupportedTemplateWarned = true;
            ShuttleLog.WarnOnce(
                LogCategory,
                "unsupported-template",
                "Shader flame template material is missing required flame properties; using placeholder flame fallback.");
        }

        private static bool IsValid(Material material)
        {
            return UnityCustomShaderMaterialValidator.Validate(
                material,
                ShuttleCustomShaderMaterialContracts.Flame).IsValid;
        }
    }
}
