// R2-ACC-CAP2: synthetic C#↔SQL bridge fixture (original, committed — not third-party source).
// Exercises the deterministic SQL-in-C# detection: raw SQL strings, FromSqlRaw/ExecuteSqlRaw, and EXEC.
using Microsoft.EntityFrameworkCore;

namespace Bridge.Data
{
    public class OrderRepository
    {
        private readonly DbContext _db;
        public OrderRepository(DbContext db) { _db = db; }

        // Raw SELECT against dbo.Orders -> AccessesSql(OrderRepository.GetOrders -> dbo.Orders)
        public void GetOrders()
        {
            _db.Database.ExecuteSqlRaw("SELECT Id, CustomerId, Total FROM dbo.Orders WHERE CustomerId = 1");
        }

        // SELECT against dbo.Customers via a local string -> AccessesSql(... -> dbo.Customers)
        public void GetCustomers()
        {
            var sql = "SELECT Id, Name FROM dbo.Customers ORDER BY Name";
            _db.Database.ExecuteSqlRaw(sql);
        }

        // EXEC of a stored procedure -> AccessesSql(... -> dbo.usp_GetCustomerOrders) at higher confidence
        public void GetCustomerOrders()
        {
            _db.Database.ExecuteSqlRaw("EXEC dbo.usp_GetCustomerOrders @CustomerId = 1");
        }
    }
}
