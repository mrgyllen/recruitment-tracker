# CV Auto-Match, Manual Assignment & Individual Upload Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Automatically match split CVs to candidates by name, allow manual assignment of unmatched CVs, and provide individual PDF upload per candidate.

**Architecture:** DDD with Candidate aggregate owning CandidateDocument. NameNormalizer (pure static) + DocumentMatchingEngine (infra implementing app interface) perform matching. Two new CQRS commands (AssignDocument, UploadDocument) through MediatR with mandatory auth checks. Frontend extends ImportSummary for unmatched docs and adds CandidateDetail page with DocumentUpload component.

**Tech Stack:** .NET 10, EF Core, MediatR, FluentValidation, Azure Blob Storage | React 19, TypeScript, TanStack Query, shadcn/ui, MSW

---

## Task 1: NameNormalizer static class + unit tests

**Testing mode:** Test-first (pure function, ideal for TDD)

**Files:**
- Create: `api/src/Infrastructure/Services/NameNormalizer.cs`
- Create: `api/tests/Domain.UnitTests/Services/NameNormalizerTests.cs`

**Step 1: Write the failing tests**

Create `api/tests/Domain.UnitTests/Services/NameNormalizerTests.cs`:

```csharp
using api.Infrastructure.Services;
using FluentAssertions;
using NUnit.Framework;

namespace api.Domain.UnitTests.Services;

public class NameNormalizerTests
{
    [Test]
    public void Normalize_NameWithDiacritics_ReturnsDiacriticsStripped()
    {
        NameNormalizer.Normalize("Éric du Pont").Should().Be("eric du pont");
    }

    [Test]
    public void Normalize_NameWithMultipleSpaces_ReturnsCollapsedSpaces()
    {
        NameNormalizer.Normalize("  Éric  du   Pont ").Should().Be("eric du pont");
    }

    [Test]
    public void Normalize_NullInput_ReturnsEmptyString()
    {
        NameNormalizer.Normalize(null).Should().Be(string.Empty);
    }

    [Test]
    public void Normalize_EmptyInput_ReturnsEmptyString()
    {
        NameNormalizer.Normalize("  ").Should().Be(string.Empty);
    }

    [Test]
    public void Normalize_HyphensPreserved()
    {
        NameNormalizer.Normalize("AnnA-Lisa").Should().Be("anna-lisa");
    }

    [Test]
    public void Normalize_IcelandicName_ReturnsDiacriticsStripped()
    {
        NameNormalizer.Normalize("Björk Guðmundsdóttir").Should().Be("bjork gudmundsdottir");
    }

    [Test]
    public void Normalize_MixedCase_ReturnsLowercase()
    {
        NameNormalizer.Normalize("ALICE JOHNSON").Should().Be("alice johnson");
    }
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test api/tests/Domain.UnitTests/ --filter "FullyQualifiedName~NameNormalizerTests" --no-restore`
Expected: Build error -- `NameNormalizer` type does not exist.

**Step 3: Write minimal implementation**

Create `api/src/Infrastructure/Services/NameNormalizer.cs`:

```csharp
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace api.Infrastructure.Services;

public static partial class NameNormalizer
{
    public static string Normalize(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        var result = name.Trim();
        result = result.ToLowerInvariant();
        result = RemoveDiacritics(result);
        result = MultipleSpaces().Replace(result, " ");

        return result;
    }

    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex MultipleSpaces();
}
```

**Step 4: Run tests to verify they pass**

Run: `dotnet test api/tests/Domain.UnitTests/ --filter "FullyQualifiedName~NameNormalizerTests" --no-restore -v quiet`
Expected: 7/7 PASS

**Step 5: Commit**

```bash
git add api/src/Infrastructure/Services/NameNormalizer.cs api/tests/Domain.UnitTests/Services/NameNormalizerTests.cs
git commit -m "feat(3.5): add NameNormalizer with diacritics stripping + unit tests"
```

---

## Task 2: DocumentMatchingEngine + SplitDocument/DocumentMatchResult models + unit tests

**Testing mode:** Test-first (core business logic with clear matching rules)

**Files:**
- Create: `api/src/Application/Common/Interfaces/IDocumentMatchingEngine.cs`
- Create: `api/src/Application/Common/Models/SplitDocument.cs`
- Create: `api/src/Application/Common/Models/DocumentMatchResult.cs`
- Create: `api/src/Infrastructure/Services/DocumentMatchingEngine.cs`
- Create: `api/tests/Domain.UnitTests/Services/DocumentMatchingEngineTests.cs`

**Step 1: Write the failing tests**

Create `api/tests/Domain.UnitTests/Services/DocumentMatchingEngineTests.cs`:

```csharp
using api.Application.Common.Models;
using api.Domain.Entities;
using api.Domain.Enums;
using api.Infrastructure.Services;
using FluentAssertions;
using NUnit.Framework;

namespace api.Domain.UnitTests.Services;

public class DocumentMatchingEngineTests
{
    private DocumentMatchingEngine _engine = null!;

    [SetUp]
    public void SetUp() => _engine = new DocumentMatchingEngine();

    private static Candidate CreateCandidate(string fullName)
    {
        return Candidate.Create(
            Guid.NewGuid(), fullName, $"{fullName.Replace(" ", "")}@test.com",
            null, null, DateTimeOffset.UtcNow);
    }

    [Test]
    public void Match_ExactNameAfterNormalization_ReturnsAutoMatched()
    {
        var candidates = new[] { CreateCandidate("Alice Johnson") };
        var docs = new[] { new SplitDocument("alice johnson", "blob://cv.pdf", null) };

        var results = _engine.MatchDocumentsToCandidates(docs, candidates);

        results.Should().ContainSingle()
            .Which.Status.Should().Be(ImportDocumentMatchStatus.AutoMatched);
        results[0].MatchedCandidateId.Should().Be(candidates[0].Id);
    }

    [Test]
    public void Match_NoMatchingCandidate_ReturnsUnmatched()
    {
        var candidates = new[] { CreateCandidate("Bob Smith") };
        var docs = new[] { new SplitDocument("Unknown Person", "blob://cv.pdf", null) };

        var results = _engine.MatchDocumentsToCandidates(docs, candidates);

        results.Should().ContainSingle()
            .Which.Status.Should().Be(ImportDocumentMatchStatus.Unmatched);
        results[0].MatchedCandidateId.Should().BeNull();
    }

    [Test]
    public void Match_MultipleCandidatesWithSameName_ReturnsUnmatched()
    {
        var candidates = new[]
        {
            CreateCandidate("Alice Johnson"),
            CreateCandidate("Alice Johnson"),
        };
        var docs = new[] { new SplitDocument("Alice Johnson", "blob://cv.pdf", null) };

        var results = _engine.MatchDocumentsToCandidates(docs, candidates);

        results.Should().ContainSingle()
            .Which.Status.Should().Be(ImportDocumentMatchStatus.Unmatched);
    }

    [Test]
    public void Match_DiacriticsInDocumentName_MatchesStrippedCandidate()
    {
        var candidates = new[] { CreateCandidate("Eric du Pont") };
        var docs = new[] { new SplitDocument("Éric du Pont", "blob://cv.pdf", null) };

        var results = _engine.MatchDocumentsToCandidates(docs, candidates);

        results.Should().ContainSingle()
            .Which.Status.Should().Be(ImportDocumentMatchStatus.AutoMatched);
    }

    [Test]
    public void Match_EmptyDocumentList_ReturnsEmptyResults()
    {
        var candidates = new[] { CreateCandidate("Alice") };

        var results = _engine.MatchDocumentsToCandidates([], candidates);

        results.Should().BeEmpty();
    }

    [Test]
    public void Match_EmptyCandidateList_AllUnmatched()
    {
        var docs = new[] { new SplitDocument("Alice", "blob://cv.pdf", null) };

        var results = _engine.MatchDocumentsToCandidates(docs, []);

        results.Should().ContainSingle()
            .Which.Status.Should().Be(ImportDocumentMatchStatus.Unmatched);
    }
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test api/tests/Domain.UnitTests/ --filter "FullyQualifiedName~DocumentMatchingEngineTests" --no-restore`
Expected: Build error -- types do not exist.

