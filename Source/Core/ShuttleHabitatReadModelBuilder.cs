using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Profile;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Sections;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    /// <summary>
    /// Builds a read-only Habitat UI snapshot from profile capability and occupancy state.
    /// This keeps UI code away from holders, records, and live Pawn need mutation.
    /// </summary>
    internal sealed class ShuttleHabitatReadModelBuilder
    {
        private const float UrgentNeedThreshold = 0.20f;
        private const float CriticalNeedThreshold = 0.10f;

        internal ShuttleHabitatReadModel Build(
            ShuttleProfile profile,
            ShuttleAssemblyState assemblyState,
            ThingWithComps shuttleHost)
        {
            ShuttleHabitatReadModel model = new ShuttleHabitatReadModel();
            HabitatProfile habitat = profile != null ? profile.Habitat : null;
            if (habitat != null)
            {
                model.HasHabitat = habitat.HasHabitat;
                model.SupportsSleep = habitat.SupportsSleep;
                model.SupportsDining = habitat.SupportsDining;
                model.SleepSlots = habitat.HasHabitat ? habitat.SleepSlots : 0;
                model.DiningSlots = habitat.HasHabitat ? habitat.DiningSlots : 0;
                model.SupportsJoy = habitat.SupportsJoy;
                model.JoySlots = habitat.HasHabitat ? habitat.JoySlots : 0;
                model.JoyKindCapacity = habitat.HasHabitat ? habitat.JoyKindCapacity : 0;
            }

            this.FillJoyConfigs(model, assemblyState);

            CompShuttleHabitatOccupancy occupancy = shuttleHost != null
                ? shuttleHost.TryGetComp<CompShuttleHabitatOccupancy>()
                : null;
            if (occupancy == null)
            {
                // A missing comp should not hide static Habitat capability from the UI.
                model.Occupants = new List<ShuttleHabitatOccupantReadModel>();
                return model;
            }

            model.SleepingOccupants = occupancy.SleepingOccupantCount;
            model.DiningOccupants = occupancy.DiningOccupantCount;
            model.JoyOccupants = occupancy.JoyOccupantCount;
            model.TotalOccupants = occupancy.TotalOccupantCount;
            model.HasAnyOccupants = occupancy.HasAnyOccupants;
            model.CanEjectOccupants = occupancy.HasAnyOccupants &&
                shuttleHost != null &&
                shuttleHost.Map != null;

            List<Pawn> sleepingPawns = occupancy.GetSleepingOccupantsForReading();
            List<Pawn> diningPawns = occupancy.GetDiningOccupantsForReading();
            IReadOnlyList<Pawn> joyPawns = occupancy.GetJoyOccupantsForReading();
            List<Pawn> allPawns = occupancy.OccupantsForReading;
            List<ShuttleHabitatOccupantReadModel> occupantModels = new List<ShuttleHabitatOccupantReadModel>();
            if (allPawns != null)
            {
                for (int i = 0; i < allPawns.Count; i++)
                {
                    Pawn pawn = allPawns[i];
                    if (pawn != null)
                    {
                        // Occupancy returns copies for reading; never expose the underlying holder.
                        occupantModels.Add(this.BuildOccupantModel(pawn, occupancy, sleepingPawns, diningPawns, joyPawns));
                    }
                }
            }

            model.Occupants = occupantModels;
            return model;
        }

        private ShuttleHabitatOccupantReadModel BuildOccupantModel(
            Pawn pawn,
            CompShuttleHabitatOccupancy occupancy,
            List<Pawn> sleepingPawns,
            List<Pawn> diningPawns,
            IReadOnlyList<Pawn> joyPawns)
        {
            ShuttleHabitatOccupantReadModel model = new ShuttleHabitatOccupantReadModel();
            model.PawnThingID = pawn.thingIDNumber;
            model.LabelShort = pawn.LabelShort;
            model.LabelCap = pawn.LabelCap;
            model.DisplayThing = pawn;
            model.Activity = this.GetActivity(pawn, sleepingPawns, diningPawns, joyPawns);
            model.ActivityLabelKey = this.GetActivityLabelKey(model.Activity);
            model.FoodLabel = string.Empty;

            if (model.Activity == HabitatOccupantActivity.Dining && occupancy != null)
            {
                Thing food;
                if (occupancy.TryGetDiningFoodForPawn(pawn, out food) && food != null)
                {
                    model.FoodLabel = food.LabelCap;
                }
            }

            model.FoodPct = this.GetFoodPct(pawn);
            model.RestPct = this.GetRestPct(pawn);
            model.JoyPct = this.GetJoyPct(pawn);
            model.MoodPct = this.GetMoodPct(pawn);
            this.FillUrgentNeed(model);
            return model;
        }

        private HabitatOccupantActivity GetActivity(
            Pawn pawn,
            List<Pawn> sleepingPawns,
            List<Pawn> diningPawns,
            IReadOnlyList<Pawn> joyPawns)
        {
            if (ContainsPawn(diningPawns, pawn))
            {
                return HabitatOccupantActivity.Dining;
            }

            if (ContainsPawn(joyPawns, pawn))
            {
                return HabitatOccupantActivity.Joying;
            }

            if (ContainsPawn(sleepingPawns, pawn))
            {
                return HabitatOccupantActivity.Sleeping;
            }

            return HabitatOccupantActivity.None;
        }

        private string GetActivityLabelKey(HabitatOccupantActivity activity)
        {
            if (activity == HabitatOccupantActivity.Sleeping)
            {
                return "CT_Shuttle_HabitatPanel_Sleeping";
            }

            if (activity == HabitatOccupantActivity.Dining)
            {
                return "CT_Shuttle_HabitatPanel_Dining";
            }

            if (activity == HabitatOccupantActivity.Joying)
            {
                return "CT_Shuttle_HabitatPanel_Joying";
            }

            return string.Empty;
        }

        private void FillJoyConfigs(
            ShuttleHabitatReadModel model,
            ShuttleAssemblyState assemblyState)
        {
            List<ShuttleHabitatJoyConfigReadModel> configs = new List<ShuttleHabitatJoyConfigReadModel>();
            if (model == null || assemblyState == null)
            {
                if (model != null)
                {
                    model.JoyConfigs = configs;
                }
                return;
            }

            assemblyState.EnsureInitialized();
            IReadOnlyList<ShuttleModule> modules = assemblyState.Modules;
            if (modules == null)
            {
                model.JoyConfigs = configs;
                return;
            }

            for (int i = 0; i < modules.Count; i++)
            {
                ShuttleModule module = modules[i];
                ShuttleHabitatModuleDef habitatDef = module != null
                    ? module.GetModuleDef<ShuttleHabitatModuleDef>()
                    : null;
                if (module == null || !module.IsEnabled || habitatDef == null || !habitatDef.habitatSupportsJoy)
                {
                    continue;
                }

                IReadOnlyList<JoyKindDef> selectedJoyKinds =
                    HabitatJoySelectionUtility.GetSelectedJoyKinds(module, habitatDef);
                configs.Add(new ShuttleHabitatJoyConfigReadModel(
                    module.ModuleInstanceID,
                    this.GetModuleLabel(habitatDef),
                    habitatDef.habitatJoyKindCapacity,
                    this.CopyJoyKinds(habitatDef.allowedJoyKinds),
                    this.CopyJoyKinds(selectedJoyKinds)));
            }

            model.JoyConfigs = configs;
        }

        private void FillUrgentNeed(ShuttleHabitatOccupantReadModel model)
        {
            // Critical/urgent ordering is intentionally simple and stable for the compact panel.
            HabitatNeedKind kind = HabitatNeedKind.None;
            float pct = -1f;
            if (this.TryFindPriorityUrgentNeed(model, out kind, out pct) ||
                this.TryFindLowestAvailableNeed(model, out kind, out pct))
            {
                model.MostUrgentNeed = kind;
                model.MostUrgentNeedPct = pct;
                model.MostUrgentNeedLabelKey = this.GetNeedLabelKey(kind);
            }
            else
            {
                model.MostUrgentNeed = HabitatNeedKind.None;
                model.MostUrgentNeedPct = -1f;
                model.MostUrgentNeedLabelKey = "CT_Shuttle_HabitatPanel_NoUrgentNeed";
            }

            model.IsCritical =
                IsCritical(model.FoodPct) ||
                IsCritical(model.RestPct) ||
                IsCritical(model.JoyPct) ||
                IsCritical(model.MoodPct);
        }

        private bool TryFindPriorityUrgentNeed(
            ShuttleHabitatOccupantReadModel model,
            out HabitatNeedKind kind,
            out float pct)
        {
            if (IsUrgent(model.FoodPct))
            {
                kind = HabitatNeedKind.Food;
                pct = model.FoodPct;
                return true;
            }

            if (IsUrgent(model.RestPct))
            {
                kind = HabitatNeedKind.Rest;
                pct = model.RestPct;
                return true;
            }

            if (IsUrgent(model.JoyPct))
            {
                kind = HabitatNeedKind.Joy;
                pct = model.JoyPct;
                return true;
            }

            if (IsUrgent(model.MoodPct))
            {
                kind = HabitatNeedKind.Mood;
                pct = model.MoodPct;
                return true;
            }

            kind = HabitatNeedKind.None;
            pct = -1f;
            return false;
        }

        private bool TryFindLowestAvailableNeed(
            ShuttleHabitatOccupantReadModel model,
            out HabitatNeedKind kind,
            out float pct)
        {
            kind = HabitatNeedKind.None;
            pct = -1f;
            this.TrySetLowestNeed(HabitatNeedKind.Food, model.FoodPct, ref kind, ref pct);
            this.TrySetLowestNeed(HabitatNeedKind.Rest, model.RestPct, ref kind, ref pct);
            this.TrySetLowestNeed(HabitatNeedKind.Joy, model.JoyPct, ref kind, ref pct);
            this.TrySetLowestNeed(HabitatNeedKind.Mood, model.MoodPct, ref kind, ref pct);
            return kind != HabitatNeedKind.None;
        }

        private void TrySetLowestNeed(
            HabitatNeedKind candidateKind,
            float candidatePct,
            ref HabitatNeedKind currentKind,
            ref float currentPct)
        {
            if (candidatePct < 0f)
            {
                return;
            }

            if (currentKind == HabitatNeedKind.None || candidatePct < currentPct)
            {
                currentKind = candidateKind;
                currentPct = candidatePct;
            }
        }

        private string GetNeedLabelKey(HabitatNeedKind kind)
        {
            if (kind == HabitatNeedKind.Food)
            {
                return "CT_Shuttle_HabitatPanel_FoodNeed";
            }

            if (kind == HabitatNeedKind.Rest)
            {
                return "CT_Shuttle_HabitatPanel_RestNeed";
            }

            if (kind == HabitatNeedKind.Joy)
            {
                return "CT_Shuttle_HabitatPanel_JoyNeed";
            }

            if (kind == HabitatNeedKind.Mood)
            {
                return "CT_Shuttle_HabitatPanel_MoodNeed";
            }

            return "CT_Shuttle_HabitatPanel_NoUrgentNeed";
        }

        private float GetFoodPct(Pawn pawn)
        {
            return pawn != null && pawn.needs != null && pawn.needs.food != null
                ? pawn.needs.food.CurLevelPercentage
                : -1f;
        }

        private float GetRestPct(Pawn pawn)
        {
            return pawn != null && pawn.needs != null && pawn.needs.rest != null
                ? pawn.needs.rest.CurLevelPercentage
                : -1f;
        }

        private float GetJoyPct(Pawn pawn)
        {
            return pawn != null && pawn.needs != null && pawn.needs.joy != null
                ? pawn.needs.joy.CurLevelPercentage
                : -1f;
        }

        private float GetMoodPct(Pawn pawn)
        {
            return pawn != null && pawn.needs != null && pawn.needs.mood != null
                ? pawn.needs.mood.CurLevelPercentage
                : -1f;
        }

        private string GetModuleLabel(ShuttleHabitatModuleDef habitatDef)
        {
            return habitatDef != null ? habitatDef.LabelCap.ToString() : string.Empty;
        }

        private IReadOnlyList<JoyKindDef> CopyJoyKinds(IReadOnlyList<JoyKindDef> joyKinds)
        {
            List<JoyKindDef> copy = new List<JoyKindDef>();
            if (joyKinds == null)
            {
                return copy;
            }

            for (int i = 0; i < joyKinds.Count; i++)
            {
                JoyKindDef joyKind = joyKinds[i];
                if (joyKind != null && !copy.Contains(joyKind))
                {
                    copy.Add(joyKind);
                }
            }

            return copy;
        }

        private static bool ContainsPawn(IReadOnlyList<Pawn> pawns, Pawn pawn)
        {
            if (pawn == null || pawns == null)
            {
                return false;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                if (pawns[i] == pawn)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsUrgent(float pct)
        {
            return pct >= 0f && pct < UrgentNeedThreshold;
        }

        private static bool IsCritical(float pct)
        {
            return pct >= 0f && pct < CriticalNeedThreshold;
        }
    }
}
