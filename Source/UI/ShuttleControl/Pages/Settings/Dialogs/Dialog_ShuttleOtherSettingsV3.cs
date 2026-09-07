using System;
using System.Globalization;
using CeleTech.ShuttleExtension.ModularShuttle.Onboarding;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared.Dialogs;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Settings.Dialogs
{
    internal sealed class Dialog_ShuttleOtherSettingsV3 : Window, IShuttleSettingsHubSection
    {
        private readonly Action onSettingsChanged;
        private string payloadCapacityMultiplierBuffer;
        private float payloadCapacityMultiplier;
        private bool payloadCapacityMultiplierValid;
        private string constructionWorkMultiplierBuffer;
        private float constructionWorkMultiplier;
        private bool constructionWorkMultiplierValid;
        private bool shareRefrigeratedCargoMass;
        private string refrigeratedCargoCapacityRatioBuffer;
        private float independentRefrigeratedCargoCapacityRatio;
        private bool refrigeratedCargoCapacityRatioValid;
        private bool allowColonistUseAtBase;
        private bool allowMechUseAtBase;
        private bool useCompleteBasicShuttlePreset;
        private bool showPerformanceDashboard;
        private float appliedPayloadCapacityMultiplier;
        private float appliedConstructionWorkMultiplier;
        private bool appliedShareRefrigeratedCargoMass;
        private float appliedIndependentRefrigeratedCargoCapacityRatio;
        private bool appliedAllowColonistUseAtBase;
        private bool appliedAllowMechUseAtBase;
        private bool appliedUseCompleteBasicShuttlePreset;
        private bool appliedShowPerformanceDashboard;
        private Vector2 settingsScroll;

        internal Dialog_ShuttleOtherSettingsV3(Action onSettingsChanged)
        {
            this.onSettingsChanged = onSettingsChanged;
            ShuttleOtherSettings settings = CeleTechShuttleMod.Settings.Other ??
                new ShuttleOtherSettings();
            this.payloadCapacityMultiplier = settings.PayloadCapacityMultiplier;
            this.payloadCapacityMultiplierBuffer = this.payloadCapacityMultiplier.ToString(
                "0.0",
                CultureInfo.InvariantCulture);
            this.payloadCapacityMultiplierValid = true;
            this.constructionWorkMultiplier = settings.ConstructionWorkMultiplier;
            this.constructionWorkMultiplierBuffer =
                this.constructionWorkMultiplier.ToString(
                    "0.0",
                    CultureInfo.InvariantCulture);
            this.constructionWorkMultiplierValid = true;
            this.shareRefrigeratedCargoMass =
                settings.ShareRefrigeratedCargoMassWithOverallCapacity;
            this.independentRefrigeratedCargoCapacityRatio =
                settings.IndependentRefrigeratedCargoCapacityRatio;
            this.refrigeratedCargoCapacityRatioBuffer =
                this.independentRefrigeratedCargoCapacityRatio.ToString(
                    "0.00",
                    CultureInfo.InvariantCulture);
            this.refrigeratedCargoCapacityRatioValid = true;
            this.allowColonistUseAtBase =
                settings.AllowColonistOnboardDeviceUseAtPlayerHome;
            this.allowMechUseAtBase =
                settings.AllowMechOnboardDeviceUseAtPlayerHome;
            this.showPerformanceDashboard =
                CeleTechShuttleMod.Settings.ShowPerformanceDashboard;
            ShuttleStarterPresetGameComponent component =
                ShuttleStarterPresetGameComponent.CurrentComponent;
            this.useCompleteBasicShuttlePreset = component != null &&
                component.SelectionResolved &&
                component.SelectedMode == ShuttleStarterPresetMode.BasicFlightAndCargo;
            this.CaptureAppliedValues();
            this.doCloseX = true;
            this.absorbInputAroundWindow = true;
            this.forcePause = false;
            this.draggable = true;
            this.resizeable = false;
        }

        public override Vector2 InitialSize
        {
            get { return new Vector2(820f, 640f); }
        }

        public ShuttleSettingsHubCommitMode CommitMode
        {
            get { return ShuttleSettingsHubCommitMode.Explicit; }
        }

        public bool CanApply
        {
            get
            {
                return this.payloadCapacityMultiplierValid &&
                    this.constructionWorkMultiplierValid &&
                    (this.shareRefrigeratedCargoMass ||
                        this.refrigeratedCargoCapacityRatioValid);
            }
        }

        public bool HasPendingChanges
        {
            get
            {
                return Math.Abs(
                        this.payloadCapacityMultiplier -
                        this.appliedPayloadCapacityMultiplier) > 0.0001f ||
                    Math.Abs(
                        this.constructionWorkMultiplier -
                        this.appliedConstructionWorkMultiplier) > 0.0001f ||
                    this.shareRefrigeratedCargoMass !=
                        this.appliedShareRefrigeratedCargoMass ||
                    Math.Abs(
                        this.independentRefrigeratedCargoCapacityRatio -
                        this.appliedIndependentRefrigeratedCargoCapacityRatio) > 0.0001f ||
                    this.allowColonistUseAtBase != this.appliedAllowColonistUseAtBase ||
                    this.allowMechUseAtBase != this.appliedAllowMechUseAtBase ||
                    this.useCompleteBasicShuttlePreset !=
                        this.appliedUseCompleteBasicShuttlePreset ||
                    this.showPerformanceDashboard !=
                        this.appliedShowPerformanceDashboard;
            }
        }

        public string ApplyDisabledReason
        {
            get
            {
                if (!this.payloadCapacityMultiplierValid)
                {
                    return "CT_Shuttle_OtherSettings_InvalidPayloadCapacityMultiplier"
                        .Translate()
                        .ToString();
                }

                if (!this.shareRefrigeratedCargoMass &&
                    !this.refrigeratedCargoCapacityRatioValid)
                {
                    return "CT_Shuttle_OtherSettings_InvalidRefrigeratedCapacityRatio"
                        .Translate()
                        .ToString();
                }

                return this.constructionWorkMultiplierValid
                    ? null
                    : "CT_Shuttle_OtherSettings_InvalidConstructionWorkMultiplier"
                        .Translate()
                        .ToString();
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            ShuttleV3DialogLayout.DrawPanelBackground(inRect);
            Rect headerRect = new Rect(inRect.x, inRect.y, inRect.width, 70f);
            Rect bottomRect = new Rect(inRect.x, inRect.yMax - 48f, inRect.width, 48f);
            Rect bodyRect = new Rect(
                inRect.x,
                headerRect.yMax + ShuttleV3DialogStyle.Gap,
                inRect.width,
                bottomRect.y - headerRect.yMax - (ShuttleV3DialogStyle.Gap * 2f));

            this.DrawHeader(headerRect);
            this.DrawBody(bodyRect);
            this.DrawBottomBar(bottomRect);
        }

        public void Draw(Rect rect)
        {
            this.DrawSettingsPanel(rect);
        }

        public void Reset()
        {
            this.ResetWorkingCopy();
        }

        public bool Apply()
        {
            return this.ApplyWorkingCopy();
        }

        public void OnHubClosed()
        {
        }

        private void DrawHeader(Rect rect)
        {
            GameFont oldFont = Text.Font;
            Color oldColor = GUI.color;
            try
            {
                Text.Font = GameFont.Medium;
                GUI.color = ShuttleV3DialogStyle.HeaderTitleTextColor;
                ShuttleV3DialogLayout.SafeLabel(
                    new Rect(rect.x + 12f, rect.y + 6f, rect.width - 24f, 30f),
                    "CT_Shuttle_OtherSettings_Title".Translate().ToString());
                Text.Font = GameFont.Tiny;
                GUI.color = ShuttleV3DialogStyle.MutedTextColor;
                ShuttleV3DialogLayout.SafeLabel(
                    new Rect(rect.x + 12f, rect.y + 38f, rect.width - 24f, 24f),
                    "CT_Shuttle_OtherSettings_Subtitle".Translate().ToString());
            }
            finally
            {
                Text.Font = oldFont;
                GUI.color = oldColor;
            }
        }

        private void DrawBody(Rect rect)
        {
            this.DrawSettingsPanel(rect);
        }

        private void DrawSettingsPanel(Rect rect)
        {
            ShuttleV3DialogLayout.DrawPanelBackground(rect);
            ShuttleV3DialogLayout.DrawSectionHeader(
                rect,
                "CT_Shuttle_OtherSettings_GameplaySection".Translate().ToString());
            Rect inner = new Rect(rect.x + 10f, rect.y + 42f, rect.width - 20f, rect.height - 52f);
            const float contentHeight = 612f;
            Rect viewRect = new Rect(0f, 0f, Mathf.Max(0f, inner.width - 16f), contentHeight);
            Widgets.BeginScrollView(inner, ref this.settingsScroll, viewRect);
            float y = viewRect.y;
            this.payloadCapacityMultiplierValid =
                ShuttleOtherSettingsControlDrawer.DrawMultiplierCard(
                new Rect(viewRect.x, y, viewRect.width, 72f),
                "CT_Shuttle_OtherSettings_PayloadCapacityMultiplier",
                "CT_Shuttle_OtherSettings_PayloadCapacityMultiplierTooltip",
                ShuttleOtherSettings.DefaultPayloadCapacityMultiplier,
                ShuttleOtherSettings.MinimumPayloadCapacityMultiplier,
                ShuttleOtherSettings.MaximumPayloadCapacityMultiplier,
                0.1f,
                ref this.payloadCapacityMultiplierBuffer,
                out this.payloadCapacityMultiplier);
            y += 80f;
            this.constructionWorkMultiplierValid =
                ShuttleOtherSettingsControlDrawer.DrawMultiplierCard(
                    new Rect(viewRect.x, y, viewRect.width, 72f),
                    "CT_Shuttle_OtherSettings_ConstructionWorkMultiplier",
                    "CT_Shuttle_OtherSettings_ConstructionWorkMultiplierTooltip",
                    ShuttleOtherSettings.DefaultConstructionWorkMultiplier,
                    ShuttleOtherSettings.MinimumConstructionWorkMultiplier,
                    ShuttleOtherSettings.MaximumConstructionWorkMultiplier,
                    0.1f,
                    ref this.constructionWorkMultiplierBuffer,
                    out this.constructionWorkMultiplier);
            y += 80f;
            ShuttleOtherSettingsControlDrawer.DrawToggleCard(
                new Rect(viewRect.x, y, viewRect.width, 68f),
                "CT_Shuttle_OtherSettings_ShareRefrigeratedCapacity",
                "CT_Shuttle_OtherSettings_ShareRefrigeratedCapacityTooltip",
                ref this.shareRefrigeratedCargoMass);
            y += 76f;
            this.refrigeratedCargoCapacityRatioValid =
                ShuttleOtherSettingsControlDrawer.DrawRatioCard(
                    new Rect(viewRect.x, y, viewRect.width, 72f),
                    "CT_Shuttle_OtherSettings_IndependentRefrigeratedCapacityRatio",
                    "CT_Shuttle_OtherSettings_IndependentRefrigeratedCapacityRatioTooltip",
                    !this.shareRefrigeratedCargoMass,
                    ShuttleOtherSettings.DefaultIndependentRefrigeratedCargoCapacityRatio,
                    ShuttleOtherSettings.MinimumIndependentRefrigeratedCargoCapacityRatio,
                    ShuttleOtherSettings.MaximumIndependentRefrigeratedCargoCapacityRatio,
                    0.05f,
                    ref this.refrigeratedCargoCapacityRatioBuffer,
                    out this.independentRefrigeratedCargoCapacityRatio);
            y += 80f;
            ShuttleOtherSettingsControlDrawer.DrawToggleCard(
                new Rect(viewRect.x, y, viewRect.width, 68f),
                "CT_Shuttle_OtherSettings_AllowColonistsAtBase",
                "CT_Shuttle_OtherSettings_AllowColonistsAtBaseDesc",
                ref this.allowColonistUseAtBase);
            y += 76f;
            ShuttleOtherSettingsControlDrawer.DrawToggleCard(
                new Rect(viewRect.x, y, viewRect.width, 68f),
                "CT_Shuttle_OtherSettings_AllowMechsAtBase",
                "CT_Shuttle_OtherSettings_AllowMechsAtBaseDesc",
                ref this.allowMechUseAtBase);
            y += 76f;
            ShuttleOtherSettingsControlDrawer.DrawToggleCard(
                new Rect(viewRect.x, y, viewRect.width, 68f),
                "CT_Shuttle_OtherSettings_CompleteBasicPreset",
                "CT_Shuttle_OtherSettings_CompleteBasicPresetDesc",
                ref this.useCompleteBasicShuttlePreset);
            y += 76f;
            ShuttleOtherSettingsControlDrawer.DrawToggleCard(
                new Rect(viewRect.x, y, viewRect.width, 68f),
                "CT_Shuttle_Settings_ShowPerformanceDashboard",
                "CT_Shuttle_Settings_ShowPerformanceDashboardTooltip",
                ref this.showPerformanceDashboard);
            Widgets.EndScrollView();
        }

        private void DrawBottomBar(Rect rect)
        {
            Rect resetRect = new Rect(rect.x, rect.y + 6f, 150f, 34f);
            Rect cancelRect = new Rect(rect.xMax - 250f, rect.y + 6f, 112f, 34f);
            Rect applyRect = new Rect(rect.xMax - 128f, rect.y + 6f, 128f, 34f);
            if (ShuttleV3DialogLayout.DrawDialogButton(
                resetRect,
                "CT_Shuttle_Settings_ResetDefaults".Translate().ToString(),
                true,
                ShuttleV3DialogButtonKind.Normal,
                null))
            {
                this.ResetWorkingCopy();
            }

            if (ShuttleV3DialogLayout.DrawDialogButton(
                cancelRect,
                "CT_Shuttle_UI_Cancel".Translate().ToString(),
                true,
                ShuttleV3DialogButtonKind.Normal,
                null))
            {
                this.Close(false);
            }

            if (ShuttleV3DialogLayout.DrawDialogButton(
                applyRect,
                "CT_Shuttle_UI_Apply".Translate().ToString(),
                this.CanApply,
                ShuttleV3DialogButtonKind.Primary,
                this.ApplyDisabledReason))
            {
                if (this.ApplyWorkingCopy())
                {
                    this.Close(false);
                }
            }
        }

        private void ResetWorkingCopy()
        {
            this.payloadCapacityMultiplier =
                ShuttleOtherSettings.DefaultPayloadCapacityMultiplier;
            this.payloadCapacityMultiplierBuffer =
                this.payloadCapacityMultiplier.ToString(
                "0.0",
                CultureInfo.InvariantCulture);
            this.payloadCapacityMultiplierValid = true;
            this.constructionWorkMultiplier =
                ShuttleOtherSettings.DefaultConstructionWorkMultiplier;
            this.constructionWorkMultiplierBuffer =
                this.constructionWorkMultiplier.ToString(
                    "0.0",
                    CultureInfo.InvariantCulture);
            this.constructionWorkMultiplierValid = true;
            this.shareRefrigeratedCargoMass =
                ShuttleOtherSettings.DefaultShareRefrigeratedCargoMassWithOverallCapacity;
            this.independentRefrigeratedCargoCapacityRatio =
                ShuttleOtherSettings.DefaultIndependentRefrigeratedCargoCapacityRatio;
            this.refrigeratedCargoCapacityRatioBuffer =
                this.independentRefrigeratedCargoCapacityRatio.ToString(
                    "0.00",
                    CultureInfo.InvariantCulture);
            this.refrigeratedCargoCapacityRatioValid = true;
            this.allowColonistUseAtBase =
                ShuttleOtherSettings.DefaultAllowColonistOnboardDeviceUseAtPlayerHome;
            this.allowMechUseAtBase =
                ShuttleOtherSettings.DefaultAllowMechOnboardDeviceUseAtPlayerHome;
            this.useCompleteBasicShuttlePreset = false;
            this.showPerformanceDashboard =
                CeleTechShuttleModSettings.DefaultShowPerformanceDashboard;
        }

        private bool ApplyWorkingCopy()
        {
            if (!this.CanApply)
            {
                return false;
            }

            if (Math.Abs(
                    this.constructionWorkMultiplier -
                    this.appliedConstructionWorkMultiplier) > 0.0001f &&
                ShuttleHostConstructionProjectGuard.HasPendingHostConstruction())
            {
                Messages.Message(
                    "CT_Shuttle_OtherSettings_WorkMultiplierBlockedConstruction"
                        .Translate(),
                    MessageTypeDefOf.RejectInput,
                    false);
                return false;
            }

            if (this.useCompleteBasicShuttlePreset !=
                this.appliedUseCompleteBasicShuttlePreset)
            {
                ShuttleStarterPresetGameComponent component =
                    ShuttleStarterPresetGameComponent.CurrentComponent;
                string failureReason = null;
                ShuttleStarterPresetMode requestedMode =
                    this.useCompleteBasicShuttlePreset
                        ? ShuttleStarterPresetMode.BasicFlightAndCargo
                        : ShuttleStarterPresetMode.FromScratch;
                if (component == null ||
                    !component.TrySetModeFromSettings(requestedMode, out failureReason))
                {
                    if (string.IsNullOrEmpty(failureReason))
                    {
                        failureReason = "CT_Shuttle_StarterPreset_ModeChangeUnavailable"
                            .Translate()
                            .ToString();
                    }

                    Messages.Message(failureReason, MessageTypeDefOf.RejectInput, false);
                    return false;
                }
            }

            CeleTechShuttleModSettings root = CeleTechShuttleMod.Settings;
            if (root.Other == null)
            {
                root.Other = new ShuttleOtherSettings();
            }

            root.Other.PayloadCapacityMultiplier = this.payloadCapacityMultiplier;
            root.Other.ConstructionWorkMultiplier = this.constructionWorkMultiplier;
            root.Other.ShareRefrigeratedCargoMassWithOverallCapacity =
                this.shareRefrigeratedCargoMass;
            root.Other.IndependentRefrigeratedCargoCapacityRatio =
                this.independentRefrigeratedCargoCapacityRatio;
            root.Other.AllowColonistOnboardDeviceUseAtPlayerHome =
                this.allowColonistUseAtBase;
            root.Other.AllowMechOnboardDeviceUseAtPlayerHome = this.allowMechUseAtBase;
            root.ShowPerformanceDashboard = this.showPerformanceDashboard;
            ShuttleSettingsSanitizer.Sanitize(root);
            this.payloadCapacityMultiplier = root.Other.PayloadCapacityMultiplier;
            this.payloadCapacityMultiplierBuffer =
                this.payloadCapacityMultiplier.ToString(
                "0.0",
                CultureInfo.InvariantCulture);
            this.constructionWorkMultiplier = root.Other.ConstructionWorkMultiplier;
            this.constructionWorkMultiplierBuffer =
                this.constructionWorkMultiplier.ToString(
                    "0.0",
                    CultureInfo.InvariantCulture);
            this.shareRefrigeratedCargoMass =
                root.Other.ShareRefrigeratedCargoMassWithOverallCapacity;
            this.independentRefrigeratedCargoCapacityRatio =
                root.Other.IndependentRefrigeratedCargoCapacityRatio;
            this.refrigeratedCargoCapacityRatioBuffer =
                this.independentRefrigeratedCargoCapacityRatio.ToString(
                    "0.00",
                    CultureInfo.InvariantCulture);
            CeleTechShuttleMod.SaveSettingsSafe();
            if (this.onSettingsChanged != null)
            {
                this.onSettingsChanged();
            }

            this.CaptureAppliedValues();
            return true;
        }

        private void CaptureAppliedValues()
        {
            this.appliedPayloadCapacityMultiplier = this.payloadCapacityMultiplier;
            this.appliedConstructionWorkMultiplier =
                this.constructionWorkMultiplier;
            this.appliedShareRefrigeratedCargoMass =
                this.shareRefrigeratedCargoMass;
            this.appliedIndependentRefrigeratedCargoCapacityRatio =
                this.independentRefrigeratedCargoCapacityRatio;
            this.appliedAllowColonistUseAtBase = this.allowColonistUseAtBase;
            this.appliedAllowMechUseAtBase = this.allowMechUseAtBase;
            this.appliedUseCompleteBasicShuttlePreset =
                this.useCompleteBasicShuttlePreset;
            this.appliedShowPerformanceDashboard =
                this.showPerformanceDashboard;
        }
    }
}
