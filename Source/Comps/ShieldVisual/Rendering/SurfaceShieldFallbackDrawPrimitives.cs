using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    internal sealed class SurfaceShieldFallbackDrawPrimitives
    {
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private static readonly int MainTexProperty = Shader.PropertyToID("_MainTex");

        private MaterialPropertyBlock propertyBlock;

        internal SurfaceShieldFallbackDrawPrimitives(MaterialPropertyBlock propertyBlock)
        {
            this.propertyBlock = propertyBlock;
        }

        internal MaterialPropertyBlock PropertyBlock
        {
            get { return this.propertyBlock; }
        }

        internal MaterialPropertyBlock ConfigureTexturedColorBlock(Texture texture, Color color)
        {
            MaterialPropertyBlock block = this.EnsurePropertyBlock();
            block.Clear();
            block.SetTexture(MainTexProperty, texture);
            block.SetColor(ColorProperty, color);
            return block;
        }

        internal bool HasPulseMaterial()
        {
            return SurfaceShieldMaterialCache.GetFallbackPulseMaterial() != null;
        }

        internal void DrawDisc(Vector3 center, float radius, Color color)
        {
            this.DrawMesh(SurfaceShieldMeshCache.GetFallbackDiscMesh(), center, radius, color);
        }

        internal void DrawThinRing(Vector3 center, float radius, Color color)
        {
            this.DrawMesh(SurfaceShieldMeshCache.GetFallbackThinRingMesh(), center, radius, color);
        }

        internal void DrawHexCell(Vector3 center, float radius, float rotationDeg, Color color)
        {
            float diameter = Mathf.Max(0.04f, radius * 2f);
            this.DrawMesh(
                SurfaceShieldMeshCache.GetFallbackHexMesh(),
                center,
                Quaternion.Euler(0f, rotationDeg, 0f),
                new Vector3(diameter, 1f, diameter),
                color,
                0.26f);
        }

        internal void DrawShard(
            Vector3 center,
            float angleDeg,
            float distance,
            float length,
            float width,
            Color color)
        {
            float radians = angleDeg * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians));
            Vector3 shardCenter = center + direction * (distance + length * 0.5f);
            Quaternion rotation = Quaternion.Euler(0f, angleDeg, 0f);

            this.DrawMesh(
                SurfaceShieldMeshCache.GetFallbackShardMesh(),
                shardCenter,
                rotation,
                new Vector3(Mathf.Max(0.01f, width), 1f, Mathf.Max(0.01f, length)),
                color,
                0.24f);
        }

        internal void DrawMesh(Mesh mesh, Vector3 center, float radius, Color color)
        {
            // Fallback meshes are authored with outer radius 0.5 in XZ, so a
            // world scale equal to diameter gives the requested world radius.
            float diameter = Mathf.Max(0.1f, radius * 2f);
            this.DrawMesh(
                mesh,
                center,
                Quaternion.identity,
                new Vector3(diameter, 1f, diameter),
                color,
                0.18f);
        }

        internal void DrawMesh(
            Mesh mesh,
            Vector3 center,
            Quaternion rotation,
            Vector3 scale,
            Color color,
            float yOffset)
        {
            if (mesh == null || color.a <= 0f || scale.x <= 0f || scale.z <= 0f)
            {
                return;
            }

            Material material = SurfaceShieldMaterialCache.GetFallbackPulseMaterial();
            if (material == null)
            {
                return;
            }

            MaterialPropertyBlock block = this.EnsurePropertyBlock();
            block.Clear();
            block.SetColor(ColorProperty, color);

            Vector3 drawCenter = center + new Vector3(0f, yOffset, 0f);
            Matrix4x4 matrix = Matrix4x4.TRS(
                drawCenter,
                rotation,
                scale);
            Graphics.DrawMesh(
                mesh,
                matrix,
                material,
                0,
                null,
                0,
                block);
        }

        private MaterialPropertyBlock EnsurePropertyBlock()
        {
            if (this.propertyBlock == null)
            {
                this.propertyBlock = new MaterialPropertyBlock();
            }

            return this.propertyBlock;
        }
    }
}
