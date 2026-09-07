using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Tutorial
{
    internal sealed class ShuttleCrewTutorialDefinitionBuilder
    {
        internal ShuttleTutorialDefinition Build()
        {
            List<ShuttleTutorialStep> steps = new List<ShuttleTutorialStep>();
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.CrewOverviewPanel,
                "CT_Shuttle_Tutorial_Crew_Overview_Title",
                "CT_Shuttle_Tutorial_Crew_Overview_Body",
                ShuttleTutorialPlacement.Auto,
                6f,
                null,
                ShuttleTutorialPreAction.None,
                1.8f);
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.CrewOverviewPanel,
                "CT_Shuttle_Tutorial_Crew_Boarding_Title",
                "CT_Shuttle_Tutorial_Crew_Boarding_Body",
                ShuttleTutorialPlacement.Auto,
                6f,
                null,
                ShuttleTutorialPreAction.None,
                2.2f);
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.CrewHumansPanel,
                "CT_Shuttle_Tutorial_Crew_Humans_Title",
                "CT_Shuttle_Tutorial_Crew_Humans_Body",
                ShuttleTutorialPlacement.Right,
                6f,
                ShuttleTutorialTargetIds.CrewOverviewPanel,
                ShuttleTutorialPreAction.None,
                2.0f);
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.CrewMechanoidsPanel,
                "CT_Shuttle_Tutorial_Crew_Mechanoids_Title",
                "CT_Shuttle_Tutorial_Crew_Mechanoids_Body",
                ShuttleTutorialPlacement.Right,
                6f,
                ShuttleTutorialTargetIds.CrewOverviewPanel,
                ShuttleTutorialPreAction.None,
                2.0f,
                true);
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.CrewAnimalsPanel,
                "CT_Shuttle_Tutorial_Crew_AnimalsEntities_Title",
                "CT_Shuttle_Tutorial_Crew_AnimalsEntities_Body",
                ShuttleTutorialPlacement.Right,
                6f,
                null,
                ShuttleTutorialPreAction.None,
                2.0f,
                true);
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.CrewEntitiesPanel,
                "CT_Shuttle_Tutorial_Crew_Entities_Title",
                "CT_Shuttle_Tutorial_Crew_Entities_Body",
                ShuttleTutorialPlacement.Right,
                6f,
                null,
                ShuttleTutorialPreAction.None,
                2.0f,
                true);
            ShuttleTutorialStepFactory.Add(
                steps,
                ShuttleTutorialTargetIds.CrewHabitatOpsPanel,
                "CT_Shuttle_Tutorial_Crew_HabitatOps_Title",
                "CT_Shuttle_Tutorial_Crew_HabitatOps_Body",
                ShuttleTutorialPlacement.Left,
                6f,
                ShuttleTutorialTargetIds.CrewMessagesPanel,
                ShuttleTutorialPreAction.None,
                2.0f);
            return new ShuttleTutorialDefinition(ShuttleControlPageId.Crew, steps);
        }
    }
}
