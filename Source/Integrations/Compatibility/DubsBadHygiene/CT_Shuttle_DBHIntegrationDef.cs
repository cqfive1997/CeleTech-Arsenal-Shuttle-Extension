using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.AdditionalModule.Compatibility.DubsBadHygiene
{
    public sealed class CT_Shuttle_DBHIntegrationDef : Def
    {
        public List<string> packageIds = new List<string>();
        public string runtimeSystemKey;
        public string panelSystemKey;
        public string runtimeLabelKey;
        public string runtimeDescriptionKey;
        public string hygieneNeedDefName;
        public string bladderNeedDefName;
        public string thirstNeedDefName;
        public string sewageThingDefName;
        public string sewageFilthDefName;
        public int serviceIntervalTicks = 600;
        public int pipeCheckIntervalTicks = 600;
        public float cleanWaterCapacity;
        public float sewageCapacity;
        public float defaultTankCapacityLiters = 1000f;
        public float maxTankCapacityLiters = 5000f;
        public float minTankCapacityLiters = 250f;
        public float tankCapacityStepLiters = 250f;
        public float waterMassKgPerLiter = 1f;
        public float sewageMassKgPerLiter = 1f;
        public bool linkSewageCapacityToWaterCapacity = true;
        public float pipeFillRatePerService;
        public float pipeSewageDrainRatePerService;
        public bool allowSepticTreatment = true;
        public float septicTreatmentRatePerDay = 1500f;
        public float maxSepticTreatmentRatePerDay = 10000f;
        public float septicCleanWaterRecoveryRatio = 0.6f;
        public float showerHygieneThreshold = 0.70f;
        public float showerHygieneGain = 0.08f;
        public float showerWaterCost;
        public float showerSewageOutput;
        public float bladderServiceThreshold = 0.35f;
        public float bladderServiceTarget = 0.70f;
        public float toiletWaterCost;
        public float toiletSewageOutput;
        public bool allowPipeRefill;
        public bool allowPipeSewageDrain;
        public bool allowMapDump;
        public bool allowWorldDump;
        public bool allowCargoWaterRefill;
        public float cargoWaterRefillRatePerService = 20f;
        public float cargoWaterUnitValue = 1f;
        public List<string> cargoWaterThingDefNames = new List<string>();
        public int pipeSearchRadius = 1;
        public bool requireAdjacentPipe = true;
        public List<string> waterPipeThingDefNames = new List<string>();
        public List<string> sewagePipeThingDefNames = new List<string>();
        public List<string> waterNetworkCompTypeNames = new List<string>();
        public List<string> sewageNetworkCompTypeNames = new List<string>();

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }

            if (packageIds == null)
            {
                packageIds = new List<string>();
            }

            if (packageIds.Count == 0)
            {
                yield return defName + " must define at least one packageIds entry.";
            }

            for (int i = 0; i < packageIds.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(packageIds[i]))
                {
                    yield return defName + " contains an empty packageIds entry at index " + i + ".";
                }
            }

            if (string.IsNullOrWhiteSpace(runtimeSystemKey))
            {
                yield return defName + " must define runtimeSystemKey.";
            }
            else if (!LooksLikeFullKey(runtimeSystemKey))
            {
                yield return defName + " runtimeSystemKey must use owner.package/local-key format.";
            }

            if (string.IsNullOrWhiteSpace(panelSystemKey))
            {
                yield return defName + " must define panelSystemKey.";
            }
            else if (!LooksLikeFullKey(panelSystemKey))
            {
                yield return defName + " panelSystemKey must use owner.package/local-key format.";
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

            if (serviceIntervalTicks <= 0)
            {
                yield return defName + " must define serviceIntervalTicks > 0.";
            }

            if (pipeCheckIntervalTicks <= 0)
            {
                yield return defName + " must define pipeCheckIntervalTicks > 0.";
            }

            if (defaultTankCapacityLiters <= 0f)
            {
                yield return defName + " must define defaultTankCapacityLiters > 0.";
            }

            if (maxTankCapacityLiters <= 0f)
            {
                yield return defName + " must define maxTankCapacityLiters > 0.";
            }

            if (minTankCapacityLiters <= 0f)
            {
                yield return defName + " must define minTankCapacityLiters > 0.";
            }

            if (minTankCapacityLiters > maxTankCapacityLiters)
            {
                yield return defName + " minTankCapacityLiters must be <= maxTankCapacityLiters.";
            }

            if (defaultTankCapacityLiters < minTankCapacityLiters ||
                defaultTankCapacityLiters > maxTankCapacityLiters)
            {
                yield return defName + " defaultTankCapacityLiters must be between min and max tank capacity.";
            }

            if (tankCapacityStepLiters <= 0f)
            {
                yield return defName + " must define tankCapacityStepLiters > 0.";
            }

            if (waterMassKgPerLiter <= 0f)
            {
                yield return defName + " must define waterMassKgPerLiter > 0.";
            }

            if (sewageMassKgPerLiter <= 0f)
            {
                yield return defName + " must define sewageMassKgPerLiter > 0.";
            }

            if (pipeFillRatePerService < 0f)
            {
                yield return defName + " has negative pipeFillRatePerService.";
            }

            if (pipeSewageDrainRatePerService < 0f)
            {
                yield return defName + " has negative pipeSewageDrainRatePerService.";
            }

            if (septicTreatmentRatePerDay < 0f)
            {
                yield return defName + " has negative septicTreatmentRatePerDay.";
            }

            if (maxSepticTreatmentRatePerDay < 0f)
            {
                yield return defName + " has negative maxSepticTreatmentRatePerDay.";
            }

            if (septicTreatmentRatePerDay > maxSepticTreatmentRatePerDay)
            {
                yield return defName + " septicTreatmentRatePerDay must be <= maxSepticTreatmentRatePerDay.";
            }

            if (septicCleanWaterRecoveryRatio < 0f || septicCleanWaterRecoveryRatio > 1f)
            {
                yield return defName + " septicCleanWaterRecoveryRatio must be between 0 and 1.";
            }

            if (showerHygieneThreshold < 0f || showerHygieneThreshold > 1f)
            {
                yield return defName + " showerHygieneThreshold must be between 0 and 1.";
            }

            if (showerHygieneGain < 0f)
            {
                yield return defName + " has negative showerHygieneGain.";
            }

            if (showerWaterCost < 0f)
            {
                yield return defName + " has negative showerWaterCost.";
            }

            if (showerSewageOutput < 0f)
            {
                yield return defName + " has negative showerSewageOutput.";
            }

            if (bladderServiceThreshold < 0f || bladderServiceThreshold > 1f)
            {
                yield return defName + " bladderServiceThreshold must be between 0 and 1.";
            }

            if (bladderServiceTarget < 0f || bladderServiceTarget > 1f)
            {
                yield return defName + " bladderServiceTarget must be between 0 and 1.";
            }

            if (bladderServiceTarget <= bladderServiceThreshold)
            {
                yield return defName + " bladderServiceTarget must be greater than bladderServiceThreshold.";
            }

            if (toiletWaterCost < 0f)
            {
                yield return defName + " has negative toiletWaterCost.";
            }

            if (toiletSewageOutput < 0f)
            {
                yield return defName + " has negative toiletSewageOutput.";
            }

            if (pipeSearchRadius < 0)
            {
                yield return defName + " has negative pipeSearchRadius.";
            }

            if (cargoWaterRefillRatePerService < 0f)
            {
                yield return defName + " has negative cargoWaterRefillRatePerService.";
            }

            if (allowCargoWaterRefill && cargoWaterUnitValue <= 0f)
            {
                yield return defName + " must define cargoWaterUnitValue > 0 when cargo water refill is enabled.";
            }

            if (cargoWaterThingDefNames == null)
            {
                cargoWaterThingDefNames = new List<string>();
            }

            for (int i = 0; i < cargoWaterThingDefNames.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(cargoWaterThingDefNames[i]))
                {
                    yield return defName + " contains an empty cargoWaterThingDefNames entry at index " + i + ".";
                }
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
    }

    public sealed class DBHHygieneSuiteModuleExtension : DefModExtension
    {
        public string integrationDefName = "CT_Shuttle_DBHIntegration";
    }
}
