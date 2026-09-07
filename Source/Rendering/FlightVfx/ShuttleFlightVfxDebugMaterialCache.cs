using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx
{
    internal sealed class ShuttleFlightVfxDebugMaterialCache
    {
        internal static readonly ShuttleFlightVfxDebugMaterialCache Shared =
            new ShuttleFlightVfxDebugMaterialCache();

        private Material tailMainMaterial;
        private Material rearVtolMaterial;
        private Material bellyVtolMaterial;
        private Material airflowReferenceMaterial;

        internal Material GetMaterial(ShuttleThrusterKind kind)
        {
            if (kind == ShuttleThrusterKind.TailMain)
            {
                return this.GetOrCreate(ref this.tailMainMaterial, new Color(0.30f, 0.95f, 1f, 0.95f));
            }

            if (kind == ShuttleThrusterKind.RearVtol)
            {
                return this.GetOrCreate(ref this.rearVtolMaterial, new Color(0.20f, 0.45f, 1f, 0.95f));
            }

            if (kind == ShuttleThrusterKind.BellyVtol)
            {
                return this.GetOrCreate(ref this.bellyVtolMaterial, new Color(0.15f, 1f, 0.70f, 0.95f));
            }

            return this.GetOrCreate(ref this.airflowReferenceMaterial, new Color(1f, 0.1f, 1f, 0.95f));
        }

        private Material GetOrCreate(ref Material material, Color color)
        {
            if (material == null || material == BaseContent.BadMat)
            {
                material = MaterialPool.MatFrom(BaseContent.WhiteTex, this.ResolveDebugShader(), color);
            }

            return material;
        }

        private Shader ResolveDebugShader()
        {
            return ShaderDatabase.MetaOverlay != null
                ? ShaderDatabase.MetaOverlay
                : ShaderDatabase.Transparent;
        }
    }
}
