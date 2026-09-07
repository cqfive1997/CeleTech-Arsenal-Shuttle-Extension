using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Onboarding
{
    internal static class ShuttleStarterResearchTreePolicy
    {
        private const string AirframeDefName = "CT_Shuttle_ModularAirframe";
        private const string PowerBusDefName = "CT_Shuttle_InternalPowerBus";
        private const string CargoEngineeringDefName = "CT_Shuttle_CargoBayEngineering";
        private const string StandaloneResearchTabDefName = "CT_Shuttle_ResearchTab";

        private static readonly ResearchCoordinateProjection[] BasicCoordinates =
        {
            new ResearchCoordinateProjection(CargoEngineeringDefName, 2f, 1.4f),
            new ResearchCoordinateProjection("CT_Shuttle_ColdChainCargo", 3f, 0f),
            new ResearchCoordinateProjection("CT_Shuttle_MobileFabricationBay", 3f, 0.7f),
            new ResearchCoordinateProjection("CT_Shuttle_HabitatSupport", 3f, 1.4f),
            new ResearchCoordinateProjection("CT_Shuttle_MedicalEvacuationBay", 4f, 0f),
            new ResearchCoordinateProjection("CT_Shuttle_PrisonerContainment", 4f, 0.7f),
            new ResearchCoordinateProjection("CT_Shuttle_MechLogisticsInterface", 4f, 1.4f),
            new ResearchCoordinateProjection("CT_Shuttle_AutomaticAmmoLoading", 5f, 1.4f)
        };

        private static readonly FieldInfo ResolvedXField =
            AccessTools.Field(typeof(ResearchProjectDef), "x");
        private static readonly FieldInfo ResolvedYField =
            AccessTools.Field(typeof(ResearchProjectDef), "y");
        private static readonly FieldInfo CachedDescriptionField =
            AccessTools.Field(typeof(ResearchProjectDef), "cachedDescription");

        private static bool baselineCaptured;
        private static List<ResearchProjectDef> originalCargoPrerequisites;
        private static Dictionary<string, ResearchCoordinates> originalCoordinates;

        internal static void Apply(
            ShuttleStarterPresetMode mode,
            bool selectionResolved)
        {
            try
            {
                ResearchProjectDef airframe;
                ResearchProjectDef powerBus;
                ResearchProjectDef cargoEngineering;
                if (!TryResolveProjects(
                    out airframe,
                    out powerBus,
                    out cargoEngineering))
                {
                    Log.ErrorOnce(
                        "[CeleTech Shuttle] Starter preset research projects could not " +
                        "be resolved; the research-tree presentation was not changed.",
                        0x43A17F21);
                    return;
                }

                CaptureBaseline(cargoEngineering);
                bool basicMode = selectionResolved &&
                    mode == ShuttleStarterPresetMode.BasicFlightAndCargo;
                bool projectStandaloneStructure = basicMode &&
                    UsesStandaloneResearchTab(
                        airframe,
                        powerBus,
                        cargoEngineering);

                if (projectStandaloneStructure)
                {
                    cargoEngineering.prerequisites =
                        new List<ResearchProjectDef> { powerBus };
                    ApplyBasicCoordinates();
                }
                else
                {
                    cargoEngineering.prerequisites =
                        CloneProjects(originalCargoPrerequisites);
                    RestoreOriginalCoordinates();
                }

                ClearPresentationCaches(airframe, powerBus, cargoEngineering);
                if (projectStandaloneStructure)
                {
                    PauseInvalidActiveResearch(powerBus, cargoEngineering);
                }
            }
            catch (Exception exception)
            {
                Log.ErrorOnce(
                    "[CeleTech Shuttle] Starter preset research-tree projection " +
                    "failed: " + exception,
                    0x43A17F22);
            }
        }

        internal static bool TryGetBasicLabel(
            ResearchProjectDef project,
            out TaggedString label)
        {
            label = default(TaggedString);
            if (!ShuttleStarterPresetBuildPolicy.IsBasicModeActive || project == null)
            {
                return false;
            }

            string key = GetBasicLabelKey(project.defName);
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            label = key.Translate();
            return true;
        }

        internal static bool TryGetBasicDescription(
            ResearchProjectDef project,
            out string description)
        {
            description = null;
            if (!ShuttleStarterPresetBuildPolicy.IsBasicModeActive || project == null)
            {
                return false;
            }

            string key = GetBasicDescriptionKey(project.defName);
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            description = key.Translate().ToString();
            return true;
        }

        private static bool TryResolveProjects(
            out ResearchProjectDef airframe,
            out ResearchProjectDef powerBus,
            out ResearchProjectDef cargoEngineering)
        {
            airframe = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(
                AirframeDefName);
            powerBus = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(
                PowerBusDefName);
            cargoEngineering = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(
                CargoEngineeringDefName);
            return airframe != null && powerBus != null && cargoEngineering != null;
        }

        private static bool UsesStandaloneResearchTab(
            ResearchProjectDef airframe,
            ResearchProjectDef powerBus,
            ResearchProjectDef cargoEngineering)
        {
            ResearchTabDef standaloneTab = airframe.tab;
            if (standaloneTab == null ||
                standaloneTab.defName != StandaloneResearchTabDefName ||
                powerBus.tab != standaloneTab ||
                cargoEngineering.tab != standaloneTab)
            {
                return false;
            }

            for (int i = 0; i < BasicCoordinates.Length; i++)
            {
                ResearchProjectDef project =
                    DefDatabase<ResearchProjectDef>.GetNamedSilentFail(
                        BasicCoordinates[i].DefName);
                if (project != null && project.tab != standaloneTab)
                {
                    return false;
                }
            }

            return true;
        }

        private static void CaptureBaseline(ResearchProjectDef cargoEngineering)
        {
            if (baselineCaptured)
            {
                return;
            }

            originalCargoPrerequisites = CloneProjects(
                cargoEngineering.prerequisites);
            originalCoordinates = new Dictionary<string, ResearchCoordinates>();
            for (int i = 0; i < BasicCoordinates.Length; i++)
            {
                ResearchProjectDef project =
                    DefDatabase<ResearchProjectDef>.GetNamedSilentFail(
                        BasicCoordinates[i].DefName);
                if (project == null)
                {
                    continue;
                }

                originalCoordinates[project.defName] = new ResearchCoordinates(
                    project.researchViewX,
                    project.researchViewY,
                    project.ResearchViewX,
                    project.ResearchViewY);
            }

            baselineCaptured = true;
        }

        private static List<ResearchProjectDef> CloneProjects(
            List<ResearchProjectDef> projects)
        {
            return projects != null
                ? new List<ResearchProjectDef>(projects)
                : null;
        }

        private static void ApplyBasicCoordinates()
        {
            for (int i = 0; i < BasicCoordinates.Length; i++)
            {
                ResearchCoordinateProjection projection = BasicCoordinates[i];
                ResearchProjectDef project =
                    DefDatabase<ResearchProjectDef>.GetNamedSilentFail(
                        projection.DefName);
                if (project != null)
                {
                    SetCoordinates(
                        project,
                        projection.X,
                        projection.Y,
                        projection.X,
                        projection.Y);
                }
            }
        }

        private static void RestoreOriginalCoordinates()
        {
            if (originalCoordinates == null)
            {
                return;
            }

            foreach (KeyValuePair<string, ResearchCoordinates> pair in
                originalCoordinates)
            {
                ResearchProjectDef project =
                    DefDatabase<ResearchProjectDef>.GetNamedSilentFail(pair.Key);
                if (project != null)
                {
                    SetCoordinates(
                        project,
                        pair.Value.AuthoredX,
                        pair.Value.AuthoredY,
                        pair.Value.ResolvedX,
                        pair.Value.ResolvedY);
                }
            }
        }

        private static void SetCoordinates(
            ResearchProjectDef project,
            float authoredX,
            float authoredY,
            float resolvedX,
            float resolvedY)
        {
            project.researchViewX = authoredX;
            project.researchViewY = authoredY;

            if (ResolvedXField == null || ResolvedYField == null)
            {
                Log.WarningOnce(
                    "[CeleTech Shuttle] RimWorld research coordinate fields were not " +
                    "found; the starter research labels and dependencies will still " +
                    "change, but the node position cannot update immediately.",
                    0x43A17F23);
                return;
            }

            ResolvedXField.SetValue(project, resolvedX);
            ResolvedYField.SetValue(project, resolvedY);
        }

        private static void ClearPresentationCaches(
            ResearchProjectDef airframe,
            ResearchProjectDef powerBus,
            ResearchProjectDef cargoEngineering)
        {
            ClearPresentationCache(airframe);
            ClearPresentationCache(powerBus);
            ClearPresentationCache(cargoEngineering);
        }

        private static void ClearPresentationCache(ResearchProjectDef project)
        {
            project.ClearCachedData();
            if (CachedDescriptionField != null)
            {
                CachedDescriptionField.SetValue(project, null);
                return;
            }

            Log.WarningOnce(
                "[CeleTech Shuttle] RimWorld's cached research description field was " +
                "not found; research tooltips may refresh only after restarting.",
                0x43A17F24);
        }

        private static void PauseInvalidActiveResearch(
            ResearchProjectDef powerBus,
            ResearchProjectDef cargoEngineering)
        {
            if (Current.Game == null ||
                Find.ResearchManager == null ||
                powerBus.IsFinished ||
                cargoEngineering.IsFinished ||
                !Find.ResearchManager.IsCurrentProject(cargoEngineering))
            {
                return;
            }

            Find.ResearchManager.StopProject(cargoEngineering);
            if (Current.ProgramState == ProgramState.Playing)
            {
                Messages.Message(
                    "CT_Shuttle_StarterPreset_ResearchPausedForBasicSequence".Translate(),
                    MessageTypeDefOf.NeutralEvent,
                    false);
            }
        }

        private static string GetBasicLabelKey(string defName)
        {
            switch (defName)
            {
                case AirframeDefName:
                    return "CT_Shuttle_Research_BasicShuttleI_Label";
                case PowerBusDefName:
                    return "CT_Shuttle_Research_BasicShuttleII_Label";
                case CargoEngineeringDefName:
                    return "CT_Shuttle_Research_BasicShuttleIII_Label";
                default:
                    return null;
            }
        }

        private static string GetBasicDescriptionKey(string defName)
        {
            switch (defName)
            {
                case AirframeDefName:
                    return "CT_Shuttle_Research_BasicShuttleI_Description";
                case PowerBusDefName:
                    return "CT_Shuttle_Research_BasicShuttleII_Description";
                case CargoEngineeringDefName:
                    return "CT_Shuttle_Research_BasicShuttleIII_Description";
                default:
                    return null;
            }
        }

        private sealed class ResearchCoordinateProjection
        {
            internal ResearchCoordinateProjection(string defName, float x, float y)
            {
                this.DefName = defName;
                this.X = x;
                this.Y = y;
            }

            internal string DefName { get; private set; }

            internal float X { get; private set; }

            internal float Y { get; private set; }
        }

        private struct ResearchCoordinates
        {
            internal ResearchCoordinates(
                float authoredX,
                float authoredY,
                float resolvedX,
                float resolvedY)
            {
                this.AuthoredX = authoredX;
                this.AuthoredY = authoredY;
                this.ResolvedX = resolvedX;
                this.ResolvedY = resolvedY;
            }

            internal float AuthoredX;
            internal float AuthoredY;
            internal float ResolvedX;
            internal float ResolvedY;
        }
    }
}
