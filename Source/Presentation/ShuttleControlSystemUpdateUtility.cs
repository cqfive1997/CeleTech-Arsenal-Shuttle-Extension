using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Presentation
{
    internal static class ShuttleControlSystemUpdateUtility
    {
        internal const float TotalDuration = 8f;
        internal const string TitleKey = "CT_ShuttleControlV3_SystemUpdate_Title";
        internal const string PreparedMessageKey = "CT_Shuttle_ControlPanel_SystemUpdatePrepared_VersionChanged";
        internal const string FirstStepKey = "CT_ShuttleControlV3_SystemUpdate_Step_VerifyPackage";
        internal const string LastStepKey = "CT_ShuttleControlV3_SystemUpdate_Step_VerifyUpdate";

        private static readonly string[] MiddleStepKeys =
        {
            "CT_ShuttleControlV3_SystemUpdate_Step_UpgradeFireControl",
            "CT_ShuttleControlV3_SystemUpdate_Step_UpgradeMedical",
            "CT_ShuttleControlV3_SystemUpdate_Step_UpgradeLogistics",
            "CT_ShuttleControlV3_SystemUpdate_Step_UpgradeWorkshop",
            "CT_ShuttleControlV3_SystemUpdate_Step_UpgradeHabitat"
        };

        internal static List<string> BuildRandomStepKeys()
        {
            List<string> candidates = new List<string>(MiddleStepKeys);
            string firstMiddle = TakeRandom(candidates);
            string secondMiddle = TakeRandom(candidates);
            return BuildSequence(firstMiddle, secondMiddle);
        }

        internal static List<string> SanitizeStepKeys(IReadOnlyList<string> stepKeys)
        {
            if (stepKeys == null || stepKeys.Count != 4)
            {
                return BuildDefaultStepKeys();
            }

            string firstMiddle = IsValidMiddleStep(stepKeys[1]) ? stepKeys[1] : MiddleStepKeys[0];
            string secondMiddle = IsValidMiddleStep(stepKeys[2]) && stepKeys[2] != firstMiddle
                ? stepKeys[2]
                : MiddleStepKeys[1];

            if (secondMiddle == firstMiddle)
            {
                secondMiddle = MiddleStepKeys[2];
            }

            return BuildSequence(firstMiddle, secondMiddle);
        }

        internal static string GetStepKeyForElapsed(IReadOnlyList<string> stepKeys, float elapsed)
        {
            List<string> sanitized = SanitizeStepKeys(stepKeys);
            int index = UnityEngine.Mathf.Clamp(
                UnityEngine.Mathf.FloorToInt(UnityEngine.Mathf.Max(0f, elapsed) / 2f),
                0,
                sanitized.Count - 1);
            return sanitized[index];
        }

        private static List<string> BuildDefaultStepKeys()
        {
            return BuildSequence(MiddleStepKeys[0], MiddleStepKeys[1]);
        }

        private static List<string> BuildSequence(string firstMiddle, string secondMiddle)
        {
            return new List<string>
            {
                FirstStepKey,
                firstMiddle,
                secondMiddle,
                LastStepKey
            };
        }

        private static string TakeRandom(List<string> candidates)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return MiddleStepKeys[0];
            }

            int index = Rand.RangeInclusive(0, candidates.Count - 1);
            string result = candidates[index];
            candidates.RemoveAt(index);
            return result;
        }

        private static bool IsValidMiddleStep(string stepKey)
        {
            if (string.IsNullOrEmpty(stepKey))
            {
                return false;
            }

            for (int i = 0; i < MiddleStepKeys.Length; i++)
            {
                if (MiddleStepKeys[i] == stepKey)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