**Step 3: Create the models and interface**

Create `api/src/Application/Common/Models/SplitDocument.cs`:

```csharp
namespace api.Application.Common.Models;

public record SplitDocument(
    string CandidateName,
    string BlobStorageUrl,
    string? WorkdayCandidateId);
```

Create `api/src/Application/Common/Models/DocumentMatchResult.cs`:

```csharp
using api.Domain.Enums;

namespace api.Application.Common.Models;

public record DocumentMatchResult(
    SplitDocument Document,
    Guid? MatchedCandidateId,
    ImportDocumentMatchStatus Status);
```

Create `api/src/Application/Common/Interfaces/IDocumentMatchingEngine.cs`:

```csharp
using api.Application.Common.Models;
using api.Domain.Entities;

namespace api.Application.Common.Interfaces;

public interface IDocumentMatchingEngine
{
    IReadOnlyList<DocumentMatchResult> MatchDocumentsToCandidates(
        IReadOnlyList<SplitDocument> documents,
        IReadOnlyList<Candidate> candidates);
}
```

**Step 4: Implement DocumentMatchingEngine**

Create `api/src/Infrastructure/Services/DocumentMatchingEngine.cs`:

```csharp
using api.Application.Common.Interfaces;
using api.Application.Common.Models;
using api.Domain.Entities;
using api.Domain.Enums;

namespace api.Infrastructure.Services;

public class DocumentMatchingEngine : IDocumentMatchingEngine
{
    public IReadOnlyList<DocumentMatchResult> MatchDocumentsToCandidates(
        IReadOnlyList<SplitDocument> documents,
        IReadOnlyList<Candidate> candidates)
    {
        var candidateLookup = candidates
            .GroupBy(c => NameNormalizer.Normalize(c.FullName))
            .ToDictionary(g => g.Key, g => g.ToList());

        var results = new List<DocumentMatchResult>();

        foreach (var doc in documents)
        {
            var normalizedDocName = NameNormalizer.Normalize(doc.CandidateName);

            if (candidateLookup.TryGetValue(normalizedDocName, out var matches)
                && matches.Count == 1)
            {
                results.Add(new DocumentMatchResult(
                    doc, matches[0].Id, ImportDocumentMatchStatus.AutoMatched));
            }
            else
            {
                results.Add(new DocumentMatchResult(
                    doc, null, ImportDocumentMatchStatus.Unmatched));
            }
        }

        return results;
    }
}
```

**Step 5: Run tests to verify they pass**

Run: `dotnet test api/tests/Domain.UnitTests/ --filter "FullyQualifiedName~DocumentMatchingEngineTests" --no-restore -v quiet`
Expected: 6/6 PASS

**Step 6: Commit**

```bash
git add api/src/Application/Common/Interfaces/IDocumentMatchingEngine.cs \
  api/src/Application/Common/Models/SplitDocument.cs \
  api/src/Application/Common/Models/DocumentMatchResult.cs \
  api/src/Infrastructure/Services/DocumentMatchingEngine.cs \
  api/tests/Domain.UnitTests/Services/DocumentMatchingEngineTests.cs
git commit -m "feat(3.5): add DocumentMatchingEngine with auto-match logic + tests"
```

---

## Task 3: Candidate.ReplaceDocument domain method + unit tests

**Testing mode:** Test-first (aggregate invariant)

**Files:**
- Modify: `api/src/Domain/Entities/Candidate.cs` -- add `ReplaceDocument()` method
- Modify: `api/tests/Domain.UnitTests/Entities/CandidateTests.cs` -- add tests

**Step 1: Write the failing tests**

Add to `api/tests/Domain.UnitTests/Entities/CandidateTests.cs`:

```csharp
[Test]
public void ReplaceDocument_ExistingDocument_RemovesOldAndAddsNew()
{
    var candidate = CreateCandidate();
    candidate.AttachDocument("CV", "https://blob.storage/old.pdf");
    candidate.ClearDomainEvents();

    var oldUrl = candidate.ReplaceDocument("CV", "https://blob.storage/new.pdf");

    oldUrl.Should().Be("https://blob.storage/old.pdf");
    candidate.Documents.Should().HaveCount(1);
    candidate.Documents.First().BlobStorageUrl.Should().Be("https://blob.storage/new.pdf");
}

[Test]
public void ReplaceDocument_NoExistingDocument_AddsNewAndReturnsNull()
{
    var candidate = CreateCandidate();

    var oldUrl = candidate.ReplaceDocument("CV", "https://blob.storage/new.pdf");

    oldUrl.Should().BeNull();
    candidate.Documents.Should().HaveCount(1);
}

[Test]
public void ReplaceDocument_CaseInsensitiveType_ReplacesExisting()
{
    var candidate = CreateCandidate();
    candidate.AttachDocument("CV", "https://blob.storage/old.pdf");

    var oldUrl = candidate.ReplaceDocument("cv", "https://blob.storage/new.pdf");

    oldUrl.Should().Be("https://blob.storage/old.pdf");
    candidate.Documents.Should().HaveCount(1);
}

[Test]
public void ReplaceDocument_RaisesDocumentUploadedEvent()
{
    var candidate = CreateCandidate();
    candidate.ClearDomainEvents();

    candidate.ReplaceDocument("CV", "https://blob.storage/new.pdf");

    candidate.DomainEvents.Should().ContainSingle()
        .Which.Should().BeOfType<DocumentUploadedEvent>();
}

[Test]
public void ReplaceDocument_WithWorkdayParams_SetsMetadataOnDocument()
{
    var candidate = CreateCandidate();

    candidate.ReplaceDocument("CV", "https://blob.storage/new.pdf",
        workdayCandidateId: "WD-123", documentSource: DocumentSource.BundleSplit);

    var doc = candidate.Documents.First();
    doc.WorkdayCandidateId.Should().Be("WD-123");
    doc.DocumentSource.Should().Be(DocumentSource.BundleSplit);
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test api/tests/Domain.UnitTests/ --filter "FullyQualifiedName~CandidateTests.ReplaceDocument" --no-restore`
Expected: Build error -- `ReplaceDocument` method does not exist.

**Step 3: Implement ReplaceDocument**

Add to `api/src/Domain/Entities/Candidate.cs` (after `AttachDocument` method):

```csharp
public string? ReplaceDocument(
    string documentType,
    string newBlobStorageUrl,
    string? workdayCandidateId = null,
    DocumentSource documentSource = DocumentSource.IndividualUpload)
{
    var existing = _documents.FirstOrDefault(
        d => d.DocumentType.Equals(documentType, StringComparison.OrdinalIgnoreCase));

    string? oldBlobUrl = null;

    if (existing is not null)
    {
        oldBlobUrl = existing.BlobStorageUrl;
        _documents.Remove(existing);
    }

    var newDoc = CandidateDocument.Create(Id, documentType, newBlobStorageUrl, workdayCandidateId, documentSource);
    _documents.Add(newDoc);
    AddDomainEvent(new DocumentUploadedEvent(Id, newDoc.Id));

    return oldBlobUrl;
}
```

**Step 4: Run tests to verify they pass**

Run: `dotnet test api/tests/Domain.UnitTests/ --filter "FullyQualifiedName~CandidateTests" --no-restore -v quiet`
Expected: ALL PASS (existing + 5 new)

**Step 5: Commit**

