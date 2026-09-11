using FluentAssertions;
using PMS.Application.Abstractions.Caching;

namespace PMS.UnitTests.Caching;

public sealed class CacheKeyBuilderTests
{
    [Fact]
    public void Create_Should_NormalizeTextAndUseInvariantDimensions()
    {
        Guid projectId = Guid.Parse("6eeab99e-07af-4f50-a864-caaa8af2ed22");
        var date = new DateOnly(2026, 9, 10);

        string key = CacheKeyBuilder.Create(
            " Action-Items ",
            " SEARCH Value ",
            projectId,
            date,
            new[] { 2, 0, 2, 1 });

        key.Should().Be(
            "pms:v1:action-items:search value:6eeab99e07af4f50a864caaa8af2ed22:2026-09-10:0,1,2");
    }

    [Fact]
    public void Fingerprint_Should_BeStableForEquivalentStatusCollections()
    {
        string first = CacheKeyBuilder.Fingerprint("Search", new[] { 2, 0, 2, 1 });
        string second = CacheKeyBuilder.Fingerprint(" Search ", new[] { 1, 2, 0 });

        first.Should().Be(second);
        first.Should().HaveLength(64);
    }

    [Fact]
    public void Fingerprint_Should_PreserveCaseSensitiveFilterSemantics()
    {
        string upperCase = CacheKeyBuilder.Fingerprint("Project Alpha");
        string lowerCase = CacheKeyBuilder.Fingerprint("project alpha");

        upperCase.Should().NotBe(lowerCase);
    }

    [Fact]
    public void Fingerprint_Should_NotCollideWhenValuesContainSeparators()
    {
        string first = CacheKeyBuilder.Fingerprint("a|b", "c");
        string second = CacheKeyBuilder.Fingerprint("a", "b|c");

        first.Should().NotBe(second);
    }

    [Theory]
    [InlineData(-10, 1)]
    [InlineData(0, 1)]
    [InlineData(4, 4)]
    public void NormalizePageNumber_Should_ClampToAtLeastOne(int input, int expected)
    {
        CacheKeyBuilder.NormalizePageNumber(input).Should().Be(expected);
    }

    [Theory]
    [InlineData(-10, 1)]
    [InlineData(0, 1)]
    [InlineData(25, 25)]
    [InlineData(500, 100)]
    public void NormalizePageSize_Should_ClampToSupportedRange(int input, int expected)
    {
        CacheKeyBuilder.NormalizePageSize(input).Should().Be(expected);
    }
}
