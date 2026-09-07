using System;
using System.Collections.Generic;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.AssemblyState
{
    /// <summary>
    /// Durable player configuration for visual cargo regions.
    /// It is static shuttle configuration, not runtime cargo truth; the real cargo items remain
    /// only in CompTransporter.innerContainer.
    /// </summary>
    public sealed class ShuttleCargoRegionConfigState : IExposable
    {
        private List<ShuttleCargoRegionSettings> regions = new List<ShuttleCargoRegionSettings>();

        public IReadOnlyList<ShuttleCargoRegionSettings> Regions
        {
            get
            {
                return this.regions;
            }
        }

        public void EnsureRegionCount(int regionCount)
        {
            if (regionCount < 0)
            {
                regionCount = 0;
            }

            if (this.regions == null)
            {
                this.regions = new List<ShuttleCargoRegionSettings>();
            }

            for (int i = 0; i < this.regions.Count; i++)
            {
                if (this.regions[i] != null)
                {
                    this.regions[i].EnsureInitialized(i);
                }
            }

            while (this.regions.Count < regionCount)
            {
                ShuttleCargoRegionSettings settings = new ShuttleCargoRegionSettings();
                settings.EnsureInitialized(this.regions.Count);
                this.regions.Add(settings);
            }
        }

        public ShuttleCargoRegionSettings GetRegion(int regionIndex)
        {
            this.EnsureRegionCount(regionIndex + 1);
            if (regionIndex < 0 || regionIndex >= this.regions.Count)
            {
                return null;
            }

            return this.regions[regionIndex];
        }

        public bool TryGetRegion(int regionIndex, out ShuttleCargoRegionSettings settings)
        {
            settings = this.GetRegionOrNull(regionIndex);
            return settings != null;
        }

        public ShuttleCargoRegionSettings GetRegionOrNull(int regionIndex)
        {
            if (regionIndex < 0 || this.regions == null || regionIndex >= this.regions.Count)
            {
                return null;
            }

            return this.regions[regionIndex];
        }

        public ShuttleCargoRegionSettings GetRegionForRead(int regionIndex)
        {
            ShuttleCargoRegionSettings existing = this.GetRegionOrNull(regionIndex);
            if (existing != null)
            {
                return existing;
            }

            ShuttleCargoRegionSettings fallback = new ShuttleCargoRegionSettings();
            fallback.EnsureInitialized(regionIndex);
            return fallback;
        }

        public bool AllowsAnyActiveRegion(Thing thing, int activeRegionCount)
        {
            return this.FindFirstAllowedRegion(thing, activeRegionCount) >= 0;
        }

        public int FindStableAllowedRegion(Thing thing, int activeRegionCount)
        {
            if (thing == null || activeRegionCount <= 0)
            {
                return -1;
            }

            int allowedCount = 0;
            for (int i = 0; i < activeRegionCount; i++)
            {
                if (this.RegionAllowsForRead(i, thing))
                {
                    allowedCount++;
                }
            }

            if (allowedCount <= 0)
            {
                return -1;
            }

            // The current transporter backend has one real container, so display assignment is
            // deterministic within the regions that allow the thing instead of always choosing bay 1.
            int selectedAllowedIndex = this.PositiveModulo(this.GetStableThingSeed(thing), allowedCount);
            int currentAllowedIndex = 0;
            for (int i = 0; i < activeRegionCount; i++)
            {
                if (!this.RegionAllowsForRead(i, thing))
                {
                    continue;
                }

                if (currentAllowedIndex == selectedAllowedIndex)
                {
                    return i;
                }

                currentAllowedIndex++;
            }

            return -1;
        }

        public int FindFirstAllowedRegion(Thing thing, int activeRegionCount)
        {
            if (thing == null || activeRegionCount <= 0)
            {
                return -1;
            }

            for (int i = 0; i < activeRegionCount; i++)
            {
                if (this.RegionAllowsForRead(i, thing))
                {
                    return i;
                }
            }

            return -1;
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(ref this.regions, "regions", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.EnsureRegionCount(this.regions != null ? this.regions.Count : 0);
            }
        }

        private bool RegionAllowsForRead(int regionIndex, Thing thing)
        {
            ShuttleCargoRegionSettings settings = this.GetRegionOrNull(regionIndex);
            if (settings != null)
            {
                return settings.AllowsForRead(thing);
            }

            return this.DefaultRegionAllows(thing);
        }

        private bool DefaultRegionAllows(Thing thing)
        {
            if (thing == null)
            {
                return false;
            }

            Pawn pawn = thing as Pawn;
            if (pawn != null)
            {
                return true;
            }

            ThingFilter defaultFilter = ThingFilter.CreateOnlyEverStorableThingFilter();
            return defaultFilter != null && defaultFilter.Allows(thing);
        }

        private int GetStableThingSeed(Thing thing)
        {
            if (thing == null)
            {
                return 0;
            }

            unchecked
            {
                int seed = thing.thingIDNumber;
                if (thing.def != null)
                {
                    seed = (seed * 397) ^ this.StableStringHash(thing.def.defName);
                }

                return seed;
            }
        }

        private int StableStringHash(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return 0;
            }

            unchecked
            {
                int hash = 23;
                for (int i = 0; i < value.Length; i++)
                {
                    hash = (hash * 31) + value[i];
                }

                return hash;
            }
        }

        private int PositiveModulo(int value, int divisor)
        {
            if (divisor <= 0)
            {
                return 0;
            }

            int result = value % divisor;
            return result < 0 ? result + divisor : result;
        }
    }

    public sealed class ShuttleCargoRegionSettings : IExposable
    {
        private int regionIndex;
        private string label;
        private bool hasCustomLabel;
        private bool labelClassificationInitialized;
        private bool allowHumans = true;
        private bool allowAnimals = true;
        private bool allowMechs = true;
        private bool itemFilterUsesDefaultAllowAll = true;
        private bool itemFilterIntentInitialized = true;
        private ThingFilter filter;

        public int RegionIndex
        {
            get
            {
                return this.regionIndex;
            }
        }

        public string Label
        {
            get
            {
                return this.label;
            }
            set
            {
                this.SetLabel(value);
            }
        }

        public bool HasCustomLabel
        {
            get
            {
                return this.hasCustomLabel;
            }
        }

        public bool AllowPawns
        {
            get
            {
                return this.allowHumans || this.allowAnimals || this.allowMechs;
            }
            set
            {
                this.allowHumans = value;
                this.allowAnimals = value;
                this.allowMechs = value;
            }
        }

        public bool AllowHumans
        {
            get
            {
                return this.allowHumans;
            }
            set
            {
                this.allowHumans = value;
            }
        }

        public bool AllowAnimals
        {
            get
            {
                return this.allowAnimals;
            }
            set
            {
                this.allowAnimals = value;
            }
        }

        public bool AllowMechs
        {
            get
            {
                return this.allowMechs;
            }
            set
            {
                this.allowMechs = value;
            }
        }

        public ThingFilter Filter
        {
            get
            {
                this.EnsureFilter();
                return this.filter;
            }
        }

        public ThingFilter FilterForRead
        {
            get
            {
                return this.filter;
            }
        }

        public bool ItemFilterUsesDefaultAllowAll
        {
            get
            {
                return this.itemFilterUsesDefaultAllowAll;
            }
        }

        public void EnsureInitialized(int fallbackIndex)
        {
            this.regionIndex = fallbackIndex;
            if (!this.labelClassificationInitialized)
            {
                this.hasCustomLabel = !string.IsNullOrEmpty(this.label) &&
                    !IsGeneratedDefaultLabel(this.label, fallbackIndex);
                this.labelClassificationInitialized = true;
            }

            this.label = this.hasCustomLabel
                ? NormalizeLabel(this.label)
                : null;
            if (string.IsNullOrEmpty(this.label))
            {
                this.hasCustomLabel = false;
            }

            this.EnsureFilter();
        }

        public bool Allows(Thing thing)
        {
            if (thing == null)
            {
                return false;
            }

            Pawn pawn = thing as Pawn;
            if (pawn != null)
            {
                return this.AllowsPawn(pawn);
            }

            this.EnsureFilter();
            return this.filter != null && this.filter.Allows(thing);
        }

        public bool AllowsForRead(Thing thing)
        {
            if (thing == null)
            {
                return false;
            }

            Pawn pawn = thing as Pawn;
            if (pawn != null)
            {
                return this.AllowsPawn(pawn);
            }

            if (this.filter != null)
            {
                return this.filter.Allows(thing);
            }

            ThingFilter defaultFilter = ThingFilter.CreateOnlyEverStorableThingFilter();
            return defaultFilter != null && defaultFilter.Allows(thing);
        }

        public void AllowAllItems()
        {
            this.filter = ThingFilter.CreateOnlyEverStorableThingFilter();
            this.itemFilterUsesDefaultAllowAll = true;
            this.itemFilterIntentInitialized = true;
        }

        public void DisallowAllItems()
        {
            this.EnsureFilter();
            this.filter.SetDisallowAll(null, null);
            this.itemFilterUsesDefaultAllowAll = false;
            this.itemFilterIntentInitialized = true;
        }

        public void SetItemFilter(ThingFilter source)
        {
            ThingFilter replacement = ThingFilter.CreateOnlyEverStorableThingFilter();
            if (source != null)
            {
                replacement.CopyAllowancesFrom(source);
            }

            this.itemFilterUsesDefaultAllowAll =
                ShuttleCargoThingFilterIntentPolicy.IsCurrentDefaultAllowAll(replacement);
            this.itemFilterIntentInitialized = true;
            this.filter = this.itemFilterUsesDefaultAllowAll
                ? ThingFilter.CreateOnlyEverStorableThingFilter()
                : replacement;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.regionIndex, "regionIndex", 0);
            Scribe_Values.Look(ref this.label, "label");
            Scribe_Values.Look(ref this.hasCustomLabel, "hasCustomLabel", false);
            Scribe_Values.Look(
                ref this.labelClassificationInitialized,
                "labelClassificationInitialized",
                false);
            Scribe_Values.Look(ref this.allowHumans, "allowHumans", true);
            Scribe_Values.Look(ref this.allowAnimals, "allowAnimals", true);
            Scribe_Values.Look(ref this.allowMechs, "allowMechs", true);
            Scribe_Values.Look(
                ref this.itemFilterUsesDefaultAllowAll,
                "itemFilterUsesDefaultAllowAll",
                true);
            Scribe_Values.Look(
                ref this.itemFilterIntentInitialized,
                "itemFilterIntentInitialized",
                false);
            Scribe_Deep.Look(ref this.filter, "filter");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.InitializeItemFilterIntentAfterLoad();
                this.EnsureInitialized(this.regionIndex);
            }
        }

        private void InitializeItemFilterIntentAfterLoad()
        {
            if (!this.itemFilterIntentInitialized)
            {
                this.itemFilterUsesDefaultAllowAll =
                    ShuttleCargoThingFilterIntentPolicy.IsLegacyDefaultAllowAll(this.filter);
                this.itemFilterIntentInitialized = true;
            }

            if (this.itemFilterUsesDefaultAllowAll)
            {
                this.filter = ThingFilter.CreateOnlyEverStorableThingFilter();
            }
        }

        private void EnsureFilter()
        {
            if (this.filter == null)
            {
                this.filter = ThingFilter.CreateOnlyEverStorableThingFilter();
                this.itemFilterUsesDefaultAllowAll = true;
                this.itemFilterIntentInitialized = true;
            }
        }

        private void SetLabel(string value)
        {
            string normalized = NormalizeLabel(value);
            this.labelClassificationInitialized = true;
            this.hasCustomLabel = !string.IsNullOrEmpty(normalized) &&
                !IsGeneratedDefaultLabel(normalized, this.regionIndex);
            this.label = this.hasCustomLabel ? normalized : null;
        }

        private static string NormalizeLabel(string value)
        {
            string trimmed = value != null ? value.Trim() : null;
            return string.IsNullOrEmpty(trimmed) ? null : trimmed;
        }

        private static bool IsGeneratedDefaultLabel(string value, int regionIndex)
        {
            string normalized = NormalizeLabel(value);
            if (string.IsNullOrEmpty(normalized))
            {
                return true;
            }

            int displayIndex = regionIndex + 1;
            string current = "CT_Shuttle_UI_CargoBayLabel".Translate(displayIndex).ToString();
            return string.Equals(normalized, current, StringComparison.Ordinal) ||
                string.Equals(normalized, "Cargo Bay " + displayIndex, StringComparison.Ordinal) ||
                string.Equals(normalized, "货舱 " + displayIndex, StringComparison.Ordinal);
        }

        private bool AllowsPawn(Pawn pawn)
        {
            return pawn != null;
        }
    }
}
