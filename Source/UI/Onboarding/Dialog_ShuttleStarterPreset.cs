using CeleTech.ShuttleExtension.ModularShuttle.Onboarding;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared.Dialogs;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Onboarding
{
    internal sealed class Dialog_ShuttleStarterPreset : Window
    {
        private const string PortraitPath = "UI/Shuttle/Console/BGPlanet";

        private readonly ShuttleStarterPresetGameComponent component;
        private Texture2D portrait;
        private bool choiceCommitted;

        internal Dialog_ShuttleStarterPreset(ShuttleStarterPresetGameComponent component)
        {
            this.component = component;
            this.doWindowBackground = false;
            this.doCloseX = false;
            this.closeOnCancel = false;
            this.closeOnClickedOutside = false;
            this.absorbInputAroundWindow = true;
            this.forcePause = true;
            this.draggable = false;
            this.resizeable = false;
            this.preventCameraMotion = true;
            this.layer = WindowLayer.Dialog;
        }

        public override Vector2 InitialSize
        {
            get { return new Vector2(1080f, 690f); }
        }

        public override void PreOpen()
        {
            base.PreOpen();
            this.portrait = ContentFinder<Texture2D>.Get(PortraitPath, false);
        }

        public override void DoWindowContents(Rect inRect)
        {
            Widgets.DrawBoxSolid(inRect, ShuttleV3DialogStyle.PanelColor);
            ShuttleV3DialogLayout.DrawRectBorder(
                inRect,
                ShuttleV3DialogStyle.BorderColor,
                2f);

            Rect headerRect = new Rect(inRect.x + 18f, inRect.y + 14f, inRect.width - 36f, 58f);
            Rect footerRect = new Rect(inRect.x + 20f, inRect.yMax - 48f, inRect.width - 40f, 30f);
            Rect bodyRect = new Rect(
                inRect.x + 18f,
                headerRect.yMax + 10f,
                inRect.width - 36f,
                footerRect.y - headerRect.yMax - 18f);

            this.DrawHeader(headerRect);
            this.DrawBody(bodyRect);
            this.DrawFooter(footerRect);
        }

        public override void PostClose()
        {
            base.PostClose();
            if (this.component != null)
            {
                this.component.NotifyDialogClosed();
            }
        }

        private void DrawHeader(Rect rect)
        {
            GameFont oldFont = Text.Font;
            Color oldColor = GUI.color;
            try
            {
                Text.Font = GameFont.Medium;
                GUI.color = ShuttleV3DialogStyle.HeaderTitleTextColor;
                Widgets.Label(
                    new Rect(rect.x, rect.y, rect.width, 30f),
                    "CT_Shuttle_StarterPreset_DialogTitle".Translate());
                Text.Font = GameFont.Tiny;
                GUI.color = ShuttleV3DialogStyle.MutedTextColor;
                Widgets.Label(
                    new Rect(rect.x, rect.y + 34f, rect.width, 22f),
                    "CT_Shuttle_StarterPreset_DialogSubtitle".Translate());
            }
            finally
            {
                Text.Font = oldFont;
                GUI.color = oldColor;
            }
        }

        private void DrawBody(Rect rect)
        {
            const float portraitWidth = 356f;
            Rect portraitRect = new Rect(rect.x, rect.y, portraitWidth, rect.height);
            Rect dialogueRect = new Rect(
                portraitRect.xMax + 14f,
                rect.y,
                rect.width - portraitWidth - 14f,
                rect.height);
            this.DrawPortrait(portraitRect);
            this.DrawDialogue(dialogueRect);
        }

        private void DrawPortrait(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.025f, 0.035f, 0.050f, 1f));
            if (this.portrait != null)
            {
                // BGPlanet is a wide transparent scene with the assistant on its left half.
                // Crop a portrait-aspect source region around the face/body instead of
                // center-cropping the full 16:9 canvas, which would cut off the character.
                GUI.DrawTextureWithTexCoords(
                    rect,
                    this.portrait,
                    new Rect(0.11f, 0f, 0.396f, 1f),
                    true);
            }
            else
            {
                GameFont oldFont = Text.Font;
                TextAnchor oldAnchor = Text.Anchor;
                Color oldColor = GUI.color;
                try
                {
                    Text.Font = GameFont.Medium;
                    Text.Anchor = TextAnchor.MiddleCenter;
                    GUI.color = ShuttleV3DialogStyle.MutedTextColor;
                    Widgets.Label(rect, "CT_Shuttle_StarterPreset_PortraitFallback".Translate());
                }
                finally
                {
                    Text.Font = oldFont;
                    Text.Anchor = oldAnchor;
                    GUI.color = oldColor;
                }
            }

            Widgets.DrawBoxSolid(
                new Rect(rect.x, rect.yMax - 54f, rect.width, 54f),
                new Color(0.025f, 0.040f, 0.055f, 0.92f));
            GameFont savedFont = Text.Font;
            Color savedColor = GUI.color;
            try
            {
                Text.Font = GameFont.Small;
                GUI.color = ShuttleV3DialogStyle.HeaderTitleTextColor;
                Widgets.Label(
                    new Rect(rect.x + 14f, rect.yMax - 43f, rect.width - 28f, 28f),
                    "CT_Shuttle_StarterPreset_Speaker".Translate());
            }
            finally
            {
                Text.Font = savedFont;
                GUI.color = savedColor;
            }

            ShuttleV3DialogLayout.DrawRectBorder(
                rect,
                ShuttleV3DialogStyle.ButtonBorderColor,
                2f);
        }

        private void DrawDialogue(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.045f, 0.060f, 0.080f, 0.92f));
            ShuttleV3DialogLayout.DrawRectBorder(
                rect,
                ShuttleV3DialogStyle.SubtleBorderColor,
                1f);

            GameFont oldFont = Text.Font;
            bool oldWordWrap = Text.WordWrap;
            Color oldColor = GUI.color;
            try
            {
                Text.Font = GameFont.Small;
                Text.WordWrap = true;
                GUI.color = Color.white;
                Widgets.Label(
                    new Rect(rect.x + 18f, rect.y + 14f, rect.width - 36f, 92f),
                    "CT_Shuttle_StarterPreset_Intro".Translate());
            }
            finally
            {
                Text.Font = oldFont;
                Text.WordWrap = oldWordWrap;
                GUI.color = oldColor;
            }

            Rect scratchRect = new Rect(rect.x + 16f, rect.y + 112f, rect.width - 32f, 145f);
            Rect basicRect = new Rect(rect.x + 16f, scratchRect.yMax + 12f, rect.width - 32f, 145f);
            if (ShuttleStarterPresetChoiceDrawer.Draw(
                scratchRect,
                "CT_Shuttle_StarterPreset_FromScratch_Title".Translate().ToString(),
                "CT_Shuttle_StarterPreset_FromScratch_Body".Translate().ToString(),
                "CT_Shuttle_StarterPreset_FromScratch_Tag".Translate().ToString(),
                false))
            {
                this.CommitChoice(ShuttleStarterPresetMode.FromScratch);
            }

            if (ShuttleStarterPresetChoiceDrawer.Draw(
                basicRect,
                "CT_Shuttle_StarterPreset_Basic_Title".Translate().ToString(),
                "CT_Shuttle_StarterPreset_Basic_Body".Translate().ToString(),
                "CT_Shuttle_StarterPreset_Basic_Tag".Translate().ToString(),
                true))
            {
                this.CommitChoice(ShuttleStarterPresetMode.BasicFlightAndCargo);
            }

            GameFont savedFont = Text.Font;
            bool savedWordWrap = Text.WordWrap;
            Color savedColor = GUI.color;
            try
            {
                Text.Font = GameFont.Tiny;
                Text.WordWrap = true;
                GUI.color = ShuttleV3DialogStyle.YellowStatusColor;
                Widgets.Label(
                    new Rect(
                        rect.x + 20f,
                        basicRect.yMax + 13f,
                        rect.width - 40f,
                        rect.yMax - basicRect.yMax - 20f),
                    "CT_Shuttle_StarterPreset_ResearchNote".Translate());
            }
            finally
            {
                Text.Font = savedFont;
                Text.WordWrap = savedWordWrap;
                GUI.color = savedColor;
            }
        }

        private void DrawFooter(Rect rect)
        {
            GameFont oldFont = Text.Font;
            Color oldColor = GUI.color;
            try
            {
                Text.Font = GameFont.Tiny;
                GUIStyle footerStyle = new GUIStyle(Text.CurFontStyle);
                footerStyle.alignment = TextAnchor.MiddleCenter;
                footerStyle.fontStyle = FontStyle.Bold;
                footerStyle.normal.textColor = ShuttleV3DialogStyle.PrimaryButtonTextColor;
                GUI.color = Color.white;
                GUI.Label(
                    rect,
                    "CT_Shuttle_StarterPreset_Footer".Translate().ToString(),
                    footerStyle);
            }
            finally
            {
                Text.Font = oldFont;
                GUI.color = oldColor;
            }
        }

        private void CommitChoice(ShuttleStarterPresetMode mode)
        {
            if (this.choiceCommitted)
            {
                return;
            }

            this.choiceCommitted = true;
            if (this.component != null)
            {
                this.component.ResolveSelection(mode);
            }

            this.Close(false);
        }
    }
}
