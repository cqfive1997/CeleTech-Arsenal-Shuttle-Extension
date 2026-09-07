using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx
{
    internal struct ShuttleFlightVfxThrusterVisualTuning
    {
        internal ShuttleFlightVfxThrusterVisualMode VisualMode;
        internal Vector2 VisualOffset;
        internal float VisualOriginInset;
        internal float VisualYawOffsetDegrees;
        internal float VisibleStartOffset;
        internal float MinVisibleLength;
        internal float MinVisibleWidth;

        internal ShuttleFlightVfxThrusterVisualTuning(
            ShuttleFlightVfxThrusterVisualMode visualMode,
            Vector2 visualOffset,
            float visualOriginInset,
            float visualYawOffsetDegrees,
            float visibleStartOffset,
            float minVisibleLength,
            float minVisibleWidth)
        {
            this.VisualMode = visualMode;
            this.VisualOffset = visualOffset;
            this.VisualOriginInset = visualOriginInset;
            this.VisualYawOffsetDegrees = visualYawOffsetDegrees;
            this.VisibleStartOffset = visibleStartOffset;
            this.MinVisibleLength = minVisibleLength;
            this.MinVisibleWidth = minVisibleWidth;
        }
    }
}
