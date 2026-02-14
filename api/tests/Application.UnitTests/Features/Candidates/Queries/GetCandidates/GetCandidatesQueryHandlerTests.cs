using api.Application.Common.Interfaces;
using api.Application.Features.Candidates.Queries.GetCandidates;
using api.Domain.Entities;
using api.Domain.Enums;
using FluentAssertions;
using MockQueryable.NSubstitute;
using NSubstitute;
using NUnit.Framework;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.UnitTests.Features.Candidates.Queries.GetCandidates;

[TestFixture]
public class GetCandidatesQueryHandlerTests
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
        _blobStorage.GenerateSasUri(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>())
            .Returns(new Uri("https://storage.blob.core.windows.net/documents/test.pdf?sig=mock"));
    }

    private (Recruitment recruitment, Guid userId) SetUpRecruitmentWithSteps()
    {
        var userId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(userId);

        var recruitment = Recruitment.Create("Test", null, userId);
        recruitment.AddStep("Screening", 1);
        recruitment.AddStep("Interview", 2);
        recruitment.AddStep("Final Review", 3);

        var recruitmentMockSet = new List<Recruitment> { recruitment }
            .AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(recruitmentMockSet);

        return (recruitment, userId);
    }

    private void SetUpCandidates(params Candidate[] candidates)
    {
        var candidateMockSet = candidates.ToList().AsQueryable().BuildMockDbSet();
        _dbContext.Candidates.Returns(candidateMockSet);
    }

    [Test]
    public async Task Handle_ValidRequest_ReturnsPaginatedCandidates()
    {
        var (recruitment, _) = SetUpRecruitmentWithSteps();

        var candidate1 = Candidate.Create(
            recruitment.Id, "Alice", "alice@example.com", null, null, DateTimeOffset.UtcNow);
        var candidate2 = Candidate.Create(
            recruitment.Id, "Bob", "bob@example.com", null, null, DateTimeOffset.UtcNow);
        SetUpCandidates(candidate1, candidate2);

        var handler = new GetCandidatesQueryHandler(_dbContext, _tenantContext, _blobStorage);
        var query = new GetCandidatesQuery
        {
            RecruitmentId = recruitment.Id,
            Page = 1,
            PageSize = 50,
        };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(50);
    }

    [Test]
    public async Task Handle_UserNotMember_ThrowsForbiddenAccessException()
    {
        var creatorId = Guid.NewGuid();
        var nonMemberId = Guid.NewGuid();
        _tenantContext.UserGuid.Returns(nonMemberId);

        var recruitment = Recruitment.Create("Test", null, creatorId);
        var recruitmentMockSet = new List<Recruitment> { recruitment }
            .AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(recruitmentMockSet);

        var handler = new GetCandidatesQueryHandler(_dbContext, _tenantContext, _blobStorage);
        var query = new GetCandidatesQuery
        {
            RecruitmentId = recruitment.Id,
        };

        var act = () => handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task Handle_RecruitmentNotFound_ThrowsNotFoundException()
    {
        _tenantContext.UserGuid.Returns(Guid.NewGuid());

        var recruitmentMockSet = new List<Recruitment>()
            .AsQueryable().BuildMockDbSet();
        _dbContext.Recruitments.Returns(recruitmentMockSet);

        var handler = new GetCandidatesQueryHandler(_dbContext, _tenantContext, _blobStorage);
        var query = new GetCandidatesQuery
        {
            RecruitmentId = Guid.NewGuid(),
        };

        var act = () => handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task Handle_WithSearchTerm_FiltersByNameSubstring()
    {
        var (recruitment, _) = SetUpRecruitmentWithSteps();

        var alice = Candidate.Create(
            recruitment.Id, "Alice Johnson", "alice@example.com", null, null, DateTimeOffset.UtcNow);
        var bob = Candidate.Create(
            recruitment.Id, "Bob Smith", "bob@example.com", null, null, DateTimeOffset.UtcNow);
        SetUpCandidates(alice, bob);

        var handler = new GetCandidatesQueryHandler(_dbContext, _tenantContext, _blobStorage);
        var query = new GetCandidatesQuery
        {
            RecruitmentId = recruitment.Id,
            Search = "Alice",
        };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].FullName.Should().Be("Alice Johnson");
        result.TotalCount.Should().Be(1);
    }

    [Test]
    public async Task Handle_WithSearchTerm_FiltersByEmailSubstring()
    {
        var (recruitment, _) = SetUpRecruitmentWithSteps();

        var alice = Candidate.Create(
            recruitment.Id, "Alice", "alice@acme.com", null, null, DateTimeOffset.UtcNow);
        var bob = Candidate.Create(
            recruitment.Id, "Bob", "bob@contoso.com", null, null, DateTimeOffset.UtcNow);
        SetUpCandidates(alice, bob);

        var handler = new GetCandidatesQueryHandler(_dbContext, _tenantContext, _blobStorage);
        var query = new GetCandidatesQuery
        {
            RecruitmentId = recruitment.Id,
            Search = "contoso",
        };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].Email.Should().Be("bob@contoso.com");
    }

    [Test]
    public async Task Handle_WithStepIdFilter_ReturnsOnlyCandidatesAtStep()
    {
        var (recruitment, _) = SetUpRecruitmentWithSteps();
        var steps = recruitment.Steps.OrderBy(s => s.Order).ToList();
        var screeningStep = steps[0];
        var interviewStep = steps[1];

        // Alice: no outcomes => at Screening (first step)
        var alice = Candidate.Create(
            recruitment.Id, "Alice", "alice@example.com", null, null, DateTimeOffset.UtcNow);

        // Bob: passed Screening => at Interview
        var bob = Candidate.Create(
            recruitment.Id, "Bob", "bob@example.com", null, null, DateTimeOffset.UtcNow);
        bob.RecordOutcome(screeningStep.Id, OutcomeStatus.Pass, Guid.NewGuid());

        SetUpCandidates(alice, bob);

        var handler = new GetCandidatesQueryHandler(_dbContext, _tenantContext, _blobStorage);
        var query = new GetCandidatesQuery
        {
            RecruitmentId = recruitment.Id,
            StepId = interviewStep.Id,
        };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].FullName.Should().Be("Bob");
        result.Items[0].CurrentWorkflowStepName.Should().Be("Interview");
    }

    [Test]
    public async Task Handle_WithOutcomeStatusFilter_ReturnsOnlyCandidatesWithStatus()
    {
        var (recruitment, _) = SetUpRecruitmentWithSteps();
        var steps = recruitment.Steps.OrderBy(s => s.Order).ToList();
        var screeningStep = steps[0];

        // Alice: failed screening
        var alice = Candidate.Create(
            recruitment.Id, "Alice", "alice@example.com", null, null, DateTimeOffset.UtcNow);
        alice.RecordOutcome(screeningStep.Id, OutcomeStatus.Fail, Guid.NewGuid());

        // Bob: no outcomes => NotStarted
        var bob = Candidate.Create(
            recruitment.Id, "Bob", "bob@example.com", null, null, DateTimeOffset.UtcNow);

        SetUpCandidates(alice, bob);

        var handler = new GetCandidatesQueryHandler(_dbContext, _tenantContext, _blobStorage);
        var query = new GetCandidatesQuery
        {
            RecruitmentId = recruitment.Id,
            OutcomeStatus = OutcomeStatus.Fail,
        };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].FullName.Should().Be("Alice");
        result.Items[0].CurrentOutcomeStatus.Should().Be("Fail");
    }

    [Test]
    public async Task Handle_WithCombinedFilters_AppliesAndLogic()
    {
        var (recruitment, _) = SetUpRecruitmentWithSteps();
        var steps = recruitment.Steps.OrderBy(s => s.Order).ToList();
        var screeningStep = steps[0];

        // Alice: failed screening => at Screening with Fail
        var alice = Candidate.Create(
            recruitment.Id, "Alice", "alice@example.com", null, null, DateTimeOffset.UtcNow);
        alice.RecordOutcome(screeningStep.Id, OutcomeStatus.Fail, Guid.NewGuid());

        // Bob: no outcomes => at Screening with NotStarted
        var bob = Candidate.Create(
            recruitment.Id, "Bob", "bob@example.com", null, null, DateTimeOffset.UtcNow);

        // Carol: passed screening => at Interview with NotStarted
        var carol = Candidate.Create(
            recruitment.Id, "Carol", "carol@example.com", null, null, DateTimeOffset.UtcNow);
        carol.RecordOutcome(screeningStep.Id, OutcomeStatus.Pass, Guid.NewGuid());

        SetUpCandidates(alice, bob, carol);

        var handler = new GetCandidatesQueryHandler(_dbContext, _tenantContext, _blobStorage);
        // Filter: at Screening step AND Fail status => only Alice
        var query = new GetCandidatesQuery
        {
            RecruitmentId = recruitment.Id,
            StepId = screeningStep.Id,
            OutcomeStatus = OutcomeStatus.Fail,
        };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].FullName.Should().Be("Alice");
    }

    [Test]
    public async Task Handle_WithSearchAndFilters_PaginationReflectsFilteredCount()
    {
        var (recruitment, _) = SetUpRecruitmentWithSteps();

        // Create 5 candidates, only 2 match the search
        var candidates = new List<Candidate>();
        for (var i = 0; i < 5; i++)
        {
            var name = i < 2 ? $"TargetName{i}" : $"Other{i}";
            candidates.Add(Candidate.Create(
                recruitment.Id, name, $"c{i}@example.com", null, null, DateTimeOffset.UtcNow));
        }

        SetUpCandidates(candidates.ToArray());

        var handler = new GetCandidatesQueryHandler(_dbContext, _tenantContext, _blobStorage);
        var query = new GetCandidatesQuery
        {
            RecruitmentId = recruitment.Id,
            Search = "TargetName",
            Page = 1,
            PageSize = 50,
        };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Test]
    public async Task Handle_IncludesCurrentStepInfoInDto()
    {
        var (recruitment, _) = SetUpRecruitmentWithSteps();

        var candidate = Candidate.Create(
            recruitment.Id, "Alice", "alice@example.com", null, null, DateTimeOffset.UtcNow);
        SetUpCandidates(candidate);

        var handler = new GetCandidatesQueryHandler(_dbContext, _tenantContext, _blobStorage);
        var query = new GetCandidatesQuery
        {
            RecruitmentId = recruitment.Id,
        };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].CurrentWorkflowStepName.Should().Be("Screening");
        result.Items[0].CurrentOutcomeStatus.Should().Be("NotStarted");
    }
}
