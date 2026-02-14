using System.Reflection;
using api.Application.Common.Interfaces;
using FluentAssertions;
using MediatR;
using NUnit.Framework;

namespace api.Application.UnitTests.Architecture;

/// <summary>
/// Architectural test (experiment E-004): Enforces that all IRequestHandler implementations
/// inject ITenantContext for recruitment-scoped authorization.
///
/// Handlers that are legitimately exempt must be listed in <see cref="ExemptHandlerTypes"/>.
/// Adding a new exemption requires a code review justification.
/// </summary>
[TestFixture]
public class AuthorizationArchitectureTests
{
    /// <summary>
    /// Handlers that are exempt from the ITenantContext requirement, with documented reasons.
    /// </summary>
    private static readonly HashSet<string> ExemptHandlerTypes = new()
    {
        // Creates a new recruitment — uses IUser for creator identity, not recruitment-scoped
        "CreateRecruitmentCommandHandler",

        // Background service handler — runs in service context, not user-scoped
        "ProcessPdfBundleCommandHandler",

        // Searches organizational directory — not recruitment-scoped
        "SearchDirectoryQueryHandler",
    };

    private static readonly Assembly ApplicationAssembly =
        typeof(IApplicationDbContext).Assembly;

    [Test]
    public void AllRequestHandlers_ShouldInjectITenantContext_UnlessExempt()
    {
        var handlerTypes = ApplicationAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                (i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>) ||
                 i.GetGenericTypeDefinition() == typeof(IRequestHandler<>))))
            .ToList();

        handlerTypes.Should().NotBeEmpty(
            "the Application assembly should contain IRequestHandler implementations");

        var violations = new List<string>();

        foreach (var handlerType in handlerTypes)
        {
            if (ExemptHandlerTypes.Contains(handlerType.Name))
                continue;

            var constructors = handlerType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            var injectsTenantContext = constructors.Any(c =>
                c.GetParameters().Any(p => p.ParameterType == typeof(ITenantContext)));

            if (!injectsTenantContext)
            {
                violations.Add(handlerType.FullName!);
            }
        }

        violations.Should().BeEmpty(
            "all IRequestHandler implementations (except documented exemptions) must inject " +
            "ITenantContext for recruitment-scoped authorization. " +
            "If a handler is legitimately exempt, add it to ExemptHandlerTypes with a justification comment. " +
            $"Violating handlers: [{string.Join(", ", violations)}]");
    }

    [Test]
    public void ExemptHandlers_ShouldAllExistInApplicationAssembly()
    {
        var handlerTypeNames = ApplicationAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                (i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>) ||
                 i.GetGenericTypeDefinition() == typeof(IRequestHandler<>))))
            .Select(t => t.Name)
            .ToHashSet();

        var staleExemptions = ExemptHandlerTypes
            .Where(name => !handlerTypeNames.Contains(name))
            .ToList();

        staleExemptions.Should().BeEmpty(
            "all exempt handler names in ExemptHandlerTypes should correspond to actual " +
            "IRequestHandler implementations. Remove stale entries: " +
            $"[{string.Join(", ", staleExemptions)}]");
    }
}