```bash
git add api/src/Domain/Entities/Candidate.cs api/tests/Domain.UnitTests/Entities/CandidateTests.cs
git commit -m "feat(3.5): add Candidate.ReplaceDocument domain method + tests"
```

---

## Task 4: ImportDocument domain methods for match status updates + ImportSession tracking

**Testing mode:** Test-first (aggregate invariant)

**Files:**
- Modify: `api/src/Domain/Entities/ImportDocument.cs` -- add `MarkAutoMatched()`, `MarkUnmatched()`, `MarkManuallyAssigned()` methods
- Modify: `api/src/Domain/Entities/ImportSession.cs` -- add `SetMatchCounts()` and `UpdateImportDocumentMatch()` methods
- Modify: `api/tests/Domain.UnitTests/Entities/ImportDocumentTests.cs` -- add tests
- Modify: `api/tests/Domain.UnitTests/Entities/ImportSessionTests.cs` -- add tests

**Step 1: Write failing tests for ImportDocument**

Add to `api/tests/Domain.UnitTests/Entities/ImportDocumentTests.cs`:

```csharp
[Test]
public void MarkAutoMatched_SetStatusAndCandidateId()
{
    // ImportDocument.Create is internal, so we need ImportSession to create one
    var session = ImportSession.Create(Guid.NewGuid(), Guid.NewGuid(), "test.pdf");
    session.AddImportDocument("Alice Johnson", "blob://cv.pdf", null);
    var doc = session.ImportDocuments.First();
    var candidateId = Guid.NewGuid();

    doc.MarkAutoMatched(candidateId);

    doc.MatchStatus.Should().Be(ImportDocumentMatchStatus.AutoMatched);
    doc.MatchedCandidateId.Should().Be(candidateId);
}

[Test]
public void MarkUnmatched_SetsStatusToUnmatched()
{
    var session = ImportSession.Create(Guid.NewGuid(), Guid.NewGuid(), "test.pdf");
    session.AddImportDocument("Unknown", "blob://cv.pdf", null);
    var doc = session.ImportDocuments.First();

    doc.MarkUnmatched();

    doc.MatchStatus.Should().Be(ImportDocumentMatchStatus.Unmatched);
    doc.MatchedCandidateId.Should().BeNull();
}

[Test]
public void MarkManuallyAssigned_SetsStatusAndCandidateId()
{
    var session = ImportSession.Create(Guid.NewGuid(), Guid.NewGuid(), "test.pdf");
    session.AddImportDocument("Person", "blob://cv.pdf", null);
    var doc = session.ImportDocuments.First();
    var candidateId = Guid.NewGuid();

    doc.MarkManuallyAssigned(candidateId);

    doc.MatchStatus.Should().Be(ImportDocumentMatchStatus.ManuallyAssigned);
    doc.MatchedCandidateId.Should().Be(candidateId);
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test api/tests/Domain.UnitTests/ --filter "FullyQualifiedName~ImportDocumentTests.Mark" --no-restore`
Expected: Build error -- methods do not exist.

**Step 3: Implement ImportDocument match methods**

Modify `api/src/Domain/Entities/ImportDocument.cs` -- add inside the class:

```csharp
public void MarkAutoMatched(Guid candidateId)
{
    MatchStatus = ImportDocumentMatchStatus.AutoMatched;
    MatchedCandidateId = candidateId;
}

public void MarkUnmatched()
{
    MatchStatus = ImportDocumentMatchStatus.Unmatched;
    MatchedCandidateId = null;
}

public void MarkManuallyAssigned(Guid candidateId)
{
    MatchStatus = ImportDocumentMatchStatus.ManuallyAssigned;
    MatchedCandidateId = candidateId;
}
```

**Step 4: Run ImportDocument tests**

Run: `dotnet test api/tests/Domain.UnitTests/ --filter "FullyQualifiedName~ImportDocumentTests" --no-restore -v quiet`
Expected: ALL PASS

**Step 5: Add ImportSession.UpdateImportDocumentMatch method + test**

Add to `api/tests/Domain.UnitTests/Entities/ImportSessionTests.cs`:

```csharp
[Test]
public void UpdateImportDocumentMatch_AutoMatched_UpdatesDocStatus()
{
    var session = ImportSession.Create(Guid.NewGuid(), Guid.NewGuid(), "test.pdf");
    session.AddImportDocument("Alice", "blob://cv.pdf", null);
    var doc = session.ImportDocuments.First();
    var candidateId = Guid.NewGuid();

    session.UpdateImportDocumentMatch(doc.Id, candidateId, ImportDocumentMatchStatus.AutoMatched);

    doc.MatchStatus.Should().Be(ImportDocumentMatchStatus.AutoMatched);
    doc.MatchedCandidateId.Should().Be(candidateId);
}
```

Add to `api/src/Domain/Entities/ImportSession.cs`:

```csharp
public void UpdateImportDocumentMatch(Guid importDocumentId, Guid? candidateId, ImportDocumentMatchStatus status)
{
    var doc = _importDocuments.FirstOrDefault(d => d.Id == importDocumentId)
        ?? throw new ArgumentException($"ImportDocument {importDocumentId} not found in session");

    switch (status)
    {
        case ImportDocumentMatchStatus.AutoMatched:
            doc.MarkAutoMatched(candidateId!.Value);
            break;
        case ImportDocumentMatchStatus.Unmatched:
            doc.MarkUnmatched();
            break;
        case ImportDocumentMatchStatus.ManuallyAssigned:
            doc.MarkManuallyAssigned(candidateId!.Value);
            break;
    }
}
```

**Step 6: Run all domain tests**

Run: `dotnet test api/tests/Domain.UnitTests/ --no-restore -v quiet`
Expected: ALL PASS

**Step 7: Commit**

```bash
git add api/src/Domain/Entities/ImportDocument.cs \
  api/src/Domain/Entities/ImportSession.cs \
  api/tests/Domain.UnitTests/Entities/ImportDocumentTests.cs \
  api/tests/Domain.UnitTests/Entities/ImportSessionTests.cs
git commit -m "feat(3.5): add ImportDocument match status methods + ImportSession coordination"
```

---

## Task 5: DocumentDto + AssignDocumentCommand with handler, validator, and unit tests

**Testing mode:** Test-first (command handler with security checks)

**Files:**
- Create: `api/src/Application/Features/Candidates/Commands/AssignDocument/AssignDocumentCommand.cs`
- Create: `api/src/Application/Features/Candidates/Commands/AssignDocument/AssignDocumentCommandValidator.cs`
- Create: `api/src/Application/Features/Candidates/Commands/AssignDocument/AssignDocumentCommandHandler.cs`
- Create: `api/src/Application/Features/Candidates/Commands/DocumentDto.cs`

**Step 1: Create DocumentDto**

Create `api/src/Application/Features/Candidates/Commands/DocumentDto.cs`:

```csharp
using api.Domain.Entities;

namespace api.Application.Features.Candidates.Commands;

public record DocumentDto
{
    public Guid Id { get; init; }
    public Guid CandidateId { get; init; }
    public string DocumentType { get; init; } = string.Empty;
    public string BlobStorageUrl { get; init; } = string.Empty;
    public DateTimeOffset UploadedAt { get; init; }

    public static DocumentDto From(CandidateDocument entity) => new()
    {
        Id = entity.Id,
        CandidateId = entity.CandidateId,
        DocumentType = entity.DocumentType,
        BlobStorageUrl = entity.BlobStorageUrl,
        UploadedAt = entity.UploadedAt,
    };
}
```

**Step 2: Create AssignDocumentCommand**

Create `api/src/Application/Features/Candidates/Commands/AssignDocument/AssignDocumentCommand.cs`:

