using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Enums;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace LocalAIFactory.Ingestion.Symbols;

// KE-009: deterministic T-SQL schema extractor. Offline syntax-only — it parses script text into a ScriptDom
// AST and walks DDL declarations. It NEVER opens a connection, reads sys.* or resolves a live catalog;
// cross-object resolution is KE-010's job. Parses per batch (split on GO) so one malformed batch can't lose
// the rest of the file. Emits object-scoped schema symbols (schema/tables/columns/views/procs/functions/
// triggers/constraints/FKs/indexes) plus the references they make (staging for KE-010). DML statements yield
// no symbols — that is the DDL/DML classification.
public sealed class TSqlSchemaExtractor : ISqlSchemaExtractor
{
    public string Dialect => "tsql";

    public SqlExtractionResult Extract(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return SqlExtractionResult.Empty;

        // Strip any BOM characters (leading, or embedded by concatenation/editors) — ScriptDom treats an
        // unexpected U+FEFF as a syntax error and would drop the whole batch that follows it.
        content = content.Replace("﻿", string.Empty);

        var symbols = new List<ExtractedSqlSymbol>();
        var refs = new List<ExtractedSqlReference>();

        // Batch isolation: GO is a client batch separator, not T-SQL. Parsing each batch independently means a
        // syntax error in one batch (R1) does not discard the symbols in the others.
        foreach (var (batchText, baseLine) in SplitBatches(content))
        {
            if (string.IsNullOrWhiteSpace(batchText)) continue;
            var parser = new TSql160Parser(true); // initialQuotedIdentifiers: true
            TSqlFragment fragment;
            try { fragment = parser.Parse(new StringReader(batchText), out _); }
            catch { continue; } // never throw on malformed SQL
            if (fragment is null) continue;

            var walker = new SchemaWalker(batchText, baseLine, symbols, refs);
            try { fragment.Accept(walker); } catch { /* best-effort: keep what we collected */ }
        }

        return new SqlExtractionResult(symbols, refs);
    }

    // Split on lines consisting only of GO (optionally followed by whitespace/comment), case-insensitive.
    // Tracks each batch's starting line so symbols keep file-relative line provenance. Offsets are batch-local
    // (the store/graph key on object identity, not byte offset, so absolute byte offset is not load-bearing).
    private static IEnumerable<(string text, int baseLine)> SplitBatches(string content)
    {
        var lines = content.Split('\n');
        var sb = new System.Text.StringBuilder();
        int batchStartLine = 1, lineNo = 0;
        foreach (var raw in lines)
        {
            lineNo++;
            var trimmed = raw.Trim();
            var isGo = trimmed.Equals("GO", StringComparison.OrdinalIgnoreCase)
                       || trimmed.StartsWith("GO ", StringComparison.OrdinalIgnoreCase)
                       || trimmed.StartsWith("GO\t", StringComparison.OrdinalIgnoreCase)
                       || trimmed.StartsWith("GO--", StringComparison.OrdinalIgnoreCase);
            if (isGo)
            {
                yield return (sb.ToString(), batchStartLine);
                sb.Clear();
                batchStartLine = lineNo + 1;
            }
            else
            {
                sb.Append(raw).Append('\n');
            }
        }
        if (sb.Length > 0) yield return (sb.ToString(), batchStartLine);
    }

    // Walks one parsed batch, emitting symbols and references. Top-level DDL only.
    private sealed class SchemaWalker : TSqlFragmentVisitor
    {
        private readonly string _sql;
        private readonly int _baseLine;
        private readonly List<ExtractedSqlSymbol> _symbols;
        private readonly List<ExtractedSqlReference> _refs;
        private readonly HashSet<string> _schemasEmitted = new(StringComparer.OrdinalIgnoreCase);

        public SchemaWalker(string sql, int baseLine, List<ExtractedSqlSymbol> symbols, List<ExtractedSqlReference> refs)
        { _sql = sql; _baseLine = baseLine; _symbols = symbols; _refs = refs; }

        public override void Visit(CreateTableStatement node) => Table(node.SchemaObjectName, node.Definition, node);
        public override void Visit(AlterTableAddTableElementStatement node) => Table(node.SchemaObjectName, node.Definition, node);

