using System.Reflection;
using FluentAssertions;
using PMS.Application.Abstractions.Caching;
using PMS.Application.Abstractions.Messaging;
using PMS.Application.Projects.GetProjectById;

namespace PMS.UnitTests.Caching;

public sealed class CachingCoverageTests
{
    [Fact]
    public void DatabaseBackedQueryHandlers_Should_DeclareCacheDependency()
    {
        Assembly applicationAssembly = typeof(GetProjectByIdQuery).Assembly;

        Type[] handlers = applicationAssembly.GetTypes()
            .Where(type => type.IsClass &&
                           type.Name.EndsWith("QueryHandler", StringComparison.Ordinal) &&
                           type.Namespace?.StartsWith("PMS.Application.", StringComparison.Ordinal) == true &&
                           type.Namespace != "PMS.Application.Projects.ExportProjectExcel")
            .ToArray();

        string[] missing = handlers
            .Where(type => !HasConstructorParameter<IApplicationCache>(type))
            .Select(type => type.FullName!)
            .Order()
            .ToArray();

        missing.Should().BeEmpty("every database-backed query except the binary export must participate in caching");
    }

    [Fact]
    public void DataMutatingCommandHandlers_Should_DeclareCacheDependency()
    {
        Assembly applicationAssembly = typeof(GetProjectByIdQuery).Assembly;

        Type[] handlers = applicationAssembly.GetTypes()
            .Where(type => type.IsClass &&
                           type.Name.EndsWith("CommandHandler", StringComparison.Ordinal) &&
                           type.Namespace?.StartsWith("PMS.Application.", StringComparison.Ordinal) == true &&
                           !type.Namespace.StartsWith("PMS.Application.Users.", StringComparison.Ordinal))
            .ToArray();

        string[] missing = handlers
            .Where(type => !HasConstructorParameter<IApplicationCache>(type))
            .Select(type => type.FullName!)
            .Order()
            .ToArray();

        missing.Should().BeEmpty("every project or holiday mutation must invalidate affected cache dependencies");
    }

    private static bool HasConstructorParameter<T>(Type type) =>
        type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .SelectMany(constructor => constructor.GetParameters())
            .Any(parameter => parameter.ParameterType == typeof(T));
}
