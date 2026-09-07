using System.Text;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Core;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using LudeonTK;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CelestialArsenal
{
    /// <summary>
    /// Read-only selected-shuttle report for the live HPJ-L01 acceptance matrix.
    /// </summary>
    internal static class CelestialSustainLaserRuntimeDebugAction
    {
        [DebugAction(
            "CeleTech Shuttle",
            "Particle lance: report selected shuttle runtime",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.Playing)]
        private static void ReportSelectedShuttleRuntime()
        {
            StringBuilder output = new StringBuilder();
            output.AppendLine("[CeleTech Shuttle][Particle Lance] selected runtime");
            ThingWithComps host = Find.Selector.SingleSelectedThing as ThingWithComps;
            CompModularShuttleCore core = host != null
                ? host.GetComp<CompModularShuttleCore>()
                : null;
            if (core == null || core.Controller == null ||
                core.Controller.AssemblyState == null)
            {
                output.Append("selectedShuttle=false");
                Write(output);
                return;
            }

            int inspected = 0;
            int ready = 0;
            ShuttleAssemblyState assembly = core.Controller.AssemblyState;
            for (int i = 0; assembly.Modules != null && i < assembly.Modules.Count; i++)
            {
                ShuttleModule module = assembly.Modules[i];
                ShuttleWeaponModuleDef moduleDef = module != null
                    ? module.ModuleDef as ShuttleWeaponModuleDef
                    : null;
                if (moduleDef == null ||
                    !CelestialSustainLaserCompatibilityEvaluator.MatchesExactDefinitionShape(
                        moduleDef))
                {
                    continue;
                }

                inspected++;
                ShuttleWeaponRuntimeState state =
                    core.Controller.TryGetWeaponRuntimeState(module);
                ShuttleWeaponBackendBinding binding = null;
                if (state != null)
                {
                    state.TryGetBackendBindingForRuntimeOnly(moduleDef, out binding);
                }

                ThingWithComps gun = state != null
                    ? state.GunForRuntimeOnly as ThingWithComps
                    : null;
                CompEquippable equippable = gun != null
                    ? gun.GetComp<CompEquippable>()
                    : null;
                Verb_ShuttleCelestialSustainLaser verb = equippable != null
                    ? equippable.PrimaryVerb as Verb_ShuttleCelestialSustainLaser
                    : null;
                CompShuttleSustainLaserData data = gun != null
                    ? gun.GetComp<CompShuttleSustainLaserData>()
                    : null;
                bool invariant = state != null && binding != null &&
                    binding.BackendId == CelestialSustainLaserBackendFactory.Id &&
                    gun != null && gun.def == moduleDef.weaponDef && verb != null &&
                    data != null;
                if (invariant)
                {
                    ready++;
                }

                output.Append("module=").Append(module.ModuleInstanceID)
                    .Append(" cached=")
                    .Append(binding != null ? binding.BackendId : "none")
                    .Append(" gun=")
                    .Append(gun != null && gun.def != null ? gun.def.defName : "none")
                    .Append(" verbState=")
                    .Append(verb != null ? verb.state.ToString() : "none")
                    .Append(" target=")
                    .Append(state != null
                        ? state.GetCurrentTargetForRuntimeOnly().ToString()
                        : "none")
                    .Append(" warmup/cooldown/active=")
                    .Append(state != null ? state.GetWarmupTicksForRuntimeOnly() : 0)
                    .Append("/")
                    .Append(state != null ? state.GetCooldownTicksForRuntimeOnly() : 0)
                    .Append("/")
                    .Append(state != null ? state.GetActivePowerTicksForRuntimeOnly() : 0)
                    .Append(" muzzleCell=")
                    .Append(state != null
                        ? state.GetLastResolvedMuzzleCellForRuntimeOnly().ToString()
                        : "none")
                    .Append(" muzzleDraw=")
                    .Append(state != null
                        ? state.GetLastResolvedMuzzleDrawPosForRuntimeOnly().ToString()
                        : "none")
                    .Append(" pulse=left/total/next/last/interval=")
                    .Append(verb != null ? verb.BurstShotsLeftForDiagnostics : -1)
                    .Append("/")
                    .Append(verb != null ? verb.BurstShotCount : -1)
                    .Append("/")
                    .Append(verb != null ? verb.TicksToNextPulseForDiagnostics : -1)
                    .Append("/")
                    .Append(verb != null ? verb.LastPulseTickForDiagnostics : -1)
                    .Append("/")
                    .Append(verb != null && verb.verbProps != null
                        ? verb.verbProps.ticksBetweenBurstShots
                        : -1)
                    .Append(" beamActive=")
                    .Append(verb != null && verb.BeamActiveForDiagnostics)
                    .Append(" power=idle/firing=")
                    .Append(moduleDef.idlePowerDrawWatts).Append("/")
                    .Append(moduleDef.firingPowerDrawWatts)
                    .Append(" impactChance=")
                    .Append(data != null && data.Props != null
                        ? data.Props.ImpactFleckChancePerDamagePulse
                        : 0f)
                    .Append(" invariant=").Append(invariant)
                    .AppendLine();
            }

            output.Append("summary inspected=").Append(inspected)
                .Append(" ready=").Append(ready)
                .Append(" allReady=").Append(inspected > 0 && ready == inspected);
            Write(output);
        }

        private static void Write(StringBuilder output)
        {
            Log.Message(output.ToString());
            Messages.Message(
                "Particle-lance selected-shuttle runtime report written to the log.",
                RimWorld.MessageTypeDefOf.NeutralEvent,
                false);
        }
    }
}
