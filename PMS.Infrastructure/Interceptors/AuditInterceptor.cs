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
    /// Intercepts saves to auto-populate audit timestamps and user IDs on AuditableBaseEntity instances.
    /// Preserves existing CreatedByUserId/UpdatedByUserId if userContext.UserId is null.
    /// </summary>
    private static void ApplyAuditInfo(DbContext? context, IUserContext userContext)
    {
        if (context == null)
            return;

        var now = DateTimeOffset.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<AuditableBaseEntity>())
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
    }
}
