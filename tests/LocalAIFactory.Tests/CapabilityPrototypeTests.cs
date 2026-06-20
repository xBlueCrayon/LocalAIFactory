using System.Text;
using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Ingestion.Cheque;
using LocalAIFactory.Ingestion.Documents;
using LocalAIFactory.Workspaces.Autonomy;
using Xunit;

namespace LocalAIFactory.Tests;

// R2-ACC-CAP4/5/6/7: PDF intelligence, cheque-risk triage, deployment artifacts, and the autonomous workspace
// safety skeleton. Every test protects an honesty/safety guarantee, not a happy path.
public class CapabilityPrototypeTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (int i = 0; i < 10 && dir is not null; i++, dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "LocalAIFactory.sln"))) return dir.FullName;
        throw new InvalidOperationException("repo root not found");
    }

    // ---------------- WP4: PDF ----------------
    private static byte[] Ascii(string s) => Encoding.Latin1.GetBytes(s);

    private const string TextPdf =
        "%PDF-1.4\n3 0 obj<</Type /Page/Resources<</Font<</F1 4 0 R>>>>/Contents 5 0 R>>endobj\n" +
        "4 0 obj<</Type/Font/Subtype/Type1/BaseFont/Helvetica>>endobj\n" +
        "5 0 obj<</Length 40>>stream\nBT /F1 12 Tf (Invoice total) Tj ET\nendstream endobj\n" +
        "trailer<</Root 1 0 R/Info<</Title(Invoice)>>>>\n%%EOF";
    private const string ScannedPdf =
        "%PDF-1.4\n3 0 obj<</Type /Page/Resources<</XObject<</Im1 6 0 R>>>>/Contents 5 0 R>>endobj\n" +
        "6 0 obj<</Type/XObject/Subtype/Image/Width 800/Height 600/Filter/DCTDecode/Length 9>>stream\nXXXXXXXXX\nendstream endobj\n%%EOF";

    [Fact]
    public void Pdf_text_based_is_classified_and_hashed()
    {
        var a = new PdfAnalyzer().Analyze(Ascii(TextPdf));
        Assert.True(a.IsPdf);
        Assert.Equal(PdfClass.TextBased, a.Class);
        Assert.Equal(1, a.PageCount);
        Assert.Equal("Invoice", a.Title);
        Assert.True(a.ExtractableText);
        Assert.False(a.OcrRequired);
        Assert.Equal(64, a.DocumentHash.Length);                          // SHA-256 hex = provenance
        Assert.Equal(a.DocumentHash, new PdfAnalyzer().Analyze(Ascii(TextPdf)).DocumentHash); // stable
        Assert.Contains(a.Notes, n => n.Contains("parser library"));      // honest about extraction limits
    }

    [Fact]
    public void Pdf_scanned_requires_ocr_and_non_pdf_is_flagged()
    {
        var scanned = new PdfAnalyzer().Analyze(Ascii(ScannedPdf));
        Assert.Equal(PdfClass.ScannedImageOnly, scanned.Class);
        Assert.True(scanned.OcrRequired);
        Assert.False(scanned.ExtractableText);

        var notPdf = new PdfAnalyzer().Analyze(Ascii("this is not a pdf"));
        Assert.False(notPdf.IsPdf);
        Assert.Equal(PdfClass.NotPdf, notPdf.Class);
    }

    [Fact]
    public void Extractive_summary_preserves_provenance_and_never_invents_text()
    {
        var segs = new List<TextSegment>
        {
            new(1, "Reconciliation matches settlement files to core banking postings every day. Exceptions require maker checker approval before posting."),
            new(2, "The operations dashboard surfaces reconciliation breaks. Operators resolve exceptions before the settlement cut off time.")
        };
        var sum = new ExtractiveSummarizer().Summarize(segs, maxSentences: 2);
        Assert.True(sum.IsExtractive);
        Assert.True(sum.Sentences.Count is >= 1 and <= 2);
        var sourceText = string.Join(" ", segs.Select(s => s.Text));
        foreach (var s in sum.Sentences)
        {
            Assert.Contains(s.Text, sourceText);          // verbatim from source (no hallucination)
            Assert.InRange(s.Page, 1, 2);                  // page provenance retained
        }
        Assert.Empty(new ExtractiveSummarizer().Summarize(new List<TextSegment>()).Sentences);
    }

    // ---------------- WP5: cheque risk ----------------
    private static OcrField F(string? v, double c) => new(v, c);
    private static ChequeOcrResult Ocr(double conf = 0.95) =>
        new(F("100.00", conf), F("one hundred", conf), F("123456789", conf), F("2026-06-21", conf), F("Acme Ltd", conf));

    [Fact]
    public void Cheque_high_forgery_routes_to_human_and_is_never_a_fraud_verdict()
    {
        var sig = new SignatureAnalysis(true, true, VerificationScore: 0.2, ForgeryRiskScore: 0.9, ReferenceSpecimenId: "ref-1");
        var r = new ChequeRiskEngine().Assess(Ocr(), sig);
        Assert.Equal(ChequeTriage.Reject, r.Triage);              // triage, NOT "fraud confirmed"
        Assert.True(r.HumanReviewRequired);
        Assert.Contains(r.RiskFlags, f => f.Contains("forgery"));
        Assert.Contains("not a fraud determination", r.LimitationNote, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Cheque_detection_and_verification_are_separate_signals()
    {
        // detection failure (no signature) vs verification concern (present but risky) produce DISTINCT flags
        var absent = new ChequeRiskEngine().Assess(Ocr(), new SignatureAnalysis(false, false, null, null, null));
        Assert.Contains(absent.RiskFlags, f => f.Contains("not detected") && f.Contains("detection"));

        var risky = new ChequeRiskEngine().Assess(Ocr(), new SignatureAnalysis(true, true, null, 0.7, null));
        Assert.Contains(risky.RiskFlags, f => f.Contains("forgery risk") && f.Contains("verification"));
        Assert.DoesNotContain(risky.RiskFlags, f => f.Contains("not detected"));
        Assert.True(absent.HumanReviewRequired && risky.HumanReviewRequired);
    }

    [Fact]
    public void Cheque_low_confidence_forces_review_clean_item_accepts()
    {
        var lowConf = new ChequeRiskEngine().Assess(Ocr(0.3), new SignatureAnalysis(true, true, 0.9, 0.1, "ref-1"));
        Assert.Equal(ChequeTriage.Review, lowConf.Triage);
        Assert.True(lowConf.HumanReviewRequired);

        var clean = new ChequeRiskEngine().Assess(Ocr(0.97), new SignatureAnalysis(true, true, 0.95, 0.05, "ref-1"), declaredAmount: 100m);
        Assert.Equal(ChequeTriage.Accept, clean.Triage);
        Assert.Empty(clean.RiskFlags);
        Assert.False(clean.HumanReviewRequired);
        Assert.False(string.IsNullOrWhiteSpace(clean.LimitationNote)); // limitation always disclosed
    }

    // ---------------- WP7: autonomous command policy + planner ----------------
    [Theory]
    [InlineData("git reset --hard HEAD~1")]
    [InlineData("DROP DATABASE LocalAIFactory")]
    [InlineData("rm -rf /data")]
    [InlineData("git push --force origin main")]
    [InlineData("git merge main")]
    [InlineData("iisreset /restart")]
    [InlineData("truncate table Orders")]
    public void Policy_denies_dangerous_commands(string cmd)
        => Assert.Equal(CommandDecision.Denied, new CommandPolicy().Classify(cmd).Decision);

    [Theory]
    [InlineData("dotnet build LocalAIFactory.sln -c Release")]
    [InlineData("dotnet test tests/LocalAIFactory.Tests/LocalAIFactory.Tests.csproj")]
    [InlineData("git status")]
    public void Policy_allows_safe_build_and_read_commands(string cmd)
        => Assert.Equal(CommandDecision.Allowed, new CommandPolicy().Classify(cmd).Decision);

    [Theory]
    [InlineData("git commit -m \"x\"")]
    [InlineData("git push origin branch")]
    [InlineData("dotnet ef migrations add Foo")]
    [InlineData("some-unknown-tool --go")]
    public void Policy_requires_approval_for_state_changes_and_unknowns(string cmd)
        => Assert.Equal(CommandDecision.RequiresApproval, new CommandPolicy().Classify(cmd).Decision);

    [Fact]
    public void Planner_is_dry_run_and_gates_commit_push_and_executes_nothing()
    {
        var planner = new AutonomousWorkflowPlanner(new CommandPolicy());
        var plan = planner.Plan(new ChangeRequest("CR-1", "Fix a null check", "desc", "."));
        Assert.True(plan.DryRun);
        Assert.True(plan.RequiresHumanApprovalBeforeCommit);
        Assert.All(plan.Steps, s => Assert.NotEqual(CommandDecision.Denied, s.CommandDecision)); // no denied command planned
        Assert.True(plan.Steps.Single(s => s.Kind == PlanStepKind.Commit).RequiresApproval);
        Assert.True(plan.Steps.Single(s => s.Kind == PlanStepKind.Push).RequiresApproval);
        var report = planner.GenerateReport(plan);
        Assert.Contains("APPROVAL REQUIRED", report);
        Assert.Contains("Dry-run", report, StringComparison.OrdinalIgnoreCase);
    }

    // ---------------- WP6: deployment artifacts exist + no secrets ----------------
    [Theory]
    [InlineData("deploy/docker-compose.cpu.yml")]
    [InlineData("deploy/docker-compose.gpu.yml")]
    [InlineData("deploy/.env.example")]
    [InlineData("deploy/Dockerfile")]
    [InlineData("deploy/scripts/backup.ps1")]
    [InlineData("deploy/scripts/restore-verify.ps1")]
    [InlineData("deploy/scripts/health-check.ps1")]
    [InlineData("deploy/scripts/deploy-smoke.ps1")]
    [InlineData("deploy/scripts/windows-deploy.ps1")]
    public void Deployment_artifact_exists(string rel)
    {
        var path = Path.Combine(new[] { RepoRoot() }.Concat(rel.Split('/')).ToArray());
        Assert.True(File.Exists(path), $"missing {rel}");
    }

    [Fact]
    public void Env_example_uses_a_placeholder_not_a_real_secret()
    {
        var env = File.ReadAllText(Path.Combine(RepoRoot(), "deploy", ".env.example"));
        Assert.Contains("CHANGE_ME", env);                          // password is a placeholder
        Assert.Contains("${MSSQL_SA_PASSWORD}", env);               // referenced, not duplicated as a literal
    }
}
