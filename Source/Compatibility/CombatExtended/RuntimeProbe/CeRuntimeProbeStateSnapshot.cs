using CombatExtended;
using Verse;

namespace CeleTech.ShuttleExtension.Compatibility.CombatExtended.RuntimeProbe
{
    public sealed class CeRuntimeProbeStateSnapshot : IExposable
    {
        private int loadedCount;
        private string selectedAmmo;
        private string fireMode;
        private string aimMode;
        private string verbState;
        private bool verbBursting;

        public CeRuntimeProbeStateSnapshot()
        {
        }

        internal void Capture(ThingWithComps gun)
        {
            CompAmmoUser ammo = CeRuntimeProbeGunAccess.GetAmmo(gun);
            CompFireModes modes = CeRuntimeProbeGunAccess.GetModes(gun);
            this.loadedCount = ammo != null ? ammo.CurMagCount : 0;
            this.selectedAmmo = DefName(ammo != null ? ammo.SelectedAmmo : null);
            this.fireMode = EnumName(modes != null ? (object)modes.CurrentFireMode : null);
            this.aimMode = EnumName(modes != null ? (object)modes.CurrentAimMode : null);
            Verb_ShootCE verb = CeRuntimeProbeGunAccess.GetVerb(gun);
            this.verbState = EnumName(verb != null ? (object)verb.state : null);
            this.verbBursting = verb != null && verb.Bursting;
        }

        internal bool Matches(ThingWithComps gun)
        {
            CompAmmoUser ammo = CeRuntimeProbeGunAccess.GetAmmo(gun);
            CompFireModes modes = CeRuntimeProbeGunAccess.GetModes(gun);
            Verb_ShootCE verb = CeRuntimeProbeGunAccess.GetVerb(gun);
            return ammo != null &&
                ammo.CurMagCount == this.loadedCount &&
                DefName(ammo.SelectedAmmo) == this.selectedAmmo &&
                EnumName(modes != null ? (object)modes.CurrentFireMode : null) == this.fireMode &&
                EnumName(modes != null ? (object)modes.CurrentAimMode : null) == this.aimMode &&
                EnumName(verb != null ? (object)verb.state : null) == this.verbState &&
                (verb != null && verb.Bursting) == this.verbBursting;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref this.loadedCount, "loadedCount", 0);
            Scribe_Values.Look(ref this.selectedAmmo, "selectedAmmo");
            Scribe_Values.Look(ref this.fireMode, "fireMode");
            Scribe_Values.Look(ref this.aimMode, "aimMode");
            Scribe_Values.Look(ref this.verbState, "verbState");
            Scribe_Values.Look(ref this.verbBursting, "verbBursting", false);
        }

        private static string DefName(Def def)
        {
            return def != null ? def.defName : null;
        }

        private static string EnumName(object value)
        {
            return value != null ? value.ToString() : null;
        }
    }
}
