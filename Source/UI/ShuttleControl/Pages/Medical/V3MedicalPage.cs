using System;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.Diagnostics;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shell;
using CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Shared;
using UnityEngine;
using Verse;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Medical
{
    internal sealed class V3MedicalPage : IShuttleIntegratedHeaderPageV3
    {
        private const float RightPanelWidth = ShuttleUIStyle.PageRightPanelWidth;
        private const float WideLayoutMinWidth = 1040f;
        private const float WideLayoutMinHeight = 500f;
        private const float MediumLayoutMinWidth = 720f;
        private const float ScrollBarWidth = 16f;
        private const float MediumTopSectionHeight = 320f;
        private const float MediumBottomSectionHeight = 320f;
        private const float NarrowPatientListHeight = 260f;
        private const float NarrowOverviewHeight = 340f;
        private const float NarrowActionHeight = 320f;
        private const float NarrowStatusHeight = 300f;

        private readonly V3MedicalText text = new V3MedicalText();
        private readonly V3MedicalPageModel model = new V3MedicalPageModel();
        private readonly V3MedicalPageModelBuilder modelBuilder =
            new V3MedicalPageModelBuilder();
        private readonly V3MedicalPageState fallbackState = new V3MedicalPageState();
        private readonly V3SharedMessagePanel messagePanel =
            new V3SharedMessagePanel();
        private readonly V3MedicalHeader header;
        private readonly V3MedicalPatientListPanel patientListPanel;
        private readonly V3MedicalDetailPanel detailPanel;
        private readonly V3MedicalAdmissionPanel admissionPanel;
        private readonly V3MedicalActionPanel actionPanel;

        internal V3MedicalPage()
        {
            this.header = new V3MedicalHeader(this.text);
            this.patientListPanel = new V3MedicalPatientListPanel(this.text);
            this.detailPanel = new V3MedicalDetailPanel(this.text);
            this.admissionPanel = new V3MedicalAdmissionPanel(this.text);
            this.actionPanel = new V3MedicalActionPanel(this.text);
        }

        public ShuttleControlPageId Page
        {
            get { return ShuttleControlPageId.Medical; }
        }

        public void OnEnter(ShuttlePageDrawContext context)
        {
        }

        public void OnExit(ShuttlePageDrawContext context)
        {
        }

        public void Draw(Rect rect, ShuttlePageDrawContext context)
        {
            if (context == null)
            {
                return;
            }

            V3MedicalPageState state = this.GetPageState(context);
            using (ShuttleUIProfiler.Scope(ShuttleUIProfileSection.MedicalPageProjection))
            {
                this.modelBuilder.Fill(this.model, context.MedicalInputs);
            }

            ShuttlePageFrameLayout layout = ShuttlePageFrameLayout.HeaderBody(rect);
            this.header.Draw(layout.HeaderRect, this.model, context);
            using (ShuttleUIProfiler.Scope(ShuttleUIProfileSection.MedicalBodyDraw))
            {
                this.DrawBody(layout.BodyRect, this.model, context, state);
            }
        }

        private void DrawBody(
            Rect rect,
            V3MedicalPageModel pageModel,
            ShuttlePageDrawContext context,
            V3MedicalPageState state)
        {
            Rect safeRect = MakeSafeRect(rect);
            if (!IsDrawable(safeRect))
            {
                return;
            }

            if (this.TryDrawWideBody(safeRect, pageModel, context, state))
            {
                return;
            }

            if (safeRect.width >= MediumLayoutMinWidth)
            {
                this.DrawMediumBody(safeRect, pageModel, context, state);
                return;
            }

            this.DrawNarrowBody(safeRect, pageModel, context, state);
        }

        private bool TryDrawWideBody(
            Rect rect,
            V3MedicalPageModel pageModel,
            ShuttlePageDrawContext context,
            V3MedicalPageState state)
        {
            if (rect.width < WideLayoutMinWidth || rect.height < WideLayoutMinHeight)
            {
                return false;
            }

            float gap = ShuttleUIStyle.Gap;
            float rightWidth = RightPanelWidth;
            float rightX = rect.xMax - rightWidth;
            float leftMiddleWidth = Mathf.Max(0f, rightX - gap - rect.x);
            if (leftMiddleWidth < 560f)
            {
                return false;
            }

            ShuttlePageBottomMessageLayout messageLayout =
                ShuttlePageBottomMessageLayout.FromBodyHeight(rect.height, gap);
            float upperHeight = Mathf.Max(0f, messageLayout.ContentHeight);
            if (upperHeight < 220f || messageLayout.MessageHeight <= 1f)
            {
                return false;
            }

            float maxLeftWidth = Mathf.Max(0f, leftMiddleWidth - 250f - gap);
            if (maxLeftWidth <= 220f)
            {
                return false;
            }

            float leftMinWidth = Mathf.Min(300f, maxLeftWidth);
            float leftMaxWidth = Mathf.Min(370f, maxLeftWidth);
            float leftWidth = Mathf.Clamp(Mathf.Floor(leftMiddleWidth * 0.38f), leftMinWidth, leftMaxWidth);
            float middleWidth = Mathf.Max(0f, leftMiddleWidth - leftWidth - gap);
            if (middleWidth < 240f)
            {
                return false;
            }

            float actionHeight = Mathf.Min(330f, Mathf.Max(286f, rect.height - 170f));
            if (actionHeight + gap > rect.height)
            {
                actionHeight = Mathf.Max(0f, rect.height - gap);
            }

            float statusHeight = Mathf.Max(0f, rect.height - actionHeight - gap);
            if (actionHeight < 250f || statusHeight < 150f)
            {
                return false;
            }

            Rect patientListRect = MakeSafeRect(new Rect(rect.x, rect.y, leftWidth, upperHeight));
            Rect overviewRect = MakeSafeRect(new Rect(patientListRect.xMax + gap, rect.y, middleWidth, upperHeight));
            Rect messageRect = MakeSafeRect(new Rect(rect.x, patientListRect.yMax + gap, leftMiddleWidth, messageLayout.MessageHeight));
            Rect actionRect = MakeSafeRect(new Rect(rightX, rect.y, rightWidth, actionHeight));
            Rect statusRect = MakeSafeRect(new Rect(rightX, actionRect.yMax + gap, rightWidth, statusHeight));

            this.DrawMedicalSections(
                patientListRect,
                overviewRect,
                actionRect,
                statusRect,
                pageModel,
                context,
                state,
                null);
            this.messagePanel.Draw(
                messageRect,
                context,
                state.MessagePanelState,
                ref state.MessageScroll);
            this.RegisterTutorialTargets(
                context,
                rect,
                rect,
                patientListRect,
                overviewRect,
                actionRect,
                statusRect,
                messageRect);
            return true;
        }

        private void DrawMediumBody(
            Rect rect,
            V3MedicalPageModel pageModel,
            ShuttlePageDrawContext context,
            V3MedicalPageState state)
        {
            float gap = ShuttleUIStyle.Gap;
            ShuttlePageBottomMessageLayout messageLayout =
                ShuttlePageBottomMessageLayout.FromBodyHeight(rect.height, gap);
            Rect scrollRect = MakeSafeRect(new Rect(rect.x, rect.y, rect.width, messageLayout.ContentHeight));
            Rect messageRect = MakeSafeRect(new Rect(rect.x, scrollRect.yMax + gap, rect.width, messageLayout.MessageHeight));
            float viewWidth = Mathf.Max(0f, scrollRect.width - ScrollBarWidth);
            if (viewWidth < 520f)
            {
                this.DrawNarrowBody(rect, pageModel, context, state);
                return;
            }

            float columnWidth = Mathf.Max(0f, (viewWidth - gap) * 0.5f);
            Rect patientListRect = MakeSafeRect(new Rect(0f, 0f, columnWidth, MediumTopSectionHeight));
            Rect overviewRect = MakeSafeRect(new Rect(patientListRect.xMax + gap, 0f, columnWidth, MediumTopSectionHeight));
            float lowerY = MediumTopSectionHeight + gap;
            Rect actionRect = MakeSafeRect(new Rect(0f, lowerY, columnWidth, MediumBottomSectionHeight));
            Rect statusRect = MakeSafeRect(new Rect(actionRect.xMax + gap, lowerY, columnWidth, MediumBottomSectionHeight));
            Rect viewRect = MakeSafeRect(new Rect(
                0f,
                0f,
                viewWidth,
                MediumTopSectionHeight + gap + MediumBottomSectionHeight));

            this.DrawScrolledMedicalSections(
                scrollRect,
                viewRect,
                patientListRect,
                overviewRect,
                actionRect,
                statusRect,
                pageModel,
                context,
                state);
            this.messagePanel.Draw(
                messageRect,
                context,
                state.MessagePanelState,
                ref state.MessageScroll);
            this.RegisterTutorialTargets(
                context,
                rect,
                rect,
                this.GetScrolledVisibleRect(context, scrollRect, patientListRect, state),
                this.GetScrolledVisibleRect(context, scrollRect, overviewRect, state),
                this.GetScrolledVisibleRect(context, scrollRect, actionRect, state),
                this.GetScrolledVisibleRect(context, scrollRect, statusRect, state),
                messageRect);
        }

        private void DrawNarrowBody(
            Rect rect,
            V3MedicalPageModel pageModel,
            ShuttlePageDrawContext context,
            V3MedicalPageState state)
        {
            float gap = ShuttleUIStyle.Gap;
            ShuttlePageBottomMessageLayout messageLayout =
                ShuttlePageBottomMessageLayout.FromBodyHeight(rect.height, gap);
            Rect scrollRect = MakeSafeRect(new Rect(rect.x, rect.y, rect.width, messageLayout.ContentHeight));
            Rect messageRect = MakeSafeRect(new Rect(rect.x, scrollRect.yMax + gap, rect.width, messageLayout.MessageHeight));
            float viewWidth = Mathf.Max(0f, scrollRect.width - ScrollBarWidth);
            if (viewWidth <= 1f)
            {
                this.messagePanel.Draw(
                    messageRect,
                    context,
                    state.MessagePanelState,
                    ref state.MessageScroll);
                this.RegisterTutorialTargets(
                    context,
                    rect,
                    rect,
                    Rect.zero,
                    Rect.zero,
                    Rect.zero,
                    Rect.zero,
                    messageRect);
                return;
            }

            float y = 0f;
            Rect patientListRect = MakeSafeRect(new Rect(0f, y, viewWidth, NarrowPatientListHeight));
            y += NarrowPatientListHeight + gap;
            Rect overviewRect = MakeSafeRect(new Rect(0f, y, viewWidth, NarrowOverviewHeight));
            y += NarrowOverviewHeight + gap;
            Rect actionRect = MakeSafeRect(new Rect(0f, y, viewWidth, NarrowActionHeight));
            y += NarrowActionHeight + gap;
            Rect statusRect = MakeSafeRect(new Rect(0f, y, viewWidth, NarrowStatusHeight));
            y += NarrowStatusHeight;
            Rect viewRect = MakeSafeRect(new Rect(0f, 0f, viewWidth, y));

            this.DrawScrolledMedicalSections(
                scrollRect,
                viewRect,
                patientListRect,
                overviewRect,
                actionRect,
                statusRect,
                pageModel,
                context,
                state);
            this.messagePanel.Draw(
                messageRect,
                context,
                state.MessagePanelState,
                ref state.MessageScroll);
            this.RegisterTutorialTargets(
                context,
                rect,
                rect,
                this.GetScrolledVisibleRect(context, scrollRect, patientListRect, state),
                this.GetScrolledVisibleRect(context, scrollRect, overviewRect, state),
                this.GetScrolledVisibleRect(context, scrollRect, actionRect, state),
                this.GetScrolledVisibleRect(context, scrollRect, statusRect, state),
                messageRect);
        }

        private void DrawScrolledMedicalSections(
            Rect scrollRect,
            Rect viewRect,
            Rect patientListRect,
            Rect overviewRect,
            Rect actionRect,
            Rect statusRect,
            V3MedicalPageModel pageModel,
            ShuttlePageDrawContext context,
            V3MedicalPageState state)
        {
            if (!IsDrawable(scrollRect) || !IsDrawable(viewRect))
            {
                return;
            }

            this.ClampMedicalLayoutScroll(scrollRect, viewRect, state);
            Func<Rect, Rect> tutorialRectMapper = delegate(Rect localRect)
            {
                return this.GetScrolledVisibleRect(context, scrollRect, localRect, state);
            };

            Widgets.BeginScrollView(scrollRect, ref state.LayoutScroll, viewRect);
            this.DrawMedicalSections(
                patientListRect,
                overviewRect,
                actionRect,
                statusRect,
                pageModel,
                context,
                state,
                tutorialRectMapper);
            Widgets.EndScrollView();
            this.ClampMedicalLayoutScroll(scrollRect, viewRect, state);
        }

        private void DrawMedicalSections(
            Rect patientListRect,
            Rect overviewRect,
            Rect actionRect,
            Rect statusRect,
            V3MedicalPageModel pageModel,
            ShuttlePageDrawContext context,
            V3MedicalPageState state,
            Func<Rect, Rect> tutorialRectMapper)
        {
            V3MedicalPatientCardModel selectedPatient =
                this.GetSelectedPatient(pageModel != null ? pageModel.MedicalModel : null, state);
            this.patientListPanel.Draw(patientListRect, pageModel, selectedPatient, state);
            this.detailPanel.Draw(overviewRect, selectedPatient, state);
            this.admissionPanel.Draw(
                actionRect,
                selectedPatient,
                pageModel,
                context,
                tutorialRectMapper);
            this.actionPanel.DrawStatus(statusRect, pageModel, state, context);
        }

        private V3MedicalPatientCardModel GetSelectedPatient(
            V3MedicalPageReadModel medicalModel,
            V3MedicalPageState state)
        {
            if (medicalModel == null || medicalModel.Patients == null || medicalModel.Patients.Count == 0)
            {
                return null;
            }

            if (state != null)
            {
                for (int i = 0; i < medicalModel.Patients.Count; i++)
                {
                    V3MedicalPatientCardModel patient = medicalModel.Patients[i];
                    if (patient == null)
                    {
                        continue;
                    }

                    if (patient.PatientThingID > 0 &&
                        patient.PatientThingID == state.SelectedPatientThingID)
                    {
                        return patient;
                    }

                    if (!string.IsNullOrEmpty(patient.PatientKey) &&
                        patient.PatientKey == state.SelectedPatientKey)
                    {
                        return patient;
                    }
                }
            }

            return medicalModel.Patients[0];
        }

        private Rect GetScrolledVisibleRect(
            ShuttlePageDrawContext context,
            Rect scrollRect,
            Rect localRect,
            V3MedicalPageState state)
        {
            IShuttleTutorialTargetService tutorialTargets = GetTutorialTargets(context);
            return tutorialTargets != null
                ? tutorialTargets.GetScrolledVisibleRect(
                    scrollRect,
                    localRect,
                    state != null ? state.LayoutScroll : Vector2.zero)
                : Rect.zero;
        }

        private void RegisterTutorialTargets(
            ShuttlePageDrawContext context,
            Rect visibleBounds,
            Rect pageRect,
            Rect patientListRect,
            Rect overviewRect,
            Rect actionRect,
            Rect statusRect,
            Rect messageRect)
        {
            IShuttleTutorialTargetService tutorialTargets = GetTutorialTargets(context);
            if (tutorialTargets == null)
            {
                return;
            }

            tutorialTargets.RegisterClipped(ShuttleTutorialTargetIds.MedicalOverviewPanel, pageRect, visibleBounds);
            tutorialTargets.RegisterClipped(ShuttleTutorialTargetIds.MedicalPatientListPanel, patientListRect, visibleBounds);
            tutorialTargets.RegisterClipped(ShuttleTutorialTargetIds.MedicalPatientDetailPanel, overviewRect, visibleBounds);
            tutorialTargets.RegisterClipped(ShuttleTutorialTargetIds.MedicalProcedureStatusPanel, statusRect, visibleBounds);
            tutorialTargets.RegisterClipped(ShuttleTutorialTargetIds.MedicalMessagesPanel, messageRect, visibleBounds);
            tutorialTargets.RegisterClipped(ShuttleTutorialTargetIds.MedicalActionButtonsPanel, actionRect, visibleBounds);
        }

        private static IShuttleTutorialTargetService GetTutorialTargets(
            ShuttlePageDrawContext context)
        {
            return context != null && context.MedicalPageContext != null
                ? context.MedicalPageContext.TutorialTargets
                : null;
        }

        private void ClampMedicalLayoutScroll(
            Rect scrollRect,
            Rect viewRect,
            V3MedicalPageState state)
        {
            if (state == null)
            {
                return;
            }

            float maxY = Mathf.Max(0f, viewRect.height - scrollRect.height);
            state.LayoutScroll.x = 0f;
            state.LayoutScroll.y = Mathf.Clamp(state.LayoutScroll.y, 0f, maxY);
        }

        private V3MedicalPageState GetPageState(ShuttlePageDrawContext context)
        {
            if (context != null && context.State != null && context.State.Medical != null)
            {
                return context.State.Medical;
            }

            return this.fallbackState;
        }

        private static Rect MakeSafeRect(Rect rect)
        {
            return new Rect(
                rect.x,
                rect.y,
                Mathf.Max(0f, rect.width),
                Mathf.Max(0f, rect.height));
        }

        private static bool IsDrawable(Rect rect)
        {
            return rect.width > 1f && rect.height > 1f;
        }
    }
}
