using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Main
{
    internal sealed class V3MainText
    {
        internal static readonly Color CardColor =
            ShuttleUIStyle.CardColor;
        internal static readonly Color StrongCardColor =
            ShuttleUIStyle.SelectedColor;
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

        internal string Tr(string key, object arg0)
        {
            return ShuttleUIText.Tr(key, arg0);
        }

        internal string ValueOrDash(string value)
        {
            return ShuttleAssemblyDisplayTextResolver.ResolveSafeDisplayText(value);
        }

        internal string FormatCountPair(int current, int max)
        {
            return Mathf.Max(0, current).ToString() + "/" + Mathf.Max(0, max).ToString();
        }

        internal string FormatLaunchReady(float value01)
        {
            return ShuttleUIMetricFormatter.FormatPercent(Mathf.Clamp01(value01));
        }

        internal string FormatProgress(float value01)
        {
            return ShuttleUIMetricFormatter.FormatPercent(Mathf.Clamp01(value01));
        }

        internal string GetSegmentLabel(ShuttleControlSegmentSlotModel segment)
        {
            return ShuttleAssemblyDisplayTextResolver.ResolveSegmentDisplayName(segment);
        }

        internal string GetSegmentSlotLabel(ShuttleControlSegmentSlotModel segment)
        {
            return ShuttleAssemblyDisplayTextResolver.ResolveSegmentSlotDisplayName(
                segment,
                true);
        }

        internal string GetModuleLabel(ShuttleControlModuleSlotModel moduleSlot)
        {
            return ShuttleAssemblyDisplayTextResolver.ResolveModuleDisplayName(moduleSlot);
        }

        internal string GetModuleSlotLabel(
            ShuttleControlSegmentSlotModel segment,
            ShuttleControlModuleSlotModel moduleSlot)
        {
            return ShuttleAssemblyDisplayTextResolver.ResolveModuleSlotDisplayName(
                segment,
                moduleSlot,
                true);
        }

        internal string GetSegmentStatusLabel(
            ShuttleControlSegmentSlotModel segment,
            V3MainModuleSelection selection)
        {
            if (segment == null)
            {
                return "-";
            }

            if (segment.IsRemovalInProgress)
            {
                return this.ValueOrDash(segment.RemovalStatusLabel);
            }

            if (!selection.IsSegmentInstalled(segment))
            {
                return segment.IsRequired
                    ? this.Tr("CT_Shuttle_Main_Missing")
                    : this.Tr("CT_Shuttle_Main_OptionalSection");
            }

            return selection.HasAnyDisabledInstalledModule(segment)
                ? this.Tr("CT_Shuttle_Main_Partial")
                : this.Tr("CT_Shuttle_Main_Enabled");
        }

        internal string GetModuleStatusLabel(
            ShuttleControlModuleSlotModel moduleSlot,
            V3MainModuleSelection selection)
        {
            if (moduleSlot == null)
            {
                return "-";
            }

            if (moduleSlot.IsRemovalInProgress)
            {
                return this.ValueOrDash(moduleSlot.RemovalStatusLabel);
            }

            if (!selection.IsModuleInstalled(moduleSlot))
            {
                return moduleSlot.IsRequired
                    ? this.Tr("CT_Shuttle_Main_Missing")
                    : this.Tr("CT_Shuttle_Main_Empty");
            }

            return moduleSlot.InstalledModuleEnabled
                ? this.Tr("CT_Shuttle_Main_Enabled")
                : this.Tr("CT_Shuttle_Main_Disabled");
        }

        internal Color GetInstalledStateColor(bool installed, bool enabled, bool inProgress)
        {
            if (inProgress)
            {
                return YellowColor;
            }

            if (!installed)
            {
                return ShuttleUIStyle.MutedTextColor;
            }

            return enabled ? GreenColor : RedColor;
        }

        internal string FitLabelText(string value, float width)
        {
            return ShuttleUILayout.FitSingleLineLabelText(value, width);
        }

        internal Color GetSeverityColor(string severityKey)
        {
            if (severityKey == ShuttleUIText.SeverityCritical ||
                severityKey == ShuttleUIText.SeverityError)
            {
                return RedColor;
            }

            if (severityKey == ShuttleUIText.SeverityModerate ||
                severityKey == ShuttleUIText.SeverityWarning ||
                severityKey == ShuttleUIText.SeverityPending)
            {
                return YellowColor;
            }

            if (severityKey == ShuttleUIText.SeverityStable)
            {
                return GreenColor;
            }

            return ShuttleUIStyle.MutedTextColor;
        }

        internal string GetSeverityLabel(string severityKey)
        {
            if (severityKey == ShuttleUIText.SeverityCritical ||
                severityKey == ShuttleUIText.SeverityError)
            {
                return this.Tr("CT_Shuttle_InfoPanel_Summary_ErrorShort");
            }

            if (severityKey == ShuttleUIText.SeverityModerate ||
                severityKey == ShuttleUIText.SeverityWarning ||
                severityKey == ShuttleUIText.SeverityPending)
            {
                return this.Tr("CT_Shuttle_InfoPanel_Summary_WarningShort");
            }

            if (severityKey == ShuttleUIText.SeverityStable)
            {
                return this.Tr("CT_Shuttle_InfoPanel_Summary_Ready");
            }

            return this.Tr("CT_Shuttle_InfoPanel_Summary_InfoShort");
        }

        internal void AddTooltip(Rect rect, string tooltip)
        {
            ShuttleUITooltip.Tip(rect, tooltip);
        }
    }
}
