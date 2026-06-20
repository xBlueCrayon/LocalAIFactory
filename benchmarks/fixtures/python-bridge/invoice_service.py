# R2-ACC-CAP3: synthetic Python↔SQL bridge fixture (original, committed — not third-party source).
# Exercises the deterministic Python extractor: classes, methods, async, FastAPI route, and SQL-in-string bridge.
from fastapi import APIRouter

router = APIRouter()


class InvoiceService:
    def __init__(self, db):
        self.db = db

    async def get_invoices(self):
        # SELECT against dbo.Invoices -> AccessesSql(InvoiceService.get_invoices -> dbo.Invoices)
        return self.db.execute("SELECT Id, Amount FROM dbo.Invoices WHERE Amount > 0")

    def post_invoice(self, invoice_id):
        # EXEC of a stored procedure -> AccessesSql(... -> dbo.usp_PostInvoice)
        return self.db.execute("EXEC dbo.usp_PostInvoice @Id = 1")


@router.get("/invoices")
def list_invoices(service: InvoiceService):
    return service.get_invoices()
