namespace LafErp.Core;

/// <summary>Manufacturing lifecycle states for a production order.</summary>
public enum ProductionStatus { Draft, MaterialsIssued, QualityPassed, QualityFailed, Completed, Cancelled }

/// <summary>
/// Bill of materials: the component recipe to produce one unit of a finished item. Real manufacturing
/// depth (beyond the catalog stub): a BOM header plus typed component lines drive material issue + costing.
/// </summary>
public class Bom : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public int FinishedItemId { get; set; }
    public decimal Quantity { get; set; } = 1m; // units produced per BOM run
    public bool IsActive { get; set; } = true;
    public List<BomLine> Lines { get; set; } = new();
}

public class BomLine : EntityBase
{
    public int BomId { get; set; }
    public int ComponentItemId { get; set; }
    public decimal Quantity { get; set; } // component qty per BOM run
}

/// <summary>
/// A production order: consume raw materials per a BOM and receive a finished good. Enforces a real
/// lifecycle — materials must be issued (and not exceed stock), quality must pass, and a completed order
/// is immutable. Production cost is the moving-average value of the materials actually issued.
/// </summary>
public class ProductionOrder : EntityBase
{
    public string DocNo { get; set; } = string.Empty;
    public int BomId { get; set; }
    public int CompanyId { get; set; }
    public int WarehouseId { get; set; }
    public int FinishedItemId { get; set; }
    public decimal Quantity { get; set; } // finished units to produce
    public ProductionStatus Status { get; set; } = ProductionStatus.Draft;
    public decimal MaterialCost { get; set; }   // accumulated cost of issued materials
    public decimal UnitCost { get; set; }        // MaterialCost / Quantity at completion
    public bool QualityInspected { get; set; }
}
