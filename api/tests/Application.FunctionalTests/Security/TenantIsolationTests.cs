using api.Application.Features.Candidates.Commands.AssignDocument;
using api.Application.Features.Candidates.Commands.CreateCandidate;
using api.Application.Features.Candidates.Queries.GetCandidateById;
using api.Application.Features.Candidates.Queries.GetCandidates;
using api.Application.Features.Recruitments.Commands.AddWorkflowStep;
using api.Application.Features.Recruitments.Commands.CreateRecruitment;
using api.Application.Features.Screening.Commands.RecordOutcome;
using api.Application.Features.Screening.Queries.GetCandidateOutcomeHistory;
using api.Application.Features.Recruitments.Queries.GetRecruitmentById;
using api.Domain.Enums;
using ForbiddenAccessException = api.Application.Common.Exceptions.ForbiddenAccessException;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.FunctionalTests.Security;

using static Testing;

/// <summary>
/// Verifies that users who are members of one recruitment cannot access
/// candidates, outcomes, or documents belonging to a different recruitment.
/// These tests require SQL Server and run in CI only.
/// </summary>
public class TenantIsolationTests : BaseTestFixture
{
    private Guid _recruitmentA;
    private Guid _recruitmentB;
    private Guid _candidateA;
    private Guid _stepA;

    /// <summary>
    /// Sets up two recruitments owned by different users, each with a candidate.
    /// User A owns Recruitment A; User B owns Recruitment B.
    /// After setup, the active user is User B (the "attacker").
    /// </summary>
    private async Task SetUpTwoRecruitments()
    {
        // User A creates Recruitment A with a candidate
        await RunAsUserAsync("userA@local", Array.Empty<string>());
        _recruitmentA = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Recruitment A",
        });
        var stepResult = await SendAsync(new AddWorkflowStepCommand
        {
            RecruitmentId = _recruitmentA,
            Name = "Screening",
            Order = 1,
        });
        _stepA = stepResult.Id;
        _candidateA = await SendAsync(new CreateCandidateCommand
        {
            RecruitmentId = _recruitmentA,
            FullName = "Alice (A)",
            Email = "alice-a@example.com",
        });

        // User B creates Recruitment B (separate tenant)
        await RunAsUserAsync("userB@local", Array.Empty<string>());
        _recruitmentB = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Recruitment B",
        });
    }

    [Test]
    public async Task GetCandidates_ForOtherRecruitment_ThrowsForbidden()
    {
        await SetUpTwoRecruitments();

        // User B tries to list candidates in Recruitment A
        var act = () => SendAsync(new GetCandidatesQuery
        {
            RecruitmentId = _recruitmentA,
        });

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task GetCandidateById_ForOtherRecruitment_ThrowsForbidden()
    {
        await SetUpTwoRecruitments();

        // User B tries to get a specific candidate from Recruitment A
        var act = () => SendAsync(new GetCandidateByIdQuery
        {
            RecruitmentId = _recruitmentA,
            CandidateId = _candidateA,
        });

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task GetCandidateById_WithWrongRecruitmentId_ThrowsNotFound()
    {
        await SetUpTwoRecruitments();

        // User B references Candidate A but claims it belongs to Recruitment B
        var act = () => SendAsync(new GetCandidateByIdQuery
        {
            RecruitmentId = _recruitmentB,
            CandidateId = _candidateA,
        });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task RecordOutcome_ForOtherRecruitment_ThrowsForbidden()
    {
        await SetUpTwoRecruitments();

        // User B tries to record an outcome on Recruitment A's candidate
        var act = () => SendAsync(new RecordOutcomeCommand(
            RecruitmentId: _recruitmentA,
            CandidateId: _candidateA,
            WorkflowStepId: _stepA,
            Outcome: OutcomeStatus.Pass,
            Reason: "cross-tenant attack"
        ));

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task AssignDocument_ToOtherRecruitment_ThrowsForbidden()
    {
        await SetUpTwoRecruitments();

        // User B tries to assign a document to Recruitment A's candidate
        var act = () => SendAsync(new AssignDocumentCommand(
            RecruitmentId: _recruitmentA,
            CandidateId: _candidateA,
            DocumentBlobUrl: $"{_recruitmentA}/candidates/fake.pdf",
            DocumentName: "fake.pdf",
            ImportSessionId: null
        ));

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task GetOutcomeHistory_ForOtherRecruitment_ThrowsForbidden()
    {
        await SetUpTwoRecruitments();

        // User B tries to get outcome history for Recruitment A's candidate
        var act = () => SendAsync(new GetCandidateOutcomeHistoryQuery(
            RecruitmentId: _recruitmentA,
            CandidateId: _candidateA
        ));

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }
}