```csharp
namespace api.Application.Features.Candidates.Commands.AssignDocument;

public record AssignDocumentCommand(
    Guid RecruitmentId,
    Guid CandidateId,
    string DocumentBlobUrl,
    string DocumentName,
    Guid? ImportSessionId) : IRequest<DocumentDto>;
```

**Step 3: Create AssignDocumentCommandValidator**

Create `api/src/Application/Features/Candidates/Commands/AssignDocument/AssignDocumentCommandValidator.cs`:

```csharp
namespace api.Application.Features.Candidates.Commands.AssignDocument;

public class AssignDocumentCommandValidator : AbstractValidator<AssignDocumentCommand>
{
    public AssignDocumentCommandValidator()
    {
        RuleFor(x => x.RecruitmentId).NotEmpty();
        RuleFor(x => x.CandidateId).NotEmpty();
        RuleFor(x => x.DocumentBlobUrl).NotEmpty();
        RuleFor(x => x.DocumentName).NotEmpty();
    }
}
```

**Step 4: Create AssignDocumentCommandHandler**

Create `api/src/Application/Features/Candidates/Commands/AssignDocument/AssignDocumentCommandHandler.cs`:

```csharp
using api.Application.Common.Interfaces;
using api.Domain.Entities;
using api.Domain.Enums;
using api.Domain.Exceptions;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.Features.Candidates.Commands.AssignDocument;

public class AssignDocumentCommandHandler(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext,
    IBlobStorageService blobStorage)
    : IRequestHandler<AssignDocumentCommand, DocumentDto>
{
    private const string ContainerName = "documents";

    public async Task<DocumentDto> Handle(
        AssignDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var recruitment = await dbContext.Recruitments
            .Include(r => r.Members)
            .FirstOrDefaultAsync(r => r.Id == request.RecruitmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Recruitment), request.RecruitmentId);

        var userId = tenantContext.UserGuid;
        if (userId is null || !recruitment.Members.Any(m => m.UserId == userId))
            throw new ForbiddenAccessException();

        if (recruitment.Status == RecruitmentStatus.Closed)
            throw new RecruitmentClosedException(recruitment.Id);

        var candidate = await dbContext.Candidates
            .Include(c => c.Documents)
            .FirstOrDefaultAsync(c => c.Id == request.CandidateId
                && c.RecruitmentId == request.RecruitmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Candidate), request.CandidateId);

        var oldBlobUrl = candidate.ReplaceDocument("CV", request.DocumentBlobUrl);

        if (oldBlobUrl is not null)
        {
            var blobName = ExtractBlobName(oldBlobUrl);
            await blobStorage.DeleteAsync(ContainerName, blobName, cancellationToken);
        }

        // Update ImportDocument status if from an import session
        if (request.ImportSessionId.HasValue)
        {
            var session = await dbContext.ImportSessions
                .Include(s => s.ImportDocuments)
                .FirstOrDefaultAsync(s => s.Id == request.ImportSessionId.Value, cancellationToken);

            var importDoc = session?.ImportDocuments
                .FirstOrDefault(d => d.BlobStorageUrl == request.DocumentBlobUrl);

            if (importDoc is not null)
            {
                session!.UpdateImportDocumentMatch(
                    importDoc.Id, request.CandidateId, ImportDocumentMatchStatus.ManuallyAssigned);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var newDoc = candidate.Documents.First(d =>
            d.BlobStorageUrl == request.DocumentBlobUrl);
        return DocumentDto.From(newDoc);
    }

    private static string ExtractBlobName(string blobUrl)
    {
        // BlobStorageUrl stores the blob name (path within container)
        return blobUrl;
    }
}
```

**Step 5: Build to verify compilation**

Run: `dotnet build api/src/Application/ --no-restore -v quiet`
Expected: 0 errors

**Step 6: Commit**

```bash
git add api/src/Application/Features/Candidates/Commands/DocumentDto.cs \
  api/src/Application/Features/Candidates/Commands/AssignDocument/
git commit -m "feat(3.5): add AssignDocumentCommand with handler + validation"
```

---

## Task 6: UploadDocumentCommand with handler, validator

**Testing mode:** Test-first (command handler with file validation + blob operations)

**Files:**
- Create: `api/src/Application/Features/Candidates/Commands/UploadDocument/UploadDocumentCommand.cs`
- Create: `api/src/Application/Features/Candidates/Commands/UploadDocument/UploadDocumentCommandValidator.cs`
- Create: `api/src/Application/Features/Candidates/Commands/UploadDocument/UploadDocumentCommandHandler.cs`

**Step 1: Create UploadDocumentCommand**

Create `api/src/Application/Features/Candidates/Commands/UploadDocument/UploadDocumentCommand.cs`:

```csharp
namespace api.Application.Features.Candidates.Commands.UploadDocument;

public record UploadDocumentCommand(
    Guid RecruitmentId,
    Guid CandidateId,
    Stream FileStream,
    string FileName,
    long FileSize) : IRequest<DocumentDto>;
```

**Step 2: Create UploadDocumentCommandValidator**

Create `api/src/Application/Features/Candidates/Commands/UploadDocument/UploadDocumentCommandValidator.cs`:

```csharp
namespace api.Application.Features.Candidates.Commands.UploadDocument;

public class UploadDocumentCommandValidator : AbstractValidator<UploadDocumentCommand>
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    public UploadDocumentCommandValidator()
    {
        RuleFor(x => x.RecruitmentId).NotEmpty();
        RuleFor(x => x.CandidateId).NotEmpty();
        RuleFor(x => x.FileName)
            .NotEmpty()
            .Must(f => f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Only PDF files are accepted.");
        RuleFor(x => x.FileSize)
            .LessThanOrEqualTo(MaxFileSizeBytes)
            .WithMessage("File size must not exceed 10 MB.");
    }
}
```

**Step 3: Create UploadDocumentCommandHandler**

Create `api/src/Application/Features/Candidates/Commands/UploadDocument/UploadDocumentCommandHandler.cs`:

```csharp
using api.Application.Common.Interfaces;
using api.Domain.Entities;
using api.Domain.Enums;
using api.Domain.Exceptions;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.Features.Candidates.Commands.UploadDocument;

public class UploadDocumentCommandHandler(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext,
    IBlobStorageService blobStorage)
    : IRequestHandler<UploadDocumentCommand, DocumentDto>
{
    private const string ContainerName = "documents";

    public async Task<DocumentDto> Handle(
        UploadDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var recruitment = await dbContext.Recruitments
            .Include(r => r.Members)
            .FirstOrDefaultAsync(r => r.Id == request.RecruitmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Recruitment), request.RecruitmentId);

        var userId = tenantContext.UserGuid;
        if (userId is null || !recruitment.Members.Any(m => m.UserId == userId))
            throw new ForbiddenAccessException();

        if (recruitment.Status == RecruitmentStatus.Closed)
            throw new RecruitmentClosedException(recruitment.Id);

        var candidate = await dbContext.Candidates
            .Include(c => c.Documents)
            .FirstOrDefaultAsync(c => c.Id == request.CandidateId
                && c.RecruitmentId == request.RecruitmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Candidate), request.CandidateId);

        // Upload to blob storage
        var docId = Guid.NewGuid();
        var blobName = $"{request.RecruitmentId}/cvs/{docId}.pdf";
        await blobStorage.UploadAsync(ContainerName, blobName,
            request.FileStream, "application/pdf", cancellationToken);

        var oldBlobUrl = candidate.ReplaceDocument("CV", blobName);

        if (oldBlobUrl is not null)
        {
            await blobStorage.DeleteAsync(ContainerName, oldBlobUrl, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var newDoc = candidate.Documents.First(d => d.BlobStorageUrl == blobName);
        return DocumentDto.From(newDoc);
    }
}
```

**Step 4: Build to verify compilation**

