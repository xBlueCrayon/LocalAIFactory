using LafErp.Core;
using LafErp.Data;
using Microsoft.EntityFrameworkCore;

namespace LafErp.Services;

public record StockBalance(int ItemId, int WarehouseId, decimal Qty, decimal Value, decimal ValuationRate);

/// <summary>
/// Stock ledger: every movement is an immutable signed entry. Running quantity and a moving-average
/// valuation are carried forward per item+warehouse. Outward movements may not drive on-hand negative
/// when AllowNegativeStock is false (the default), mirroring a standard inventory control.
/// </summary>
public class StockService
{
    private readonly ErpDbContext _db;
    public bool AllowNegativeStock { get; set; } = false;

    public StockService(ErpDbContext db) => _db = db;

    private StockLedgerEntry? Last(int itemId, int warehouseId) =>
        _db.StockLedgerEntries
           .Where(s => s.ItemId == itemId && s.WarehouseId == warehouseId)
           .OrderByDescending(s => s.Id)
           .FirstOrDefault();

    public decimal CurrentQty(int itemId, int warehouseId) => Last(itemId, warehouseId)?.QtyAfter ?? 0m;

    public decimal ValuationRate(int itemId, int warehouseId)
    {
        var last = Last(itemId, warehouseId);
        if (last is null || last.QtyAfter <= 0) return 0m;
        return Math.Round(last.ValueAfter / last.QtyAfter, 4);
    }

    public StockBalance Balance(int itemId, int warehouseId)
    {
        var last = Last(itemId, warehouseId);
        var qty = last?.QtyAfter ?? 0m;
        var val = last?.ValueAfter ?? 0m;
        return new StockBalance(itemId, warehouseId, qty, val, qty > 0 ? Math.Round(val / qty, 4) : 0m);
    }

    /// <summary>Post an inward movement (receipt) at the supplied incoming rate.</summary>
    public StockLedgerEntry MoveIn(int companyId, int itemId, int warehouseId, decimal qty, decimal incomingRate, string voucherType, string voucherNo, DateTime postingDate)
    {
        if (qty <= 0) throw new DomainException("Inward quantity must be positive.");
        var last = Last(itemId, warehouseId);
        var prevQty = last?.QtyAfter ?? 0m;
        var prevVal = last?.ValueAfter ?? 0m;
        var newQty = prevQty + qty;
        var newVal = prevVal + qty * incomingRate;
        return Write(companyId, itemId, warehouseId, qty, incomingRate, newQty, newVal, voucherType, voucherNo, postingDate, false);
    }

    /// <summary>Post an outward movement (issue/sale) at the current moving-average valuation rate.</summary>
    public StockLedgerEntry MoveOut(int companyId, int itemId, int warehouseId, decimal qty, string voucherType, string voucherNo, DateTime postingDate)
    {
        if (qty <= 0) throw new DomainException("Outward quantity must be positive.");
        var last = Last(itemId, warehouseId);
        var prevQty = last?.QtyAfter ?? 0m;
        var prevVal = last?.ValueAfter ?? 0m;
        if (!AllowNegativeStock && qty > prevQty)
            throw new DomainException($"Insufficient stock for item #{itemId} in warehouse #{warehouseId}: on hand {prevQty}, requested {qty}.");
        var rate = prevQty > 0 ? prevVal / prevQty : 0m;
        var newQty = prevQty - qty;
        var newVal = prevVal - qty * rate;
        return Write(companyId, itemId, warehouseId, -qty, Math.Round(rate, 4), newQty, Math.Round(newVal, 4), voucherType, voucherNo, postingDate, false);
    }

    /// <summary>Reverse all movements of a voucher (used on cancellation).</summary>
    public void ReverseVoucher(string voucherType, string voucherNo)
    {
        var entries = _db.StockLedgerEntries
            .Where(s => s.VoucherType == voucherType && s.VoucherNo == voucherNo && !s.IsReversal)
            .ToList();
        foreach (var e in entries)
        {
            if (e.QtyChange < 0)
                MoveIn(e.CompanyId, e.ItemId, e.WarehouseId, -e.QtyChange, e.ValuationRate, voucherType, voucherNo + "-REV", e.PostingDate).IsReversal = true;
            else
                MoveOut(e.CompanyId, e.ItemId, e.WarehouseId, e.QtyChange, voucherType, voucherNo + "-REV", e.PostingDate).IsReversal = true;
        }
    }

    private StockLedgerEntry Write(int companyId, int itemId, int warehouseId, decimal qtyChange, decimal rate, decimal qtyAfter, decimal valueAfter, string vt, string vn, DateTime date, bool reversal)
    {
        var sle = new StockLedgerEntry
        {
            CompanyId = companyId,
            ItemId = itemId,
            WarehouseId = warehouseId,
            PostingDate = date,
            QtyChange = qtyChange,
            ValuationRate = rate,
            QtyAfter = qtyAfter,
            ValueAfter = valueAfter,
            VoucherType = vt,
            VoucherNo = vn,
            IsReversal = reversal
        };
        _db.StockLedgerEntries.Add(sle);
        return sle;
    }
}
