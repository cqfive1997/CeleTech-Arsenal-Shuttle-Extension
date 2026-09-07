using System;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.Common
{
    internal sealed class ShuttleGUIStateScope : IDisposable
    {
        private readonly Color color;
        private readonly Color backgroundColor;
        private readonly Color contentColor;
        private readonly bool enabled;
        private readonly Matrix4x4 matrix;
        private readonly TextAnchor anchor;
        private readonly GameFont font;
        private readonly bool wordWrap;

        internal ShuttleGUIStateScope()
        {
            this.color = GUI.color;
            this.backgroundColor = GUI.backgroundColor;
            this.contentColor = GUI.contentColor;
            this.enabled = GUI.enabled;
            this.matrix = GUI.matrix;
            this.anchor = Text.Anchor;
            this.font = Text.Font;
            this.wordWrap = Text.WordWrap;
        }

        public void Dispose()
        {
            GUI.color = this.color;
            GUI.backgroundColor = this.backgroundColor;
            GUI.contentColor = this.contentColor;
            GUI.enabled = this.enabled;
            GUI.matrix = this.matrix;
            Text.Anchor = this.anchor;
            Text.Font = this.font;
            Text.WordWrap = this.wordWrap;
        }
    }
}
