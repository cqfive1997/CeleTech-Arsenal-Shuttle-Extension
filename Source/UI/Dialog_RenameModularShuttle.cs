using RimWorld;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI
{
    /// <summary>
    /// Minimal local rename dialog for the current shuttle host.
    /// This stays intentionally small and only edits the IRenameable label.
    /// </summary>
    public sealed class Dialog_RenameModularShuttle : Window
    {
        private readonly IRenameable target;
        private string currentName;

        public Dialog_RenameModularShuttle(IRenameable target)
        {
            this.target = target;
            this.currentName = target != null ? target.RenamableLabel ?? string.Empty : string.Empty;
            this.forcePause = true;
            this.doCloseX = true;
            this.closeOnClickedOutside = true;
            this.absorbInputAroundWindow = true;
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(420f, 160f);
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            Rect labelRect = new Rect(inRect.x, inRect.y, inRect.width, 30f);
            Widgets.Label(labelRect, "CT_Shuttle_UI_RenameTitle".Translate().ToString());

            Rect textRect = new Rect(inRect.x, labelRect.yMax + 10f, inRect.width, 35f);
            this.currentName = Widgets.TextField(textRect, this.currentName);

            Rect confirmRect = new Rect(inRect.x, inRect.yMax - 35f, 140f, 30f);
            Rect cancelRect = new Rect(inRect.xMax - 140f, inRect.yMax - 35f, 140f, 30f);

            if (Widgets.ButtonText(confirmRect, "OK".Translate().ToString()))
            {
                this.CommitRename();
            }

            if (Widgets.ButtonText(cancelRect, "CT_Shuttle_UI_Cancel".Translate().ToString()))
            {
                this.Close();
            }
        }

        public override void PreClose()
        {
            base.PreClose();
            GUI.FocusControl(null);
        }

        private void CommitRename()
        {
            if (this.target == null)
            {
                this.Close();
                return;
            }

            string trimmedName = this.currentName != null ? this.currentName.Trim() : string.Empty;
            this.target.RenamableLabel = string.IsNullOrEmpty(trimmedName) ? null : trimmedName;
            this.Close();
        }
    }
}
