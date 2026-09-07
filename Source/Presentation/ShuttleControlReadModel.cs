using System;
using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Profile;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Presentation
{
    public static class ShuttleControlDisplayNameResolver
    {
        public static string ResolveSegmentSlotLabel(
            ShuttleControlReadModel model,
            string slotID,
            bool shortLabel = false)
        {
            ShuttleControlSegmentSlotModel slot =
                model != null ? model.FindSegmentSlot(slotID) : null;
            if (slot != null)
            {
                return ResolveSegmentSlotLabel(slot, shortLabel);
            }

            int index = ParseTrailingIndex(slotID);
            if (index >= 0)
            {
                return "CT_Shuttle_InfoPanel_RequiredSegmentSlotIndexed"
                    .Translate(index + 1)
                    .ToString();
            }

            return "CT_Shuttle_InfoPanel_RequiredSegmentSlotGeneric".Translate().ToString();
        }

        public static string ResolveModuleSlotLabel(
            ShuttleControlReadModel model,
            string moduleSlotID,
            string segmentSlotID = null,
            bool shortLabel = false)
        {
            if (model != null && model.SegmentSlots != null)
            {
                string segmentReference = segmentSlotID;
                string moduleReference = moduleSlotID;
                SplitModuleReference(moduleSlotID, ref segmentReference, ref moduleReference);

                for (int i = 0; i < model.SegmentSlots.Count; i++)
                {
                    ShuttleControlSegmentSlotModel segment = model.SegmentSlots[i];
                    if (segment == null)
                    {
                        continue;
                    }

                    bool segmentMatches =
                        string.IsNullOrEmpty(segmentReference) ||
                        segmentReference == segment.SlotID ||
                        segmentReference == segment.InstalledSegmentInstanceID ||
                        segmentReference == segment.InstalledSegmentDefName;

                    if (segment.ModuleSlots == null)
                    {
                        continue;
                    }

                    for (int j = 0; j < segment.ModuleSlots.Count; j++)
                    {
                        ShuttleControlModuleSlotModel module = segment.ModuleSlots[j];
                        if (module == null)
                        {
                            continue;
                        }

                        if (segmentMatches && ModuleSlotMatches(module, moduleSlotID, moduleReference))
                        {
                            return ResolveModuleSlotLabel(segment, module, shortLabel);
                        }
                    }

                    if (segmentMatches && !string.IsNullOrEmpty(moduleReference))
                    {
                        return ResolveModuleSlotFallback(segment, moduleReference);
                    }
                }
            }

            int index = ParseTrailingIndex(moduleSlotID);
            if (index >= 0)
            {
                return "CT_Shuttle_InfoPanel_RequiredModuleSlotIndexed"
                    .Translate(index + 1)
                    .ToString();
            }

            return "CT_Shuttle_InfoPanel_RequiredModuleSlotGeneric".Translate().ToString();
        }

        public static string ResolveSegmentLabel(ShuttleControlSegmentSlotModel slot)
        {
            if (slot == null)
            {
                return "CT_Shuttle_InfoPanel_RequiredSegmentSlotGeneric".Translate().ToString();
            }

            string installedDefLabel = ResolveSegmentDefLabel(slot.InstalledSegmentDefName);
            if (!string.IsNullOrEmpty(installedDefLabel))
            {
                return installedDefLabel;
            }

            string defaultDefLabel = ResolveSegmentDefLabel(slot.DefaultSegmentDefName);
            if (!string.IsNullOrEmpty(defaultDefLabel))
            {
                return defaultDefLabel;
            }

            if (!string.IsNullOrEmpty(slot.InstalledSegmentLabel))
            {
                return slot.InstalledSegmentLabel;
            }

            if (!string.IsNullOrEmpty(slot.DefaultSegmentLabel))
            {
                return slot.DefaultSegmentLabel;
            }

            return ResolveSegmentSlotLabel(slot, false);
        }

        public static string ResolveModuleLabel(ShuttleControlModuleSlotModel slot)
        {
            if (slot == null)
            {
                return "CT_Shuttle_InfoPanel_RequiredModuleSlotGeneric".Translate().ToString();
            }

            string installedDefLabel = ResolveModuleDefLabel(slot.InstalledModuleDefName);
            if (!string.IsNullOrEmpty(installedDefLabel))
            {
                return installedDefLabel;
            }

            if (!string.IsNullOrEmpty(slot.InstalledModuleLabel))
            {
                return slot.InstalledModuleLabel;
            }

            return ResolveModuleSlotLabelOnly(slot, false);
        }

        public static string ResolveCategoryLabel(string category)
        {
            string normalized = NormalizeCategoryKey(category);
            string key = "CT_Shuttle_InfoPanel_Category_" + normalized;
            string translated = TranslateIfAvailable(key);
            return !string.IsNullOrEmpty(translated)
                ? translated
                : "CT_Shuttle_InfoPanel_Category_Runtime".Translate().ToString();
        }

        public static string ResolveSeverityLabel(string severity)
        {
            string normalized = NormalizeSeverityKey(severity);
            string key = "CT_Shuttle_InfoPanel_Severity_" + normalized;
            string translated = TranslateIfAvailable(key);
            return !string.IsNullOrEmpty(translated)
                ? translated
                : "CT_Shuttle_InfoPanel_Severity_Info".Translate().ToString();
        }

        private static string NormalizeSeverityKey(string severity)
        {
            if (string.IsNullOrEmpty(severity))
            {
                return "Info";
            }

            string lower = severity.Trim().ToLowerInvariant();
            if (lower.Contains("fatal") || lower.Contains("error"))
            {
                return "Error";
            }

            if (lower.Contains("warn"))
            {
                return "Warning";
            }

            if (lower == "ok")
            {
                return "OK";
            }

            if (lower.Contains("resolved"))
            {
                return "Resolved";
            }

            return "Info";
        }

        private static string NormalizeCategoryKey(string category)
        {
            if (string.IsNullOrEmpty(category))
            {
                return "Runtime";
            }

            string lower = category.Trim().ToLowerInvariant();
            if (lower.Contains("dev") || lower.Contains("diagnostic") || lower == "issue" || lower == "perf")
            {
                return "Dev";
            }

            if (lower.Contains("segment"))
            {
                return "Segment";
            }

            if (lower.Contains("crew") || lower.Contains("cockpit"))
            {
                return "Crew";
            }

            if (lower.Contains("cargo"))
            {
                return "Cargo";
            }

            if (lower.Contains("power") || lower.Contains("energy"))
            {
                return "Power";
            }

            if (lower.Contains("medical"))
            {
                return "Medical";
            }

            if (lower.Contains("prison"))
            {
                return "Prison";
            }

            if (lower.Contains("mech"))
            {
                return "Mech";
            }

            if (lower.Contains("assembly"))
            {
                return "Assembly";
            }

            if (lower.Contains("environment"))
            {
                return "Environment";
            }

            if (lower.Contains("target"))
            {
                return "Target";
            }

            if (lower.Contains("food"))
            {
                return "Food";
            }

            if (lower.Contains("processing") || lower.Contains("production"))
            {
                return "Processing";
            }

            if (lower.Contains("defense"))
            {
                return "Defense";
            }

            if (lower.Contains("launch"))
            {
                return "Launch";
            }

            return "Runtime";
        }

        public static string ResolveSegmentSlotLabel(ShuttleControlSegmentSlotModel slot, bool shortLabel = false)
        {
            if (slot == null)
            {
                return "CT_Shuttle_InfoPanel_RequiredSegmentSlotGeneric".Translate().ToString();
            }

            string explicitLabel = shortLabel && !string.IsNullOrEmpty(slot.ShortDisplayLabel)
                ? slot.ShortDisplayLabel
                : slot.DisplayLabel;
            if (!string.IsNullOrEmpty(explicitLabel))
            {
                return "CT_Shuttle_InfoPanel_NamedSlot".Translate(explicitLabel).ToString();
            }

            string translatedKey = TranslateIfAvailable(slot.DisplayLabelKey);
            if (!string.IsNullOrEmpty(translatedKey))
            {
                return "CT_Shuttle_InfoPanel_NamedSlot".Translate(translatedKey).ToString();
            }

            string segmentLabel = ResolveSegmentLabelDirect(slot);
            if (!string.IsNullOrEmpty(segmentLabel))
            {
                return "CT_Shuttle_InfoPanel_NamedSlot".Translate(segmentLabel).ToString();
            }

            string typeLabel = ResolveSegmentSlotTypeLabel(slot.SlotTypeID, shortLabel);
            if (!string.IsNullOrEmpty(typeLabel))
            {
                return "CT_Shuttle_InfoPanel_NamedSlot".Translate(typeLabel).ToString();
            }

            int index = ParseTrailingIndex(slot.SlotID);
            if (index >= 0)
            {
                return (slot.IsRequired
                    ? "CT_Shuttle_InfoPanel_RequiredSegmentSlotIndexed"
                    : "CT_Shuttle_InfoPanel_OptionalSegmentSlotIndexed")
                    .Translate(index + 1)
                    .ToString();
            }

            return (slot.IsRequired
                ? "CT_Shuttle_InfoPanel_RequiredSegmentSlotGeneric"
                : "CT_Shuttle_InfoPanel_OptionalSegmentSlotGeneric")
                .Translate()
                .ToString();
        }

        public static string ResolveModuleSlotLabel(
            ShuttleControlSegmentSlotModel segment,
            ShuttleControlModuleSlotModel module,
            bool shortLabel = false)
        {
            string moduleLabel = ResolveModuleSlotLabelOnly(module, shortLabel);
            string segmentLabel = ResolveSegmentSlotLabel(segment, true);
            return string.IsNullOrEmpty(segmentLabel)
                ? moduleLabel
                : segmentLabel + " / " + moduleLabel;
        }

        private static string ResolveModuleSlotLabelOnly(
            ShuttleControlModuleSlotModel module,
            bool shortLabel)
        {
            if (module == null)
            {
                return "CT_Shuttle_InfoPanel_RequiredModuleSlotGeneric".Translate().ToString();
            }

            string explicitLabel = shortLabel && !string.IsNullOrEmpty(module.ShortDisplayLabel)
                ? module.ShortDisplayLabel
                : module.DisplayLabel;
            if (!string.IsNullOrEmpty(explicitLabel))
            {
                return "CT_Shuttle_InfoPanel_NamedSlot".Translate(explicitLabel).ToString();
            }

            string translatedKey = TranslateIfAvailable(module.DisplayLabelKey);
            if (!string.IsNullOrEmpty(translatedKey))
            {
                return "CT_Shuttle_InfoPanel_NamedSlot".Translate(translatedKey).ToString();
            }

            string moduleTypeLabel = ResolveModuleSlotTypeLabel(module.SlotTypeID, shortLabel);
            if (!string.IsNullOrEmpty(moduleTypeLabel))
            {
                return "CT_Shuttle_InfoPanel_NamedSlot".Translate(moduleTypeLabel).ToString();
            }

            int index = ParseTrailingIndex(module.SlotID);
            if (index >= 0)
            {
                return (module.IsRequired
                    ? "CT_Shuttle_InfoPanel_RequiredModuleSlotIndexed"
                    : "CT_Shuttle_InfoPanel_OptionalModuleSlotIndexed")
                    .Translate(index + 1)
                    .ToString();
            }

            return (module.IsRequired
                ? "CT_Shuttle_InfoPanel_RequiredModuleSlotGeneric"
                : "CT_Shuttle_InfoPanel_OptionalModuleSlotGeneric")
                .Translate()
                .ToString();
        }

        private static string ResolveModuleSlotFallback(
            ShuttleControlSegmentSlotModel segment,
            string moduleReference)
        {
            int index = ParseTrailingIndex(moduleReference);
            string moduleLabel = index >= 0
                ? "CT_Shuttle_InfoPanel_RequiredModuleSlotIndexed".Translate(index + 1).ToString()
                : "CT_Shuttle_InfoPanel_RequiredModuleSlotGeneric".Translate().ToString();
            string segmentLabel = ResolveSegmentSlotLabel(segment, true);
            return string.IsNullOrEmpty(segmentLabel)
                ? moduleLabel
                : segmentLabel + " / " + moduleLabel;
        }

        private static string ResolveSegmentLabelDirect(ShuttleControlSegmentSlotModel slot)
        {
            if (slot == null)
            {
                return null;
            }

            string installedDefLabel = ResolveSegmentDefLabel(slot.InstalledSegmentDefName);
            if (!string.IsNullOrEmpty(installedDefLabel))
            {
                return installedDefLabel;
            }

            string defaultDefLabel = ResolveSegmentDefLabel(slot.DefaultSegmentDefName);
            if (!string.IsNullOrEmpty(defaultDefLabel))
            {
                return defaultDefLabel;
            }

            if (!string.IsNullOrEmpty(slot.InstalledSegmentLabel))
            {
                return slot.InstalledSegmentLabel;
            }

            return !string.IsNullOrEmpty(slot.DefaultSegmentLabel) ? slot.DefaultSegmentLabel : null;
        }

        private static string ResolveSegmentDefLabel(string defName)
        {
            if (string.IsNullOrEmpty(defName))
            {
                return null;
            }

            ShuttleSegmentBaseDef def =
                DefDatabase<ShuttleSegmentBaseDef>.GetNamedSilentFail(defName);
            return ResolveDefLabel(def);
        }

        private static string ResolveModuleDefLabel(string defName)
        {
            if (string.IsNullOrEmpty(defName))
            {
                return null;
            }

            ShuttleModuleBaseDef def =
                DefDatabase<ShuttleModuleBaseDef>.GetNamedSilentFail(defName);
            return ResolveDefLabel(def);
        }

        private static string ResolveDefLabel(Def def)
        {
            if (def == null)
            {
                return null;
            }

            string label = ShuttleLocalizedDefTextLookup.GetLabel(def);
            if (string.IsNullOrEmpty(label) ||
                label == "-" ||
                label == def.defName)
            {
                return null;
            }

            return label;
        }

        private static string ResolveSegmentSlotTypeLabel(string slotTypeID, bool shortLabel)
        {
            string suffix = ResolveSegmentSlotKeySuffix(slotTypeID);
            if (string.IsNullOrEmpty(suffix))
            {
                return null;
            }

            string key = "CT_Shuttle_Slot_" + suffix + (shortLabel ? "Short" : string.Empty);
            string translated = TranslateIfAvailable(key);
            if (!string.IsNullOrEmpty(translated))
            {
                return translated;
            }

            return shortLabel ? ResolveSegmentSlotTypeLabel(slotTypeID, false) : null;
        }

        private static string ResolveModuleSlotTypeLabel(string slotTypeID, bool shortLabel)
        {
            string suffix = ResolveModuleSlotKeySuffix(slotTypeID);
            if (string.IsNullOrEmpty(suffix))
            {
                return null;
            }

            string key = "CT_Shuttle_ModuleSlot_" + suffix + (shortLabel ? "Short" : string.Empty);
            string translated = TranslateIfAvailable(key);
            if (!string.IsNullOrEmpty(translated))
            {
                return translated;
            }

            return shortLabel ? ResolveModuleSlotTypeLabel(slotTypeID, false) : null;
        }

        private static string ResolveSegmentSlotKeySuffix(string slotTypeID)
        {
            string normalized = ShuttleSegmentTypeCatalog.NormalizeSegmentSlotTypeID(slotTypeID);
            if (normalized == "cockpit")
            {
                return "Cockpit";
            }

            if (normalized == "power" || normalized == "engine")
            {
                return "Engine";
            }

            if (normalized == "cargo")
            {
                return "Cargo";
            }

            if (normalized == "support")
            {
                return "Support";
            }

            if (normalized == "weapon")
            {
                return "Weapon";
            }

            if (normalized == "living" || normalized == "habitat")
            {
                return "Habitat";
            }

            return null;
        }

        private static string ResolveModuleSlotKeySuffix(string slotTypeID)
        {
            string normalized = ShuttleModuleTypeCatalog.NormalizeModuleSlotTypeID(slotTypeID);
            if (normalized == "cockpit")
            {
                return "Cockpit";
            }

            if (normalized == "navigation")
            {
                return "Navigation";
            }

            if (normalized == "stabilizer" || normalized == "drive-control")
            {
                return "Stabilizer";
            }

            if (normalized == "reactor")
            {
                return "Reactor";
            }

            if (normalized == "battery" || normalized == "power-regulator")
            {
                return "Battery";
            }

            if (normalized == "cargo" || normalized == "refrigerated-cargo")
            {
                return "Cargo";
            }

            if (normalized == "logistics")
            {
                return "Logistics";
            }

            if (normalized == "medical" || normalized == "medical-bay")
            {
                return "Medical";
            }

            if (normalized == "prison" || normalized == "prison-cell")
            {
                return "Prison";
            }

            if (normalized == "mech" || normalized == "mech-charger")
            {
                return "Mech";
            }

            if (normalized == "weapon" || normalized == "fire-control" || normalized == "ammo-loader")
            {
                return "Weapon";
            }

            if (normalized == "shield" || normalized == "armor")
            {
                return "Shield";
            }

            if (normalized == "processing" || normalized == "production" || normalized == "auto-worktable")
            {
                return "Processing";
            }

            return null;
        }

        private static bool ModuleSlotMatches(
            ShuttleControlModuleSlotModel module,
            string rawReference,
            string moduleReference)
        {
            if (module == null)
            {
                return false;
            }

            return rawReference == module.SlotID ||
                moduleReference == module.SlotID ||
                (!string.IsNullOrEmpty(rawReference) && rawReference.EndsWith("/" + module.SlotID)) ||
                (!string.IsNullOrEmpty(rawReference) && rawReference.EndsWith("::" + module.SlotID));
        }

        private static void SplitModuleReference(
            string referenceID,
            ref string segmentReference,
            ref string moduleReference)
        {
            if (string.IsNullOrEmpty(referenceID))
            {
                return;
            }

            int separatorIndex = referenceID.IndexOf("::");
            if (separatorIndex >= 0)
            {
                segmentReference = referenceID.Substring(0, separatorIndex);
                moduleReference = referenceID.Substring(separatorIndex + 2);
                return;
            }

            separatorIndex = referenceID.LastIndexOf('/');
            if (separatorIndex >= 0)
            {
                segmentReference = referenceID.Substring(0, separatorIndex);
                moduleReference = referenceID.Substring(separatorIndex + 1);
            }
        }

        private static string TranslateIfAvailable(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            if (!Translator.CanTranslate(key))
            {
                return null;
            }

            string translated = key.Translate().ToString();
            return string.IsNullOrEmpty(translated) || translated == key ? null : translated;
        }

        private static int ParseTrailingIndex(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return -1;
            }

            int end = value.Length - 1;
            while (end >= 0 && !char.IsDigit(value[end]))
            {
                end--;
            }

            if (end < 0)
            {
                return -1;
            }

            int start = end;
            while (start >= 0 && char.IsDigit(value[start]))
            {
                start--;
            }

            int index;
            return int.TryParse(value.Substring(start + 1, end - start), out index) ? index : -1;
        }
    }

    public sealed class ShuttleControlReadModelBuilder
    {
        private readonly ShuttleControlRemovalReadModelBuilder removalBuilder =
            new ShuttleControlRemovalReadModelBuilder();
        private readonly ShuttleControlLaunchStatusReadModelBuilder launchStatusBuilder =
            new ShuttleControlLaunchStatusReadModelBuilder();
        private readonly ShuttleControlHullReadModelBuilder hullBuilder =
            new ShuttleControlHullReadModelBuilder();
        private readonly ShuttleControlAssemblyConstructionReadModelBuilder assemblyConstructionBuilder =
            new ShuttleControlAssemblyConstructionReadModelBuilder();
        private readonly ShuttleControlChecklistReadModelBuilder checklistBuilder =
            new ShuttleControlChecklistReadModelBuilder();
        private readonly ShuttleControlSlotReadModelBuilder slotBuilder;

        public ShuttleControlReadModelBuilder()
        {
            this.slotBuilder = new ShuttleControlSlotReadModelBuilder(this.removalBuilder);
        }

        public ShuttleControlReadModel Build(
            ShuttleProfile profile,
            bool isProfileDirty,
            string shuttleLabel,
            ShuttleAssemblyReadSnapshot assemblySnapshot,
            ShuttlePowerRuntimeSnapshot powerSnapshot,
            ShuttleRuntimeState runtimeState = null,
            ShuttleAssemblyState assemblyState = null,
            IReadOnlyList<ProfileBuildIssue> assemblyReadinessIssues = null,
            IReadOnlyList<ShuttleDeveloperDiagnosticModel> developerDiagnostics = null)
        {
            ShuttleControlReadModel model = new ShuttleControlReadModel();
            model.ProfileRevision = profile != null ? profile.Revision : 0;
            model.IsProfileDirty = isProfileDirty;
            model.ShuttleLabel = !string.IsNullOrEmpty(shuttleLabel)
                ? shuttleLabel
                : "CT_Shuttle_UI_DefaultShuttleLabel".Translate().ToString();

            if (profile != null && profile.Layout != null)
            {
                model.SegmentSlotCount = profile.Layout.SegmentSlotCount;
                model.InstalledSegmentCount = profile.Layout.InstalledSegmentCount;
                model.ModuleSlotCount = profile.Layout.ModuleSlotCount;
                model.InstalledModuleCount = profile.Layout.InstalledModuleCount;
            }

            if (profile != null && profile.Mass != null)
            {
                model.TotalMass = profile.Mass.TotalMass;
                model.MassCapacity = profile.Mass.MassCapacity;
                model.RemainingMassCapacity = profile.Mass.RemainingMassCapacity;
            }

            if (profile != null && profile.Power != null)
            {
                model.EnergyCapacityWd = profile.Power.EnergyStorageCapacityWd;
                model.ReactorGenerationWatts = profile.Power.ReactorGenerationWatts;
                model.InternalIdleDemandWatts = profile.Power.InternalIdleDemandWatts;
                model.MaxBatteryChargeWatts = profile.Power.MaxBatteryChargeWatts;
                model.MaxBatteryDischargeWatts = profile.Power.MaxBatteryDischargeWatts;
                model.PowerGeneration = model.ReactorGenerationWatts;
            }

            if (powerSnapshot != null)
            {
                model.HasPowerRuntimeSnapshot = true;
                model.StoredEnergyWd = powerSnapshot.StoredEnergyWd;
                model.GridExportWatts = powerSnapshot.LastGridExportWatts;
                model.BatteryChargeWatts = powerSnapshot.LastBatteryChargeWatts;
                model.BatteryDischargeWatts = powerSnapshot.LastBatteryDischargeWatts;
                model.CurrentReactorGenerationWatts =
                    powerSnapshot.LastReactorGenerationWatts;
                model.ActiveInternalDemandWatts =
                    powerSnapshot.LastInternalDemandWatts;
                model.TransientInternalDemandWatts =
                    powerSnapshot.LastTransientInternalDemandWatts;
                model.UnmetInternalDemandWatts = powerSnapshot.LastUnmetDemandWatts;
                model.InternalBusPowered = powerSnapshot.InternalBusPowered;
            }

            if (profile != null && profile.Cargo != null)
            {
                model.CargoRegionCount = profile.Cargo.CargoRegionCount;
                model.CargoMassCapacityKg = profile.Cargo.CargoMassCapacityKg;
            }

            if (profile != null && profile.CargoLogistics != null)
            {
                model.HasCargoLogistics = profile.CargoLogistics.HasCargoLogistics;
                model.CargoLogisticsSupportsItemTransfer = profile.CargoLogistics.SupportsItemTransfer;
                model.CargoLogisticsSupportsItemConsumption = profile.CargoLogistics.SupportsItemConsumption;
                model.CargoLogisticsSupportsItemDeposit = profile.CargoLogistics.SupportsItemDeposit;
                model.CargoLogisticsSupportsNutritionDistribution = profile.CargoLogistics.SupportsNutritionDistribution;
                model.CargoLogisticsMaxStacksMovedPerTick = profile.CargoLogistics.MaxStacksMovedPerTick;
            }

            if (profile != null && profile.Flight != null)
            {
                model.HardRangeCapTiles = profile.Flight.HardRangeCapTiles;
                model.LaunchCooldownTicks = profile.Flight.LaunchCooldownTicks;
                model.EnergyPerTileWd = profile.Flight.EnergyPerTileWd;
                model.EnergyPerKgTileWd = profile.Flight.EnergyPerKgTileWd;
            }

            this.launchStatusBuilder.ApplyLaunchCooldown(model, runtimeState);
            this.hullBuilder.ApplyHull(model, profile, runtimeState, assemblyState);
            this.assemblyConstructionBuilder.ApplyAssemblyConstruction(model, runtimeState);
            this.slotBuilder.ApplyPageAvailability(model, profile);
            model.PaintScheme = assemblyState != null
                ? ShuttlePaintSchemeSnapshot.FromScheme(assemblyState.PaintScheme)
                : ShuttlePaintSchemeSnapshot.Default;

            if (profile != null && profile.Command != null)
            {
                model.HasCockpit = profile.Command.HasCockpit;
                model.StabilizesAdverseWeather = profile.Command.StabilizesAdverseWeather;
                model.ProvidesAutonomousLaunchControl = profile.Command.ProvidesAutonomousLaunchControl;
            }

            this.slotBuilder.AddAssemblySlots(model, assemblySnapshot, runtimeState);
            this.checklistBuilder.AddIssues(model, profile);
            this.checklistBuilder.AddAssemblyReadinessIssues(model, assemblyReadinessIssues);
            this.checklistBuilder.AddLaunchChecklist(model, assemblyReadinessIssues);
            this.checklistBuilder.AddDeveloperDiagnostics(model, profile, developerDiagnostics);
            return model;
        }
    }
}
