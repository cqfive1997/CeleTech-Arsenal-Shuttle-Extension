using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Crew
{
    internal static class V3CrewVisuals
    {
        internal static Color GetRiskColor(V3CrewRiskLevel risk)
        {
            if (risk == V3CrewRiskLevel.Severe)
            {
                return V3CrewText.RedColor;
            }

            if (risk == V3CrewRiskLevel.Medium)
            {
                return V3CrewText.YellowColor;
            }

            if (risk == V3CrewRiskLevel.Normal)
            {
                return V3CrewText.GreenColor;
            }

            return ShuttleUIStyle.MutedTextColor;
        }

        internal static Color GetIssueColor(V3CrewIssueSeverity severity)
        {
            if (severity == V3CrewIssueSeverity.Severe)
            {
                return V3CrewText.RedColor;
            }

            if (severity == V3CrewIssueSeverity.Medium)
            {
                return V3CrewText.YellowColor;
            }

            if (severity == V3CrewIssueSeverity.Low)
            {
                return V3CrewText.BlueColor;
            }

            return ShuttleUIStyle.MutedTextColor;
        }

        internal static Color GetCompartmentColor(V3CrewCardSourceKind sourceKind)
        {
            if (sourceKind == V3CrewCardSourceKind.MedicalBay)
            {
                return V3CrewText.GreenColor;
            }

            if (sourceKind == V3CrewCardSourceKind.PrisonCell)
            {
                return V3CrewText.RedColor;
            }

            if (sourceKind == V3CrewCardSourceKind.MechCharger ||
                sourceKind == V3CrewCardSourceKind.Habitat)
            {
                return V3CrewText.BlueColor;
            }

            if (sourceKind == V3CrewCardSourceKind.Cockpit ||
                sourceKind == V3CrewCardSourceKind.CargoLoaded)
            {
                return V3CrewText.YellowColor;
            }

            return ShuttleUIStyle.MutedTextColor;
        }

        internal static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        internal static V3CrewRiskLevel ResolveRisk(
            List<V3CrewIssueModel> issues,
            bool hasDisplayData)
        {
            if (!hasDisplayData)
            {
                return V3CrewRiskLevel.Unknown;
            }

            if (issues == null || issues.Count == 0)
            {
                return V3CrewRiskLevel.Normal;
            }

            bool hasMedium = false;
            for (int i = 0; i < issues.Count; i++)
            {
                V3CrewIssueModel issue = issues[i];
                if (issue == null)
                {
                    continue;
                }

                if (issue.Severity == V3CrewIssueSeverity.Severe)
                {
                    return V3CrewRiskLevel.Severe;
                }

                if (issue.Severity == V3CrewIssueSeverity.Medium)
                {
                    hasMedium = true;
                }
            }

            return hasMedium ? V3CrewRiskLevel.Medium : V3CrewRiskLevel.Normal;
        }
    }
}
