using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Crew
{
    internal sealed class V3CrewNeedIssueModelBuilder
    {
        internal void AddNeedIssues(V3CrewCardModel card)
        {
            if (card == null)
            {
                return;
            }

            this.AddNeedIssue(
                card,
                card.FoodPct,
                0.16f,
                0.32f,
                ShuttleUIText.Tr("CT_ShuttleCrew_Issue_SevereHunger"),
                ShuttleUIText.Tr("CT_ShuttleCrew_Issue_MinorHunger"),
                ShuttleUIText.Tr("CT_ShuttleCrew_Issue_HungerSummary"));
            this.AddNeedIssue(
                card,
                card.RestPct,
                0.16f,
                0.32f,
                ShuttleUIText.Tr("CT_ShuttleCrew_Issue_SevereRestLoss"),
                ShuttleUIText.Tr("CT_ShuttleCrew_Issue_LowRest"),
                ShuttleUIText.Tr("CT_ShuttleCrew_Issue_RestSummary"));
            this.AddNeedIssue(
                card,
                card.MoodPct,
                -1f,
                0.28f,
                null,
                ShuttleUIText.Tr("CT_ShuttleCrew_Issue_LowMood"),
                ShuttleUIText.Tr("CT_ShuttleCrew_Issue_MoodSummary"));
            this.AddNeedIssue(
                card,
                card.JoyPct,
                -1f,
                0.30f,
                null,
                ShuttleUIText.Tr("CT_ShuttleCrew_Issue_LowRecreation"),
                ShuttleUIText.Tr("CT_ShuttleCrew_Issue_RecreationSummary"));
        }

        private void AddNeedIssue(
            V3CrewCardModel card,
            float value01,
            float severeThreshold,
            float mediumThreshold,
            string severeLabel,
            string mediumLabel,
            string summary)
        {
            if (card == null || value01 < 0f)
            {
                return;
            }

            if (severeThreshold >= 0f &&
                value01 <= severeThreshold &&
                !string.IsNullOrEmpty(severeLabel))
            {
                V3CrewIssueUtility.AddIssue(
                    card.Issues,
                    severeLabel,
                    summary,
                    V3CrewIssueSeverity.Severe,
                    V3CrewIssueUtility.SevereHealthPriority + Mathf.RoundToInt(value01 * 10f));
                return;
            }

            if (mediumThreshold >= 0f &&
                value01 <= mediumThreshold &&
                !string.IsNullOrEmpty(mediumLabel))
            {
                V3CrewIssueUtility.AddIssue(
                    card.Issues,
                    mediumLabel,
                    summary,
                    V3CrewIssueSeverity.Medium,
                    V3CrewIssueUtility.LowNeedPriority + Mathf.RoundToInt(value01 * 10f));
            }
        }
    }
}
