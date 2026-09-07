using System.Collections.Generic;
using CeleTech.ShuttleExtension.ModularShuttle.Cargo;
using CeleTech.ShuttleExtension.ModularShuttle.Comps;
using CeleTech.ShuttleExtension.ModularShuttle.Rendering;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.AssemblyConstruction;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Hull;
using CeleTech.ShuttleExtension.ModularShuttle.Runtime.Modules.Shield.Surface;
using CeleTech.ShuttleExtension.ModularShuttle.UI;
using CeleTech.ShuttleExtension.ModularShuttle.Utilities;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.Core
{
    /// <summary>
    /// Building_ModularShuttle is the payload of the the modular shuttle.
    /// It does not have any functions, all functions are provided by inserted modules.
    /// Besides, it is also the interface for the player to interact with the shuttle.
    ///
    /// Rename behavior follows the usual RimWorld IRenameable pattern:
    /// - BaseLabel is the fallback label from the ThingDef
    /// - RenamableLabel is the user-facing current name
    /// - when no custom name is stored, RenamableLabel falls back to BaseLabel
    /// - InspectLabel and Label both show the current visible name
    /// </summary>
    [StaticConstructorOnStartup]
    public class Building_ModularShuttle : Building, IRenameable, IHaulEnroute
    {
        private static Texture2D renameGizmoIcon;
        private static Texture2D openControlGizmoIcon;
        private static readonly ShuttleQuickActionGizmoProvider QuickActionGizmoProvider =
            new ShuttleQuickActionGizmoProvider();
        private string shuttleName;

        public int SpaceRemainingFor(ThingDef stuff)
        {
            int constructionDemand =
                ShuttleAssemblyConstructionEnrouteUtility.SpaceRemainingFor(this, stuff);
            int transporterDemand =
                ShuttleTransporterEnrouteCapacityReader.GetPendingCountFor(this, stuff);
            if (constructionDemand >= int.MaxValue - transporterDemand)
            {
                return int.MaxValue;
            }

            return constructionDemand + transporterDemand;
        }

        private static Texture2D RenameGizmoIcon
        {
            get
            {
                if (renameGizmoIcon == null)
                {
                    renameGizmoIcon = ContentFinder<Texture2D>.Get(
                        "Icon/Console/Rename",
                        false);
                }

                return renameGizmoIcon;
            }
        }

        private static Texture2D OpenControlGizmoIcon
        {
            get
            {
                if (openControlGizmoIcon == null)
                {
                    openControlGizmoIcon = ContentFinder<Texture2D>.Get(
                        "Icon/Console/UIWeaker",
                        false);
                }

                return openControlGizmoIcon;
            }
        }

        public string RenamableLabel
		{
			get
			{
                // The shuttle uses its saved custom name when present.
                // Otherwise it falls back to the default ThingDef label.
				return this.shuttleName ?? this.BaseLabel;
			}
			set
			{
                // A null or whitespace custom name should mean "use the default label" again.
				this.shuttleName = string.IsNullOrWhiteSpace(value)
                    ? null
                    : value.Trim();
			}
		}

        public string BaseLabel
		{
			get
			{
				return this.def.LabelCap;
			}
		}

        public string InspectLabel
		{
			get
			{
				return this.RenamableLabel;
			}
		}

        public override string Label
		{
			get
			{
				return this.RenamableLabel;
			}
		}

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            ShuttleVisualOverlayDrawer.DrawThingGroundShadow(this, drawLoc, 1f);
            base.DrawAt(drawLoc, flip);
            ShuttleVisualOverlayDrawer.DrawThingLightOverlay(this, drawLoc);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            if (ShuttleCriticalDamagePolicy.ShouldRestrictPlayerInteractions(this))
            {
                yield break;
            }

            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }

            yield return new Command_Action
            {
                defaultLabel = "CT_Shuttle_GizmoOpenLabel".Translate().ToString(),
                defaultDesc = "CT_Shuttle_GizmoOpenDesc".Translate().ToString(),
                icon = OpenControlGizmoIcon,
                action = delegate
                {
                    this.OpenControlUI();
                }
            };

            CompModularShuttleCore coreComp = this.GetComp<CompModularShuttleCore>();
            Gizmo fireControlGizmo = coreComp != null
                ? coreComp.GetFireControlGizmo()
                : null;
            if (fireControlGizmo != null)
            {
                yield return fireControlGizmo;
            }

            foreach (Gizmo gizmo in QuickActionGizmoProvider.GetGizmos(
                coreComp != null ? (System.Action)coreComp.OpenQuickLoadCargoWindow : null,
                coreComp != null ? (System.Action)coreComp.OpenQuickCargoUnloadWindow : null,
                coreComp != null ? (System.Action)coreComp.OpenQuickCrewUnloadDialog : null,
                coreComp != null ? (System.Action)coreComp.OpenQuickLaunchFlow : null))
            {
                yield return gizmo;
            }

            // Rename stays last because it is an infrequent building-level utility.
            yield return new Command_Action
            {
                defaultLabel = "CT_Shuttle_GizmoRenameLabel".Translate().ToString(),
                defaultDesc = "CT_Shuttle_GizmoRenameDesc".Translate().ToString(),
                icon = RenameGizmoIcon,
                action = delegate
                {
                    this.OpenRenameDialog();
                }
            };
        }

        public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            base.PreApplyDamage(ref dinfo, out absorbed);
            if (absorbed)
            {
                return;
            }

            CompModularShuttleCore core = this.GetComp<CompModularShuttleCore>();
            IShuttleSurfaceShieldPort surfaceShieldPort = core as IShuttleSurfaceShieldPort;
            if (surfaceShieldPort != null)
            {
                ShuttleSurfaceShieldAbsorbResult result;
                if (surfaceShieldPort.TryAbsorbIncomingDamage(ref dinfo, out result) &&
                    result != null &&
                    result.Handled)
                {
                    CompModularShuttleSurfaceShieldVisual visual =
                        this.GetComp<CompModularShuttleSurfaceShieldVisual>();
                    if (visual != null)
                    {
                        visual.NotifyShieldHit(result);
                    }

                    if (result.FullyAbsorbed)
                    {
                        absorbed = true;
                        return;
                    }

                    // Partial absorption already reduced dinfo through the port service.
                    // Keep absorbed false so hull and then normal building damage continue.
                }
            }

            IShuttleHullIntegrityPort hullPort = core as IShuttleHullIntegrityPort;
            if (hullPort == null)
            {
                return;
            }

            ShuttleHullDamageResult hullResult;
            if (hullPort.TryApplyIncomingHullDamage(ref dinfo, out hullResult) &&
                hullResult != null &&
                hullResult.Handled &&
                hullResult.FullyAbsorbed)
            {
                absorbed = true;
                return;
            }

            // Partial hull absorption leaves dinfo.Amount as the remaining damage.
            // Keep absorbed false so vanilla building HP receives that remainder.
        }

        public override void PostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.PostApplyDamage(dinfo, totalDamageDealt);
            if (ShuttleCriticalDamagePolicy.ClampToCriticalDamageFloor(this))
            {
                this.NotifyCriticalDamageChanged();
            }
        }

        public override void Kill(DamageInfo? dinfo = null, Hediff exactCulprit = null)
        {
            if (ShuttleCriticalDamagePolicy.TryConvertKillToCriticalDamage(this))
            {
                this.NotifyCriticalDamageChanged();
                return;
            }

            base.Kill(dinfo, exactCulprit);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if ((mode == DestroyMode.Deconstruct || mode == DestroyMode.KillFinalize) &&
                this.Spawned &&
                this.Map != null)
            {
                string failureReason;
                if (!ShuttleDestructionRecoveryService.TryRecoverBeforeDestruction(
                    this,
                    this.Map,
                    out failureReason))
                {
                    Log.Error("[CeleTech Shuttle] Shuttle destruction was aborted because held contents could not be recovered. " +
                        "mode=" + mode + " reason=" + (failureReason ?? "unknown"));
                    Messages.Message(
                        "CT_Shuttle_DestroyRecoveryFailed".Translate(),
                        this,
                        MessageTypeDefOf.RejectInput,
                        true);
                    return;
                }
            }

            base.Destroy(mode);
        }

        public override string GetInspectString()
        {
            string inspectString = base.GetInspectString();
            string criticalDamageLine = ShuttleCriticalDamagePolicy.GetInspectString(this);
            if (string.IsNullOrEmpty(criticalDamageLine))
            {
                return inspectString;
            }

            return string.IsNullOrEmpty(inspectString)
                ? criticalDamageLine
                : inspectString + "\n" + criticalDamageLine;
        }

        public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look<string>(ref this.shuttleName, "shuttleName", null, false);
		}

        protected virtual void OpenRenameDialog()
        {
            if (ShuttleCriticalDamagePolicy.TryRejectRestrictedInteraction(this))
            {
                return;
            }

            // Keep rename UI local to the host object.
            // This avoids coupling the assembly/profile controller to simple name editing.
            Find.WindowStack.Add(new Dialog_RenameModularShuttle(this));
        }

        protected virtual void OpenControlUI()
        {
            if (ShuttleCriticalDamagePolicy.TryRejectRestrictedInteraction(this))
            {
                return;
            }

            // Route control UI activation through the core comp when present.
            // The building remains the host entry point, while the comp/controller own the deeper seam.
            CompModularShuttleCore coreComp = this.GetComp<CompModularShuttleCore>();
            if (coreComp != null)
            {
                coreComp.OpenControlUI();
                return;
            }

            Messages.Message("CT_Shuttle_Error_CoreUnavailable".Translate(), this, MessageTypeDefOf.RejectInput);
        }

        private void NotifyCriticalDamageChanged()
        {
            ShuttleCriticalDamagePolicy.ClearBlockedDesignations(this);
            CompModularShuttleCore coreComp = this.GetComp<CompModularShuttleCore>();
            if (coreComp != null)
            {
                coreComp.CloseOpenControlDialogForCriticalDamage();
            }
        }
    }
}
