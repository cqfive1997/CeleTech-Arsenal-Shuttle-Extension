using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Presentation
{
    /// <summary>
    /// High-level activity currently represented inside the shuttle Habitat holder.
    /// </summary>
    public enum HabitatOccupantActivity
    {
        None,
        Sleeping,
        Dining,
        Joying
    }

    /// <summary>
    /// Need kinds surfaced by the Habitat panel without exposing mutable Pawn need objects.
    /// </summary>
    public enum HabitatNeedKind
    {
        None,
        Food,
        Rest,
        Joy,
        Mood
    }

    /// <summary>
    /// Immutable-by-convention UI snapshot for shuttle Habitat capability and occupants.
    /// The UI reads this model instead of querying CompShuttleHabitatOccupancy directly.
    /// </summary>
    public sealed class ShuttleHabitatReadModel
    {
        public bool HasHabitat;
        public bool SupportsSleep;
        public bool SupportsDining;
        public int SleepSlots;
        public int DiningSlots;
        public bool SupportsJoy;
        public int JoySlots;
        public int JoyOccupants;
        public int JoyKindCapacity;
        public int SleepingOccupants;
        public int DiningOccupants;
        public int TotalOccupants;
        public bool HasAnyOccupants;
        public bool CanEjectOccupants;
        public IReadOnlyList<ShuttleHabitatOccupantReadModel> Occupants = new List<ShuttleHabitatOccupantReadModel>();
        public IReadOnlyList<ShuttleHabitatJoyConfigReadModel> JoyConfigs = new List<ShuttleHabitatJoyConfigReadModel>();
    }

    /// <summary>
    /// UI-facing static configuration snapshot for one recreation-capable Habitat module.
    /// It carries defs for display only; mutations still go through SetHabitatJoyKindsCommand.
    /// </summary>
    public sealed class ShuttleHabitatJoyConfigReadModel
    {
        public ShuttleHabitatJoyConfigReadModel(
            string moduleInstanceID,
            string moduleLabel,
            int capacity,
            IReadOnlyList<JoyKindDef> allowedJoyKinds,
            IReadOnlyList<JoyKindDef> selectedJoyKinds)
        {
            this.ModuleInstanceID = moduleInstanceID;
            this.ModuleLabel = moduleLabel;
            this.Capacity = capacity;
            this.AllowedJoyKinds = allowedJoyKinds ?? new List<JoyKindDef>();
            this.SelectedJoyKinds = selectedJoyKinds ?? new List<JoyKindDef>();
        }

        public string ModuleInstanceID { get; private set; }
        public string ModuleLabel { get; private set; }
        public int Capacity { get; private set; }
        public IReadOnlyList<JoyKindDef> AllowedJoyKinds { get; private set; }
        public IReadOnlyList<JoyKindDef> SelectedJoyKinds { get; private set; }
    }

    /// <summary>
    /// UI-facing snapshot of one pawn held by Habitat sleep or dining logic.
    /// Percent values use -1 when the underlying need is unavailable.
    /// </summary>
    public sealed class ShuttleHabitatOccupantReadModel
    {
        public int PawnThingID;
        public string LabelShort;
        public string LabelCap;
        public Thing DisplayThing;
        public HabitatOccupantActivity Activity;
        public string ActivityLabelKey;
        public string FoodLabel;
        public float FoodPct = -1f;
        public float RestPct = -1f;
        public float JoyPct = -1f;
        public float MoodPct = -1f;
        public HabitatNeedKind MostUrgentNeed;
        public string MostUrgentNeedLabelKey;
        public float MostUrgentNeedPct = -1f;
        public bool IsCritical;
    }
}