Run: `dotnet build api/src/Application/ --no-restore -v quiet`
Expected: 0 errors

**Step 5: Commit**

```bash
git add api/src/Application/Features/Candidates/Commands/UploadDocument/
git commit -m "feat(3.5): add UploadDocumentCommand with handler + validation"
```

---

## Task 7: API endpoints for document upload and assign

**Testing mode:** Test-first (integration boundary)

**Files:**
- Modify: `api/src/Web/Endpoints/CandidateEndpoints.cs` -- add document endpoints

**Step 1: Add endpoints**

Modify `api/src/Web/Endpoints/CandidateEndpoints.cs` to add the two document endpoints:

Add using statements:
```csharp
using api.Application.Features.Candidates.Commands.AssignDocument;
using api.Application.Features.Candidates.Commands.UploadDocument;
using api.Application.Features.Candidates.Commands;
```

Add to `Map()` method:
```csharp
group.MapPost("/{candidateId:guid}/document", UploadDocument)
    .DisableAntiforgery();
group.MapPost("/{candidateId:guid}/document/assign", AssignDocument);
```

Add handler methods:
```csharp
private static async Task<IResult> UploadDocument(
    ISender sender,
    Guid recruitmentId,
    Guid candidateId,
    IFormFile file)
{
    using var stream = file.OpenReadStream();
    var result = await sender.Send(new UploadDocumentCommand(
        recruitmentId, candidateId, stream, file.FileName, file.Length));
    return Results.Ok(result);
}

private static async Task<IResult> AssignDocument(
    ISender sender,
    Guid recruitmentId,
    Guid candidateId,
    AssignDocumentCommand command)
{
    var result = await sender.Send(command with
    {
        RecruitmentId = recruitmentId,
        CandidateId = candidateId
    });
    return Results.Ok(result);
}
```

**Step 2: Build to verify compilation**

Run: `dotnet build api/src/Web/ --no-restore -v quiet`
Expected: 0 errors

**Step 3: Commit**

```bash
git add api/src/Web/Endpoints/CandidateEndpoints.cs
git commit -m "feat(3.5): add document upload + assign API endpoints"
```

---

## Task 8: Integrate auto-matching into import pipeline + DI registration

**Testing mode:** Test-first (integration boundary -- verify matching runs after splitting)

**Files:**
- Modify: `api/src/Infrastructure/Services/ImportPipelineHostedService.cs` -- add auto-matching step after PDF bundle processing
- Modify: `api/src/Infrastructure/DependencyInjection.cs` -- register `IDocumentMatchingEngine`

**Step 1: Register DocumentMatchingEngine in DI**

Add to `api/src/Infrastructure/DependencyInjection.cs` after `IBlobStorageService` registration:

```csharp
builder.Services.AddScoped<IDocumentMatchingEngine, DocumentMatchingEngine>();
```

**Step 2: Extend ImportPipelineHostedService with auto-matching**

Modify `api/src/Infrastructure/Services/ImportPipelineHostedService.cs`:

Add to imports at top:
```csharp
using api.Application.Common.Models;
```

After the PDF bundle processing block and before `await db.SaveChangesAsync(ct)`, add:

```csharp
// Auto-match documents to candidates (Story 3.5)
if (session.ImportDocuments.Any(d => d.MatchStatus == ImportDocumentMatchStatus.Pending))
{
    try
    {
        var documentMatcher = scope.ServiceProvider.GetRequiredService<IDocumentMatchingEngine>();

        var splitDocs = session.ImportDocuments
            .Where(d => d.MatchStatus == ImportDocumentMatchStatus.Pending)
            .Select(d => new SplitDocument(d.CandidateName, d.BlobStorageUrl, d.WorkdayCandidateId))
            .ToList();

        var candidates = await db.Candidates
            .Include(c => c.Documents)
            .Where(c => c.RecruitmentId == request.RecruitmentId)
            .ToListAsync(ct);

        var matchResults = documentMatcher.MatchDocumentsToCandidates(splitDocs, candidates);

        foreach (var result in matchResults)
        {
            var importDoc = session.ImportDocuments
                .First(d => d.BlobStorageUrl == result.Document.BlobStorageUrl);

            session.UpdateImportDocumentMatch(importDoc.Id, result.MatchedCandidateId, result.Status);

            if (result.Status == ImportDocumentMatchStatus.AutoMatched && result.MatchedCandidateId.HasValue)
            {
                var candidate = candidates.First(c => c.Id == result.MatchedCandidateId.Value);
                candidate.ReplaceDocument("CV", result.Document.BlobStorageUrl,
                    result.Document.WorkdayCandidateId, DocumentSource.BundleSplit);
            }
        }
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
        logger.LogError(ex, "Auto-matching failed for session {ImportSessionId}", request.ImportSessionId);
    }
}
```

**Step 3: Build to verify compilation**

Run: `dotnet build api/ --no-restore -v quiet`
Expected: 0 errors, 0 warnings

**Step 4: Run all domain tests to ensure no regressions**

Run: `dotnet test api/tests/Domain.UnitTests/ --no-restore -v quiet`
Expected: ALL PASS

**Step 5: Commit**

```bash
git add api/src/Infrastructure/Services/ImportPipelineHostedService.cs \
  api/src/Infrastructure/DependencyInjection.cs
git commit -m "feat(3.5): integrate auto-matching into import pipeline"
```

---

## Task 9: Frontend API types and client extensions

**Testing mode:** Characterization (thin wrapper)

**Files:**
- Modify: `web/src/lib/api/candidates.types.ts` -- add document types
- Modify: `web/src/lib/api/candidates.ts` -- add document API methods
- Modify: `web/src/lib/api/import.types.ts` -- add PDF/document fields to ImportSessionResponse

**Step 1: Extend candidates.types.ts**

Add to `web/src/lib/api/candidates.types.ts`:

```typescript
export interface CandidateDocumentDto {
  id: string
  candidateId: string
  documentType: string
  blobStorageUrl: string
  uploadedAt: string
}

export interface AssignDocumentRequest {
  documentBlobUrl: string
  documentName: string
  importSessionId?: string
}
```

**Step 2: Extend candidates.ts**

Add to `web/src/lib/api/candidates.ts` -- add import for `apiPostFormData` and new methods:

```typescript
import { apiDelete, apiGet, apiPost, apiPostFormData } from './httpClient'
import type {
  AssignDocumentRequest,
  CandidateDocumentDto,
  CreateCandidateRequest,
  PaginatedCandidateList,
} from './candidates.types'

export const candidateApi = {
  // existing methods...

  uploadDocument: (recruitmentId: string, candidateId: string, file: File) => {
    const formData = new FormData()
    formData.append('file', file)
    return apiPostFormData<CandidateDocumentDto>(
      `/recruitments/${recruitmentId}/candidates/${candidateId}/document`,
      formData,
    )
  },

  assignDocument: (
    recruitmentId: string,
    candidateId: string,
    data: AssignDocumentRequest,
  ) =>
    apiPost<CandidateDocumentDto>(
      `/recruitments/${recruitmentId}/candidates/${candidateId}/document/assign`,
      data,
    ),
}
```

**Step 3: Extend import.types.ts**

Add to `ImportSessionResponse` in `web/src/lib/api/import.types.ts`:

```typescript
pdfTotalCandidates: number | null
pdfSplitCandidates: number | null
pdfSplitErrors: number
originalBundleBlobUrl: string | null
importDocuments: ImportDocumentDto[]
```

Add new type:

```typescript
export interface ImportDocumentDto {
  id: string
  candidateName: string
  blobStorageUrl: string
  workdayCandidateId: string | null
  matchStatus: 'Pending' | 'AutoMatched' | 'Unmatched' | 'ManuallyAssigned'
  matchedCandidateId: string | null
}
```

