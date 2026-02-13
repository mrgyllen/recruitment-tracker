namespace api.Domain.Exceptions;

public class InvalidWorkflowTransitionException : Exception
{
    public InvalidWorkflowTransitionException(string from, string to)
        : base($"Invalid status transition from '{from}' to '{to}'.")
    {
    }
}
