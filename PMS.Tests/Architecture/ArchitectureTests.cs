using FluentAssertions;
using FluentValidation;
using PMS.Application.Abstractions.Messaging;
using PMS.Domain.Projects;
using PMS.SharedKernel;
using System.Reflection;
using Xunit;

namespace PMS.UnitTests.Architecture;

public class ArchitectureTests
{
    private static readonly Assembly DomainAssembly = typeof(Project).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(ICommandHandler<>).Assembly;

    [Fact]
    public void Domain_Should_Not_Have_Dependency_On_Application_Or_Infrastructure()
    {
        // Arrange
        var referencedAssemblies = DomainAssembly.GetReferencedAssemblies();

        // Assert
        referencedAssemblies.Should().NotContain(a => a.Name == "PMS.Application");
        referencedAssemblies.Should().NotContain(a => a.Name == "PMS.Infrastructure");
        referencedAssemblies.Should().NotContain(a => a.Name == "PMS.API");
    }

    [Fact]
    public void Application_Should_Not_Have_Dependency_On_Infrastructure_Or_API()
    {
        // Arrange
        var referencedAssemblies = ApplicationAssembly.GetReferencedAssemblies();

        // Assert
        referencedAssemblies.Should().NotContain(a => a.Name == "PMS.Infrastructure");
        referencedAssemblies.Should().NotContain(a => a.Name == "PMS.API");
    }

    [Fact]
    public void Handlers_Should_Be_Internal_And_Sealed()
    {
        // Arrange
        var handlerTypes = ApplicationAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract &&
                t.GetInterfaces().Any(i =>
                    (i == typeof(ICommandHandler<>) || i == typeof(ICommandHandler<,>) ||
                     (i.IsGenericType && (
                         i.GetGenericTypeDefinition() == typeof(ICommandHandler<>) ||
                         i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>) ||
                         i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>))))))
            .ToList();

        // Assert
        handlerTypes.Should().NotBeEmpty();
        foreach (var type in handlerTypes)
        {
            type.IsSealed.Should().BeTrue($"{type.Name} should be sealed.");
            (type.IsNotPublic || type.IsNestedAssembly).Should().BeTrue($"{type.Name} should be internal.");
        }
    }

    [Fact]
    public void Commands_Should_Have_Matching_Validators()
    {
        // Arrange
        var commandTypes = ApplicationAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract &&
                t.GetInterfaces().Any(i =>
                    i == typeof(ICommand) ||
                    (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>))))
            .ToList();

        var validatorTypes = ApplicationAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract &&
                t.BaseType is not null && t.BaseType.IsGenericType &&
                t.BaseType.GetGenericTypeDefinition() == typeof(AbstractValidator<>))
            .Select(t => t.BaseType!.GetGenericArguments()[0])
            .ToHashSet();

        // Assert
        foreach (var commandType in commandTypes)
        {
            validatorTypes.Should().Contain(commandType,
                $"Command {commandType.Name} should have a corresponding FluentValidation validator.");
        }
    }
}
