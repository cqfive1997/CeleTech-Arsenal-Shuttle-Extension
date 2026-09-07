using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyConstruction;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Onboarding
{
    internal static class ShuttleStarterPresetBuildPolicy
    {
        internal const string HostDefName = "CT_ModularShuttleHost";
        internal const string BasicPresetDefName = "CT_Shuttle_StarterPreset_BasicFlightCargo";

        internal static bool IsBasicModeActive
        {
            get
            {
                ShuttleStarterPresetGameComponent component =
                    ShuttleStarterPresetGameComponent.CurrentComponent;
                return component != null &&
                    component.SelectionResolved &&
                    component.SelectedMode == ShuttleStarterPresetMode.BasicFlightAndCargo;
            }
        }

        internal static bool AppliesTo(BuildableDef buildableDef)
        {
            return IsBasicModeActive && IsHost(buildableDef);
        }

        internal static bool IsHost(BuildableDef buildableDef)
        {
            return buildableDef != null && buildableDef.defName == HostDefName;
        }

        internal static ShuttleStarterPresetDef ResolveBasicPreset()
        {
            return DefDatabase<ShuttleStarterPresetDef>.GetNamedSilentFail(
                BasicPresetDefName);
        }

        internal static bool ArePackageResearchPrerequisitesFinished()
        {
            ShuttleStarterPresetDef preset = ResolveBasicPreset();
            if (preset == null || preset.steps == null || preset.steps.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < preset.steps.Count; i++)
            {
                ShuttleStarterPresetStepDef step = preset.steps[i];
                if (step == null || string.IsNullOrEmpty(step.targetDefName))
                {
                    return false;
                }

                if (step.kind == ShuttleStarterPresetStepKind.Segment)
                {
                    ShuttleSegmentBaseDef segmentDef =
                        DefDatabase<ShuttleSegmentBaseDef>.GetNamedSilentFail(
                            step.targetDefName);
                    if (segmentDef == null ||
                        !ShuttleResearchGateUtility.IsUnlocked(segmentDef))
                    {
                        return false;
                    }

                    continue;
                }

                ShuttleModuleBaseDef moduleDef =
                    DefDatabase<ShuttleModuleBaseDef>.GetNamedSilentFail(
                        step.targetDefName);
                if (moduleDef == null ||
                    !ShuttleResearchGateUtility.IsUnlocked(moduleDef))
                {
                    return false;
                }
            }

            return true;
        }

        internal static List<ThingDefCountClass> BuildEffectiveCostList(
            List<ThingDefCountClass> hostCostList)
        {
            List<ThingDefCountClass> result = CloneCosts(hostCostList);
            ShuttleStarterPresetDef preset = ResolveBasicPreset();
            if (preset == null || preset.steps == null)
            {
                return result;
            }

            for (int i = 0; i < preset.steps.Count; i++)
            {
                ShuttleStarterPresetStepDef step = preset.steps[i];
                if (step == null)
                {
                    continue;
                }

                if (step.kind == ShuttleStarterPresetStepKind.Segment)
                {
                    ShuttleSegmentBaseDef segmentDef =
                        DefDatabase<ShuttleSegmentBaseDef>.GetNamedSilentFail(
                            step.targetDefName);
                    AddCosts(
                        result,
                        ShuttleConstructionCostUtility.GetSegmentConstructionCost(
                            segmentDef));
                    continue;
                }

                ShuttleModuleBaseDef moduleDef =
                    DefDatabase<ShuttleModuleBaseDef>.GetNamedSilentFail(
                        step.targetDefName);
                AddCosts(
                    result,
                    ShuttleConstructionCostUtility.GetModuleConstructionCost(
                        moduleDef));
            }

            return result;
        }

        internal static float BuildEffectiveWorkAmount(float hostWorkAmount)
        {
            double total = Math.Max(0d, hostWorkAmount);
            if (!IsBasicModeActive)
            {
                return ShuttleConstructionWorkTuning.ApplyToAuthoredWork((float)total);
            }

            ShuttleStarterPresetDef preset = ResolveBasicPreset();
            if (preset == null || preset.steps == null)
            {
                return ShuttleConstructionWorkTuning.ApplyToAuthoredWork((float)total);
            }

            for (int i = 0; i < preset.steps.Count; i++)
            {
                ShuttleStarterPresetStepDef step = preset.steps[i];
                if (step == null)
                {
                    continue;
                }

                if (step.kind == ShuttleStarterPresetStepKind.Segment)
                {
                    ShuttleSegmentBaseDef segmentDef =
                        DefDatabase<ShuttleSegmentBaseDef>.GetNamedSilentFail(
                            step.targetDefName);
                    total += ShuttleConstructionCostUtility
                        .GetSegmentBaseConstructionWorkTicks(segmentDef);
                    continue;
                }

                ShuttleModuleBaseDef moduleDef =
                    DefDatabase<ShuttleModuleBaseDef>.GetNamedSilentFail(
                        step.targetDefName);
                total += ShuttleConstructionCostUtility
                    .GetModuleBaseConstructionWorkTicks(moduleDef);
            }

            float authoredTotal = total < float.MaxValue
                ? (float)total
                : float.MaxValue;
            return ShuttleConstructionWorkTuning.ApplyToAuthoredWork(authoredTotal);
        }

        internal static void ResetVanillaBuildCostCache()
        {
            CostListCalculator.Reset();
        }

        private static List<ThingDefCountClass> CloneCosts(
            List<ThingDefCountClass> costs)
        {
            List<ThingDefCountClass> result = new List<ThingDefCountClass>();
            AddCosts(result, costs);
            return result;
        }

        private static void AddCosts(
            List<ThingDefCountClass> destination,
            IReadOnlyList<ThingDefCountClass> source)
        {
            if (destination == null || source == null)
            {
                return;
            }

            for (int i = 0; i < source.Count; i++)
            {
                ThingDefCountClass cost = source[i];
                if (cost == null || cost.thingDef == null || cost.count <= 0)
                {
                    continue;
                }

                ThingDefCountClass existing = null;
                for (int j = 0; j < destination.Count; j++)
                {
                    ThingDefCountClass candidate = destination[j];
                    if (candidate != null && candidate.thingDef == cost.thingDef)
                    {
                        existing = candidate;
                        break;
                    }
                }

                if (existing == null)
                {
                    destination.Add(new ThingDefCountClass(cost.thingDef, cost.count));
                    continue;
                }

                long combined = (long)existing.count + cost.count;
                existing.count = combined < int.MaxValue
                    ? (int)combined
                    : int.MaxValue;
            }
        }
    }
}
