using api.Application.Features.Candidates.Commands.CreateCandidate;
using api.Application.Features.Import.Commands.ResolveMatchConflict;
using api.Application.Features.Recruitments.Commands.CreateRecruitment;
using api.Domain.Entities;
using api.Domain.Enums;
using api.Domain.ValueObjects;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.FunctionalTests.Import;

using static Testing;

public class ResolveMatchConflictTests : BaseTestFixture
{
    private async Task<(Guid RecruitmentId, ImportSession Session, Guid CandidateId)> SetUpSessionWithFlaggedRow()
    {
        var userId = await RunAsDefaultUserAsync();
        var recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Test Recruitment",
        });
        var candidateId = await SendAsync(new CreateCandidateCommand
        {
            RecruitmentId = recruitmentId,
            FullName = "Alice Johnson",
            Email = "alice@example.com",
        });

        var session = ImportSession.Create(recruitmentId, Guid.Parse(userId), "candidates.xlsx");
        session.AddRowResult(new ImportRowResult(
            rowNumber: 0,
            candidateEmail: "alice@example.com",
            action: ImportRowAction.Flagged,
            errorMessage: null,
            fullName: "Alice J.",
            phoneNumber: "+1-555-0101",
            location: "Seattle",
            dateApplied: DateTimeOffset.UtcNow,
            matchedCandidateId: candidateId));
        session.MarkCompleted(0, 0, 0, 1);
        await AddAsync(session);

        return (recruitmentId, session, candidateId);
    }

    [Test]
    public async Task Handle_ConfirmMatch_ResolvesConflictAndUpdatesCandidate()
    {
        var (recruitmentId, session, candidateId) = await SetUpSessionWithFlaggedRow();

        var result = await SendAsync(new ResolveMatchConflictCommand(
            ImportSessionId: session.Id,
            MatchIndex: 0,
            Action: "Confirm"
        ));

        result.Should().NotBeNull();
        result.MatchIndex.Should().Be(0);
        result.Action.Should().Be("Confirmed");
        result.CandidateEmail.Should().Be("alice@example.com");
    }

    [Test]
    public async Task Handle_RejectMatch_CreatesNewCandidate()
    {
        var (recruitmentId, session, candidateId) = await SetUpSessionWithFlaggedRow();

        var result = await SendAsync(new ResolveMatchConflictCommand(
            ImportSessionId: session.Id,
            MatchIndex: 0,
            Action: "Reject"
        ));

        result.Should().NotBeNull();
        result.MatchIndex.Should().Be(0);
        result.Action.Should().Be("Rejected");
    }

    [Test]
    public async Task Handle_NonExistentSession_ThrowsNotFoundException()
    {
        await RunAsDefaultUserAsync();

        var act = () => SendAsync(new ResolveMatchConflictCommand(
            ImportSessionId: Guid.NewGuid(),
            MatchIndex: 0,
            Action: "Confirm"
        ));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task Handle_NonMember_ThrowsNotFoundException()
    {
        var (recruitmentId, session, candidateId) = await SetUpSessionWithFlaggedRow();

        await RunAsUserAsync("other@local", Array.Empty<string>());

        var act = () => SendAsync(new ResolveMatchConflictCommand(
            ImportSessionId: session.Id,
            MatchIndex: 0,
            Action: "Confirm"
        ));

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
