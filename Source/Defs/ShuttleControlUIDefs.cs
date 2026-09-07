using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Defs
{
    public enum ShuttleControlPageKind
    {
        Main,
        Crew,
        Defense,
        Medical,
        PrisonCell,
        Loading,
        Processing,
        ExternalModules,
        Performance,
        Settings
    }

    public sealed class ShuttleControlPageDef : Def
    {
        public int order;
        public string iconKey;
        public ShuttleControlPageKind pageKind = ShuttleControlPageKind.Main;
        public bool enabled = true;
    }

    public sealed class ShuttleControlIconDef : Def
    {
        public string texPath;
        public float drawScale = 1f;
    }

    public sealed class ShuttleCrewKindDef : Def
    {
        public string iconKey;
        public Color cardColor = new Color(0.080f, 0.125f, 0.172f, 0.94f);
        public Color textColor = new Color(0.28f, 0.64f, 0.86f, 1f);
        public int order;
    }

    public sealed class ShuttleCrewActivityDef : Def
    {
        public string iconKey;
        public Color color = new Color(0.65f, 0.71f, 0.77f, 1f);
        public int order;
    }

    public sealed class ShuttleCargoCategoryUIDef : Def
    {
        public string shortLabel;
        public string iconKey;
        public Color color = new Color(0.28f, 0.64f, 0.86f, 1f);
        public int order;
    }

    public sealed class ShuttleMedicalSeverityDef : Def
    {
        public Color color = new Color(0.65f, 0.71f, 0.77f, 1f);
        public int priority;
        public string iconKey;
    }
}
