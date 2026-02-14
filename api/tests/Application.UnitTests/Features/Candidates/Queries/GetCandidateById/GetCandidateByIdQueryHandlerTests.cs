using api.Application.Common.Interfaces;
using api.Application.Features.Candidates.Queries.GetCandidateById;
using api.Domain.Entities;
using api.Domain.Enums;
using FluentAssertions;
using MockQueryable.NSubstitute;
using NSubstitute;
using NUnit.Framework;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.UnitTests.Features.Candidates.Queries.GetCandidateById;

[TestFixture]
public class GetCandidateByIdQueryHandlerTests
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

    private Recruitment SetUpRecruitmentWithSteps(Guid userId)
    {
        _tenantContext.UserGuid.Returns(userId);

        var recruitment = Recruitment.Create("Test", null, userId);
        recruitment.AddStep("Screening", 1);
        recruitment.AddStep("Interview", 2);

        var recruitmentMockSet = new List<Recruitment> { recruitment }
            .AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(recruitmentMockSet);

        return recruitment;
    }

    [Test]
    public async Task Handle_ValidRequest_ReturnsCandidateWithAllFields()
    {
        var userId = Guid.NewGuid();
        var recruitment = SetUpRecruitmentWithSteps(userId);

        var candidate = Candidate.Create(
            recruitment.Id, "Alice", "alice@example.com", "555-1234", "NYC", DateTimeOffset.UtcNow);

        var candidateMockSet = new List<Candidate> { candidate }
            .AsQueryable().BuildMockDbSet();
        _dbContext.Candidates.Returns(candidateMockSet);

        var handler = new GetCandidateByIdQueryHandler(_dbContext, _tenantContext, _blobStorage);
        var query = new GetCandidateByIdQuery
        {
            RecruitmentId = recruitment.Id,
            CandidateId = candidate.Id,
        };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Id.Should().Be(candidate.Id);
        result.FullName.Should().Be("Alice");
        result.Email.Should().Be("alice@example.com");
        result.PhoneNumber.Should().Be("555-1234");
        result.Location.Should().Be("NYC");
        result.CurrentWorkflowStepName.Should().Be("Screening");
        result.CurrentOutcomeStatus.Should().Be("NotStarted");
    }

    [Test]
    public async Task Handle_ValidRequest_IncludesOutcomeHistoryWithStepNames()
    {
        var userId = Guid.NewGuid();
        var recruitment = SetUpRecruitmentWithSteps(userId);
        var steps = recruitment.Steps.OrderBy(s => s.Order).ToList();

        var candidate = Candidate.Create(
            recruitment.Id, "Alice", "alice@example.com", null, null, DateTimeOffset.UtcNow);
        candidate.RecordOutcome(steps[0].Id, OutcomeStatus.Pass, userId);

        var candidateMockSet = new List<Candidate> { candidate }
            .AsQueryable().BuildMockDbSet();
        _dbContext.Candidates.Returns(candidateMockSet);

        var handler = new GetCandidateByIdQueryHandler(_dbContext, _tenantContext, _blobStorage);
        var query = new GetCandidateByIdQuery
        {
            RecruitmentId = recruitment.Id,
            CandidateId = candidate.Id,
        };

        var result = await handler.Handle(query, CancellationToken.None);

        result.OutcomeHistory.Should().HaveCount(1);
        result.OutcomeHistory[0].WorkflowStepName.Should().Be("Screening");
        result.OutcomeHistory[0].Status.Should().Be("Pass");
        result.OutcomeHistory[0].StepOrder.Should().Be(1);
    }

    [Test]
    public async Task Handle_ValidRequest_IncludesDocumentsWithSasUrls()
    {
        var userId = Guid.NewGuid();
        var recruitment = SetUpRecruitmentWithSteps(userId);

        var candidate = Candidate.Create(
            recruitment.Id, "Alice", "alice@example.com", null, null, DateTimeOffset.UtcNow);
        candidate.AttachDocument("CV", "recruitments/cvs/test.pdf");

        var candidateMockSet = new List<Candidate> { candidate }
            .AsQueryable().BuildMockDbSet();
        _dbContext.Candidates.Returns(candidateMockSet);

        var sasUri = new Uri("https://storage.blob.core.windows.net/documents/test.pdf?sv=2024&sig=abc");
        _blobStorage.GenerateSasUri("documents", "recruitments/cvs/test.pdf", Arg.Any<TimeSpan>())
            .Returns(sasUri);

        var handler = new GetCandidateByIdQueryHandler(_dbContext, _tenantContext, _blobStorage);
        var query = new GetCandidateByIdQuery
        {
            RecruitmentId = recruitment.Id,
            CandidateId = candidate.Id,
        };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Documents.Should().HaveCount(1);
        result.Documents[0].DocumentType.Should().Be("CV");
        result.Documents[0].SasUrl.Should().Contain("sig=abc");
    }

    [Test]
    public async Task Handle_NonMemberUser_ThrowsForbiddenAccessException()
    {
        var creatorId = Guid.NewGuid();
        var nonMemberId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(nonMemberId);

        var recruitment = Recruitment.Create("Test", null, creatorId);
        var recruitmentMockSet = new List<Recruitment> { recruitment }
            .AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(recruitmentMockSet);

        var handler = new GetCandidateByIdQueryHandler(_dbContext, _tenantContext, _blobStorage);
        var query = new GetCandidateByIdQuery
        {
            RecruitmentId = recruitment.Id,
            CandidateId = Guid.NewGuid(),
        };

        var act = () => handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task Handle_CandidateNotFound_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        var recruitment = SetUpRecruitmentWithSteps(userId);

        var candidateMockSet = new List<Candidate>()
            .AsQueryable().BuildMockDbSet();
        _dbContext.Candidates.Returns(candidateMockSet);

        var handler = new GetCandidateByIdQueryHandler(_dbContext, _tenantContext, _blobStorage);
        var query = new GetCandidateByIdQuery
        {
            RecruitmentId = recruitment.Id,
            CandidateId = Guid.NewGuid(),
        };

        var act = () => handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
