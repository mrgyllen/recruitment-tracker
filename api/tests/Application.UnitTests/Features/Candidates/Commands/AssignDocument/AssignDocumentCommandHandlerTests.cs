using api.Application.Common.Interfaces;
using api.Application.Features.Candidates.Commands;
using api.Application.Features.Candidates.Commands.AssignDocument;
using api.Domain.Entities;
using api.Domain.Enums;
using api.Domain.Exceptions;
using FluentAssertions;
using MockQueryable.NSubstitute;
using NSubstitute;
using NUnit.Framework;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.UnitTests.Features.Candidates.Commands.AssignDocument;

[TestFixture]
public class AssignDocumentCommandHandlerTests
{
    private IApplicationDbContext _dbContext = null!;
    private ITenantContext _tenantContext = null!;
    private IBlobStorageService _blobStorage = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _tenantContext = Substitute.For<ITenantContext>();
        _blobStorage = Substitute.For<IBlobStorageService>();
        _blobStorage.VerifyBlobOwnership(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid>()).Returns(true);
    }

    [Test]
    public async Task Handle_ValidRequest_AssignsDocumentAndReturnsDto()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);

        var recruitment = Recruitment.Create("Test Recruitment", null, userId);
        var recruitmentMockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(recruitmentMockSet);

        var candidate = Candidate.Create(
            recruitment.Id, "Jane Doe", "jane@example.com", null, null, DateTimeOffset.UtcNow);
        var candidateMockSet = new List<Candidate> { candidate }.AsQueryable().BuildMockDbSet();
        _dbContext.Candidates.Returns(candidateMockSet);

        var handler = new AssignDocumentCommandHandler(_dbContext, _tenantContext, _blobStorage);
        var command = new AssignDocumentCommand(
            recruitment.Id,
            candidate.Id,
            "blob/cv-new.pdf",
            "cv.pdf",
            null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.CandidateId.Should().Be(candidate.Id);
        result.DocumentType.Should().Be("CV");
        result.BlobStorageUrl.Should().Be("blob/cv-new.pdf");
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_RecruitmentNotFound_ThrowsNotFoundException()
    {
        _tenantContext.UserGuid.Returns(Guid.NewGuid());

        var recruitmentMockSet = new List<Recruitment>().AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(recruitmentMockSet);

        var handler = new AssignDocumentCommandHandler(_dbContext, _tenantContext, _blobStorage);
        var command = new AssignDocumentCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "blob/cv.pdf",
            "cv.pdf",
            null);

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task Handle_UserNotMember_ThrowsForbiddenAccessException()
    {
        var creatorId = Guid.NewGuid();
        var nonMemberId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(nonMemberId);

        var recruitment = Recruitment.Create("Test", null, creatorId);
        var recruitmentMockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(recruitmentMockSet);

        var handler = new AssignDocumentCommandHandler(_dbContext, _tenantContext, _blobStorage);
        var command = new AssignDocumentCommand(
            recruitment.Id,
            Guid.NewGuid(),
            "blob/cv.pdf",
            "cv.pdf",
            null);

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task Handle_ClosedRecruitment_ThrowsRecruitmentClosedException()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);

        var recruitment = Recruitment.Create("Test", null, userId);
        recruitment.Close();
        var recruitmentMockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(recruitmentMockSet);

        var handler = new AssignDocumentCommandHandler(_dbContext, _tenantContext, _blobStorage);
        var command = new AssignDocumentCommand(
            recruitment.Id,
            Guid.NewGuid(),
            "blob/cv.pdf",
            "cv.pdf",
            null);

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<RecruitmentClosedException>();
    }

    [Test]
    public async Task Handle_CandidateNotFound_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);

        var recruitment = Recruitment.Create("Test", null, userId);
        var recruitmentMockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(recruitmentMockSet);

        var candidateMockSet = new List<Candidate>().AsQueryable().BuildMockDbSet();
        _dbContext.Candidates.Returns(candidateMockSet);

        var handler = new AssignDocumentCommandHandler(_dbContext, _tenantContext, _blobStorage);
        var command = new AssignDocumentCommand(
            recruitment.Id,
            Guid.NewGuid(),
            "blob/cv.pdf",
            "cv.pdf",
            null);

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task Handle_WithExistingDocument_DeletesOldBlob()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);

        var recruitment = Recruitment.Create("Test", null, userId);
        var recruitmentMockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(recruitmentMockSet);

        var candidate = Candidate.Create(
            recruitment.Id, "Jane Doe", "jane@example.com", null, null, DateTimeOffset.UtcNow);
        // Attach an existing CV document so ReplaceDocument returns the old URL
        candidate.ReplaceDocument("CV", "blob/old-cv.pdf");
        var candidateMockSet = new List<Candidate> { candidate }.AsQueryable().BuildMockDbSet();
        _dbContext.Candidates.Returns(candidateMockSet);

        var handler = new AssignDocumentCommandHandler(_dbContext, _tenantContext, _blobStorage);
        var command = new AssignDocumentCommand(
            recruitment.Id,
            candidate.Id,
            "blob/new-cv.pdf",
            "cv.pdf",
            null);

        await handler.Handle(command, CancellationToken.None);

        await _blobStorage.Received(1).DeleteAsync(
            "documents", "blob/old-cv.pdf", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithoutExistingDocument_DoesNotDeleteBlob()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);

        var recruitment = Recruitment.Create("Test", null, userId);
        var recruitmentMockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(recruitmentMockSet);

        var candidate = Candidate.Create(
            recruitment.Id, "Jane Doe", "jane@example.com", null, null, DateTimeOffset.UtcNow);
        // No existing document — ReplaceDocument will return null
        var candidateMockSet = new List<Candidate> { candidate }.AsQueryable().BuildMockDbSet();
        _dbContext.Candidates.Returns(candidateMockSet);

        var handler = new AssignDocumentCommandHandler(_dbContext, _tenantContext, _blobStorage);
        var command = new AssignDocumentCommand(
            recruitment.Id,
            candidate.Id,
            "blob/cv.pdf",
            "cv.pdf",
            null);

        await handler.Handle(command, CancellationToken.None);

        await _blobStorage.DidNotReceive().DeleteAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