**Step 4: Build frontend to verify**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic3/web && npx tsc --noEmit`
Expected: 0 errors

**Step 5: Commit**

```bash
git add web/src/lib/api/candidates.types.ts \
  web/src/lib/api/candidates.ts \
  web/src/lib/api/import.types.ts
git commit -m "feat(3.5): add candidates API client with document upload/assign methods"
```

---

## Task 10: MSW handlers for document endpoints + fixture updates

**Testing mode:** Characterization (test infrastructure)

**Files:**
- Modify: `web/src/mocks/candidateHandlers.ts` -- add document endpoint handlers
- Modify: `web/src/mocks/importHandlers.ts` -- add importDocuments to mock session
- Modify: `web/src/mocks/fixtures/candidates.ts` -- add document fixture data

**Step 1: Update fixtures**

Add to `web/src/mocks/fixtures/candidates.ts`:

```typescript
import type { CandidateDocumentDto } from '@/lib/api/candidates.types'

export const mockDocumentId = 'doc-1111-1111-1111-111111111111'

export const mockCandidateDocument: CandidateDocumentDto = {
  id: mockDocumentId,
  candidateId: mockCandidateId1,
  documentType: 'CV',
  blobStorageUrl: 'recruitment-1/cvs/doc-1.pdf',
  uploadedAt: '2026-02-14T12:00:00Z',
}
```

**Step 2: Add MSW handlers**

Add to `web/src/mocks/candidateHandlers.ts`:

```typescript
http.post(
  '/api/recruitments/:recruitmentId/candidates/:candidateId/document',
  async ({ params }) => {
    return HttpResponse.json(
      {
        id: `doc-new-${Date.now()}`,
        candidateId: params.candidateId as string,
        documentType: 'CV',
        blobStorageUrl: `recruitment-1/cvs/new-doc.pdf`,
        uploadedAt: new Date().toISOString(),
      },
      { status: 200 },
    )
  },
),

http.post(
  '/api/recruitments/:recruitmentId/candidates/:candidateId/document/assign',
  async ({ params }) => {
    return HttpResponse.json(
      {
        id: `doc-assigned-${Date.now()}`,
        candidateId: params.candidateId as string,
        documentType: 'CV',
        blobStorageUrl: 'recruitment-1/cvs/assigned-doc.pdf',
        uploadedAt: new Date().toISOString(),
      },
      { status: 200 },
    )
  },
),
```

**Step 3: Update import mock session with importDocuments**

Modify `web/src/mocks/importHandlers.ts` `mockCompletedSession` to include:

```typescript
pdfTotalCandidates: null,
pdfSplitCandidates: null,
pdfSplitErrors: 0,
originalBundleBlobUrl: null,
importDocuments: [],
```

**Step 4: Build frontend to verify**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic3/web && npx tsc --noEmit`
Expected: 0 errors

**Step 5: Run existing frontend tests to verify no regressions**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic3/web && npx vitest run --reporter=verbose`
Expected: ALL PASS

**Step 6: Commit**

```bash
git add web/src/mocks/candidateHandlers.ts \
  web/src/mocks/importHandlers.ts \
  web/src/mocks/fixtures/candidates.ts
git commit -m "feat(3.5): add MSW handlers and fixtures for document endpoints"
```

---

## Task 11: DocumentUpload component + useDocumentUpload hook + CandidateDetail page + tests

**Testing mode:** Test-first (user-facing upload flow with validation)

**Files:**
- Create: `web/src/features/candidates/hooks/useDocumentUpload.ts`
- Create: `web/src/features/candidates/DocumentUpload.tsx`
- Create: `web/src/features/candidates/DocumentUpload.test.tsx`
- Create: `web/src/features/candidates/CandidateDetail.tsx`
- Modify: `web/src/features/candidates/CandidateList.tsx` -- add navigation to CandidateDetail
- Modify: `web/src/routes/index.tsx` -- add CandidateDetail route

**Step 1: Create useDocumentUpload hook**

Create `web/src/features/candidates/hooks/useDocumentUpload.ts`:

```typescript
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { candidateApi } from '@/lib/api/candidates'

export function useDocumentUpload(
  recruitmentId: string,
  candidateId: string,
) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (file: File) =>
      candidateApi.uploadDocument(recruitmentId, candidateId, file),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: ['candidates', recruitmentId],
      })
    },
  })
}
```

**Step 2: Write failing tests for DocumentUpload**

Create `web/src/features/candidates/DocumentUpload.test.tsx`:

```typescript
import { describe, expect, it, vi } from 'vitest'
import userEvent from '@testing-library/user-event'
import { DocumentUpload } from './DocumentUpload'
import { render, screen, waitFor } from '@/test-utils'
import type { CandidateDocumentDto } from '@/lib/api/candidates.types'

const defaultProps = {
  recruitmentId: '550e8400-e29b-41d4-a716-446655440000',
  candidateId: 'cand-1111-1111-1111-111111111111',
  existingDocument: null as CandidateDocumentDto | null,
  isClosed: false,
}

describe('DocumentUpload', () => {
  it('should render upload area when no document exists', () => {
    render(<DocumentUpload {...defaultProps} />)
    expect(screen.getByText(/no cv uploaded/i)).toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: /upload cv/i }),
    ).toBeInTheDocument()
  })

  it('should display current document info when document exists', () => {
    const doc: CandidateDocumentDto = {
      id: 'doc-1',
      candidateId: defaultProps.candidateId,
      documentType: 'CV',
      blobStorageUrl: 'blob://cv.pdf',
      uploadedAt: '2026-02-14T12:00:00Z',
    }
    render(<DocumentUpload {...defaultProps} existingDocument={doc} />)
    expect(screen.getByText(/cv uploaded/i)).toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: /replace/i }),
    ).toBeInTheDocument()
  })

  it('should validate file type and reject non-PDF', async () => {
    const user = userEvent.setup()
    render(<DocumentUpload {...defaultProps} />)

    const input = screen.getByTestId('file-input') as HTMLInputElement
    const textFile = new File(['hello'], 'test.txt', { type: 'text/plain' })
    await user.upload(input, textFile)

    expect(screen.getByText(/only pdf files/i)).toBeInTheDocument()
  })

  it('should validate file size and reject files over 10 MB', async () => {
    const user = userEvent.setup()
    render(<DocumentUpload {...defaultProps} />)

    const input = screen.getByTestId('file-input') as HTMLInputElement
    const largeFile = new File(
      [new ArrayBuffer(11 * 1024 * 1024)],
      'large.pdf',
      { type: 'application/pdf' },
    )
    await user.upload(input, largeFile)

    expect(screen.getByText(/must not exceed 10 mb/i)).toBeInTheDocument()
  })

  it('should hide upload controls when recruitment is closed', () => {
    render(<DocumentUpload {...defaultProps} isClosed={true} />)
    expect(
      screen.queryByRole('button', { name: /upload cv/i }),
    ).not.toBeInTheDocument()
  })

  it('should show replacement confirmation when document exists', async () => {
    const user = userEvent.setup()
    const doc: CandidateDocumentDto = {
      id: 'doc-1',
      candidateId: defaultProps.candidateId,
      documentType: 'CV',
      blobStorageUrl: 'blob://cv.pdf',
      uploadedAt: '2026-02-14T12:00:00Z',
    }
    render(<DocumentUpload {...defaultProps} existingDocument={doc} />)

    const input = screen.getByTestId('file-input') as HTMLInputElement
    const file = new File(['pdf-content'], 'cv.pdf', {
      type: 'application/pdf',
    })
    await user.upload(input, file)

    expect(
      screen.getByText(/this will replace the existing cv/i),
    ).toBeInTheDocument()
  })
})
```

**Step 3: Run tests to verify they fail**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic3/web && npx vitest run src/features/candidates/DocumentUpload.test.tsx`
Expected: FAIL -- `DocumentUpload` module not found.

