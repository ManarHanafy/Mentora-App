namespace api.Entities;

/// <summary>
/// Base class that provides automatic audit timestamps.
/// DbContext.SaveChangesAsync sets CreatedAt on insert and UpdatedAt on update.
/// </summary>
public abstract class AuditableEntity
{
    public DateTime  CreatedAt  { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt  { get; set; }
}
