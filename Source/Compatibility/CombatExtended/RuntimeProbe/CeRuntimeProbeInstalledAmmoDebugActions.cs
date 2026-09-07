using System.Collections.Generic;
using System.Text;
using CombatExtended;
using LudeonTK;
using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended.RuntimeProbe
{
    internal static class CeRuntimeProbeInstalledAmmoDebugActions
    {
        private const string SixMillimeterAmmoDefName = "CT_ShuttleAmmo_6mmAP";
        private const string SixMillimeterAmmoSetDefName = "CT_ShuttleAmmoSet_PointDefense";
        private const string FortyMillimeterAmmoDefName = "CT_ShuttleAmmo_40mmAPHE";
        private const string FortyMillimeterAmmoSetDefName = "CT_ShuttleAmmoSet_40mmCIWS";

        [DebugAction(
            "CeleTech Shuttle",
            "CE installed 6mm/40mm ammo: report",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.Playing)]
        private static void ReportInstalledAmmo()
        {
            StringBuilder report = new StringBuilder();
            report.AppendLine("[CeleTech Shuttle][CE Installed Ammo] begin");
            AppendAmmoReport(
                report,
                SixMillimeterAmmoDefName,
                SixMillimeterAmmoSetDefName);
            AppendAmmoReport(
                report,
                FortyMillimeterAmmoDefName,
                FortyMillimeterAmmoSetDefName);
            report.Append("[CeleTech Shuttle][CE Installed Ammo] end");

            Log.Message(report.ToString());
            Messages.Message(
                "Installed shuttle 6mm/40mm CE ammunition report written to the log.",
                MessageTypeDefOf.NeutralEvent,
                false);
        }

        private static void AppendAmmoReport(
            StringBuilder report,
            string ammoDefName,
            string ammoSetDefName)
        {
            ThingDef thingDef = DefDatabase<ThingDef>.GetNamedSilentFail(ammoDefName);
            AmmoDef ammoDef = DefDatabase<AmmoDef>.GetNamedSilentFail(ammoDefName);
            AmmoSetDef ammoSet = DefDatabase<AmmoSetDef>.GetNamedSilentFail(ammoSetDefName);
            AmmoLink link = FindAmmoLink(ammoSet, ammoDef);
            AmmoInstanceCounts instances = CountMapInstances(thingDef);

            report.Append("[CeleTech Shuttle][CE Installed Ammo] ammo=")
                .Append(ammoDefName)
                .Append(" thingDefResolved=").Append(thingDef != null)
                .Append(" ammoDefResolved=").Append(ammoDef != null)
                .Append(" defRuntimeType=").Append(TypeName(thingDef == null ? null : thingDef.GetType()))
                .Append(" newThingClass=").Append(TypeName(thingDef == null ? null : thingDef.thingClass))
                .Append(" ammoSet=").Append(ammoSet == null ? "null" : ammoSet.defName)
                .Append(" projectile=").Append(link == null || link.projectile == null ? "null" : link.projectile.defName)
                .Append(" ammoClass=").Append(ammoDef == null || ammoDef.ammoClass == null ? "null" : ammoDef.ammoClass.defName)
                .Append(" menuHidden=").Append(ammoDef != null && ammoDef.menuHidden)
                .Append(" destroyOnDrop=").Append(thingDef != null && thingDef.destroyOnDrop)
                .Append(" mapStacks=").Append(instances.TotalStacks)
                .Append(" mapRounds=").Append(instances.TotalRounds)
                .Append(" ammoThingStacks=").Append(instances.AmmoThingStacks)
                .Append(" legacyThingStacks=").Append(instances.LegacyThingStacks)
                .AppendLine();
        }

        private static AmmoLink FindAmmoLink(AmmoSetDef ammoSet, AmmoDef ammoDef)
        {
            if (ammoSet == null || ammoDef == null || ammoSet.ammoTypes == null)
            {
                return null;
            }

            for (int i = 0; i < ammoSet.ammoTypes.Count; i++)
            {
                AmmoLink link = ammoSet.ammoTypes[i];
                if (link != null && link.ammo == ammoDef)
                {
                    return link;
                }
            }

            return null;
        }

        private static AmmoInstanceCounts CountMapInstances(ThingDef thingDef)
        {
            AmmoInstanceCounts counts = new AmmoInstanceCounts();
            if (thingDef == null || Current.Game == null)
            {
                return counts;
            }

            List<Map> maps = Find.Maps;
            for (int mapIndex = 0; mapIndex < maps.Count; mapIndex++)
            {
                List<Thing> things = maps[mapIndex].listerThings.ThingsOfDef(thingDef);
                for (int thingIndex = 0; thingIndex < things.Count; thingIndex++)
                {
                    Thing thing = things[thingIndex];
                    if (thing == null || thing.Destroyed)
                    {
                        continue;
                    }

                    counts.TotalStacks++;
                    counts.TotalRounds += thing.stackCount;
                    if (thing is AmmoThing)
                    {
                        counts.AmmoThingStacks++;
                    }
                    else
                    {
                        counts.LegacyThingStacks++;
                    }
                }
            }

            return counts;
        }

        private static string TypeName(System.Type type)
        {
            return type == null ? "null" : type.FullName;
        }

        private struct AmmoInstanceCounts
        {
            public int TotalStacks;
            public int TotalRounds;
            public int AmmoThingStacks;
            public int LegacyThingStacks;
        }
    }
}
