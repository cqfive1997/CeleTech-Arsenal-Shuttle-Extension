using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Commands
{
    public sealed class SetShuttlePaintSchemeCommand : IShuttleCommand
    {
        public const string ID = "set-shuttle-paint-scheme";

        public SetShuttlePaintSchemeCommand(ShuttlePaintSchemeSnapshot scheme)
        {
            ShuttlePaintSchemeSnapshot copy = scheme != null ? scheme.Clone() : ShuttlePaintSchemeSnapshot.Default;
            this.Enabled = copy.Enabled;
            this.PrimaryColor = copy.PrimaryColor;
            this.SecondaryColor = copy.SecondaryColor;
            this.AccentColor = copy.AccentColor;
            this.PresetKey = copy.PresetKey;
        }

        public string CommandID
        {
            get
            {
                return ID;
            }
        }

        public bool Enabled { get; private set; }
        public Color PrimaryColor { get; private set; }
        public Color SecondaryColor { get; private set; }
        public Color AccentColor { get; private set; }
        public string PresetKey { get; private set; }
    }

    public sealed class ResetShuttlePaintSchemeCommand : IShuttleCommand
    {
        public const string ID = "reset-shuttle-paint-scheme";

        public string CommandID
        {
            get
            {
                return ID;
            }
        }
    }
}
