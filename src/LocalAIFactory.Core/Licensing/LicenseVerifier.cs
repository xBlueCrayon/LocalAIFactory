namespace LocalAIFactory.Core.Licensing;

// R2-ACC-20X: deterministic, demo-safe license evaluation. No DRM, no network, no clock access — "today" is
// passed in. A null/invalid/expired paid license degrades gracefully to the Community core; the product never
// blocks itself. This is the placeholder enforcement seam: a real deployment would load LicenseInfo from a
// signed file, but the evaluation rules and feature gating live here and are unit-tested.
public sealed class LicenseVerifier : ILicenseVerifier
{
    public const int GraceDays = 14;

    // Features that are ALWAYS available, regardless of edition or license validity (the proven core).
    private static readonly HashSet<LicenseFeature> CoreFeatures = new()
    {
        LicenseFeature.CoreRepositoryUnderstanding,
        LicenseFeature.KnowledgeBase,
        LicenseFeature.BenchmarkHarness,
        LicenseFeature.CSharpSqlBridge
    };

    // Default feature map per edition (used when a license doesn't enumerate features explicitly).
    public static IReadOnlySet<LicenseFeature> DefaultFeatures(Edition edition) => edition switch
    {
        Edition.Community => new HashSet<LicenseFeature>(CoreFeatures),
        Edition.ProfessionalPilot => new HashSet<LicenseFeature>(CoreFeatures)
        {
            LicenseFeature.MultiProject,
            LicenseFeature.ErpCrmModule,
            LicenseFeature.CoreBankingModule,
            LicenseFeature.DocumentIntelligence
        },
        Edition.Enterprise => new HashSet<LicenseFeature>(System.Enum.GetValues<LicenseFeature>()),
        _ => new HashSet<LicenseFeature>(CoreFeatures)
    };

    public LicenseEvaluation Evaluate(LicenseInfo? license, DateOnly today)
    {
        // No license at all -> Community core (never block the product).
        if (license is null)
            return Community(LicenseStatus.Valid, "No license present — running Community edition (proven core).");

        // Community is perpetual and always valid.
        if (license.Edition == Edition.Community)
            return new LicenseEvaluation(LicenseStatus.Valid, Edition.Community,
                Resolve(license, Edition.Community), "Community edition (perpetual).");

        // Paid edition must identify a customer; malformed -> Invalid -> Community core.
        if (string.IsNullOrWhiteSpace(license.CustomerId) || string.IsNullOrWhiteSpace(license.CustomerName))
            return Community(LicenseStatus.Invalid, "License is missing a customer identity — falling back to Community core.");

        // Paid edition with no expiry is treated as valid (perpetual enterprise agreement).
        if (license.ExpiryUtc is null)
            return new LicenseEvaluation(LicenseStatus.Valid, license.Edition, Resolve(license, license.Edition),
                $"{license.Edition} (perpetual) for {license.CustomerName}.");

        var expiry = license.ExpiryUtc.Value;
        if (today <= expiry)
            return new LicenseEvaluation(LicenseStatus.Valid, license.Edition, Resolve(license, license.Edition),
                $"{license.Edition} valid for {license.CustomerName} until {expiry:yyyy-MM-dd}.");

        var graceEnd = expiry.AddDays(GraceDays);
        if (today <= graceEnd)
            return new LicenseEvaluation(LicenseStatus.GracePeriod, license.Edition, Resolve(license, license.Edition),
                $"{license.Edition} EXPIRED on {expiry:yyyy-MM-dd}; in {GraceDays}-day grace until {graceEnd:yyyy-MM-dd} — renew now.");

        // Past grace -> paid modules off, fall back to Community core.
        return Community(LicenseStatus.Expired,
            $"{license.Edition} expired on {expiry:yyyy-MM-dd} (grace ended {graceEnd:yyyy-MM-dd}) — reverted to Community core.");
    }

    public bool IsFeatureEnabled(LicenseInfo? license, LicenseFeature feature, DateOnly today)
    {
        var e = Evaluate(license, today);
        // Expired/Invalid already collapse Features to the Community core in Evaluate(); Valid/Grace keep them.
        return e.Features.Contains(feature);
    }

    // Resolve effective features: explicit list if the license enumerates them, else the edition default. The
    // core features are always unioned in so the proven core can never be accidentally licensed away.
    private static IReadOnlySet<LicenseFeature> Resolve(LicenseInfo license, Edition effective)
    {
        var set = (license.EnabledFeatures is { Count: > 0 })
            ? new HashSet<LicenseFeature>(license.EnabledFeatures)
            : new HashSet<LicenseFeature>(DefaultFeatures(effective));
        set.UnionWith(CoreFeatures);
        return set;
    }

    private static LicenseEvaluation Community(LicenseStatus status, string reason) =>
        new(status, Edition.Community, new HashSet<LicenseFeature>(CoreFeatures), reason);
}
