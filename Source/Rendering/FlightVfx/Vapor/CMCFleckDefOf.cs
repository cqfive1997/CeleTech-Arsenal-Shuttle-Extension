using RimWorld;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx.Vapor
{
    [DefOf]
    internal static class CMCFleckDefOf
    {
#pragma warning disable 0649
        public static FleckDef CMC_ShuttleCondensationPuff;
#pragma warning restore 0649

        static CMCFleckDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(CMCFleckDefOf));
        }
    }
}
