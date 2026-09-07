using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Utilities
{
    public static class ShuttleResearchGateUtility
    {
        public static bool IsUnlocked(List<ResearchProjectDef> prerequisites)
        {
            if (prerequisites == null || prerequisites.Count == 0)
            {
                return true;
            }

            for (int i = 0; i < prerequisites.Count; i++)
            {
                ResearchProjectDef researchProject = prerequisites[i];
                if (researchProject != null && !researchProject.IsFinished)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsUnlocked(ShuttleModuleBaseDef def)
        {
            return def == null || IsUnlocked(def.researchPrerequisites);
        }

        public static bool IsUnlocked(ShuttleSegmentBaseDef def)
        {
            return def == null || IsUnlocked(def.researchPrerequisites);
        }

        public static ResearchProjectDef FirstUnfinished(List<ResearchProjectDef> prerequisites)
        {
            if (prerequisites == null || prerequisites.Count == 0)
            {
                return null;
            }

            for (int i = 0; i < prerequisites.Count; i++)
            {
                ResearchProjectDef researchProject = prerequisites[i];
                if (researchProject == null || researchProject.IsFinished)
                {
                    continue;
                }

                return researchProject;
            }

            return null;
        }

        public static string FirstUnfinishedLabel(List<ResearchProjectDef> prerequisites)
        {
            ResearchProjectDef researchProject = FirstUnfinished(prerequisites);
            if (researchProject == null)
            {
                return null;
            }

            return !string.IsNullOrEmpty(researchProject.label)
                ? researchProject.LabelCap.ToString()
                : researchProject.defName;
        }

        public static string GetLockedReason(List<ResearchProjectDef> prerequisites)
        {
            string label = FirstUnfinishedLabel(prerequisites);
            return string.IsNullOrEmpty(label)
                ? null
                : "CT_Shuttle_Command_ResearchLockedByProject".Translate(label).ToString();
        }

        public static string GetLockedReason(ShuttleModuleBaseDef def)
        {
            return def != null ? GetLockedReason(def.researchPrerequisites) : null;
        }

        public static string GetLockedReason(ShuttleSegmentBaseDef def)
        {
            return def != null ? GetLockedReason(def.researchPrerequisites) : null;
        }

        public static string GetUILockedReason(ShuttleModuleBaseDef def)
        {
            return BuildUILockedReason(def != null ? def.researchPrerequisites : null);
        }

        public static string GetUILockedReason(ShuttleSegmentBaseDef def)
        {
            return BuildUILockedReason(def != null ? def.researchPrerequisites : null);
        }

        private static string BuildUILockedReason(List<ResearchProjectDef> prerequisites)
        {
            string label = FirstUnfinishedLabel(prerequisites);
            return string.IsNullOrEmpty(label)
                ? "CT_Shuttle_UI_ResearchLocked".Translate().ToString()
                : "CT_Shuttle_UI_ResearchRequired".Translate(label).ToString();
        }
    }
}
