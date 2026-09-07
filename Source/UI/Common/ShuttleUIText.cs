using System;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common
{
    internal static class ShuttleUIText
    {
        internal const string StatusMissing = "Missing";
        internal const string StatusDisabled = "Disabled";
        internal const string StatusOffline = "Offline";
        internal const string StatusOnline = "Online";
        internal const string StatusBroken = "Broken";
        internal const string StatusRechargeBlocked = "RechargeBlocked";
        internal const string StatusNoEnergy = "NoEnergy";
        internal const string StatusFull = "Full";
        internal const string StatusRecharging = "Recharging";
        internal const string StatusUnpowered = "Unpowered";
        internal const string StatusDown = "Down";
        internal const string StatusOverload = "Overload";
        internal const string StatusNormal = "Normal";
        internal const string StatusHoldFire = "HoldFire";
        internal const string StatusDormant = "Dormant";
        internal const string StatusBreached = "Breached";
        internal const string StatusCritical = "Critical";
        internal const string StatusDamaged = "Damaged";
        internal const string StatusNominal = "Nominal";

        internal const string SeverityCritical = "Critical";
        internal const string SeverityError = "Error";
        internal const string SeverityModerate = "Moderate";
        internal const string SeverityWarning = "Warning";
        internal const string SeverityStable = "Stable";
        internal const string SeverityMissing = "Missing";
        internal const string SeverityPending = "Pending";
        internal const string SeverityUnknown = "Unknown";

        internal static string Tr(string key)
        {
            return string.IsNullOrEmpty(key) ? "-" : key.Translate().ToString();
        }

        internal static string Tr(string key, params NamedArgument[] args)
        {
            if (string.IsNullOrEmpty(key))
            {
                return "-";
            }

            return args != null && args.Length > 0
                ? key.Translate(args).ToString()
                : key.Translate().ToString();
        }

        internal static string Tr(string key, object arg0)
        {
            string text = Tr(key);
            try
            {
                return string.Format(text, arg0);
            }
            catch (FormatException)
            {
                return text;
            }
        }

        internal static string Tr(string key, object arg0, object arg1)
        {
            string text = Tr(key);
            try
            {
                return string.Format(text, arg0, arg1);
            }
            catch (FormatException)
            {
                return text;
            }
        }

        internal static string Tr(string key, object arg0, object arg1, object arg2)
        {
            string text = Tr(key);
            try
            {
                return string.Format(text, arg0, arg1, arg2);
            }
            catch (FormatException)
            {
                return text;
            }
        }

        internal static bool IsEnglishLanguage()
        {
            try
            {
                if (LanguageDatabase.activeLanguage == null)
                {
                    return false;
                }

                string folderName = LanguageDatabase.activeLanguage.folderName;
                return !string.IsNullOrEmpty(folderName) &&
                    folderName.IndexOf("english", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch
            {
                return false;
            }
        }

    }
}
