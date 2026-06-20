namespace LocalAIFactory.Core.Abstractions;

// R2-ACC-CAP5: cheque OCR / signature-forgery RISK workflow skeleton. Hard rules baked into the types: this
// NEVER asserts fraud, never produces an automatic decision, always separates signature *detection* from
// *verification*, always records confidence + evidence, and always routes high-risk items to a human. Actual
// computer vision is out of scope here — the engine reasons over OCR/signature DTOs that a future Python/CV
// service would populate.

// A field read from the cheque image, with the model's confidence (0..1). Value may be null if not read.
public sealed record OcrField(string? Value, double Confidence);

// Deterministic, evidence-bearing OCR result (populated by a future CV service; here it is the engine's input).
public sealed record ChequeOcrResult(
    OcrField CourtesyAmount,    // CAR — numeric amount box
    OcrField LegalAmount,       // LAR — written amount
    OcrField Micr,              // MICR line
    OcrField Date,
    OcrField Payee);

// Signature analysis. DETECTION (is a signature present / where) is strictly separate from VERIFICATION
// (does it match a reference) — different questions with different error profiles. Scores are probabilistic;
// null means "not assessed".
public sealed record SignatureAnalysis(
    bool SignaturePresent,          // detection
    bool RegionDetected,            // detection (localisation)
    double? VerificationScore,      // verification vs a reference specimen (higher = more similar); null if none
    double? ForgeryRiskScore,       // probabilistic forgery risk (higher = riskier); null if not assessed
    string? ReferenceSpecimenId);   // which reference was compared, if any

public enum ChequeTriage { Accept = 0, Review = 1, Reject = 2 }

// The risk outcome. Triage is a TRIAGE recommendation, never a fraud verdict. HumanReviewRequired is true for
// anything not cleanly low-risk. Flags + Evidence make the decision auditable. There is no "Fraud" or
// "Approved-as-genuine" state — those are human, legal determinations the platform must not make.
public sealed record ChequeRiskAssessment(
    ChequeTriage Triage,
    bool HumanReviewRequired,
    double OverallConfidence,
    IReadOnlyList<string> RiskFlags,
    IReadOnlyList<string> Evidence,
    string LimitationNote);

public interface IChequeRiskEngine
{
    // Apply deterministic risk rules. High value or any risk flag forces HumanReviewRequired and Review/Reject.
    ChequeRiskAssessment Assess(ChequeOcrResult ocr, SignatureAnalysis signature, decimal? declaredAmount = null);
}
