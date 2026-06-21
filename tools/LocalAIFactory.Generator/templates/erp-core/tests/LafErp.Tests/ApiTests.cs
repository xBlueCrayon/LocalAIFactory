using System.Net;
using System.Net.Http.Json;
using LafErp.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace LafErp.Tests;

/// <summary>Boots the real ASP.NET Core app over an isolated SQLite file and exercises the REST API + UI routes.</summary>
public class ApiTests : IClassFixture<ApiTests.Factory>
{
    private readonly Factory _factory;
    public ApiTests(Factory factory) => _factory = factory;

    public class Factory : WebApplicationFactory<Program>
    {
        private readonly string _db = Path.Combine(Path.GetTempPath(), $"laferp-api-{Guid.NewGuid():N}.db");
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(DbContextOptions<ErpDbContext>));
                services.AddDbContext<ErpDbContext>(o => o.UseSqlite($"Data Source={_db}"));
            });
        }
    }

    [Theory]
    [InlineData("/api/health")]
    [InlineData("/api/customers")]
    [InlineData("/api/suppliers")]
    [InlineData("/api/items")]
    [InlineData("/api/sales-invoices")]
    [InlineData("/api/purchase-invoices")]
    [InlineData("/api/payments")]
    [InlineData("/api/stock-ledger")]
    [InlineData("/api/reports/trial-balance?companyId=1")]
    [InlineData("/api/reports/stock-balance")]
    [InlineData("/api/reports/ar-ap?companyId=1")]
    [InlineData("/api/workflows")]
    [InlineData("/api/audit")]
    public async Task Api_endpoints_return_200(string url)
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/Home/Customers")]
    [InlineData("/Home/Items")]
    [InlineData("/Home/SalesInvoices")]
    [InlineData("/Home/GeneralLedger")]
    [InlineData("/Home/StockBalance")]
    [InlineData("/Home/WorkflowInbox")]
    [InlineData("/Home/AuditLog")]
    [InlineData("/Home/Login")]
    public async Task Ui_pages_render(string url)
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var html = await resp.Content.ReadAsStringAsync();
        Assert.Contains("{{PRODUCT_NAME}}", html);
    }

    [Fact]
    public async Task Demo_data_populates_trial_balance_and_it_balances()
    {
        var client = _factory.CreateClient();
        var rows = await client.GetFromJsonAsync<List<TbRow>>("/api/reports/trial-balance?companyId=1");
        Assert.NotNull(rows);
        Assert.NotEmpty(rows!);
        Assert.Equal(rows!.Sum(r => r.Debit), rows.Sum(r => r.Credit));
    }

    [Fact]
    public async Task Create_then_submit_sales_invoice_via_api()
    {
        var client = _factory.CreateClient();
        // Customer 1, warehouse 1, widget item 1 already has stock from demo purchase.
        var dto = new
        {
            companyId = 1, customerId = 1, warehouseId = 1,
            lines = new[] { new { itemId = 1, qty = 1, rate = 100, taxRatePercent = 0 } }
        };
        var create = await client.PostAsJsonAsync("/api/sales-invoices", dto);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<CreatedInvoice>();
        var submit = await client.PostAsync($"/api/sales-invoices/{created!.Id}/submit", null);
        Assert.Equal(HttpStatusCode.OK, submit.StatusCode);
    }

    [Fact]
    public async Task Insufficient_stock_returns_400_not_500()
    {
        var client = _factory.CreateClient();
        // qty 50 >> on-hand (~5), total 500 is under the 1000 threshold so submit auto-approves and posts,
        // which triggers the insufficient-stock domain rule on the stock-out.
        var dto = new
        {
            companyId = 1, customerId = 1, warehouseId = 1,
            lines = new[] { new { itemId = 1, qty = 50, rate = 10, taxRatePercent = 0 } }
        };
        var create = await client.PostAsJsonAsync("/api/sales-invoices", dto);
        var created = await create.Content.ReadFromJsonAsync<CreatedInvoice>();
        var submit = await client.PostAsync($"/api/sales-invoices/{created!.Id}/submit", null);
        Assert.Equal(HttpStatusCode.BadRequest, submit.StatusCode); // domain rule -> 400, not a crash
    }

    private record TbRow(string Account, decimal Debit, decimal Credit);
    private record CreatedInvoice(int Id, string DocNo, decimal GrandTotal);
}
