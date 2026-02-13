namespace api.Domain.Exceptions;

public class DuplicateStepNameException : Exception
{
    public DuplicateStepNameException(string stepName)
        : base($"A workflow step named '{stepName}' already exists in this recruitment.")
    {
    }
}
