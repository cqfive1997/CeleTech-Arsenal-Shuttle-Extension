using CeleTech.ShuttleExtension.ModularShuttle.Rendering.CustomShaders;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx.Vapor
{
    internal sealed class ShuttleFlightVfxVaporMaterialCache
    {
        internal static readonly ShuttleFlightVfxVaporMaterialCache Shared =
            new ShuttleFlightVfxVaporMaterialCache();

        private const string LogCategory = "ShuttleFlightVfxVapor";

        private Material vaporMaterial;
        private bool materialReadyLogged;
        private bool unsupportedTemplateWarned;
        private ShuttleFlightVfxVaporAvailability availability =
            ShuttleFlightVfxVaporAvailability.MissingBundle;

        internal ShuttleFlightVfxVaporAvailability Availability
        {
            get
            {
                return this.availability;
            }
        }

        internal Material GetVaporMaterial()
        {
            if (!this.EnsureMaterial())
            {
                return null;
            }

            return this.vaporMaterial;
        }

        private bool EnsureMaterial()
        {
            if (IsValid(this.vaporMaterial))
            {
                this.availability = ShuttleFlightVfxVaporAvailability.Available;
                return true;
            }

            Material template;
            ShuttleFlightVfxVaporAvailability loadStatus;
            if (!ShuttleFlightVfxVaporBundleLoader.TryLoadTemplateMaterial(out template, out loadStatus))
            {
                this.availability = loadStatus;
                return false;
            }

            string missingProperties;
            if (!this.SupportsRequiredProperties(template, out missingProperties))
            {
                this.availability = ShuttleFlightVfxVaporAvailability.MissingShader;
                this.WarnUnsupportedTemplateOnce(template, missingProperties);
                return false;
            }

            this.vaporMaterial = new Material(template);
            this.vaporMaterial.name = "CMC_ShuttleVaporRuntime";
            if (this.vaporMaterial.HasProperty(ShuttleFlightVfxVaporShaderPropertyIds.Color))
            {
                this.vaporMaterial.SetColor(
                    ShuttleFlightVfxVaporShaderPropertyIds.Color,
                    Color.white);
            }

            this.availability = ShuttleFlightVfxVaporAvailability.Available;
            this.LogMaterialReadyOnce(template);
            return IsValid(this.vaporMaterial);
        }

        private bool SupportsRequiredProperties(Material material, out string missingProperties)
        {
            CustomShaderMaterialValidationResult validation =
                UnityCustomShaderMaterialValidator.Validate(
                    material,
                    ShuttleCustomShaderMaterialContracts.Vapor);
            missingProperties = validation.IsValid ?
                "none" :
                (validation.Detail ?? validation.ReasonCode);
            return validation.IsValid;
        }

        private void WarnUnsupportedTemplateOnce(Material template, string missingProperties)
        {
            if (this.unsupportedTemplateWarned)
            {
                return;
            }

            this.unsupportedTemplateWarned = true;
            ShuttleLog.WarnOnce(
                LogCategory,
                "unsupported-template",
                "Vapor template material is missing required vapor properties; vapor envelope will be skipped. " +
                "availability=" +
                this.availability +
                " missingReason=missing-shader-properties" +
                " missingProperties=" +
                (missingProperties ?? "unknown") +
                " templateMaterial=" +
                MaterialName(template) +
                " templateShader=" +
                ShaderName(template));
        }

        private void LogMaterialReadyOnce(Material template)
        {
            if (this.materialReadyLogged ||
                !ShuttleFlightVfxVaporDebugSettings.ShouldLogAvailability)
            {
                return;
            }

            this.materialReadyLogged = true;
            ShuttleLog.WarnOnce(
                LogCategory,
                "material-ready",
                "Vapor runtime material is ready. availability=" +
                this.availability +
                " missingReason=none" +
                " templateMaterial=" +
                MaterialName(template) +
                " templateShader=" +
                ShaderName(template) +
                " runtimeMaterial=" +
                MaterialName(this.vaporMaterial) +
                " runtimeShader=" +
                ShaderName(this.vaporMaterial));
        }

        private static bool IsValid(Material material)
        {
            return UnityCustomShaderMaterialValidator.Validate(
                material,
                ShuttleCustomShaderMaterialContracts.Vapor).IsValid;
        }

        private static string MaterialName(Material material)
        {
            if (material == null)
            {
                return "null";
            }

            return string.IsNullOrEmpty(material.name) ? "<unnamed>" : material.name;
        }

        private static string ShaderName(Material material)
        {
            if (material == null || material.shader == null)
            {
                return "null";
            }

            return string.IsNullOrEmpty(material.shader.name) ? "<unnamed>" : material.shader.name;
        }
    }
}
