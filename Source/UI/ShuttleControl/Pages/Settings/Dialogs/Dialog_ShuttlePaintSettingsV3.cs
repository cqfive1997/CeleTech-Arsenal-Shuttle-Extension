using System;
using CeleTech.ShuttleExtension.ModularShuttle.AssemblyState;
using CeleTech.ShuttleExtension.ModularShuttle.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.Snapshots;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared.Dialogs;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell.Paint;
using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Settings.Dialogs
{
    internal enum ShuttlePaintChannel
    {
        Primary,
        Secondary,
        Accent
    }

    internal sealed class Dialog_ShuttlePaintSettingsV3 : Window, IShuttleSettingsHubSection
    {
        private const float LeftWidth = 220f;
        private const float RightWidth = 300f;
        private const float BottomHeight = 48f;

        private readonly IShuttleCommandExecutor commandExecutor;
        private readonly Action onPaintChanged;
        private readonly Rot4 previewRotation;
        private readonly ShuttlePaintPreviewBaker previewBaker =
            new ShuttlePaintPreviewBaker(
                ShuttlePaintPreviewSourceKind.LandedBrowser,
                ShuttlePaintPreviewRenderTextureCache.PreviewWidth,
                ShuttlePaintPreviewRenderTextureCache.PreviewHeight,
                "CeleTech_ShuttlePaintDialogPreview");
        private ShuttlePaintSchemeSnapshot workingCopy;
        private ShuttlePaintSchemeSnapshot lastApplied;
        private ShuttlePaintChannel selectedChannel = ShuttlePaintChannel.Primary;
        private ShuttlePaintChannel rgbInputBufferChannel = ShuttlePaintChannel.Primary;
        private string redInputBuffer;
        private string greenInputBuffer;
        private string blueInputBuffer;
        private bool previewDisposed;

        internal Dialog_ShuttlePaintSettingsV3(
            ShuttlePaintSchemeSnapshot currentScheme,
            IShuttleCommandExecutor commandExecutor,
            Rot4 previewRotation,
            Action onPaintChanged)
        {
            this.commandExecutor = commandExecutor;
            this.onPaintChanged = onPaintChanged;
            this.previewRotation = previewRotation;
            this.workingCopy = this.SanitizeSnapshot(currentScheme);
            this.lastApplied = this.workingCopy.Clone();
            this.forcePause = false;
            this.doCloseX = false;
            this.absorbInputAroundWindow = true;
            this.draggable = true;
            this.resizeable = false;
            this.previewBaker.RequestRebuild(
                this.workingCopy,
                false,
                0,
                this.previewRotation);
            this.SyncRgbInputBuffers(this.GetSelectedColor());
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(920f, 660f);
            }
        }

        public ShuttleSettingsHubCommitMode CommitMode
        {
            get { return ShuttleSettingsHubCommitMode.Explicit; }
        }

        public bool CanApply
        {
            get { return this.commandExecutor != null; }
        }

        public bool HasPendingChanges
        {
            get { return this.HasUnsavedChanges(); }
        }

        public string ApplyDisabledReason
        {
            get
            {
                return this.commandExecutor != null
                    ? null
                    : "CT_Shuttle_Command_ExecutorUnavailable".Translate().ToString();
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            this.workingCopy = this.SanitizeSnapshot(this.workingCopy);
            Rect titleRect = new Rect(inRect.x, inRect.y, inRect.width, 62f);
            this.DrawTitle(titleRect);

            Rect bodyRect = new Rect(inRect.x, titleRect.yMax + 10f, inRect.width, inRect.height - titleRect.height - BottomHeight - 20f);
            Rect bottomRect = new Rect(inRect.x, bodyRect.yMax + 10f, inRect.width, BottomHeight);

            Rect leftRect = new Rect(bodyRect.x, bodyRect.y, LeftWidth, bodyRect.height);
            Rect rightRect = new Rect(bodyRect.xMax - RightWidth, bodyRect.y, RightWidth, bodyRect.height);
            Rect centerRect = new Rect(leftRect.xMax + 10f, bodyRect.y, rightRect.x - leftRect.xMax - 20f, bodyRect.height);

            this.DrawChannelPanel(leftRect);
            this.DrawPreviewPanel(centerRect);
            this.DrawEditorPanel(rightRect);
            this.DrawBottomBar(bottomRect);
        }

        public void Draw(Rect rect)
        {
            this.workingCopy = this.SanitizeSnapshot(this.workingCopy);
            float leftWidth = Mathf.Min(LeftWidth, Mathf.Max(178f, rect.width * 0.25f));
            float rightWidth = Mathf.Min(RightWidth, Mathf.Max(250f, rect.width * 0.32f));
            Rect leftRect = new Rect(rect.x, rect.y, leftWidth, rect.height);
            Rect rightRect = new Rect(rect.xMax - rightWidth, rect.y, rightWidth, rect.height);
            Rect centerRect = new Rect(
                leftRect.xMax + 10f,
                rect.y,
                Mathf.Max(0f, rightRect.x - leftRect.xMax - 20f),
                rect.height);

            this.DrawChannelPanel(leftRect);
            this.DrawPreviewPanel(centerRect);
            this.DrawEditorPanel(rightRect);
        }

        public void Reset()
        {
            this.workingCopy = ShuttlePaintSchemeSnapshot.Default;
            this.SyncRgbInputBuffers(this.GetSelectedColor());
            this.RequestPreviewRebuild(false);
        }

        public bool Apply()
        {
            return this.ApplyWorkingCopy();
        }

        public void OnHubClosed()
        {
            this.DisposePreview();
        }

        public override void PostClose()
        {
            this.DisposePreview();
            base.PostClose();
        }

        private void DrawTitle(Rect rect)
        {
            Color oldColor = GUI.color;
            GameFont oldFont = Text.Font;
            try
            {
                Text.Font = GameFont.Medium;
                GUI.color = ShuttleV3DialogStyle.HeaderTitleTextColor;
                ShuttleV3DialogLayout.SafeLabel(
                    new Rect(rect.x, rect.y, rect.width, 30f),
                    "CT_Shuttle_Paint_Title".Translate().ToString());
                Text.Font = GameFont.Tiny;
                GUI.color = ShuttleV3DialogStyle.MutedTextColor;
                ShuttleV3DialogLayout.SafeLabel(
                    new Rect(rect.x, rect.y + 34f, rect.width, 22f),
                    "CT_Shuttle_Paint_Subtitle".Translate().ToString());
            }
            finally
            {
                GUI.color = oldColor;
                Text.Font = oldFont;
            }
        }

        private void DrawChannelPanel(Rect rect)
        {
            ShuttleV3DialogLayout.DrawPanelBackground(rect);
            Rect inner = new Rect(rect.x + 10f, rect.y + 12f, rect.width - 20f, rect.height - 24f);
            bool enabled = this.workingCopy.Enabled;
            Rect checkRect = new Rect(inner.x, inner.y, inner.width, 26f);
            Widgets.CheckboxLabeled(checkRect, "CT_Shuttle_Paint_EnableCustom".Translate().ToString(), ref enabled);
            if (enabled != this.workingCopy.Enabled)
            {
                this.workingCopy.Enabled = enabled;
                this.RequestPreviewRebuild(false);
            }

            float y = checkRect.yMax + 16f;
            if (this.DrawPaintButton(
                new Rect(inner.x, y, inner.width, 40f),
                "CT_Shuttle_Paint_PrimaryColor".Translate().ToString(),
                this.selectedChannel == ShuttlePaintChannel.Primary,
                false,
                null))
            {
                this.selectedChannel = ShuttlePaintChannel.Primary;
                this.SyncRgbInputBuffers(this.GetSelectedColor());
            }

            y += 48f;
            if (this.DrawPaintButton(
                new Rect(inner.x, y, inner.width, 40f),
                "CT_Shuttle_Paint_SecondaryColor".Translate().ToString(),
                this.selectedChannel == ShuttlePaintChannel.Secondary,
                false,
                null))
            {
                this.selectedChannel = ShuttlePaintChannel.Secondary;
                this.SyncRgbInputBuffers(this.GetSelectedColor());
            }

            y += 48f;
            if (this.DrawPaintButton(
                new Rect(inner.x, y, inner.width, 40f),
                "CT_Shuttle_Paint_AccentColor".Translate().ToString(),
                this.selectedChannel == ShuttlePaintChannel.Accent,
                false,
                null))
            {
                this.selectedChannel = ShuttlePaintChannel.Accent;
                this.SyncRgbInputBuffers(this.GetSelectedColor());
            }

            y += 64f;
            this.DrawPresetSwatches(new Rect(inner.x, y, inner.width, 132f));

            Rect noticeRect = new Rect(inner.x, inner.yMax - 94f, inner.width, 94f);
            this.DrawDescription(
                noticeRect,
                "CT_Shuttle_Paint_MapVisualConnected".Translate().ToString() + "\n\n" +
                "CT_Shuttle_Paint_RGBMaskNotice".Translate().ToString());
        }

        private void DrawPresetSwatches(Rect rect)
        {
            ShuttleV3DialogLayout.DrawCardBackground(rect, false, false, ShuttleV3DialogStyle.SettingsInfoRowColor);
            GameFont oldFont = Text.Font;
            Color oldColor = GUI.color;
            try
            {
                Text.Font = GameFont.Tiny;
                GUI.color = Color.white;
                ShuttleV3DialogLayout.SafeLabel(
                    new Rect(rect.x + 8f, rect.y + 6f, rect.width - 16f, 18f),
                    "CT_Shuttle_Paint_Presets".Translate().ToString());
                float size = 28f;
                float gap = 8f;
                float y = rect.y + 32f;
                this.DrawSwatch(new Rect(rect.x + 8f, y, size, size), Color.white, "CT_Shuttle_Paint_ColorWhite".Translate().ToString());
                this.DrawSwatch(new Rect(rect.x + 8f + (size + gap), y, size, size), Color.black, "CT_Shuttle_Paint_ColorBlack".Translate().ToString());
                this.DrawSwatch(new Rect(rect.x + 8f + ((size + gap) * 2f), y, size, size), new Color(0.78f, 0.12f, 0.10f, 1f), "CT_Shuttle_Paint_ColorRed".Translate().ToString());
                this.DrawSwatch(new Rect(rect.x + 8f + ((size + gap) * 3f), y, size, size), new Color(0.12f, 0.42f, 0.88f, 1f), "CT_Shuttle_Paint_ColorBlue".Translate().ToString());
                y += size + gap;
                this.DrawSwatch(new Rect(rect.x + 8f, y, size, size), new Color(0.95f, 0.68f, 0.22f, 1f), "CT_Shuttle_Paint_ColorGold".Translate().ToString());
                this.DrawSwatch(new Rect(rect.x + 8f + (size + gap), y, size, size), new Color(0.10f, 0.78f, 0.86f, 1f), "CT_Shuttle_Paint_ColorCyan".Translate().ToString());
            }
            finally
            {
                Text.Font = oldFont;
                GUI.color = oldColor;
            }
        }

        private void DrawSwatch(Rect rect, Color color, string tooltip)
        {
            Widgets.DrawBoxSolid(rect, color);
            ShuttleV3DialogLayout.DrawRectBorder(rect, ShuttleV3DialogStyle.SubtleBorderColor, 1f);
            if (!string.IsNullOrEmpty(tooltip))
            {
                TooltipHandler.TipRegion(rect, tooltip);
            }

            if (Widgets.ButtonInvisible(rect))
            {
                this.SetSelectedColor(color);
                this.SyncRgbInputBuffers(this.GetSelectedColor());
                this.workingCopy.Enabled = true;
                this.RequestPreviewRebuild(false);
            }
        }

        private void DrawPreviewPanel(Rect rect)
        {
            ShuttleV3DialogLayout.DrawPanelBackground(rect);
            Rect inner = new Rect(rect.x + 14f, rect.y + 14f, rect.width - 28f, rect.height - 28f);
            Color oldColor = GUI.color;
            GameFont oldFont = Text.Font;
            try
            {
                Text.Font = GameFont.Small;
                GUI.color = Color.white;
                ShuttleV3DialogLayout.SafeLabel(
                    new Rect(inner.x, inner.y, inner.width, 24f),
                    "CT_Shuttle_Paint_Preview".Translate().ToString());

                Rect previewRect = new Rect(inner.x, inner.y + 34f, inner.width, Mathf.Min(360f, inner.height - 154f));
                ShuttleV3DialogLayout.DrawCardBackground(previewRect, false, false, ShuttleV3DialogStyle.SettingsPreviewCardColor);
                Rect iconRect = new Rect(previewRect.x + 12f, previewRect.y + 12f, previewRect.width - 24f, previewRect.height - 24f);
                this.previewBaker.TryRebuildIfDue();
                ShuttlePaintPreviewDrawer.Draw(
                    iconRect,
                    this.previewBaker.PreviewTexture);

                float y = previewRect.yMax + 14f;
                this.DrawColorSummary(new Rect(inner.x, y, inner.width, 72f));
                if (this.HasUnsavedChanges())
                {
                    GUI.color = ShuttleV3DialogStyle.YellowStatusColor;
                    Text.Font = GameFont.Tiny;
                    ShuttleV3DialogLayout.SafeLabel(
                        new Rect(inner.x, inner.yMax - 24f, inner.width, 20f),
                        "CT_Shuttle_Paint_UnsavedChanges".Translate().ToString());
                }
            }
            finally
            {
                GUI.color = oldColor;
                Text.Font = oldFont;
            }
        }

        private void DrawColorSummary(Rect rect)
        {
            ShuttleV3DialogLayout.DrawCardBackground(rect, false, false, ShuttleV3DialogStyle.SettingsInfoRowColor);
            float swatch = 24f;
            float y = rect.y + 10f;
            this.DrawSummarySwatch(new Rect(rect.x + 10f, y, swatch, swatch), this.workingCopy.PrimaryColor, "CT_Shuttle_Paint_PrimaryColor".Translate().ToString());
            this.DrawSummarySwatch(new Rect(rect.x + 46f, y, swatch, swatch), this.workingCopy.SecondaryColor, "CT_Shuttle_Paint_SecondaryColor".Translate().ToString());
            this.DrawSummarySwatch(new Rect(rect.x + 82f, y, swatch, swatch), this.workingCopy.AccentColor, "CT_Shuttle_Paint_AccentColor".Translate().ToString());

            Text.Font = GameFont.Tiny;
            GUI.color = ShuttleV3DialogStyle.MutedTextColor;
            ShuttleV3DialogLayout.SafeLabel(
                new Rect(rect.x + 120f, rect.y + 8f, rect.width - 130f, 22f),
                this.workingCopy.Enabled
                    ? "CT_Shuttle_Paint_CustomSchemeEnabled".Translate().ToString()
                    : "CT_Shuttle_Paint_DefaultScheme".Translate().ToString());
            ShuttleV3DialogLayout.SafeLabel(
                new Rect(rect.x + 120f, rect.y + 34f, rect.width - 130f, 22f),
                this.FormatColor(this.GetSelectedColor()));
            GUI.color = Color.white;
        }

        private void DrawSummarySwatch(Rect rect, Color color, string tooltip)
        {
            Widgets.DrawBoxSolid(rect, color);
            ShuttleV3DialogLayout.DrawRectBorder(rect, ShuttleV3DialogStyle.SubtleBorderColor, 1f);
            TooltipHandler.TipRegion(rect, tooltip);
        }

        private void DrawEditorPanel(Rect rect)
        {
            ShuttleV3DialogLayout.DrawPanelBackground(rect);
            Rect inner = new Rect(rect.x + 12f, rect.y + 12f, rect.width - 24f, rect.height - 24f);
            Color selectedColor = this.GetSelectedColor();
            this.EnsureRgbInputBuffers(selectedColor);
            Color oldColor = GUI.color;
            GameFont oldFont = Text.Font;
            try
            {
                Text.Font = GameFont.Small;
                GUI.color = Color.white;
                ShuttleV3DialogLayout.SafeLabel(
                    new Rect(inner.x, inner.y, inner.width, 24f),
                    this.GetSelectedChannelLabel());

                Rect previewSwatch = new Rect(inner.x, inner.y + 34f, inner.width, 52f);
                Widgets.DrawBoxSolid(previewSwatch, selectedColor);
                ShuttleV3DialogLayout.DrawRectBorder(previewSwatch, ShuttleV3DialogStyle.ButtonBorderColor, 1f);

                float y = previewSwatch.yMax + 20f;
                bool colorChanged = false;
                colorChanged |= this.DrawColorSlider(new Rect(inner.x, y, inner.width, 44f), "CT_Shuttle_Paint_Red".Translate().ToString(), ref selectedColor.r, ref this.redInputBuffer, "CT_Shuttle_Paint_RedInput");
                y += 52f;
                colorChanged |= this.DrawColorSlider(new Rect(inner.x, y, inner.width, 44f), "CT_Shuttle_Paint_Green".Translate().ToString(), ref selectedColor.g, ref this.greenInputBuffer, "CT_Shuttle_Paint_GreenInput");
                y += 52f;
                colorChanged |= this.DrawColorSlider(new Rect(inner.x, y, inner.width, 44f), "CT_Shuttle_Paint_Blue".Translate().ToString(), ref selectedColor.b, ref this.blueInputBuffer, "CT_Shuttle_Paint_BlueInput");
                if (colorChanged)
                {
                    selectedColor.a = 1f;
                    this.SetSelectedColor(selectedColor);
                    this.workingCopy.Enabled = true;
                    this.RequestPreviewRebuild(true);
                }

                y += 68f;
                if (this.DrawPaintButton(
                    new Rect(inner.x, y, inner.width, 34f),
                    "CT_Shuttle_Paint_ResetChannel".Translate().ToString(),
                    false,
                    false,
                    null))
                {
                    this.ResetSelectedChannel();
                    this.SyncRgbInputBuffers(this.GetSelectedColor());
                    this.RequestPreviewRebuild(false);
                }
            }
            finally
            {
                GUI.color = oldColor;
                Text.Font = oldFont;
            }
        }

        private bool DrawColorSlider(Rect rect, string label, ref float value, ref string inputBuffer, string inputControlName)
        {
            float oldValue = Mathf.Clamp01(value);
            value = oldValue;
            Text.Font = GameFont.Tiny;
            GUI.color = Color.white;
            ShuttleV3DialogLayout.SafeLabel(new Rect(rect.x, rect.y, 56f, 20f), label);
            Rect sliderRect = new Rect(rect.x + 62f, rect.y + 1f, rect.width - 126f, 20f);
            value = Widgets.HorizontalSlider(sliderRect, value, 0f, 1f);
            value = Mathf.Clamp01(value);
            if (Mathf.Abs(value - oldValue) > 0.0001f)
            {
                inputBuffer = this.FormatRgbComponent(value);
            }

            Rect inputRect = new Rect(rect.xMax - 54f, rect.y - 2f, 54f, 24f);
            GUI.SetNextControlName(inputControlName);
            string editedText = Widgets.TextField(inputRect, inputBuffer ?? string.Empty);
            string sanitizedText = this.SanitizeRgbInput(editedText);
            if (sanitizedText != inputBuffer)
            {
                inputBuffer = sanitizedText;
            }

            int parsedValue;
            if (int.TryParse(inputBuffer, out parsedValue))
            {
                if (parsedValue < 0)
                {
                    parsedValue = 0;
                }
                else if (parsedValue > 255)
                {
                    parsedValue = 255;
                }

                float inputValue = parsedValue / 255f;
                if (Mathf.Abs(inputValue - value) > 0.0001f)
                {
                    value = inputValue;
                }
            }

            GUI.color = Color.white;
            return Mathf.Abs(value - oldValue) > 0.0001f;
        }

        private void EnsureRgbInputBuffers(Color color)
        {
            if (this.rgbInputBufferChannel != this.selectedChannel ||
                this.redInputBuffer == null ||
                this.greenInputBuffer == null ||
                this.blueInputBuffer == null)
            {
                this.SyncRgbInputBuffers(color);
            }
        }

        private void SyncRgbInputBuffers(Color color)
        {
            this.redInputBuffer = this.FormatRgbComponent(color.r);
            this.greenInputBuffer = this.FormatRgbComponent(color.g);
            this.blueInputBuffer = this.FormatRgbComponent(color.b);
            this.rgbInputBufferChannel = this.selectedChannel;
        }

        private string FormatRgbComponent(float value)
        {
            return Mathf.RoundToInt(Mathf.Clamp01(value) * 255f).ToString();
        }

        private string SanitizeRgbInput(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            string digits = string.Empty;
            for (int i = 0; i < text.Length; i++)
            {
                if (char.IsDigit(text[i]))
                {
                    digits += text[i];
                }
            }

            if (digits.Length == 0)
            {
                return string.Empty;
            }

            if (digits.Length > 3)
            {
                digits = digits.Substring(0, 3);
            }

            int value;
            if (int.TryParse(digits, out value) && value > 255)
            {
                return "255";
            }

            return digits;
        }

        private void DrawBottomBar(Rect rect)
        {
            float buttonWidth = 150f;
            Rect resetRect = new Rect(rect.x, rect.y + 7f, buttonWidth, 34f);
            Rect applyRect = new Rect(rect.xMax - (buttonWidth * 2f) - 10f, rect.y + 7f, buttonWidth, 34f);
            Rect closeRect = new Rect(rect.xMax - buttonWidth, rect.y + 7f, buttonWidth, 34f);

            if (this.DrawPaintButton(
                resetRect,
                "CT_Shuttle_Paint_ResetDefaults".Translate().ToString(),
                false,
                true,
                "CT_Shuttle_Paint_ResetDefaultsTooltip".Translate().ToString()))
            {
                this.Reset();
            }

            if (this.DrawPaintButton(applyRect, "CT_Shuttle_Paint_Apply".Translate().ToString(), false, false, null))
            {
                this.ApplyWorkingCopy();
            }

            string closeTooltip = this.HasUnsavedChanges()
                ? "CT_Shuttle_Paint_CloseUnsavedTooltip".Translate().ToString()
                : null;
            if (this.DrawPaintButton(closeRect, "CT_Shuttle_Paint_Close".Translate().ToString(), false, false, closeTooltip))
            {
                this.Close(false);
            }
        }

        private bool ApplyWorkingCopy()
        {
            if (this.commandExecutor == null)
            {
                Messages.Message("CT_Shuttle_Command_ExecutorUnavailable".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }

            this.workingCopy = this.SanitizeSnapshot(this.workingCopy);
            ShuttleCommandResult result = this.commandExecutor.Execute(new SetShuttlePaintSchemeCommand(this.workingCopy));
            if (result == null || !result.Success)
            {
                Messages.Message(
                    result != null && !string.IsNullOrEmpty(result.Message)
                        ? result.Message
                        : "CT_Shuttle_Paint_CommandUnavailable".Translate().ToString(),
                    MessageTypeDefOf.RejectInput,
                    false);
                return false;
            }

            this.lastApplied = this.workingCopy.Clone();
            this.RequestPreviewRebuild(false);
            Messages.Message(result.Message, MessageTypeDefOf.NeutralEvent, false);
            if (this.onPaintChanged != null)
            {
                this.onPaintChanged();
            }

            return true;
        }

        private void DisposePreview()
        {
            if (this.previewDisposed)
            {
                return;
            }

            this.previewDisposed = true;
            this.previewBaker.Dispose();
        }

        private bool DrawPaintButton(Rect rect, string label, bool selected, bool danger, string tooltip)
        {
            Color color = selected
                ? ShuttleV3DialogStyle.SelectedColor
                : danger ? ShuttleV3DialogStyle.DangerButtonBgColor : ShuttleV3DialogStyle.ButtonBgColor;
            return ShuttleV3DialogLayout.DrawTintedButton(
                rect,
                label,
                true,
                color,
                selected ? ShuttleV3DialogStyle.BlueStatusColor : ShuttleV3DialogStyle.ButtonBorderColor,
                danger ? ShuttleV3DialogStyle.DangerButtonTextColor : ShuttleV3DialogStyle.ButtonTextColor,
                tooltip);
        }

        private void DrawDescription(Rect rect, string text)
        {
            ShuttleV3DialogLayout.DrawCardBackground(rect, false, false, ShuttleV3DialogStyle.SettingsInfoRowColor);
            Color oldColor = GUI.color;
            GameFont oldFont = Text.Font;
            bool oldWordWrap = Text.WordWrap;
            try
            {
                GUI.color = ShuttleV3DialogStyle.MutedTextColor;
                Text.Font = GameFont.Tiny;
                Text.WordWrap = true;
                ShuttleV3DialogLayout.SafeLabel(new Rect(rect.x + 8f, rect.y + 8f, rect.width - 16f, rect.height - 16f), text);
            }
            finally
            {
                GUI.color = oldColor;
                Text.Font = oldFont;
                Text.WordWrap = oldWordWrap;
            }
        }

        private Color GetSelectedColor()
        {
            if (this.selectedChannel == ShuttlePaintChannel.Secondary)
            {
                return this.workingCopy.SecondaryColor;
            }

            if (this.selectedChannel == ShuttlePaintChannel.Accent)
            {
                return this.workingCopy.AccentColor;
            }

            return this.workingCopy.PrimaryColor;
        }

        private void SetSelectedColor(Color color)
        {
            Color sanitized = ShuttlePaintSchemeSanitizer.SanitizeColor(color, this.GetDefaultColorForSelectedChannel());
            if (this.selectedChannel == ShuttlePaintChannel.Secondary)
            {
                this.workingCopy.SecondaryColor = sanitized;
                return;
            }

            if (this.selectedChannel == ShuttlePaintChannel.Accent)
            {
                this.workingCopy.AccentColor = sanitized;
                return;
            }

            this.workingCopy.PrimaryColor = sanitized;
        }

        private void ResetSelectedChannel()
        {
            Color oldColor = this.GetSelectedColor();
            Color defaultColor = this.GetDefaultColorForSelectedChannel();
            if (!this.SameColor(oldColor, defaultColor))
            {
                this.SetSelectedColor(defaultColor);
                this.workingCopy.Enabled = true;
            }
        }

        private Color GetDefaultColorForSelectedChannel()
        {
            if (this.selectedChannel == ShuttlePaintChannel.Secondary)
            {
                return ShuttlePaintSchemeSanitizer.DefaultSecondaryColor;
            }

            if (this.selectedChannel == ShuttlePaintChannel.Accent)
            {
                return ShuttlePaintSchemeSanitizer.DefaultAccentColor;
            }

            return ShuttlePaintSchemeSanitizer.DefaultPrimaryColor;
        }

        private string GetSelectedChannelLabel()
        {
            if (this.selectedChannel == ShuttlePaintChannel.Secondary)
            {
                return "CT_Shuttle_Paint_SecondaryColor".Translate().ToString();
            }

            if (this.selectedChannel == ShuttlePaintChannel.Accent)
            {
                return "CT_Shuttle_Paint_AccentColor".Translate().ToString();
            }

            return "CT_Shuttle_Paint_PrimaryColor".Translate().ToString();
        }

        private ShuttlePaintSchemeSnapshot SanitizeSnapshot(ShuttlePaintSchemeSnapshot source)
        {
            ShuttlePaintSchemeSnapshot safe = source != null ? source.Clone() : ShuttlePaintSchemeSnapshot.Default;
            safe.PrimaryColor = ShuttlePaintSchemeSanitizer.SanitizeColor(safe.PrimaryColor, ShuttlePaintSchemeSanitizer.DefaultPrimaryColor);
            safe.SecondaryColor = ShuttlePaintSchemeSanitizer.SanitizeColor(safe.SecondaryColor, ShuttlePaintSchemeSanitizer.DefaultSecondaryColor);
            safe.AccentColor = ShuttlePaintSchemeSanitizer.SanitizeColor(safe.AccentColor, ShuttlePaintSchemeSanitizer.DefaultAccentColor);
            if (string.IsNullOrEmpty(safe.PresetKey))
            {
                safe.PresetKey = ShuttlePaintScheme.DefaultPresetKey;
            }

            return safe;
        }

        private bool HasUnsavedChanges()
        {
            return !this.SameScheme(this.workingCopy, this.lastApplied);
        }

        private bool SameScheme(ShuttlePaintSchemeSnapshot left, ShuttlePaintSchemeSnapshot right)
        {
            if (left == null || right == null)
            {
                return left == right;
            }

            return left.Enabled == right.Enabled &&
                this.SameColor(left.PrimaryColor, right.PrimaryColor) &&
                this.SameColor(left.SecondaryColor, right.SecondaryColor) &&
                this.SameColor(left.AccentColor, right.AccentColor);
        }

        private bool SameColor(Color left, Color right)
        {
            return Mathf.Abs(left.r - right.r) < 0.001f &&
                Mathf.Abs(left.g - right.g) < 0.001f &&
                Mathf.Abs(left.b - right.b) < 0.001f &&
                Mathf.Abs(left.a - right.a) < 0.001f;
        }

        private string FormatColor(Color color)
        {
            return "RGB " +
                Mathf.RoundToInt(color.r * 255f) + " / " +
                Mathf.RoundToInt(color.g * 255f) + " / " +
                Mathf.RoundToInt(color.b * 255f);
        }

        private void RequestPreviewRebuild(bool throttled)
        {
            this.previewBaker.RequestRebuild(
                this.workingCopy,
                throttled,
                0,
                this.previewRotation);
        }
    }
}
