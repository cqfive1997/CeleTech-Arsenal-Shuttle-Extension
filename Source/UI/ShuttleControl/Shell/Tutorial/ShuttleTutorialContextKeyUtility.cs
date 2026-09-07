using CeleTech.ShuttleExtension.ModularShuttle.Defs;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Tutorial
{
    internal static class ShuttleTutorialContextKeyUtility
    {
        internal const string Cockpit = "cockpit";
        internal const string Power = "power";
        internal const string Cargo = "cargo";
        internal const string Weapon = "weapon";
        internal const string Living = "living";
        internal const string Support = "support";
        internal const string Generic = "generic";

        internal static string GetSegmentInstalledContextKey(
            ShuttleTutorialEvent tutorialEvent)
        {
            if (tutorialEvent == null ||
                tutorialEvent.Kind != ShuttleTutorialEventKind.SegmentInstalled)
            {
                return null;
            }

            return "segment-installed:" + GetSegmentInstalledContextKind(tutorialEvent);
        }

        internal static string GetSegmentInstalledContextKind(
            ShuttleTutorialEvent tutorialEvent)
        {
            if (tutorialEvent == null)
            {
                return Generic;
            }

            string text = ((tutorialEvent.SegmentKindKey ?? string.Empty) + " " +
                (tutorialEvent.SegmentDefName ?? string.Empty)).ToLowerInvariant();
            if (tutorialEvent.SegmentKindKey == ShuttleSegmentTypeCatalog.Cockpit)
            {
                return Cockpit;
            }

            if (tutorialEvent.SegmentKindKey == ShuttleSegmentTypeCatalog.Power)
            {
                return Power;
            }

            if (tutorialEvent.SegmentKindKey == ShuttleSegmentTypeCatalog.Cargo)
            {
                return Cargo;
            }

            if (tutorialEvent.SegmentKindKey == ShuttleSegmentTypeCatalog.Weapon)
            {
                return Weapon;
            }

            if (tutorialEvent.SegmentKindKey == ShuttleSegmentTypeCatalog.Living)
            {
                return Living;
            }

            if (tutorialEvent.SegmentKindKey == ShuttleSegmentTypeCatalog.Support)
            {
                return Support;
            }

            if (ContainsAny(text, "cockpit", "command", "control"))
            {
                return Cockpit;
            }

            if (ContainsAny(text, "power", "reactor", "battery"))
            {
                return Power;
            }

            if (ContainsAny(text, "cargo", "freight", "storage", "hold"))
            {
                return Cargo;
            }

            if (ContainsAny(text, "weapon", "turret", "ammo", "firecontrol", "fire-control"))
            {
                return Weapon;
            }

            if (ContainsAny(text, "living", "habitat", "crew"))
            {
                return Living;
            }

            if (ContainsAny(text, "support", "utility"))
            {
                return Support;
            }

            return Generic;
        }

        private static bool ContainsAny(string text, params string[] tokens)
        {
            if (string.IsNullOrEmpty(text) || tokens == null)
            {
                return false;
            }

            for (int i = 0; i < tokens.Length; i++)
            {
                string token = tokens[i];
                if (!string.IsNullOrEmpty(token) && text.Contains(token))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
