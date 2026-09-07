using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels.Pawns
{
    internal sealed class ShuttlePawnDynamicStatusRecord
    {
        internal int PawnThingID;
        internal string StableKey;
        internal Pawn Pawn;
        internal Thing DisplayThing;
        internal bool IsDead;
        internal bool IsDowned;
        internal bool IsConsciousKnown;
        internal bool IsConscious;
        internal float ConsciousnessPct = -1f;
        internal float MovingPct = -1f;
        internal float PainPct = -1f;
        internal bool IsBleeding;
        internal float BleedRateTotal;
        internal int BleedingHediffCount;
        internal float BleedSeverityPct = -1f;
        internal float FoodPct = -1f;
        internal float RestPct = -1f;
        internal float JoyPct = -1f;
        internal float MoodPct = -1f;
        internal bool HasMechDurability;
        internal float DurabilityPct = -1f;
        internal float DamagePct = -1f;
        internal bool HasMechEnergy;
        internal float EnergyPct = -1f;
    }
}
