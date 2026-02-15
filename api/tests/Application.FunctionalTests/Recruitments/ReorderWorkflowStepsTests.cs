using api.Application.Features.Recruitments.Commands.AddWorkflowStep;
using api.Application.Features.Recruitments.Commands.CreateRecruitment;
using api.Application.Features.Recruitments.Commands.ReorderWorkflowSteps;
using api.Application.Features.Recruitments.Queries.GetRecruitmentById;
using NotFoundException = api.Application.Common.Exceptions.NotFoundException;
using ValidationException = api.Application.Common.Exceptions.ValidationException;

namespace api.Application.FunctionalTests.Recruitments;

using static Testing;

public class ReorderWorkflowStepsTests : BaseTestFixture
{
    [Test]
    public async Task ReorderWorkflowSteps_ValidReordering_UpdatesStepOrders()
    {
        await RunAsDefaultUserAsync();
        var recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Test Recruitment",
        });
        var step1 = await SendAsync(new AddWorkflowStepCommand
        {
            RecruitmentId = recruitmentId,
            Name = "Screening",
            Order = 1,
        });
        var step2 = await SendAsync(new AddWorkflowStepCommand
        {
            RecruitmentId = recruitmentId,
            Name = "Interview",
            Order = 2,
        });
        var step3 = await SendAsync(new AddWorkflowStepCommand
        {
            RecruitmentId = recruitmentId,
            Name = "Final Review",
            Order = 3,
        });

        // Reverse the order: Final Review -> Interview -> Screening
        await SendAsync(new ReorderWorkflowStepsCommand
        {
            RecruitmentId = recruitmentId,
            Steps =
            [
                new StepOrderDto { StepId = step3.Id, Order = 1 },
                new StepOrderDto { StepId = step2.Id, Order = 2 },
                new StepOrderDto { StepId = step1.Id, Order = 3 },
            ],
        });

        var recruitment = await SendAsync(new GetRecruitmentByIdQuery { Id = recruitmentId });

        recruitment.Steps.Should().HaveCount(3);
        recruitment.Steps.First(s => s.Name == "Final Review").Order.Should().Be(1);
        recruitment.Steps.First(s => s.Name == "Interview").Order.Should().Be(2);
        recruitment.Steps.First(s => s.Name == "Screening").Order.Should().Be(3);
    }

    [Test]
    public async Task ReorderWorkflowSteps_NonContiguousOrders_ThrowsArgumentException()
    {
        await RunAsDefaultUserAsync();
        var recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Test Recruitment",
        });
        var step1 = await SendAsync(new AddWorkflowStepCommand
        {
            RecruitmentId = recruitmentId,
            Name = "Screening",
            Order = 1,
        });
        var step2 = await SendAsync(new AddWorkflowStepCommand
        {
            RecruitmentId = recruitmentId,
            Name = "Interview",
            Order = 2,
        });

        var act = () => SendAsync(new ReorderWorkflowStepsCommand
        {
            RecruitmentId = recruitmentId,
            Steps =
            [
                new StepOrderDto { StepId = step1.Id, Order = 1 },
                new StepOrderDto { StepId = step2.Id, Order = 5 }, // Gap: 1, 5 is not contiguous
            ],
        });

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task ReorderWorkflowSteps_NonMember_ThrowsNotFoundException()
    {
        await RunAsUserAsync("userA@local", Array.Empty<string>());
        var recruitmentId = await SendAsync(new CreateRecruitmentCommand
        {
            Title = "Test Recruitment",
        });
        var step1 = await SendAsync(new AddWorkflowStepCommand
        {
            RecruitmentId = recruitmentId,
            Name = "Screening",
            Order = 1,
        });

        await RunAsUserAsync("userB@local", Array.Empty<string>());

        var act = () => SendAsync(new ReorderWorkflowStepsCommand
        {
            RecruitmentId = recruitmentId,
            Steps =
            [
                new StepOrderDto { StepId = step1.Id, Order = 1 },
            ],
        });

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
