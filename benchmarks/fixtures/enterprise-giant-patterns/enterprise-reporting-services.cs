// FINAL-ENTERPRISE-REASONING: synthetic reporting service layer (ORIGINAL, committed — NOT a vendor clone).
// Power BI / Tableau-style operations and finance dashboards. Each method names the SQL objects its report reads,
// so the C#<->SQL bridge can answer "which reports break if table X changes" and "what does this dashboard depend on".
using Microsoft.EntityFrameworkCore;

namespace EnterprisePatterns.Services
{
    // The operations-manager dashboard: open incidents, SLA breaches, and payment pipeline at a glance.
    public class OperationsReportingService
    {
        private readonly DbContext _db;
        public OperationsReportingService(DbContext db) { _db = db; }
        public void OperationsDashboard()
        {
            _db.Database.ExecuteSqlRaw("SELECT COUNT(*) AS OpenIncidents FROM dbo.Incident WHERE State <> 'Resolved'");
            _db.Database.ExecuteSqlRaw("SELECT COUNT(*) AS Breached FROM dbo.SlaBreach");
            _db.Database.ExecuteSqlRaw("SELECT State, COUNT(*) AS N FROM dbo.PaymentInstruction GROUP BY State");
            _db.Database.ExecuteSqlRaw("SELECT TOP 1 * FROM dbo.OperationsDailySnapshot ORDER BY SnapshotDate DESC");
        }
        public void StockMovementReport() => _db.Database.ExecuteSqlRaw(
            "SELECT ItemId, SUM(Delta) AS Net FROM dbo.StockMovement GROUP BY ItemId");
    }

    // The finance dashboard: GL journal and AR/invoice position.
    public class FinanceReportingService
    {
        private readonly DbContext _db;
        public FinanceReportingService(DbContext db) { _db = db; }
        public void FinanceDashboard()
        {
            _db.Database.ExecuteSqlRaw("SELECT GlAccountId, SUM(Debit) - SUM(Credit) AS Net FROM dbo.GlJournal GROUP BY GlAccountId");
            _db.Database.ExecuteSqlRaw("SELECT COUNT(*) AS Unposted FROM dbo.Invoice WHERE Posted = 0");
        }
        public void ProcurementReport()
        {
            _db.Database.ExecuteSqlRaw("SELECT Status, COUNT(*) AS N FROM dbo.PurchaseOrder GROUP BY Status");
            _db.Database.ExecuteSqlRaw("SELECT PurchaseOrderId, SUM(ReceivedQty) AS Qty FROM dbo.GoodsReceipt GROUP BY PurchaseOrderId");
        }
    }
}
