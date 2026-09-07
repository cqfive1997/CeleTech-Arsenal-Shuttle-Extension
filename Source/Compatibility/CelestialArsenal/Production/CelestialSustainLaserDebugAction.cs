using System.Text;
using CeleTech.ShuttleExtension.ModularShuttle.Defs;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Weapon;
using LudeonTK;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CelestialArsenal
{
    /// <summary>
    /// Definition-only readiness report for the standalone HPJ-L01 clone.
    /// </summary>
    internal static class CelestialSustainLaserDebugAction
    {
        [DebugAction(
            "CeleTech Shuttle",
            "Particle lance: report standalone definition",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.Playing)]
        private static void ReportStandaloneDefinition()
        {
            StringBuilder output = new StringBuilder();
            int matchingDefCount;
            ShuttleWeaponModuleDef moduleDef = FindMatchingModuleDef(out matchingDefCount);
            output.Append("[CeleTech Shuttle][Particle Lance] registration=")
                .Append(CelestialWeaponBackendRegistrar.RegistrationSucceeded)
                .Append(" shapeMatches=").Append(matchingDefCount)
                .Append(" moduleLoaded=").Append(moduleDef != null);

            bool ready = false;
            if (moduleDef != null)
            {
                ShuttleWeaponBackendProbeContext context =
                    new ShuttleWeaponBackendProbeContext(
                        moduleDef,
                        null,
                        ShuttleWeaponRuntimeState.CoreMagazineAuthorityId,
                        true);
                ShuttleWeaponCompatibilityReport report =
                    CelestialWeaponBackendRegistrar.Factory.Probe(context);
                ShuttleWeaponBackendBinding binding;
                ShuttleWeaponCompatibilityReport selectedReport;
                bool resolved = ShuttleWeaponBackendRegistry.Shared.TryResolve(
                    context,
                    out binding,
                    out selectedReport);
                ThingWithComps gun = moduleDef.weaponDef != null
                    ? ThingMaker.MakeThing(moduleDef.weaponDef) as ThingWithComps
                    : null;
                CompEquippable equippable = gun != null
                    ? gun.GetComp<CompEquippable>()
                    : null;
                CompShuttleSustainLaserData data = gun != null
                    ? gun.GetComp<CompShuttleSustainLaserData>()
                    : null;
                ready = report != null && report.IsSupported && resolved &&
                    binding != null &&
                    binding.BackendId == CelestialSustainLaserBackendFactory.Id &&
                    equippable != null &&
                    equippable.PrimaryVerb is Verb_ShuttleCelestialSustainLaser &&
                    data != null;

                output.Append(" report=")
                    .Append(report != null
                        ? report.Support + ":" + report.ReasonCode
                        : "none")
                    .Append(" resolved=").Append(resolved)
                    .Append(" selected=")
                    .Append(binding != null ? binding.BackendId : "none")
                    .Append(" power=idle/firing=")
                    .Append(moduleDef.idlePowerDrawWatts).Append("/")
                    .Append(moduleDef.firingPowerDrawWatts)
                    .Append(" impactChance=")
                    .Append(data != null && data.Props != null
                        ? data.Props.ImpactFleckChancePerDamagePulse
                        : 0f);
            }

            output.Append(" ready=").Append(ready);
            Log.Message(output.ToString());
            Messages.Message(
                "Particle-lance standalone definition report written to the log.",
                RimWorld.MessageTypeDefOf.NeutralEvent,
                false);
        }

        private static ShuttleWeaponModuleDef FindMatchingModuleDef(out int matchingDefCount)
        {
            matchingDefCount = 0;
            ShuttleWeaponModuleDef firstMatch = null;
            System.Collections.Generic.List<ShuttleWeaponModuleDef> defs =
                DefDatabase<ShuttleWeaponModuleDef>.AllDefsListForReading;
            for (int i = 0; defs != null && i < defs.Count; i++)
            {
                ShuttleWeaponModuleDef candidate = defs[i];
                if (!CelestialSustainLaserCompatibilityEvaluator.MatchesExactDefinitionShape(
                    candidate))
                {
                    continue;
                }

                matchingDefCount++;
                if (firstMatch == null)
                {
                    firstMatch = candidate;
                }
            }

            return firstMatch;
        }
    }
}
