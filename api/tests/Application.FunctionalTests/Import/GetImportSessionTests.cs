using api.Application.Features.Import.Queries.GetImportSession;
using api.Application.Features.Recruitments.Commands.CreateRecruitment;
using api.Domain.Entities;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;

namespace api.Application.FunctionalTests.Import;

using static Testing;

public class GetImportSessionTests : BaseTestFixture
{
    [Test]
    public async Task Handle_ValidSession_ReturnsImportSessionDto()
    {
        var userId = await RunAsDefaultUserAsync();
        var recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Test Recruitment",
        });

        var session = ImportSession.Create(recruitmentId, Guid.Parse(userId), "candidates.xlsx");
        session.MarkCompleted(5, 2, 1, 0);
        await AddAsync(session);

        var result = await SendAsync(new GetImportSessionQuery(session.Id));

        result.Should().NotBeNull();
        result.Id.Should().Be(session.Id);
        result.RecruitmentId.Should().Be(recruitmentId);
        result.Status.Should().Be("Completed");
        result.SourceFileName.Should().Be("candidates.xlsx");
        result.CreatedCount.Should().Be(5);
        result.UpdatedCount.Should().Be(2);
        result.ErroredCount.Should().Be(1);
    }

    [Test]
    public async Task Handle_NonExistentSession_ThrowsNotFoundException()
    {
        await RunAsDefaultUserAsync();

        var act = () => SendAsync(new GetImportSessionQuery(Guid.NewGuid()));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task Handle_NonMember_ThrowsNotFoundException()
    {
        var userId = await RunAsDefaultUserAsync();
        var recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Test Recruitment",
        });

        var session = ImportSession.Create(recruitmentId, Guid.Parse(userId), "candidates.xlsx");
        session.MarkCompleted(3, 0, 0, 0);
        await AddAsync(session);

        await RunAsUserAsync("other@local", Array.Empty<string>());

        var act = () => SendAsync(new GetImportSessionQuery(session.Id));

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
