using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.CustomShaders
{
    internal struct UnityCustomShaderMaterialProbe : ICustomShaderMaterialProbe
    {
        private readonly Material material;

        internal UnityCustomShaderMaterialProbe(Material material)
        {
            this.material = material;
        }

        public bool MaterialExists
        {
            get { return this.material != null; }
        }

        public bool IsBadMaterial
        {
            get { return this.material == BaseContent.BadMat; }
        }

        public bool ShaderExists
        {
            get { return this.material != null && this.material.shader != null; }
        }

        public bool ShaderSupported
        {
            get
            {
                return this.material != null &&
                    this.material.shader != null &&
                    this.material.shader.isSupported;
            }
        }

        public string ShaderName
        {
            get
            {
                return this.material != null && this.material.shader != null ?
                    this.material.shader.name :
                    null;
            }
        }

        public bool HasProperty(string propertyName)
        {
            return this.material != null && this.material.HasProperty(propertyName);
        }
    }

    internal static class UnityCustomShaderMaterialValidator
    {
        internal static CustomShaderMaterialValidationResult Validate(
            Material material,
            CustomShaderMaterialContract contract)
        {
            return ShuttleCustomShaderMaterialValidator.Validate(
                new UnityCustomShaderMaterialProbe(material),
                contract);
        }
    }
}
