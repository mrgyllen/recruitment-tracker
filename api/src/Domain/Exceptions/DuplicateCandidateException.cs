namespace api.Domain.Exceptions;

public class DuplicateCandidateException : Exception
{
    public DuplicateCandidateException(string email, Guid recruitmentId)
        : base($"A candidate with email '{email}' already exists in recruitment {recruitmentId}.")
    {
    }
}