**Step 4: Implement DocumentUpload component**

Create `web/src/features/candidates/DocumentUpload.tsx`:

```typescript
import { useRef, useState } from 'react'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { Button } from '@/components/ui/button'
import { useAppToast } from '@/hooks/useAppToast'
import { useDocumentUpload } from './hooks/useDocumentUpload'
import type { CandidateDocumentDto } from '@/lib/api/candidates.types'
import { ApiError } from '@/lib/api/httpClient'

const MAX_FILE_SIZE = 10 * 1024 * 1024 // 10 MB

interface DocumentUploadProps {
  recruitmentId: string
  candidateId: string
  existingDocument: CandidateDocumentDto | null
  isClosed: boolean
}

export function DocumentUpload({
  recruitmentId,
  candidateId,
  existingDocument,
  isClosed,
}: DocumentUploadProps) {
  const inputRef = useRef<HTMLInputElement>(null)
  const [validationError, setValidationError] = useState<string | null>(null)
  const [pendingFile, setPendingFile] = useState<File | null>(null)
  const [showConfirm, setShowConfirm] = useState(false)
  const toast = useAppToast()
  const uploadMutation = useDocumentUpload(recruitmentId, candidateId)

  if (isClosed) {
    return existingDocument ? (
      <div className="text-muted-foreground text-sm">
        CV uploaded on{' '}
        {new Date(existingDocument.uploadedAt).toLocaleDateString()}
      </div>
    ) : (
      <div className="text-muted-foreground text-sm">No CV uploaded</div>
    )
  }

  function handleFileChange(e: React.ChangeEvent<HTMLInputElement>) {
    setValidationError(null)
    const file = e.target.files?.[0]
    if (!file) return

    if (!file.name.endsWith('.pdf')) {
      setValidationError('Only PDF files are accepted.')
      return
    }
    if (file.size > MAX_FILE_SIZE) {
      setValidationError('File must not exceed 10 MB.')
      return
    }

    if (existingDocument) {
      setPendingFile(file)
      setShowConfirm(true)
    } else {
      doUpload(file)
    }
  }

  function doUpload(file: File) {
    uploadMutation.mutate(file, {
      onSuccess: () => {
        toast.success('CV uploaded successfully')
        if (inputRef.current) inputRef.current.value = ''
      },
      onError: (error) => {
        if (error instanceof ApiError) {
          toast.error(error.problemDetails.title)
        } else {
          toast.error('Failed to upload CV')
        }
      },
    })
  }

  function handleConfirmReplace() {
    if (pendingFile) {
      doUpload(pendingFile)
      setPendingFile(null)
    }
    setShowConfirm(false)
  }

  return (
    <div className="space-y-2">
      {existingDocument ? (
        <p className="text-sm">
          CV uploaded on{' '}
          {new Date(existingDocument.uploadedAt).toLocaleDateString()}
        </p>
      ) : (
        <p className="text-muted-foreground text-sm">No CV uploaded</p>
      )}

      <input
        ref={inputRef}
        data-testid="file-input"
        type="file"
        accept=".pdf"
        className="hidden"
        onChange={handleFileChange}
      />

      <Button
        variant="outline"
        size="sm"
        disabled={uploadMutation.isPending}
        onClick={() => inputRef.current?.click()}
      >
        {uploadMutation.isPending
          ? 'Uploading...'
          : existingDocument
            ? 'Replace CV'
            : 'Upload CV'}
      </Button>

      {validationError && (
        <p className="text-destructive text-sm">{validationError}</p>
      )}

      <AlertDialog open={showConfirm} onOpenChange={setShowConfirm}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Replace CV</AlertDialogTitle>
            <AlertDialogDescription>
              This will replace the existing CV. This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel
              onClick={() => {
                setPendingFile(null)
                if (inputRef.current) inputRef.current.value = ''
              }}
            >
              Cancel
            </AlertDialogCancel>
            <AlertDialogAction onClick={handleConfirmReplace}>
              Replace
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}
```

**Step 5: Create CandidateDetail page**

Create `web/src/features/candidates/CandidateDetail.tsx`:

```typescript
import { useParams, Link } from 'react-router'
import { useCandidates } from './hooks/useCandidates'
import { DocumentUpload } from './DocumentUpload'
import { SkeletonLoader } from '@/components/SkeletonLoader'
import { Button } from '@/components/ui/button'
import { ArrowLeft } from 'lucide-react'

interface CandidateDetailProps {
  isClosed: boolean
}

export function CandidateDetail({ isClosed }: CandidateDetailProps) {
  const { recruitmentId, candidateId } = useParams<{
    recruitmentId: string
    candidateId: string
  }>()

  const { data, isPending } = useCandidates(recruitmentId ?? '')

  if (isPending) {
    return <SkeletonLoader variant="card" />
  }

  const candidate = data?.items.find((c) => c.id === candidateId)

  if (!candidate || !recruitmentId || !candidateId) {
    return <p className="text-muted-foreground">Candidate not found.</p>
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-2">
        <Button variant="ghost" size="sm" asChild>
          <Link to={`/recruitments/${recruitmentId}`}>
            <ArrowLeft className="mr-1 size-4" />
            Back
          </Link>
        </Button>
      </div>

      <div>
        <h2 className="text-lg font-semibold">{candidate.fullName}</h2>
        <p className="text-muted-foreground text-sm">{candidate.email}</p>
        {candidate.phoneNumber && (
          <p className="text-muted-foreground text-sm">
            {candidate.phoneNumber}
          </p>
        )}
        {candidate.location && (
          <p className="text-muted-foreground text-sm">
            {candidate.location}
          </p>
        )}
      </div>

      <div>
        <h3 className="mb-2 text-sm font-medium">Document</h3>
        <DocumentUpload
          recruitmentId={recruitmentId}
          candidateId={candidateId}
          existingDocument={null}
          isClosed={isClosed}
        />
      </div>
    </div>
  )
}
```

**Step 6: Add route for CandidateDetail**

Modify `web/src/routes/index.tsx` -- add import and route:

```typescript
import { CandidateDetailPage } from '@/features/candidates/CandidateDetailPage'
```

Add route entry inside the ProtectedRoute children:
```typescript
{ path: '/recruitments/:recruitmentId/candidates/:candidateId', element: <CandidateDetailPage /> },
```

Note: You may need to create a thin wrapper `CandidateDetailPage.tsx` that resolves recruitment status, or pass `isClosed` via context. The exact approach depends on how `RecruitmentPage` currently works. Follow existing patterns for getting recruitment data.

**Step 7: Add navigation link in CandidateList**

Modify `web/src/features/candidates/CandidateList.tsx` -- wrap candidate name in a Link:

In the candidate list row, change the `<p className="font-medium">` to:
```typescript
<Link
  to={`/recruitments/${recruitmentId}/candidates/${candidate.id}`}
  className="font-medium hover:underline"
>
  {candidate.fullName}
</Link>
```

Add `import { Link } from 'react-router'` at top.

**Step 8: Run DocumentUpload tests**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic3/web && npx vitest run src/features/candidates/DocumentUpload.test.tsx`
Expected: ALL PASS

**Step 9: Run all frontend tests to verify no regressions**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic3/web && npx vitest run --reporter=verbose`
Expected: ALL PASS

**Step 10: Commit**

```bash
git add web/src/features/candidates/hooks/useDocumentUpload.ts \
  web/src/features/candidates/DocumentUpload.tsx \
  web/src/features/candidates/DocumentUpload.test.tsx \
  web/src/features/candidates/CandidateDetail.tsx \
  web/src/features/candidates/CandidateList.tsx \
  web/src/routes/index.tsx
git commit -m "feat(3.5): add DocumentUpload component + CandidateDetail page + tests"
```

