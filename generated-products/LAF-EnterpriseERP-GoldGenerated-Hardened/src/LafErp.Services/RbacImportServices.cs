using System.Globalization;
using LafErp.Core;
using LafErp.Data;
using Microsoft.EntityFrameworkCore;

namespace LafErp.Services;

/// <summary>Role-based authorization check against the role-permission matrix.</summary>
public class RbacService
{
    private readonly ErpDbContext _db;
    private readonly ICurrentUser _user;
    public RbacService(ErpDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public bool Can(string docType, string action)
    {
        var perms = _db.RolePermissions.Where(p => p.DocType == docType && _user.Roles.Contains(p.RoleName)).ToList();
        if (perms.Count == 0) return false;
        return action.ToLowerInvariant() switch
        {
            "read" => perms.Any(p => p.CanRead),
            "create" => perms.Any(p => p.CanCreate),
            "write" => perms.Any(p => p.CanWrite),
            "submit" => perms.Any(p => p.CanSubmit),
            "approve" => perms.Any(p => p.CanApprove),
            "cancel" => perms.Any(p => p.CanCancel),
            _ => false
        };
    }

    public void Demand(string docType, string action)
    {
        if (!Can(docType, action))
            throw new DomainException($"User '{_user.Username}' is not authorized to {action} {docType}.");
    }
}

/// <summary>CSV import producing an ImportBatch with per-row error capture.</summary>
public class ImportService
{
    private readonly ErpDbContext _db;
    private readonly AuditService _audit;
    public ImportService(ErpDbContext db, AuditService audit) { _db = db; _audit = audit; }

    /// <summary>Imports customers from CSV text with header: Code,Name,Email,ReceivableAccountId.</summary>
    public ImportBatch ImportCustomers(string fileName, string csv)
    {
        var batch = new ImportBatch { DocType = "Customer", FileName = fileName };
        var errors = new List<string>();
        var lines = csv.Replace("\r", "").Split('\n', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 1; i < lines.Length; i++) // skip header
        {
            batch.TotalRows++;
            var cols = lines[i].Split(',');
            try
            {
                if (cols.Length < 4) throw new DomainException("expected 4 columns");
                var code = cols[0].Trim();
                if (string.IsNullOrWhiteSpace(code)) throw new DomainException("missing code");
                if (_db.Customers.IgnoreQueryFilters().Any(c => c.Code == code)) throw new DomainException($"duplicate code {code}");
                _db.Customers.Add(new Customer
                {
                    Code = code, Name = cols[1].Trim(), Email = cols[2].Trim(),
                    ReceivableAccountId = int.Parse(cols[3].Trim(), CultureInfo.InvariantCulture)
                });
                batch.ImportedRows++;
            }
            catch (Exception ex)
            {
                batch.FailedRows++;
                errors.Add($"row {i}: {ex.Message}");
            }
        }
        batch.Errors = errors.Count == 0 ? null : string.Join("; ", errors);
        _db.ImportBatches.Add(batch);
        _audit.Record("ImportBatch", 0, "Import", $"{batch.DocType}: {batch.ImportedRows}/{batch.TotalRows} imported, {batch.FailedRows} failed");
        _db.SaveChanges();
        return batch;
    }

    public string ExportCustomersCsv()
    {
        var rows = _db.Customers.OrderBy(c => c.Code)
            .Select(c => $"{c.Code},{c.Name},{c.Email},{c.ReceivableAccountId}").ToList();
        return "Code,Name,Email,ReceivableAccountId\n" + string.Join("\n", rows);
    }
}
