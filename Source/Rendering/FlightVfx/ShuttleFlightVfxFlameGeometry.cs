namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx
{
    internal struct ShuttleFlightVfxFlameGeometry
    {
        internal float BaseLength;
        internal float BaseWidth;

        internal ShuttleFlightVfxFlameGeometry(float baseLength, float baseWidth)
        {
            this.BaseLength = baseLength;
            this.BaseWidth = baseWidth;
        }

        internal static ShuttleFlightVfxFlameGeometry ForKind(ShuttleThrusterKind kind)
        {
            if (kind == ShuttleThrusterKind.TailMain)
            {
                return new ShuttleFlightVfxFlameGeometry(4.35f, 0.30f);
            }

            if (kind == ShuttleThrusterKind.RearVtol)
            {
                return new ShuttleFlightVfxFlameGeometry(1.20f, 0.22f);
            }

            return new ShuttleFlightVfxFlameGeometry(1.15f, 0.20f);
        }
    }
}
