using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Comps
{
    // Transient living restore plan only. Not Scribed.
    internal sealed class HabitatLivingRestorePlan
    {
        internal readonly List<HabitatSleepRestorePlanEntry> SleepEntries =
            new List<HabitatSleepRestorePlanEntry>();

        internal readonly List<HabitatDiningRestorePlanEntry> DiningEntries =
            new List<HabitatDiningRestorePlanEntry>();
    }

    // Transient living restore plan entry only. Not Scribed.
    internal sealed class HabitatSleepRestorePlanEntry
    {
        internal ShuttleHolderLaunchManifestEntry Entry;
        internal Pawn Pawn;
        internal ThingOwner SourceOwner;
    }

    // Transient living restore plan entry only. Not Scribed.
    internal sealed class HabitatDiningRestorePlanEntry
    {
        internal ShuttleHolderLaunchManifestEntry PawnEntry;
        internal ShuttleHolderLaunchManifestEntry FoodEntry;
        internal Pawn Pawn;
        internal ThingOwner PawnSourceOwner;
        internal Thing FoodSourceThing;
        internal ThingOwner FoodSourceOwner;
        internal int FoodStackCount;
        internal Thing FoodForRestore;
        internal bool FoodWasSplit;
    }

    // Transient joy restore plan entry only. Not Scribed.
    internal sealed class HabitatJoyRestorePlanEntry
    {
        internal ShuttleHolderLaunchManifestEntry Entry;
        internal Pawn Pawn;
        internal JoyKindDef JoyKind;
        internal ThingOwner SourceOwner;
    }

    // Transient mixed restore plan only. Not Scribed.
    internal sealed class HabitatMixedRestorePlan
    {
        internal readonly List<HabitatMixedSleepRestorePlanEntry> SleepEntries =
            new List<HabitatMixedSleepRestorePlanEntry>();

        internal readonly List<HabitatMixedDiningRestorePlanEntry> DiningEntries =
            new List<HabitatMixedDiningRestorePlanEntry>();

        internal readonly List<HabitatMixedJoyRestorePlanEntry> JoyEntries =
            new List<HabitatMixedJoyRestorePlanEntry>();

        internal readonly List<HabitatMixedFoodAllocation> FoodAllocations =
            new List<HabitatMixedFoodAllocation>();

        internal bool HasEntries
        {
            get
            {
                return this.SleepEntries.Count > 0 ||
                    this.DiningEntries.Count > 0 ||
                    this.JoyEntries.Count > 0;
            }
        }

        internal string DumpForDebug()
        {
            return ShuttleHabitatDebugFormatter.DumpMixedRestorePlan(this);
        }
    }

    // Transient mixed restore plan entry only. Not Scribed.
    internal sealed class HabitatMixedSleepRestorePlanEntry
    {
        internal ShuttleHolderLaunchManifestEntry Entry;
        internal Pawn Pawn;
        internal HabitatMixedSourceOwnerRef SourceOwner;
        internal int RestStartTick;
        internal int RestedTicks;
        internal bool IsSleeping;

        internal string DumpForDebug()
        {
            return ShuttleHabitatDebugFormatter.DumpMixedSleepRestorePlanEntry(this);
        }
    }

    // Transient mixed restore plan entry only. Not Scribed.
    internal sealed class HabitatMixedDiningRestorePlanEntry
    {
        internal ShuttleHolderLaunchManifestEntry PawnEntry;
        internal ShuttleHolderLaunchManifestEntry FoodEntry;
        internal Pawn Pawn;
        internal HabitatMixedSourceOwnerRef PawnSourceOwner;
        internal HabitatMixedFoodAllocation FoodAllocation;
        internal int FoodStackCount;
        internal Thing FoodForRestore;
        internal bool FoodWasSplit;
        internal int DiningStartTick;
        internal int ChewTicksLeft;
        internal int ChewTicksTotal;
        internal bool IsDining;
        internal bool Finalized;
        internal string ActivityID;

        internal string DumpForDebug()
        {
            return ShuttleHabitatDebugFormatter.DumpMixedDiningRestorePlanEntry(this);
        }
    }

    // Transient mixed restore plan entry only. Not Scribed.
    internal sealed class HabitatMixedJoyRestorePlanEntry
    {
        internal ShuttleHolderLaunchManifestEntry Entry;
        internal Pawn Pawn;
        internal HabitatMixedSourceOwnerRef SourceOwner;
        internal JoyKindDef JoyKind;
        internal int JoyStartTick;
        internal int JoyTicks;
        internal float JoyGainRate;
        internal int MaxJoyTicks;
        internal bool IsJoying;
        internal string ActivityID;

        internal string DumpForDebug()
        {
            return ShuttleHabitatDebugFormatter.DumpMixedJoyRestorePlanEntry(this);
        }
    }

    // Transient mixed food allocation only. Not Scribed.
    internal sealed class HabitatMixedFoodAllocation
    {
        internal ShuttleHolderLaunchManifestEntry Entry;
        internal Thing SourceThing;
        internal HabitatMixedSourceOwnerRef SourceOwner;
        internal int RequiredStackCount;
        internal int SourceStackCountBefore;
        internal int ReservedStackCountBefore;
        internal bool WouldRequireSplit;

        internal string DumpForDebug()
        {
            return ShuttleHabitatDebugFormatter.DumpMixedFoodAllocation(this);
        }
    }

    // Transient source-owner label carrier only. Not Scribed.
    internal sealed class HabitatMixedSourceOwnerRef
    {
        internal ThingOwner Owner;
        internal string Label;
    }
}
