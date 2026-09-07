using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx.Vapor
{
    internal struct ShuttleFlightVfxVaporAnchor
    {
        internal readonly string Id;
        internal readonly ShuttleFlightVfxVaporKind Kind;
        internal readonly Vector2 RootTextureUv;
        internal readonly Vector2 TailTextureUv;
        internal readonly Vector2 OutwardTextureUv;
        internal readonly bool HasCalibratedDirections;
        internal readonly float CenterOffsetLocal;
        internal readonly float LengthAxisDeg;
        internal readonly float OutwardNormalDeg;
        internal readonly Vector2 BaseSize;
        internal readonly float SizeMultiplier;
        internal readonly float LengthMultiplier;
        internal readonly float WidthMultiplier;
        internal readonly float StreamwiseOffsetLocal;
        internal readonly float Strength;
        internal readonly float PhaseOffset;
        internal readonly bool DrawOverHull;
        internal readonly int QualityRank;

        internal ShuttleFlightVfxVaporAnchor(
            string id,
            ShuttleFlightVfxVaporKind kind,
            Vector2 rootTextureUv,
            float centerOffsetLocal,
            float lengthAxisDeg,
            float outwardNormalDeg,
            Vector2 baseSize,
            float sizeMultiplier,
            float lengthMultiplier,
            float widthMultiplier,
            float streamwiseOffsetLocal,
            float strength,
            float phaseOffset,
            bool drawOverHull,
            int qualityRank)
            : this(
                id,
                kind,
                rootTextureUv,
                Vector2.zero,
                Vector2.zero,
                false,
                centerOffsetLocal,
                lengthAxisDeg,
                outwardNormalDeg,
                baseSize,
                sizeMultiplier,
                lengthMultiplier,
                widthMultiplier,
                streamwiseOffsetLocal,
                strength,
                phaseOffset,
                drawOverHull,
                qualityRank)
        {
        }

        internal ShuttleFlightVfxVaporAnchor(
            string id,
            ShuttleFlightVfxVaporKind kind,
            Vector2 rootTextureUv,
            Vector2 tailTextureUv,
            Vector2 outwardTextureUv,
            float centerOffsetLocal,
            float lengthAxisDeg,
            float outwardNormalDeg,
            Vector2 baseSize,
            float sizeMultiplier,
            float lengthMultiplier,
            float widthMultiplier,
            float streamwiseOffsetLocal,
            float strength,
            float phaseOffset,
            bool drawOverHull,
            int qualityRank)
            : this(
                id,
                kind,
                rootTextureUv,
                tailTextureUv,
                outwardTextureUv,
                true,
                centerOffsetLocal,
                lengthAxisDeg,
                outwardNormalDeg,
                baseSize,
                sizeMultiplier,
                lengthMultiplier,
                widthMultiplier,
                streamwiseOffsetLocal,
                strength,
                phaseOffset,
                drawOverHull,
                qualityRank)
        {
        }

        private ShuttleFlightVfxVaporAnchor(
            string id,
            ShuttleFlightVfxVaporKind kind,
            Vector2 rootTextureUv,
            Vector2 tailTextureUv,
            Vector2 outwardTextureUv,
            bool hasCalibratedDirections,
            float centerOffsetLocal,
            float lengthAxisDeg,
            float outwardNormalDeg,
            Vector2 baseSize,
            float sizeMultiplier,
            float lengthMultiplier,
            float widthMultiplier,
            float streamwiseOffsetLocal,
            float strength,
            float phaseOffset,
            bool drawOverHull,
            int qualityRank)
        {
            this.Id = id;
            this.Kind = kind;
            this.RootTextureUv = rootTextureUv;
            this.TailTextureUv = tailTextureUv;
            this.OutwardTextureUv = outwardTextureUv;
            this.HasCalibratedDirections = hasCalibratedDirections;
            this.CenterOffsetLocal = centerOffsetLocal;
            this.LengthAxisDeg = lengthAxisDeg;
            this.OutwardNormalDeg = outwardNormalDeg;
            this.BaseSize = baseSize;
            this.SizeMultiplier = sizeMultiplier;
            this.LengthMultiplier = lengthMultiplier;
            this.WidthMultiplier = widthMultiplier;
            this.StreamwiseOffsetLocal = streamwiseOffsetLocal;
            this.Strength = strength;
            this.PhaseOffset = phaseOffset;
            this.DrawOverHull = drawOverHull;
            this.QualityRank = qualityRank;
        }
    }
}
