using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Research
{
    internal sealed class ShuttleControlResearchFingerprintService
    {
        private const string ModularShuttleHostDefName = "CT_ModularShuttleHost";

        private static List<ResearchProjectDef> cachedResearchDefs;

        internal int GetFingerprint()
        {
            List<ResearchProjectDef> defs = GetRelevantResearchDefs();
            unchecked
            {
                int fingerprint = 17;
                fingerprint = (fingerprint * 31) + defs.Count;
                for (int i = 0; i < defs.Count; i++)
                {
                    ResearchProjectDef researchDef = defs[i];
                    fingerprint = (fingerprint * 31) + (researchDef != null && researchDef.IsFinished ? 1 : 0);
                }

                return fingerprint;
            }
        }

        private static List<ResearchProjectDef> GetRelevantResearchDefs()
        {
            if (cachedResearchDefs != null)
            {
                return cachedResearchDefs;
            }

            List<ResearchProjectDef> result = new List<ResearchProjectDef>();
            HashSet<ResearchProjectDef> seen = new HashSet<ResearchProjectDef>();
            AddSegmentResearch(result, seen);
            AddModuleResearch(result, seen);
            AddHostResearch(result, seen);
            result.Sort(CompareResearchDefNames);
            cachedResearchDefs = result;
            return cachedResearchDefs;
        }

        private static void AddSegmentResearch(
            List<ResearchProjectDef> result,
            HashSet<ResearchProjectDef> seen)
        {
            List<ShuttleSegmentBaseDef> defs = DefDatabase<ShuttleSegmentBaseDef>.AllDefsListForReading;
            for (int i = 0; defs != null && i < defs.Count; i++)
            {
                ShuttleSegmentBaseDef segmentDef = defs[i];
                AddResearchList(segmentDef != null ? segmentDef.researchPrerequisites : null, result, seen);
            }
        }

        private static void AddModuleResearch(
            List<ResearchProjectDef> result,
            HashSet<ResearchProjectDef> seen)
        {
            List<ShuttleModuleBaseDef> defs = DefDatabase<ShuttleModuleBaseDef>.AllDefsListForReading;
            for (int i = 0; defs != null && i < defs.Count; i++)
            {
                ShuttleModuleBaseDef moduleDef = defs[i];
                AddResearchList(moduleDef != null ? moduleDef.researchPrerequisites : null, result, seen);
            }
        }

        private static void AddHostResearch(
            List<ResearchProjectDef> result,
            HashSet<ResearchProjectDef> seen)
        {
            ThingDef hostDef = DefDatabase<ThingDef>.GetNamedSilentFail(ModularShuttleHostDefName);
            AddResearchList(hostDef != null ? hostDef.researchPrerequisites : null, result, seen);
        }

        private static void AddResearchList(
            List<ResearchProjectDef> source,
            List<ResearchProjectDef> result,
            HashSet<ResearchProjectDef> seen)
        {
            for (int i = 0; source != null && i < source.Count; i++)
            {
                ResearchProjectDef researchDef = source[i];
                if (researchDef == null || seen.Contains(researchDef))
                {
                    continue;
                }

                seen.Add(researchDef);
                result.Add(researchDef);
            }
        }

        private static int CompareResearchDefNames(
            ResearchProjectDef left,
            ResearchProjectDef right)
        {
            string leftName = left != null ? left.defName : null;
            string rightName = right != null ? right.defName : null;
            return string.Compare(leftName, rightName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
