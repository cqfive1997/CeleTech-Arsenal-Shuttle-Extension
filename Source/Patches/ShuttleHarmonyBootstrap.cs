using HarmonyLib;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Patches
{
    [StaticConstructorOnStartup]
    internal static class ShuttleHarmonyBootstrap
    {
        static ShuttleHarmonyBootstrap()
        {
            new Harmony("CeleTech.ShuttleExtension").PatchAll();
        }
    }
}
