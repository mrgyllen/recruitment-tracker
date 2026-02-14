using System.Reflection;
using api.Application.Common.Interfaces;
using FluentAssertions;
using FluentValidation;
using MediatR;
using NUnit.Framework;

namespace api.Application.UnitTests.Architecture;

/// <summary>
/// Architectural test: Enforces that all IRequest types in the Application assembly
/// have a corresponding AbstractValidator registered.
///
/// Requests that are legitimately exempt must be listed in <see cref="ExemptRequestTypes"/>.
/// Adding a new exemption requires a code review justification.
/// </summary>
[TestFixture]
public class ValidatorArchitectureTests
{
    /// <summary>
    /// IRequest types that are exempt from the validator requirement, with documented reasons.
    /// </summary>
    private static readonly HashSet<string> ExemptRequestTypes = new()
    {
        // Simple ID-based queries with no user input to validate beyond route params
        "GetMembersQuery",
        "GetRecruitmentsQuery",
        "GetRecruitmentByIdQuery",
        "GetImportSessionQuery",

        // ID-based commands where all params come from route/system — no user-provided body
        "RemoveWorkflowStepCommand",

        // Import pipeline command — match conflict resolution with minimal input
        "ResolveMatchConflictCommand",
    };

    private static readonly Assembly ApplicationAssembly =
        typeof(IApplicationDbContext).Assembly;

    [Test]
    public void AllRequestTypes_ShouldHaveValidator_UnlessExempt()
    {
        var requestTypes = ApplicationAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } or { IsValueType: false })
            .Where(t => t.GetInterfaces().Any(i =>
                i == typeof(IRequest) ||
                (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>))))
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .ToList();

        requestTypes.Should().NotBeEmpty(
            "the Application assembly should contain IRequest types");

        var validatorBaseType = typeof(AbstractValidator<>);
        var validatorTypes = ApplicationAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => HasBaseType(t, validatorBaseType))
            .ToList();

        // Build a set of request type names that have validators
        var validatedRequestTypeNames = validatorTypes
            .Select(v => GetValidatedRequestTypeName(v, validatorBaseType))
            .Where(name => name is not null)
            .ToHashSet();

        var violations = new List<string>();

        foreach (var requestType in requestTypes)
        {
            if (ExemptRequestTypes.Contains(requestType.Name))
                continue;

            if (!validatedRequestTypeNames.Contains(requestType.Name))
            {
                violations.Add(requestType.FullName!);
            }
        }

        violations.Should().BeEmpty(
            "all IRequest types (except documented exemptions) must have a corresponding " +
            "AbstractValidator<T> for input validation. " +
            "If a request is legitimately exempt, add it to ExemptRequestTypes with a justification comment. " +
            $"Request types missing validators: [{string.Join(", ", violations)}]");
    }

    [Test]
    public void ExemptRequestTypes_ShouldAllExistInApplicationAssembly()
    {
        var requestTypeNames = ApplicationAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => t.GetInterfaces().Any(i =>
                i == typeof(IRequest) ||
                (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>))))
            .Select(t => t.Name)
            .ToHashSet();

        var staleExemptions = ExemptRequestTypes
            .Where(name => !requestTypeNames.Contains(name))
            .ToList();

        staleExemptions.Should().BeEmpty(
            "all exempt request names in ExemptRequestTypes should correspond to actual " +
            "IRequest types. Remove stale entries: " +
            $"[{string.Join(", ", staleExemptions)}]");
    }

    private static bool HasBaseType(Type type, Type genericBaseType)
    {
        var current = type.BaseType;
        while (current is not null)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == genericBaseType)
                return true;
            current = current.BaseType;
        }
        return false;
    }

    private static string? GetValidatedRequestTypeName(Type validatorType, Type genericBaseType)
    {
        var current = validatorType.BaseType;
        while (current is not null)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == genericBaseType)
            {
                var validatedType = current.GetGenericArguments().FirstOrDefault();
                return validatedType?.Name;
            }
            current = current.BaseType;
        }
        return null;
    }
}
