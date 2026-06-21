using Microsoft.Extensions.DependencyInjection;

namespace LafErp.Services;

public static class ServiceRegistration
{
    /// <summary>Registers all ERP domain services. DbContext + ICurrentUser are registered by the host.</summary>
    public static IServiceCollection AddErpServices(this IServiceCollection services)
    {
        services.AddScoped<AuditService>();
        services.AddScoped<UserAuthService>();
        services.AddScoped<NumberingService>();
        services.AddScoped<WorkflowService>();
        services.AddScoped<StockService>();
        services.AddScoped<AccountingService>();
        services.AddScoped<SalesService>();
        services.AddScoped<PurchaseService>();
        services.AddScoped<PaymentService>();
        services.AddScoped<JournalService>();
        services.AddScoped<RbacService>();
        services.AddScoped<ImportService>();
        services.AddScoped<CrmService>();
        services.AddScoped<ProjectService>();
        services.AddScoped<SupportService>();
        services.AddScoped<AssetService>();
        return services;// __CATALOG_SERVICES__
        
    }
}
