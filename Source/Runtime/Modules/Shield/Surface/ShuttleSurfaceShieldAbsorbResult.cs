using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Shield.Surface
{
    /// <summary>
    /// Detached result for one surface shield damage request. Phase 4 will use this to decide
    /// whether the host hook should fully absorb or continue normal hull damage with a reduced
    /// DamageInfo amount.
    /// </summary>
    internal sealed class ShuttleSurfaceShieldAbsorbResult
    {
        public bool Handled;
        public bool FullyAbsorbed;
        public bool PartiallyAbsorbed;
        public bool BrokeShield;
        public int ShieldDamageApplied;
        public float IncomingDamageAmount;
        public float RemainingDamageAmount;
        public Vector3 HitWorldPosition;
        public string ModuleInstanceID;
        public string ModuleDefName;
        public string DamageCategory;
        public string StatusKey;
    }
}