        public override void Visit(CreateViewStatement node) => View(node.SchemaObjectName, node.SelectStatement, node);
        public override void Visit(AlterViewStatement node) => View(node.SchemaObjectName, node.SelectStatement, node);

        public override void Visit(CreateProcedureStatement node) => Routine(CodeSymbolKind.StoredProcedure, node.ProcedureReference?.Name, node.Parameters, null, node.StatementList, node);
        public override void Visit(AlterProcedureStatement node) => Routine(CodeSymbolKind.StoredProcedure, node.ProcedureReference?.Name, node.Parameters, null, node.StatementList, node);

        public override void Visit(CreateFunctionStatement node) => Routine(CodeSymbolKind.SqlFunction, node.Name, node.Parameters, node.ReturnType, node.StatementList, node);
        public override void Visit(AlterFunctionStatement node) => Routine(CodeSymbolKind.SqlFunction, node.Name, node.Parameters, node.ReturnType, node.StatementList, node);

        public override void Visit(CreateTriggerStatement node) => Trigger(node);
        public override void Visit(CreateIndexStatement node) => IndexStmt(node);

        // ---- emitters ----

        private void Table(SchemaObjectName name, TableDefinition? def, TSqlFragment node)
        {
            var (db, schema, obj) = Parts(name);
            var schemaFull = EnsureSchema(db, schema, node);
            var tableFull = Display(db, schema, obj);
            Emit(CodeSymbolKind.Table, db, schema, obj, null, obj, tableFull, null, node, schemaFull, 0);
            if (def is null) return;

            foreach (var col in def.ColumnDefinitions)
            {
                var cname = col.ColumnIdentifier?.Value ?? "?";
                Emit(CodeSymbolKind.Column, db, schema, obj, cname, cname, Display(tableFull, cname), TypeOf(col), col, tableFull, 0);
            }
            foreach (var c in def.TableConstraints) Constraint(c, db, schema, obj, tableFull);
            foreach (var col in def.ColumnDefinitions)
                foreach (var c in col.Constraints) Constraint(c, db, schema, obj, tableFull);
            foreach (var ix in def.Indexes)
            {
                var ixName = ix.Name?.Value ?? "index";
                Emit(CodeSymbolKind.Index, db, schema, obj, ixName, ixName, Display(tableFull, ixName), null, ix, tableFull, 0);
            }
        }

        private void Constraint(ConstraintDefinition c, string? db, string schema, string table, string tableFull)
        {
            switch (c)
            {
                case ForeignKeyConstraintDefinition fk:
                {
                    var fkName = fk.ConstraintIdentifier?.Value ?? "FK";
                    Emit(CodeSymbolKind.ForeignKey, db, schema, table, fkName, fkName, Display(tableFull, fkName), null, fk, tableFull, 0);
                    var (rdb, rschema, robj) = Parts(fk.ReferenceTableName);
                    if (fk.ReferencedTableColumns.Count == 0)
                        _refs.Add(new ExtractedSqlReference(CodeReferenceKind.ForeignKeyReference, Display(tableFull, fkName), rdb, rschema, robj, null));
                    else
                        foreach (var rc in fk.ReferencedTableColumns)
                            _refs.Add(new ExtractedSqlReference(CodeReferenceKind.ForeignKeyReference, Display(tableFull, fkName), rdb, rschema, robj, rc.Value));
                    break;
                }
                case UniqueConstraintDefinition u:
                {
                    var n = u.ConstraintIdentifier?.Value ?? (u.IsPrimaryKey ? "PK" : "UQ");
                    Emit(CodeSymbolKind.Constraint, db, schema, table, n, n, Display(tableFull, n), u.IsPrimaryKey ? "PRIMARY KEY" : "UNIQUE", u, tableFull, 0);
                    break;
                }
                case CheckConstraintDefinition ck:
                {
                    var n = ck.ConstraintIdentifier?.Value ?? "CK";
                    Emit(CodeSymbolKind.Constraint, db, schema, table, n, n, Display(tableFull, n), "CHECK", ck, tableFull, 0);
                    break;
                }
                case DefaultConstraintDefinition dc:
                {
                    var n = dc.ConstraintIdentifier?.Value ?? "DF";
                    Emit(CodeSymbolKind.Constraint, db, schema, table, n, n, Display(tableFull, n), "DEFAULT", dc, tableFull, 0);
                    break;
                }
            }
        }

