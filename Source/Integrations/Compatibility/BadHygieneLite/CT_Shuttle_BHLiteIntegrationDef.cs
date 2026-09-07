using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.AdditionalModule.Compatibility.BadHygieneLite
{
    public sealed class CT_Shuttle_BHLiteIntegrationDef : Def
    {
        public List<string> packageIds = new List<string>();
        public List<string> fullDbhPackageIds = new List<string>();
        public string runtimeSystemKey;
        public string panelSystemKey;
        public string runtimeLabelKey;
        public string runtimeDescriptionKey;
        public string hygieneNeedDefName;
        public string bladderNeedDefName;
        public string thirstNeedDefName;
        public int diagnosticIntervalTicks = 600;
        public float powerDemandWatts = 40f;
        public bool serviceEnabled = true;
        public int serviceIntervalTicks = 600;
        public int maxPawnsPerService = 4;
        public float hygieneServiceThreshold = 0.65f;
        public float hygieneServiceGain = 0.05f;
        public float hygieneServiceTarget = 0.75f;
        public float bladderServiceThreshold = 0.35f;
        public float bladderServiceTarget = 0.60f;
        public int bladderDumpCallsPerService = 30;
        public bool requireInternalBusPowered = true;
        public bool thirstServiceEnabled = false;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }

            EnsureList(ref packageIds);
            EnsureList(ref fullDbhPackageIds);

            if (packageIds.Count == 0)
            {
                yield return defName + " must define at least one packageIds entry.";
            }

            if (fullDbhPackageIds.Count == 0)
            {
                yield return defName + " must define at least one fullDbhPackageIds entry.";
            }

            foreach (string error in ValidatePackageIds(packageIds, "packageIds"))
            {
                yield return defName + " " + error;
            }

            foreach (string error in ValidatePackageIds(fullDbhPackageIds, "fullDbhPackageIds"))
            {
                yield return defName + " " + error;
            }

            if (string.IsNullOrWhiteSpace(runtimeSystemKey) || !LooksLikeFullKey(runtimeSystemKey))
            {
                yield return defName + " must define runtimeSystemKey in owner.package/local-key format.";
            }

            if (string.IsNullOrWhiteSpace(panelSystemKey) || !LooksLikeFullKey(panelSystemKey))
            {
                yield return defName + " must define panelSystemKey in owner.package/local-key format.";
            }

            if (string.IsNullOrWhiteSpace(runtimeLabelKey))
            {
                yield return defName + " must define runtimeLabelKey.";
            }

            if (string.IsNullOrWhiteSpace(runtimeDescriptionKey))
            {
                yield return defName + " must define runtimeDescriptionKey.";
            }

            if (string.IsNullOrWhiteSpace(hygieneNeedDefName))
            {
                yield return defName + " must define hygieneNeedDefName.";
            }

            if (string.IsNullOrWhiteSpace(bladderNeedDefName))
            {
                yield return defName + " must define bladderNeedDefName.";
            }

            if (diagnosticIntervalTicks <= 0)
            {
                yield return defName + " must define diagnosticIntervalTicks > 0.";
            }

            if (powerDemandWatts < 0f)
            {
                yield return defName + " has negative powerDemandWatts.";
            }

            if (serviceIntervalTicks <= 0)
            {
                yield return defName + " must define serviceIntervalTicks > 0.";
            }

            if (maxPawnsPerService <= 0)
            {
                yield return defName + " must define maxPawnsPerService > 0.";
            }

            if (!IsPercent(hygieneServiceThreshold))
            {
                yield return defName + " hygieneServiceThreshold must be between 0 and 1.";
            }

            if (!IsPercent(hygieneServiceGain))
            {
                yield return defName + " hygieneServiceGain must be between 0 and 1.";
            }

            if (!IsPercent(hygieneServiceTarget))
            {
                yield return defName + " hygieneServiceTarget must be between 0 and 1.";
            }

            if (hygieneServiceTarget <= hygieneServiceThreshold)
            {
                yield return defName + " hygieneServiceTarget must be greater than hygieneServiceThreshold.";
            }

            if (!IsPercent(bladderServiceThreshold))
            {
                yield return defName + " bladderServiceThreshold must be between 0 and 1.";
            }

            if (!IsPercent(bladderServiceTarget))
            {
                yield return defName + " bladderServiceTarget must be between 0 and 1.";
            }

            if (bladderServiceTarget <= bladderServiceThreshold)
            {
                yield return defName + " bladderServiceTarget must be greater than bladderServiceThreshold.";
            }

            if (bladderDumpCallsPerService <= 0)
            {
                yield return defName + " bladderDumpCallsPerService must be > 0.";
            }

            if (thirstServiceEnabled)
            {
                yield return defName + " thirstServiceEnabled is not supported in Bad Hygiene Lite Service v1.";
            }
        }

        private static IEnumerable<string> ValidatePackageIds(List<string> values, string fieldName)
        {
            for (int i = 0; i < values.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(values[i]))
                {
                    yield return "contains an empty " + fieldName + " entry at index " + i + ".";
                }
            }
        }

        private static void EnsureList(ref List<string> values)
        {
            if (values == null)
            {
                values = new List<string>();
            }
        }

        private static bool LooksLikeFullKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            string trimmed = key.Trim();
            int slashIndex = trimmed.IndexOf('/');
            return slashIndex > 0 && slashIndex < trimmed.Length - 1;
        }

        private static bool IsPercent(float value)
        {
            return value >= 0f && value <= 1f;
        }
    }

    public sealed class BHLiteHygieneModuleExtension : DefModExtension
    {
        public string integrationDefName = "CT_Shuttle_BHLiteIntegration";
    }
}
