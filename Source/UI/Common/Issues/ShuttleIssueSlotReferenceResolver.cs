using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Issues
{
    internal static class ShuttleIssueSlotReferenceResolver
    {
        internal static string ResolveSegmentSlotDisplayName(
            ShuttleControlReadModel model,
            string referenceID)
        {
            ShuttleControlSegmentSlotModel segment;
            if (TryResolveSegmentSlot(model, referenceID, out segment))
            {
                return ShuttleAssemblyDisplayTextResolver.ResolveSegmentSlotDisplayName(
                    segment,
                    false);
            }

            string indexedFallback = ResolveIndexedSegmentSlotFallback(referenceID);
            return !string.IsNullOrEmpty(indexedFallback)
                ? indexedFallback
                : "CT_Shuttle_Issue_Evidence_UnknownSlot".Translate().ToString();
        }

        internal static string ResolveModuleSlotDisplayName(
            ShuttleControlReadModel model,
            string referenceID)
        {
            ShuttleControlSegmentSlotModel segment;
            ShuttleControlModuleSlotModel moduleSlot;
            if (TryResolveModuleSlot(model, referenceID, out segment, out moduleSlot))
            {
                return ShuttleAssemblyDisplayTextResolver.ResolveModuleSlotDisplayName(
                    segment,
                    moduleSlot,
                    false);
            }

            return "CT_Shuttle_Issue_Evidence_UnknownSlot".Translate().ToString();
        }

        internal static bool IsModuleSlotReference(string code, string referenceID)
        {
            string lowerCode = !string.IsNullOrEmpty(code)
                ? code.ToLowerInvariant()
                : string.Empty;
            return lowerCode.Contains("module") ||
                (!string.IsNullOrEmpty(referenceID) &&
                    (referenceID.Contains("::") || referenceID.Contains("/")));
        }

        internal static void SplitModuleReference(
            string referenceID,
            ref string segmentID,
            ref string moduleSlotID)
        {
            if (string.IsNullOrEmpty(referenceID))
            {
                return;
            }

            int separatorIndex = referenceID.IndexOf("::");
            if (separatorIndex >= 0)
            {
                segmentID = referenceID.Substring(0, separatorIndex);
                moduleSlotID = referenceID.Substring(separatorIndex + 2);
                return;
            }

            separatorIndex = referenceID.LastIndexOf('/');
            if (separatorIndex >= 0)
            {
                segmentID = referenceID.Substring(0, separatorIndex);
                moduleSlotID = referenceID.Substring(separatorIndex + 1);
            }
        }

        internal static bool TryResolveSegmentSlot(
            ShuttleControlReadModel model,
            string referenceID,
            out ShuttleControlSegmentSlotModel foundSegment)
        {
            foundSegment = null;
            if (model == null ||
                model.SegmentSlots == null ||
                string.IsNullOrEmpty(referenceID))
            {
                return false;
            }

            for (int i = 0; i < model.SegmentSlots.Count; i++)
            {
                ShuttleControlSegmentSlotModel segment = model.SegmentSlots[i];
                if (segment == null)
                {
                    continue;
                }

                if (referenceID == segment.SlotID ||
                    referenceID == segment.InstalledSegmentInstanceID ||
                    referenceID == segment.InstalledSegmentDefName)
                {
                    foundSegment = segment;
                    return true;
                }
            }

            return false;
        }

        internal static bool TryResolveModuleSlot(
            ShuttleControlReadModel model,
            string referenceID,
            out ShuttleControlSegmentSlotModel foundSegment,
            out ShuttleControlModuleSlotModel foundModuleSlot)
        {
            foundSegment = null;
            foundModuleSlot = null;
            if (model == null ||
                model.SegmentSlots == null ||
                string.IsNullOrEmpty(referenceID))
            {
                return false;
            }

            string segmentReference = null;
            string moduleReference = referenceID;
            SplitModuleReference(referenceID, ref segmentReference, ref moduleReference);
            for (int i = 0; i < model.SegmentSlots.Count; i++)
            {
                ShuttleControlSegmentSlotModel segment = model.SegmentSlots[i];
                if (segment == null || segment.ModuleSlots == null)
                {
                    continue;
                }

                bool segmentMatches = string.IsNullOrEmpty(segmentReference) ||
                    segmentReference == segment.SlotID ||
                    segmentReference == segment.InstalledSegmentInstanceID ||
                    segmentReference == segment.InstalledSegmentDefName;
                for (int j = 0; j < segment.ModuleSlots.Count; j++)
                {
                    ShuttleControlModuleSlotModel moduleSlot = segment.ModuleSlots[j];
                    if (segmentMatches &&
                        ModuleReferenceMatches(moduleSlot, referenceID, moduleReference))
                    {
                        foundSegment = segment;
                        foundModuleSlot = moduleSlot;
                        return true;
                    }
                }
            }

            return false;
        }

        internal static bool TryResolveModuleSlot(
            ShuttleControlReadModel model,
            string segmentReference,
            string moduleReference,
            out ShuttleControlSegmentSlotModel foundSegment,
            out ShuttleControlModuleSlotModel foundModuleSlot)
        {
            foundSegment = null;
            foundModuleSlot = null;
            if (string.IsNullOrEmpty(segmentReference))
            {
                return TryResolveModuleSlot(
                    model,
                    moduleReference,
                    out foundSegment,
                    out foundModuleSlot);
            }

            if (string.IsNullOrEmpty(moduleReference))
            {
                return TryResolveModuleSlot(
                    model,
                    segmentReference,
                    out foundSegment,
                    out foundModuleSlot);
            }

            return TryResolveModuleSlot(
                model,
                segmentReference + "::" + moduleReference,
                out foundSegment,
                out foundModuleSlot);
        }

        private static string ResolveIndexedSegmentSlotFallback(string referenceID)
        {
            if (string.IsNullOrEmpty(referenceID))
            {
                return null;
            }

            const string prefix = "segment.slot.";
            if (!referenceID.StartsWith(prefix))
            {
                return null;
            }

            int index;
            if (!int.TryParse(referenceID.Substring(prefix.Length), out index))
            {
                return null;
            }

            return "CT_Shuttle_Issue_Evidence_SegmentSlotIndexed".Translate(index + 1).ToString();
        }

        private static bool ModuleReferenceMatches(
            ShuttleControlModuleSlotModel moduleSlot,
            string rawReference,
            string moduleReference)
        {
            if (moduleSlot == null)
            {
                return false;
            }

            return rawReference == moduleSlot.SlotID ||
                moduleReference == moduleSlot.SlotID ||
                rawReference == moduleSlot.InstalledModuleInstanceID ||
                rawReference == moduleSlot.InstalledModuleDefName ||
                (!string.IsNullOrEmpty(rawReference) &&
                    rawReference.EndsWith("/" + moduleSlot.SlotID)) ||
                (!string.IsNullOrEmpty(rawReference) &&
                    rawReference.EndsWith("::" + moduleSlot.SlotID));
        }
    }
}
