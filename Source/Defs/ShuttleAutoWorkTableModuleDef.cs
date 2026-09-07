using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Defs
{
    /// <summary>
    /// Static configuration for a selected-recipe-repeat shuttle auto worktable module.
    /// Source benches are recipe-user labels; they are never spawned by the runtime system.
    /// </summary>
    public sealed class ShuttleAutoWorkTableModuleDef : ShuttleModuleBaseDef
    {
        public List<ThingDef> sourceBenchDefs = new List<ThingDef>();
        public List<WorkGiverDef> sourceWorkGiverDefs = new List<WorkGiverDef>();
        public List<RecipeDef> allowedRecipes = new List<RecipeDef>();
        public List<RecipeDef> blockedRecipes = new List<RecipeDef>();

        public ThingDef defaultSelectedSourceBenchDef;
        public RecipeDef defaultSelectedRecipeDef;

        public bool allowAutoPickFirstCraftableRecipe;
        public bool requiresCargoLogistics = true;
        public bool requirePoweredInternalBus = true;
        public bool allowSurgeryRecipes;
        public bool allowMechanitorOnlyRecipes;
        public bool allowGestationRecipes;
        public bool allowMechResurrectionRecipes;
        public bool allowFormingTickRecipes;
        public bool allowUnfinishedThingRecipes;
        public bool allowSpecialProductRecipes;
        public bool allowCustomRecipeWorkers;
        public bool allowQualityProducts;
        public bool allowNonItemProducts;
        public bool allowPawnProducts;
        public bool allowBuildingProducts;
        public bool allowStuffProducts;

        public float workAmountFactor = 1f;
        public float workSpeedFactor = 1f;
        public float activePowerDrawWatts;
        public int workTickIntervalTicks = 60;
        public int recipeCheckIntervalTicks = 250;
        public int depositRetryIntervalTicks = 250;
        public int maxCatchUpTicks = 2500;

        public List<AutoWorkTableVirtualSkillLevel> virtualSkillLevels =
            new List<AutoWorkTableVirtualSkillLevel>();

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }

            this.EnsureLists();

            if (this.sourceBenchDefs.Count == 0 && this.sourceWorkGiverDefs.Count == 0)
            {
                yield return this.defName + " must define sourceBenchDefs or sourceWorkGiverDefs.";
            }

            if (!this.IsFiniteFloat(this.workAmountFactor) || this.workAmountFactor <= 0f)
            {
                yield return this.defName + " must define positive finite workAmountFactor.";
            }

            if (!this.IsFiniteFloat(this.workSpeedFactor) || this.workSpeedFactor <= 0f)
            {
                yield return this.defName + " must define positive finite workSpeedFactor.";
            }

            if (!this.IsFiniteFloat(this.activePowerDrawWatts) || this.activePowerDrawWatts < 0f)
            {
                yield return this.defName + " must define non-negative finite activePowerDrawWatts.";
            }

            if (this.workTickIntervalTicks <= 0)
            {
                yield return this.defName + " must define positive workTickIntervalTicks.";
            }

            if (this.recipeCheckIntervalTicks <= 0)
            {
                yield return this.defName + " must define positive recipeCheckIntervalTicks.";
            }

            if (this.depositRetryIntervalTicks <= 0)
            {
                yield return this.defName + " must define positive depositRetryIntervalTicks.";
            }

            if (this.maxCatchUpTicks <= 0)
            {
                yield return this.defName + " must define positive maxCatchUpTicks.";
            }

            if (this.defaultSelectedRecipeDef != null && this.defaultSelectedSourceBenchDef == null)
            {
                yield return this.defName + " defines defaultSelectedRecipeDef without defaultSelectedSourceBenchDef.";
            }

            if (this.defaultSelectedSourceBenchDef != null &&
                !this.IsConfiguredSourceBench(this.defaultSelectedSourceBenchDef))
            {
                yield return this.defName + " default selected source bench " +
                    this.defaultSelectedSourceBenchDef.defName + " is not listed in sourceBenchDefs or sourceWorkGiverDefs fixedBillGiverDefs.";
            }

            if (this.defaultSelectedRecipeDef != null &&
                this.defaultSelectedSourceBenchDef != null &&
                !this.IsRecipeDirectlyRegisteredForBench(
                    this.defaultSelectedSourceBenchDef,
                    this.defaultSelectedRecipeDef))
            {
                yield return this.defName + " default selected recipe " +
                    this.defaultSelectedRecipeDef.defName + " is not directly registered for source bench " +
                    this.defaultSelectedSourceBenchDef.defName +
                    " through ThingDef.recipes or RecipeDef.recipeUsers.";
            }

            for (int i = 0; i < this.virtualSkillLevels.Count; i++)
            {
                AutoWorkTableVirtualSkillLevel skillLevel = this.virtualSkillLevels[i];
                if (skillLevel == null || skillLevel.skill == null)
                {
                    yield return this.defName + " has a null virtual skill level entry.";
                    continue;
                }

                if (skillLevel.level < 0 || skillLevel.level > 20)
                {
                    yield return this.defName + " virtual skill " + skillLevel.skill.defName +
                        " has level outside 0..20.";
                }
            }
        }

        public int GetVirtualSkillLevel(SkillDef skill)
        {
            if (skill == null || this.virtualSkillLevels == null)
            {
                return 0;
            }

            for (int i = 0; i < this.virtualSkillLevels.Count; i++)
            {
                AutoWorkTableVirtualSkillLevel skillLevel = this.virtualSkillLevels[i];
                if (skillLevel != null && skillLevel.skill == skill)
                {
                    return skillLevel.level;
                }
            }

            return 0;
        }

        private bool IsConfiguredSourceBench(ThingDef sourceBenchDef)
        {
            if (sourceBenchDef == null)
            {
                return false;
            }

            if (this.sourceBenchDefs != null && this.sourceBenchDefs.Contains(sourceBenchDef))
            {
                return true;
            }

            if (this.sourceWorkGiverDefs == null)
            {
                return false;
            }

            for (int i = 0; i < this.sourceWorkGiverDefs.Count; i++)
            {
                WorkGiverDef workGiverDef = this.sourceWorkGiverDefs[i];
                if (workGiverDef != null &&
                    workGiverDef.fixedBillGiverDefs != null &&
                    workGiverDef.fixedBillGiverDefs.Contains(sourceBenchDef))
                {
                    return true;
                }
            }

            return false;
        }

        private void EnsureLists()
        {
            if (this.sourceBenchDefs == null)
            {
                this.sourceBenchDefs = new List<ThingDef>();
            }

            if (this.sourceWorkGiverDefs == null)
            {
                this.sourceWorkGiverDefs = new List<WorkGiverDef>();
            }

            if (this.allowedRecipes == null)
            {
                this.allowedRecipes = new List<RecipeDef>();
            }

            if (this.blockedRecipes == null)
            {
                this.blockedRecipes = new List<RecipeDef>();
            }

            if (this.virtualSkillLevels == null)
            {
                this.virtualSkillLevels = new List<AutoWorkTableVirtualSkillLevel>();
            }
        }

        private bool IsRecipeDirectlyRegisteredForBench(ThingDef sourceBenchDef, RecipeDef recipeDef)
        {
            if (sourceBenchDef == null || recipeDef == null)
            {
                return false;
            }

            if (sourceBenchDef.recipes != null && sourceBenchDef.recipes.Contains(recipeDef))
            {
                return true;
            }

            return recipeDef.recipeUsers != null && recipeDef.recipeUsers.Contains(sourceBenchDef);
        }
    }

    public sealed class AutoWorkTableVirtualSkillLevel
    {
        public SkillDef skill;
        public int level;
    }
}
