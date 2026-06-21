using LafErp.Core;
using LafErp.Data;
using Microsoft.EntityFrameworkCore;

namespace LafErp.Services;

/// <summary>Generates document numbers from a per-doctype series (clean-room naming series).</summary>
public class NumberingService
{
    private readonly ErpDbContext _db;
    public NumberingService(ErpDbContext db) => _db = db;

    public string Next(string docType)
    {
        var series = _db.NumberingSeries.FirstOrDefault(s => s.DocType == docType);
        if (series is null)
        {
            series = new NumberingSeries { DocType = docType, Prefix = docType[..Math.Min(4, docType.Length)].ToUpperInvariant() + "-", NextNumber = 1, Padding = 5 };
            _db.NumberingSeries.Add(series);
        }
        var number = series.NextNumber;
        series.NextNumber++;
        return $"{series.Prefix}{number.ToString().PadLeft(series.Padding, '0')}";
    }
}
