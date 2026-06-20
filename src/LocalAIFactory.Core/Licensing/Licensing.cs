namespace LocalAIFactory.Core.Licensing;

// R2-ACC-20X: edition / feature-flag / license model. Design rules (honest, demo-safe):
//   * NO DRM, NO phone-home, NO hard dependency on a license server. A missing/invalid license degrades to the
//     Community edition — the product NEVER bricks itself or blocks a demo because a license file is absent.
//   * Editions gate optional MODULES (ERP/CRM, core-banking, market intelligence, autonomy, SSO, document
//     intelligence, priority support). The proven core (repository understanding + knowledge base + benchmark
//     + C#/SQL bridge) is available in every edition.
//   * Evaluation is deterministic and takes "today" as a parameter, so it is fully unit-testable and never
//     reaches for the wall clock inside Core.
public enum Edition
{
    Community = 0,          // free / dev — proven core only, perpetual
    ProfessionalPilot = 1,  // a scoped, time-boxed paid pilot
    Enterprise = 2          // full module set
}

public enum LicenseFeature
{
    CoreRepositoryUnderstanding = 0,  // always on (every edition)
    KnowledgeBase = 1,                // always on
    BenchmarkHarness = 2,             // always on
    CSharpSqlBridge = 3,              // always on
    MultiProject = 4,
    ErpCrmModule = 5,
    CoreBankingModule = 6,
    MarketIntelligenceModule = 7,
    AutonomousEngineering = 8,
    SsoIntegration = 9,
    DocumentIntelligence = 10,
    PrioritySupport = 11
}

public enum LicenseStatus
{
    Valid = 0,
    GracePeriod = 1,   // past expiry but within the grace window — still functional, warn the operator
    Expired = 2,       // past expiry + grace — paid modules off, falls back to Community core
    Invalid = 3        // malformed (e.g. paid edition with no customer id) — treated as Community
}

// The license record. ExpiryUtc == null means perpetual (Community). EnabledFeatures may be supplied explicitly;
// when empty, the default feature map for the edition is used.
public sealed record LicenseInfo(
    Edition Edition,
    string CustomerId,
    string CustomerName,
    DateOnly? ExpiryUtc,
    IReadOnlySet<LicenseFeature>? EnabledFeatures = null)
{
    public static LicenseInfo Community() =>
        new(Edition.Community, "COMMUNITY", "Community / Development", null);
}

// Outcome of evaluating a license as-of a given day: the effective status, edition, the features actually
// available right now, and a human-readable reason for the support/admin dashboard.
public sealed record LicenseEvaluation(
    LicenseStatus Status,
    Edition EffectiveEdition,
    IReadOnlySet<LicenseFeature> Features,
    string Reason);

public interface ILicenseVerifier
{
    LicenseEvaluation Evaluate(LicenseInfo? license, DateOnly today);
    bool IsFeatureEnabled(LicenseInfo? license, LicenseFeature feature, DateOnly today);
}
