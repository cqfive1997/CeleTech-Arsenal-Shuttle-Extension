using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Profile
{
    internal static class HabitatJoySelectionUtility
    {
        internal const string HabitatJoyKindsConfigKey = "habitatJoyKinds";
        private const char Separator = '|';

        internal static IReadOnlyList<JoyKindDef> GetSelectedJoyKinds(
            ShuttleModule module,
            ShuttleHabitatModuleDef habitatDef)
        {
            if (habitatDef == null || !habitatDef.habitatSupportsJoy)
            {
                return new List<JoyKindDef>();
            }

            string rawValue;
            if (module != null &&
                module.TryGetInstanceConfig(HabitatJoyKindsConfigKey, out rawValue) &&
                !string.IsNullOrEmpty(rawValue))
            {
                List<JoyKindDef> configured = ParseJoyKinds(rawValue);
                List<JoyKindDef> sanitized = SanitizeSelection(
                    configured,
                    habitatDef.allowedJoyKinds,
                    habitatDef.habitatJoyKindCapacity);
                if (sanitized.Count > 0)
                {
                    return sanitized;
                }
            }

            List<JoyKindDef> defaults = SanitizeSelection(
                habitatDef.defaultJoyKinds,
                habitatDef.allowedJoyKinds,
                habitatDef.habitatJoyKindCapacity);
            if (defaults.Count > 0)
            {
                return defaults;
            }

            return SanitizeSelection(
                habitatDef.allowedJoyKinds,
                habitatDef.allowedJoyKinds,
                habitatDef.habitatJoyKindCapacity);
        }

        internal static List<JoyKindDef> SanitizeSelection(
            IReadOnlyList<JoyKindDef> selectedJoyKinds,
            IReadOnlyList<JoyKindDef> allowedJoyKinds,
            int capacity)
        {
            List<JoyKindDef> sanitized = new List<JoyKindDef>();
            if (selectedJoyKinds == null || allowedJoyKinds == null || capacity <= 0)
            {
                return sanitized;
            }

            for (int i = 0; i < selectedJoyKinds.Count; i++)
            {
                JoyKindDef joyKind = selectedJoyKinds[i];
                if (joyKind == null ||
                    sanitized.Contains(joyKind) ||
                    !ContainsJoyKind(allowedJoyKinds, joyKind))
                {
                    continue;
                }

                sanitized.Add(joyKind);
                if (sanitized.Count >= capacity)
                {
                    break;
                }
            }

            return sanitized;
        }

        internal static string SerializeSelection(IReadOnlyList<JoyKindDef> joyKinds)
        {
            if (joyKinds == null || joyKinds.Count == 0)
            {
                return string.Empty;
            }

            List<string> defNames = new List<string>();
            for (int i = 0; i < joyKinds.Count; i++)
            {
                JoyKindDef joyKind = joyKinds[i];
                if (joyKind != null && !string.IsNullOrEmpty(joyKind.defName))
                {
                    defNames.Add(joyKind.defName);
                }
            }

            return string.Join(Separator.ToString(), defNames.ToArray());
        }

        private static List<JoyKindDef> ParseJoyKinds(string rawValue)
        {
            List<JoyKindDef> joyKinds = new List<JoyKindDef>();
            if (string.IsNullOrEmpty(rawValue))
            {
                return joyKinds;
            }

            string[] parts = rawValue.Split(Separator);
            for (int i = 0; i < parts.Length; i++)
            {
                string defName = parts[i] != null ? parts[i].Trim() : null;
                if (string.IsNullOrEmpty(defName))
                {
                    continue;
                }

                JoyKindDef joyKind = DefDatabase<JoyKindDef>.GetNamedSilentFail(defName);
                if (joyKind != null && !joyKinds.Contains(joyKind))
                {
                    joyKinds.Add(joyKind);
                }
            }

            return joyKinds;
        }

        private static bool ContainsJoyKind(IReadOnlyList<JoyKindDef> joyKinds, JoyKindDef joyKind)
        {
            if (joyKinds == null || joyKind == null)
            {
                return false;
            }

            for (int i = 0; i < joyKinds.Count; i++)
            {
                if (joyKinds[i] == joyKind)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
