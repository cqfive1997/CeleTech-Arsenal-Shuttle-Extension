using System;
using System.Reflection;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using CeleTech.ShuttleExtension.ModularShuttle.World;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Launch
{
    internal static class ShuttleLaunchArrivalReflectionUtility
    {
        private const string ReflectionLogCategory = "ArrivalReflection";

        private static readonly FieldInfo LandInSpecificCellLandInShuttleField =
            typeof(TransportersArrivalAction_LandInSpecificCell).GetField(
                "landInShuttle",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo VisitSiteSiteField =
            typeof(TransportersArrivalAction_VisitSite).GetField(
                "site",
                BindingFlags.Instance | BindingFlags.NonPublic);

        internal static bool TryReadsLandInShuttle(TransportersArrivalAction_LandInSpecificCell arrivalAction)
        {
            if (arrivalAction == null)
            {
                return false;
            }

            if (LandInSpecificCellLandInShuttleField == null)
            {
                WarnMissingReflectionMemberOnce(
                    "MissingLandInSpecificCellLandInShuttleField",
                    "TransportersArrivalAction_LandInSpecificCell.landInShuttle",
                    "holder launch transfer arrival compatibility",
                    "holder transfer will treat this arrival action as unsupported");
                return false;
            }

            object value;
            try
            {
                value = LandInSpecificCellLandInShuttleField.GetValue(arrivalAction);
            }
            catch (Exception exception)
            {
                WarnReflectionReadFailedOnce(
                    "LandInSpecificCellLandInShuttleFieldReadFailed",
                    "TransportersArrivalAction_LandInSpecificCell.landInShuttle",
                    "holder launch transfer arrival compatibility",
                    "holder transfer will treat this arrival action as unsupported",
                    exception);
                return false;
            }

            return value is bool && (bool)value;
        }

        internal static bool IsModularShuttleSpecificCellArrival(TransportersArrivalAction arrivalAction)
        {
            return arrivalAction is ModularShuttleLandInSpecificCellArrivalAction;
        }

        internal static Site TryReadVisitSite(TransportersArrivalAction_VisitSite arrivalAction)
        {
            if (arrivalAction == null)
            {
                return null;
            }

            if (VisitSiteSiteField == null)
            {
                WarnMissingReflectionMemberOnce(
                    "MissingVisitSiteSiteFieldForHolderTransfer",
                    "TransportersArrivalAction_VisitSite.site",
                    "holder launch transfer visit-site compatibility",
                    "holder transfer will treat this arrival action as unsupported");
                return null;
            }

            Site site;
            try
            {
                site = VisitSiteSiteField.GetValue(arrivalAction) as Site;
            }
            catch (Exception exception)
            {
                WarnReflectionReadFailedOnce(
                    "VisitSiteSiteFieldReadFailedForHolderTransfer",
                    "TransportersArrivalAction_VisitSite.site",
                    "holder launch transfer visit-site compatibility",
                    "holder transfer will treat this arrival action as unsupported",
                    exception);
                return null;
            }

            if (site == null || !site.Spawned)
            {
                return null;
            }

            if (site.def == null ||
                site.def.mapGenerator == null ||
                site.def.mapGenerator == MapGeneratorDefOf.Space ||
                site.EnterCooldownBlocksEntering())
            {
                return null;
            }

            return site;
        }

        private static void WarnMissingReflectionMemberOnce(
            string key,
            string memberName,
            string feature,
            string fallback)
        {
            ShuttleLog.WarnOnce(
                ReflectionLogCategory,
                key,
                "Could not find " + memberName + ". " +
                "Affected compatibility feature: " + feature + ". " +
                "Fallback: " + fallback + ".");
        }

        private static void WarnReflectionReadFailedOnce(
            string key,
            string memberName,
            string feature,
            string fallback,
            Exception exception)
        {
            ShuttleLog.WarnOnce(
                ReflectionLogCategory,
                key,
                "Could not read " + memberName + ". " +
                "Affected compatibility feature: " + feature + ". " +
                "Fallback: " + fallback + ". reason=" +
                (exception != null ? exception.GetType().Name + ": " + exception.Message : "<unknown>"));
        }
    }
}
