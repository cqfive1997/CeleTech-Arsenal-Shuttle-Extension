using CeleTech.ShuttleExtension.ModularShuttle.Presentation;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common;
using CeleTech.ShuttleExtension.ModularShuttle.UI.Common.ReadModels.Pawns;

namespace CeleTech.ShuttleExtension.ModularShuttle.UI.ShuttleControl.Pages.Medical
{
    internal sealed class V3MedicalAdmissionModelBuilder
    {
        private readonly V3MedicalClassificationFormatter classificationFormatter;

        internal V3MedicalAdmissionModelBuilder(
            V3MedicalClassificationFormatter classificationFormatter)
        {
            this.classificationFormatter = classificationFormatter;
        }

        internal void BuildAdmissionCandidates(
            V3MedicalPageReadModel pageModel,
            ShuttleMedicalBayReadModel medicalBay,
            ShuttlePawnPresenceSnapshot pawnPresenceSnapshot)
        {
            if (pageModel == null ||
                medicalBay == null ||
                medicalBay.AdmissionCandidates == null)
            {
                return;
            }

            for (int i = 0; i < medicalBay.AdmissionCandidates.Count; i++)
            {
                ShuttleMedicalAdmissionCandidateReadModel source =
                    medicalBay.AdmissionCandidates[i];
                if (source == null)
                {
                    continue;
                }

                pageModel.AdmissionCandidates.Add(
                    this.BuildAdmissionCandidate(source, i, pawnPresenceSnapshot));
            }
        }

        private V3MedicalAdmissionCandidateModel BuildAdmissionCandidate(
            ShuttleMedicalAdmissionCandidateReadModel source,
            int index,
            ShuttlePawnPresenceSnapshot pawnPresenceSnapshot)
        {
            ShuttlePawnPresenceRecord presence = this.FindPresence(
                pawnPresenceSnapshot,
                source.PawnThingID,
                ShuttlePawnPresenceKind.MedicalAdmissionCandidate);
            V3MedicalAdmissionCandidateModel candidate =
                new V3MedicalAdmissionCandidateModel();
            candidate.PawnThingID = source.PawnThingID;
            candidate.Id = !string.IsNullOrEmpty(source.Id)
                ? source.Id
                : (source.PawnThingID > 0
                    ? source.PawnThingID.ToString()
                    : "candidate-" + index.ToString());
            candidate.Label = presence != null && !string.IsNullOrEmpty(presence.Label)
                ? presence.Label
                : (!string.IsNullOrEmpty(source.Label)
                    ? source.Label
                    : ShuttleUIText.Tr("CT_Shuttle_Medical_UnknownCandidate"));
            candidate.DisplayThing = presence != null && presence.DisplayThing != null
                ? presence.DisplayThing
                : source.DisplayThing;
            this.ApplyCandidateClassification(candidate, source, presence);
            candidate.ReasonText = !string.IsNullOrEmpty(source.ReasonText)
                ? source.ReasonText
                : ShuttleUIText.Tr("CT_Shuttle_Medical_ReasonUnavailable");
            candidate.CanAdmit = source.CanAdmit;
            candidate.NeedsCarry = source.NeedsCarry;
            candidate.IsAlreadyInMedicalBay = source.IsAlreadyInMedicalBay;
            candidate.Tooltip = !string.IsNullOrEmpty(source.Tooltip)
                ? source.Tooltip
                : this.BuildCandidateTooltip(candidate);
            this.CopyCarryCandidates(candidate, source, pawnPresenceSnapshot);
            return candidate;
        }

        private void ApplyCandidateClassification(
            V3MedicalAdmissionCandidateModel candidate,
            ShuttleMedicalAdmissionCandidateReadModel source,
            ShuttlePawnPresenceRecord presence)
        {
            candidate.KindKey = this.GetPresenceKindKey(presence, source.KindKey);
            candidate.KindLabel = !string.IsNullOrEmpty(source.KindLabel)
                ? source.KindLabel
                : this.classificationFormatter.GetCandidateKindFallbackLabel(
                    candidate.KindKey);
            candidate.TriageKey = !string.IsNullOrEmpty(source.TriageKey)
                ? source.TriageKey
                : "Stable";
            candidate.TriageLabel = !string.IsNullOrEmpty(source.TriageLabel)
                ? source.TriageLabel
                : this.classificationFormatter.GetTriageFallbackLabel(
                    candidate.TriageKey);
            candidate.AdmissionModeKey = !string.IsNullOrEmpty(source.AdmissionModeKey)
                ? source.AdmissionModeKey
                : "Pending";
            candidate.AdmissionModeLabel = !string.IsNullOrEmpty(source.AdmissionModeLabel)
                ? source.AdmissionModeLabel
                : this.classificationFormatter.GetAdmissionModeFallbackLabel(
                    candidate.AdmissionModeKey);
        }

