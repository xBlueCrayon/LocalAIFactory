using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LafErp.Data;

/// <summary>
/// Design-time factory so `dotnet ef migrations add` / `dotnet ef database update` can build the context
/// for the SQL Server provider without starting the web host. The connection string here is a placeholder
/// used only for model/SQL generation; the running app supplies the real one via configuration.
/// </summary>
public sealed class ErpDbContextFactory : IDesignTimeDbContextFactory<ErpDbContext>
{
    public ErpDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("LAFERP_MIGRATION_CONNECTION")
                 ?? "Server=.;Database=LafErpGold;Trusted_Connection=True;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<ErpDbContext>().UseSqlServer(cs).Options;
        return new ErpDbContext(options);
    }
}
