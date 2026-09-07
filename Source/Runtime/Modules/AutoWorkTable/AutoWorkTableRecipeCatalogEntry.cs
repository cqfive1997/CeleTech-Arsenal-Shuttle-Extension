using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.AutoWorkTable
{
    internal sealed class AutoWorkTableRecipeCatalogEntry
    {
        public AutoWorkTableRecipeCatalogEntry(ThingDef sourceBenchDef, RecipeDef recipeDef)
        {
            this.SourceBenchDef = sourceBenchDef;
            this.RecipeDef = recipeDef;
            this.SourceBenchDefName = sourceBenchDef != null ? sourceBenchDef.defName : null;
            this.RecipeDefName = recipeDef != null ? recipeDef.defName : null;
        }

        public ThingDef SourceBenchDef { get; private set; }
        public RecipeDef RecipeDef { get; private set; }
        public string SourceBenchDefName { get; private set; }
        public string RecipeDefName { get; private set; }
    }
}
