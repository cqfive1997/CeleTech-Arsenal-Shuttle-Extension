using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Defs
{
    /// <summary>
    /// Static capability definition for Habitat and Recreation modules.
    /// Runtime pawns, dining food, and holder-transfer state live in occupancy comps,
    /// not in this def.
    /// </summary>
    public sealed class ShuttleHabitatModuleDef : ShuttleModuleBaseDef
    {
        // Living Habitat capabilities.
        public int habitatSleepSlots;
        public int habitatDiningSlots;
        public bool habitatSupportsSleep = true;
        public bool habitatSupportsDining = true;
        public int habitatSleepThoughtStageIndex;
        public int habitatDiningThoughtStageIndex;
        public float habitatRestEffectiveness = 1f;
        public bool suppressSleepDisturbedThoughts = true;
        public bool suppressBarracksThoughts = true;
        public bool allowInventoryFood = true;
        public bool allowCargoFoodWithdrawal = true;
        public bool requireCargoLogisticsForFoodWithdrawal = true;
        public List<ThingDef> preferredAutoFoodDefs = new List<ThingDef>();

        // Recreation Room capabilities. Joy records share the Habitat holder but
        // remain distinct from sleep/dining records.
        public bool habitatSupportsJoy;
        public int habitatJoySlots;
        public int habitatJoyKindCapacity = 5;
        public List<JoyKindDef> allowedJoyKinds = new List<JoyKindDef>();
        public List<JoyKindDef> defaultJoyKinds = new List<JoyKindDef>();
        public int habitatJoyThoughtStageIndex;
        public float habitatJoyGainFactor = 1f;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }

            if (this.habitatSleepSlots < 0)
            {
                yield return this.defName + " has negative habitatSleepSlots.";
            }

            if (this.habitatDiningSlots < 0)
            {
                yield return this.defName + " has negative habitatDiningSlots.";
            }

            if (!this.IsFinitePositive(this.habitatRestEffectiveness))
            {
                yield return this.defName + " must define finite positive habitatRestEffectiveness.";
            }

            foreach (string error in this.ValidateSleepSettings())
            {
                yield return error;
            }

            foreach (string error in this.ValidateDiningSettings())
            {
                yield return error;
            }

            foreach (string error in this.ValidatePreferredFoodDefs())
            {
                yield return error;
            }

            foreach (string error in this.ValidateJoySettings())
            {
                yield return error;
            }
        }

        private IEnumerable<string> ValidateSleepSettings()
        {
            if (this.habitatSupportsSleep)
            {
                if (this.habitatSleepSlots == 0)
                {
                    yield return "Warning: " + this.defName +
                        " supports habitat sleep but habitatSleepSlots is 0.";
                }

                foreach (string error in this.ValidateThoughtStageIndex(
                    "habitatSleepThoughtStageIndex",
                    this.habitatSleepThoughtStageIndex,
                    ThoughtDefOf.SleptInBedroom,
                    "SleptInBedroom"))
                {
                    yield return error;
                }
            }
            else if (this.habitatSleepSlots > 0)
            {
                yield return "Warning: " + this.defName +
                    " has habitatSleepSlots but habitatSupportsSleep is false.";
            }
        }

        private IEnumerable<string> ValidateDiningSettings()
        {
            if (this.habitatSupportsDining)
            {
                if (this.habitatDiningSlots == 0)
                {
                    yield return "Warning: " + this.defName +
                        " supports habitat dining but habitatDiningSlots is 0.";
                }

                foreach (string error in this.ValidateThoughtStageIndex(
                    "habitatDiningThoughtStageIndex",
                    this.habitatDiningThoughtStageIndex,
                    ThoughtDefOf.AteInImpressiveDiningRoom,
                    "AteInImpressiveDiningRoom"))
                {
                    yield return error;
                }
            }
            else if (this.habitatDiningSlots > 0)
            {
                yield return "Warning: " + this.defName +
                    " has habitatDiningSlots but habitatSupportsDining is false.";
            }

            if (this.habitatSupportsDining &&
                !this.allowInventoryFood &&
                !this.allowCargoFoodWithdrawal)
            {
                yield return "Warning: " + this.defName +
                    " supports habitat dining but both inventory food and cargo food withdrawal are disabled.";
            }
        }

        private IEnumerable<string> ValidateThoughtStageIndex(
            string fieldName,
            int stageIndex,
            ThoughtDef thoughtDef,
            string thoughtDefName)
        {
            if (stageIndex < 0)
            {
                yield return this.defName + " has negative " + fieldName + ".";
                yield break;
            }

            if (thoughtDef == null)
            {
                yield return this.defName + " could not resolve thought def " + thoughtDefName +
                    " for " + fieldName + ".";
                yield break;
            }

            if (thoughtDef.stages == null || thoughtDef.stages.Count == 0)
            {
                yield return this.defName + " thought def " + thoughtDefName +
                    " has no stages for " + fieldName + ".";
                yield break;
            }

            if (stageIndex >= thoughtDef.stages.Count)
            {
                yield return this.defName + " " + fieldName + " " + stageIndex +
                    " is outside " + thoughtDefName + " stage count " + thoughtDef.stages.Count + ".";
            }
        }

        private IEnumerable<string> ValidatePreferredFoodDefs()
        {
            if (this.preferredAutoFoodDefs == null || this.preferredAutoFoodDefs.Count == 0)
            {
                yield break;
            }

            if (!this.allowCargoFoodWithdrawal)
            {
                yield return "Warning: " + this.defName +
                    " defines preferredAutoFoodDefs but allowCargoFoodWithdrawal is false.";
            }

            for (int i = 0; i < this.preferredAutoFoodDefs.Count; i++)
            {
                ThingDef foodDef = this.preferredAutoFoodDefs[i];
                if (foodDef == null)
                {
                    yield return this.defName + " has null preferredAutoFoodDefs entry at index " + i + ".";
                    continue;
                }

                if (foodDef.ingestible == null)
                {
                    yield return this.defName + " preferredAutoFoodDefs entry " +
                        foodDef.defName + " is not ingestible.";
                    continue;
                }

                if (!foodDef.IsNutritionGivingIngestible)
                {
                    yield return this.defName + " preferredAutoFoodDefs entry " +
                        foodDef.defName + " is not a nutrition-giving ingestible.";
                }
            }
        }

        private IEnumerable<string> ValidateJoySettings()
        {
            if (this.habitatJoySlots < 0)
            {
                yield return this.defName + " has negative habitatJoySlots.";
            }

            if (this.habitatJoyKindCapacity < 0)
            {
                yield return this.defName + " has negative habitatJoyKindCapacity.";
            }

            if (!this.IsFinitePositive(this.habitatJoyGainFactor))
            {
                yield return this.defName + " must define finite positive habitatJoyGainFactor.";
            }

            if (this.habitatJoyThoughtStageIndex < 0)
            {
                yield return this.defName + " has negative habitatJoyThoughtStageIndex.";
            }

            if (this.allowedJoyKinds == null)
            {
                this.allowedJoyKinds = new List<JoyKindDef>();
            }

            if (this.defaultJoyKinds == null)
            {
                this.defaultJoyKinds = new List<JoyKindDef>();
            }

            for (int i = 0; i < this.allowedJoyKinds.Count; i++)
            {
                if (this.allowedJoyKinds[i] == null)
                {
                    yield return this.defName + " has null allowedJoyKinds entry at index " + i + ".";
                }
            }

            for (int i = 0; i < this.defaultJoyKinds.Count; i++)
            {
                JoyKindDef joyKind = this.defaultJoyKinds[i];
                if (joyKind == null)
                {
                    yield return this.defName + " has null defaultJoyKinds entry at index " + i + ".";
                    continue;
                }

                if (!this.allowedJoyKinds.Contains(joyKind))
                {
                    yield return this.defName + " defaultJoyKinds entry " + joyKind.defName +
                        " is not present in allowedJoyKinds.";
                }
            }

            if (this.defaultJoyKinds.Count > this.habitatJoyKindCapacity)
            {
                yield return this.defName + " defaultJoyKinds count " + this.defaultJoyKinds.Count +
                    " exceeds habitatJoyKindCapacity " + this.habitatJoyKindCapacity + ".";
            }
        }

        private bool IsFinitePositive(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;
        }
    }
}
