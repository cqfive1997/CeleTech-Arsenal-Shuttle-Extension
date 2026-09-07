using System.Collections.Generic;
using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared
{
    /// <summary>
    /// Owns only one transient left-button sweep across filtered cargo-row ordinals.
    /// Selection models and cargo commands remain with the calling panels.
    /// </summary>
    internal sealed class V3CargoRowSweepSelectionGesture
    {
        private readonly HashSet<int> appliedRowIndices = new HashSet<int>();
        private bool active;
        private bool selectMode;
        private int lastRowIndex = -1;

        internal void BeginFrame()
        {
            Event evt = Event.current;
            if (evt == null)
            {
                return;
            }

            if (evt.rawType == EventType.MouseUp ||
                evt.rawType == EventType.Ignore ||
                (evt.type == EventType.MouseDown && evt.button != 0))
            {
                this.Reset();
            }
        }

        internal bool TryHandleRow(
            Rect rowRect,
            int filteredRowIndex,
            bool currentlySelected,
            bool canStartSelection,
            out bool shouldSelect,
            out int firstRowIndex,
            out int lastRowIndex)
        {
            shouldSelect = false;
            firstRowIndex = -1;
            lastRowIndex = -1;

            Event evt = Event.current;
            if (evt == null || filteredRowIndex < 0)
            {
                return false;
            }

            if (evt.type == EventType.MouseDown && evt.button == 0)
            {
                if (!rowRect.Contains(evt.mousePosition))
                {
                    return false;
                }

                // Reserve ordinary row gestures before Verse.Window reaches GUI.DragWindow().
                // Child quantity/info controls run first and leave this method a Used event.
                evt.Use();
                this.Reset();
                if (!currentlySelected && !canStartSelection)
                {
                    return false;
                }

                this.active = true;
                this.selectMode = !currentlySelected;
                this.lastRowIndex = filteredRowIndex;
                shouldSelect = this.selectMode;
                firstRowIndex = filteredRowIndex;
                lastRowIndex = filteredRowIndex;
                return true;
            }

            if (!this.active ||
                evt.type != EventType.MouseDrag ||
                evt.button != 0 ||
                !rowRect.Contains(evt.mousePosition))
            {
                return false;
            }

            evt.Use();
            shouldSelect = this.selectMode;
            firstRowIndex = filteredRowIndex < this.lastRowIndex
                ? filteredRowIndex
                : this.lastRowIndex;
            lastRowIndex = filteredRowIndex > this.lastRowIndex
                ? filteredRowIndex
                : this.lastRowIndex;
            this.lastRowIndex = filteredRowIndex;
            return true;
        }

        internal bool TryMarkRowForApplication(int filteredRowIndex)
        {
            return this.active &&
                filteredRowIndex >= 0 &&
                this.appliedRowIndices.Add(filteredRowIndex);
        }

        private void Reset()
        {
            this.active = false;
            this.selectMode = false;
            this.lastRowIndex = -1;
            this.appliedRowIndices.Clear();
        }
    }
}
