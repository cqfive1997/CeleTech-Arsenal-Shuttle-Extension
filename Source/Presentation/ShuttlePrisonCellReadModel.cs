using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Presentation
{
    /// <summary>
    /// UI-facing Prison Cell snapshot. It exposes containment and command candidates only;
    /// prisoner status changes and holder mutation stay behind commands/services.
    /// </summary>
    public sealed class ShuttlePrisonCellReadModel
    {
        public bool HasPrisonCell;
        public int PrisonerSlots;
        public int PrisonerCount;
        public int FreePrisonerSlots;
        public bool CanAdmitMore;
        public bool SupportsCargoFoodSupply;
        public bool SupportsRefrigeratedCargoFoodSupply;
        public bool CargoFoodSupplyConfiguredEnabled = true;
        public bool RefrigeratedFoodSupplyConfiguredEnabled = true;
        public bool CargoFoodSupplyEnabled;
        public bool RefrigeratedCargoFoodSupplyEnabled;
        public bool RequiresCargoLogisticsForFoodSupply;
        public FoodPreferability MaximumCargoFoodPreferability =
            FoodPreferability.MealAwful;
        public string UnavailableReason;
        public IReadOnlyList<ShuttleHeldPrisonerReadModel> Prisoners =
            new List<ShuttleHeldPrisonerReadModel>();
        public IReadOnlyList<ShuttlePrisonerCandidateReadModel> Candidates =
            new List<ShuttlePrisonerCandidateReadModel>();
        public IReadOnlyList<ShuttlePrisonerCarrierReadModel> Carriers =
            new List<ShuttlePrisonerCarrierReadModel>();
        public IReadOnlyList<ShuttlePrisonerFeederReadModel> Feeders =
            new List<ShuttlePrisonerFeederReadModel>();
        public IReadOnlyList<ShuttlePrisonerDoctorReadModel> Doctors =
            new List<ShuttlePrisonerDoctorReadModel>();
    }

    public sealed class ShuttleHeldPrisonerReadModel
    {
        public int ThingIDNumber;
        public string Label;
        public string FactionLabel;
        public string HostFactionLabel;
        public string InteractionModeLabel;
        public float FoodLevelPercent = -1f;
        public string FoodLabel;
        public string HealthLabel;
        public bool IsDowned;
        public bool IsDead;
        public bool CanEject;
        public bool NeedsFeeding;
        public bool CareAvailabilityKnown;
        public bool CanFeed;
        public string FeedTooltip;
        public bool NeedsTending;
        public bool CanTend;
        public string TendTooltip;
        public string TendStatusLabel;
        public Thing DisplayThing;
    }

    public sealed class ShuttlePrisonerCandidateReadModel
    {
        public int ThingIDNumber;
        public string Label;
        public string FactionLabel;
        public string ReasonLabel;
        public bool IsExistingPrisoner;
        public bool IsDownedHostile;
        public bool CanAdmit;
        public string CannotAdmitReason;
        public Thing DisplayThing;
    }

    public sealed class ShuttlePrisonerCarrierReadModel
    {
        public int ThingIDNumber;
        public string Label;
        public bool CanCarry;
        public string CannotCarryReason;
        public Thing DisplayThing;
    }

    public sealed class ShuttlePrisonerFeederReadModel
    {
        public int ThingIDNumber;
        public string Label;
        public bool CanFeed;
        public string CannotFeedReason;
        public Thing DisplayThing;
    }

    public sealed class ShuttlePrisonerDoctorReadModel
    {
        public int ThingIDNumber;
        public string Label;
        public bool CanTend;
        public string CannotTendReason;
        public Thing DisplayThing;
    }
}
