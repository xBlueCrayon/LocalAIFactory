using LafErp.Core;
using LafErp.Data;
using Microsoft.EntityFrameworkCore;

namespace LafErp.Services;

/// <summary>Generic CRUD service for generated catalog entities, with a reflective required-Name check + audit.</summary>
public class CatalogCrudService<T> where T : EntityBase, new()
{
    private readonly ErpDbContext _db;
    private readonly AuditService _audit;
    public CatalogCrudService(ErpDbContext db, AuditService audit) { _db = db; _audit = audit; }

    public List<T> List() => _db.Set<T>().Where(x => !x.IsDeleted).OrderByDescending(x => x.Id).Take(500).ToList();
    public int Count() => _db.Set<T>().Count(x => !x.IsDeleted);
    public T? Get(int id) => _db.Set<T>().FirstOrDefault(x => x.Id == id && !x.IsDeleted);

    public T Create(T entity)
    {
        RequireName(entity);
        _db.Set<T>().Add(entity);
        _audit.Record(typeof(T).Name, 0, "Create", null);
        _db.SaveChanges();
        return entity;
    }

    /// <summary>Edit an existing master record (master data is mutable; posted documents are not).</summary>
    public T Update(T entity)
    {
        RequireName(entity);
        var existing = _db.Set<T>().FirstOrDefault(x => x.Id == entity.Id)
                       ?? throw new DomainException($"{typeof(T).Name} {entity.Id} not found.");
        _db.Entry(existing).CurrentValues.SetValues(entity);
        _audit.Record(typeof(T).Name, entity.Id, "Update", null);
        _db.SaveChanges();
        return existing;
    }

    /// <summary>Soft-delete (deactivate) a master record — never a hard delete, so audit/history is preserved.</summary>
    public void Deactivate(int id)
    {
        var existing = _db.Set<T>().FirstOrDefault(x => x.Id == id)
                       ?? throw new DomainException($"{typeof(T).Name} {id} not found.");
        existing.IsDeleted = true;
        _audit.Record(typeof(T).Name, id, "Deactivate", null);
        _db.SaveChanges();
    }

    private static void RequireName(T entity)
    {
        var nameProp = typeof(T).GetProperty("Name");
        if (nameProp != null && nameProp.PropertyType == typeof(string) &&
            string.IsNullOrWhiteSpace(nameProp.GetValue(entity) as string))
            throw new DomainException($"{typeof(T).Name}: Name is required.");
    }
}