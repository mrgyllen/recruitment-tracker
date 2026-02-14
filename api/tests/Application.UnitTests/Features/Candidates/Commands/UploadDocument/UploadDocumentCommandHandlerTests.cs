using api.Application.Common.Interfaces;
using api.Application.Features.Candidates.Commands;
using api.Application.Features.Candidates.Commands.UploadDocument;
using api.Domain.Entities;
using api.Domain.Enums;
using api.Domain.Exceptions;
using FluentAssertions;
using MockQueryable.NSubstitute;
using NSubstitute;
using NUnit.Framework;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.UnitTests.Features.Candidates.Commands.UploadDocument;

[TestFixture]
public class UploadDocumentCommandHandlerTests
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
    }

    [Test]
    public async Task Handle_ValidRequest_UploadsAndAssignsDocument()
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

        using var fileStream = new MemoryStream([0x01, 0x02, 0x03]);
        var handler = new UploadDocumentCommandHandler(_dbContext, _tenantContext, _blobStorage);
        var command = new UploadDocumentCommand(
            recruitment.Id,
            candidate.Id,
            fileStream,
            "resume.pdf",
            1024);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.CandidateId.Should().Be(candidate.Id);
        result.DocumentType.Should().Be("CV");
        result.BlobStorageUrl.Should().Contain($"{recruitment.Id}/cvs/");
        result.BlobStorageUrl.Should().EndWith(".pdf");
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_RecruitmentNotFound_ThrowsNotFoundException()
    {
        _tenantContext.UserGuid.Returns(Guid.NewGuid());

        var recruitmentMockSet = new List<Recruitment>().AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(recruitmentMockSet);

        using var fileStream = new MemoryStream([0x01]);
        var handler = new UploadDocumentCommandHandler(_dbContext, _tenantContext, _blobStorage);
        var command = new UploadDocumentCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            fileStream,
            "resume.pdf",
            1024);

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

        using var fileStream = new MemoryStream([0x01]);
        var handler = new UploadDocumentCommandHandler(_dbContext, _tenantContext, _blobStorage);
        var command = new UploadDocumentCommand(
            recruitment.Id,
            Guid.NewGuid(),
            fileStream,
            "resume.pdf",
            1024);

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

        using var fileStream = new MemoryStream([0x01]);
        var handler = new UploadDocumentCommandHandler(_dbContext, _tenantContext, _blobStorage);
        var command = new UploadDocumentCommand(
            recruitment.Id,
            Guid.NewGuid(),
            fileStream,
            "resume.pdf",
            1024);

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

        using var fileStream = new MemoryStream([0x01]);
        var handler = new UploadDocumentCommandHandler(_dbContext, _tenantContext, _blobStorage);
        var command = new UploadDocumentCommand(
            recruitment.Id,
            Guid.NewGuid(),
            fileStream,
            "resume.pdf",
            1024);

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task Handle_ValidRequest_CallsBlobUploadAsync()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);

        var recruitment = Recruitment.Create("Test", null, userId);
        var recruitmentMockSet = new List<Recruitment> { recruitment }.AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(recruitmentMockSet);

        var candidate = Candidate.Create(
            recruitment.Id, "Jane Doe", "jane@example.com", null, null, DateTimeOffset.UtcNow);
        var candidateMockSet = new List<Candidate> { candidate }.AsQueryable().BuildMockDbSet();
        _dbContext.Candidates.Returns(candidateMockSet);

        using var fileStream = new MemoryStream([0x01, 0x02]);
        var handler = new UploadDocumentCommandHandler(_dbContext, _tenantContext, _blobStorage);
        var command = new UploadDocumentCommand(
            recruitment.Id,
            candidate.Id,
            fileStream,
            "resume.pdf",
            2048);

        await handler.Handle(command, CancellationToken.None);

        await _blobStorage.Received(1).UploadAsync(
            "documents",
            Arg.Is<string>(s => s.StartsWith($"{recruitment.Id}/cvs/") && s.EndsWith(".pdf")),
            fileStream,
            "application/pdf",
            Arg.Any<CancellationToken>());
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

        using var fileStream = new MemoryStream([0x01]);
        var handler = new UploadDocumentCommandHandler(_dbContext, _tenantContext, _blobStorage);
        var command = new UploadDocumentCommand(
            recruitment.Id,
            candidate.Id,
            fileStream,
            "resume.pdf",
            1024);

        await handler.Handle(command, CancellationToken.None);

        await _blobStorage.Received(1).DeleteAsync(
            "documents", "blob/old-cv.pdf", Arg.Any<CancellationToken>());
    }
}
