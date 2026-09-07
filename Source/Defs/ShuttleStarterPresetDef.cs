using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Defs
{
    public enum ShuttleStarterPresetStepKind
    {
        Segment = 0,
        Module = 1
    }

    public sealed class ShuttleStarterPresetStepDef
    {
        public string stepID;
        public ShuttleStarterPresetStepKind kind;
        public string segmentSlotTypeID;
        public string moduleSlotTypeID;
        public string targetDefName;
    }

    public sealed class ShuttleStarterPresetDef : Def
    {
        public List<ShuttleStarterPresetStepDef> steps =
            new List<ShuttleStarterPresetStepDef>();

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }

            if (this.steps == null || this.steps.Count == 0)
            {
                yield return this.defName + " must define at least one starter preset step.";
                yield break;
            }

            HashSet<string> stepIDs = new HashSet<string>();
            for (int i = 0; i < this.steps.Count; i++)
            {
                ShuttleStarterPresetStepDef step = this.steps[i];
                if (step == null)
                {
                    yield return this.defName + " has a null starter preset step at index " + i + ".";
                    continue;
                }

                if (string.IsNullOrWhiteSpace(step.stepID))
                {
                    yield return this.defName + " has a starter preset step without stepID at index " + i + ".";
                }
                else if (!stepIDs.Add(step.stepID))
                {
                    yield return this.defName + " has duplicate starter preset stepID " + step.stepID + ".";
                }

                if (ShuttleSegmentTypeCatalog.ParseSlotType(step.segmentSlotTypeID) ==
                    ShuttleSegmentType.Unknown)
                {
                    yield return this.defName + "/" + step.stepID +
                        " defines unknown segmentSlotTypeID " + step.segmentSlotTypeID + ".";
                }

                if (string.IsNullOrWhiteSpace(step.targetDefName))
                {
                    yield return this.defName + "/" + step.stepID + " must define targetDefName.";
                    continue;
                }

                if (step.kind == ShuttleStarterPresetStepKind.Segment)
                {
                    ShuttleSegmentBaseDef segmentDef =
                        DefDatabase<ShuttleSegmentBaseDef>.GetNamedSilentFail(step.targetDefName);
                    if (segmentDef == null)
                    {
                        yield return this.defName + "/" + step.stepID +
                            " cannot resolve segment target " + step.targetDefName + ".";
                    }
                    else if (!segmentDef.playerInstallable)
                    {
                        yield return this.defName + "/" + step.stepID +
                            " targets non-player-installable segment " + step.targetDefName + ".";
                    }

                    continue;
                }

                if (ShuttleModuleTypeCatalog.ParseSlotType(step.moduleSlotTypeID) ==
                    ShuttleModuleType.Unknown)
                {
                    yield return this.defName + "/" + step.stepID +
                        " defines unknown moduleSlotTypeID " + step.moduleSlotTypeID + ".";
                }

                ShuttleModuleBaseDef moduleDef =
                    DefDatabase<ShuttleModuleBaseDef>.GetNamedSilentFail(step.targetDefName);
                if (moduleDef == null)
                {
                    yield return this.defName + "/" + step.stepID +
                        " cannot resolve module target " + step.targetDefName + ".";
                }
                else if (!moduleDef.playerInstallable)
                {
                    yield return this.defName + "/" + step.stepID +
                        " targets non-player-installable module " + step.targetDefName + ".";
                }
            }
        }
    }
}
