using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels
{
    internal sealed class ShuttleReadModelIssueDisplayNameResolver
    {
        internal List<ShuttleReadModelDisplayNameReplacement> BuildReplacements(
            ShuttleControlReadModel model)
        {
            List<ShuttleReadModelDisplayNameReplacement> replacements =
                new List<ShuttleReadModelDisplayNameReplacement>();
            if (model == null || model.SegmentSlots == null)
            {
                return replacements;
            }

            for (int i = 0; i < model.SegmentSlots.Count; i++)
            {
                this.AddSegmentReplacements(replacements, model.SegmentSlots[i]);
            }

            return replacements;
        }

        internal string ResolveReferenceDisplayName(
            ShuttleControlReadModel model,
            string referenceID)
        {
            if (string.IsNullOrEmpty(referenceID) ||
                model == null ||
                model.SegmentSlots == null)
            {
                return null;
            }

            for (int i = 0; i < model.SegmentSlots.Count; i++)
            {
                string displayName = this.ResolveSegmentReference(
                    model.SegmentSlots[i],
                    referenceID);
                if (!string.IsNullOrEmpty(displayName))
                {
                    return displayName;
                }
            }

            return null;
        }

        private void AddSegmentReplacements(
            List<ShuttleReadModelDisplayNameReplacement> replacements,
            ShuttleControlSegmentSlotModel segment)
        {
            if (segment == null)
            {
                return;
            }

            string segmentName = this.GetSegmentDisplayName(segment);
            this.AddReplacement(replacements, segment.SlotID, segmentName);
            this.AddReplacement(replacements, segment.InstalledSegmentInstanceID, segmentName);
            this.AddReplacement(replacements, segment.InstalledSegmentDefName, segmentName);

            if (segment.ModuleSlots == null)
            {
                return;
            }

            for (int i = 0; i < segment.ModuleSlots.Count; i++)
            {
                this.AddModuleReplacements(replacements, segment, segment.ModuleSlots[i]);
            }
        }

        private void AddModuleReplacements(
            List<ShuttleReadModelDisplayNameReplacement> replacements,
            ShuttleControlSegmentSlotModel segment,
            ShuttleControlModuleSlotModel module)
        {
            if (module == null)
            {
                return;
            }

            string moduleSlotName = this.GetModuleSlotDisplayName(segment, module);
            string moduleName = this.GetModuleDisplayName(module);
            this.AddReplacement(replacements, module.SlotID, moduleSlotName);
            this.AddReplacement(replacements, module.InstalledModuleInstanceID, moduleName);
            this.AddReplacement(replacements, module.InstalledModuleDefName, moduleName);
        }

        private void AddReplacement(
            List<ShuttleReadModelDisplayNameReplacement> replacements,
            string rawID,
            string displayName)
        {
            if (string.IsNullOrEmpty(rawID) ||
                string.IsNullOrEmpty(displayName) ||
                rawID == displayName)
            {
                return;
            }

            replacements.Add(new ShuttleReadModelDisplayNameReplacement(rawID, displayName));
        }

        private string ResolveSegmentReference(
            ShuttleControlSegmentSlotModel segment,
            string referenceID)
        {
            if (segment == null)
            {
                return null;
            }

            if (referenceID == segment.SlotID ||
                referenceID == segment.InstalledSegmentInstanceID ||
                referenceID == segment.InstalledSegmentDefName)
            {
                return this.GetSegmentDisplayName(segment);
            }

            return this.ResolveModuleReference(segment, referenceID);
        }

        private string ResolveModuleReference(
            ShuttleControlSegmentSlotModel segment,
            string referenceID)
        {
            if (segment == null || segment.ModuleSlots == null)
            {
                return null;
            }

            for (int i = 0; i < segment.ModuleSlots.Count; i++)
            {
                ShuttleControlModuleSlotModel module = segment.ModuleSlots[i];
                if (module == null)
                {
                    continue;
                }

                if (referenceID == module.SlotID)
                {
                    return this.GetModuleSlotDisplayName(segment, module);
                }

                if (referenceID == module.InstalledModuleInstanceID ||
                    referenceID == module.InstalledModuleDefName)
                {
                    return this.GetModuleDisplayName(module);
                }
            }

            return null;
        }

        private string GetSegmentDisplayName(ShuttleControlSegmentSlotModel segment)
        {
            string resolved = ShuttleControlDisplayNameResolver.ResolveSegmentSlotLabel(segment);
            return !string.IsNullOrEmpty(resolved)
                ? resolved
                : "CT_Shuttle_InfoPanel_RequiredSegmentSlotGeneric".Translate().ToString();
        }

        private string GetModuleSlotDisplayName(
            ShuttleControlSegmentSlotModel segment,
            ShuttleControlModuleSlotModel module)
        {
            string resolved = ShuttleControlDisplayNameResolver.ResolveModuleSlotLabel(segment, module);
            return !string.IsNullOrEmpty(resolved)
                ? resolved
                : "CT_Shuttle_InfoPanel_RequiredModuleSlotGeneric".Translate().ToString();
        }

        private string GetModuleDisplayName(ShuttleControlModuleSlotModel module)
        {
            if (module == null)
            {
                return "CT_Shuttle_InfoPanel_RequiredModuleSlotGeneric".Translate().ToString();
            }

            string defLabel = this.GetModuleDefLabel(module.InstalledModuleDefName);
            if (!string.IsNullOrEmpty(defLabel))
            {
                return defLabel;
            }

            if (!string.IsNullOrEmpty(module.InstalledModuleLabel))
            {
                return module.InstalledModuleLabel;
            }

            string cleaned = this.CleanDefName(module.InstalledModuleDefName);
            return !string.IsNullOrEmpty(cleaned) && cleaned != "-"
                ? cleaned
                : ShuttleControlDisplayNameResolver.ResolveModuleLabel(module);
        }

        private string GetModuleDefLabel(string defName)
        {
            if (string.IsNullOrEmpty(defName))
            {
                return null;
            }

            string key = defName + ".label";
            if (Translator.CanTranslate(key))
            {
                string translated = key.Translate().ToString();
                if (!string.IsNullOrEmpty(translated) && translated != key)
                {
                    return translated;
                }
            }

            ShuttleModuleBaseDef moduleDef =
                DefDatabase<ShuttleModuleBaseDef>.GetNamedSilentFail(defName);
            if (moduleDef == null)
            {
                return null;
            }

            string label = ShuttleAssemblyDisplayTextResolver.ResolveDefDisplayName(
                moduleDef,
                null);
            return !string.IsNullOrEmpty(label) && label != "-"
                ? label
                : null;
        }

        private string CleanDefName(string raw)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return "-";
            }

            string normalized = raw
                .Replace("CT_", string.Empty)
                .Replace("CeleTech_", string.Empty)
                .Replace("ModularShuttle", string.Empty)
                .Replace("modularShuttle", string.Empty)
                .Replace("Shuttle", string.Empty)
                .Replace("shuttle", string.Empty)
                .Replace("Segment", string.Empty)
                .Replace("segment", string.Empty)
                .Replace("Module", string.Empty)
                .Replace("module", string.Empty)
                .Replace("Slot", string.Empty)
                .Replace("slot", string.Empty)
                .Replace("Def", string.Empty)
                .Replace("def", string.Empty);
            char[] separators = { '_', '-', '.', '/', '\\', ' ' };
            string[] parts = normalized.Split(separators, System.StringSplitOptions.RemoveEmptyEntries);
            return parts == null || parts.Length == 0
                ? raw
                : this.JoinDisplayParts(parts, raw);
        }

        private string JoinDisplayParts(string[] parts, string fallback)
        {
            string result = string.Empty;
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                if (string.IsNullOrEmpty(part))
                {
                    continue;
                }

                string lower = part.ToLowerInvariant();
                if (lower == "ct" || lower == "main" || lower == "base" || lower == "type")
                {
                    continue;
                }

                if (result.Length > 0)
                {
                    result += " ";
                }

                result += char.ToUpperInvariant(part[0]) + (part.Length > 1 ? part.Substring(1) : string.Empty);
            }

            return string.IsNullOrEmpty(result) ? fallback : result;
        }
    }
}