        private void CopyCarryCandidates(
            V3MedicalAdmissionCandidateModel candidate,
            ShuttleMedicalAdmissionCandidateReadModel source,
            ShuttlePawnPresenceSnapshot pawnPresenceSnapshot)
        {
            if (source.CarryCandidates == null)
            {
                return;
            }

            for (int i = 0; i < source.CarryCandidates.Count; i++)
            {
                ShuttleMedicalAdmissionCarrierReadModel sourceCarrier =
                    source.CarryCandidates[i];
                if (sourceCarrier == null)
                {
                    continue;
                }

                candidate.CarryCandidates.Add(
                    this.BuildAdmissionCarrier(
                        sourceCarrier,
                        i,
                        pawnPresenceSnapshot));
            }
        }

        private V3MedicalAdmissionCarrierModel BuildAdmissionCarrier(
            ShuttleMedicalAdmissionCarrierReadModel sourceCarrier,
            int index,
            ShuttlePawnPresenceSnapshot pawnPresenceSnapshot)
        {
            ShuttlePawnPresenceRecord presence = this.FindPresence(
                pawnPresenceSnapshot,
                sourceCarrier.PawnThingID,
                ShuttlePawnPresenceKind.MedicalAdmissionCarrier);
            V3MedicalAdmissionCarrierModel carrier =
                new V3MedicalAdmissionCarrierModel();
            carrier.PawnThingID = sourceCarrier.PawnThingID;
            carrier.Id = !string.IsNullOrEmpty(sourceCarrier.Id)
                ? sourceCarrier.Id
                : (sourceCarrier.PawnThingID > 0
                    ? sourceCarrier.PawnThingID.ToString()
                    : "carrier-" + index.ToString());
            carrier.Label = presence != null && !string.IsNullOrEmpty(presence.Label)
                ? presence.Label
                : (!string.IsNullOrEmpty(sourceCarrier.Label)
                    ? sourceCarrier.Label
                    : ShuttleUIText.Tr("CT_Shuttle_Medical_UnknownCarrier"));
            carrier.DisplayThing = presence != null && presence.DisplayThing != null
                ? presence.DisplayThing
                : sourceCarrier.DisplayThing;
            carrier.CanCarry = sourceCarrier.CanCarry;
            carrier.StatusText = !string.IsNullOrEmpty(sourceCarrier.StatusText)
                ? sourceCarrier.StatusText
                : (carrier.CanCarry
                    ? ShuttleUIText.Tr("CT_Shuttle_Medical_CanCarry")
                    : ShuttleUIText.Tr("CT_Shuttle_Medical_Unavailable"));
            carrier.ReasonText = !string.IsNullOrEmpty(sourceCarrier.ReasonText)
                ? sourceCarrier.ReasonText
                : carrier.StatusText;
            carrier.Tooltip = !string.IsNullOrEmpty(sourceCarrier.Tooltip)
                ? sourceCarrier.Tooltip
                : carrier.Label + "\n" + carrier.StatusText + "\n" + carrier.ReasonText;
            return carrier;
        }

        private ShuttlePawnPresenceRecord FindPresence(
            ShuttlePawnPresenceSnapshot pawnPresenceSnapshot,
            int pawnThingID,
            ShuttlePawnPresenceKind kind)
        {
            return pawnPresenceSnapshot != null
                ? pawnPresenceSnapshot.FindByPawnThingIDAndKind(pawnThingID, kind)
                : null;
        }

        private string GetPresenceKindKey(
            ShuttlePawnPresenceRecord presence,
            string fallbackKindKey)
        {
            if (presence != null)
            {
                if (presence.IsMech)
                {
                    return "Mechanoid";
                }

                if (presence.IsAnimal)
                {
                    return "Animal";
                }

                if (presence.IsPrisoner)
                {
                    return "Prisoner";
                }

                if (presence.IsSlave)
                {
                    return "Slave";
                }

                if (presence.IsColonist)
                {
                    return "Colonist";
                }

                if (presence.IsHumanlike)
                {
                    return "Human";
                }
            }

            return !string.IsNullOrEmpty(fallbackKindKey)
                ? fallbackKindKey
                : "Human";
        }

        private string BuildCandidateTooltip(
            V3MedicalAdmissionCandidateModel candidate)
        {
            return candidate.Label + "\n" +
                candidate.KindLabel + " / " + candidate.TriageLabel + "\n" +
                candidate.AdmissionModeLabel + "\n" +
                candidate.ReasonText;
        }

    }
}
