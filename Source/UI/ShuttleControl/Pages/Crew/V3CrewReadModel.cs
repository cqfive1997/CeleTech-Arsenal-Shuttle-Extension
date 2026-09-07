using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Crew
{
    internal enum V3CrewGroupKind
    {
        Human,
        Mech,
        Animal,
        Entity
    }

    internal enum V3CrewCardSourceKind
    {
        Unknown,
        Cockpit,
        CargoLoaded,
        Habitat,
        MedicalBay,
        MechCharger,
        PrisonCell
    }

    internal enum V3CrewRiskLevel
    {
        Unknown,
        Normal,
        Medium,
        Severe
    }

    internal enum V3CrewIssueSeverity
    {
        Info,
        Low,
        Medium,
        Severe
    }

    internal sealed class V3CrewIssueModel
    {
        internal string Label;
        internal string Summary;
        internal V3CrewIssueSeverity Severity;
        internal int Priority;
    }

    internal sealed class V3CrewPageData
    {
        internal readonly List<V3CrewCardModel> Humans = new List<V3CrewCardModel>();
        internal readonly List<V3CrewCardModel> Mechs = new List<V3CrewCardModel>();
        internal readonly List<V3CrewCardModel> Animals = new List<V3CrewCardModel>();
        internal readonly List<V3CrewCardModel> Entities = new List<V3CrewCardModel>();
        internal int CockpitPawnCount;
        internal int TotalCount;
    }

    internal sealed class V3CrewCardModel
    {
        internal V3CrewGroupKind Group;
        internal V3CrewCardSourceKind SourceKind;
        internal Thing DisplayThing;
        internal string Label;
        internal int TransporterIndex = -1;
        internal int LoadedIndex = -1;
        internal int ThingIDNumber;
        internal string DefName;
        internal string KindKey;
        internal string ActivityKey;
        internal string CategoryLabel;
        internal string ActivityLabel;
        internal string TitleLabel;
        internal string IdentityLabel;
        internal string CurrentActivityLabel;
        internal string CompartmentLabel;
        internal string CompartmentTooltip;
        internal string IssueTooltip;
        internal string Note;
        internal string FallbackIconText;
        internal string AppearanceTooltip;
        internal float FoodPct = -1f;
        internal float RestPct = -1f;
        internal float MoodPct = -1f;
        internal float JoyPct = -1f;
        internal float EnergyPct = -1f;
        internal Color CardColor;
        internal Color CategoryTextColor;
        internal Color ActivityTextColor;
        internal bool HasCrewKindDisplay;
        internal bool HasCrewActivityDisplay;
        internal V3CrewRiskLevel RiskLevel = V3CrewRiskLevel.Unknown;
        internal readonly List<V3CrewIssueModel> Issues = new List<V3CrewIssueModel>();
    }
}
