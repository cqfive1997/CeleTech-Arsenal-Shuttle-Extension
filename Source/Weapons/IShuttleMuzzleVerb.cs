using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Weapons
{
    public interface IShuttleMuzzleVerb
    {
        void SetShuttleMuzzleSource(IntVec3 sourceCell, Vector3 sourceDrawPos);

        void SetShuttleFireControlTuning(
            float directFireAccuracyMultiplier,
            float directFireAccuracyBonus,
            float directFireAccuracyFloor,
            float forcedMissRadiusMultiplier);
    }
}
