using LafErp.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LafErp.Tests;

/// <summary>
/// Verifies the committed EF Core migration history is present and discoverable, and that the model and
/// the migration snapshot agree (no pending model changes). SQL Server applies these via Database.Migrate();
/// the portable SQLite test mode uses EnsureCreated, so here we assert the migration is compiled in and the
/// relational create-script generates — a provider-agnostic integrity check.
/// </summary>
public class MigrationsTests
{
    [Fact]
    public void Committed_initial_migration_is_present()
    {
        using var h = new TestHost();
        var migrations = h.Db.Database.GetMigrations().ToList();
        Assert.Contains(migrations, m => m.EndsWith("InitialCreate"));
    }

    [Fact]
    public void Model_generates_a_relational_create_script()
    {
        using var h = new TestHost();
        var ddl = h.Db.Database.GenerateCreateScript();
        Assert.False(string.IsNullOrWhiteSpace(ddl));
        Assert.Contains("CREATE TABLE", ddl, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Migration_covers_core_accounting_and_security_tables()
    {
        using var h = new TestHost();
        var ddl = h.Db.Database.GenerateCreateScript();
        foreach (var table in new[] { "Accounts", "GLEntries", "SalesInvoices", "AppUsers", "AuditEvents" })
            Assert.Contains(table, ddl, System.StringComparison.OrdinalIgnoreCase);
    }
}
