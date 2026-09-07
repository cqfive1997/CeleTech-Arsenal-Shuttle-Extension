using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Hull;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Main;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Actions.Main
{
    internal sealed class ShuttleMainHullPlatingMaterialMenu
    {
        private readonly IShuttleCommandExecutor commandExecutor;

        internal ShuttleMainHullPlatingMaterialMenu(
            IShuttleCommandExecutor commandExecutor)
        {
            this.commandExecutor = commandExecutor;
        }

        internal void Open(
            ShuttleControlSegmentSlotModel segment,
            ShuttleControlModuleSlotModel moduleSlot,
            ShuttleHullPlatingModuleDef moduleDef,
            bool replacing)
        {
            if (segment == null ||
                moduleSlot == null ||
                moduleDef == null ||
                this.commandExecutor == null)
            {
                this.ShowReject(ShuttleUIText.Tr("CT_Shuttle_Command_ContextUnavailable"));
                return;
            }

            List<ThingDef> stuffs = ShuttleHullArmorStuffUtility.GetAvailableHullArmorStuffs();
            if (stuffs == null || stuffs.Count == 0)
            {
                this.ShowReject(ShuttleUIText.Tr("CT_Shuttle_Hull_NoArmorStuffAvailable"));
                return;
            }

            List<FloatMenuOption> options = new List<FloatMenuOption>();
            for (int i = 0; i < stuffs.Count; i++)
            {
                ThingDef stuffDef = stuffs[i];
                if (stuffDef == null)
                {
                    continue;
                }

                string selectedStuffDefName = stuffDef.defName;
                string label = V3MainInstallCandidateText.GetHullPlatingStuffOptionLabel(stuffDef);
                options.Add(new FloatMenuOption(label, delegate
                {
                    this.BeginModuleConstruction(
                        segment,
                        moduleSlot,
                        moduleDef,
                        selectedStuffDefName,
                        replacing);
                }));
            }

            if (options.Count == 0)
            {
                this.ShowReject(ShuttleUIText.Tr("CT_Shuttle_Hull_NoArmorStuffAvailable"));
                return;
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void BeginModuleConstruction(
            ShuttleControlSegmentSlotModel segment,
            ShuttleControlModuleSlotModel moduleSlot,
            ShuttleModuleBaseDef moduleDef,
            string selectedStuffDefName,
            bool replacing)
        {
            if (segment == null ||
                moduleSlot == null ||
                moduleDef == null ||
                string.IsNullOrEmpty(segment.InstalledSegmentInstanceID) ||
                string.IsNullOrEmpty(moduleSlot.SlotID))
            {
                this.ShowReject(ShuttleUIText.Tr("CT_Shuttle_Command_ContextUnavailable"));
                return;
            }

            ShuttleCommandResult result = replacing
                ? this.commandExecutor.Execute(
                    new BeginModuleReplacementConstructionCommand(
                        segment.InstalledSegmentInstanceID,
                        moduleSlot.SlotID,
                        moduleDef.defName,
                        selectedStuffDefName))
                : this.commandExecutor.Execute(
                    new BeginModuleConstructionCommand(
                        segment.InstalledSegmentInstanceID,
                        moduleSlot.SlotID,
                        moduleDef.defName,
                        selectedStuffDefName));
            this.ShowResult(result);
        }

        private bool ShowResult(ShuttleCommandResult result)
        {
            return ShuttleUICommandFeedback.ShowResult(result, MessageTypeDefOf.PositiveEvent);
        }

        private void ShowReject(string message)
        {
            ShuttleUICommandFeedback.ShowReject(message, false);
        }
    }
}
