using LafErp.Core;
using LafErp.Data;
using Microsoft.EntityFrameworkCore;

namespace LafErp.Services;

/// <summary>
/// Real manufacturing depth: BOM-driven production orders with material issue, quality gating, finished-goods
/// receipt, and moving-average production costing. Stock is relieved/received through the StockService so the
/// stock ledger and valuation stay authoritative. Audited throughout; a completed order is immutable.
/// </summary>
public class ManufacturingService
{
    private readonly ErpDbContext _db;
    private readonly StockService _stock;
    private readonly NumberingService _numbering;
    private readonly AuditService _audit;

    public ManufacturingService(ErpDbContext db, StockService stock, NumberingService numbering, AuditService audit)
    { _db = db; _stock = stock; _numbering = numbering; _audit = audit; }

    public Bom CreateBom(string name, int finishedItemId, decimal quantity, IEnumerable<(int ComponentItemId, decimal Qty)> lines)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("BOM name is required.");
        if (quantity <= 0) throw new DomainException("BOM quantity must be positive.");
        var bom = new Bom { Name = name, FinishedItemId = finishedItemId, Quantity = quantity };
        foreach (var l in lines)
        {
            if (l.Qty <= 0) throw new DomainException("BOM component quantity must be positive.");
            bom.Lines.Add(new BomLine { ComponentItemId = l.ComponentItemId, Quantity = l.Qty });
        }
        if (bom.Lines.Count == 0) throw new DomainException("A BOM needs at least one component.");
        _db.Boms.Add(bom);
        _audit.Record("Bom", 0, "Create", name);
        _db.SaveChanges();
        return bom;
    }

    public ProductionOrder CreateOrder(int companyId, int warehouseId, int bomId, decimal quantity)
    {
        if (quantity <= 0) throw new DomainException("Production quantity must be positive.");
        var bom = _db.Boms.Include(b => b.Lines).FirstOrDefault(b => b.Id == bomId)
                  ?? throw new DomainException($"BOM {bomId} not found.");
        var order = new ProductionOrder
        {
            DocNo = _numbering.Next("ProductionOrder"),
            BomId = bomId,
            CompanyId = companyId,
            WarehouseId = warehouseId,
            FinishedItemId = bom.FinishedItemId,
            Quantity = quantity,
            Status = ProductionStatus.Draft
        };
        _db.ProductionOrders.Add(order);
        _audit.Record("ProductionOrder", 0, "Create", order.DocNo);
        _db.SaveChanges();
        return order;
    }

    /// <summary>Issue raw materials for the order (scaled from the BOM), relieving component stock. Blocked if short.</summary>
    public void IssueMaterials(int orderId)
    {
        var order = Get(orderId);
        if (order.Status != ProductionStatus.Draft) throw new DomainException("Materials can only be issued for a Draft order.");
        var bom = _db.Boms.Include(b => b.Lines).First(b => b.Id == order.BomId);
        var runs = order.Quantity / bom.Quantity;
        decimal cost = 0m;
        foreach (var line in bom.Lines)
        {
            var needed = line.Quantity * runs;
            var onHand = _stock.CurrentQty(line.ComponentItemId, order.WarehouseId);
            if (needed > onHand)
                throw new DomainException($"Insufficient material #{line.ComponentItemId}: need {needed}, on hand {onHand}.");
        }
        foreach (var line in bom.Lines)
        {
            var needed = line.Quantity * runs;
            var rate = _stock.ValuationRate(line.ComponentItemId, order.WarehouseId);
            _stock.MoveOut(order.CompanyId, line.ComponentItemId, order.WarehouseId, needed, "ProductionIssue", order.DocNo, DateTime.UtcNow);
            cost += needed * rate;
        }
        order.MaterialCost = Math.Round(cost, 4);
        order.Status = ProductionStatus.MaterialsIssued;
        _audit.Record("ProductionOrder", order.Id, "IssueMaterials", $"cost {order.MaterialCost}");
        _db.SaveChanges();
    }

    /// <summary>Record a quality inspection. A failure blocks completion; a pass allows it.</summary>
    public void InspectQuality(int orderId, bool passed)
    {
        var order = Get(orderId);
        if (order.Status is not (ProductionStatus.MaterialsIssued or ProductionStatus.QualityFailed))
            throw new DomainException("Quality inspection requires materials to be issued first.");
        order.QualityInspected = true;
        order.Status = passed ? ProductionStatus.QualityPassed : ProductionStatus.QualityFailed;
        _audit.Record("ProductionOrder", order.Id, passed ? "QualityPassed" : "QualityFailed", null);
        _db.SaveChanges();
    }

    /// <summary>Complete the order: receive the finished good at the computed unit cost. Requires a quality pass.</summary>
    public void Complete(int orderId)
    {
        var order = Get(orderId);
        if (order.Status == ProductionStatus.Completed) throw new DomainException("Order already completed.");
        if (order.Status != ProductionStatus.QualityPassed)
            throw new DomainException("Order cannot be completed until quality has passed.");
        order.UnitCost = Math.Round(order.MaterialCost / order.Quantity, 4);
        _stock.MoveIn(order.CompanyId, order.FinishedItemId, order.WarehouseId, order.Quantity, order.UnitCost, "ProductionReceipt", order.DocNo, DateTime.UtcNow);
        order.Status = ProductionStatus.Completed;
        _audit.Record("ProductionOrder", order.Id, "Complete", $"unitCost {order.UnitCost}");
        _db.SaveChanges();
    }

    public ProductionOrder Get(int id) =>
        _db.ProductionOrders.FirstOrDefault(o => o.Id == id) ?? throw new DomainException($"ProductionOrder {id} not found.");
}
