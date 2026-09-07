using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx
{
    internal static class ShuttleFlightVfxThrusterVisualTuningProvider
    {
        private static readonly Vector2 NoOffset = Vector2.zero;

        internal static ShuttleFlightVfxThrusterVisualTuning ForAnchor(
            ShuttleThrusterAnchor anchor)
        {
            if (anchor.Kind == ShuttleThrusterKind.TailMain)
            {
                return ForTailMain(anchor);
            }

            if (anchor.Kind == ShuttleThrusterKind.BellyVtol)
            {
                return ForBottomVtol(anchor);
            }

            if (anchor.Kind == ShuttleThrusterKind.RearVtol)
            {
                return ForBottomVtol(anchor);
            }

            return new ShuttleFlightVfxThrusterVisualTuning(
                ShuttleFlightVfxThrusterVisualMode.BellyParallel,
                NoOffset,
                0f,
                0f,
                0f,
                0f,
                0f);
        }

        private static ShuttleFlightVfxThrusterVisualTuning ForTailMain(
            ShuttleThrusterAnchor anchor)
        {
            Vector2 visualOffset = NoOffset;
            if (anchor.Id == "TailMainUpper")
            {
                visualOffset = new Vector2(0f, 0.12f);
            }

            return new ShuttleFlightVfxThrusterVisualTuning(
                ShuttleFlightVfxThrusterVisualMode.TailMain,
                visualOffset,
                0f,
                0f,
                0f,
                0f,
                0f);
        }

        private static ShuttleFlightVfxThrusterVisualTuning ForBottomVtol(
            ShuttleThrusterAnchor anchor)
        {
            bool forwardBellyNozzle = anchor.Kind == ShuttleThrusterKind.BellyVtol;
            float visualOriginInset = forwardBellyNozzle ? 0.20f : 0.10f;
            float visibleStartOffset = forwardBellyNozzle ? 0.24f : 0.10f;
            float minVisibleLength = forwardBellyNozzle ? 0.85f : 0.58f;
            float minVisibleWidth = forwardBellyNozzle ? 0.24f : 0.20f;

            return new ShuttleFlightVfxThrusterVisualTuning(
                ShuttleFlightVfxThrusterVisualMode.BellyParallel,
                NoOffset,
                visualOriginInset,
                0f,
                visibleStartOffset,
                minVisibleLength,
                minVisibleWidth);
        }
    }
}
