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

    public List<T> List() => _db.Set<T>().OrderByDescending(x => x.Id).Take(500).ToList();
    public int Count() => _db.Set<T>().Count();
    public T? Get(int id) => _db.Set<T>().FirstOrDefault(x => x.Id == id);

    public T Create(T entity)
    {
        var nameProp = typeof(T).GetProperty("Name");
        if (nameProp != null && nameProp.PropertyType == typeof(string) &&
            string.IsNullOrWhiteSpace(nameProp.GetValue(entity) as string))
            throw new DomainException($"{typeof(T).Name}: Name is required.");
        _db.Set<T>().Add(entity);
        _audit.Record(typeof(T).Name, 0, "Create", null);
        _db.SaveChanges();
        return entity;
    }
}