---

## Task 12: Unmatched document assignment UI in ImportSummary + tests

**Testing mode:** Test-first (user-facing assignment flow)

**Files:**
- Modify: `web/src/features/candidates/ImportFlow/ImportSummary.tsx` -- add unmatched documents section
- Modify: `web/src/features/candidates/ImportFlow/ImportSummary.test.tsx` -- add tests

**Step 1: Write failing tests**

Add to `web/src/features/candidates/ImportFlow/ImportSummary.test.tsx`:

```typescript
import type { ImportDocumentDto } from '@/lib/api/import.types'

const unmatchedDocs: ImportDocumentDto[] = [
  {
    id: 'doc-1',
    candidateName: 'Unmatched Person',
    blobStorageUrl: 'blob://unmatched.pdf',
    workdayCandidateId: null,
    matchStatus: 'Unmatched',
    matchedCandidateId: null,
  },
]

describe('ImportSummary - Unmatched Documents', () => {
  it('should display unmatched documents section when unmatched count > 0', () => {
    render(
      <ImportSummary
        {...defaultProps}
        importDocuments={unmatchedDocs}
        recruitmentId="550e8400-e29b-41d4-a716-446655440000"
      />,
    )
    expect(screen.getByText(/unmatched documents/i)).toBeInTheDocument()
    expect(screen.getByText('Unmatched Person')).toBeInTheDocument()
  })

  it('should not display unmatched section when all documents matched', () => {
    render(
      <ImportSummary
        {...defaultProps}
        importDocuments={[]}
        recruitmentId="550e8400-e29b-41d4-a716-446655440000"
      />,
    )
    expect(
      screen.queryByText(/unmatched documents/i),
    ).not.toBeInTheDocument()
  })

  it('should hide assign buttons when recruitment is closed', () => {
    render(
      <ImportSummary
        {...defaultProps}
        importDocuments={unmatchedDocs}
        recruitmentId="550e8400-e29b-41d4-a716-446655440000"
        isClosed={true}
      />,
    )
    expect(
      screen.queryByRole('button', { name: /assign/i }),
    ).not.toBeInTheDocument()
  })
})
```

**Step 2: Run tests to verify they fail**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic3/web && npx vitest run src/features/candidates/ImportFlow/ImportSummary.test.tsx`
Expected: FAIL -- props `importDocuments`, `recruitmentId`, `isClosed` do not exist on `ImportSummaryProps`.

**Step 3: Extend ImportSummary with unmatched documents section**

Modify `web/src/features/candidates/ImportFlow/ImportSummary.tsx`:

Add to `ImportSummaryProps`:
```typescript
importDocuments?: ImportDocumentDto[]
recruitmentId?: string
isClosed?: boolean
importSessionId?: string
```

Add import:
```typescript
import type { ImportDocumentDto } from '@/lib/api/import.types'
```

After the RowDetailSections and before the Done button, add the UnmatchedDocuments section:

```typescript
{unmatchedDocs.length > 0 && (
  <UnmatchedDocumentsSection
    documents={unmatchedDocs}
    recruitmentId={recruitmentId}
    isClosed={isClosed}
    importSessionId={importSessionId}
    candidates={[]} // Populated from a query in the real implementation
  />
)}
```

Where `unmatchedDocs` is derived at the top of the component:
```typescript
const unmatchedDocs = (importDocuments ?? []).filter(
  (d) => d.matchStatus === 'Unmatched',
)
```

Create `UnmatchedDocumentsSection` component at the bottom of the file (or as a separate file -- implementer's choice based on complexity):

```typescript
function UnmatchedDocumentsSection({
  documents,
  recruitmentId,
  isClosed,
  importSessionId,
}: {
  documents: ImportDocumentDto[]
  recruitmentId?: string
  isClosed?: boolean
  importSessionId?: string
  candidates: CandidateResponse[]
}) {
  // Each doc shows: name from TOC + Assign button (if not closed)
  // Assign button can open a combobox to select candidate
  // On assignment: call candidateApi.assignDocument(), show toast
  return (
    <div className="space-y-2">
      <h3 className="text-sm font-medium">
        Unmatched Documents ({documents.length})
      </h3>
      <div className="divide-y rounded-md border text-sm">
        {documents.map((doc) => (
          <div
            key={doc.id}
            className="flex items-center justify-between px-3 py-2"
          >
            <span>{doc.candidateName}</span>
            {!isClosed && (
              <Button variant="outline" size="sm">
                Assign
              </Button>
            )}
          </div>
        ))}
      </div>
    </div>
  )
}
```

Note: The full candidate selection combobox with search is a UI enhancement. The base implementation shows the assign button and document names. The implementer should add a simple Select/Combobox that loads candidates via `useCandidates(recruitmentId)`, prioritizing candidates without documents.

**Step 4: Run tests**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic3/web && npx vitest run src/features/candidates/ImportFlow/ImportSummary.test.tsx`
Expected: ALL PASS (existing + new)

**Step 5: Commit**

```bash
git add web/src/features/candidates/ImportFlow/ImportSummary.tsx \
  web/src/features/candidates/ImportFlow/ImportSummary.test.tsx
git commit -m "feat(3.5): add unmatched document assignment UI to ImportSummary"
```

---

## Task 13: Full verification and Dev Agent Record

**Testing mode:** Verification

**Step 1: Run all backend tests**

Run: `dotnet test api/tests/Domain.UnitTests/ --no-restore -v quiet`
Expected: ALL PASS

**Step 2: Build entire solution**

Run: `dotnet build api/ --no-restore -v quiet`
Expected: 0 errors, 0 warnings

**Step 3: Run all frontend tests**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic3/web && npx vitest run --reporter=verbose`
Expected: ALL PASS

**Step 4: TypeScript compilation check**

Run: `cd /home/thomasg/Projects/Web/recruitment-tracker-epic3/web && npx tsc --noEmit`
Expected: 0 errors

**Step 5: Anti-pattern scan**

Run: `grep -r "dbContext\.CandidateDocuments\." api/src/ --include="*.cs"` (should return nothing -- documents accessed through aggregate root)

Run: `grep -r "dbContext\.ImportDocuments\." api/src/ --include="*.cs"` (should return nothing -- ImportDocuments accessed through ImportSession)

**Step 6: Fill Dev Agent Record in story file**

Update `_bmad-output/implementation-artifacts/3-5-cv-auto-match-manual-assignment-individual-upload.md`:
- Set status to `done`
- Fill Agent Model Used
- Fill testing rationale, key decisions, debug log, completion notes, file list

**Step 7: Commit**

```bash
git add _bmad-output/implementation-artifacts/3-5-cv-auto-match-manual-assignment-individual-upload.md
git commit -m "feat(3.5): mark story 3.5 done with Dev Agent Record"
```

---

## Dependency graph

```
Task 1 (NameNormalizer) ──────────┐
                                  ├── Task 2 (DocumentMatchingEngine) ── Task 8 (Pipeline integration)
Task 3 (Candidate.ReplaceDocument)┤
                                  ├── Task 5 (AssignDocumentCommand) ── Task 7 (API endpoints)
Task 4 (ImportDocument methods) ──┤
                                  └── Task 6 (UploadDocumentCommand) ── Task 7 (API endpoints)

Task 9 (Frontend types/client) ── Task 10 (MSW handlers) ── Task 11 (DocumentUpload + CandidateDetail)
                                                          ── Task 12 (Unmatched assignment UI)

Task 13 (Full verification) depends on all above.
```

Tasks 1, 3, 4 can run in parallel. Tasks 5, 6 can run in parallel after Task 3. Task 9 can start once frontend types are clear (after Task 5/6 define DTOs).
