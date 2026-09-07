using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Medical
{
    internal sealed class V3MedicalText
    {
        internal static readonly Color CardColor =
            ShuttleUIStyle.LeftColumnCardColor;
        internal static readonly Color StrongCardColor =
            ShuttleUIStyle.RightBottomCardColor;
        internal static readonly Color MutedCardColor =
            ShuttleUIStyle.MutedCardColor;
        internal static readonly Color AccentColor =
            ShuttleUIStyle.BlueStatusColor;
        internal static readonly Color GreenColor =
            ShuttleUIStyle.GreenStatusColor;
        internal static readonly Color YellowColor =
            ShuttleUIStyle.YellowStatusColor;
        internal static readonly Color RedColor =
            ShuttleUIStyle.RedStatusColor;
        internal static readonly Color BlueColor =
            ShuttleUIStyle.BlueStatusColor;

        internal string Tr(string key)
        {
            return ShuttleUIText.Tr(key);
        }

        internal string ValueOrDash(string value)
        {
            return ShuttleAssemblyDisplayTextResolver.ResolveSafeDisplayText(value);
        }

        internal string GetKindLabel(V3MedicalPatientCardModel patient)
        {
            return patient != null
                ? this.ValueOrDash(patient.KindFallbackLabel)
                : "-";
        }

        internal string GetStateLabel(V3MedicalPatientCardModel patient)
        {
            if (patient != null &&
                patient.IsActiveProcedurePatient &&
                !string.IsNullOrEmpty(patient.ActiveProcedureStatusLabel))
            {
                return patient.ActiveProcedureStatusLabel;
            }

            return patient != null
                ? this.ValueOrDash(patient.StateFallbackLabel)
                : "-";
        }

        internal string GetTriageLabel(V3MedicalPatientCardModel patient)
        {
            return patient != null
                ? this.ValueOrDash(patient.TriageFallbackLabel)
                : "-";
        }

        internal Color GetSeverityColor(string severityKey)
        {
            if (severityKey == "Critical" || severityKey == "Severe")
            {
                return RedColor;
            }

            if (severityKey == "Moderate" ||
                severityKey == "Worsening" ||
                severityKey == "Pending")
            {
                return YellowColor;
            }

            if (severityKey == "Unknown" || severityKey == "Missing")
            {
                return ShuttleUIStyle.MutedTextColor;
            }

            return GreenColor;
        }

        internal Color GetPatientCardColor(V3MedicalPatientCardModel patient)
        {
            if (patient == null)
            {
                return CardColor;
            }

            if (patient.Kind == V3MedicalPatientKind.Mechanoid)
            {
                return new Color(0.080f, 0.132f, 0.150f, 0.94f);
            }

            if (patient.Kind == V3MedicalPatientKind.Animal)
            {
                return new Color(0.094f, 0.118f, 0.142f, 0.94f);
            }

            return new Color(0.080f, 0.125f, 0.172f, 0.94f);
        }

        internal Color GetPatientKindTextColor(V3MedicalPatientCardModel patient)
        {
            if (patient == null)
            {
                return ShuttleUIStyle.MutedTextColor;
            }

            if (patient.Kind == V3MedicalPatientKind.Animal)
            {
                return GreenColor;
            }

            return BlueColor;
        }

        internal string GetPatientFallbackIconText(V3MedicalPatientCardModel patient)
        {
            if (patient == null)
            {
                return "?";
            }

            if (patient.Kind == V3MedicalPatientKind.Mechanoid)
            {
                return "M";
            }

            if (patient.Kind == V3MedicalPatientKind.Animal)
            {
                return "A";
            }

            return "H";
        }

        internal string FitLabelText(string text, float width)
        {
            return ShuttleUILayout.FitSingleLineLabelText(text, width);
        }

        internal void AddTooltip(Rect rect, string tooltip)
        {
            ShuttleUITooltip.Tip(rect, tooltip);
        }

        internal void DrawThingIcon(
            Rect rect,
            Thing displayThing,
            string fallbackText,
            string tooltip)
        {
            if (ShuttleThingIconDrawer.Draw(rect, displayThing))
            {
                return;
            }

            Widgets.DrawBoxSolid(rect, ShuttleUIStyle.IconFallbackBackgroundColor);
            ShuttleUILayout.DrawRectBorder(rect, ShuttleUIStyle.SubtleBorderColor, ShuttleUIStyle.ThinBorder);
            Color oldColor = GUI.color;
            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            GUI.color = BlueColor;
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            ShuttleUILayout.SafeLabel(rect, this.ValueOrDash(fallbackText));
            Text.Anchor = oldAnchor;
            Text.Font = oldFont;
            GUI.color = oldColor;
            this.AddTooltip(rect, tooltip);
        }
    }
}