        private void View(SchemaObjectName name, SelectStatement? select, TSqlFragment node)
        {
            var (db, schema, obj) = Parts(name);
            var schemaFull = EnsureSchema(db, schema, node);
            var viewFull = Display(db, schema, obj);
            Emit(CodeSymbolKind.View, db, schema, obj, null, obj, viewFull, null, node, schemaFull, 0);
            if (select is not null) CollectRefs(select, viewFull);
        }

        private void Routine(CodeSymbolKind kind, SchemaObjectName? name, IList<ProcedureParameter>? parms, FunctionReturnType? ret, StatementList? body, TSqlFragment node)
        {
            if (name is null) return;
            var (db, schema, obj) = Parts(name);
            var schemaFull = EnsureSchema(db, schema, node);
            var full = Display(db, schema, obj);
            var cx = body is null ? 1 : Complexity(body);
            Emit(kind, db, schema, obj, null, obj, full, Signature(parms, ret), node, schemaFull, cx);
            if (body is not null) CollectRefs(body, full);
        }

        private void Trigger(CreateTriggerStatement node)
        {
            var (db, schema, obj) = Parts(node.Name);
            var schemaFull = EnsureSchema(db, schema, node);
            var full = Display(db, schema, obj);
            var cx = node.StatementList is null ? 1 : Complexity(node.StatementList);
            Emit(CodeSymbolKind.Trigger, db, schema, obj, null, obj, full, null, node, schemaFull, cx);
            if (node.TriggerObject?.Name is { } tname)
            {
                var (tdb, tschema, tobj) = Parts(tname);
                _refs.Add(new ExtractedSqlReference(CodeReferenceKind.TriggerTable, full, tdb, tschema, tobj, null));
            }
            if (node.StatementList is not null) CollectRefs(node.StatementList, full);
        }

        private void IndexStmt(CreateIndexStatement node)
        {
            var (db, schema, obj) = Parts(node.OnName);
            var tableFull = Display(db, schema, obj);
            var ixName = node.Name?.Value ?? "index";
            Emit(CodeSymbolKind.Index, db, schema, obj, ixName, ixName, Display(tableFull, ixName), null, node, tableFull, 0);
        }

        // Emit a schema (Namespace) container once per (db, schema); returns its display FullName so callers
        // can set it as the parent of tables/views/routines. Reconciliation dedups across batches by key.
        private string EnsureSchema(string? db, string schema, TSqlFragment node)
        {
            var full = Display(db, schema, "");
            if (_schemasEmitted.Add(full))
                Emit(CodeSymbolKind.Namespace, db, schema, schema, null, schema, full, null, node, null, 0);
            return full;
        }

        private void Emit(CodeSymbolKind kind, string? db, string schema, string obj, string? member, string name, string full, string? sig, TSqlFragment node, string? parentFull, int complexity)
        {
            int sl = (_baseLine - 1) + Math.Max(1, node.StartLine);
            int start = node.StartOffset >= 0 ? node.StartOffset : 0;
            int len = node.FragmentLength > 0 ? node.FragmentLength : 0;
            _symbols.Add(new ExtractedSqlSymbol(kind, db, schema, obj, member, name, full, sig,
                start, start + len, sl, sl + NewlineCount(node), complexity, parentFull));
        }

        // Collect FROM/JOIN/INTO named-object references and EXEC procedure references within a fragment.
        private void CollectRefs(TSqlFragment scope, string ownerFull)
        {
            var rc = new ReferenceCollector();
            scope.Accept(rc);
            foreach (var (name, kind) in rc.Found)
            {
                var (db, schema, obj) = Parts(name);
                if (!string.IsNullOrEmpty(obj))
                    _refs.Add(new ExtractedSqlReference(kind, ownerFull, db, schema, obj, null));
            }
        }

