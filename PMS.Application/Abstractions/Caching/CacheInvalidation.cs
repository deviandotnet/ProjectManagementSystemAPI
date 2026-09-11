namespace PMS.Application.Abstractions.Caching;

public static class CacheInvalidation
{
    public static ValueTask ProjectAsync(
        IApplicationCache? cache,
        Guid projectId,
        CancellationToken cancellationToken) =>
        (cache ?? NullApplicationCache.Instance).RemoveByTagsAsync(
            [CacheTags.Project(projectId), CacheTags.AllProjectLists, CacheTags.AllDashboards],
            cancellationToken);

    public static ValueTask MembershipAsync(
        IApplicationCache? cache,
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken) =>
        (cache ?? NullApplicationCache.Instance).RemoveByTagsAsync(
            [
                CacheTags.Project(projectId),
                CacheTags.UserProjects(userId),
                CacheTags.UserDashboard(userId),
                CacheTags.AllProjectLists,
                CacheTags.AllDashboards
            ],
            cancellationToken);

    public static ValueTask ProjectCreatedAsync(
        IApplicationCache? cache,
        Guid creatorUserId,
        CancellationToken cancellationToken) =>
        (cache ?? NullApplicationCache.Instance).RemoveByTagsAsync(
            [
                CacheTags.UserProjects(creatorUserId),
                CacheTags.UserDashboard(creatorUserId),
                CacheTags.AllProjectLists,
                CacheTags.AllDashboards
            ],
            cancellationToken);

    public static ValueTask HolidaysAsync(
        IApplicationCache? cache,
        CancellationToken cancellationToken) =>
        (cache ?? NullApplicationCache.Instance).RemoveByTagsAsync(
            [CacheTags.Holidays, CacheTags.WorkingDays],
            cancellationToken);
}
