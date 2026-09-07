using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Extensions;
using CeleTech.ShuttleExtension.ModularShuttle.Profile;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Presentation
{
    internal sealed class ShuttleControlSlotReadModelBuilder
    {
        private readonly ShuttleControlRemovalReadModelBuilder removalBuilder;

        internal ShuttleControlSlotReadModelBuilder(ShuttleControlRemovalReadModelBuilder removalBuilder)
        {
            this.removalBuilder = removalBuilder;
        }

        internal void AddAssemblySlots(
            ShuttleControlReadModel model,
            ShuttleAssemblyReadSnapshot assemblySnapshot,
            ShuttleRuntimeState runtimeState)
        {
            if (model == null || assemblySnapshot == null || assemblySnapshot.SegmentSlots == null)
            {
                return;
            }

            for (int i = 0; i < assemblySnapshot.SegmentSlots.Count; i++)
            {
                ShuttleAssemblySegmentSlotSnapshot slot = assemblySnapshot.SegmentSlots[i];
                if (slot == null)
                {
                    continue;
                }

                ShuttleControlSegmentSlotModel slotModel = new ShuttleControlSegmentSlotModel();
                slotModel.SlotID = slot.SlotID;
                slotModel.SlotTypeID = slot.SlotTypeID;
                slotModel.IsRequired = slot.IsRequired;
                slotModel.IsLocked = slot.IsLocked;
                slotModel.IsFixed = slot.IsFixed;
                slotModel.DisplayLabel = slot.SlotDisplayLabel;
                slotModel.ShortDisplayLabel = slot.SlotShortDisplayLabel;
                slotModel.DisplayLabelKey = slot.SlotLabelKey;
                slotModel.Description = slot.SlotDescription;
                slotModel.DescriptionKey = slot.SlotDescriptionKey;
                slotModel.DefaultSegmentDefName = slot.DefaultSegmentDefName;
                slotModel.DefaultSegmentLabel = slot.DefaultSegmentLabel;
                slotModel.DefaultSegmentDescription = slot.DefaultSegmentDescription;
                slotModel.InstalledSegmentInstanceID = slot.InstalledSegmentInstanceID;
                slotModel.InstalledSegmentDefName = slot.InstalledSegmentDefName;
                slotModel.InstalledSegmentLabel = slot.InstalledSegmentLabel;
                slotModel.InstalledSegmentDescription = slot.InstalledSegmentDescription;
                slotModel.SegmentTypeID = slot.InstalledSegmentTypeID;
                this.AddModuleSlots(model, slotModel, slot.ModuleSlots, runtimeState);
                this.ApplySegmentPageAvailability(model, slotModel);
                this.removalBuilder.ApplySegmentRemoval(slotModel, runtimeState);

                model.SegmentSlots.Add(slotModel);
            }
        }

        internal void ApplyPageAvailability(
            ShuttleControlReadModel model,
            ShuttleProfile profile)
        {
            if (model == null)
            {
                return;
            }

            if (model.PageAvailability == null)
            {
                model.PageAvailability = new ShuttleControlPageAvailabilityReadModel();
            }

            model.PageAvailability.Medical =
                profile != null &&
                profile.MedicalBay != null &&
                profile.MedicalBay.HasMedicalBay;
            model.PageAvailability.PrisonCell =
                profile != null &&
                profile.PrisonCell != null &&
                profile.PrisonCell.HasPrisonCell;
            model.PageAvailability.ExternalModules = false;
        }

        private void AddModuleSlots(
            ShuttleControlReadModel model,
            ShuttleControlSegmentSlotModel segmentModel,
            List<ShuttleAssemblyModuleSlotSnapshot> moduleSlots,
            ShuttleRuntimeState runtimeState)
        {
            if (segmentModel == null || moduleSlots == null)
            {
                return;
            }

            for (int i = 0; i < moduleSlots.Count; i++)
            {
                ShuttleAssemblyModuleSlotSnapshot slot = moduleSlots[i];
                if (slot == null)
                {
                    continue;
                }

                ShuttleControlModuleSlotModel slotModel = new ShuttleControlModuleSlotModel();
                slotModel.SlotID = slot.SlotID;
                slotModel.SlotTypeID = slot.SlotTypeID;
                slotModel.IsRequired = slot.IsRequired;
                slotModel.IsLocked = slot.IsLocked;
                slotModel.DisplayLabel = slot.SlotDisplayLabel;
                slotModel.ShortDisplayLabel = slot.SlotShortDisplayLabel;
                slotModel.DisplayLabelKey = slot.SlotLabelKey;
                slotModel.Description = slot.SlotDescription;
                slotModel.DescriptionKey = slot.SlotDescriptionKey;
                slotModel.InstalledModuleInstanceID = slot.InstalledModuleInstanceID;
                slotModel.InstalledModuleDefName = slot.InstalledModuleDefName;
                slotModel.InstalledModuleLabel = slot.InstalledModuleLabel;
                slotModel.InstalledModuleDescription = slot.InstalledModuleDescription;
                slotModel.InstalledModuleTypeID = slot.InstalledModuleTypeID;
                slotModel.InstalledModuleEnabled = !string.IsNullOrEmpty(slot.InstalledModuleInstanceID) &&
                    slot.InstalledModuleEnabled;
                this.removalBuilder.ApplyModuleRemoval(slotModel, runtimeState);
                this.ApplyModulePageAvailability(model, slotModel);

                segmentModel.ModuleSlots.Add(slotModel);
            }
        }

        private void ApplySegmentPageAvailability(
            ShuttleControlReadModel model,
            ShuttleControlSegmentSlotModel segmentSlot)
        {
            if (model == null ||
                model.PageAvailability == null ||
                segmentSlot == null ||
                string.IsNullOrEmpty(segmentSlot.InstalledSegmentInstanceID))
            {
                return;
            }

            if (this.InstalledSegmentMatchesType(segmentSlot, ShuttleSegmentType.Cargo))
            {
                model.PageAvailability.Crew = true;
                model.PageAvailability.Loading = true;
            }

            if (this.InstalledSegmentMatchesType(segmentSlot, ShuttleSegmentType.Weapon))
            {
                model.PageAvailability.Defense = true;
            }
        }

        private void ApplyModulePageAvailability(
            ShuttleControlReadModel model,
            ShuttleControlModuleSlotModel moduleSlot)
        {
            if (model == null ||
                model.PageAvailability == null ||
                moduleSlot == null ||
                string.IsNullOrEmpty(moduleSlot.InstalledModuleInstanceID))
            {
                return;
            }

            ShuttleModuleType moduleType =
                ShuttleModuleTypeCatalog.ParseModuleType(moduleSlot.InstalledModuleTypeID);
            if (moduleType == ShuttleModuleType.Production ||
                this.IsKnownProcessingModuleDef(moduleSlot.InstalledModuleDefName))
            {
                model.PageAvailability.Processing = true;
            }

            if (this.InstalledModuleHasExternalRuntime(moduleSlot.InstalledModuleDefName))
            {
                model.PageAvailability.ExternalModules = true;
            }
        }

        private bool IsKnownProcessingModuleDef(string moduleDefName)
        {
            // Primary support is moduleTypeID=production. These known def names only keep
            // current built-in processing modules visible if a copied slot lacks type metadata.
            return moduleDefName == "CT_Shuttle_AutoWorkTableModule" ||
                moduleDefName == "CT_Shuttle_AutoKitchen_Basic";
        }

        private bool InstalledModuleHasExternalRuntime(string moduleDefName)
        {
            if (string.IsNullOrEmpty(moduleDefName))
            {
                return false;
            }

            ShuttleModuleBaseDef moduleDef =
                DefDatabase<ShuttleModuleBaseDef>.GetNamedSilentFail(moduleDefName);
            if (moduleDef == null)
            {
                return false;
            }

            ShuttleRuntimeSystemDefExtension extension =
                moduleDef.GetModExtension<ShuttleRuntimeSystemDefExtension>();
            if (extension == null)
            {
                return false;
            }

            List<string> runtimeSystemKeys = extension.GetRuntimeSystemKeys();
            return runtimeSystemKeys != null && runtimeSystemKeys.Count > 0;
        }

        private bool InstalledSegmentMatchesType(
            ShuttleControlSegmentSlotModel segmentSlot,
            ShuttleSegmentType expectedType)
        {
            if (segmentSlot == null || string.IsNullOrEmpty(segmentSlot.InstalledSegmentInstanceID))
            {
                return false;
            }

            ShuttleSegmentType installedType = ShuttleSegmentTypeCatalog.ParseSegmentType(segmentSlot.SegmentTypeID);
            if (installedType == expectedType)
            {
                return true;
            }

            if (installedType != ShuttleSegmentType.Unknown &&
                installedType != ShuttleSegmentType.Optional)
            {
                return false;
            }

            return ShuttleSegmentTypeCatalog.ParseSlotType(segmentSlot.SlotTypeID) == expectedType;
        }
    }
}
