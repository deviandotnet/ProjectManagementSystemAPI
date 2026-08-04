using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PMS.Application.Abstractions.Authentication;
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
    /// Intercepts saves to auto-populate audit timestamps on AuditableBaseEntity instances.
    /// CreatedAt is set only on Added entries; UpdatedAt is set on every Modified entry.
    /// </summary>
    private static void ApplyAuditInfo(DbContext? context, IUserContext userContext)
    {
        if (context == null)
            return;

        var now = DateTimeOffset.UtcNow;

        foreach (var entry in context.ChangeTracker
                                     .Entries<AuditableBaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.CreatedByUserId = userContext.UserId ?? null; 
            }

            if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedByUserId = userContext.UserId ?? null; 
            }
        }
    }

}
