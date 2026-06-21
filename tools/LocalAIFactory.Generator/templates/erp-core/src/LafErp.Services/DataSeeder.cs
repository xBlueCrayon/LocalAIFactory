using LafErp.Core;
using LafErp.Data;
using Microsoft.EntityFrameworkCore;

namespace LafErp.Services;

/// <summary>Handy ids produced by the demo seed, for tests and the demo UI.</summary>
public record SeedResult(
    int CompanyId, int WarehouseId, int CustomerId, int SupplierId,
    int WidgetItemId, int GadgetItemId,
    int CashAccountId, int BankAccountId, int DebtorsAccountId, int CreditorsAccountId,
    int SalesAccountId, int CogsAccountId, int StockAccountId, int TaxAccountId);

/// <summary>
/// Seeds a complete, internally-consistent demo company: currency, fiscal year, chart of accounts,
/// warehouse, tax, items, parties, roles, role-permission matrix, approval workflows and demo users.
/// Idempotent: does nothing if a company already exists.
/// </summary>
public static class DataSeeder
{
    public static SeedResult Seed(ErpDbContext db)
    {
        if (db.Companies.Any())
            return Existing(db);

        var usd = new Currency { Code = "USD", Name = "US Dollar", Symbol = "$" };
        db.Currencies.Add(usd);
        db.SaveChanges();

        var company = new Company { Name = "LAF Demo Corp", Abbreviation = "LAF", DefaultCurrencyId = usd.Id };
        db.Companies.Add(company);
        db.SaveChanges();

        db.FiscalYears.Add(new FiscalYear { Name = "2026", StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 12, 31) });

        // Chart of accounts (group + leaf).
        Account Grp(string code, string name, RootType rt) => Add(db, company.Id, code, name, rt, true, null);
        var assets = Grp("1000", "Assets", RootType.Asset);
        var liabilities = Grp("2000", "Liabilities", RootType.Liability);
        var equity = Grp("3000", "Equity", RootType.Equity);
        var income = Grp("4000", "Income", RootType.Income);
        var expense = Grp("5000", "Expenses", RootType.Expense);
        db.SaveChanges();

        var cash = Add(db, company.Id, "1100", "Cash", RootType.Asset, false, assets.Id);
        var bank = Add(db, company.Id, "1110", "Bank", RootType.Asset, false, assets.Id);
        var debtors = Add(db, company.Id, "1200", "Debtors", RootType.Asset, false, assets.Id, PartyType.Customer);
        var stock = Add(db, company.Id, "1300", "Stock In Hand", RootType.Asset, false, assets.Id);
        var creditors = Add(db, company.Id, "2100", "Creditors", RootType.Liability, false, liabilities.Id, PartyType.Supplier);
        var tax = Add(db, company.Id, "2200", "Tax Payable", RootType.Liability, false, liabilities.Id);
        Add(db, company.Id, "3100", "Retained Earnings", RootType.Equity, false, equity.Id);
        var sales = Add(db, company.Id, "4100", "Sales", RootType.Income, false, income.Id);
        var cogs = Add(db, company.Id, "5100", "Cost of Goods Sold", RootType.Expense, false, expense.Id);
        Add(db, company.Id, "5200", "Operating Expenses", RootType.Expense, false, expense.Id);
        db.SaveChanges();

        var warehouse = new Warehouse { CompanyId = company.Id, Code = "MAIN", Name = "Main Store", StockAccountId = stock.Id };
        db.Warehouses.Add(warehouse);
        db.TaxTemplates.Add(new TaxTemplate { CompanyId = company.Id, Name = "Standard 10%", RatePercent = 10m, TaxAccountId = tax.Id });
        var grp = new ItemGroup { Name = "Products" };
        db.ItemGroups.Add(grp);
        db.SaveChanges();

        var widget = new Item { Code = "WIDGET", Name = "Widget", ItemGroupId = grp.Id, Uom = "Nos", StandardRate = 100, StandardBuyRate = 60, IncomeAccountId = sales.Id, ExpenseAccountId = cogs.Id };
        var gadget = new Item { Code = "GADGET", Name = "Gadget", ItemGroupId = grp.Id, Uom = "Nos", StandardRate = 250, StandardBuyRate = 150, IncomeAccountId = sales.Id, ExpenseAccountId = cogs.Id };
        db.Items.AddRange(widget, gadget);
        var customer = new Customer { Code = "CUST-0001", Name = "Acme Industries", Email = "ap@acme.test", ReceivableAccountId = debtors.Id, CreditLimit = 100000 };
        var supplier = new Supplier { Code = "SUPP-0001", Name = "Globex Supplies", Email = "ar@globex.test", PayableAccountId = creditors.Id };
        db.Customers.Add(customer);
        db.Suppliers.Add(supplier);
        db.SaveChanges();

