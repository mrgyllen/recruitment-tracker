using System.Reflection;
using api.Application.Common.Interfaces;
using FluentAssertions;
using NUnit.Framework;

namespace api.Application.UnitTests.Architecture;

/// <summary>
/// Architectural test: Enforces that command handlers in the Application layer
/// do not directly modify owned entity types via DbContext.
/// All state changes to owned entities must flow through aggregate root methods.
///
/// Aggregate roots (direct DbSet access allowed): Recruitment, Candidate, ImportSession, AuditEntry
/// Owned entities (must modify through aggregate root): WorkflowStep, RecruitmentMember,
///   CandidateOutcome, CandidateDocument, ImportDocument, ImportRowResult
/// </summary>
[TestFixture]
public class AggregateRootArchitectureTests
{
    private static readonly Assembly ApplicationAssembly =
        typeof(IApplicationDbContext).Assembly;

    /// <summary>
    /// Owned entity type names that should only be modified through their aggregate root.
    /// If a handler needs to create/modify these, it should load the aggregate root
    /// and call methods on it (e.g., recruitment.AddStep(), not dbContext.WorkflowSteps.Add()).
    /// </summary>
    private static readonly HashSet<string> OwnedEntityTypeNames = new()
    {
        "WorkflowStep",
        "RecruitmentMember",
        "CandidateOutcome",
        "CandidateDocument",
        "ImportDocument",
        "ImportRowResult",
    };

    [Test]
    public void ApplicationDbContext_ShouldNotExposeDbSetsForOwnedEntities()
    {
        // IApplicationDbContext should only have DbSet properties for aggregate roots,
        // not for owned entities.
        var dbContextInterface = typeof(IApplicationDbContext);
        var dbSetProperties = dbContextInterface.GetProperties()
            .Where(p => p.PropertyType.IsGenericType &&
                        p.PropertyType.GetGenericTypeDefinition().Name.StartsWith("DbSet"))
            .Select(p => p.PropertyType.GetGenericArguments().FirstOrDefault()?.Name)
            .Where(name => name is not null)
            .ToList();

        var violations = dbSetProperties
            .Where(name => OwnedEntityTypeNames.Contains(name!))
            .ToList();

        violations.Should().BeEmpty(
            "IApplicationDbContext should not expose DbSet<T> for owned entity types. " +
            "Owned entities must be accessed through their aggregate root. " +
            $"Violations: [{string.Join(", ", violations)}]");
    }

    [Test]
    public void OwnedEntityTypeNames_ShouldAllExistInDomainAssembly()
    {
        // Guard against stale entries — all listed owned types should exist in the Domain assembly
        var domainAssembly = typeof(api.Domain.Entities.Recruitment).Assembly;

        var domainTypeNames = domainAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Select(t => t.Name)
            .ToHashSet();

        var staleEntries = OwnedEntityTypeNames
            .Where(name => !domainTypeNames.Contains(name))
            .ToList();

        staleEntries.Should().BeEmpty(
            "all entries in OwnedEntityTypeNames should correspond to actual domain entity types. " +
            $"Remove stale entries: [{string.Join(", ", staleEntries)}]");
    }
}
