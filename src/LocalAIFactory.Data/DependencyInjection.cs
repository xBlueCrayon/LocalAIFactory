using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Data.Permanence;
using LocalAIFactory.Data.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LocalAIFactory.Data;

public static class DependencyInjection
{
    public static IServiceCollection AddLocalAIFactoryData(this IServiceCollection services, IConfiguration config)
    {
        var cs = config.GetConnectionString("DefaultConnection")
                 ?? "Server=(localdb)\\MSSQLLocalDB;Database=LocalAIFactory;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(cs));
        services.AddScoped<IApiKeyProtector, DataProtectionApiKeyProtector>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IPermanenceGuard, KnowledgePermanenceService>();
        return services;
    }
}
