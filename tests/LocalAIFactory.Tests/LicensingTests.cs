using System;
using LocalAIFactory.Core.Licensing;
using Xunit;

namespace LocalAIFactory.Tests;

// R2-ACC-20X: edition / license evaluation. Demo-safe by design — a missing/invalid/expired paid license never
// blocks the product; it degrades to the Community core. Deterministic ("today" is injected).
public class LicensingTests
{
    private static readonly ILicenseVerifier V = new LicenseVerifier();
    private static readonly DateOnly Today = new(2026, 6, 21);

    [Fact]
    public void Null_license_runs_community_core_never_blocks()
    {
        var e = V.Evaluate(null, Today);
        Assert.Equal(LicenseStatus.Valid, e.Status);
        Assert.Equal(Edition.Community, e.EffectiveEdition);
        Assert.True(e.Features.Contains(LicenseFeature.CoreRepositoryUnderstanding));
        Assert.False(e.Features.Contains(LicenseFeature.MarketIntelligenceModule));
    }

    [Fact]
    public void Community_is_perpetual_and_valid()
    {
        var e = V.Evaluate(LicenseInfo.Community(), Today.AddYears(50));
        Assert.Equal(LicenseStatus.Valid, e.Status);
        Assert.True(e.Features.Contains(LicenseFeature.CSharpSqlBridge));
    }

    [Fact]
    public void Enterprise_within_term_enables_all_modules()
    {
        var lic = new LicenseInfo(Edition.Enterprise, "CUST-001", "Pilot Bank Ltd", new DateOnly(2027, 1, 1));
        var e = V.Evaluate(lic, Today);
        Assert.Equal(LicenseStatus.Valid, e.Status);
        Assert.True(e.Features.Contains(LicenseFeature.MarketIntelligenceModule));
        Assert.True(e.Features.Contains(LicenseFeature.AutonomousEngineering));
        Assert.True(e.Features.Contains(LicenseFeature.SsoIntegration));
    }

    [Fact]
    public void Professional_pilot_enables_pilot_modules_not_enterprise_only()
    {
        var lic = new LicenseInfo(Edition.ProfessionalPilot, "CUST-002", "Mgmt Co", new DateOnly(2026, 12, 31));
        Assert.True(V.IsFeatureEnabled(lic, LicenseFeature.ErpCrmModule, Today));
        Assert.True(V.IsFeatureEnabled(lic, LicenseFeature.CoreBankingModule, Today));
        Assert.False(V.IsFeatureEnabled(lic, LicenseFeature.AutonomousEngineering, Today));
    }

    [Fact]
    public void Within_grace_window_stays_functional_but_warns()
    {
        var lic = new LicenseInfo(Edition.Enterprise, "CUST-003", "Bank", new DateOnly(2026, 6, 15)); // 6 days ago
        var e = V.Evaluate(lic, Today);
        Assert.Equal(LicenseStatus.GracePeriod, e.Status);
        Assert.Equal(Edition.Enterprise, e.EffectiveEdition);
        Assert.True(e.Features.Contains(LicenseFeature.ErpCrmModule));
        Assert.Contains("grace", e.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Past_grace_reverts_to_community_core()
    {
        var lic = new LicenseInfo(Edition.Enterprise, "CUST-004", "Bank", new DateOnly(2026, 1, 1)); // long expired
        var e = V.Evaluate(lic, Today);
        Assert.Equal(LicenseStatus.Expired, e.Status);
        Assert.Equal(Edition.Community, e.EffectiveEdition);
        Assert.False(e.Features.Contains(LicenseFeature.ErpCrmModule));
        Assert.True(e.Features.Contains(LicenseFeature.CoreRepositoryUnderstanding)); // core still works
    }

    [Fact]
    public void Paid_edition_without_customer_is_invalid_and_degrades()
    {
        var lic = new LicenseInfo(Edition.Enterprise, "", "", new DateOnly(2030, 1, 1));
        var e = V.Evaluate(lic, Today);
        Assert.Equal(LicenseStatus.Invalid, e.Status);
        Assert.Equal(Edition.Community, e.EffectiveEdition);
    }

    [Fact]
    public void Core_features_can_never_be_licensed_away()
    {
        var lic = new LicenseInfo(Edition.Enterprise, "CUST-005", "Bank", new DateOnly(2027, 1, 1),
            EnabledFeatures: new HashSet<LicenseFeature> { LicenseFeature.MarketIntelligenceModule }); // explicit, minimal
        var e = V.Evaluate(lic, Today);
        Assert.True(e.Features.Contains(LicenseFeature.CoreRepositoryUnderstanding));
        Assert.True(e.Features.Contains(LicenseFeature.KnowledgeBase));
        Assert.True(e.Features.Contains(LicenseFeature.MarketIntelligenceModule));
    }
}
