using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PMS.Application.Abstractions.Authentication;
using PMS.Domain.AuditLogs;
using PMS.SharedKernel;

namespace PMS.Infrastructure.Interceptors;

public class AuditInterceptor(IUserContext userContext) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyAuditInfo(eventData.Context, userContext);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditInfo(eventData.Context, userContext);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// Intercepts saves to auto-populate audit timestamps, user IDs, and structured AuditLog records.
    /// Preserves existing CreatedByUserId/UpdatedByUserId if userContext.UserId is null.
    /// </summary>
    private static void ApplyAuditInfo(DbContext? context, IUserContext userContext)
    {
        if (context == null)
            return;

        var now = DateTimeOffset.UtcNow;
        var auditLogs = new List<AuditLog>();

        // 1. Process AuditableBaseEntity timestamps
        foreach (var entry in context.ChangeTracker.Entries<AuditableBaseEntity>().ToList())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                if (userContext.UserId.HasValue)
                {
                    entry.Entity.CreatedByUserId = userContext.UserId;
                }
            }

            if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                if (userContext.UserId.HasValue)
                {
                    entry.Entity.UpdatedByUserId = userContext.UserId;
                }
            }
        }

        // 2. Generate structured AuditLog entries for tracked entities
        foreach (var entry in context.ChangeTracker.Entries().ToList())
        {
            if (entry.Entity is AuditLog || entry.State is EntityState.Detached or EntityState.Unchanged)
                continue;

            string entityName = entry.Metadata.ClrType.Name;
            string entityId = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(entityId))
                continue;

            if (entry.State == EntityState.Added)
            {
                auditLogs.Add(new AuditLog
                {
                    EntityName = entityName,
                    EntityId = entityId,
                    Action = "Create",
                    FieldName = null,
                    OldValue = null,
                    NewValue = null,
                    ChangedByUserId = userContext.UserId,
                    ChangedByName = userContext.Email ?? "System",
                    ChangedAt = now
                });
            }
            else if (entry.State == EntityState.Modified)
            {
                foreach (var prop in entry.Properties)
                {
                    if (!prop.IsModified || prop.Metadata.IsPrimaryKey())
                        continue;

                    // Skip internal audit timestamp columns
                    string propName = prop.Metadata.Name;
                    if (propName is "UpdatedAt" or "UpdatedByUserId" or "CreatedAt" or "CreatedByUserId")
                        continue;

                    string? originalVal = prop.OriginalValue?.ToString();
                    string? currentVal = prop.CurrentValue?.ToString();

                    if (originalVal != currentVal)
                    {
                        auditLogs.Add(new AuditLog
                        {
                            EntityName = entityName,
                            EntityId = entityId,
                            Action = "Update",
                            FieldName = propName,
                            OldValue = originalVal,
                            NewValue = currentVal,
                            ChangedByUserId = userContext.UserId,
                            ChangedByName = userContext.Email ?? "System",
                            ChangedAt = now
                        });
                    }
                }
            }
            else if (entry.State == EntityState.Deleted)
            {
                auditLogs.Add(new AuditLog
                {
                    EntityName = entityName,
                    EntityId = entityId,
                    Action = "Delete",
                    FieldName = null,
                    OldValue = null,
                    NewValue = null,
                    ChangedByUserId = userContext.UserId,
                    ChangedByName = userContext.Email ?? "System",
                    ChangedAt = now
                });
            }
        }

        if (auditLogs.Count > 0)
        {
            context.Set<AuditLog>().AddRange(auditLogs);
        }
    }
}
