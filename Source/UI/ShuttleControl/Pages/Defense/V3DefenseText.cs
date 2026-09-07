using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Defense
{
    internal sealed class V3DefenseText
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

        internal string GetAutoReloadLabel(
            bool autoReloadEnabled,
            bool autoLoaderAvailable)
        {
            if (!autoReloadEnabled)
            {
                return this.Tr("CT_Shuttle_WeaponAmmo_AutoReloadOff");
            }

            return this.Tr(
                autoLoaderAvailable
                    ? "CT_Shuttle_WeaponAmmo_AutoReloadLoaderColonist"
                    : "CT_Shuttle_WeaponAmmo_AutoReloadColonist");
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

            if (severityKey == ShuttleUIText.SeverityUnknown ||
                severityKey == ShuttleUIText.SeverityMissing)
            {
                return ShuttleUIStyle.MutedTextColor;
            }

            return GreenColor;
        }

        internal Color GetWeaponStatusColor(string statusKey)
        {
            if (statusKey == ShuttleUIText.StatusOnline)
            {
                return GreenColor;
            }

            if (statusKey == ShuttleUIText.StatusHoldFire)
            {
                return YellowColor;
            }

            if (statusKey == ShuttleUIText.StatusOffline)
            {
                return RedColor;
            }

            return ShuttleUIStyle.MutedTextColor;
        }

        internal Color GetShieldStatusColor(string statusKey)
        {
            if (statusKey == ShuttleUIText.StatusDown ||
                statusKey == ShuttleUIText.StatusUnpowered ||
                statusKey == ShuttleUIText.StatusBroken ||
                statusKey == ShuttleUIText.StatusOffline ||
                statusKey == ShuttleUIText.StatusMissing)
            {
                return RedColor;
            }

            if (statusKey == ShuttleUIText.StatusOverload ||
                statusKey == ShuttleUIText.StatusDisabled ||
                statusKey == ShuttleUIText.StatusRechargeBlocked ||
                statusKey == ShuttleUIText.StatusNoEnergy)
            {
                return YellowColor;
            }

            if (statusKey == ShuttleUIText.StatusNormal ||
                statusKey == ShuttleUIText.StatusOnline ||
                statusKey == ShuttleUIText.StatusFull ||
                statusKey == ShuttleUIText.StatusRecharging)
            {
                return GreenColor;
            }

            return ShuttleUIStyle.MutedTextColor;
        }

        internal Color GetShieldStrengthColor(V3DefenseShieldPanelModel shield)
        {
            if (shield == null || !shield.HasShield || shield.StrengthPct < 0f)
            {
                return ShuttleUIStyle.MutedTextColor;
            }

            if (shield.StrengthPct < 0.25f)
            {
                return RedColor;
            }

            if (shield.StrengthPct <= 0.60f)
            {
                return YellowColor;
            }

            return GreenColor;
        }

        internal Color GetHullStatusColor(string statusKey)
        {
            if (statusKey == ShuttleUIText.StatusBreached ||
                statusKey == ShuttleUIText.StatusCritical)
            {
                return RedColor;
            }

            if (statusKey == ShuttleUIText.StatusDamaged)
            {
                return YellowColor;
            }

            if (statusKey == ShuttleUIText.StatusNominal)
            {
                return GreenColor;
            }

            return ShuttleUIStyle.MutedTextColor;
        }

        internal Color GetHullIntegrityColor(ShuttleHullReadModel hull)
        {
            return this.GetHullStatusColor(hull != null ? hull.StatusKey : null);
        }

        internal string FormatHullHitPoints(float hitPoints)
        {
            return Mathf.Max(0f, hitPoints).ToString("0.#");
        }

        internal string FormatRange(float range)
        {
            return range >= 0f
                ? range.ToString("0.#") + " " + this.Tr("CT_Shuttle_Defense_Tiles")
                : this.Tr("CT_Shuttle_Defense_Unavailable");
        }

        internal string FitLabelText(string text, float width)
        {
            return ShuttleUILayout.FitSingleLineLabelText(text, width);
        }

        internal void AddTooltip(Rect rect, string tooltip)
        {
            ShuttleUITooltip.Tip(rect, tooltip);
        }
    }
}
