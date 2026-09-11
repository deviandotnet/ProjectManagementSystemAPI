using FluentAssertions;
using NSubstitute;
using PMS.Application.Abstractions.Caching;

namespace PMS.UnitTests.Caching;

public sealed class CacheInvalidationTests
{
    [Fact]
    public async Task ProjectAsync_Should_InvalidateProjectListsAndDashboardsTogether()
    {
        IApplicationCache cache = Substitute.For<IApplicationCache>();
        Guid projectId = Guid.NewGuid();
        IReadOnlyCollection<string>? capturedTags = null;

        cache.RemoveByTagsAsync(
                Arg.Do<IReadOnlyCollection<string>>(tags => capturedTags = tags),
                Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        await CacheInvalidation.ProjectAsync(cache, projectId, CancellationToken.None);

        capturedTags.Should().BeEquivalentTo(
            CacheTags.Project(projectId),
            CacheTags.AllProjectLists,
            CacheTags.AllDashboards);
    }

    [Fact]
    public async Task MembershipAsync_Should_AlsoInvalidateAffectedUserScopes()
    {
        IApplicationCache cache = Substitute.For<IApplicationCache>();
        Guid projectId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        IReadOnlyCollection<string>? capturedTags = null;

        cache.RemoveByTagsAsync(
                Arg.Do<IReadOnlyCollection<string>>(tags => capturedTags = tags),
                Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        await CacheInvalidation.MembershipAsync(cache, projectId, userId, CancellationToken.None);

        capturedTags.Should().Contain(
            CacheTags.Project(projectId),
            CacheTags.UserProjects(userId),
            CacheTags.UserDashboard(userId),
            CacheTags.AllProjectLists,
            CacheTags.AllDashboards);
    }

    [Fact]
    public async Task HolidaysAsync_Should_InvalidateHolidayAndWorkingDayResults()
    {
        IApplicationCache cache = Substitute.For<IApplicationCache>();
        IReadOnlyCollection<string>? capturedTags = null;

        cache.RemoveByTagsAsync(
                Arg.Do<IReadOnlyCollection<string>>(tags => capturedTags = tags),
                Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        await CacheInvalidation.HolidaysAsync(cache, CancellationToken.None);

        capturedTags.Should().BeEquivalentTo(CacheTags.Holidays, CacheTags.WorkingDays);
    }
}
