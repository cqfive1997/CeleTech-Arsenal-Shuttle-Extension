using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Presentation
{
    internal static class ShuttleLocalizedDefTextLookup
    {
        private static readonly Dictionary<string, string> TextCache =
            new Dictionary<string, string>();
        private static readonly HashSet<string> MissingCache =
            new HashSet<string>();

        internal static string GetLabel(Def def)
        {
            string text;
            if (TryGetInjectedText(def, "label", out text))
            {
                return text;
            }

            return def != null && !string.IsNullOrEmpty(def.label)
                ? def.LabelCap.ToString()
                : def != null ? def.defName : "-";
        }

        internal static string GetDescription(Def def)
        {
            string text;
            if (TryGetInjectedText(def, "description", out text))
            {
                return text;
            }

            return def != null && !string.IsNullOrEmpty(def.description)
                ? def.description
                : "-";
        }

        private static bool TryGetInjectedText(
            Def def,
            string fieldName,
            out string text)
        {
            text = null;
            if (def == null ||
                string.IsNullOrEmpty(def.defName) ||
                string.IsNullOrEmpty(fieldName))
            {
                return false;
            }

            string modRoot = GetModRoot();
            if (string.IsNullOrEmpty(modRoot))
            {
                return false;
            }

            List<string> languageFolders = BuildLanguageFolderCandidates();
            for (int languageIndex = 0; languageIndex < languageFolders.Count; languageIndex++)
            {
                string languageFolder = languageFolders[languageIndex];
                if (string.IsNullOrEmpty(languageFolder))
                {
                    continue;
                }

                Type type = def.GetType();
                while (type != null && typeof(Def).IsAssignableFrom(type))
                {
                    if (TryGetInjectedTextFromFolder(
                        modRoot,
                        languageFolder,
                        type.Name,
                        def.defName,
                        fieldName,
                        out text))
                    {
                        return true;
                    }

                    type = type.BaseType;
                }
            }

            return false;
        }

        private static bool TryGetInjectedTextFromFolder(
            string modRoot,
            string languageFolder,
            string defTypeFolder,
            string defName,
            string fieldName,
            out string text)
        {
            text = null;
            string cacheKey = languageFolder + "|" +
                defTypeFolder + "|" +
                defName + "." + fieldName;
            if (TextCache.TryGetValue(cacheKey, out text))
            {
                return true;
            }

            if (MissingCache.Contains(cacheKey))
            {
                return false;
            }

            string directory = Path.Combine(
                modRoot,
                "Languages",
                languageFolder,
                "DefInjected",
                defTypeFolder);
            if (!Directory.Exists(directory))
            {
                MissingCache.Add(cacheKey);
                return false;
            }

            string elementName = defName + "." + fieldName;
            string[] files = Directory.GetFiles(
                directory,
                "*.xml",
                SearchOption.TopDirectoryOnly);
            for (int i = 0; i < files.Length; i++)
            {
                string candidate = TryReadElement(files[i], elementName);
                if (!string.IsNullOrEmpty(candidate))
                {
                    TextCache[cacheKey] = candidate;
                    text = candidate;
                    return true;
                }
            }

            MissingCache.Add(cacheKey);
            return false;
        }

        private static string TryReadElement(string path, string elementName)
        {
            try
            {
                XDocument document = XDocument.Load(path);
                if (document.Root == null)
                {
                    return null;
                }

                XElement element = document.Root.Element(elementName);
                return element != null ? element.Value : null;
            }
            catch (Exception ex)
            {
                Log.Warning(
                    "[CeleTech Shuttle] Failed to read DefInjected text from " +
                    path +
                    ": " +
                    ex.Message);
                return null;
            }
        }

        private static List<string> BuildLanguageFolderCandidates()
        {
            List<string> candidates = new List<string>();
            AddUnique(candidates, Prefs.LangFolderName);

            string language = Prefs.LangFolderName ?? string.Empty;
            if (language.StartsWith(
                "ChineseSimplified",
                StringComparison.OrdinalIgnoreCase))
            {
                AddUnique(candidates, "ChineseSimplified");
            }
            else if (language.StartsWith(
                "English",
                StringComparison.OrdinalIgnoreCase))
            {
                AddUnique(candidates, "English");
            }

            return candidates;
        }

        private static void AddUnique(List<string> candidates, string value)
        {
            if (string.IsNullOrEmpty(value) || candidates.Contains(value))
            {
                return;
            }

            candidates.Add(value);
        }

        private static string GetModRoot()
        {
            if (LoadedModManager.RunningModsListForReading == null)
            {
                return null;
            }

            for (int i = 0; i < LoadedModManager.RunningModsListForReading.Count; i++)
            {
                ModContentPack mod = LoadedModManager.RunningModsListForReading[i];
                if (IsShuttleModPackage(mod))
                {
                    return mod.RootDir;
                }
            }

            return null;
        }

        private static bool IsShuttleModPackage(ModContentPack mod)
        {
            if (mod == null || string.IsNullOrEmpty(mod.PackageId))
            {
                return false;
            }

            if (string.Equals(
                mod.PackageId,
                ShuttleModConstants.PackageId,
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return string.Equals(
                mod.PackageId,
                ShuttleModConstants.PackageId + ".MKII",
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