        SeedRolesAndWorkflows(db);

        return new SeedResult(company.Id, warehouse.Id, customer.Id, supplier.Id, widget.Id, gadget.Id,
            cash.Id, bank.Id, debtors.Id, creditors.Id, sales.Id, cogs.Id, stock.Id, tax.Id);
    }

    private static void SeedRolesAndWorkflows(ErpDbContext db)
    {
        foreach (var r in new[] { "System Manager", "Accounts User", "Accounts Manager", "Stock User", "Sales User", "Purchase User" })
            db.AppRoles.Add(new AppRole { Name = r });

        var admin = new AppUser { Username = "admin", FullName = "System Administrator" };
        var alice = new AppUser { Username = "alice", FullName = "Alice (Sales)" };
        var bob = new AppUser { Username = "bob", FullName = "Bob (Accounts Manager)" };
        db.AppUsers.AddRange(admin, alice, bob);
        db.SaveChanges();

        void Perm(string role, string doc, bool create, bool write, bool submit, bool approve, bool cancel) =>
            db.RolePermissions.Add(new RolePermission { RoleName = role, DocType = doc, CanRead = true, CanCreate = create, CanWrite = write, CanSubmit = submit, CanApprove = approve, CanCancel = cancel });

        foreach (var doc in new[] { "SalesInvoice", "SalesOrder" })
        {
            Perm("Sales User", doc, true, true, true, false, false);
            Perm("Accounts Manager", doc, false, false, true, true, true);
        }
        foreach (var doc in new[] { "PurchaseInvoice", "PurchaseOrder" })
        {
            Perm("Purchase User", doc, true, true, true, false, false);
            Perm("Accounts Manager", doc, false, false, true, true, true);
        }
        foreach (var doc in new[] { "PaymentEntry", "JournalEntry" })
        {
            Perm("Accounts User", doc, true, true, true, false, false);
            Perm("Accounts Manager", doc, false, false, true, true, true);
        }

        // Approval workflows: amounts above threshold require a separate Accounts Manager (maker≠checker).
        WorkflowDefinition Wf(string doc, string submitRole, decimal threshold) => new()
        {
            DocType = doc, Name = doc + " Approval", SubmitRole = submitRole, ApproverRole = "Accounts Manager",
            ApprovalThreshold = threshold, MakerCannotApprove = true
        };
        db.WorkflowDefinitions.Add(Wf("SalesInvoice", "Sales User", 1000m));
        db.WorkflowDefinitions.Add(Wf("PurchaseInvoice", "Purchase User", 1000m));
        db.WorkflowDefinitions.Add(Wf("PaymentEntry", "Accounts User", 500m));
        db.WorkflowDefinitions.Add(Wf("JournalEntry", "Accounts User", 0m));
        db.SaveChanges();
    }

    private static Account Add(ErpDbContext db, int companyId, string code, string name, RootType rt, bool isGroup, int? parentId, PartyType? party = null)
    {
        var a = new Account { CompanyId = companyId, Code = code, Name = name, RootType = rt, IsGroup = isGroup, ParentAccountId = parentId, PartyTypeRequired = party };
        db.Accounts.Add(a);
        return a;
    }

    private static SeedResult Existing(ErpDbContext db)
    {
        int Acc(string name) => db.Accounts.Where(a => a.Name == name).Select(a => a.Id).First();
        var company = db.Companies.First();
        var wh = db.Warehouses.First();
        return new SeedResult(company.Id, wh.Id,
            db.Customers.IgnoreQueryFilters().First().Id, db.Suppliers.IgnoreQueryFilters().First().Id,
            db.Items.IgnoreQueryFilters().First(i => i.Code == "WIDGET").Id,
            db.Items.IgnoreQueryFilters().First(i => i.Code == "GADGET").Id,
            Acc("Cash"), Acc("Bank"), Acc("Debtors"), Acc("Creditors"),
            Acc("Sales"), Acc("Cost of Goods Sold"), Acc("Stock In Hand"), Acc("Tax Payable"));
    }
}
