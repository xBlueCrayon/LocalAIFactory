using System.Globalization;
using LocalAIFactory.Core.Abstractions;

namespace LocalAIFactory.Ingestion.Cheque;

// R2-ACC-CAP5: deterministic cheque risk-triage engine. It NEVER asserts fraud and never auto-approves — it
// produces an auditable TRIAGE (Accept/Review/Reject) with flags + evidence, forces human review on any risk,
// and keeps signature DETECTION separate from VERIFICATION. No computer vision here; it reasons over the
// OCR/signature DTOs a future CV service would populate.
public sealed class ChequeRiskEngine : IChequeRiskEngine
{
    private const double LowConfidence = 0.60;
    private const double ForgeryConcern = 0.50;
    private const double ForgeryHigh = 0.80;
    private const double VerificationFloor = 0.50;
    private const decimal HighValue = 10000m;

    public ChequeRiskAssessment Assess(ChequeOcrResult ocr, SignatureAnalysis signature, decimal? declaredAmount = null)
    {
        var flags = new List<string>();
        var evidence = new List<string>();

        void Field(string name, OcrField f)
        {
            evidence.Add($"{name}: '{f.Value ?? "(none)"}' conf={f.Confidence:0.00}");
            if (f.Value is null) flags.Add($"{name} not read");
            else if (f.Confidence < LowConfidence) flags.Add($"low-confidence OCR: {name}");
        }
        Field("CAR", ocr.CourtesyAmount);
        Field("LAR", ocr.LegalAmount);
        Field("MICR", ocr.Micr);
        Field("Date", ocr.Date);
        Field("Payee", ocr.Payee);

        // CAR/LAR consistency vs a declared amount (deterministic; the written/numeric comparison itself is a CV
        // concern). If a declared amount is supplied and the numeric CAR disagrees, flag it.
        if (declaredAmount is decimal dec && TryAmount(ocr.CourtesyAmount.Value, out var car) && Math.Abs(car - dec) > 0.005m)
            flags.Add($"CAR/declared amount mismatch ({car} vs {dec})");

        // Signature: detection and verification are DISTINCT.
        evidence.Add($"signaturePresent={signature.SignaturePresent} regionDetected={signature.RegionDetected} " +
                     $"verificationScore={Fmt(signature.VerificationScore)} forgeryRiskScore={Fmt(signature.ForgeryRiskScore)}");
        if (!signature.SignaturePresent) flags.Add("signature not detected (detection)");
        if (signature.ForgeryRiskScore is double fr && fr >= ForgeryConcern) flags.Add("elevated signature forgery risk (verification)");
        if (signature.VerificationScore is double vs && signature.ReferenceSpecimenId != null && vs < VerificationFloor)
            flags.Add("signature verification below threshold (verification)");

        bool highValue = (declaredAmount is decimal d && d >= HighValue)
                         || (TryAmount(ocr.CourtesyAmount.Value, out var c) && c >= HighValue);
        if (highValue) flags.Add("high-value item");

        // Honest confidence floor: the lowest signal we relied on.
        var confidences = new List<double> { ocr.CourtesyAmount.Confidence, ocr.LegalAmount.Confidence, ocr.Micr.Confidence, ocr.Date.Confidence, ocr.Payee.Confidence };
        var overall = confidences.Count > 0 ? confidences.Min() : 0;

        // Triage — NEVER a fraud verdict. Reject = route to manual rejection workflow; Review = human review;
        // Accept = proceed (still not a "genuine" legal determination).
        var triage = ChequeTriage.Accept;
        if (signature.ForgeryRiskScore is double f2 && f2 >= ForgeryHigh) triage = ChequeTriage.Reject;
        else if (flags.Count > 0) triage = ChequeTriage.Review;

        bool humanReview = flags.Count > 0 || triage != ChequeTriage.Accept;

        const string limitation =
            "Risk triage only — NOT a fraud determination and not legally conclusive. Signature detection and " +
            "verification are separate; results are probabilistic with false-positive/false-negative risk and no " +
            "asserted accuracy. High-risk items require human review; the final decision is a human/legal one.";

        return new ChequeRiskAssessment(triage, humanReview, overall, flags, evidence, limitation);
    }

    private static bool TryAmount(string? s, out decimal value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(s)) return false;
        var cleaned = new string(s.Where(ch => char.IsDigit(ch) || ch == '.' || ch == ',').ToArray()).Replace(",", "");
        return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
    }

    private static string Fmt(double? d) => d is double v ? v.ToString("0.00", CultureInfo.InvariantCulture) : "n/a";
}
