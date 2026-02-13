namespace api.Domain.Exceptions;

public class StepHasOutcomesException : Exception
{
    public StepHasOutcomesException(Guid stepId)
        : base($"Workflow step {stepId} cannot be removed because it has recorded outcomes.")
    {
    }
}
