namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Cargo
{
    internal sealed class V3CargoProjectionKey
    {
        internal static readonly V3CargoProjectionKey Empty =
            new V3CargoProjectionKey(0, 0, 0, 0, 0, 0);

        internal readonly int ProfileRevision;
        internal readonly int CargoSnapshotRevision;
        internal readonly int CargoSnapshotShapeKey;
        internal readonly int ControlShapeKey;
        internal readonly int CargoRegionSettingsShapeKey;
        internal readonly int UISettingShapeKey;

        internal V3CargoProjectionKey(
            int profileRevision,
            int cargoSnapshotRevision,
            int cargoSnapshotShapeKey,
            int controlShapeKey,
            int cargoRegionSettingsShapeKey,
            int uiSettingShapeKey)
        {
            this.ProfileRevision = profileRevision;
            this.CargoSnapshotRevision = cargoSnapshotRevision;
            this.CargoSnapshotShapeKey = cargoSnapshotShapeKey;
            this.ControlShapeKey = controlShapeKey;
            this.CargoRegionSettingsShapeKey = cargoRegionSettingsShapeKey;
            this.UISettingShapeKey = uiSettingShapeKey;
        }

        internal bool Matches(V3CargoProjectionKey other)
        {
            return other != null &&
                this.ProfileRevision == other.ProfileRevision &&
                this.CargoSnapshotRevision == other.CargoSnapshotRevision &&
                this.CargoSnapshotShapeKey == other.CargoSnapshotShapeKey &&
                this.ControlShapeKey == other.ControlShapeKey &&
                this.CargoRegionSettingsShapeKey ==
                    other.CargoRegionSettingsShapeKey &&
                this.UISettingShapeKey == other.UISettingShapeKey;
        }

        public override bool Equals(object obj)
        {
            return this.Matches(obj as V3CargoProjectionKey);
        }

        public override int GetHashCode()
        {
            int key = 17;
            key = AddHash(key, this.ProfileRevision);
            key = AddHash(key, this.CargoSnapshotRevision);
            key = AddHash(key, this.CargoSnapshotShapeKey);
            key = AddHash(key, this.ControlShapeKey);
            key = AddHash(key, this.CargoRegionSettingsShapeKey);
            key = AddHash(key, this.UISettingShapeKey);
            return key;
        }

        private static int AddHash(int key, int value)
        {
            unchecked
            {
                return (key * 31) + value;
            }
        }
    }
}