        private int NewlineCount(TSqlFragment node)
        {
            if (node.StartOffset < 0 || node.FragmentLength <= 0) return 0;
            int end = Math.Min(_sql.Length, node.StartOffset + node.FragmentLength);
            int n = 0;
            for (int i = node.StartOffset; i < end; i++) if (_sql[i] == '\n') n++;
            return n;
        }

        private static (string? db, string schema, string obj) Parts(SchemaObjectName? n)
        {
            if (n is null) return (null, "dbo", "");
            var db = n.DatabaseIdentifier?.Value;
            var schema = string.IsNullOrEmpty(n.SchemaIdentifier?.Value) ? "dbo" : n.SchemaIdentifier!.Value;
            var obj = n.BaseIdentifier?.Value ?? "";
            return (db, schema, obj);
        }

        private static string Display(string? a, string b, string c)
        {
            var parts = new List<string>(3);
            if (!string.IsNullOrEmpty(a)) parts.Add(a!);
            if (!string.IsNullOrEmpty(b)) parts.Add(b);
            if (!string.IsNullOrEmpty(c)) parts.Add(c);
            return string.Join(".", parts);
        }

        private static string Display(string parentFull, string leaf) => $"{parentFull}.{leaf}";

        private string? TypeOf(ColumnDefinition col)
        {
            var text = col.DataType is null ? null : Text(col.DataType);
            var nullable = col.Constraints.OfType<NullableConstraintDefinition>().FirstOrDefault();
            var nn = nullable is null ? "" : nullable.Nullable ? " NULL" : " NOT NULL";
            var identity = col.IdentityOptions is null ? "" : " IDENTITY";
            var computed = col.ComputedColumnExpression is null ? "" : " COMPUTED";
            return ((text ?? "?") + nn + identity + computed).Trim();
        }

        private string? Signature(IList<ProcedureParameter>? parms, FunctionReturnType? ret)
        {
            var p = parms is null || parms.Count == 0
                ? "()"
                : "(" + string.Join(",", parms.Select(x => x.DataType is null ? "?" : Text(x.DataType) ?? "?")) + ")";
            if (ret is ScalarFunctionReturnType sret && sret.DataType is not null)
                return p + " : " + (Text(sret.DataType) ?? "?");
            return p;
        }

        private string? Text(TSqlFragment frag)
        {
            if (frag.StartOffset < 0 || frag.FragmentLength <= 0 || frag.StartOffset >= _sql.Length) return null;
            int end = Math.Min(_sql.Length, frag.StartOffset + frag.FragmentLength);
            return _sql.Substring(frag.StartOffset, end - frag.StartOffset).Trim();
        }

        private static int Complexity(TSqlFragment body)
        {
            var cc = new ComplexityCounter();
            body.Accept(cc);
            return cc.Count;
        }
    }

    // Collects referenced objects within a body: named table/view references and EXEC procedure references.
    private sealed class ReferenceCollector : TSqlFragmentVisitor
    {
        public readonly List<(SchemaObjectName name, CodeReferenceKind kind)> Found = new();

        public override void Visit(NamedTableReference node)
        {
            if (node.SchemaObject is not null) Found.Add((node.SchemaObject, CodeReferenceKind.TableReference));
        }

        public override void Visit(ExecutableProcedureReference node)
        {
            var name = node.ProcedureReference?.ProcedureReference?.Name;
            if (name is not null) Found.Add((name, CodeReferenceKind.ProcedureReference));
        }
    }

    // Syntactic cyclomatic complexity over a T-SQL body: 1 + decision points. Deterministic; no semantics.
    private sealed class ComplexityCounter : TSqlFragmentVisitor
    {
        public int Count = 1;
        public override void Visit(IfStatement node) => Count++;
        public override void Visit(WhileStatement node) => Count++;
        public override void Visit(TryCatchStatement node) => Count++; // the CATCH branch
        public override void Visit(SearchedCaseExpression node) => Count += node.WhenClauses.Count;
        public override void Visit(SimpleCaseExpression node) => Count += node.WhenClauses.Count;
        public override void Visit(BooleanBinaryExpression node)
        {
            if (node.BinaryExpressionType is BooleanBinaryExpressionType.And or BooleanBinaryExpressionType.Or) Count++;
        }
    }
}
