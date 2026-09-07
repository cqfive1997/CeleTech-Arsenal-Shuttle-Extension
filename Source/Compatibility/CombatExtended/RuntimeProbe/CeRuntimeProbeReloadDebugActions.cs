using System.Collections.Generic;
using LudeonTK;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended.RuntimeProbe
{
    internal static class CeRuntimeProbeReloadDebugActions
    {
        private enum ReloadProbeAction
        {
            StageInsufficient,
            StageRemaining,
            Commit,
            Cancel,
            InjectRollback,
            CommitFromCargo,
            InjectCargoRollback
        }

        [DebugAction(
            "CeleTech Shuttle",
            "CE temporary external-gun reload probe...",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void BeginReloadProbe()
        {
            TargetingParameters parameters = new TargetingParameters();
            parameters.canTargetBuildings = true;
            parameters.canTargetPawns = false;
            parameters.canTargetItems = false;
            parameters.canTargetLocations = false;
            parameters.validator = delegate(TargetInfo target)
            {
                return target.Thing is ThingWithComps;
            };

            Find.Targeter.BeginTargeting(
                parameters,
                delegate(LocalTargetInfo target)
                {
                    if (target.Thing != null)
                    {
                        OpenCandidateMenu(target.Thing);
                    }
                },
                null,
                null,
                null,
                true);
        }

        [DebugAction(
            "CeleTech Shuttle",
            "CE reload probe: stage insufficient",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.Playing)]
        private static void StageInsufficient()
        {
            Run(
                ReloadProbeAction.StageInsufficient,
                "An intentionally insufficient CE ammunition grant is staged.");
        }

        [DebugAction(
            "CeleTech Shuttle",
            "CE reload probe: stage remaining",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.Playing)]
        private static void StageRemaining()
        {
            Run(
                ReloadProbeAction.StageRemaining,
                "The exact remaining CE ammunition count is staged.");
        }

        [DebugAction(
            "CeleTech Shuttle",
            "CE reload probe: commit staged",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.Playing)]
        private static void CommitStaged()
        {
            Run(
                ReloadProbeAction.Commit,
                "The staged CE ammunition count was committed to the magazine.");
        }

        [DebugAction(
            "CeleTech Shuttle",
            "CE reload probe: cancel staged",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.Playing)]
        private static void CancelStaged()
        {
            Run(
                ReloadProbeAction.Cancel,
                "The staged CE ammunition grant was cancelled without changing the magazine.");
        }

        [DebugAction(
            "CeleTech Shuttle",
            "CE reload probe: inject commit rollback",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.Playing)]
        private static void InjectCommitRollback()
        {
            Run(
                ReloadProbeAction.InjectRollback,
                "The injected CE magazine mutation was rolled back; the staged grant remains pending.");
        }

        [DebugAction(
            "CeleTech Shuttle",
            "CE reload probe: commit from shuttle cargo",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.Playing)]
        private static void CommitFromShuttleCargo()
        {
            Run(
                ReloadProbeAction.CommitFromCargo,
                "Matching shuttle cargo was committed to the CE magazine.");
        }

        [DebugAction(
            "CeleTech Shuttle",
            "CE reload probe: inject cargo rollback",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.Playing)]
        private static void InjectCargoRollback()
        {
            Run(
                ReloadProbeAction.InjectCargoRollback,
                "The CE mutation and exact shuttle-cargo withdrawal were both rolled back.");
        }

        private static void OpenCandidateMenu(Thing host)
        {
            List<CeRuntimeProbeCandidate> candidates =
                CeRuntimeProbeCandidateResolver.Resolve();
            if (candidates.Count == 0)
            {
                Notify(
                    "No resolved CE magazine weapon with Verb_ShootCE was found.",
                    MessageTypeDefOf.RejectInput);
                return;
            }

            List<FloatMenuOption> options = new List<FloatMenuOption>(candidates.Count);
            for (int i = 0; i < candidates.Count; i++)
            {
                CeRuntimeProbeCandidate candidate = candidates[i];
                options.Add(new FloatMenuOption(
                    candidate.MenuLabel,
                    delegate { StartSession(host, candidate); }));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static void StartSession(
            Thing host,
            CeRuntimeProbeCandidate candidate)
        {
            CeRuntimeProbeGameComponent component =
                CeRuntimeProbeGameComponent.CurrentComponent;
            if (component == null)
            {
                Notify("The CE runtime probe component is unavailable.", MessageTypeDefOf.RejectInput);
                return;
            }

            string failure;
            if (!component.TryStartReloadProbe(host, candidate, out failure))
            {
                Notify(
                    failure ?? "The CE reload probe did not start.",
                    MessageTypeDefOf.RejectInput);
                return;
            }

            Notify(
                "Temporary external CE candidate started: " + candidate.WeaponDef.LabelCap +
                ". This is not an installed shuttle 6mm/40mm weapon. Put " +
                candidate.InitialAmmo.LabelCap +
                " [" + candidate.InitialAmmo.defName + "] from CE ammo set " +
                candidate.InitialAmmoSet.defName +
                " into the shuttle's ordinary cargo before running the cargo actions.",
                MessageTypeDefOf.NeutralEvent);
        }

        private static void Run(ReloadProbeAction action, string successMessage)
        {
            CeRuntimeProbeGameComponent component =
                CeRuntimeProbeGameComponent.CurrentComponent;
            string failure = null;
            bool succeeded = component != null && Execute(component, action, out failure);
            if (!succeeded)
            {
                Notify(
                    failure ?? "No active CE reload probe is available.",
                    MessageTypeDefOf.RejectInput);
                return;
            }

            Notify(successMessage, MessageTypeDefOf.NeutralEvent);
        }

        private static bool Execute(
            CeRuntimeProbeGameComponent component,
            ReloadProbeAction action,
            out string failure)
        {
            switch (action)
            {
                case ReloadProbeAction.StageInsufficient:
                    return component.StageInsufficientReload(out failure);
                case ReloadProbeAction.StageRemaining:
                    return component.StageRemainingReload(out failure);
                case ReloadProbeAction.Commit:
                    return component.CommitStagedReload(out failure);
                case ReloadProbeAction.Cancel:
                    return component.CancelStagedReload(out failure);
                case ReloadProbeAction.InjectRollback:
                    return component.InjectReloadCommitRollback(out failure);
                case ReloadProbeAction.CommitFromCargo:
                    return component.CommitReloadFromShuttleCargo(out failure);
                case ReloadProbeAction.InjectCargoRollback:
                    return component.InjectShuttleCargoReloadRollback(out failure);
                default:
                    failure = "Unknown CE reload-probe action.";
                    return false;
            }
        }

        private static void Notify(string text, MessageTypeDef messageType)
        {
            Messages.Message(text, messageType, false);
        }
    }
}
