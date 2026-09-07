using System;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.CustomShaders
{
    internal static class CustomShaderMaterialValidationReason
    {
        internal const string Valid = "valid";
        internal const string MaterialNull = "material-null";
        internal const string BadMaterial = "bad-material";
        internal const string ShaderNull = "shader-null";
        internal const string ShaderUnsupported = "shader-unsupported";
        internal const string UnexpectedShader = "unexpected-shader";
        internal const string MissingProperty = "missing-property";
    }

    internal struct CustomShaderMaterialValidationResult
    {
        internal CustomShaderMaterialValidationResult(
            bool isValid,
            string reasonCode,
            string detail)
        {
            this.IsValid = isValid;
            this.ReasonCode = reasonCode;
            this.Detail = detail;
        }

        internal bool IsValid { get; private set; }

        internal string ReasonCode { get; private set; }

        internal string Detail { get; private set; }
    }

    internal interface ICustomShaderMaterialProbe
    {
        bool MaterialExists { get; }

        bool IsBadMaterial { get; }

        bool ShaderExists { get; }

        bool ShaderSupported { get; }

        string ShaderName { get; }

        bool HasProperty(string propertyName);
    }

    internal sealed class CustomShaderMaterialContract
    {
        internal CustomShaderMaterialContract(
            string expectedShaderName,
            string[] requiredPropertyNames,
            bool strictShaderName)
        {
            this.ExpectedShaderName = expectedShaderName;
            this.RequiredPropertyNames = requiredPropertyNames ?? new string[0];
            this.StrictShaderName = strictShaderName;
        }

        internal string ExpectedShaderName { get; private set; }

        internal string[] RequiredPropertyNames { get; private set; }

        internal bool StrictShaderName { get; private set; }
    }

    internal static class ShuttleCustomShaderMaterialValidator
    {
        internal static CustomShaderMaterialValidationResult Validate<TProbe>(
            TProbe probe,
            CustomShaderMaterialContract contract)
            where TProbe : struct, ICustomShaderMaterialProbe
        {
            if (!probe.MaterialExists)
            {
                return Invalid(CustomShaderMaterialValidationReason.MaterialNull, null);
            }

            if (probe.IsBadMaterial)
            {
                return Invalid(CustomShaderMaterialValidationReason.BadMaterial, null);
            }

            if (!probe.ShaderExists)
            {
                return Invalid(CustomShaderMaterialValidationReason.ShaderNull, null);
            }

            if (!probe.ShaderSupported)
            {
                return Invalid(CustomShaderMaterialValidationReason.ShaderUnsupported, null);
            }

            if (contract == null)
            {
                return Invalid(CustomShaderMaterialValidationReason.UnexpectedShader, null);
            }

            if (contract.StrictShaderName &&
                !string.Equals(
                    probe.ShaderName,
                    contract.ExpectedShaderName,
                    StringComparison.Ordinal))
            {
                return Invalid(
                    CustomShaderMaterialValidationReason.UnexpectedShader,
                    probe.ShaderName);
            }

            string[] requiredProperties = contract.RequiredPropertyNames;
            for (int i = 0; i < requiredProperties.Length; i++)
            {
                string propertyName = requiredProperties[i];
                if (string.IsNullOrEmpty(propertyName))
                {
                    continue;
                }

                if (!probe.HasProperty(propertyName))
                {
                    return Invalid(
                        CustomShaderMaterialValidationReason.MissingProperty,
                        propertyName);
                }
            }

            return new CustomShaderMaterialValidationResult(
                true,
                CustomShaderMaterialValidationReason.Valid,
                null);
        }

        private static CustomShaderMaterialValidationResult Invalid(
            string reasonCode,
            string detail)
        {
            return new CustomShaderMaterialValidationResult(false, reasonCode, detail);
        }
    }

    internal static class ShuttleCustomShaderDrawGate
    {
        internal static bool AllowsCustomRendering(
            CustomShaderMaterialValidationResult validation)
        {
            return validation.IsValid;
        }
    }
}
