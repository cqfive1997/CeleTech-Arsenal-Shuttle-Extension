using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    internal sealed class ShuttleAssemblyReadSnapshotBuilder
    {
        public ShuttleAssemblyReadSnapshot Build(ShuttleAssemblyState assemblyState)
        {
            ShuttleAssemblyReadSnapshot snapshot = new ShuttleAssemblyReadSnapshot();
            if (assemblyState == null)
            {
                return snapshot;
            }

            assemblyState.EnsureInitialized();

            for (int i = 0; i < assemblyState.SegmentSlots.Count; i++)
            {
                ShuttleSegmentSlot slot = assemblyState.SegmentSlots[i];
                if (slot == null)
                {
                    continue;
                }

                ShuttleAssemblySegmentSlotSnapshot slotSnapshot = new ShuttleAssemblySegmentSlotSnapshot();
                slotSnapshot.SlotID = slot.SlotID;
                slotSnapshot.SlotTypeID = slot.SlotTypeID;
                slotSnapshot.IsRequired = slot.IsRequired;
                slotSnapshot.IsLocked = slot.IsLocked;
                slotSnapshot.IsFixed = slot.IsFixed;
                slotSnapshot.SlotLabelKey = slot.LabelKey;
                slotSnapshot.SlotShortLabelKey = slot.ShortLabelKey;
                slotSnapshot.SlotDescriptionKey = slot.DescriptionKey;
                slotSnapshot.SlotDisplayLabel = this.TranslateKeyOrNull(slot.LabelKey);
                slotSnapshot.SlotShortDisplayLabel = this.TranslateKeyOrNull(slot.ShortLabelKey);
                slotSnapshot.SlotDescription = this.TranslateKeyOrNull(slot.DescriptionKey);
                slotSnapshot.DefaultSegmentDefName = slot.DefaultSegmentDefName;
                slotSnapshot.InstalledSegmentInstanceID = slot.InstalledSegmentInstanceID;

                if (!string.IsNullOrEmpty(slot.DefaultSegmentDefName))
                {
                    ShuttleSegmentBaseDef defaultSegmentDef =
                        DefDatabase<ShuttleSegmentBaseDef>.GetNamedSilentFail(slot.DefaultSegmentDefName);
                    slotSnapshot.DefaultSegmentLabel = this.GetDefLabel(defaultSegmentDef);
                    slotSnapshot.DefaultSegmentDescription = this.GetDefDescription(defaultSegmentDef);
                }

                ShuttleSegment segment = null;
                if (!string.IsNullOrEmpty(slot.InstalledSegmentInstanceID))
                {
                    segment = assemblyState.GetSegment(slot.InstalledSegmentInstanceID);
                }

                if (segment != null)
                {
                    slotSnapshot.InstalledSegmentDefName = segment.segmentDefName;
                    slotSnapshot.InstalledSegmentLabel = this.GetDefLabel(segment.SegmentDef);
                    slotSnapshot.InstalledSegmentDescription = this.GetDefDescription(segment.SegmentDef);
                    if (segment.SegmentDef != null)
                    {
                        slotSnapshot.InstalledSegmentTypeID = segment.SegmentDef.segmentTypeID;
                    }

                    this.AddModuleSlots(slotSnapshot, segment, assemblyState);
                }

                snapshot.SegmentSlots.Add(slotSnapshot);
            }

            return snapshot;
        }

        private void AddModuleSlots(
            ShuttleAssemblySegmentSlotSnapshot segmentSnapshot,
            ShuttleSegment segment,
            ShuttleAssemblyState assemblyState)
        {
            if (segmentSnapshot == null || segment == null || segment.ModuleSlots == null || assemblyState == null)
            {
                return;
            }

            int weaponSlotOrdinal = 0;
            for (int i = 0; i < segment.ModuleSlots.Count; i++)
            {
                ShuttleModuleSlot slot = segment.ModuleSlots[i];
                if (slot == null)
                {
                    continue;
                }

                ShuttleModuleType slotType =
                    ShuttleModuleTypeCatalog.ParseSlotType(slot.SlotTypeID);
                if (slotType == ShuttleModuleType.Weapon)
                {
                    weaponSlotOrdinal++;
                }

                ShuttleAssemblyModuleSlotSnapshot slotSnapshot = new ShuttleAssemblyModuleSlotSnapshot();
                slotSnapshot.SlotID = slot.SlotID;
                slotSnapshot.SlotTypeID = slot.SlotTypeID;
                slotSnapshot.IsRequired = slot.IsRequired;
                slotSnapshot.IsLocked = slot.IsLocked;
                slotSnapshot.SlotLabelKey = slot.LabelKey;
                slotSnapshot.SlotShortLabelKey = slot.ShortLabelKey;
                slotSnapshot.SlotDescriptionKey = slot.DescriptionKey;
                slotSnapshot.SlotDisplayLabel =
                    this.GetModuleSlotDisplayLabel(slot, slotType, weaponSlotOrdinal, false);
                slotSnapshot.SlotShortDisplayLabel =
                    this.GetModuleSlotDisplayLabel(slot, slotType, weaponSlotOrdinal, true);
                slotSnapshot.SlotDescription = this.TranslateKeyOrNull(slot.DescriptionKey);
                slotSnapshot.InstalledModuleInstanceID = slot.InstalledModuleInstanceID;

                ShuttleModule module = null;
                if (!string.IsNullOrEmpty(slot.InstalledModuleInstanceID))
                {
                    module = assemblyState.GetModule(slot.InstalledModuleInstanceID);
                }

                if (module != null)
                {
                    slotSnapshot.InstalledModuleDefName = module.moduleDefName;
                    slotSnapshot.InstalledModuleLabel = this.GetDefLabel(module.ModuleDef);
                    slotSnapshot.InstalledModuleDescription = this.GetDefDescription(module.ModuleDef);
                    slotSnapshot.InstalledModuleTypeID = module.ModuleDef != null ? module.ModuleDef.moduleTypeID : null;
                    slotSnapshot.InstalledModuleEnabled = module.IsEnabled;
                }

                segmentSnapshot.ModuleSlots.Add(slotSnapshot);
            }
        }

        private string GetModuleSlotDisplayLabel(
            ShuttleModuleSlot slot,
            ShuttleModuleType slotType,
            int weaponSlotOrdinal,
            bool shortLabel)
        {
            string key = slot != null
                ? shortLabel ? slot.ShortLabelKey : slot.LabelKey
                : null;
            if (slotType != ShuttleModuleType.Weapon ||
                !this.IsGenericWeaponSlotLabelKey(key))
            {
                return this.TranslateKeyOrNull(key);
            }

            if (weaponSlotOrdinal <= 0)
            {
                return this.TranslateKeyOrNull(key);
            }

            string indexedKey = shortLabel
                ? "CT_Shuttle_ModuleSlot_WeaponStationIndexedShort"
                : "CT_Shuttle_ModuleSlot_WeaponStationIndexed";
            return indexedKey.Translate(weaponSlotOrdinal).ToString();
        }

        private bool IsGenericWeaponSlotLabelKey(string key)
        {
            return key == "CT_Shuttle_ModuleSlot_Weapon" ||
                key == "CT_Shuttle_ModuleSlot_WeaponShort";
        }

        private string GetDefLabel(Def def)
        {
            if (def == null)
            {
                return null;
            }

            string label = def.LabelCap;
            if (!string.IsNullOrEmpty(label))
            {
                return label;
            }

            return def.defName;
        }

        private string GetDefDescription(Def def)
        {
            if (def == null)
            {
                return null;
            }

            return string.IsNullOrEmpty(def.description) ? null : def.description;
        }

        private string TranslateKeyOrNull(string key)
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
    }
}
