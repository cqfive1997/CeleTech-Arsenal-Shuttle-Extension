using System;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo.Refrigerated;
using HarmonyLib;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Patches
{
    [HarmonyPatch(typeof(ThingOwnerUtility), nameof(ThingOwnerUtility.TryGetFixedTemperature))]
    internal static class ThingOwnerUtility_TryGetFixedTemperature_Patch
    {
        private static bool Prefix(
            IThingHolder holder,
            Thing forThing,
            ref float temperature,
            ref bool __result)
        {
            try
            {
                IShuttleFixedTemperatureHolder fixedHolder = holder as IShuttleFixedTemperatureHolder;
                if (fixedHolder == null)
                {
                    return true;
                }

                float fixedTemperature;
                if (!fixedHolder.TryGetFixedTemperature(out fixedTemperature))
                {
                    return true;
                }

                temperature = fixedTemperature;
                __result = true;
                return false;
            }
            catch (Exception exception)
            {
                Log.ErrorOnce(
                    "[CeleTech Shuttle] Refrigerated cargo fixed-temperature patch failed: " + exception,
                    53142001);
                return true;
            }
        }
    }
}
